"""A guard that fires in a worker must fail FAST, not hang the pool.

**Every fail-fast guard in this project did the opposite in the configuration
sweeps actually run in.** `run_one` refuses with `SystemExit` — unknown arm, arm
and cache disagreeing, more text than the corpus holds, an absurd loss — and
every sweep passes `--workers 2`.

`SystemExit` inherits from `BaseException`, not `Exception`. `Pool` catches
`Exception` in a worker and returns it as a result; a `BaseException` kills the
worker silently and `map` waits for a result that never arrives. So a guard
designed to stop a bad run in one second instead consumed the job's entire
300-minute timeout and reported nothing.

Measured on g11-07's first dispatch: the baseline cell tripped a guard and sat
for 23 minutes against an expected 2, and was cancelled by hand.

## Why this test runs in a subprocess

The property under test is *does not hang*. A test that asserts it directly
would hang the whole suite when it regresses — turning a failure into a stall,
which is the same trade this bug made. The subprocess carries the timeout, so a
regression is a fast, readable failure.
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

PROGRAM = textwrap.dedent('''
    import sys
    sys.path.insert(0, {root!r})
    from experiments import harness

    def refuse(x):
        if x == 1:
            raise SystemExit("guard fired")
        return x

    if __name__ == "__main__":
        try:
            harness.spread(refuse, [0, 1], 2)
        except RuntimeError as error:
            print("REFUSED:", error)
        except BaseException as error:
            print("OTHER:", type(error).__name__, error)
        else:
            print("NO ERROR -- the refusal vanished")
''')


class AGuardInAWorker(unittest.TestCase):

    def run_pool(self, timeout: int = 90) -> str:
        """Run the program from a FILE, not `-c`.

        `spread` uses the spawn start method, which re-imports `__main__` in the
        child and pickles the target function BY NAME. A function defined in a
        `-c` program has no importable module, so the child cannot rebuild it --
        and the failure mode is the pool hanging, which is the exact bug under
        test. The first version of this test hung for that reason and would have
        looked like the fix not working.
        """
        with tempfile.TemporaryDirectory() as directory:
            script = Path(directory) / "refusal_probe.py"
            script.write_text(PROGRAM.format(root=str(ROOT)), encoding="utf-8")
            result = subprocess.run(
                [sys.executable, str(script)], capture_output=True, text=True,
                timeout=timeout, cwd=str(ROOT))
        return result.stdout.strip()

    def test_it_does_not_hang_the_pool(self):
        """The regression. Before the fix this timed out; after it, 0.3s."""
        try:
            output = self.run_pool()
        except subprocess.TimeoutExpired:
            self.fail("a worker's SystemExit hung the pool -- the guard that "
                      "should stop a bad run in one second now costs the job's "
                      "whole timeout")
        self.assertTrue(output, "the worker produced no output at all")

    def test_the_refusal_survives_as_an_error(self):
        """Not merely 'does not hang'. A wrapper that swallowed the refusal
        would also not hang, and would let a guarded-against run continue."""
        self.assertIn("REFUSED:", self.run_pool())

    def test_the_reason_is_preserved(self):
        """A guard's message is the diagnosis. Converting the exception must
        not discard what it said, or the failure becomes 'a worker died'."""
        self.assertIn("guard fired", self.run_pool())


class TheWrapperItself(unittest.TestCase):
    """Fast checks on the conversion, without paying for a pool."""

    def test_it_passes_results_through(self):
        from experiments.harness import _Guarded
        self.assertEqual(_Guarded(abs)(-3), 3)

    def test_it_converts_system_exit(self):
        from experiments.harness import _Guarded

        def refuse(_):
            raise SystemExit("nope")

        with self.assertRaises(RuntimeError) as raised:
            _Guarded(refuse)(0)
        self.assertIn("nope", str(raised.exception))

    def test_it_leaves_ordinary_exceptions_alone(self):
        """Only `SystemExit` needs converting. Catching everything would hide
        the ordinary failures `Pool` already reports correctly."""
        from experiments.harness import _Guarded

        def broken(_):
            raise ValueError("ordinary")

        with self.assertRaises(ValueError):
            _Guarded(broken)(0)


if __name__ == "__main__":
    unittest.main()
