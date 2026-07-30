"""The archived results log must not drift back into disagreeing with itself.

These guards used to run against `GOALS.md`, where the narrative lived until
2026-07-28. The narrative moved to `docs/archive/goals-results-log.md`; the
figures it holds did not change, and neither did the reason for checking them.

An archive is not exempt. It is cited from notes and decisions, several of its
sections quote a refuted sentence deliberately, and a reader who lands there
needs the same protection: `T^0.67` appearing as the live answer for minimum
machine width while `T^0.82` sits two paragraphs away, with the consequences —
how much wider, how many fewer machines — still computed from the older figure.

Nothing was wrong in either sweep. The document grew a second answer and kept the
first, and the derived number is the one a reader acts on.
"""

from __future__ import annotations

import pathlib
import unittest

LOG = (pathlib.Path(__file__).resolve().parent.parent
       / "docs" / "archive" / "goals-results-log.md").read_text(encoding="utf-8")


ARCHIVE = pathlib.Path(__file__).resolve().parent.parent / "docs" / "archive"

#: Any of these in the opening of an archived file says "do not read this as current".
HISTORY = ("ARCHIVED", "This is history", "superseded", "Replaced by")

#: And it has to say where to go instead. A file that says "this is old" and stops
#: leaves the reader with nothing, which is worse than the stale claim.
REPLACEMENT = ("DECISIONS.md", "docs/options", "CLAUDE.md", "GOALS.md")

#: How much of the file counts as its opening. Generous: the point is that a reader who
#: lands here sees it before the content, not that it is in the first sentence.
HEAD = 900


class EveryArchivedFileSaysItIsArchived(unittest.TestCase):
    """Written over the ENUMERATION rather than over one file, which is rule 12.

    The single-file version of this guard existed for `goals-results-log.md` and passed
    for weeks. Run over the whole directory on 2026-07-30 it found two failures
    immediately:

    - `backlog-2026-07-28.md` sent the reader to `STATE.md`, which had not existed since
      the option tree replaced it. **A dangling pointer at the top of an archive.**
    - `state-2026-07-29-before-pruning.md` had no header at all and opened with *"This is
      the only document in this project that is kept current"* — a stale claim wearing a
      current document's authority, in the archive of the very restructure that fixed
      that failure.

    Neither would have been found by reading, because nobody reads an archive header;
    they read past it. Both are fixed in place with the reason beside them rather than
    quietly, per rule 11's split-do-not-loosen.

    The 105 archived notes are covered by their directory's README rather than
    individually — a per-file header on each would be 105 copies of one sentence, which
    rule 9 refuses.
    """

    def files(self):
        return sorted(ARCHIVE.glob("*.md")) + sorted(ARCHIVE.glob("*/README.md"))

    def test_there_is_something_to_check(self):
        """A glob that matches nothing passes every assertion below vacuously."""
        self.assertGreater(len(self.files()), 5)

    def test_each_declares_itself_history(self):
        for path in self.files():
            with self.subTest(path=path.name):
                head = path.read_text(encoding="utf-8")[:HEAD]
                self.assertTrue(
                    any(mark.lower() in head.lower() for mark in HISTORY),
                    f"{path.name} does not say it is archived in its first {HEAD} "
                    f"characters, so a reader who lands there reads it as current. "
                    f"That is the failure the split exists to prevent.")

    def test_each_names_where_the_current_state_lives(self):
        for path in self.files():
            with self.subTest(path=path.name):
                head = path.read_text(encoding="utf-8")[:HEAD]
                self.assertTrue(
                    any(name in head for name in REPLACEMENT),
                    f"{path.name} says it is old and does not say what replaced it. "
                    f"A pointer that rots is why `backlog-2026-07-28.md` sent readers "
                    f"to STATE.md for a year after it stopped existing.")


class ItIsMarkedAsHistory(unittest.TestCase):
    """The original single-file guard, kept because it names the specific pointer that
    had to be updated when the three-document structure became one tree."""

    def test_the_header_says_not_to_read_it_for_current_state(self):
        self.assertIn("This is history", LOG)

    def test_it_names_where_the_current_state_lives(self):
        # DECISIONS.md, not STATE.md: the three-document structure was replaced by
        # a single option tree on 2026-07-29. This assertion is why the archive's
        # pointer got updated with it rather than quietly rotting.
        self.assertIn("DECISIONS.md", LOG)


class TheGatingClaimIsNotStillTheOldOne(unittest.TestCase):
    """It said "Nothing tried can tell it" about selective storage.

    That was true when written and g9-02 made it false: a reward token in the
    stream recovers 0.23, and g9-06's tag recovers 0.16 flat across delay
    including at delay 20, where every window is negative. The sentence gated how
    everything below it was read, so it is quoted and marked rather than deleted
    — and these stop it drifting back to being a live claim.
    """

    def test_the_refuted_sentence_is_marked_as_corrected(self):
        """It may appear as a QUOTATION. It may not appear as an assertion."""
        if "Nothing tried can tell it" not in LOG:
            self.skipTest("the sentence is gone entirely, which is also fine")
        self.assertIn(
            "CORRECTED", LOG,
            "the log still says nothing can tell a device which inputs matter, "
            "without saying that g9-02 and g9-06 refuted it")

    def test_the_recovery_that_replaced_it_appears_with_its_delay(self):
        """0.16 on its own is a number; 0.16 at delay 20, where the window is
        negative, is the finding. A figure quoted without the condition that
        makes it interesting is how this document drifted the first time."""
        self.assertRegex(
            LOG, r"\+0\.16.{0,200}delay 20",
            "the tag's recovery should appear alongside the delay that makes it "
            "a result rather than a number")

    def test_it_does_not_claim_the_gap_is_closed(self):
        """The ceiling is still a ceiling. 0.16 of the oracle's advantage is not
        the oracle's advantage, and the section's whole point is that the three
        findings under it are ceiling results."""
        self.assertIn("not all of it", LOG,
                      "the corrected section should say plainly that the tag "
                      "recovers a fraction, or the reader will take the "
                      "correction as closing the gap")


class SupersededFiguresAreMarked(unittest.TestCase):

    def test_the_older_width_exponent_is_named_as_superseded(self):
        """0.67 may appear, but not as the live answer."""
        if "0.67" not in LOG:
            self.skipTest("the older figure is no longer mentioned at all")
        self.assertIn("superseded", LOG,
                      "the log still quotes the 0.67 width exponent without "
                      "saying 0.82 replaced it")

    def test_the_current_width_exponent_appears_with_its_interval(self):
        self.assertRegex(
            LOG, r"0\.82.{0,40}\[0\.61, 1\.03\]",
            "the current minimum-width exponent should appear with the "
            "interval it was measured to")

    def test_the_derived_machine_count_matches_the_current_exponent(self):
        """0.37 − 0.82 = −0.45. A stale document said −0.30, from 0.67.

        The derived number is the one a reader acts on, and it is the one that
        silently goes stale when the measurement it came from is updated.
        """
        self.assertIn("T^-0.45", LOG,
                      "the machine-count exponent should be 0.37 - 0.82 = -0.45")
        self.assertNotIn("`T^-0.30`", LOG,
                         "the log still carries the machine-count exponent "
                         "derived from the superseded 0.67")


if __name__ == "__main__":
    unittest.main()
