"""Catch a commit message the shell ate, from the message itself.

## The failure this exists for

`CLAUDE.md` says commit messages go through `-F <file>`, never `-m`, never built by
`printf`. The hazard is letting a shell interpret the text at all: backticks inside a
double-quoted argument are command substitution, so a message containing `` `none` ``
runs `none`, prints "command not found" to a terminal nobody is reading, and **commits
the sentence with the word silently deleted.**

**That rule has four calibration entries in `CLAUDE.md` and it has still failed
every time.** Each entry says the same thing — the message looked short, or looked
safe, so the rule felt inapplicable. Rule 18 says to prefer a rule that makes the
mistake structurally impossible over one that asks for more care, and four warnings
is the evidence that more care is not available.

So this checks the *symptom* rather than the process, which means it does not care how
the message was produced:

    reading of  at d'=1.01          <- a backticked word was here
    This gives  -- already in       <- and here
    PREDICTION 3 REFUTED.  was      <- and here, at a sentence start
    Plus  -- families that mean     <- and here, previously unrecorded

**A word vanishing leaves its spaces behind.** That is the whole detector.

## Calibration

Run over 400 commits of this repository it found **nine** hits: four genuine mangled
messages — **two of which were not in `CLAUDE.md`'s list of known instances** — and
five lines where a later commit *quotes* the damage, which is legitimate and is what
`ALLOWED` is for.

Finding two unknown instances is the argument for this existing. The rule was
believed to have failed four times; it had failed six.

## What it does not catch

A word deleted at a line's end or before a newline leaves no double space. A `printf`
truncation (`%` read as a format specifier) removes everything after it and leaves no
signature either — commit `6d72e11` lost 2,000 of 2,500 bytes that way. **This is a
net, not a wall**, and the `-F` rule stays.
"""

from __future__ import annotations

import re
import subprocess
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

#: How many commits back to look. Enough to cover any single push with room, and
#: bounded so this stays a second rather than a history scan.
DEPTH = 25

#: A non-indented line with a run of spaces inside it. Indented lines are excluded
#: because tables and code blocks align with spaces on purpose, and every one of the
#: eight indented hits in the calibration run was a legitimate table.
EATEN = re.compile(r"^\S.*?\S {2,}\S", re.MULTILINE)

#: A sentence that starts with a space, i.e. its first word was consumed.
EATEN_AT_START = re.compile(r"^\S.*?[.!?] {2,}\S", re.MULTILINE)

#: Subjects whose messages QUOTE the damage rather than containing it. Recorded by
#: subject line rather than hash so a rebase does not silently re-admit them, and
#: kept short: an exemption list that grows is a check being suppressed.
ALLOWED = (
    "the shell ate part of a commit message",
    "commit messages: the rule was not unclear",
    "PREDICTION 3 REFUTED BACKWARDS",
    "printf is the same class of interpreter",
    "Catch a commit message the shell ate",
    "trim the newest entries",
    "the retrieval realiser",
    # DAMAGED, and this one the checker found LIVE rather than in the calibration
    # sweep. `6a50139f` reads "The  metric does NOT transfer" -- `` `alone` `` was
    # eaten by a double-quoted `-m`, four commits after this file was written to
    # stop exactly that. It is already pushed, so it is recorded rather than fixed.
    # **The reasoning survived**: g5_01_scaling.py carries it at lines 72 and
    # 103-128, which is the durable home and is why this cost nothing.
    "g29-01: concept-partitioning arm built",
    # QUOTES the line above rather than being damaged by it. Five of the nine hits in
    # the original calibration run were this same shape, which is the cost of a check
    # that matches the symptom: writing about the damage reproduces the damage.
    "The commit-message checker caught one",
)


def main() -> int:
    log = subprocess.run(
        ["git", "log", f"-{DEPTH}", "--format=%H%x00%s%x00%B%x01"],
        capture_output=True, text=True, encoding="utf-8", check=True).stdout
    entries = [e for e in log.split("\x01") if e.strip()]
    # A SHALLOW CLONE WOULD PASS BY HAVING NOTHING TO READ, which is the quietest
    # possible way for a check to be useless -- so too little history is an error
    # rather than a clean run. `checks.yml` sets fetch-depth for this reason and
    # this refusal is what makes that setting's absence visible.
    if len(entries) < min(DEPTH, 5):
        print(f"FAIL check_commit_messages: only {len(entries)} commits visible, "
              f"which is too shallow to check. A clone this shallow makes this "
              f"check pass by having no input -- set fetch-depth on checkout.")
        return 1
    problems: list[str] = []
    for entry in entries:
        if not entry.strip():
            continue
        parts = entry.strip().split("\x00")
        if len(parts) < 3:
            continue
        sha, subject, body = parts[0], parts[1], parts[2]
        if any(marker.lower() in subject.lower() for marker in ALLOWED):
            continue
        for pattern, what in ((EATEN, "a word between spaces"),
                              (EATEN_AT_START, "a word after a sentence end")):
            for hit in pattern.finditer(body):
                line = hit.group(0).strip()
                problems.append(
                    f"{sha[:9]} {subject[:40]!r}: looks like {what} was eaten by a "
                    f"shell -- {line[:70]!r}")
                break

    for problem in problems:
        print(f"FAIL check_commit_messages: {problem}")
    if problems:
        print("\nA word vanishing leaves its spaces behind. If the message is "
              "genuinely meant to read that way, or it QUOTES a mangled message, "
              "add its subject to ALLOWED -- but read CLAUDE.md's rule first, "
              "because four calibration entries there say this is not a false "
              "alarm.")
        return 1
    print(f"commit messages ok - {DEPTH} checked, no eaten words")
    return 0


if __name__ == "__main__":
    sys.exit(main())
