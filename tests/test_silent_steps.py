"""A step that dies without failing its job, which is how g11-04 lost its numbers.

`tools/check_workflows.py` guards two halves of one failure. Neither is exotic
and neither announces itself:

- a job runs a tool that imports a package the job never installed, so the tool
  dies at import and prints nothing;
- the tool is piped into `tee`, so the pipeline reports `tee`'s status and the
  step is marked **success**.

Together they produce a green run whose summary contains only the header line —
which reads as "the sweep found nothing" rather than "the summariser never ran".
That happened on run `30295529865`: twelve of twelve cells returned, the
aggregate job had no `pip install numpy`, and every number in g11-04 had to be
recovered by hand from the raw artifacts.

The tests are about the RULE, not the tree, so they run against workflow text
written here. `test_the_real_tree_is_clean` is the one that would catch a
regression in the workflows themselves.
"""

from __future__ import annotations

import unittest

from tools import check_workflows

CLEAN = """\
name: example
jobs:
  aggregate:
    steps:
      - run: pip install numpy
      - name: Combine
        run: |
          set -o pipefail
          python -m tools.summarise_example | tee -a summary.txt
"""

NO_INSTALL = CLEAN.replace("      - run: pip install numpy\n", "")
NO_PIPEFAIL = CLEAN.replace("          set -o pipefail\n", "")


def needs_numpy(reference: str) -> set[str]:
    return {"numpy"}


def needs_nothing(reference: str) -> set[str]:
    return set()


class AStepThatCanDieQuietly(unittest.TestCase):

    def test_a_clean_step_reports_nothing(self):
        self.assertEqual(
            check_workflows.silent_failures("x.yml", CLEAN, needs_numpy), [])

    def test_an_uninstalled_import_is_reported(self):
        problems = check_workflows.silent_failures(
            "x.yml", NO_INSTALL, needs_numpy)
        self.assertEqual(len(problems), 1)
        self.assertIn("numpy", problems[0])
        self.assertIn("aggregate", problems[0])

    def test_a_tee_without_pipefail_is_reported(self):
        problems = check_workflows.silent_failures(
            "x.yml", NO_PIPEFAIL, needs_numpy)
        self.assertEqual(len(problems), 1)
        self.assertIn("pipefail", problems[0])

    def test_the_two_halves_are_independent(self):
        """Either alone still loses the output, so neither check subsumes the
        other. Installing numpy does not make a `tee` pipeline honest, and
        pipefail does not supply a missing package."""
        both = check_workflows.silent_failures(
            "x.yml", NO_INSTALL.replace("          set -o pipefail\n", ""),
            needs_numpy)
        self.assertEqual(len(both), 2)

    def test_an_install_in_a_different_job_does_not_count(self):
        """The original bug exactly: the sweep job installed numpy and the
        aggregate job did not, and a whole-file search for `pip install numpy`
        finds one and concludes the workflow is fine."""
        split = """\
name: example
jobs:
  scaling:
    steps:
      - run: pip install numpy
      - run: python experiments/thing.py
  aggregate:
    steps:
      - name: Combine
        run: |
          set -o pipefail
          python -m tools.summarise_example | tee -a summary.txt
"""
        self.assertIn("pip install numpy", split)
        problems = check_workflows.silent_failures("x.yml", split, needs_numpy)
        self.assertEqual(len(problems), 1)
        self.assertIn("numpy", problems[0])

    def test_a_tool_needing_nothing_is_not_asked_to_install(self):
        self.assertEqual(
            check_workflows.silent_failures("x.yml", NO_INSTALL, needs_nothing),
            [])

    def test_a_tool_that_does_not_exist_is_reported(self):
        """A reference resolving to no file reads as 'imports nothing', so a
        rename would switch the dependency check off in the same change that
        broke the workflow. That is not hypothetical: renaming
        summarise_g11_04 to summarise_scaling_exponent turned two tests in this
        file red, and this is the case that would have caught the workflow."""
        problems = check_workflows.silent_failures(
            "x.yml", CLEAN, lambda reference: None)
        self.assertEqual(len(problems), 1)
        self.assertIn("does not exist", problems[0])

    def test_a_real_reference_resolves_and_a_renamed_one_does_not(self):
        self.assertIsNotNone(
            check_workflows.packages_needed("tools.summarise_scaling_exponent"))
        self.assertIsNone(check_workflows.packages_needed("tools.summarise_g11_04"))


class ReadingTheImportsOffTheTree(unittest.TestCase):

    def test_numpy_is_found_in_the_summariser_that_imports_it(self):
        found = check_workflows.third_party_imports(
            check_workflows.module_for("tools.summarise_scaling_exponent"))
        self.assertEqual(found, {"numpy"})

    def test_a_pure_stdlib_tool_needs_nothing(self):
        self.assertEqual(
            check_workflows.third_party_imports(
                check_workflows.module_for("tools.recovery")),
            set())

    def test_an_import_reached_through_a_repo_module_still_counts(self):
        """The summariser imports `tools.recovery`, which is stdlib-only —
        but the walk has to follow repo-local imports, or a summariser that
        reaches numpy through a helper reports as needing nothing."""
        seen: set = set()
        check_workflows.third_party_imports(
            check_workflows.module_for("tools.summarise_scaling_exponent"), seen)
        self.assertIn(check_workflows.module_for("tools.recovery"), seen)


class TheTreeItself(unittest.TestCase):

    def test_the_real_tree_is_clean(self):
        self.assertEqual(check_workflows.silently_failing_steps(), [])


if __name__ == "__main__":
    unittest.main()
