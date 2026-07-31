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
#: **RAISED 780 -> 820 on 2026-07-30, deliberately, and this is the third raise so the
#: reasoning matters more than the number.** The rule is *trim narration, keep contract*
#: — and what pushed past 780 was neither. It was an external benchmark arriving: notes
#: 059-065 in one session, a measured floor, a layout decision on two independent
#: measurements, a per-step decomposition, and a mechanism gain of +0.219. Every line of
#: it is a citation, a figure, or a refutation condition.
#:
#: Five shaving passes were made first, which is the failure this comment exists to
#: prevent next time: **shaving a line at a time to stay under a budget is how a
#: document gets worse without anyone deciding to make it worse.** If a raise is
#: warranted, take it once and say why.
#:
#: What would say the budget is wrong rather than tight: confirmations creeping back in
#: (rule 8 puts those in the log), or process narration about what was learned while
#: building (rule 9). Neither is what is here.
#: **RAISED 820 -> 840 on 2026-07-30, deliberately, and taken in ONE step because the
#: last raise's comment says shaving is how a document gets worse without anyone
#: deciding to.** What needed the room: component 5's `bind` row carried a *stated
#: revival condition* — structured relation vectors — and note 070 met it, so the tree
#: gained a ✅ where it previously had a refusal and a promise.
#:
#: That is the document doing its job rather than accumulating. A revival condition
#: nobody can afford to record the answer to is a refusal that stays refused by budget,
#: which is the failure mode rule 12 names as the most expensive available.
#: **RAISED 840 -> 860 on 2026-07-30, and this is the SECOND raise in one session,
#: which is a smell rather than a routine.** Recorded as such.
#:
#: What forced it: note 071 REFUTES the obvious application of note 070 — structured
#: relation vectors must not enter the address — and rule 8 requires refutations be
#: exhaustive where confirmations are not. Three trims were made first and all three
#: were the document working (process narration removed under rule 9, a confirmation
#: compressed under rule 8). The fourth trim would have been damage, which is the line
#: the previous comment draws.
#:
#: **The limit, stated so the next raise is not automatic: a THIRD raise means the tree
#: has outgrown one file and component 5 should become its own, not that 880 is the
#: right number.** Component 5 now carries the largest share of the document because
#: composition is where the open work is; that is a reason to split it out, not a reason
#: to keep growing a file nobody can read whole — which is the exact failure this
#: checker was built to prevent.
#: **RAISED 860 -> 900 on 2026-07-30, the THIRD raise, and the escalation the previous
#: comment prescribed is CLOSED.** That comment said a third raise means the tree has
#: outgrown one file and component 5 should become its own. **John ruled that out**, and
#: his reason is better than mine was: *"as soon as it's big enough that you have to search
#: through it, you're gonna be missing things"* — and two files means reading one and
#: missing the other, which is the same failure wearing a different hat.
#:
#: So splitting is unavailable and shaving is worse than deciding. What grew the file is
#: nine notes in one session (070-085): C4 tested end to end, concept acquisition validated
#: on an external benchmark, credit assignment closed, and two label-free correctness
#: signals. Every line of it is a citation, a figure or a refutation condition.
#:
#: **Four shaving passes were made first and all four were real compression** — the meta
#: sections 76 -> 45, the beam block 18 -> 13 while carrying a correction, the cliff block
#: 19 -> 14, the newest extensional block 34 -> 27 under rule 9. A fifth would be damage.
#:
#: **And the number is now explicitly a proxy.** The real test is John's: can this be read
#: in one pass. 900 lines is about 11k tokens, which is one pass. If that stops being true
#: the answer is to archive settled components wholesale, not to shave and not to split.
MAX_LINES = 900

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


def rows(text: str) -> list[tuple[str, str, list[str]]]:
    """`(section, option line, the option's OWN continuation)`, in file order.

    Split out of `main` so the extent rule below can be asserted directly. A
    whole-tree run cannot check it: it goes green whenever no row happens to
    need the text it is wrongly reading, which was true on the day the rule was
    written and is exactly why the defect survived.

    **A row's block is its own bullet and nothing below it** — indented
    continuation lines up to the first blank or unindented line, which is what
    markdown means by a bullet. It previously ran on to the next option or
    section, swallowing blank lines and whole following paragraphs, and the
    `Shard the count table` row passed on a citation belonging to text
    underneath it. That row was fixed by hand; this is the class.

    Measured over the tree the day the bound landed: **15 rows had blocks
    reaching past their own bullet and 0 needed the extra text to pass.** So it
    changes no verdict today, which is the point — what it prevents is the
    uncited row added tomorrow directly above a cited one.
    """
    out: list[tuple[str, str, list[str]]] = []
    section: str | None = None
    current: tuple[str, str, list[str]] | None = None
    for line in text.split("\n"):
        component = COMPONENT.match(line) or SUBCOMPONENT.match(line)
        if component:
            current = None
            section = f"{component.group(1)}. {component.group(2)}"
            continue
        if line.startswith("## "):
            current, section = None, None
            continue
        if OPTION.match(line):
            if any(line.startswith(f"- {s}") for s in STATES):
                current = (section or "(no section)", line.strip(), [])
                out.append(current)
            else:
                current = None
            continue
        if current is not None:
            if line.strip() and line.startswith("  "):
                current[2].append(line)
            else:
                current = None
    return out


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

    # `rows` is the ONE parser and owns the extent rule; this pass adds only what
    # is about a LINE rather than about a row -- the verdict lines and the
    # unmarked bullets. Two parsers is how one of them stops honouring the bound.
    option_blocks = rows(text)
    section: str | None = None
    sections_seen: list[str] = []
    verdict_in: set[str] = set()
    counts = {state: 0 for state in STATES}

    for line in lines:
        component = COMPONENT.match(line) or SUBCOMPONENT.match(line)
        if component:
            section = f"{component.group(1)}. {component.group(2)}"
            sections_seen.append(section)
            continue
        if line.startswith("## "):
            section = None
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
            continue
        if option:
            # A top-level bullet with no marker inside a component is an option
            # whose status nobody stated.
            if section is not None and not line.startswith("- **"):
                problems.append(
                    f"bullet in section {section} carries no state marker: "
                    f"{line[:70]}")

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
