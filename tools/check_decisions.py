"""Keep DECISIONS.md a decision TREE rather than an archive, mechanically.

## The failure this exists for

`DECISIONS.md` was an append-only log and reached **6,040 lines**. Nothing could
read it whole, so it was read selectively — and on 2026-07-29 that produced three
wrong recommendations in a row, each resting on a claim a later entry had already
superseded. Decision 115 closed saturation; entries 042, 133 and 134 reopened it in
turn. The file's own Index stopped being maintained at entry 134, and entry 132 had
an Index row and no body.

John's diagnosis: *"the intention of decisions was not to be an append-only log.
The intention is to make sure we don't circle back to things that have already been
proven. But for that to be at all useful, it has to be a short bulleted list."*

**A log records; it does not prevent.** This replaces `check_state.py` and
`check_architecture.py`, which enforced the three-document structure that produced
the problem.

## What it checks, and why each one is the mechanical version of a real mistake

    budget        the tree stays under MAX_LINES. The only way to stay under it is
                  to move detail into the archived log, which is the rule the
                  document states about itself

    verdict       every component section carries a ⇒ line saying DECIDED or OPEN.
                  A component with options and no verdict is exactly the state
                  where two readers reach different conclusions

    one marker    every option line carries exactly one of the four states. Two
                  markers, or none, is an option whose status is a matter of
                  interpretation

    evidence      every ✅ and ❌ cites a decision, a sweep, or says in words that
                  it rests on no measurement. **This is the important one.**
                  `check_architecture.py` enforced "a row with no measurement is
                  UNTESTED, never probably fine"; this is that rule for a tree.
                  A ❌ with no citation is a mechanism refused by opinion, and it
                  is how a good idea gets discarded on an invalid measurement --
                  the most expensive error available (rule 12)

    census        the declared marker counts match the actual ones, so the summary
                  cannot drift from the body. `check_architecture.py` caught its
                  own summary drifting the first time a verdict changed, which is
                  the entire argument for this check existing

Raising `MAX_LINES` is allowed and is a decision: do it deliberately, in a commit
that says why, rather than as a side effect of adding a section.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TREE = ROOT / "DECISIONS.md"

# WINDOWS PRINTS THIS FILE'S OWN SUBJECT MATTER IN cp1252 AND CRASHES ON IT.
# The state markers and the arrows in a quoted problem line are outside cp1252, so
# a check whose whole job is to report on them died in `print` rather than
# reporting -- locally only, since CI is UTF-8. A checker that fails differently on
# the two machines it runs on is worse than one that fails.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

#: A forcing function rather than a measurement: detail belongs in the archived log.
#:
#: **RAISED 600 -> 700 on 2026-07-29, deliberately, which is what this docstring
#: says to do.** 600 was set against the tree's first draft at 468 lines, BEFORE the
#: back-fill that put every refutation and retraction from the archived log into it.
#: That back-fill is the document's whole job — a tree that is selective about what
#: was settled AGAINST cannot prevent a re-proposal — so the budget was measuring a
#: draft rather than the artefact.
#:
#: The number moved once, with a reason, in a commit that says so. If it needs
#: raising again, ask first whether confirmations have crept in: those belong in the
#: log, and the coverage rule at the top of the tree says so.
#:
#: **That question was asked at 688 lines and the answer was yes**, so the budget was
#: NOT raised a second time. What had crept in was process narration on the newest
#: ✅ entries — which mutation guards which claim, how a check was made to fail, what
#: was learned while building. All of it true, all of it already in the commit
#: messages and the notes, none of it what a reader consults the tree for. Trimming
#: three entries returned 28 lines. **The pressure worked as designed: it forced a
#: read of the newest writing rather than the oldest.**
#: **RAISED 700 -> 780 on 2026-07-29, deliberately, and for a different reason than
#: the first raise.** John asked for the maintenance contract to be spelled out at the
#: top of the tree rather than left implicit — ten numbered rules, about forty lines.
#: That is not narration and rule 9 does not apply to it: it is the document's own
#: operating instructions, and the budget was set before they existed.
#:
#: The distinction to hold if this comes up again: **trim narration, keep contract.**
#: A raise for prose about what was learned while building is the thing rule 9 refuses;
#: a raise for the rules that keep the document usable is the document working.
MAX_LINES = 780

CHOSEN, REFUTED, UNTRIED, BOTH, PAUSED = "✅", "❌", "⬜", "🔀", "⏸"
STATES = (CHOSEN, REFUTED, UNTRIED, BOTH, PAUSED)

#: A numbered component section. "How to read a row" and the standing agreements
#: are not components and carry no verdict.
COMPONENT = re.compile(r"^## (\d+)\. (.+)$")
#: A sub-component, which may carry its own verdict instead of the parent's.
SUBCOMPONENT = re.compile(r"^### (\d+[a-z])\. (.+)$")
#: An option line: a top-level bullet opening with a state marker.
OPTION = re.compile(r"^- (.+)$")
#: Evidence: a backticked decision number or sweep id, or an explicit admission.
EVIDENCE = re.compile(
    r"`\d{2,3}[^`]*`|`g\d+-\d+`|`\d{3} §\d`|note \d{3}|"
    r"no measurement|reasoned, not measured|refused rather than untried",
    re.IGNORECASE)
#: The declared census, e.g. "CENSUS: 20 chosen, 12 refuted, 18 untried, 9 both".
CENSUS = re.compile(
    r"CENSUS:\s*(\d+) chosen, (\d+) refuted, (\d+) untried, (\d+) both, "
    r"(\d+) paused", re.IGNORECASE)


def main() -> int:
    text = TREE.read_text(encoding="utf-8")
    lines = text.split("\n")
    problems: list[str] = []

    if len(lines) > MAX_LINES:
        problems.append(
            f"DECISIONS.md is {len(lines)} lines, over its {MAX_LINES} budget. "
            f"Move detail into docs/archive/decisions-log-083-171.md, which is "
            f"what it is for. Raising the budget is a decision; make it "
            f"deliberately.")

    # Walk the file once, tracking which section each option belongs to and
    # collecting the block of lines beneath it. Evidence lives in the sub-bullets,
    # not on the option line, so an option is judged on its whole block.
    section: str | None = None
    sections_seen: list[str] = []
    verdict_in: set[str] = set()
    counts = {state: 0 for state in STATES}
    option_blocks: list[tuple[str, str, list[str]]] = []
    current: tuple[str, str, list[str]] | None = None

    for line in lines:
        component = COMPONENT.match(line) or SUBCOMPONENT.match(line)
        if component:
            current = None
            section = f"{component.group(1)}. {component.group(2)}"
            sections_seen.append(section)
            continue
        if line.startswith("## "):
            current, section = None, None
            continue
        if "⇒" in line and section is not None:
            verdict_in.add(section)
        option = OPTION.match(line)
        if option and any(line.startswith(f"- {s}") for s in STATES):
            marks = [s for s in STATES if s in option.group(1)[:4]]
            if len(marks) != 1:
                problems.append(
                    f"option carries {len(marks)} state markers, expected 1: "
                    f"{line[:70]}")
            else:
                counts[marks[0]] += 1
            current = (section or "(no section)", line.strip(), [])
            option_blocks.append(current)
            continue
        if option:
            # A top-level bullet with no marker inside a component is an option
            # whose status nobody stated.
            if section is not None and not line.startswith("- **"):
                problems.append(
                    f"bullet in section {section} carries no state marker: "
                    f"{line[:70]}")
            current = None
            continue
        if current is not None:
            current[2].append(line)

    for name, title, body in option_blocks:
        if not (title.startswith(f"- {CHOSEN}") or title.startswith(f"- {REFUTED}")):
            continue
        block = title + "\n" + "\n".join(body)
        if not EVIDENCE.search(block):
            problems.append(
                f"in {name}, a {'chosen' if CHOSEN in title[:4] else 'refuted'} "
                f"option cites no decision, sweep or note, and does not say it "
                f"rests on no measurement: {title[:70]}")

    for name in sections_seen:
        parent = name.split(".")[0]
        if name not in verdict_in and not any(
                other.startswith(parent) and other in verdict_in
                for other in sections_seen):
            problems.append(
                f"component {name} has no ⇒ verdict line. A component with "
                f"options and no verdict is where two readers disagree.")

    declared = CENSUS.search(text)
    if not declared:
        problems.append(
            "no CENSUS line. The declared counts are what stops the summary "
            "drifting from the body, which is the mistake check_architecture.py "
            "caught the first time a verdict changed.")
    else:
        want = tuple(int(n) for n in declared.groups())
        got = (counts[CHOSEN], counts[REFUTED], counts[UNTRIED], counts[BOTH],
               counts[PAUSED])
        if want != got:
            problems.append(
                f"CENSUS says {want} chosen/refuted/untried/both/paused, the "
                f"tree has {got}.")

    for problem in problems:
        print(f"FAIL check_decisions: {problem}")
    if problems:
        return 1
    print(f"decisions ok - {len(lines)}/{MAX_LINES} lines, "
          f"{counts[CHOSEN]} chosen, {counts[REFUTED]} refuted, "
          f"{counts[UNTRIED]} untried, {counts[BOTH]} both, "
          f"{counts[PAUSED]} paused, every state cited")
    return 0


if __name__ == "__main__":
    sys.exit(main())
