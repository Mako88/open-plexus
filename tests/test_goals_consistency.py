"""GOALS.md must not quote a superseded measurement as current.

With a dozen sweeps and forty explainers, the largest document in this project
accumulates numbers faster than anyone re-reads them. An audit found it presenting
`T^0.67` as the answer for minimum machine width while quoting `T^0.82` for the
same quantity two paragraphs later, with the consequences — how much wider, how
many fewer machines — still computed from the older figure.

Nothing was wrong in either sweep. The document simply grew a second answer and
kept the first. These tests are cheap and catch that class of drift.
"""

from __future__ import annotations

import pathlib
import re
import unittest

GOALS = (pathlib.Path(__file__).resolve().parent.parent / "GOALS.md"
         ).read_text(encoding="utf-8")


class TheGatingClaimIsNotStillTheOldOne(unittest.TestCase):
    """GOALS said "Nothing tried can tell it" about selective storage.

    That was true when written and g9-02 made it false: a reward token in the
    stream recovers 0.23, and g9-06's tag recovers 0.16 flat across delay
    including at delay 20, where every window is negative. The sentence gated how
    everything below it was read, so it is quoted and marked rather than deleted
    — and these stop it drifting back to being a live claim.
    """

    def test_the_refuted_sentence_is_marked_as_corrected(self):
        """It may appear as a QUOTATION. It may not appear as an assertion."""
        if "Nothing tried can tell it" not in GOALS:
            self.skipTest("the sentence is gone entirely, which is also fine")
        self.assertIn(
            "CORRECTED", GOALS,
            "GOALS still says nothing can tell a device which inputs matter, "
            "without saying that g9-02 and g9-06 refuted it")

    def test_the_recovery_that_replaced_it_appears_with_its_delay(self):
        """0.16 on its own is a number; 0.16 at delay 20, where the window is
        negative, is the finding. A figure quoted without the condition that
        makes it interesting is how this document drifted the first time."""
        self.assertRegex(
            GOALS, r"\+0\.16.{0,200}delay 20",
            "the tag's recovery should appear alongside the delay that makes it "
            "a result rather than a number")

    def test_it_does_not_claim_the_gap_is_closed(self):
        """The ceiling is still a ceiling. 0.16 of the oracle's advantage is not
        the oracle's advantage, and the section's whole point is that the three
        findings under it are ceiling results."""
        self.assertIn("not all of it", GOALS,
                      "the corrected section should say plainly that the tag "
                      "recovers a fraction, or the reader will take the "
                      "correction as closing the gap")


class SupersededFiguresAreMarked(unittest.TestCase):

    def test_the_older_width_exponent_is_named_as_superseded(self):
        """0.67 may appear, but not as the live answer."""
        if "0.67" not in GOALS:
            self.skipTest("the older figure is no longer mentioned at all")
        self.assertIn("superseded", GOALS,
                      "GOALS still quotes the 0.67 width exponent without "
                      "saying 0.82 replaced it")

    def test_the_current_width_exponent_appears_with_its_interval(self):
        self.assertRegex(
            GOALS, r"0\.82.{0,40}\[0\.61, 1\.03\]",
            "the current minimum-width exponent should appear with the "
            "interval it was measured to")

    def test_the_derived_machine_count_matches_the_current_exponent(self):
        """0.37 − 0.82 = −0.45. A stale document said −0.30, from 0.67.

        The derived number is the one a reader acts on, and it is the one that
        silently goes stale when the measurement it came from is updated.
        """
        self.assertIn("T^-0.45", GOALS,
                      "the machine-count exponent should be 0.37 - 0.82 = -0.45")
        self.assertNotIn("`T^-0.30`", GOALS,
                         "GOALS still carries the machine-count exponent "
                         "derived from the superseded 0.67")


class EveryGateHasAVerdict(unittest.TestCase):
    """A gate table with a blank row is a gate nobody noticed was unanswered."""

    def test_no_gate_row_is_left_empty(self):
        rows = re.findall(r"^\| \*\*G\d.*$", GOALS, re.MULTILINE)
        self.assertGreaterEqual(len(rows), 5, "the gate table is missing rows")
        for row in rows:
            cells = [c.strip() for c in row.strip("|").split("|")]
            self.assertTrue(all(cells),
                            f"a gate row has an empty cell: {row}")


if __name__ == "__main__":
    unittest.main()
