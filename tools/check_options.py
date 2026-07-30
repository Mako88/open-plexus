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
6. **Every entry carries a CONFIG block with all seven keys.** John's request,
   2026-07-30: *"a standard config block format ... to show at-a-glance what
   configuration of the model was in use during each experiment (in case that
   invalidates the result later)"*, and then, the same day: *"any numbers that are cited
   should also cite the location of the script/test that was used to get the number."*

   `source` is where the number is WRITTEN DOWN and `script` is what PRODUCED it, and
   they are different questions. Note 105 is the case that separates them: a real
   measurement, reproducible to four decimal places, cited by two notes to sources that
   did not contain it. The source field would have been wrong in both. The script field
   would have re-run in seventy seconds.

## Why 6 is a check and not a convention

The original version of this file closed by saying the model state per entry was the
field *"most likely to be forgotten and the hardest to detect -- a heading is not
evidence that the configuration behind a number was written down."* That is an accurate
description of a rule nobody can enforce, which CLAUDE.md rule 18 says to turn into a
check or admit is unenforceable.

A fixed-key block makes it detectable, and the `unrecorded` convention is what does the
work:
**a field that cannot be recovered is written `unrecorded` rather than dropped.** An
absent line reads as "not applicable"; `unrecorded` reads as "nobody wrote it down". The
checker cannot tell a true config from a plausible one, but it can tell a stated one from
a silent one, and every regime error this project has made was a silence.

`docs/options/README.md` is the format, and is exempt from the record checks -- it quotes
them, which is the same exemption the header already gets.
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

#: The six fields of a CONFIG block, in the order README.md prints them. `unrecorded` is
#: a legal value for any of them and an omission is not: silence is what misleads.
CONFIG_KEYS = ("when", "source", "script", "task", "model", "knobs", "scale")

#: The format spec, which quotes the rules and so cannot obey them.
EXEMPT = {"README.md"}


def split_header(text: str) -> tuple[str, str]:
    """The header blockquote, and everything after it.

    The header has to be excluded from the status checks because it QUOTES them -- it
    names the four markers in order to say they do not belong below. That exclusion was
    first written as `text[:1400]`, a byte count, and the count is what made it wrong:
    **every record shorter than 1400 characters had an empty body and was exempt from
    the marker and prose checks entirely.** It passed because the two records that
    existed were long. Found by a test that fed the checker a short record.

    So the boundary is structural: the header is the leading `>` blockquote, and a record
    with no blockquote has no header and is checked whole.
    """
    lines = text.splitlines(keepends=True)
    end = 0
    for i, line in enumerate(lines):
        if line.startswith(">"):
            end = i + 1
        elif end:
            break
    return "".join(lines[:end]), "".join(lines[end:])


def config_blocks(text: str) -> list[tuple[str, str]]:
    """Every `###` entry in a record, paired with the body up to the next heading.

    Split on `###` rather than on `##` because "What exists" is a `##` section listing
    files, not an experiment, and a file list has no configuration to state.
    """
    entries: list[tuple[str, str]] = []
    heading = None
    body: list[str] = []
    for line in text.splitlines():
        if line.startswith("#"):
            if heading is not None:
                entries.append((heading, "\n".join(body)))
                heading, body = None, []
            if line.startswith("### "):
                heading = line[4:].strip()
        elif heading is not None:
            body.append(line)
    if heading is not None:
        entries.append((heading, "\n".join(body)))
    return entries


def record_problems(relative: str, text: str) -> list[str]:
    """Everything wrong with one record, judged from its text alone.

    Separated from `main` so a test can feed it a deliberately broken record and see
    the check bite -- CLAUDE.md rule 10. Reading from disk is the one thing this cannot
    do, which is why linkage is checked by the caller.
    """
    problems: list[str] = []
    head, body = split_header(text)
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

    # Records cross-link each other -- a refutation points at what replaced it, a
    # ceiling points at what closed it. `check_options` already checks the tree's links
    # into this directory; nothing checked the links WITHIN it, and during the migration
    # three records referenced a sibling that did not exist yet. All three were written
    # before their target and all three happened to get written -- which is luck, and the
    # failure mode is a reader following a link to nothing and concluding the history is
    # elsewhere when it is nowhere.
    # `docs/options/x.md` written from INSIDE docs/options resolves to
    # docs/options/docs/options/x.md, so a repo-root path is broken here even though it
    # looks right. The first version of this check used a slash-free pattern and skipped
    # exactly that case, which is the one instance of it in the directory -- a check
    # whose pattern excludes the broken form finds nothing and reports success.
    for target in re.findall(r"\]\((?!https?:|\.\./)([A-Za-z0-9._/-]+\.md)\)", body):
        if not (OPTIONS / target).exists():
            problems.append(
                f"{relative} links {target}, which is not a record in this directory. "
                f"A dangling cross-link reads as 'the rest of the story is over there' "
                f"when there is no there. Sibling records are linked by BARE FILENAME: "
                f"a `docs/options/` prefix is relative to this directory, not the repo.")

    for heading, entry in config_blocks(text):
        if not re.search(r"^\s+CONFIG\s", entry, re.MULTILINE):
            problems.append(
                f'{relative}: entry "{heading}" has no CONFIG block. A number is a '
                f"claim about a configuration; without one the reader cannot tell "
                f"what a later change invalidates.")
            continue
        # `when` sits on the CONFIG line itself in the README's layout, so the
        # prefix is optional rather than a second accepted format.
        missing = [k for k in CONFIG_KEYS
                   if not re.search(rf"^\s+(?:CONFIG\s+)?{k}\s+\S", entry,
                                    re.MULTILINE)]
        if missing:
            problems.append(
                f'{relative}: entry "{heading}" omits {", ".join(missing)} from its '
                f"CONFIG block. Write `unrecorded` instead of dropping the line -- "
                f"an absent field reads as 'not applicable' and a silence is what "
                f"lets a number be quoted into a regime it was never taken in.")
    return problems


def main() -> int:
    if not OPTIONS.exists():
        print("no docs/options yet - nothing to check")
        return 0

    decisions = DECISIONS.read_text(encoding="utf-8")
    problems: list[str] = []
    records = [p for p in sorted(OPTIONS.glob("*.md")) if p.name not in EXEMPT]
    entry_count = 0

    for path in records:
        relative = path.relative_to(ROOT).as_posix()
        text = path.read_text(encoding="utf-8")
        entry_count += len(config_blocks(text))
        problems.extend(record_problems(relative, text))
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

    print(f"options ok - {len(records)} record(s), {entry_count} entries, all linked, "
          f"every entry carrying a config, none claiming a status")
    return 0


if __name__ == "__main__":
    sys.exit(main())
