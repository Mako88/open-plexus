"""Every experiment script still starts. The one gap `check_imports` leaves open.

## Why this exists, and it is one night old

`tools/check_imports.py` loads every module under `openplexus/`, `tools/` and
`tests/`, and **deliberately skips `experiments/`** — several scripts there do
work at import time, so loading a hundred of them would turn a one-second gate
into a sweep. That exclusion is right and it leaves a hole exactly the shape of
the one that opened on 2026-08-01.

A duplicated helper was extracted out of two sweeps into a shared class. The
edit renamed every call site to `floor_of.vector(...)` and never created
`floor_of`. **Both files then had an undefined name in their main path**, and:

- the test suite did not notice, because no test imports an experiment;
- `check_imports` did not notice, because it does not scan the directory;
- `check_duplication` did not notice, because it parses without executing;
- `check_constants` did not notice, for the same reason;
- `mutate.py --verify` did not notice, because the text it looks for was intact.

**Seven checks, all green, on a script that could not run.** It surfaced because
a run was launched by hand a few minutes later. That is the same shape as the
failure `check_imports`'s own docstring was written about — a new module being
the least-tested code in the repository at the moment it is written — and the
answer is the same one: make *"this does not even start"* impossible to report
as a pass.

## What it does, and what it deliberately does not

It runs each script with `--help` and requires exit 0. `argparse` prints usage
and exits before `main` does any work, so the cost is one interpreter start per
script and nothing is measured, fetched or written.

**It is not a test.** Starting proves the file parses, its imports resolve and
its argument parser is constructible. It says nothing about whether any number
the script produces is right — that is what the sweeps' own controls and floors
are for. What it buys is that a script which cannot run can no longer sit in the
tree looking finished.

A script with no `--help` is a failure rather than a skip: every sweep here takes
arguments through `argparse`, and one that does not has diverged from the shape
the others share.
"""

from __future__ import annotations

import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
EXPERIMENTS = ROOT / "experiments"

#: Files that are not entry points. `__init__.py` makes the directory a package
#: so one sweep can import another's ranking machinery rather than copy it, and
#: `harness.py` is what they share. **Kept as a list of names rather than a
#: pattern**: a sweep that fails this check by being renamed to look like a
#: helper is exactly what a pattern would let through.
SKIP = {"__init__.py", "harness.py"}


def scripts() -> list[pathlib.Path]:
    return sorted(path for path in EXPERIMENTS.glob("*.py")
                  if path.name not in SKIP)


def starts(path: pathlib.Path) -> tuple[bool, str]:
    """Run one script's `--help`. True when it exits 0 and it offers `--json`.

    **A sweep that cannot be asked for structured output is a sweep whose result
    exists only as prose in a terminal.** That is the shape of the record this
    project threw away in its restructure, so it is checked rather than left to
    convention -- and `--help` already has to be run, so it costs nothing.
    """
    finished = subprocess.run(
        [sys.executable, str(path.relative_to(ROOT)), "--help"], cwd=ROOT,
        capture_output=True, text=True, encoding="utf-8", errors="replace",
        timeout=120)
    output = (finished.stdout or "") + (finished.stderr or "")
    if finished.returncode == 0 and "--json" not in output:
        return False, ("it starts, and it offers no --json, so its results "
                       "would exist only as prose in a terminal")
    return finished.returncode == 0, output


def main() -> int:
    if not EXPERIMENTS.exists():
        print("no experiments/ directory - nothing to check")
        return 0
    found = scripts()
    if not found:
        print("no experiment scripts - nothing to check")
        return 0

    broken: list[tuple[pathlib.Path, str]] = []
    for path in found:
        ok, output = starts(path)
        if not ok:
            broken.append((path, output))

    if broken:
        print("AN EXPERIMENT SCRIPT DOES NOT START.\n")
        for path, output in broken:
            print(f"  {path.relative_to(ROOT)}")
            for line in output.strip().splitlines()[-6:]:
                print(f"    {line}")
            print()
        print("Nothing else in the gate can see this: no test imports an\n"
              "experiment, check_imports skips the directory, and every other\n"
              "checker reads the source without running it.")
        return 1

    print(f"experiments ok - {len(found)} script(s) start")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
