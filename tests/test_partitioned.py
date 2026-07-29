"""A concept-partitioned store must answer from ONE node, and degrade honestly.

Decision 134 measured why this arrangement exists: pooled capacity is identical
to dimension splitting, and lone-node capacity is sixteen times larger at
sixteen nodes because a node owns whole concepts rather than a thinning slice of
everything.

Two properties carry that, and both are tested here rather than assumed:

- **A read touches one node.** If it needed all of them the arrangement would
  have the same barrier as the one it replaces, and amended C1 is about not
  needing a collective.
- **A departure removes concepts rather than degrading all of them.** That is
  the cost, and a test that did not check it would let the cost go unmeasured.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.partitioned import ConceptStore

WIDTH = 64


def pair(rng, width: int = WIDTH):
    spread = 1.0 / np.sqrt(width)
    return rng.normal(0.0, spread, width), rng.normal(0.0, spread, width)


class AReadTouchesOneNode(unittest.TestCase):

    def test_a_binding_written_is_a_binding_read(self):
        rng = np.random.default_rng(0)
        store = ConceptStore(nodes=8, width=WIDTH)
        key, value = pair(rng)
        store.write(7, key, value)
        got = store.read(7, key)
        # The key is unit-ish, so a single binding reads back close to its
        # value scaled by (key . key).
        self.assertGreater(
            float(got @ value) / (np.linalg.norm(got) * np.linalg.norm(value)),
            0.99, "a single stored binding did not read back")

    def test_only_the_HOLDERS_hold_it(self):
        """The load-bearing property. If every node held a piece, this would be
        dimension splitting with extra steps.

        `replicas` nodes, not one and not all -- replication is what makes churn
        survivable and it must not quietly become a broadcast.
        """
        rng = np.random.default_rng(1)
        store = ConceptStore(nodes=8, width=WIDTH, replicas=3)
        key, value = pair(rng)
        store.write(11, key, value)
        holding = sorted(i for i, s in enumerate(store._stores) if s.any())
        self.assertEqual(holding, sorted(store.holders(11)),
                         "a write touched something other than the holders")
        self.assertLess(len(holding), store.nodes,
                        "a write reached every node, which is a broadcast")

    def test_concepts_are_spread_across_nodes(self):
        """A store where one node owns everything is not partitioned."""
        rng = np.random.default_rng(2)
        store = ConceptStore(nodes=8, width=WIDTH)
        for concept in range(400):
            store.write(concept, *pair(rng))
        used = sum(1 for s in store._stores if s.any())
        self.assertEqual(used, 8, f"only {used} of 8 nodes hold anything")


class ADepartureRemovesRatherThanDegrades(unittest.TestCase):
    """The cost of this arrangement, measured rather than described."""

    def test_losing_a_node_loses_exactly_its_concepts(self):
        """AT ONE REPLICA, which is the unreplicated cost this class is about.
        `ReplicationMakesChurnSURVIVABLE` measures what fixes it."""
        rng = np.random.default_rng(3)
        store = ConceptStore(nodes=8, width=WIDTH, replicas=1)
        written = {}
        for concept in range(200):
            key, value = pair(rng)
            store.write(concept, key, value)
            written[concept] = (key, value)

        gone = 3
        theirs = {c for c in written if store.owner(c) == gone}
        self.assertGreater(len(theirs), 5, "node 3 owned too little to judge")
        store.lose(gone)

        for concept, (key, _) in written.items():
            got = store.read(concept, key)
            if concept in theirs:
                self.assertFalse(got.any(),
                                 "a lost node still answered for its concept")
            else:
                self.assertTrue(
                    got.any(),
                    f"concept {concept} was disturbed by a departure that did "
                    f"not own it -- this arrangement's whole claim is that "
                    f"survivors are untouched")

    def test_survivors_answer_exactly_as_before(self):
        """Not merely 'still answer' -- IDENTICALLY. A departure that changed a
        surviving answer would mean nodes were sharing after all."""
        rng = np.random.default_rng(4)
        store = ConceptStore(nodes=8, width=WIDTH, replicas=1)
        keys = {}
        for concept in range(120):
            key, value = pair(rng)
            store.write(concept, key, value)
            keys[concept] = key
        before = {c: store.read(c, k) for c, k in keys.items()}
        store.lose(5)
        for concept, key in keys.items():
            if store.owner(concept) == 5:
                continue
            np.testing.assert_array_equal(before[concept],
                                          store.read(concept, key))


class ReplicationMakesChurnSURVIVABLE(unittest.TestCase):
    """John's objection, 2026-07-29, and it was right.

    *"When nodes drop you just lose concepts -- that doesn't sound like a very
    robust system."* At one replica it is not. Measured, concepts still
    reachable:

        replicas   10% lost   25% lost   50% lost
        1             0.873      0.737      0.493
        3             1.000      0.989      0.896

    A concept is lost only when EVERY holder is gone, so the loss probability
    falls as the churn fraction to the power of the replica count.
    """

    def test_more_replicas_survive_more_churn(self):
        losses = []
        for replicas in (1, 2, 3):
            store = ConceptStore(nodes=20, width=16, replicas=replicas)
            for node in range(5):                      # a quarter gone
                store.lose(node)
            losses.append(store.survival(1024))
        self.assertLess(losses[0], 0.85, "one replica should lose a lot")
        self.assertGreater(losses[2], 0.95,
                           "three replicas should lose almost nothing")
        self.assertEqual(sorted(losses), losses,
                         "survival must rise with the replica count")

    def test_half_the_network_can_go_and_most_concepts_remain(self):
        """C3's hardest case, and the one G3 measured for the other
        arrangement (half removed, recovering to 0.924)."""
        store = ConceptStore(nodes=20, width=16, replicas=3)
        for node in range(10):
            store.lose(node)
        self.assertGreater(
            store.survival(1024), 0.85,
            "with half the network gone and three replicas, most concepts "
            "should still be reachable")

    def test_a_surviving_replica_still_RECOVERS_though_not_identically(self):
        """**Replicas are NOT identical copies, and that was a wrong assumption
        of mine rather than a bug.**

        Each node superposes every concept IT holds, and two holders of the same
        concept hold different other concepts — so the interference differs and
        the same key read from two replicas returns different vectors. The first
        version of this test asserted equality and failed by a relative
        difference of 48.

        What must hold is that the fallback still RECOVERS: the answer is
        nearest to the right value. That is the property a reader depends on;
        bit-identity was never available and asserting it would have pinned a
        fiction.

        Two consequences worth carrying:
        - **Replicas cannot be used to verify each other.** They legitimately
          disagree, so a mismatch is not evidence of corruption.
        - **Averaging across replicas should REDUCE interference**, since each
          carries independent noise. That is a mechanism nobody has measured
          and it is free where more than one holder is reachable.
        """
        rng = np.random.default_rng(9)
        store = ConceptStore(nodes=12, width=WIDTH, replicas=3)
        written = {}
        for concept in range(80):
            key, value = pair(rng)
            store.write(concept, key, value)
            written[concept] = (key, value)

        gone = store.holders(0)[0]
        store.lose(gone)
        checked = 0
        for concept, (key, value) in written.items():
            if gone not in store.holders(concept):
                continue
            checked += 1
            got = store.read(concept, key)
            self.assertTrue(got.any(), "a replicated concept became absent")
            similarity = float(got @ value) / (np.linalg.norm(got)
                                               * np.linalg.norm(value))
            self.assertGreater(
                similarity, 0.3,
                f"concept {concept} fell back to a replica and the answer no "
                f"longer resembles what was stored")
        self.assertGreater(checked, 5, "too few fallbacks to judge")

    def test_replicas_are_DISTINCT_nodes(self):
        """Virtual nodes mean the next positions clockwise are often the same
        machine wearing different labels, and three copies on one machine is a
        backup that dies with its original."""
        store = ConceptStore(nodes=8, width=16, replicas=3)
        for concept in range(200):
            holders = store.holders(concept)
            self.assertEqual(len(holders), len(set(holders)),
                             f"concept {concept} is replicated onto the same "
                             f"node more than once")

    def test_redundancy_DEPLETES_because_nothing_repairs_it(self):
        """The gap John's question surfaced, pinned so it is not forgotten.

        Nothing redistributes on a departure, so the replica count walks down
        and never recovers. Reads keep working until it reaches zero, which
        makes this the kind of degradation that is invisible right up until it
        is total.

        **This test asserts the DEFECT.** It should start failing the day
        repair is built, and that is the point -- the alternative is a silent
        assumption that three replicas stay three.
        """
        store = ConceptStore(nodes=20, width=16, replicas=3)
        fresh = sum(store.live_holders(c) for c in range(256)) / 256
        for node in range(10):
            store.lose(node)
        depleted = sum(store.live_holders(c) for c in range(256)) / 256
        self.assertAlmostEqual(fresh, 3.0, places=6)
        self.assertLess(
            depleted, 2.0,
            "redundancy did not deplete -- if something now repairs it, delete "
            "this test and say so in the commit")

    def test_replication_cost_is_reported(self):
        store = ConceptStore(nodes=8, width=16, replicas=3)
        self.assertEqual(store.numbers_per_concept, 3 * 16 * 16)


class StateIsReportedForEqualComparisons(unittest.TestCase):

    def test_numbers_held_counts_every_node(self):
        """g10-09 was retracted for comparing a model with a cache against one
        without at equal WIDTH rather than equal STATE."""
        self.assertEqual(ConceptStore(nodes=4, width=32).numbers_held,
                         4 * 32 * 32)


class ImpossibleStoresAreRefused(unittest.TestCase):

    def test_a_store_with_no_nodes(self):
        with self.assertRaises(ValueError):
            ConceptStore(nodes=0, width=WIDTH)


if __name__ == "__main__":
    unittest.main()
