"""Every module in the tree imports. A module nothing imports is checked by nothing.

## Why this exists, and it is not hypothetical

`openplexus/tasks/asking.py` was written on 2026-07-31 against a function that had
not been extracted yet. It could not import at all — a bare `ImportError` on line
one — and **the full pre-commit gate reported 11/11 passed.**

Nothing was wrong with any of those eleven checks. The suite runs the tests, and
no test imported the new module. `check_rails` reads source as text.
`check_constants` parses an AST without executing it. `check_duplication`
normalises function bodies. **Not one of them has to load a module to do its
job**, so a file that cannot be loaded is invisible to all of them at once.

That is a gap of exactly the shape this project keeps recording: not a wrong
answer, but a mechanism that produces a confident green while doing nothing about
the thing in front of it. A new module is the least-tested code in the repository
at the moment it is written, and it was the only code no check could see.

## What it does, and what it deliberately does not

It imports every module under the scanned packages and reports the ones that
raise. That is all. **It is not a substitute for a test** — importing proves the
file parses and its top level runs, and says nothing about whether anything in it
is correct. What it buys is that "this does not even load" can never again be
reported as a pass.

Scripts under `experiments/` are excluded: several run work at import time by
design, and loading a hundred of them would turn a one-second gate into a sweep.
Their entry point is `harness.parse_args`, which `check_rails` already requires.

## What this does NOT duplicate, and what was searched

Searched by capability — import, load, module, smoke, collect — across `tools/`,
`tests/` and `.github/workflows/`.

- **The test suite** loads whatever the tests reference. This loads what they do
  not, which is the entire point.
- **`tools/check_rails.py`** and **`tools/check_constants.py`** read files as
  text and as an AST respectively; neither executes one.
- **`tools/check_duplication.py`** parses without importing.
- **`tools/mutate.py --verify`** compares source text against its own table.
"""

from __future__ import annotations

import importlib
import pathlib
import sys
import traceback

ROOT = pathlib.Path(__file__).resolve().parent.parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

#: Packages whose every module must load. `experiments/` is out by design -- see
#: the docstring; `testbed/` is out because it expects a container runtime.
SCANNED = ("openplexus", "tools", "tests")

#: Modules that are entry points with side effects at import, or that this file
#: would recurse into. Kept explicit rather than pattern-matched so an addition
#: is a visible decision.
SKIP = {"tools.check_imports"}


def modules() -> list[str]:
    """Dotted names for every module under `SCANNED`, in a stable order."""
    found: list[str] = []
    for package in SCANNED:
        for path in sorted((ROOT / package).rglob("*.py")):
            if "__pycache__" in path.parts:
                continue
            relative = path.relative_to(ROOT).with_suffix("")
            parts = list(relative.parts)
            if parts[-1] == "__init__":
                parts.pop()
            if not parts:
                continue
            name = ".".join(parts)
            if name not in SKIP:
                found.append(name)
    return found


def main() -> int:
    broken: list[tuple[str, str]] = []
    names = modules()
    for name in names:
        try:
            importlib.import_module(name)
        except BaseException:                      # noqa: BLE001 - report, do not judge
            broken.append((name, traceback.format_exc().strip().splitlines()[-1]))

    if broken:
        print("A MODULE DOES NOT IMPORT.\n")
        for name, why in broken:
            print(f"  {name}\n      {why}")
        print("\nNo other check loads a module, so this one failing means every "
              "other check\npassed over a file it could not read. That is how a "
              "broken module reached\na green pre-commit run on 2026-07-31.")
        return 1

    print(f"imports ok - {len(names)} module(s) across {len(SCANNED)} packages")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
