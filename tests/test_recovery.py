"""The refusals have to refuse, including when refusing is inconvenient.

Five summarisers carried hand-copied versions of these two rules and one had
already lost the floor check entirely — `tools/summarise_g8_02.py`, which printed
the header *"RECOVERY, and the floor it is measured against"* while never
measuring against it, and which selected cells by maximising `oracle - none`,
the one rule guaranteed to prefer cells whose floor arm had collapsed.

So these tests are not about arithmetic. They are about the two situations where
the arithmetic is valid and meaningless, and about the selection rule that seeks
those situations out.
"""

from __future__ import annotations

import unittest

from tools.recovery import (MQAR_FLOOR, REWARD_RECALL_FLOOR, Cell, assess,
                            best_by, by_cell, margin, winner)

ARMS = ("none", "oracle", "on-use")


def cell(none: float, oracle: float, arm: float, spread: float = 0.0) -> dict:
    """One cell, as three arms x two seeds, with a controlled seed spread."""
    built = {}
    for name, mean in (("none", none), ("oracle", oracle), ("on-use", arm)):
        built[("x", name)] = {1: mean - spread / 2, 2: mean + spread / 2}
    return built


class TheFloorArmHasToWork(unittest.TestCase):
    """Two failures are not a difficulty, however far apart they are."""

    def test_a_collapsed_floor_is_refused(self):
        verdict = assess(cell(0.10, 0.90, 0.50), ("x",), ARMS,
                         REWARD_RECALL_FLOOR)
        self.assertIsNotNone(verdict.refused)
        self.assertEqual(verdict.ratios, {})

    def test_it_is_refused_PRECISELY_BECAUSE_the_gap_is_enormous(self):
        """The dangerous case, and the one that got reported before.

        A floor arm at 0.10 against an oracle at 0.90 has the largest gap
        anything in a sweep will produce, so it looks like the strongest cell on
        the grid. It is the weakest: `none` is below chance, so the arm being
        measured is being compared against something that also failed."""
        collapsed = assess(cell(0.10, 0.90, 0.50), ("x",), ARMS,
                           REWARD_RECALL_FLOOR)
        working = assess(cell(0.30, 0.50, 0.40), ("x",), ARMS,
                         REWARD_RECALL_FLOOR)
        self.assertGreater(collapsed.gap, working.gap)
        self.assertIsNotNone(collapsed.refused)
        self.assertIsNone(working.refused)

    def test_exactly_at_the_floor_is_refused(self):
        """At the floor the arm has learned nothing; `<=` not `<`."""
        self.assertIsNotNone(
            assess(cell(REWARD_RECALL_FLOOR, 0.90, 0.50), ("x",), ARMS,
                   REWARD_RECALL_FLOOR).refused)

    def test_the_refusal_says_which_floor_it_used(self):
        """A summariser prints this; a refusal that does not name its threshold
        cannot be argued with."""
        self.assertIn(f"{REWARD_RECALL_FLOOR:.3f}",
                      assess(cell(0.10, 0.90, 0.50), ("x",), ARMS,
                             REWARD_RECALL_FLOOR).refused)


class TheFloorIsAParameter(unittest.TestCase):
    """0.34375 for MQAR and 0.125 for reward_recall, and the difference decides
    real cells. Freezing either into the module is the drift it replaces."""

    def test_the_same_numbers_are_refused_by_one_floor_and_not_the_other(self):
        data = cell(0.30, 0.60, 0.45)
        self.assertIsNotNone(assess(data, ("x",), ARMS, MQAR_FLOOR).refused)
        self.assertIsNone(assess(data, ("x",), ARMS, REWARD_RECALL_FLOOR).refused)

    def test_the_two_floors_are_not_equal(self):
        self.assertNotAlmostEqual(MQAR_FLOOR, REWARD_RECALL_FLOOR)


