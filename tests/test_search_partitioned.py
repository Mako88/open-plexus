"""A traversal over a store split across nodes must find what a whole one finds.

`search.py` read a single `np.ndarray`, and a concept-partitioned store has no such
thing — each binding lives on the node that owns it. `_reader` routes every pair read
through `keys.owner`, which is the same rule `keys.concept` uses for writes.

**The claim under test is that the DECISIONS agree.** With every node present, a
partitioned store must commit to the same relations, pass the same entities, and land on
an endpoint decoding to the same token.

**Not bit-identical, and the first draft of this file asserted that and failed.** The
owner always holds the binding being read, so the signal is the same — but a whole store
also carries interference from every other binding written anywhere, while a node carries
only interference from the concepts it owns. Different noise, same answer, and the
partitioned side has *less* of it. Asserting vector equality tested arithmetic nobody
relies on; asserting the decoded decisions tests what a result would be read from.

The failure this guards is quiet. A misrouted read returns *some* store's answer rather
than raising, so the walk still completes and still looks like a walk — it is simply
worse. Equality against the monolithic result is the only assertion that notices.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.keys import PairKeys
from openplexus.partitioned import ConceptStore
from openplexus.retrieval import SuperposedRead
from openplexus.search import beam, candidates, search, walk_from

WIDTH = 64
#: `FACT` is the marker, 2-7 entities, 8-11 relations. Small and disjoint so a
#: misrouted read lands somewhere a reader can name.
FACT = 0
VOCAB = 12
ENTITIES = range(2, 8)
RELATIONS = np.array([8, 9, 10, 11])
#: A chain: 2 -8-> 3 -9-> 4 -10-> 5, plus a branch off 2 so the search has a choice
#: to get wrong. Without the branch, out-degree 1 hides routing errors exactly as
#: it hid the missing-search capability decision 108 found.
FACTS = ((2, 8, 3), (3, 9, 4), (4, 10, 5), (2, 11, 6), (6, 9, 7))


def keys_for(route: str) -> PairKeys:
    return PairKeys(seed=3, spread=1.0 / np.sqrt(WIDTH), width=WIDTH,
                    start=VOCAB, route=route, markers=frozenset({FACT}))


def values(seed: int = 5) -> np.ndarray:
    """One value vector per token, which is also the decoder."""
    rng = np.random.default_rng(seed)
    wv = rng.normal(0.0, 1.0, (VOCAB, WIDTH))
    return wv / np.linalg.norm(wv, axis=1, keepdims=True)


def bindings():
    """Every `(previous, token) -> value token` the traversal needs.

    Both halves of the alternation `walk_from` documents: `key(FACT, entity)` gives
    the entity's relation, `key(entity, relation)` gives who it reaches.
    """
    for subject, relation, obj in FACTS:
        yield (FACT, subject), relation
        yield (subject, relation), obj


def whole(keys: PairKeys, wv: np.ndarray) -> np.ndarray:
    memory = np.zeros((WIDTH, WIDTH))
    for (previous, token), value in bindings():
        memory += np.outer(wv[value], keys.pair(previous, token))
    return memory


def split(keys: PairKeys, wv: np.ndarray, nodes: int = 4,
          replicas: int = 3) -> ConceptStore:
    """The same bindings, each written to the node that owns its pair.

    `keys.owner` and not a fresh rule: the whole point is that writes and reads
    agree, and picking the owner differently here would test a system nobody runs.
    """
    store = ConceptStore(nodes=nodes, width=WIDTH, seed=1, replicas=replicas)
    for (previous, token), value in bindings():
        store.write(keys.owner(previous, token),
                    keys.pair(previous, token), wv[value])
    return store


class APartitionedStoreAnswersLikeAWholeOne(unittest.TestCase):

    def setUp(self):
        self.wv = values()
        self.retrieval = SuperposedRead()

    def assert_walks_equal(self, one, other, message):
        self.assertEqual(one.relations, other.relations, message)
        self.assertEqual(one.entities, other.entities, message)
        # The DECODED endpoint, not the vector: see this module's docstring. A
        # node's interference differs from a whole store's by construction.
        self.assertEqual(int(np.argmax(self.wv @ one.endpoint)),
                         int(np.argmax(self.wv @ other.endpoint)), message)

    def test_walk_from_agrees_under_both_routes(self):
        for route in PairKeys.ROUTES:
            with self.subTest(route=route):
                keys = keys_for(route)
                args = (self.retrieval, keys, self.wv, FACT, 2, 8, 3)
                self.assert_walks_equal(
                    walk_from(whole(keys, self.wv), *args),
                    walk_from(split(keys, self.wv), *args),
                    f"route={route}")

    def test_candidates_agree_on_WHICH_branches(self):
        """The tokens, not the scores — scores carry the interference difference."""
        for route in PairKeys.ROUTES:
            with self.subTest(route=route):
                keys = keys_for(route)
                args = (self.retrieval, keys, self.wv, FACT, 2, 2, RELATIONS)
                self.assertEqual(
                    [t for t, _ in candidates(whole(keys, self.wv), *args)],
                    [t for t, _ in candidates(split(keys, self.wv), *args)])

    def test_search_agrees(self):
        for route in PairKeys.ROUTES:
            with self.subTest(route=route):
                keys = keys_for(route)
                args = (self.retrieval, keys, self.wv, FACT, 2, self.wv[5], 3)
                one = search(whole(keys, self.wv), *args, branches=2,
                             allowed=RELATIONS)
                other = search(split(keys, self.wv), *args, branches=2,
                               allowed=RELATIONS)
                self.assertEqual(len(one), len(other))
                self.assert_walks_equal(one[0], other[0], f"route={route}")

    def test_beam_agrees_which_is_the_one_that_matters(self):
        """`beam` is what note 065 measured at 713/713, so this is the comparison
        a partitioning result would be read against."""
        for route in PairKeys.ROUTES:
            with self.subTest(route=route):
                keys = keys_for(route)
                args = (self.retrieval, keys, self.wv, FACT, 2, self.wv[5], 3)
                one = beam(whole(keys, self.wv), *args, width=2, branches=2,
                           allowed=RELATIONS)
                other = beam(split(keys, self.wv), *args, width=2, branches=2,
                             allowed=RELATIONS)
                self.assertEqual(len(one), len(other))
                # THE BEST WALK, not every walk. Both stores return (8, 9, 10)
                # first -- the correct chain -- and disagree on the RUNNER-UP,
                # which is the interference difference this module's docstring
                # describes showing up where it should. The runner-up is not the
                # answer, and requiring it to match was this test asserting
                # something the mechanism never promised.
                self.assert_walks_equal(one[0], other[0], f"route={route}")
                self.assertEqual(one[0].relations, (8, 9, 10),
                                 "the fixture must be an instrument that gets "
                                 "the right answer, or agreement means nothing")

    def test_the_walk_is_not_trivially_short(self):
        """A guard on the guards.

        Every assertion above compares two walks, and two empty walks are equal.
        This asserts the traversal actually traverses, so the equality tests are
        comparing something.
        """
        keys = keys_for("first-concept")
        walk = walk_from(split(keys, self.wv), self.retrieval, keys, self.wv,
                         FACT, 2, 8, 3)
        self.assertEqual(len(walk.relations), 3)
        self.assertEqual(walk.entities, (3, 4))


class RoutingActuallyRoutes(unittest.TestCase):
    """Without this, the tests above would pass if `_reader` ignored the store.

    A ConceptStore with one node is a whole store wearing a hat: every concept
    lands on node 0, so equivalence proves nothing about routing. These assert the
    bindings really are spread and that reading the WRONG owner really does differ.
    """

    def setUp(self):
        self.wv = values()
        self.keys = keys_for("first-concept")

    def test_the_bindings_land_on_more_than_one_node(self):
        store = split(self.keys, self.wv, nodes=4)
        owners = {self.keys.owner(previous, token)
                  for (previous, token), _ in bindings()}
        self.assertGreater(len({store.owner(c) for c in owners}), 1,
                           "every concept on one node would make the "
                           "equivalence tests vacuous")

    def test_reading_the_wrong_owner_gives_a_different_answer(self):
        """So the equivalence above is evidence that routing is right, not that
        routing is irrelevant."""
        # replicas=1 so nodes are DISJOINT. At the default 3 of 4 nodes, a
        # "wrong" owner usually holds the same binding and this would fail for a
        # reason that has nothing to do with routing.
        store = split(self.keys, self.wv, nodes=4, replicas=1)
        previous, token = FACT, 2
        right = self.keys.owner(previous, token)
        key = self.keys.pair(previous, token)
        wrong = [c for c in range(VOCAB) if store.owner(c) != store.owner(right)]
        self.assertTrue(wrong, "need at least one differently-owned concept")
        self.assertFalse(
            np.allclose(store.matrix(right) @ key,
                        store.matrix(wrong[0]) @ key),
            "if any node answers the same, the split is not a split")


if __name__ == "__main__":
    unittest.main()
