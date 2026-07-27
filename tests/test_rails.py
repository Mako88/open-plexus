"""The repo-specific rails, and the ratchet that keeps them enforceable.

`tools/check_rails.py` encodes three conventions that already cost this project a
result. Generic lint would not have found any of them, and a rule that fails on
every legacy file gets suppressed — so the known violations are exempt and only
NEW ones fail.

These tests are about the ratchet rather than the rails: that a new violation is
reported, that an exempt one is not, and that an exemption which has outlived its
reason is an error. A baseline nobody prunes eventually exempts everything, which
would leave a check that passes while checking nothing.
"""

from __future__ import annotations

import unittest

from tools import check_rails

RAIL = "R2-sweep-has-predictions-and-cost"


class TheRatchetOnlyTightens(unittest.TestCase):

    def test_a_new_violation_is_reported(self):
        new, stale = check_rails.compare(
            {RAIL: ["experiments/sweeps/new.txt"]}, {RAIL: []})
        self.assertEqual(new[RAIL], ["experiments/sweeps/new.txt"])
        self.assertEqual(stale[RAIL], [])

    def test_an_exempt_violation_is_not_reported(self):
        new, stale = check_rails.compare(
            {RAIL: ["experiments/sweeps/old.txt"]},
            {RAIL: ["experiments/sweeps/old.txt"]})
        self.assertEqual(new[RAIL], [])
        self.assertEqual(stale[RAIL], [])

    def test_an_exemption_that_now_complies_is_an_error(self):
        """Not a warning. The exemption list is the thing that can rot, and a
        stale entry silently covers whatever is added to that path later."""
        new, stale = check_rails.compare(
            {RAIL: []}, {RAIL: ["experiments/sweeps/fixed.txt"]})
        self.assertEqual(new[RAIL], [])
        self.assertEqual(stale[RAIL], ["experiments/sweeps/fixed.txt"])

    def test_exempting_one_file_does_not_exempt_another(self):
        new, _ = check_rails.compare(
            {RAIL: ["a.txt", "b.txt"]}, {RAIL: ["a.txt"]})
        self.assertEqual(new[RAIL], ["b.txt"])

    def test_an_absent_rail_in_the_baseline_exempts_nothing(self):
        """A baseline written before a rail existed must not silently excuse
        every file the new rail flags."""
        new, _ = check_rails.compare({RAIL: ["a.txt"]}, {})
        self.assertEqual(new[RAIL], ["a.txt"])


class TheRailsHoldToday(unittest.TestCase):

    def test_every_summariser_uses_the_shared_refusals(self):
        """R1 is strict: it has no exemptions and is not allowed to gain any.

        Five hand-copies had already drifted and one had lost its floor check
        entirely. This is the check that stops a sixth.
        """
        self.assertEqual(check_rails.summarisers_missing_the_rail(), [],
                         "a summariser reports a recovery ratio without "
                         "importing tools.recovery")

    def test_the_baseline_is_in_sync_with_the_repository(self):
        """Fails locally rather than only in CI, and fails for one of two
        reasons: something new violates a rail, or an exemption has outlived
        its reason. `--write-baseline` is the deliberate fix for the second."""
        self.assertEqual(check_rails.main([]), 0)

    def test_the_baseline_does_not_excuse_r1(self):
        self.assertEqual(
            check_rails.load_baseline()["R1-summariser-imports-recovery"], [],
            "R1 has acquired an exemption; it is the one rail with no legacy "
            "violations, so an entry here means a new summariser skipped it")


if __name__ == "__main__":
    unittest.main()
