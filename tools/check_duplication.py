"""Find function bodies that are the same function twice.

Rule 9's ordinary argument is that a change has to be made in two places. The
argument *here* is worse and is rule 12's: when a bug is fixed in one copy and
not the other, the surviving copy keeps producing plausible numbers, and every
measurement taken through it is invalid while looking exactly like the corrected
ones. A duplicated path is a fix that did not land, wearing the appearance of one
that did.

## It would NOT have caught the case that motivated it, and that was measured

BACKLOG asked for this check by name and said it "would have found the five
copied refusals *before* one of them lost its floor check". **That claim is
false and the check itself refutes it.** Run over the pre-port tree at `9457c16`
— the last commit with the hand-copies in it — this finds **zero** duplicated
shapes among those six files.

The reason is the finding. Those copies had already diverged: one had lost its
floor check entirely, and three were choosing the learning rate that MAXIMISES
`oracle - none`, which a collapsed floor arm maximises. Divergence is what made
them dangerous, and divergence is exactly what defeats a structural hash.

**So this catches copies that have NOT yet drifted — the harmless ones — and is
blind to the ones that have.** That is prevention, not detection, and it is a
much narrower claim than the one that justified building it.

It is kept because prevention is still worth something and it demonstrated that
within minutes of being written: it caught `load_baseline`, copied between this
file and `check_rails.py` by the author of a tool for finding copies. What
catches a DRIFTED copy is `tools/mutate.py` — a mutation in one path that the
tests do not notice — and nothing else here does.

## How bodies are compared

Each function is reduced to its SHAPE: identifiers, attribute names, constants
and docstrings are erased, and only the structure of the statements and
expressions survives. So two loaders that differ in variable naming and in which
JSON key they read hash the same, while two that differ in control flow do not.

Bodies below `MIN_STATEMENTS` are ignored. A two-line function that returns a
field is not duplication, it is a language without a shorter way to say it.

## Scope, and why it is narrow

`tools/` and `experiments/` only, which is where copy-paste is the actual working
style — a summariser is written once against one sweep and read twice. Model code
is covered by ordinary review because it is read constantly.

Ratcheted against `tools/duplication_baseline.json` for the same reason
`check_rails.py` is: a check that fails on every legacy pair gets suppressed, and
a suppressed check makes the others look optional.

    python tools/check_duplication.py
    python tools/check_duplication.py --write-baseline
"""

from __future__ import annotations

import ast
import hashlib
import json
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
# Run either as `python tools/check_duplication.py` or `python -m
# tools.check_duplication`. The first puts tools/ on the path and not the root,
# so the import below would fail without this -- and checks.yml runs the other
# checks the first way.
sys.path.insert(0, str(ROOT))

from tools.check_rails import compare, read_baseline  # noqa: E402
BASELINE = ROOT / "tools" / "duplication_baseline.json"
SCOPE = ("tools", "experiments")
#: Below this a shared shape is a coincidence of the language, not a copy.
#:
#: Chosen by measurement rather than taste. At 5 the check found seven duplicated
#: shapes and NONE in `tools/`; at 4 it found the same seven plus one more --
#: `load_baseline`, copied between this file and `check_rails.py`, written
#: minutes apart by the author of a tool for finding copies. A threshold that
#: misses the copy sitting inside the copy-detector is set too high.
MIN_STATEMENTS = 4


class Shape(ast.NodeTransformer):
    """Erase every name, constant and docstring, keeping only structure."""

    def visit_Name(self, node):
        return ast.copy_location(ast.Name(id="_", ctx=node.ctx), node)

    def visit_Attribute(self, node):
        self.generic_visit(node)
        return ast.copy_location(
            ast.Attribute(value=node.value, attr="_", ctx=node.ctx), node)

    def visit_Constant(self, node):
        return ast.copy_location(ast.Constant(value=None), node)

    def visit_arg(self, node):
        return ast.copy_location(ast.arg(arg="_", annotation=None), node)


def _statements(node: ast.AST) -> int:
    return sum(1 for child in ast.walk(node) if isinstance(child, ast.stmt))


def shapes() -> dict[str, list[str]]:
    """hash -> the functions carrying that shape, as `path::name`."""
    found: dict[str, list[str]] = defaultdict(list)
    for folder in SCOPE:
        for path in sorted((ROOT / folder).rglob("*.py")):
            try:
                tree = ast.parse(path.read_text(encoding="utf-8"))
            except SyntaxError:
                continue
            for node in ast.walk(tree):
                if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                    continue
                body = list(node.body)
                if body and isinstance(body[0], ast.Expr) and isinstance(
                        getattr(body[0], "value", None), ast.Constant):
                    body = body[1:]          # drop the docstring
                if sum(_statements(s) for s in body) < MIN_STATEMENTS:
                    continue
                stripped = [Shape().visit(ast.parse(ast.unparse(s)))
                            for s in body]
                text = "\n".join(ast.dump(s) for s in stripped)
                digest = hashlib.sha1(text.encode("utf-8")).hexdigest()[:12]
                found[digest].append(
                    f"{path.relative_to(ROOT).as_posix()}::{node.name}")
    return found


def current() -> dict[str, list[str]]:
    """One entry per duplicated shape, named by the functions sharing it."""
    duplicated = sorted(" == ".join(sorted(names))
                        for names in shapes().values() if len(names) > 1)
    return {"D1-same-function-twice": duplicated}


def load_baseline() -> dict[str, list[str]]:
    return read_baseline(BASELINE, current())


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    found = current()

    if "--write-baseline" in argv:
        BASELINE.write_text(json.dumps(found, indent=2) + "\n", encoding="utf-8")
        total = sum(len(v) for v in found.values())
        print(f"wrote {BASELINE.relative_to(ROOT)} with {total} exemption(s)")
        return 0

    new, stale = compare(found, load_baseline())
    problems = 0
    for rail, pairs in new.items():
        for pair in pairs:
            print(f"DUPLICATE {rail}: {pair}")
            problems += 1
    for rail, gone in stale.items():
        for pair in gone:
            print(f"STALE BASELINE {rail}: {pair} is no longer duplicated "
                  f"(or no longer exists) -- remove the exemption")
            problems += 1

    exemptions = sum(len(v) for v in load_baseline().values())
    if problems:
        print(f"\n{problems} problem(s). {exemptions} exemption(s) in "
              f"{BASELINE.relative_to(ROOT).as_posix()}.")
        print("Extract the shared thing rather than parallelising it. If two")
        print("call sites genuinely need to differ, make the difference a named")
        print("parameter so the divergence is visible in one place.")
        return 1

    print(f"no new duplication - {exemptions} legacy pair(s) exempt")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
