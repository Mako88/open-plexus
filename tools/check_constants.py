"""Every pinned number says where it came from, or the pin is refused.

## The rule this enforces, and why it is a CHECK rather than a fifth warning

`CLAUDE.md`: *"A variable that never changes does not look like a variable — it
looks like the background."* It carries **four** calibrations, and the fourth is
the reason this file exists rather than a fifth one:

  - the projection scale, pinned one step from where the mechanism diverges, and
    silently producing the width curve a headline came from (`g3-02`);
  - an interference account "refuted" by two sweeps that moved the wrong axis
    while `seq_len` sat at 96 in every sweep ever run (`g1-10`);
  - `slots` and `fade` frozen at values chosen for `d_model` 32, then swept at
    node 64 — **and `g9-09` named that as the standing risk in its own file
    before dispatch, which did not prevent `g9-11` repeating it one sweep
    later.** Corrected, it was worth **0.58**, more than twice the largest
    effect any mechanism in that line produced;
  - `d_model` carried from `note 065` into every CLUTRR figure this project has
    published, worth **0.71** between the smallest and largest width tried
    (`g41-01`).

`CLAUDE.md` rule 18 is explicit that a rule which keeps failing should become
something structural, and this repository has done it once already:
`check_commit_messages.py` exists because one rule collected four calibrations
and *"four calibrations of one rule is evidence that more care is not
available"*. This is the same move for the same reason.

## What it can and cannot tell

**It cannot tell whether a value was carried.** Nothing in the source says that.

What it can do is refuse a pin that never says anything about where it came
from — and *"slots 4, from g9-10 at NODE 32"* cannot be written without going to
look. That is `CLAUDE.md`'s own prescription, in its words: *"when a sweep pins a
value taken from an earlier sweep, write down which cell it came from, next to
the pin. A line reading `slots 4, from g9-10 at NODE 32` sitting above `width 64`
is visible in a way that `slots 4, FIXED` is not."*

So a green run does not mean no constant is stale. It means every pin was asked
the question.

## What this does NOT duplicate, and what was searched

Searched by capability — constant, pin, frozen, carried, default, provenance,
baseline, ratchet — across `tools/`, `tests/`, `experiments/` and `openplexus/`.

- **`tools/check_provenance.py`** resolves a citation in a RECORD to a source
  that contains the number. It is about documents. This is about source code,
  and the two never look at the same file.
- **`tools/check_rails.py`** enforces repo conventions per file — PREDICTIONS
  sections, harness use, recovery imports — and owns the baseline-ratchet idea
  this borrows. It does not read expressions. Its baseline is per FILE; this one
  is per PIN, deliberately, so a new unprovenanced constant in an
  already-exempt script still fails.
- **`tools/grid.py`** asks whether a swept axis chose at an edge. That is about
  values that DID vary; this is about the ones that did not.
- **`tools/check_duplication.py`** hashes function bodies. Unrelated.

## The baseline

`tools/constants_baseline.json`, keyed `relative/path.py::NAME`. **It can only
shrink** — an entry naming a pin that now carries provenance is an error, so the
list cannot quietly grow back. A check that fails on everything gets suppressed,
which is why the existing 90% are exempt rather than fixed in one pass.
"""

from __future__ import annotations

import ast
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
BASELINE = ROOT / "tools" / "constants_baseline.json"
SCANNED = ("experiments", "tools")

#: A pin is answered by a CITATION, or by saying outright that the value was
#: chosen for this run. Both require having looked; neither can be written by
#: accident. "carried" and "from gNN-NN" are the forms `CLAUDE.md` asks for.
PROVENANCE = re.compile(
    r"note \d{3}|g\d+-\d+|decision \d+|\d{3} §\d|"
    r"chosen (for|here|at)|swept|carried|this grid|measured (here|at)",
    re.IGNORECASE)


