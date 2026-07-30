"""Run every pre-commit check and fail if ANY of them failed.

## The near-miss this exists for

The five checks were being run as one compound shell command:

    python tools/mutate.py --verify; python -m unittest discover -s tests -t . -q;
    python tools/check_workflows.py; python tools/check_rails.py;
    python tools/check_duplication.py

A shell reports the exit code of the **last** statement, so that line says
nothing whatever about the first four. On 2026-07-28 it reported success while
`unittest` and `check_duplication` were both failing -- a real duplicate `load`
copied between two new summarisers, and the tests that guard the duplication
baseline. Both were caught only because the exit codes were then checked one at
a time, by hand, on a hunch.

**Interleaved output made it worse rather than better.** Several checks print
reassuring lines of their own -- `rails ok` appears twice, because a test shells
out to the rails checker -- so the tail of the combined output looked exactly
like a passing run.

CLAUDE.md rule 18: prefer a rule that makes the mistake structurally impossible
over one that asks for more care. Reading five exit codes correctly is care.
This is the check.

## Usage

    python tools/check_all.py           # everything
    python tools/check_all.py --fast    # skip the slow mutation pass

`--changed` is deliberately NOT run here. It edits source, so it must not run
concurrently with the test suite -- the mutation harness takes the tree
exclusively, and a concurrent run once reported seven phantom failures in a file
nobody had touched.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

#: (name, argv, is_slow). Ordered cheapest first so a fast failure is reported
#: fast, EXCEPT that the mutation verify comes first: if the tree still carries
#: a live mutation, every other result is measuring mutated source and none of
#: them means anything.
CHECKS: list[tuple[str, list[str], bool]] = [
    ("mutate --verify", [sys.executable, "tools/mutate.py", "--verify"], False),
    ("check_workflows", [sys.executable, "tools/check_workflows.py"], False),
    ("check_rails", [sys.executable, "tools/check_rails.py"], False),
    ("check_duplication", [sys.executable, "tools/check_duplication.py"], False),
    # ONE CHECK WHERE THERE WERE TWO. `check_state` and `check_architecture`
    # enforced the three-document structure that produced the drift they were
    # written to catch -- a 6,040-line log nobody could read whole. Their two real
    # ratchets are kept: a declared census that cannot drift from the body, and
    # "a state with no measurement is UNTRIED, never probably fine".
    ("check_decisions", [sys.executable, "tools/check_decisions.py"], False),
    ("check_commit_messages",
     [sys.executable, "tools/check_commit_messages.py"], False),
    ("unittest", [sys.executable, "-m", "unittest", "discover",
                  "-s", "tests", "-t", "."], True),
]


def run(name: str, argv: list[str]) -> tuple[bool, float, str]:
    """Run one check, capturing its output rather than interleaving it."""
    started = time.time()
    finished = subprocess.run(argv, cwd=ROOT, capture_output=True, text=True)
    elapsed = time.time() - started
    output = (finished.stdout or "") + (finished.stderr or "")
    return finished.returncode == 0, elapsed, output


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fast", action="store_true",
                        help="skip the slow checks")
    args = parser.parse_args()

    results: list[tuple[str, bool, float, str]] = []
    for name, argv, slow in CHECKS:
        if slow and args.fast:
            print(f"SKIP  {name}")
            continue
        passed, elapsed, output = run(name, argv)
        results.append((name, passed, elapsed, output))
        print(f"{'PASS' if passed else 'FAIL'}  {name:20s} {elapsed:6.1f}s")

    failed = [r for r in results if not r[1]]
    if not failed:
        print(f"\nall {len(results)} check(s) passed")
        return 0

    # The failing output is printed AFTER the summary, so the verdict is not
    # buried under it. Burying the verdict is how the compound command's
    # reassuring tail was mistaken for a pass.
    for name, _, _, output in failed:
        print(f"\n{'=' * 60}\nFAILED: {name}\n{'=' * 60}")
        print(output.strip()[-4000:])

    print(f"\n{len(failed)} of {len(results)} check(s) FAILED: "
          + ", ".join(name for name, _, _, _ in failed))
    return 1


if __name__ == "__main__":
    sys.exit(main())
