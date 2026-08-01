"""Counting role-marked relations, and what it does when the pair is new.

`Composition` exists to answer a composition nobody stated — its whole reason for
separating `left`, `right` and `target` into three surfaces is that a pair never
seen still has both halves counted. These tests fix what that means:

- a pair that WAS stated comes back, which is the floor
- a pair that was not is answered from its halves, and the case where it can be
  is built by hand so the mechanism is shown working before it is shown failing
- **`None` is a refusal and not a wrong answer**, because a claim about inference
  has to keep *said nothing* apart from *said the wrong thing*

The measured null lives in `experiments/clutrr_headroom.py`: on real CLUTRR, with
62 facts and 20 withheld, this recovers 0.00 of them under the demanding combiner
and 0.08-0.15 under the permissive ones, against a marginal of 0.05. That is a
result about the data volume as much as the mechanism — each relation-role has
about three observations — and the tests here are what says the mechanism itself
is connected.
"""

from __future__ import annotations

import unittest

from openplexus.composition import ROLES, Composition
from openplexus.grounding import STATISTICS

CONDITIONAL = STATISTICS["conditional"]


class ARoleIsPartOfTheSurfaceAndNotAConvention(unittest.TestCase):

    def test_the_same_relation_in_two_roles_is_two_surfaces(self):
        counts = Composition(5)
        self.assertNotEqual(counts.surface("left", 3), counts.surface("right", 3))
        self.assertNotEqual(counts.surface("left", 3), counts.surface("target", 3))

    def test_every_role_and_relation_gets_its_own_surface(self):
        counts = Composition(5)
        seen = {counts.surface(role, relation)
                for role in ROLES for relation in range(5)}
        self.assertEqual(len(seen), 15)

    def test_a_relation_outside_the_space_is_refused(self):
        # Folding it back in would make two relations one surface, and every
        # count on that surface would be a mixture of both.
        counts = Composition(5)
        with self.assertRaises(ValueError):
            counts.surface("left", 5)
        with self.assertRaises(ValueError):
            counts.surface("left", -1)

    def test_an_unknown_role_is_refused_rather_than_defaulted(self):
        with self.assertRaises(ValueError):
            Composition(5).surface("subject", 0)

    def test_a_composition_over_no_relations_is_refused(self):
        with self.assertRaises(ValueError):
            Composition(0)


class WhatWasStatedComesBack(unittest.TestCase):
    """The floor. A mechanism that cannot do this cannot do anything harder."""

    def test_a_stated_pair_answers_itself(self):
        counts = Composition(4)
        counts.observe(0, 1, 2)
        counts.observe(1, 0, 3)
        self.assertEqual(counts.answer(0, 1, CONDITIONAL), 2)
        self.assertEqual(counts.answer(1, 0, CONDITIONAL), 3)

    def test_the_order_of_a_pair_matters(self):
        # Otherwise `father . sister` and `sister . father` are one address and
        # the algebra has been made commutative, which it is not.
        counts = Composition(4)
        counts.observe(0, 1, 2)
        self.assertEqual(counts.answer(0, 1, CONDITIONAL), 2)
        self.assertIsNone(counts.answer(1, 0, CONDITIONAL))


class APairNeverStatedIsAnsweredFromItsHalves(unittest.TestCase):
    """The claim the module is for, on data built so it is answerable.

    The query is `0 . 1`, which is never stated. Answer 4 is attested WITH EACH
    HALF SEPARATELY, in two other pairs — `0 . 2 -> 4` gives the left half, and
    `3 . 1 -> 4` gives the right. Nothing else has both halves behind it.

    **Three pairs that merely behave alike is not enough**, and the first version
    of this test made that mistake: with `0 . 2 -> 4`, `0 . 3 -> 5`, `1 . 2 -> 4`,
    the query `1 . 3` has one half pointing at 4 and the other at 5, both at 1.0,
    and every combiner ties. It passed only because the tie broke toward the
    expected id.
    """

    def counts(self) -> Composition:
        counts = Composition(8)
        for _ in range(3):
            counts.observe(0, 2, 4)      # left 0 leads to 4
            counts.observe(0, 7, 5)      # and sometimes to 5
            counts.observe(3, 1, 4)      # right 1 is led to by 4
        return counts

    def test_the_unstated_pair_is_answered(self):
        self.assertEqual(self.counts().answer(0, 1, CONDITIONAL, "min"), 4)

    def test_and_it_is_the_halves_doing_it(self):
        """The connection test: change what a half attests, move the answer.

        Only `3 . 1` differs from the case above. Without this, the answer could
        be the marginal — 4 is the commonest target — and the test would pass on
        a mechanism that ignores its input entirely.
        """
        counts = Composition(8)
        for _ in range(3):
            counts.observe(0, 2, 4)
            counts.observe(0, 7, 5)
            counts.observe(3, 1, 5)      # right 1 is now led to by 5
        self.assertEqual(counts.answer(0, 1, CONDITIONAL, "min"), 5)

    def test_a_pair_whose_halves_agree_on_nothing_refuses(self):
        counts = Composition(6)
        counts.observe(0, 2, 4)
        counts.observe(1, 3, 5)
        # `min` demands both halves support the candidate. Nothing does.
        self.assertIsNone(counts.answer(0, 3, CONDITIONAL, "min"))

    def test_the_demanding_combiner_refuses_where_the_permissive_one_commits(self):
        counts = Composition(6)
        counts.observe(0, 2, 4)
        counts.observe(1, 3, 5)
        self.assertIsNone(counts.answer(0, 3, CONDITIONAL, "min"))
        self.assertIsNotNone(counts.answer(0, 3, CONDITIONAL, "max"))

    def test_an_unknown_combiner_is_refused(self):
        with self.assertRaises(ValueError):
            self.counts().ranked(0, 2, CONDITIONAL, "average")


class TheRankingIsTotalAndTheTableIsReadableByTheSearch(unittest.TestCase):

    def test_candidates_come_back_best_first(self):
        counts = Composition(5)
        for _ in range(9):
            counts.observe(0, 1, 2)
        counts.observe(0, 1, 3)
        scored = counts.ranked(0, 1, CONDITIONAL)
        self.assertEqual([relation for _, relation in scored][:2], [2, 3])
        self.assertGreater(scored[0][0], scored[1][0])

    def test_nothing_scored_is_an_empty_list_and_not_a_zero(self):
        self.assertEqual(Composition(4).ranked(0, 1, CONDITIONAL), [])
        self.assertIsNone(Composition(4).answer(0, 1, CONDITIONAL))

    def test_the_table_holds_one_commitment_per_pair(self):
        counts = Composition(4)
        counts.observe(0, 1, 2)
        counts.observe(1, 0, 3)
        table = counts.table(CONDITIONAL)
        self.assertEqual(table[(0, 1)], 2)
        self.assertEqual(table[(1, 0)], 3)

    def test_a_floor_drops_the_weak_commitments(self):
        # The pair leads to two different answers equally often, so its best
        # commitment scores 0.5 -- which is what a floor is for.
        counts = Composition(4)
        counts.observe(0, 1, 2)
        counts.observe(0, 1, 3)
        self.assertIn((0, 1), counts.table(CONDITIONAL, floor=0.0))
        self.assertNotIn((0, 1), counts.table(CONDITIONAL, floor=0.99))


if __name__ == "__main__":
    unittest.main()
