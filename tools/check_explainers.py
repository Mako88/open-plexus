"""Every explainer is in the index, and every index row points at a file.

## Why this is a check and not a habit

`docs/explainers/README.md` is the only route into the explainers. On 2026-07-30, adding
one to that index revealed that **sixteen were not in it** -- the entire distribution line
and the two most recent findings, unlisted for weeks. Nobody had noticed, because the
symptom of an unlisted document is that nobody reads it, and that looks exactly like a
document nobody needed.

This is the same defect `tools/check_options.py` catches for option records, and John asked
for it here after seeing the option version pay. Rule 14's argument makes it load-bearing
rather than tidy: **a project its owner cannot follow is a project where nobody can tell it
that it is wrong**, and an explainer nobody can find is an explainer that does not exist.

## What is checked, in both directions

    unlisted     a file in docs/explainers/ that the index does not name. The failure
                 that actually happened, sixteen times
    dangling     an index row pointing at a file that is not there. The failure that
                 happens next, when something is renamed
    numbering    a file whose leading number is claimed by another file

Numbering is checked because there are already TWO series here -- `01`-`31` and `028`
onward -- and a third would make the index ambiguous rather than merely odd. The two that
exist are legal and recorded in the index; the check refuses a `044` beside another `044`,
not a `44` beside an `044`.

What is NOT checked: whether an explainer is any good, whether it is current, or whether
its title matches its content. Those need a reader, and the index existing is what makes a
reader possible.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
EXPLAINERS = ROOT / "docs" / "explainers"
INDEX = EXPLAINERS / "README.md"

#: The index is not itself an explainer.
EXEMPT = {"README.md"}

#: A markdown link into this directory, as the index writes them.
LINK = re.compile(r"\]\((?!https?:)([A-Za-z0-9._-]+\.md)\)")

#: The leading number of a filename, which is what a reader cites.
LEADING = re.compile(r"^(\d+)-")


def disagreements(index_text: str, files: set[str]) -> list[str]:
    """Everything wrong, judged from the index text and the file list alone.

    Pure, so a test can hand it a deliberately broken index and watch the check bite --
    CLAUDE.md rule 10, and the reason it is separated is that the interesting cases are a
    missing row and a dangling row, neither of which is convenient to arrange on disk.
    """
    found: list[str] = []
    listed = set(LINK.findall(index_text))

    for name in sorted(files - listed):
        found.append(
            f"docs/explainers/{name} is not in README.md. An unlisted explainer is one "
            f"nobody finds, which is indistinguishable from one nobody needed -- and "
            f"sixteen sat like that for weeks before anyone looked.")

    for name in sorted(listed - files):
        found.append(
            f"README.md lists {name}, which does not exist. A dangling row reads as "
            f"'that has been explained' when it has not.")

    by_number: dict[str, list[str]] = {}
    for name in sorted(files):
        match = LEADING.match(name)
        if match:
            by_number.setdefault(match.group(1), []).append(name)
    for number, names in sorted(by_number.items()):
        if len(names) > 1:
            found.append(
                f"explainers {names} share the leading number {number}, so a citation to "
                f"it names two documents. Two SERIES are fine -- `31` beside `031` is "
                f"recorded in the index -- but two files at one number are not.")
    return found


def problems() -> list[str]:
    if not EXPLAINERS.exists():
        return []
    if not INDEX.exists():
        return [f"{INDEX.relative_to(ROOT).as_posix()} does not exist, so nothing lists "
                f"the explainers and none of them are reachable."]
    files = {p.name for p in sorted(EXPLAINERS.glob("*.md")) if p.name not in EXEMPT}
    return disagreements(INDEX.read_text(encoding="utf-8"), files)


def main() -> int:
    found = problems()
    if found:
        print("THE EXPLAINER INDEX AND THE DIRECTORY DISAGREE.\n")
        for problem in found:
            print(f"  {problem}\n")
        print("Rule 14: a project its owner cannot follow is a project where nobody can\n"
              "tell it that it is wrong. The index is the only way in.")
        return 1
    count = len([p for p in EXPLAINERS.glob("*.md") if p.name not in EXEMPT])
    print(f"explainers ok - {count} listed, every link resolving, no shared numbers")
    return 0


if __name__ == "__main__":
    sys.exit(main())