class TheGapHasToBeatTheNoise(unittest.TestCase):

    def test_a_gap_inside_the_seed_spread_is_refused(self):
        self.assertIsNotNone(
            assess(cell(0.30, 0.35, 0.32, spread=0.20), ("x",), ARMS,
                   REWARD_RECALL_FLOOR).refused)

    def test_the_same_gap_with_tight_seeds_is_reported(self):
        """Pins that it is the COMPARISON that refuses, not the small gap. A
        rule that refused small gaps outright would throw away real results from
        well-behaved sweeps."""
        self.assertIsNone(
            assess(cell(0.30, 0.35, 0.32, spread=0.01), ("x",), ARMS,
                   REWARD_RECALL_FLOOR).refused)

    def test_a_gap_equal_to_the_spread_is_refused(self):
        """The boundary is `<=`. Every number here is a binary fraction so that
        the two quantities are exactly equal rather than nearly — an earlier
        version used 0.30/0.40/0.10 and passed the boundary by one float ulp,
        which tests the rounding rather than the rule."""
        verdict = assess(cell(0.25, 0.50, 0.375, spread=0.25), ("x",), ARMS,
                         REWARD_RECALL_FLOOR)
        self.assertEqual(verdict.gap, verdict.spread)
        self.assertIsNotNone(verdict.refused)


class MissingIsNotRefused(unittest.TestCase):
    """A job that never returned and a job that cannot be interpreted are
    different failures, and printing them alike hides dispatch problems."""

    def test_a_missing_arm_gives_none_rather_than_a_refusal(self):
        data = cell(0.30, 0.50, 0.40)
        del data[("x", "on-use")]
        self.assertIsNone(assess(data, ("x",), ARMS, REWARD_RECALL_FLOOR))

    def test_a_present_but_uninterpretable_cell_gives_a_Cell(self):
        self.assertIsInstance(
            assess(cell(0.10, 0.90, 0.50), ("x",), ARMS, REWARD_RECALL_FLOOR),
            Cell)


class TheRatioItself(unittest.TestCase):

    def test_the_arithmetic(self):
        verdict = assess(cell(0.20, 0.60, 0.40), ("x",), ARMS,
                         REWARD_RECALL_FLOOR)
        self.assertAlmostEqual(verdict.ratio("on-use"), 0.5)

    def test_an_arm_matching_the_oracle_recovers_all_of_it(self):
        verdict = assess(cell(0.20, 0.60, 0.60), ("x",), ARMS,
                         REWARD_RECALL_FLOOR)
        self.assertAlmostEqual(verdict.ratio("on-use"), 1.0)

    def test_an_arm_below_the_floor_recovers_a_negative_share(self):
        """g9-03's whole left-hand triangle is negative, and a summariser that
        clamped at zero would have hidden the cliff that decided the project."""
        self.assertLess(
            assess(cell(0.20, 0.60, 0.10), ("x",), ARMS,
                   REWARD_RECALL_FLOOR).ratio("on-use"), 0.0)


class SelectionHappensAfterTheRefusals(unittest.TestCase):
    """The rule that bit hardest. A cell whose floor arm collapsed has the
    largest `oracle - none` on the grid, so selecting on the gap prefers exactly
    the cells that must not be reported."""

    def setUp(self):
        self.collapsed = assess(cell(0.10, 0.90, 0.50), ("x",), ARMS,
                                REWARD_RECALL_FLOOR)
        self.working = assess(cell(0.30, 0.50, 0.45), ("x",), ARMS,
                              REWARD_RECALL_FLOOR)

    def test_the_broken_cell_would_win_on_gap(self):
        """Pins the trap, so the next person does not reintroduce it."""
        self.assertGreater(self.collapsed.gap, self.working.gap)

    def test_best_by_picks_the_working_cell_anyway(self):
        label, _ = best_by([("broken", self.collapsed),
                            ("good", self.working)], "on-use")
        self.assertEqual(label, "good")

    def test_gap_and_ratio_disagree_among_cells_that_both_PASS(self):
        """The subtler half, and the one a first version of these tests missed.

        Excluding refused cells is not enough. Among cells that both pass, a
        wide gap with a small share and a narrow gap with a large share rank
        opposite ways, and the share is the quantity the sweep is about — an
        arm recovering four fifths of a small advantage has done better than one
        recovering a sixth of a large one."""
        wide = assess(cell(0.30, 0.90, 0.40), ("x",), ARMS, REWARD_RECALL_FLOOR)
        narrow = assess(cell(0.30, 0.40, 0.38), ("x",), ARMS,
                        REWARD_RECALL_FLOOR)
        self.assertIsNone(wide.refused)
        self.assertIsNone(narrow.refused)
        self.assertGreater(wide.gap, narrow.gap)
        self.assertGreater(narrow.ratio("on-use"), wide.ratio("on-use"))

        label, _ = best_by([("wide", wide), ("narrow", narrow)], "on-use")
        self.assertEqual(label, "narrow")

    def test_best_by_returns_none_when_everything_was_refused(self):
        self.assertIsNone(best_by([("broken", self.collapsed)], "on-use"))

    def test_best_by_skips_missing_candidates(self):
        label, _ = best_by([("gone", None), ("good", self.working)], "on-use")
        self.assertEqual(label, "good")


