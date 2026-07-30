"""The eaten-word detector must fire on the messages it was built from.

`tools/check_commit_messages.py` is the structural answer to a rule that has four
calibration entries in `CLAUDE.md` and failed every time anyway. A detector that has
only ever been seen to PASS is worth nothing, so these feed it the actual damaged
lines out of this repository's history.

The examples are real. Three are quoted in `CLAUDE.md`'s calibrations; two were found
by the detector itself and were not previously recorded, which is the argument for it
existing.
"""

from __future__ import annotations

import unittest

from tools.check_commit_messages import ALLOWED, EATEN, EATEN_AT_START

#: Real mangled lines from this repository's commit history. A backticked word was
#: consumed by the shell and left its spaces behind.
DAMAGED = (
    "and both words are missing: \"reading of  at d'=1.01\"",
    "This gives  -- already in the model, already in BACKLOG as one of the",
    "rate can imitate. Plus  -- families that mean nothing -- because",
    "script's  was a fourth byte-identical copy of one in g10-01 and g4-04. It",
    "The step's own NAME rendered as \"window  on a  link, run \" in the job list.",
)

#: A first word eaten right after a sentence ends, which reads as a stutter rather
#: than as a gap and is the easiest kind to skim past.
DAMAGED_AT_START = (
    "PREDICTION 3 REFUTED BACKWARDS.  was predicted to rise, because backticks",
)

#: Legitimate prose and legitimate layout. Indented lines align tables and code with
#: runs of spaces on purpose, and every indented hit in the 400-commit calibration
#: run was a table.
CLEAN = (
    "The renderer, and the faithfulness bar written before anything can fail it",
    "A word vanishing leaves its spaces behind. That is the whole detector.",
    "    115  SATURATION IS CLOSED, and eliminated store capacity by name",
    "    arm                4,000    8,000   16,000   32,000",
    "  decay 1.0            inherited from the word-level work",
    "Sentence one ends here. Sentence two starts here.",
)


class ItFiresOnRealDamage(unittest.TestCase):

    def test_every_known_mangled_line_is_caught(self):
        for line in DAMAGED:
            with self.subTest(line=line[:40]):
                self.assertTrue(EATEN.search(line),
                                "a known-eaten word was not detected")

    def test_a_word_eaten_after_a_sentence_end_is_caught(self):
        for line in DAMAGED_AT_START:
            with self.subTest(line=line[:40]):
                self.assertTrue(EATEN_AT_START.search(line) or EATEN.search(line))


class ItStaysQuietOnLegitimateText(unittest.TestCase):
    """The companion. A detector that fires on everything is not a detector."""

    def test_clean_prose_and_indented_tables_are_not_flagged(self):
        for line in CLEAN:
            with self.subTest(line=line[:40]):
                self.assertIsNone(EATEN.search(line),
                                  "legitimate text was flagged as damaged")

    def test_indentation_is_what_exempts_a_table(self):
        # Stated explicitly because it is the whole false-positive story: the same
        # characters flagged in prose are fine in a table, and only the leading
        # whitespace distinguishes them.
        table = "    one  two  three"
        self.assertIsNone(EATEN.search(table))
        self.assertIsNotNone(EATEN.search(table.strip()))


class TheExemptionListStaysSmall(unittest.TestCase):

    def test_it_is_short_enough_to_read(self):
        # An exemption list that grows is a check being suppressed rather than a
        # check being calibrated -- the same reason `rails_baseline.json` can only
        # shrink without a visible diff.
        self.assertLess(len(ALLOWED), 12)


if __name__ == "__main__":
    unittest.main()
