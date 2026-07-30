"""Which node owns a binding, i.e. what `PairKeys.concept` names.

`concept` decides where a fact is filed, and note 072 found the default sends every
binding a traversal reads to a RELATION under a `FACT s r o` ordering. There are twenty
relations and always will be, so concept partitioning would cap below the ceiling it
exists to lift.

Note 073 measured five rules and `first-concept` is the one that works. What is asserted
here:

- **The default has not moved.** Every number in this project was taken under
  `route="current"`, decision 134's included, and decision 74 is the entry about a
  default that moved and invalidated a comparison set quietly.
- **`first-concept` names the head**, so a fact about someone is filed under them.
- **Markers fall through**, which is the whole difference from a plain "first" rule —
  that one files `key(FACT, X)` under `FACT` and puts 50.0% of bindings on one owner.
- **COHERENCE**, which is the property decision 134's case actually rests on: every
  binding an entity heads shares one owner, so a node can hold an entity and everything
  said about it. This is the assertion that would fail if the rule were subtly wrong,
  and it is stated over both token orderings because ownership must stop depending on
  the order.
"""

from __future__ import annotations

import collections
import unittest

import numpy as np

from openplexus.keys import PairKeys

WIDTH = 8
VOCAB = 16
#: A task's structural markers. Small ids by convention, as in `tasks.clutrr`.
FACT, QUERY = 0, 1
MARKERS = frozenset({FACT, QUERY})
#: Entities 4-6, relations 10-11. Kept apart so an expectation naming one cannot
#: be read as naming the other.
ALICE, BOB, CAROL = 4, 5, 6
FATHER, SISTER = 10, 11


def source(route: str = "current") -> PairKeys:
    return PairKeys(seed=1, spread=0.3, width=WIDTH, start=VOCAB,
                    route=route, markers=MARKERS)


class TheDefaultHasNotMoved(unittest.TestCase):

    def test_current_names_the_current_token(self):
        tokens = np.array([FACT, ALICE, FATHER, BOB])
        for t in range(len(tokens)):
            self.assertEqual(source().concept(tokens, t), int(tokens[t]))

    def test_route_defaults_to_current_when_unspecified(self):
        plain = PairKeys(seed=1, spread=0.3, width=WIDTH, start=VOCAB)
        self.assertEqual(plain.route, "current")

    def test_the_key_vectors_are_untouched_by_the_route(self):
        """Routing says who OWNS a binding, never what its address is.

        If the route changed the vector, every stored number would move with it and
        the comparison between rules would not be a comparison.
        """
        tokens = np.array([FACT, ALICE, FATHER, BOB])
        for t in range(len(tokens)):
            np.testing.assert_allclose(
                source("current").key(tokens, t),
                source("first-concept").key(tokens, t))


class FirstConceptNamesTheHead(unittest.TestCase):

    def test_a_relation_never_owns_the_binding(self):
        """`key(ALICE, FATHER)` is filed under ALICE, not under FATHER.

        This is note 072's defect in one assertion: under `current` this position
        returns FATHER, and twenty relations cannot hold a network's facts.
        """
        tokens = np.array([FACT, ALICE, FATHER, BOB])
        self.assertEqual(source("first-concept").concept(tokens, 2), ALICE)
        self.assertEqual(source("current").concept(tokens, 2), FATHER)

    def test_a_marker_falls_through_to_the_current_token(self):
        """`key(FACT, ALICE)` stays on ALICE rather than piling onto FACT."""
        tokens = np.array([FACT, ALICE, FATHER, BOB])
        self.assertEqual(source("first-concept").concept(tokens, 1), ALICE)

    def test_start_is_a_marker_without_the_caller_saying_so(self):
        """At t=0 the previous token is `start`, which names no concept.

        Left to the caller to remember, this would file position 0 under a token
        that exists only to stand in for absence.
        """
        tokens = np.array([FACT, ALICE])
        self.assertEqual(source("first-concept").concept(tokens, 0), FACT)

    def test_an_unknown_route_raises_rather_than_falling_through(self):
        with self.assertRaises(ValueError):
            PairKeys(seed=1, spread=0.3, width=WIDTH, start=VOCAB,
                     route="frist-concept")


