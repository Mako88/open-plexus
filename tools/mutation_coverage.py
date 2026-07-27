"""Which parts of the model has no mutation ever pointed at?

**A test that reimplements the code it is testing passes whatever the code
does.** That has happened twice here: note 012 records cap values coming from a
reimplementation whose store never bound, and `test_corrective_writes` asserted
exactness against its own copy of the write rule, leaving a mutation of the real
rule alive.

**Mutation testing is the audit for that**, and it is the only one that works: a
static check cannot tell "asserts on the model" from "asserts on a local copy",
because in both cases the asserted values look model-derived. A mutation does not
care how the test is written -- it breaks the real code and asks whether anything
notices.

So the coverage question is the audit question: **where the model has no mutation
pointed at it, the tests there are unaudited**, however many of them there are.

This does not run anything. It reads `tools/mutate.py`, finds which line of which
file each mutation targets, and reports the functions that contain none.

    python tools/mutation_coverage.py

## What it cannot tell you

A function WITH a mutation is not proven well tested -- one mutation covers one
line, and `run()` is six hundred lines long. **Coverage here is a floor, not a
grade**, and the per-function counts matter more than the pass/fail.
"""

from __future__ import annotations

import ast
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from tools.mutate import MUTATIONS  # noqa: E402

#: Files worth auditing. Everything a result depends on and nothing else --
#: experiments are read once and discarded, which mutate.py's own docstring says.
AUDITED = ("openplexus/models/local_memory.py", "openplexus/distributed.py",
           "openplexus/ngram.py", "openplexus/tasks/reward_recall.py",
           "openplexus/tasks/corpus.py", "tools/recovery.py")


def functions(path: Path) -> list[tuple[str, int, int]]:
    """(name, first line, last line) for every function in the file."""
    tree = ast.parse(path.read_text(encoding="utf-8"))
    found = []
    for node in ast.walk(tree):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            found.append((node.name, node.lineno, node.end_lineno or node.lineno))
    return sorted(found, key=lambda f: f[1])


def targeted() -> dict[str, list[int]]:
    """path -> the line numbers mutations edit."""
    hit: dict[str, list[int]] = {}
    for mutation in MUTATIONS:
        try:
            text = mutation.path.read_text(encoding="utf-8")
        except OSError:
            continue
        index = text.find(mutation.old)
        if index < 0:
            continue
        line = text.count("\n", 0, index) + 1
        key = mutation.path.relative_to(ROOT).as_posix()
        hit.setdefault(key, []).append(line)
    return hit


def main() -> int:
    hit = targeted()
    print(f"{len(MUTATIONS)} mutations across "
          f"{len(hit)} files\n")
    total_covered = total = 0
    gaps: list[str] = []

    for name in AUDITED:
        path = ROOT / name
        if not path.exists():
            continue
        lines = hit.get(name, [])
        covered = uncovered = 0
        biggest: list[tuple[int, str]] = []
        for function, start, end in functions(path):
            if function.startswith("_") and function != "__init__":
                continue
            inside = [line for line in lines if start <= line <= end]
            if inside:
                covered += 1
            else:
                uncovered += 1
                biggest.append((end - start + 1, function))
        total_covered += covered
        total += covered + uncovered
        share = covered / max(1, covered + uncovered)
        print(f"{name}")
        print(f"    {covered} of {covered + uncovered} functions carry a "
              f"mutation ({share:.0%}), {len(lines)} mutations in the file")
        for size, function in sorted(biggest, reverse=True)[:5]:
            print(f"      UNAUDITED  {function:<34} {size:>4} lines")
            gaps.append(f"{name}::{function}")

    print(f"\n{total_covered} of {total} audited functions carry at least one "
          f"mutation ({total_covered / max(1, total):.0%})")
    print("\n  A function WITH a mutation is not proven well tested -- one")
    print("  mutation covers one line and `run` is six hundred lines. This is a")
    print("  FLOOR, and the largest unaudited functions above are where a test")
    print("  reimplementing the code would go unnoticed longest.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
