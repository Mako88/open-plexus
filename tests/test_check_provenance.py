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

from tools.check_provenance import (ROOT, missing_scripts,
                                    path_exists_exactly)

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


class AFetchedDatasetPathIsNotCheckable(unittest.TestCase):
    """`script` may name a real data file, and CI will not have it.

    **This does not duplicate** the whole-repo run, which is green whenever no
    record happens to name one. The exemption has to be narrow — wide enough to
    stop a false failure, tight enough that a renamed script is still caught —
    and only fixtures can say which.

    ## Why it exists

    `data/*/` is gitignored on purpose: `tools/fetch_*.py` pins each URL, size
    and sha256 instead of carrying the bytes forever. So a `script` field reading
    `tools/invariant_dimension.py --graph data/fb15k237/train.txt` resolves on a
    machine that has fetched FB15k-237 and nowhere else.

    That made this check **pass locally and fail in CI** — the exact direction
    the module's other test was written about, in this same function, and it
    cost a red run on 2026-07-31. Naming the real file is correct documentation
    of what was run, so the fix is to stop checking a path that cannot be
    checked rather than to make records vaguer.
    """

    def test_a_fetched_dataset_path_is_exempt(self):
        self.assertEqual(
            missing_scripts("    script  tools/check_provenance.py "
                            "--graph data/fb15k237/train.txt"), [])

    def test_but_a_MISSING_SCRIPT_beside_it_is_still_caught(self):
        # THE COMPANION. Without it the exemption above would pass for a
        # function that had stopped checking anything at all.
        self.assertEqual(
            missing_scripts("    script  tools/gone_missing.py "
                            "--graph data/fb15k237/train.txt"),
            ["tools/gone_missing.py"])

    def test_and_a_TRACKED_top_level_data_file_is_still_checked(self):
        # The exemption is `data/*/`, which is what .gitignore excludes.
        # `data/tinyshakespeare.txt` sits directly in `data/` and IS committed,
        # so a typo in it must still fail.
        self.assertEqual(missing_scripts("    script  data/tinyshakespeare.txt"),
                         [])
        self.assertEqual(
            missing_scripts("    script  data/not_a_real_file.txt"),
            ["data/not_a_real_file.txt"])
