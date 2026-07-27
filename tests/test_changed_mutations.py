"""`--changed` runs the mutations that this work could have invalidated.

**A surviving mutation was invisible locally, and that is the gap this closes.**
The five pre-commit checks run `mutate.py --verify`, which asserts only that
every mutation's original text is still present. The full harness edits the
source for twenty minutes, so it lives in CI, sharded — which means a vacuous
test region passes every local check and surfaces later, on a run nobody is
watching, attached to whichever commit happened to be pushed next.

That is not hypothetical. `the-cache-admits-by-RECENCY-not-residual` and
`the-cache-read-is-not-gated-by-the-MATCH` both survived at `b480926` and at
least one commit before it. The exact cache is the project's first controlled
improvement on the corpus and its two defining claims had nothing asserting
them.

`--changed` selects only the mutations whose target file this work touches:
seconds rather than twenty minutes, and exactly the set that can have been
invalidated.
"""

from __future__ import annotations

import unittest
from pathlib import Path

from tools import mutate

RETRIEVAL = mutate.ROOT / "openplexus" / "retrieval.py"
LOCAL = mutate.ROOT / "openplexus" / "models" / "local_memory.py"


class SelectingByWhatChanged(unittest.TestCase):

    def setUp(self):
        self.real = mutate.changed_files

    def tearDown(self):
        mutate.changed_files = self.real

    def pretend(self, *paths: Path):
        mutate.changed_files = lambda: {p.resolve() for p in paths}

    def test_touching_a_file_selects_its_mutations(self):
        self.pretend(RETRIEVAL)
        names = {m.name for m in mutate.selected(["--changed"])}
        self.assertIn("the-cache-admits-by-RECENCY-not-residual", names)
        self.assertIn("the-cache-read-is-not-gated-by-the-MATCH", names)

    def test_it_selects_only_that_file_s_mutations(self):
        """Running everything would be correct and useless -- it is the
        twenty-minute run that made this CI-only in the first place."""
        self.pretend(RETRIEVAL)
        paths = {m.path.resolve() for m in mutate.selected(["--changed"])}
        self.assertEqual(paths, {RETRIEVAL.resolve()})

    def test_touching_nothing_mutated_selects_nothing(self):
        self.pretend(mutate.ROOT / "README.md")
        self.assertEqual(mutate.selected(["--changed"]), [])

    def test_two_touched_files_select_both_sets(self):
        self.pretend(RETRIEVAL, LOCAL)
        paths = {m.path.resolve() for m in mutate.selected(["--changed"])}
        self.assertEqual(paths, {RETRIEVAL.resolve(), LOCAL.resolve()})

    def test_no_flag_still_means_everything(self):
        self.assertEqual(len(mutate.selected([])), len(mutate.MUTATIONS))


class TheFileListItself(unittest.TestCase):

    def test_it_returns_absolute_resolved_paths(self):
        """Mutations carry absolute paths, so a relative entry here would match
        nothing and `--changed` would silently select an empty set -- passing
        instantly and checking nothing, which is the failure mode this whole
        file is about."""
        for path in mutate.changed_files():
            self.assertTrue(path.is_absolute(), path)
            self.assertEqual(path, path.resolve(), path)


if __name__ == "__main__":
    unittest.main()
