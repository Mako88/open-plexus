"""The pinning check, which exists because printing a warning was not enough."""

from __future__ import annotations

import unittest

from tools.grid import pinned

GRID = (0.01, 0.02, 0.05, 0.1, 0.2)


class EdgesAreReported(unittest.TestCase):

    def test_every_arm_at_the_top_is_pinned(self):
        message = pinned([0.2, 0.2, 0.2, 0.2], GRID)
        self.assertIsNotNone(message)
        self.assertIn("top", message)

    def test_every_arm_at_the_bottom_is_pinned(self):
        message = pinned([0.01, 0.01, 0.01], GRID)
        self.assertIsNotNone(message)
        self.assertIn("bottom", message)

    def test_arms_split_between_both_edges_is_still_pinned(self):
        """No arm chose an interior value, so the grid still failed.

        This is the case a naive "did they all pick the same value" check would
        miss, and it is not hypothetical -- g4-01 had rows pinned at the top and
        other rows pinned at the bottom, which is what a grid that is too narrow
        in both directions looks like.
        """
        self.assertIsNotNone(pinned([0.01, 0.2, 0.2, 0.01], GRID))


class InteriorChoicesPass(unittest.TestCase):

    def test_one_interior_choice_is_enough(self):
        """One arm finding an interior optimum shows the grid reaches it.

        Deliberately lenient. The claim being made is about the GRID, not about
        each arm -- an arm at an edge alongside an arm in the middle is a real
        difference between the arms rather than evidence the range is wrong.
        """
        self.assertIsNone(pinned([0.2, 0.05, 0.2], GRID))

    def test_all_interior_passes(self):
        self.assertIsNone(pinned([0.02, 0.05, 0.1], GRID))


class DegenerateGrids(unittest.TestCase):

    def test_a_single_value_is_not_a_sweep(self):
        message = pinned([0.05, 0.05], (0.05,))
        self.assertIsNotNone(message)
        self.assertIn("fixed rather than swept", message)

    def test_nothing_chosen_is_not_an_error(self):
        self.assertIsNone(pinned([], GRID))


class TheHistoricalCase(unittest.TestCase):
    """g4-01's actual rows, so the check is seen to fire on the thing it is for."""

    def test_g4_01_rows_that_pinned_are_caught(self):
        three = (0.02, 0.05, 0.1)
        self.assertIsNotNone(pinned([0.1, 0.1, 0.1, 0.1], three))
        self.assertIsNotNone(pinned([0.02, 0.02, 0.02, 0.02], three))

    def test_the_rows_i_read_as_fine_were_also_pinned(self):
        """The two rows g4-01 reported as healthy. They were not.

        Writing up g4-01 I called these rows "varies" and treated their numbers
        as trustworthy, because the arms disagreed with each other. But they
        disagree by sitting at OPPOSITE EDGES, and no arm chose the interior
        value -- which is the case the test above already says is pinned. I
        asserted both things in the same file and only noticed because they
        contradicted each other.

        Across all six rows, the interior value 0.05 was chosen **zero times out
        of twenty-four**. The grid was pinned six rows for six, not four.
        """
        three = (0.02, 0.05, 0.1)
        self.assertIsNotNone(pinned([0.1, 0.02, 0.02, 0.02], three))
        self.assertIsNotNone(pinned([0.1, 0.1, 0.1, 0.02], three))


if __name__ == "__main__":
    unittest.main()
