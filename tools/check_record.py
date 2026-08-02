"""The record stays short, in the dimension it is actually growing in.

## Why line count was the wrong thing to watch

The predecessor reached 6,040 lines by APPENDING, and the restructure's answer
was one file of about 150 lines carrying one line per option. That worked: the
README has gone from 133 lines to 140 in a week of heavy work.

**And it has still grown, in the direction nobody was counting.** One option line
reached 1,263 characters. Eight passed 400. A 1,263-character line is a
paragraph wearing a line's clothing, and the failure it produces is the same one:
the file stops being readable in one pass, so it gets read selectively, which is
how three wrong recommendations came out of one day.

So the bound here is on the LINE, not on the file.

## What the bound is, and why a measurement is not an excuse to exceed it

An option line has a job: name the option, say what killed it or what it buys,
and say what would revive it. Numbers belong in it — a refutation without its
figure is an opinion — but a line carrying four measurements is carrying a
results table, and `CLAUDE.md` is explicit that the measurement lives with the
run and the README carries the claim.

When a line is too long the fix is almost never to delete the evidence. It is
that the line is doing two jobs and wants to be two options, or that three
figures are making one point and one of them is the point.

## What this does NOT duplicate, and what was searched

Searched by capability — record, length, README, bloat, ratchet — across
`tools/`, `tests/` and `.github/workflows/`.

- **`tools/check_constants.py`** asks whether a number in SOURCE says where it
  came from. This asks whether a number in the RECORD has crowded out the claim.
- **`tools/check_orphans.py`** keeps the code honest about what is wired. This
  keeps the file that describes it honest about being readable.
"""

from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

#: Longest an option line may be. **Chosen here at roughly four printed lines**,
#: which is the length at which a reader stops taking the whole thing in at
#: once. The eight lines over it when this was written were carrying results
#: tables; the worst was 1,263 characters.
LIMIT = 420

#: Files whose bullet lines are bounded, and what each is for.
WATCHED = {
    "README.md": "one option per line: what it is, what killed it, what revives it",
    "NOW.md": "what is unfinished. A finding SPLITS: the claim goes to the README, the numbers stay with the run in out/",
}

#: A whole-file bound, so the two failures cannot trade places. The README was
#: 133 lines at the restructure and NOW.md is rewritten rather than appended to.
LINES = {"README.md": 200, "NOW.md": 150}


def offenders(path: pathlib.Path) -> list[tuple[int, int, str]]:
    """`(line number, length, opening)` for every over-long bullet."""
    found = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if line.lstrip().startswith(("- ", "* ")) and len(line) > LIMIT:
            # ECHOED AS ASCII. The record is full of status marks a Windows
            # console cannot encode, and a checker that dies printing its own
            # finding is worse than no checker -- `check_experiments.py` caught
            # three scripts with exactly this fault the day it was written.
            opening = line.strip()[:70].encode("ascii", "replace").decode()
            found.append((number, len(line), opening))
    return found


def approved_but_not_built() -> str | None:
    """`NOW.md` must account for every 🚧 in the README, and say how many.

    **Size was checked and currency was not**, which is how an option stayed
    marked approved-but-not-built for hours after it was built, measured and
    committed. `NOW.md` declares the invariant in its own header — *every 🚧 in
    the README appears here* — and nothing enforced it, so the one thing that
    exists to stop work going quiet went quiet itself.

    Counting rather than matching text is deliberate. A fuzzy match on the
    option's wording would drift as the wording does, and would fail silently
    in whichever direction was least noticed. A count cannot: adding a 🚧 or
    resolving one breaks this until somebody looks at both files, which is the
    entire point.
    """
    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    now = ROOT / "NOW.md"
    if not now.exists():
        return None
    marks = sum(1 for line in readme.splitlines() if line.startswith("- 🚧"))
    told = now.read_text(encoding="utf-8")
    if f"{marks} 🚧" in told or (marks == 0 and "no 🚧" in told.lower()):
        return None
    return (f"README has {marks} 🚧 and NOW.md does not say so. Write "
            f"'{marks} 🚧' there with each one named, or resolve the ones "
            f"that are built — an option that is DONE and still reads as "
            f"PENDING is what the invariant exists to catch")


def main() -> int:
    problems = []
    stale = approved_but_not_built()
    if stale:
        problems.append(stale)
    for name, purpose in WATCHED.items():
        path = ROOT / name
        if not path.exists():
            continue
        total = len(path.read_text(encoding="utf-8").splitlines())
        if total > LINES[name]:
            problems.append(f"{name} is {total} lines, over {LINES[name]} — "
                            f"{purpose}")
        for number, length, opening in offenders(path):
            problems.append(f"{name}:{number} is {length} characters, over "
                            f"{LIMIT}\n      {opening}...")

    if problems:
        print("THE RECORD IS GROWING IN A DIRECTION NOBODY IS COUNTING.\n")
        for problem in problems:
            print(f"  {problem}")
        print("\nA line this long is a paragraph wearing a line's clothing, and\n"
              "the file stops being readable in one pass -- which is how it\n"
              "starts being read selectively.\n"
              "\nThe fix is rarely to delete the evidence. Usually the line is\n"
              "doing two jobs and wants to be two options, or three figures are\n"
              "making one point and one of them is the point. The measurement\n"
              "lives with the run; the record carries the claim.")
        return 1

    print(f"record ok - {len(WATCHED)} file(s), no bullet over {LIMIT} characters")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
