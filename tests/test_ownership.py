"""Ownership must survive membership changing, because C3 says it always is.

Decision 134 measured the case for concept partitioning: pooled capacity is
identical to dimension splitting, but lone-node capacity is **sixteen times
larger at sixteen nodes** and grows with the network. That arrangement needs to
know which node owns a concept.

**The obvious answer is `hash(token) % nodes` and it is disqualified by C3.**
Changing the node count remaps nearly every key, and C3's premise is that
machines leave without warning constantly. `test_modulo_would_move_nearly
_everything` is the test that makes that concrete rather than asserted.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.ownership import REPLICAS, Ring, moved

CONCEPTS = 4096


def modulo_moved(before: int, after: int, concepts: int = CONCEPTS) -> float:
    """What plain `hash % nodes` would move. The thing being avoided."""
    keys = np.arange(concepts)
    return float(((keys % before) != (keys % after)).mean())


class ChurnIsAffordable(unittest.TestCase):
    """The K/n guarantee, checked rather than assumed."""

    def test_adding_one_node_moves_about_one_nth(self):
        for nodes in (2, 4, 8, 16, 32):
            share = moved(Ring(nodes - 1), Ring(nodes), CONCEPTS)
            self.assertLess(
                share, 2.0 / nodes,
                f"adding the {nodes}th node moved {share:.3f} of concepts, "
                f"well past the ~{1 / nodes:.3f} the ring is for")

    def test_modulo_would_move_nearly_everything(self):
        """The comparison that justifies the whole module.

        Without this the ring looks like complexity for its own sake. With it,
        the alternative is visible: a single machine joining would relocate the
        entire store, and C3 says that happens constantly.
        """
        for nodes in (4, 8, 16):
            ring = moved(Ring(nodes - 1), Ring(nodes), CONCEPTS)
            naive = modulo_moved(nodes - 1, nodes, CONCEPTS)
            self.assertGreater(
                naive, 0.5,
                "modulo is supposed to be catastrophic here; if it is not, "
                "this test is not making the case it claims to")
            # `naive / 2` rather than `naive / 3`, and the change is a
            # CORRECTION rather than a loosening. At 4 nodes modulo moves 0.75,
            # so a third of it is 0.25 -- **below the 1/n the guarantee actually
            # promises**, which no correct ring can beat except by luck. It
            # passed only because the concept-domain collision (see
            # `test_SMALL_concept_ids_are_spread_like_any_others`) pinned 64
            # concepts to one node so they never moved.
            #
            # A threshold tighter than the property being tested is a test that
            # fails when the code becomes correct, and this one did.
            self.assertLess(
                ring, naive / 2,
                f"at {nodes} nodes the ring moved {ring:.3f} against modulo's "
                f"{naive:.3f} -- not the improvement the K/n guarantee "
                f"promises")
            self.assertLess(
                ring, 1.5 / nodes,
                f"at {nodes} nodes the ring moved {ring:.3f}, well past the "
                f"~{1 / nodes:.3f} the K/n guarantee promises")

    def test_losing_a_node_only_moves_what_it_held(self):
        """A departure must not disturb concepts it never owned."""
        before, after = Ring(8), Ring(7)
        keys = np.arange(CONCEPTS)
        was, now = before.owners(keys), after.owners(keys)
        # Node 7 is the one that no longer exists.
        untouched = was != 7
        changed = (was != now) & untouched
        self.assertLess(
            changed.mean(), 0.10,
            f"{changed.mean():.1%} of concepts owned by SURVIVING nodes "
            f"changed hands, so a departure is disturbing more than its own "
            f"share")


class TheRingIsNotTooLumpy(unittest.TestCase):
    """Virtual nodes exist for this, and the cost of the guess is measured."""

    def test_no_node_holds_a_wildly_unfair_share(self):
        for nodes in (2, 4, 8, 16, 32):
            worst = Ring(nodes).balance(CONCEPTS)
            self.assertLess(
                worst, 1.6,
                f"at {nodes} nodes the busiest holds {worst:.2f}x its fair "
                f"share; `REPLICAS` is too low")

    def test_one_replica_per_node_IS_lumpy(self):
        """The control, so the default is justified rather than decorative.

        If a single position per node were as even as sixty-four, `REPLICAS`
        would be complexity with nothing behind it.
        """
        lumpy = max(Ring(n, replicas=1).balance(CONCEPTS) for n in (8, 16, 32))
        even = max(Ring(n).balance(CONCEPTS) for n in (8, 16, 32))
        self.assertGreater(
            lumpy, even * 1.3,
            f"one replica per node is {lumpy:.2f}x fair share against "
            f"{even:.2f}x with the default, so virtual nodes are not earning "
            f"their place")


class OwnershipIsDerivedNotStored(unittest.TestCase):

    def test_the_same_ring_answers_the_same_way(self):
        a, b = Ring(8), Ring(8)
        keys = np.arange(256)
        np.testing.assert_array_equal(a.owners(keys), b.owners(keys))

    def test_a_different_seed_gives_a_different_ring(self):
        """Otherwise the seed is inert and every deployment shares a layout."""
        keys = np.arange(256)
        self.assertFalse(np.array_equal(Ring(8, seed=0).owners(keys),
                                        Ring(8, seed=1).owners(keys)))

    def test_every_owner_is_a_real_node(self):
        owners = Ring(5).owners(np.arange(CONCEPTS))
        self.assertTrue(((owners >= 0) & (owners < 5)).all())
        self.assertEqual(len(set(owners.tolist())), 5,
                         "some node owns nothing at all")

    def test_SMALL_concept_ids_are_spread_like_any_others(self):
        """**The one the model actually depends on, and it was broken.**

        Token ids start at 0 and a vocabulary is small, so every concept the
        model routes is below `REPLICAS`. The original domain tag put concepts
        at `(seed, 1, concept)` and node `n`'s replica `r` at `(seed, n, r)` --
        the same tuple whenever `n == 1` -- so **every concept below 64 landed
        on node 1** and a partitioned model was a single-node model wearing a
        ring.

        The old test here asserted `CONCEPT_DOMAIN != 0` and passed throughout.
        Asserting a constant's value is not asserting the property it was chosen
        for; this asserts the property.

        `balance()` over 4096 concepts did not catch it either -- 64 of 4096 is
        1.6% and reads as 1.18. **The regime that matters was measured nowhere**,
        which is decision 63's lesson again: probe the bottom of the range.
        """
        for nodes in (2, 4, 8, 16):
            with self.subTest(nodes=nodes):
                owners = Ring(nodes, seed=0).owners(np.arange(REPLICAS))
                self.assertEqual(
                    len(set(owners.tolist())), nodes,
                    f"the first {REPLICAS} concepts reach only "
                    f"{len(set(owners.tolist()))} of {nodes} nodes")

    def test_a_concept_never_draws_a_NODE_position(self):
        """The structural version of the above, checked against the ring's own
        positions rather than against the spread it happens to produce.

        A concept landing exactly on a node's position is not merely unlucky:
        `searchsorted(side="left")` then returns that node every time, so the
        collision is systematic rather than a one-in-four-billion coincidence.
        """
        ring = Ring(4, seed=0)
        positions = set(ring._positions.tolist())
        for concept in range(4 * REPLICAS):
            self.assertNotIn(ring._concept_position(concept), positions,
                             f"concept {concept} sits exactly on a node")


class ImpossibleRingsAreRefused(unittest.TestCase):

    def test_a_ring_with_no_nodes(self):
        with self.assertRaises(ValueError):
            Ring(0)

    def test_a_node_with_no_positions_owns_nothing(self):
        with self.assertRaises(ValueError):
            Ring(4, replicas=0)


if __name__ == "__main__":
    unittest.main()
