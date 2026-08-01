"""A boundary learned from observed refusal rates, and not a constant.

`learned_threshold` is the mechanism g44-01 turns on. Demoting only the group
below its cut reaches +0.2256 where an oracle calling `is_shadow` reaches
+0.2256 and watching reaches -0.2967, so the arm needs no privileged knowledge --
which makes it worth fixing that the cut is genuinely derived from the data:

- **it FOLLOWS the groups**, because a constant sitting in a convenient place
  would pass any single-arrangement test;
- **it reads RATES and not counts**, or the budget silently moves the boundary;
- **it refuses to invent a boundary** in rates that describe none;
- **an unasked pair cannot vote**, which is precisely how the arm's own
  threshold fails -- it learns from background pairs that detach for free, and
  that is why P10 came back at -0.1889 while the same rule reaches the oracle.

The swept numbers live with the experiment. These fix the behaviour they rest on.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments.g44_01_asking import learned_threshold  # noqa: E402


def rates(*groups) -> dict:
    """Refusals shaped as the experiment stores them: (asked, refused)."""
    out = {}
    for index, rate in enumerate(r for group in groups for r in group):
        out[(index, 0)] = (100, round(rate * 100))
    return out


class TheCutIsDerivedFromTheData(unittest.TestCase):

    def test_it_lands_between_two_groups(self):
        low = [0.13, 0.20, 0.21, 0.24, 0.29]
        high = [0.30, 0.36, 0.38, 0.40, 0.47]
        cut = learned_threshold(rates(low, high))
        self.assertLess(max(low), cut)
        self.assertLess(cut, min(high))

    def test_it_MOVES_when_the_groups_move(self):
        """The connection test. A boundary that does not follow its data is a
        constant, and a constant would have passed the test above."""
        near = learned_threshold(rates([0.13, 0.20], [0.30, 0.36]))
        far = learned_threshold(rates([0.13, 0.20], [0.80, 0.86]))
        self.assertGreater(far, near + 0.15)

    def test_rates_and_not_counts_decide_it(self):
        """Doubling every ask changes the counts and leaves the rates alone,
        so the same boundary must come back."""
        once = learned_threshold({(i, 0): (100, r)
                                  for i, r in enumerate([13, 20, 36, 40])})
        twice = learned_threshold({(i, 0): (200, 2 * r)
                                   for i, r in enumerate([13, 20, 36, 40])})
        self.assertAlmostEqual(once, twice, places=9)


class ItRefusesToInventABoundary(unittest.TestCase):

    def test_one_group_gives_no_cut(self):
        """Rates that are all the same describe no boundary. Returning one
        would demote half of them at random."""
        self.assertEqual(learned_threshold(rates([0.3, 0.3, 0.3, 0.3])), 0.0)

    def test_a_single_rate_gives_no_cut(self):
        self.assertEqual(learned_threshold(rates([0.42])), 0.0)

    def test_no_asks_at_all_gives_no_cut(self):
        self.assertEqual(learned_threshold({}), 0.0)

    def test_an_unasked_pair_cannot_vote(self):
        """A pair with no asks has no rate, and counting it as 0.0 would drag
        the cut down. The companion to the tests above: those assert a cut is
        NOT invented, and this one asserts a real cut is unchanged."""
        asked = rates([0.13, 0.20], [0.36, 0.40])
        with_empties = {**asked, (90, 0): (0, 0), (91, 0): (0, 0)}
        self.assertAlmostEqual(learned_threshold(asked),
                               learned_threshold(with_empties), places=9)
        self.assertGreater(learned_threshold(asked), 0.0)


if __name__ == "__main__":
    unittest.main()
