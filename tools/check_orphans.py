"""Nothing is built and left unwired. The failure that forced the restructure.

## Why this exists

John, on the restructure that opened this project's current tree: *"I came to
find out that there were a bunch of things that had been built but weren't
actually wired in and being used, and so we kept repeating the same experiments
on the same pieces in different sessions because the new sessions didn't realise
things had already been built."*

That is a specific, recurring, expensive failure and it is detectable. A module
that nothing but its own test imports has been built and not connected. It will
be rediscovered, re-proposed and rebuilt, because the only thing that would have
told a later session it exists is a search nobody thought to run.

## What counts as wired

Imports are split into TESTS and CALLERS. A test importing a module proves it
works; it does not make anything use it. A module with tests and no callers is
exactly the state this check is for — finished, correct, and connected to
nothing.

`tools/duplication_baseline.json` is the model for the ratchet: a module already
in `tools/orphans_baseline.json` is allowed and carries a written reason, and
anything new fails. **The baseline is a debt list, not an exemption list** — an
entry is a promise that someone knows, not that it does not matter.

## What this does NOT duplicate, and what was searched

Searched by capability — orphan, unused, dead code, import graph, wired —
across `tools/`, `tests/` and `.github/workflows/`.

- **`tools/check_imports.py`** proves every module LOADS. A module can load
  perfectly and be connected to nothing, which is this failure exactly.
- **`tools/check_experiments.py`** proves every sweep STARTS. Same distinction
  one level out.
- **`tools/check_duplication.py`** supplies the ratchet-with-a-baseline pattern
  this borrows.
"""

from __future__ import annotations

import ast
import collections
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
LIBRARY = ROOT / "openplexus"
BASELINE = ROOT / "tools" / "orphans_baseline.json"

#: Where a caller may live. `tests` is deliberately NOT here -- that is the whole
#: point of the check.
CALLERS = ("openplexus", "tools", "experiments", "testbed")


def modules() -> list[str]:
    """Every importable module under the library, dotted from `openplexus`."""
    return sorted(path.relative_to(LIBRARY).with_suffix("").as_posix()
                  .replace("/", ".")
                  for path in LIBRARY.rglob("*.py")
                  if path.name != "__init__.py")


def imported_by(area: str) -> collections.Counter:
    """How many files under `area` import each library module.

    Both spellings are resolved. `import openplexus.tasks.spoken` names the
    module directly; `from openplexus.tasks import spoken` names its PACKAGE and
    puts the module in the imported names, and a checker that missed the second
    would report most of `tasks/` as orphaned.
    """
    found: collections.Counter = collections.Counter()
    for path in (ROOT / area).rglob("*.py"):
        try:
            tree = ast.parse(path.read_text(encoding="utf-8"))
        except (SyntaxError, UnicodeDecodeError):
            continue
        here = path.relative_to(ROOT).with_suffix("").as_posix().replace("/", ".")
        seen = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                seen.update(alias.name for alias in node.names)
            elif isinstance(node, ast.ImportFrom) and node.module:
                seen.add(node.module)
                seen.update(f"{node.module}.{alias.name}"
                            for alias in node.names)
        for name in seen:
            if name.startswith("openplexus."):
                module = name[len("openplexus."):]
                if f"openplexus.{module}" != here:
                    found[module] += 1
    return found


def main() -> int:
    callers: collections.Counter = collections.Counter()
    for area in CALLERS:
        if (ROOT / area).exists():
            callers.update(imported_by(area))
    tested = imported_by("tests")

    known = json.loads(BASELINE.read_text(encoding="utf-8")) if BASELINE.exists() else {}
    orphans = [name for name in modules() if not callers[name]]
    fresh = [name for name in orphans if name not in known]

    if fresh:
        print("A MODULE IS BUILT AND WIRED TO NOTHING.\n")
        for name in fresh:
            state = ("has tests and no callers" if tested[name]
                     else "has no tests and no callers")
            print(f"  openplexus/{name.replace('.', '/')}.py — {state}")
        print("\nA module nothing calls will be rediscovered and rebuilt, because\n"
              "the only thing that would tell a later session it exists is a\n"
              "search nobody thought to run. That is the failure that forced this\n"
              "project's restructure.\n"
              "\nWire it in, or add it to tools/orphans_baseline.json with a\n"
              "reason -- the baseline is a debt list and every entry is a promise\n"
              "that somebody knows.")
        return 1

    carried = [name for name in known if name in orphans]
    print(f"no unwired modules - {len(modules())} checked, "
          f"{len(carried)} known orphan(s) carried")
    for name in carried:
        print(f"        {name}: {known[name]}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