def _numeric(node: ast.expr) -> bool:
    """A numeric literal, or a tuple of them. Strings and paths are not pins."""
    if isinstance(node, ast.Constant):
        return isinstance(node.value, (int, float)) and not isinstance(
            node.value, bool)
    if isinstance(node, ast.Tuple):
        return bool(node.elts) and all(_numeric(e) for e in node.elts)
    return False


def _comment_context(lines: list[str], lineno: int) -> str:
    """The contiguous comment block above a line, plus the line itself.

    Comments rather than docstrings because that is where a pin's note goes —
    `#:` for a documented constant, `#` for a plain one, and a trailing comment
    on the pin itself. All three are read.
    """
    out = [lines[lineno - 1]]
    i = lineno - 2
    while i >= 0 and lines[i].lstrip().startswith("#"):
        out.append(lines[i])
        i -= 1
    return "\n".join(out)


def pins(path: pathlib.Path) -> list[tuple[str, int, str]]:
    """`(name, line, context)` for every pinned number in one file.

    Two shapes, because the failures came in both. `WIDTH = 256` at module level
    is how `g37_02` carried `g14-01`'s width; `default=4` inside `add_argument`
    is how `tools/clutrr_recovery.py` carried `note 065`'s beam width — the exact
    value `g41-01` measured as undertuned.
    """
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    try:
        tree = ast.parse(text)
    except SyntaxError:
        return []

    found: list[tuple[str, int, str]] = []
    for node in tree.body:
        if not isinstance(node, ast.Assign) or not _numeric(node.value):
            continue
        for target in node.targets:
            if isinstance(target, ast.Name) and target.id.isupper():
                found.append((target.id, node.lineno,
                              _comment_context(lines, node.lineno)))

    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        name = getattr(node.func, "attr", None)
        if name != "add_argument":
            continue
        flag = next((a.value for a in node.args
                     if isinstance(a, ast.Constant)
                     and isinstance(a.value, str)), "?")
        for keyword in node.keywords:
            if keyword.arg != "default" or not _numeric(keyword.value):
                continue
            # The whole call, plus any comment block above it: a help string is
            # part of the call and counts, but only if it actually cites
            # something -- "width of the model" does not pass.
            end = getattr(node, "end_lineno", node.lineno)
            context = "\n".join(lines[node.lineno - 1:end])
            found.append((flag, node.lineno,
                          context + "\n" + _comment_context(lines, node.lineno)))
    return found


def main() -> int:
    baseline = set(json.loads(BASELINE.read_text(encoding="utf-8"))
                   if BASELINE.exists() else [])
    unprovenanced: set[str] = set()
    problems: list[str] = []
    total = 0

    for directory in SCANNED:
        for path in sorted((ROOT / directory).glob("*.py")):
            relative = path.relative_to(ROOT).as_posix()
            for name, lineno, context in pins(path):
                total += 1
                if PROVENANCE.search(context):
                    continue
                key = f"{relative}::{name}"
                unprovenanced.add(key)
                if key not in baseline:
                    problems.append(
                        f"{relative}:{lineno} pins `{name}` and says nothing "
                        f"about where the value came from. Write the cell it "
                        f"was chosen in next to it, or say it was chosen for "
                        f"this run.")

    for stale in sorted(baseline - unprovenanced):
        problems.append(
            f"{stale} is exempt in constants_baseline.json and no longer needs "
            f"to be. Remove the entry -- the list only shrinks.")

    if problems:
        print("A PINNED NUMBER DOES NOT SAY WHERE IT CAME FROM.\n")
        for problem in problems:
            print(f"  {problem}")
        print("\nCLAUDE.md: a variable that never changes does not look like a "
              "variable,\nit looks like the background. The rule has four "
              "calibrations and the\nlargest was worth 0.71. This cannot tell "
              "whether a value is stale -- only\nthat nobody asked.")
        return 1

    print(f"constants ok - {total} pin(s) across {len(SCANNED)} directories, "
          f"{len(baseline)} legacy exemption(s), no new unprovenanced pins")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
