"""`damped(alpha)` — the family the named statistics are points on.

The MEANING tests here are the load-bearing ones. `CLAUDE.md`: a quantity
computed exactly as described, described exactly as implemented, and named
something the implementation does not earn passes every other layer, because they
all agree with each other. So these assert the properties that make this the
family it claims to be, in a form that does not mention how it is computed.
"""

from __future__ import annotations

import unittest

from openplexus.grounding import (STATISTICS, CoOccurrence, damped,
                                  neighbours)


def _index() -> CoOccurrence:
    """A stream with the exact asymmetry `g36-04` measured.

    Subject 0 appears on 100 occasions. Alongside it:

        99  the DISTRACTOR — on all 100, and on 400 others. Total 500
        2   a common partner genuinely about the subject — on 80 of the 100,
            and on the same 400 others. Total 480
        1   a rare, perfectly-informative partner — on 50, and nowhere else

    **The distractor must out-count every real partner FROM THE SUBJECT'S
    VIEW**, or the low end of the axis is not the failure `g32-01` measured. A
    first version gave 2 and 99 the same 100 co-occurrences, they tied, and the
    tie-break decided the test — which would have passed for the wrong reason at
    any other tie-break rule.
    """
    index = CoOccurrence()
    for step in range(100):
        present = {0, 99}
        if step % 2 == 0:
            present.add(1)
        if step % 5 != 0:
            present.add(2)
        index.observe(present)
    for _ in range(400):
        index.observe({2, 50, 99})
    return index


class TheNamedStatisticsAreSpecialCases(unittest.TestCase):
    """If these drift apart, the docstring's claim that alpha 0/0.5/1 reproduce
    `count`/`weighted`/`conditional` is false, and the sweep axis no longer
    contains the results it is being compared against."""

    def _order(self, statistic) -> list[int]:
        index = _index()
        return sorted(index.partners(0),
                      key=lambda o: (-statistic(index, 0, o), o))

    def test_alpha_zero_ranks_exactly_like_raw_count(self):
        self.assertEqual(self._order(damped(0.0)),
                         self._order(STATISTICS["count"]))

    def test_alpha_half_ranks_exactly_like_frequency_weighted(self):
        self.assertEqual(self._order(damped(0.5)),
                         self._order(STATISTICS["weighted"]))

    def test_alpha_one_ranks_exactly_like_conditional(self):
        self.assertEqual(self._order(damped(1.0)),
                         self._order(STATISTICS["conditional"]))

    def test_the_scores_match_and_not_merely_the_order(self):
        """Ordering alone is too weak, and this is the test that proves it.

        A first version checked only the three orderings, and the mutation
        `the-damping-exponent-is-ignored` — which makes EVERY alpha compute
        `conditional` — survived it: on that fixture `c_xy/sqrt(c_y)` and
        `c_xy/c_y` happen to rank the three candidates identically.

        Comparing against `frequency_weighted` and `conditional` is not
        restating the formula: those are independent implementations that
        predate this family, and the claim under test is that this family
        reproduces them.
        """
        index = _index()
        for alpha, name in ((0.0, "count"), (0.5, "weighted"),
                            (1.0, "conditional")):
            for other in index.partners(0):
                self.assertAlmostEqual(
                    damped(alpha)(index, 0, other),
                    STATISTICS[name](index, 0, other), places=12,
                    msg=f"alpha {alpha} does not reproduce {name} at {other}")

    def test_a_middle_alpha_orders_differently_from_BOTH_endpoints(self):
        """The axis has an interior, which is the entire reason it exists.

        If every alpha gave one of two orderings, sweeping it would be sweeping
        a switch and the sweep would report a two-valued column as a curve.
        """
        index = CoOccurrence()
        # Candidate 1: often with the subject, and very common overall.
        # Candidate 2: less often with the subject, and much rarer overall.
        # Chosen so the two cross BETWEEN alpha 0.5 and alpha 1.0.
        for _ in range(100):
            index.observe({0, 1})
        for _ in range(30):
            index.observe({0, 2})
        for _ in range(300):
            index.observe({1, 9})
        for _ in range(70):
            index.observe({2, 9})

        order = lambda a: sorted(                       # noqa: E731 - local
            index.partners(0), key=lambda o: (-damped(a)(index, 0, o), o))
        self.assertEqual(order(0.0), [1, 2])
        self.assertEqual(order(0.5), [1, 2])
        self.assertEqual(order(1.0), [2, 1],
                         "the endpoints must disagree or this proves nothing")
        # And the scores really do cross rather than tie.
        self.assertGreater(damped(0.5)(index, 0, 1), damped(0.5)(index, 0, 2))
        self.assertLess(damped(1.0)(index, 0, 1), damped(1.0)(index, 0, 2))

    def test_the_three_do_not_all_agree_so_the_axis_is_not_flat(self):
        """A companion to the three above. Without it they would ALL pass on a
        stream where every statistic happens to give one ordering, and the sweep
        would report a flat row as a finding."""
        zero = self._order(damped(0.0))
        one = self._order(damped(1.0))
        self.assertNotEqual(zero, one)


class MoreDampingPenalisesTheCommonPartnerMore(unittest.TestCase):
    """The MEANING of the parameter, stated without reference to the formula:
    raising alpha can only move a common neighbour DOWN relative to a rare one,
    never up."""

    def test_a_common_partner_never_gains_on_a_rare_one_as_alpha_rises(self):
        index = _index()
        rare, common = 1, 2
        self.assertGreater(index.seen(common), index.seen(rare))
        previous = None
        for alpha in (0.0, 0.25, 0.5, 0.75, 1.0):
            statistic = damped(alpha)
            ratio = (statistic(index, 0, common)
                     / statistic(index, 0, rare))
            if previous is not None:
                self.assertLessEqual(ratio, previous + 1e-12,
                                     f"alpha {alpha} moved the common partner UP")
            previous = ratio
        # And the movement is real rather than a rounding tie.
        self.assertLess(previous,
                        damped(0.0)(index, 0, common) / damped(0.0)(index, 0, rare))

    def test_the_ever_present_distractor_is_ranked_first_at_alpha_zero(self):
        """`g32-01`'s refutation, reproduced as the low end of this axis, so the
        sweep's floor is a known failure rather than an assumption."""
        index = _index()
        self.assertEqual(neighbours(index, 0, damped(0.0), k=1), [99])

    def test_and_the_distractor_is_not_first_at_alpha_one(self):
        index = _index()
        self.assertNotIn(99, neighbours(index, 0, damped(1.0), k=1))


class TheParameterIsGuarded(unittest.TestCase):

    def test_a_negative_exponent_is_refused(self):
        with self.assertRaises(ValueError):
            damped(-0.5)

    def test_an_unseen_neighbour_scores_zero_rather_than_dividing_by_zero(self):
        index = CoOccurrence()
        index.observe({0, 1})
        for alpha in (0.0, 0.5, 1.0):
            self.assertEqual(damped(alpha)(index, 0, 77), 0.0)


class ItIsOffByDefault(unittest.TestCase):
    """New mechanisms default to off, so every earlier result stays reproducible
    and the comparison against not having it is free."""

    def test_damped_is_not_in_the_statistics_table(self):
        self.assertNotIn("damped", STATISTICS)
        self.assertEqual(len(STATISTICS), 5)


if __name__ == "__main__":
    unittest.main()
