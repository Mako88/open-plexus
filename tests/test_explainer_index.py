"""The explainer index check, watched going red for each reason it exists.

`tools/check_explainers.py` was written on 2026-07-30, after adding one explainer to
`docs/explainers/README.md` revealed that **sixteen were not in it** -- the whole
distribution line and the two most recent findings, unlisted for weeks.

Nobody noticed because the symptom of an unlisted document is that nobody reads it, and
that is indistinguishable from a document nobody needed. So the check has to fire on the
absence rather than on anything a reader would see, and rule 10 says it does not count
until it has been watched failing for the right reason.

Three failures, and the first is the one that actually happened.
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from tools.check_explainers import disagreements, main  # noqa: E402

INDEX = """# The explainers

| [01](01-the-big-idea.md) | The big idea |
| [044](044-the-number-was-right-and-the-pointer-was-wrong.md) | Pointers |
"""

FILES = {"01-the-big-idea.md", "044-the-number-was-right-and-the-pointer-was-wrong.md"}


class TheIndexCheckBites(unittest.TestCase):

    def test_an_agreeing_pair_passes(self):
        """Without this the failures below prove nothing."""
        self.assertEqual(disagreements(INDEX, FILES), [])

    def test_an_unlisted_explainer_is_caught(self):
        """The failure that happened, sixteen times over."""
        found = disagreements(INDEX, FILES | {"045-something-new.md"})
        self.assertTrue(
            any("045-something-new.md" in p and "not in README" in p for p in found),
            f"an explainer missing from the index passed: {found}")

    def test_a_dangling_row_is_caught(self):
        """The failure that happens next, when something is renamed."""
        found = disagreements(INDEX, {"01-the-big-idea.md"})
        self.assertTrue(
            any("does not exist" in p for p in found),
            f"an index row pointing at nothing passed: {found}")

    def test_two_files_at_one_number_are_caught(self):
        """Two SERIES are legal here and recorded in the index -- `31` beside `031`.
        Two FILES at one number are not, because a citation then names both."""
        found = disagreements(INDEX, FILES | {"044-a-different-one.md"})
        self.assertTrue(
            any("share the leading number" in p for p in found),
            f"two explainers at number 044 passed: {found}")

    def test_the_two_real_series_are_not_flagged(self):
        """`31` and `031` are different numbers as strings, which is what makes the
        existing pair legal without an exemption list."""
        index = ("| [31](31-what-the-filter-turned-out-to-be.md) | a |\n"
                 "| [031](031-losing-machines-tidily.md) | b |\n")
        self.assertEqual(
            disagreements(index, {"31-what-the-filter-turned-out-to-be.md",
                                  "031-losing-machines-tidily.md"}),
            [], "the two real numbering series were flagged, which would force a rename "
                "that breaks every link already pointing at one")


class TheRealIndexAgrees(unittest.TestCase):

    def test_the_checker_passes_over_the_repository(self):
        self.assertEqual(main(), 0, "tools/check_explainers.py is failing; run it for why")


if __name__ == "__main__":
    unittest.main()
