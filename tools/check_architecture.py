"""Keep ARCHITECTURE.md factual, mechanically rather than by intention.

The ledger's whole value is that a verdict means something. Three ways that rots,
and this checks all three:

- **a verdict with no evidence.** "PASSING" on the strength of code existing is
  the failure the file's rule 1 exists to stop
- **an invented verdict.** A row reading "mostly works" is not in the vocabulary
  and cannot be counted
- **a summary that has drifted from the table.** This is the one that actually
  happens. STATE.md carried "competing information from different time periods"
  for weeks and every line of it was true when written -- the counts below are
  recomputed from the rows so the prose cannot quietly stop matching them

Run by `tools/check_all.py`, so a ledger that has drifted fails the build the way
a broken test does.
"""

from __future__ import annotations

import pathlib
import re
import sys

LEDGER = pathlib.Path(__file__).resolve().parent.parent / "ARCHITECTURE.md"

VERDICTS = ("PASSING", "PARTIAL", "FAILING", "UNTESTED", "STALE", "CLAIMED")

#: A verdict that asserts something about the world needs a measurement behind
#: it. UNTESTED and STALE are the honest absences and are exempt by definition.
NEEDS_EVIDENCE = ("PASSING", "PARTIAL", "FAILING", "CLAIMED")

#: Enough to name a measurement: a decision number, a note, a task, or a figure.
CITES = re.compile(r"\b(?:\d{2,3}|note \d{3}|g\d+-\d+|0\.\d+|[A-Z]\d)\b")


def rows(text: str) -> list[tuple[str, str, str]]:
    """Every capability row, as `(id, verdict, evidence)`.

    A row is a table line whose first cell is an id like `A1` or `G-C1`. The
    summary tables and the re-check table have no such cell and are skipped
    without being listed as malformed.
    """
    found = []
    for line in text.splitlines():
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 4:
            continue
        if not re.fullmatch(r"[A-Z](?:-[A-Z])?\d+|G-rest", cells[0]):
            continue
        found.append((cells[0], cells[2], cells[3]))
    return found


def main() -> int:
    if not LEDGER.exists():
        print(f"FAIL check_architecture: {LEDGER.name} is missing")
        return 1
    text = LEDGER.read_text(encoding="utf-8")
    found = rows(text)
    problems: list[str] = []

    if len(found) < 10:
        problems.append(
            f"only {len(found)} capability rows parsed, which means the table "
            f"shape changed and this checker is no longer reading it")

    seen: set[str] = set()
    counts: dict[str, int] = {v: 0 for v in VERDICTS}
    for identifier, verdict, evidence in found:
        if identifier in seen:
            problems.append(f"{identifier}: appears twice")
        seen.add(identifier)

        bare = verdict.replace("*", "").strip()
        if bare not in VERDICTS:
            # `G-rest` defers to GOALS §4 on purpose -- the gate table there is
            # the only place a gate verdict is written, and duplicating it is
            # how two documents start disagreeing.
            if "GOALS" in verdict:
                continue
            problems.append(
                f"{identifier}: verdict {verdict!r} is not one of "
                f"{', '.join(VERDICTS)}. An invented verdict cannot be counted "
                f"and cannot be acted on")
            continue
        counts[bare] += 1

        if bare in NEEDS_EVIDENCE and not CITES.search(evidence):
            problems.append(
                f"{identifier}: reads {bare} with no measurement cited. Rule 1 "
                f"-- a verdict needs a number and a decision reference, not an "
                f"argument that the code exists")

    # THE ONE THAT ACTUALLY HAPPENS. Prose drifts from the table it summarises,
    # and every word of it was true when written.
    claimed = re.search(
        r"\*\*(\d+) PASSING, (\d+) PARTIAL, (\d+) FAILING, (\d+) UNTESTED, "
        r"(\d+) CLAIMED", text)
    if not claimed:
        problems.append(
            "the summary line was not found. It is what keeps the prose "
            "honest, so its absence is a failure rather than a style choice")
    else:
        stated = dict(zip(("PASSING", "PARTIAL", "FAILING", "UNTESTED",
                           "CLAIMED"), (int(g) for g in claimed.groups())))
        for verdict, number in stated.items():
            if number != counts[verdict]:
                problems.append(
                    f"summary says {number} {verdict}, the table has "
                    f"{counts[verdict]}")

    if problems:
        print("FAIL check_architecture:")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    total = sum(counts.values())
    print("architecture ok - " + ", ".join(
        f"{counts[v]} {v.lower()}" for v in VERDICTS if counts[v])
        + f" ({total} rows)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
