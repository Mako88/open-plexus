"""A row's evidence must be its OWN bullet, and no whole-tree run can check that.

**This does not duplicate** `tools/check_decisions.py`'s own run, which answers
*"is today's tree cited"*. It asserts the property that answer depends on, and it
is a property the whole-tree run is structurally unable to see: the parser goes
green whenever no row happens to need the text it is wrongly reading. That was
true on the day the bound was written — 15 rows had blocks reaching past their
own bullet and **0 of them needed the extra text to pass** — so a tree-wide run
before and after the fix is identical, and neither says whether the fix works.

## Why it exists

`check_decisions` collected an option's block by running on until the next option
or section, which swallowed blank lines and whole following paragraphs. The
`Shard the count table` row therefore passed the evidence check on a citation
belonging to text underneath it, and it was found only by accident: inserting a
new option after it made the row start failing.

That row was fixed by hand. **The class was not**, and the handoff recorded
"whether other rows pass the same way is unchecked" as an open item. The answer,
measured, is that none did — but nothing stopped the next one.
"""

from __future__ import annotations

import unittest

from tools.check_decisions import EVIDENCE, rows

#: An uncited option, then a blank line, then a paragraph that DOES carry a
#: citation. This is the exact shape of the defect: the paragraph belongs to
#: nobody, and an unbounded parser hands it to the row above.
SWALLOW = """## 1. A component

- ✅ **An option nobody cited** — it says nothing about where it came from.

**Open — something else entirely.** This paragraph cites `note 065` and
belongs to the section, not to the row above it.
"""

#: The legitimate case that must keep working: a bullet whose citation is on its
#: own wrapped continuation line. If the bound were "the first line only", this
#: row would start failing and the fix would have traded one defect for another.
WRAPPED = """## 1. A component

- ✅ **An option cited on its second line** — the claim runs long and wraps
  onto a continuation line, where it cites `note 065`.
"""


def evidence_for(text: str) -> list[bool]:
    """Whether each row carries evidence, judged exactly as `main` judges it."""
    return [bool(EVIDENCE.search(title + "\n" + "\n".join(body)))
            for _, title, body in rows(text)]


class ARowsBlockStopsAtItsOwnBullet(unittest.TestCase):

    def test_a_following_paragraph_is_not_the_rows_evidence(self):
        found = rows(SWALLOW)
        self.assertEqual(len(found), 1, "one option in the fixture")
        self.assertEqual(found[0][2], [],
                         "an option followed by a blank line has no block; the "
                         "paragraph below belongs to the section")
        self.assertEqual(evidence_for(SWALLOW), [False],
                         "the row cites nothing and must be judged uncited, "
                         "however well cited the text beneath it is")

    def test_and_a_WRAPPED_citation_still_counts(self):
        # THE COMPANION. Without it, a bound of "the option line alone" would
        # pass the test above and silently reject every row whose claim wraps --
        # trading a false pass for a false failure, which is worse.
        self.assertEqual(len(rows(WRAPPED)[0][2]), 1,
                         "the indented continuation IS part of the bullet")
        self.assertEqual(evidence_for(WRAPPED), [True])

    def test_the_real_tree_does_not_depend_on_swallowed_text(self):
        """The measurement behind the docstring, kept as an assertion.

        If a future row starts relying on a paragraph below it, the tree-wide
        run will fail and this says why. Reading the committed file rather than
        a fixture is deliberate: the fixtures prove the rule, this proves the
        tree obeys it.
        """
        from tools.check_decisions import CHOSEN, REFUTED, TREE
        text = TREE.read_text(encoding="utf-8")
        uncited = [title for _, title, body in rows(text)
                   if (title.startswith(f"- {CHOSEN}")
                       or title.startswith(f"- {REFUTED}"))
                   and not EVIDENCE.search(title + "\n" + "\n".join(body))]
        self.assertEqual(uncited, [],
                         "every chosen or refuted row must cite within its own "
                         "bullet")


if __name__ == "__main__":
    unittest.main()