class OwnershipIsCoherentUnderBothOrderings(unittest.TestCase):
    """The property decision 134's case rests on, asserted rather than described.

    A node holding an entity and everything said about it can answer alone; a node
    holding a scattered share cannot, which is dimension splitting in another
    coordinate.
    """

    #: The two orderings `tasks.clutrr` builds, as one fact each.
    KINSHIP = np.array([FACT, ALICE, FATHER, BOB])
    CLOSURE = np.array([FACT, ALICE, BOB, FATHER])

    def owners_of_bindings_headed_by(self, tokens, entity, route):
        """Every binding whose key has `entity` as its first element."""
        keys = source(route)
        return {keys.concept(tokens, t) for t in range(1, len(tokens))
                if int(tokens[t - 1]) == entity}

    def test_first_concept_is_coherent_under_both_orderings(self):
        for name, tokens in (("kinship", self.KINSHIP),
                             ("closure", self.CLOSURE)):
            with self.subTest(layout=name):
                owners = self.owners_of_bindings_headed_by(
                    tokens, ALICE, "first-concept")
                self.assertEqual(owners, {ALICE},
                                 f"{name}: ALICE heads bindings owned by {owners}")

    def test_current_is_NOT_coherent_and_that_is_the_defect(self):
        """Stated as a test so the comparison cannot quietly disappear.

        Under `current`, ALICE heads two bindings and owns neither reliably — which
        is why note 073 measured coherence at 0.0% under both orderings.
        """
        owners = self.owners_of_bindings_headed_by(
            self.KINSHIP, ALICE, "current")
        self.assertNotEqual(owners, {ALICE})

    def test_a_marker_owns_only_bindings_a_marker_HEADS(self):
        """The honest form, and the first draft of this test asserted more.

        It asserted no marker owns anything, which fails at t=0: the previous token
        is `start`, so the key is `pair(start, FACT)` and there is nothing but FACT
        to file it under. That binding predicts which entity opens a fact and states
        nothing about anybody, so it is content-free rather than misplaced.

        **What matters is that no CONTENT binding lands on a marker**, which is what
        this asserts: a marker owns a binding only when a marker heads it.
        """
        for name, tokens in (("kinship", self.KINSHIP),
                             ("closure", self.CLOSURE)):
            with self.subTest(layout=name):
                keys = source("first-concept")
                for t in range(len(tokens)):
                    head = int(tokens[t - 1]) if t else VOCAB
                    if keys.concept(tokens, t) in MARKERS:
                        self.assertIn(head, MARKERS | {VOCAB})

    def test_a_relation_owns_only_the_content_free_boundary_binding(self):
        """`pair(r, o)` is owned by the relation, and it carries no fact.

        Four keys exist per `FACT s r o` block, not the two note 073 scored, and
        this is the one that ordering leaves on a relation. The binding written
        against it is the NEXT block's `FACT` marker — so it predicts a separator,
        not a relationship, and the traversal never reads it.

        Asserted rather than left implicit, because "0.0% relation-owned" is true
        of the content bindings and not of every key.
        """
        keys = source("first-concept")
        relation_owned = [t for t in range(len(self.KINSHIP))
                          if keys.concept(self.KINSHIP, t) in {FATHER, SISTER}]
        self.assertEqual(relation_owned, [3])
        # And position 3's key is headed by the relation, so the rule is behaving:
        # it files under the head, and here the head happens to be a relation.
        self.assertEqual(int(self.KINSHIP[2]), FATHER)

    def test_a_repeated_entity_concentrates_rather_than_scatters(self):
        """Two facts about one entity land on one owner, which is the point.

        A pair rule would scatter these, and note 073 measured that at 125 owners
        with coherence 0.0%.
        """
        tokens = np.array([FACT, ALICE, FATHER, BOB,
                           FACT, ALICE, SISTER, CAROL])
        counted = collections.Counter(
            source("first-concept").concept(tokens, t)
            for t in range(1, len(tokens))
            if int(tokens[t - 1]) == ALICE)
        self.assertEqual(set(counted), {ALICE})
        self.assertEqual(counted[ALICE], 2)


if __name__ == "__main__":
    unittest.main()
