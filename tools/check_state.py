"""Keep STATE.md a statement of state rather than an archive.

## The failure this exists for

STATE.md's own header has always said *"when something here is settled it leaves,
and an entry goes in DECISIONS.md"*. Nothing enforced it, so by 2026-07-29 the
file was **1547 lines** and carried three mutually uninformative things at once:

- an instrument table saying the text line was **closed**,
- a `START HERE` section deep inside word-level **text**,
- an "In flight" section still describing **decision 119**, ten sweeps stale.

Each was true when written. Together they said nothing, and decisions 135-142
were measured down a line the document had already marked closed. John named the
cause: *"competing information in there from different time periods"*.

**A document that is only kept current by intention drifts at exactly the speed
nobody notices.** This is rule 18 applied to a document: prefer a rule that makes
the mistake structurally impossible over one that asks for more care.

## What it checks

    budget          STATE.md stays under MAX_LINES. Not because length is bad,
                    but because the only way to stay under it is to move settled
                    work out -- which is the rule the header already states.

    one question    exactly one section may declare the live question. Two
                    sections asserting different live questions is precisely the
                    drift, and it is mechanically detectable where "is this
                    stale?" is not.

Raising `MAX_LINES` is allowed and is a decision: do it deliberately, in a commit
that says why, rather than as a side effect of adding a section.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STATE = ROOT / "STATE.md"

#: Generous against the pruned file (239 lines on 2026-07-29) and a third of what
#: the drift produced. The number is a forcing function, not a measurement.
MAX_LINES = 400

#: The marker a section uses to declare the live question. Exactly one.
QUESTION = re.compile(r"^#+ .*THE QUESTION RIGHT NOW", re.MULTILINE)

#: Headings that assert live work. More than one of these plus a question marker
#: is how two sections come to disagree, so they are counted and reported rather
#: than forbidden -- some are legitimate.
START_HERE = re.compile(r"^#+ .*(START HERE|NEXT STEP|⇒⇒)", re.MULTILINE)


def main() -> int:
    text = STATE.read_text(encoding="utf-8")
    lines = text.count("\n") + 1
    problems: list[str] = []

    if lines > MAX_LINES:
        problems.append(
            f"STATE.md is {lines} lines, over its {MAX_LINES} budget. Move "
            f"settled work to DECISIONS.md or docs/archive/ -- that is the rule "
            f"in its own header. Raising the budget is allowed and is a "
            f"decision; make it deliberately.")

    questions = QUESTION.findall(text)
    if len(questions) != 1:
        problems.append(
            f"STATE.md declares {len(questions)} live questions, expected "
            f"exactly 1. Two sections asserting different live questions is the "
            f"drift that produced decisions 135-142.")

    starts = START_HERE.findall(text)
    if len(starts) > 1:
        problems.append(
            f"{len(starts)} headings claim to be the starting point. A reader "
            f"who picks the wrong one is reading a stale plan.")

    for problem in problems:
        print(f"FAIL check_state: {problem}")
    if problems:
        return 1
    print(f"state ok - {lines}/{MAX_LINES} lines, one live question")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
