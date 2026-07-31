"""Run everything that must pass before a commit, with nothing masking a failure.

## Why this exists rather than a list in a document

`CLAUDE.md` carries the pre-commit set as nine commands to be typed. Typing them
means composing them, and composing them is where the exit code gets lost:

    python -m unittest discover -s tests -t . -q 2>&1 | tail -3 && ...

That pipeline reports the exit status of `tail`, which is always 0. **A red suite
reads exactly like a green one**, and the `&&` chain after it runs anyway. It
happened here on 2026-07-31, one command before a commit, and it is the second
instance — `CLAUDE.md` already carries a note about `mutate.py --verify` exiting
1 on a lock refusal and looking like 0 for the same reason, ending in `| tail`.

`set -o pipefail` fixes it and has to be remembered every time. **This does not
have to be remembered**, which is `CLAUDE.md` rule 18's preference stated
plainly: prefer a rule that makes the mistake structurally impossible over one
that asks for more care.

No step is piped. Every exit code is checked. A failure is loud and the run
continues so one invocation reports everything wrong rather than the first thing.

## What this does NOT duplicate, and what was searched

Searched by capability — preflight, run all, before commit, gate, exit code,
pipefail — across `tools/`, `tests/`, `.github/workflows/` and `CLAUDE.md`.

- **`.github/workflows/checks.yml` runs the same checks and is not replaced.**
  CI is the authority and runs the FULL mutation harness sharded six ways, which
  is 45-80 minutes and deliberately not run locally. This is the local gate: the
  same checks minus that harness, plus `--verify`.
- **No checker is reimplemented.** Each is invoked as a subprocess by name, so
  there is one implementation of every rule and this file knows only the list.
- **`tools/mutate.py --changed`** is deliberately absent. John removed it from
  the local set on 2026-07-28 because it cost ten to twenty minutes per commit;
  `CLAUDE.md` records what that trades away and why CI covers it.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

#: In order, cheapest and most diagnostic first. `--verify` is first because it
#: is the only check that catches the source on disk not being the source anyone
#: means -- commit `3634a23` shipped a live mutation because nothing ran it.
STEPS: tuple[tuple[str, tuple[str, ...]], ...] = (
    ("source is not mid-mutation", ("tools/mutate.py", "--verify")),
    ("the test suite", ("-m", "unittest", "discover", "-s", "tests",
                        "-t", ".", "-q")),
    ("workflow flags match the scripts", ("tools/check_workflows.py",)),
    ("repo rails", ("tools/check_rails.py",)),
    ("no duplicated function bodies", ("tools/check_duplication.py",)),
    ("the decision tree", ("tools/check_decisions.py",)),
    ("option records", ("tools/check_options.py",)),
    ("every record number is in its source", ("tools/check_provenance.py",)),
    ("explainers and their index", ("tools/check_explainers.py",)),
    ("no commit message lost a word", ("tools/check_commit_messages.py",)),
    ("every pinned number says where it came from",
     ("tools/check_constants.py",)),
)


def run(command: tuple[str, ...]) -> tuple[int, str]:
    """One step, its output captured whole. **Never piped.**

    `capture_output` rather than a pipe is the whole point of this file: the
    returned code is the step's own, not a downstream `tail`'s.
    """
    finished = subprocess.run(
        [sys.executable, *command], cwd=ROOT, capture_output=True,
        text=True, encoding="utf-8", errors="replace")
    return finished.returncode, (finished.stdout or "") + (finished.stderr or "")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--quiet", action="store_true",
                        help="print output only for steps that failed")
    args = parser.parse_args()

    started = time.time()
    failures: list[tuple[str, str]] = []
    for label, command in STEPS:
        began = time.time()
        code, output = run(command)
        mark = "ok  " if code == 0 else "FAIL"
        print(f"{mark}  {label:<44} {time.time() - began:6.1f}s")
        if code != 0:
            failures.append((label, output))
        elif not args.quiet:
            tail = [line for line in output.splitlines() if line.strip()][-1:]
            for line in tail:
                print(f"        {line[:110]}")

    print(f"\n{len(STEPS) - len(failures)}/{len(STEPS)} passed in "
          f"{time.time() - started:.0f}s")
    for label, output in failures:
        print(f"\n{'=' * 70}\nFAILED: {label}\n{'=' * 70}")
        print(output.strip()[-4000:])
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