class ALeadInsideTheNoiseIsNotALead(unittest.TestCase):
    """`max` over a swept axis will always name a winner. Usually it should not.

    This is `assess`'s second refusal one step further on. `assess` refuses when
    the DENOMINATOR is inside the seed spread; these refuse when a DIFFERENCE
    BETWEEN TWO RATIOS is. Both are the same mistake — reading a number smaller
    than the measurement error — and the second one had already happened: the
    first smoke run of g9-12's summariser announced *"the best rate MOVES with
    node width"* from three rates whose ratios were identical by construction,
    because `max` broke the exact tie arbitrarily.
    """

    def setUp(self):
        # Three swept values whose arm sits at the same place, measured with a
        # seed spread of 0.08. Any apparent winner here is the tie being broken.
        self.tied = {v: assess(cell(0.40, 0.90, 0.65, spread=0.08), ("x",),
                               ARMS, REWARD_RECALL_FLOOR)
                     for v in (0.02, 0.05, 0.1)}

    def test_an_exact_tie_reports_a_lead_of_zero(self):
        _, lead, noise = winner(self.tied, "on-use", 0.05)
        self.assertEqual(lead, 0.0)
        self.assertGreater(noise, lead)

    def test_the_noise_floor_is_the_spread_in_RATIO_units(self):
        """0.08 of accuracy across a gap of 0.50 is 0.16 of recovery.

        The spread is measured in accuracy and the lead in recovery, so
        comparing them directly would compare two different quantities. That
        division is the whole content of `margin`.
        """
        self.assertAlmostEqual(margin(self.tied[0.05]), 0.08 / 0.50)

    def test_a_lead_larger_than_the_noise_survives(self):
        moved = dict(self.tied)
        moved[0.1] = assess(cell(0.40, 0.90, 0.79, spread=0.08), ("x",), ARMS,
                            REWARD_RECALL_FLOOR)
        key, lead, noise = winner(moved, "on-use", 0.05)
        self.assertEqual(key, 0.1)
        self.assertGreater(lead, noise)

    def test_a_lead_smaller_than_the_noise_does_not(self):
        """The case that matters: a real difference, too small to mean anything.

        0.02 of accuracy IS a difference. Against a seed spread of 0.08 it is
        not evidence of one, and a summariser that names 0.1 the winner here is
        publishing noise with a decimal point on it.
        """
        moved = dict(self.tied)
        moved[0.1] = assess(cell(0.40, 0.90, 0.67, spread=0.08), ("x",), ARMS,
                            REWARD_RECALL_FLOOR)
        key, lead, noise = winner(moved, "on-use", 0.05)
        self.assertEqual(key, 0.1)
        self.assertGreater(lead, 0.0)
        self.assertLessEqual(lead, noise)

    def test_there_is_no_lead_without_an_incumbent_to_lead(self):
        self.assertIsNone(winner(self.tied, "on-use", 0.5))

    def test_a_negative_lead_means_the_incumbent_won(self):
        """`winner` names the best value, which can BE the incumbent."""
        worse = dict(self.tied)
        worse[0.02] = assess(cell(0.40, 0.90, 0.50, spread=0.08), ("x",), ARMS,
                             REWARD_RECALL_FLOOR)
        key, lead, _ = winner(worse, "on-use", 0.05)
        self.assertIn(key, (0.05, 0.1))
        self.assertGreaterEqual(lead, 0.0)


class GroupingRecords(unittest.TestCase):

    def test_the_arm_is_the_last_key(self):
        rows = [{"zipf_s": 0.5, "arm": "none", "seed": 1, "accuracy": 0.4}]
        self.assertEqual(list(by_cell(rows, "zipf_s")), [(0.5, "none")])

    def test_seeds_land_in_the_same_cell(self):
        rows = [{"d": 1, "arm": "none", "seed": s, "accuracy": 0.1 * s}
                for s in (1, 2, 3)]
        self.assertEqual(len(by_cell(rows, "d")[(1, "none")]), 3)


if __name__ == "__main__":
    unittest.main()
