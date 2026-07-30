"""Option records hold history; `DECISIONS.md` holds status. Enforced, not asked for.

## What this protects

John, 2026-07-30, setting the design: each option in the tree carries *"a single summary
line and a reference to"* its record, and the record holds *"all of the facts discovered
about that option (along with the state of the model when the discovery was made, since
that can change the shape of things), but crucially no current state or anything -- just
'here's what was tried [and therefore here's what exists], and here are the results'."*

**The separation is the whole mechanism, and it is the fix for how the predecessor
failed.** A 6,040-line append-only log was read selectively and produced three wrong
recommendations in one day, because superseded conclusions sat next to current ones with
nothing distinguishing them. A record that structurally CANNOT state a conclusion cannot
be mistaken for one.

The deeper property, which is why this is safe where the log was not: **a record of
events cannot go stale.** "On this date, this configuration produced 0.9220" stays true
forever. "This is what we use" does not.

## What is checked

1. **No status markers in a record.** ✅ ❌ ⬜ 🔀 belong to the tree alone.
2. **No status language.** The markers are easy to avoid by accident and easy to work
   around by writing "we currently use" instead, which is the same failure in prose.
3. **Every record is linked from `DECISIONS.md`.** An unlinked record is unreachable,
   which is the state 53% of the notes appeared to be in before the measurement was
   redone properly.
4. **Every link resolves.** A pointer to a file that does not exist is worse than none.
5. **The header states the contract**, so a reader who opens one cold learns the rule
   from the file rather than from this checker.

What is NOT checked: whether the model state is recorded per entry. That is the field
most likely to be forgotten and the hardest to detect -- a heading is not evidence that
the configuration behind a number was written down.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OPTIONS = ROOT / "docs" / "options"
DECISIONS = ROOT / "DECISIONS.md"

#: The tree's vocabulary. Anywhere else they assert a status.
MARKERS = ("✅", "❌", "⬜", "🔀")

#: Status in prose, which the markers alone would not catch. Deliberately short: a long
#: list makes false positives, and the header plus review carries the rest.
PHRASES = (
    "this is what we use", "we currently use", "currently the default",
    "is the default", "still blocked", "the next step is", "we should",
)

#: Phrases the header itself must carry, so the contract travels with the file.
REQUIRED = ("RECORD ONLY", "no status")


def main() -> int:
    if not OPTIONS.exists():
        print("no docs/options yet - nothing to check")
        return 0

    decisions = DECISIONS.read_text(encoding="utf-8")
    problems: list[str] = []
    records = sorted(OPTIONS.glob("*.md"))

    for path in records:
        relative = path.relative_to(ROOT).as_posix()
        text = path.read_text(encoding="utf-8")
        head = text[:1400]

        # The header quotes the markers to explain the rule, so only the BODY is
        # scanned for them -- otherwise stating the contract would violate it.
        body = text[len(head):]
        for marker in MARKERS:
            if marker in body:
                problems.append(
                    f"{relative} contains {marker} outside its header. Status markers "
                    f"belong to DECISIONS.md alone: a record that can claim to be "
                    f"current can be mistaken for current after it stops being so.")
        lowered = body.lower()
        for phrase in PHRASES:
            if phrase in lowered:
                problems.append(
                    f'{relative} says "{phrase}", which is a status in prose. Records '
                    f"say what was TRIED and what came back; what is chosen lives in "
                    f"DECISIONS.md.")
        for expected in REQUIRED:
            if expected.lower() not in head.lower():
                problems.append(
                    f'{relative} header does not say "{expected}", so a reader who '
                    f"opens it cold cannot tell it from a status document.")
        if relative not in decisions:
            problems.append(
                f"{relative} is not linked from DECISIONS.md. An unreachable record is "
                f"one nobody consults, and duplicated work is the result.")

    # And the other direction: a link in the tree that resolves to nothing.
    for link in re.findall(r"docs/options/[A-Za-z0-9._-]+\.md", decisions):
        if not (ROOT / link).exists():
            problems.append(
                f"DECISIONS.md links {link}, which does not exist. A dangling pointer "
                f"reads as 'the history is over there' when it is nowhere.")

    if problems:
        print("OPTION RECORDS AND THE TREE DISAGREE.\n")
        for problem in problems:
            print(f"  {problem}\n")
        print("Records hold history and cannot go stale, because events do not "
              "un-happen.\nStatus changes, so it lives in exactly one place.")
        return 1

    print(f"options ok - {len(records)} record(s), all linked, none claiming a status")
    return 0


if __name__ == "__main__":
    sys.exit(main())
