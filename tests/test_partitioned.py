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

    def test_only_the_owning_node_holds_it(self):
        """The load-bearing property. If every node held a piece, this would be
        dimension splitting with extra steps."""
        rng = np.random.default_rng(1)
        store = ConceptStore(nodes=8, width=WIDTH)
        key, value = pair(rng)
        store.write(11, key, value)
        holding = [i for i, s in enumerate(store._stores) if s.any()]
        self.assertEqual(holding, [store.owner(11)],
                         "a write touched something other than the owner")

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
        rng = np.random.default_rng(3)
        store = ConceptStore(nodes=8, width=WIDTH)
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
        store = ConceptStore(nodes=8, width=WIDTH)
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
