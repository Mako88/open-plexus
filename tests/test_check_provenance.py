"""The provenance resolver must be case-exact, on every platform.

**This does not duplicate** `tools/check_provenance.py`'s own run, which is a
whole-repo pass and answers *"do today's citations resolve"*. This asserts the
property that pass depends on, and it is a property no whole-repo run can check:
a repo where every citation happens to be lowercase would go green under a broken
resolver forever.

## Why it exists

The resolver lowercased every citation token before testing it on disk, so
`source docs/SCALE.md` was looked up as `docs/scale.md`. NTFS resolves that and
ext4 does not. The check therefore **passed on the machine it was run on and
failed on the machine that gates the commit** — the worst direction for a check
to be wrong in, because the dev machine is the one that decides whether to push.

Two records were reported as citing numbers not in their sources
(`external-persistent-store.md`, `hop-accumulate.md`) when both citations were
correct the whole time. A checker that cries wolf is one that gets switched off,
which its own resolver docstring says about a different failure.
"""

from __future__ import annotations

import unittest
from pathlib import Path

from tools.check_provenance import ROOT, path_exists_exactly

#: A real, committed file whose name is not all-lowercase. If this is ever
#: renamed the test fails loudly rather than going vacuous, which is the point of
#: naming a specific file instead of globbing for "something uppercase".
MIXED_CASE = "docs/SCALE.md"


class TheResolverIsCaseExact(unittest.TestCase):

    def test_the_real_name_resolves(self):
        self.assertTrue(path_exists_exactly(ROOT / MIXED_CASE),
                        f"{MIXED_CASE} is cited by option records and must resolve")

    def test_the_wrong_case_does_NOT_resolve(self):
        """The assertion that actually catches the bug.

        `Path.exists()` returns True here on Windows. Without this test the fix
        is invisible on the platform where the bug lives — which is how it
        survived in the first place.
        """
        self.assertFalse(path_exists_exactly(ROOT / MIXED_CASE.lower()),
                         "a lowercased citation resolved: the resolver is "
                         "case-insensitive again and CI will disagree with local")

    def test_a_missing_file_does_not_resolve(self):
        """The companion, so the two above cannot both pass by returning False."""
        self.assertFalse(path_exists_exactly(ROOT / "docs/no-such-file-here.md"))

    def test_a_missing_PARENT_does_not_raise(self):
        """`iterdir` on an absent directory raises, and a checker must not crash.

        Reached by any citation naming a directory that was moved — which this
        project does routinely, since it archives rather than deletes.
        """
        self.assertFalse(path_exists_exactly(ROOT / "no-such-dir/whatever.md"))


if __name__ == "__main__":
    unittest.main()
