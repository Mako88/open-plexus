"""Every measurement in an option record must appear in a source that entry cites.

## What this protects, and why it was built before the migration rather than after

Moving 86 options out of `DECISIONS.md` into `docs/options/` means moving roughly 13,500
lines of measurement text in one pass. **That is where a retyped number enters a document
nobody re-derives** -- and a wrong figure in a record is worse than a wrong figure in a
chat message, because the record is what the next session will trust.

The incremental alternative does not remove that risk. It spreads the same retyping over
weeks in doses too small for anyone to check, which is how `4.540 bits, unigram BEATEN`
survived for weeks as this project's headline text result while being an offline backprop
probe on frozen features (decision 118). **An unsourced number outranks every measurement
downstream of it**, and nothing about it looks wrong.

So this is CLAUDE.md rule 18's move: prefer a check that makes the mistake structurally
hard over a rule asking for more care.

## What is checked

For every `###` entry in every record: each **measurement-shaped** numeral in the entry's
prose must appear verbatim in at least one file the entry's `source` field names.

Measurement-shaped means a decimal (`0.9220`), a percentage (`98.4%`) or a
thousands-grouped integer (`1,146`). Bare small integers are excluded deliberately -- they
are overwhelmingly configuration and counts, they are already stated in the CONFIG block,
and including them produces enough noise to get the whole check switched off.

The CONFIG block itself is excluded from the scan for the same reason: it says what the
configuration WAS, and the configuration is not a result quoted from anywhere.

## What it cannot do

It cannot tell whether a number is attached to the right claim, only whether it exists in
the source. A figure transposed between two rows of the same table passes. It is a net,
not a wall -- the same status `tools/check_duplication.py` has, and worth stating so
nobody reads a green run as verification.

It also cannot check a number this project DERIVED while writing the record -- a
difference between two source figures, say. Those go in `provenance_baseline.json`, where
an exemption is visible and can only be removed. A silent allowance would defeat the tool.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from tools.check_options import EXEMPT, config_blocks  # noqa: E402
from tools.check_rails import compare, read_baseline  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
OPTIONS = ROOT / "docs" / "options"
SWEEPS = ROOT / "experiments" / "sweeps"
ARCHIVE = ROOT / "docs" / "archive"
BASELINE = ROOT / "tools" / "provenance_baseline.json"

#: Where a note may live, in order. A note is findable wherever it sits, so archiving
#: the directory does not break the 85 records that cite one -- which is the property
#: that made archiving them affordable at all. Live first, so a note that is somehow in
#: both wins from the live tree rather than from a copy nobody is maintaining.
NOTE_DIRS = (ROOT / "docs" / "notes", ARCHIVE / "notes")

#: The archived log, split in two, and both are searched for a decision entry.
LOGS = (ARCHIVE / "decisions-001-082.md", ARCHIVE / "decisions-log-083-171.md")

#: A decimal, a percentage, or a thousands-grouped integer. See the docstring for why
#: bare integers are not here.
MEASUREMENT = re.compile(r"\d+\.\d+%?|\d+%|\d{1,3}(?:,\d{3})+")

#: Words that switch what a following number means. Anything else is not a citation.
KEYWORDS = {"note": "note", "notes": "note",
            "decision": "decision", "decisions": "decision",
            "entry": "decision", "entries": "decision"}


def entry_sources(config: str) -> list[Path]:
    """The files an entry's `source` field names, in the order they are named.

    Deliberately literal: a citation this cannot resolve contributes no source, so the
    entry's numbers go unmatched and the entry fails. Silently treating an unparsed
    citation as "everything is fine" is the failure this tool exists to prevent.
    """
    line = re.search(r"^\s+source\s+(.+)$", config, re.MULTILINE)
    if not line:
        return []
    found: list[Path] = []
    kind = None
    for token in re.split(r"[\s,;]+", line.group(1)):
        raw = token.strip("`'\"()[[]].")
        bare = raw.lower()
        if bare in KEYWORDS:
            kind = KEYWORDS[bare]
            continue
        if re.fullmatch(r"g\d+-\d+", bare):
            found.extend(sorted(SWEEPS.glob(f"{bare}-*.txt")))
            continue
        if "/" in raw and path_exists_exactly(ROOT / raw):
            found.append(ROOT / raw)
            continue
        # `notes 093-101` is a range and means nine notes. Written with an en dash as
        # often as a hyphen, and the first version of this resolver silently matched
        # neither -- which read as "these numbers have no source" for an entry whose
        # sources were fine. A citation form the parser drops is a false alarm, and a
        # checker that cries wolf is one that gets switched off.
        span = re.fullmatch(r"(\d{1,3})[-–—](\d{1,3})", bare)
        numbers = ([str(n) for n in range(int(span.group(1)), int(span.group(2)) + 1)]
                   if span else [bare] if re.fullmatch(r"\d{1,3}", bare) else [])
        for number in numbers:
            if kind == "note":
                found.extend(note_files(int(number)))
            elif kind == "decision":
                found.extend(p for p in (log_entry(int(number)),) if p is not None)
    return found


def path_exists_exactly(candidate: Path) -> bool:
    """`candidate.exists()`, but case-exact on Windows as well as on Linux.

    **This is the bug that made the checker weaker on the machine it is run on
    than in the machine that gates the commit**, which is the worst direction for
    a check to be wrong in.

    The resolver lowercased every citation token before testing it, so
    `source docs/SCALE.md` was looked up as `docs/scale.md`. NTFS resolves that
    and ext4 does not: the check passed locally, went green, and CI failed on
    `external-persistent-store.md` and `hop-accumulate.md` with *"a number is not
    in the source its entry cites"* — for two entries whose citations were
    correct all along.

    `Path.exists()` alone cannot fix it, because it is the case-insensitive call.
    Comparing against the parent's real directory listing can, because a listing
    reports the name as stored on both platforms.

    The lowercasing itself is still wanted for keywords and `g\\d+-\\d+` sweep
    ids, so the fix is to keep the raw token for paths rather than to stop
    lowercasing.
    """
    try:
        return (candidate.exists()
                and candidate.name in {p.name for p in candidate.parent.iterdir()})
    except OSError:
        return False


def note_files(number: int) -> list[Path]:
    """A note, wherever it lives. Returns from the FIRST directory that has it."""
    for directory in NOTE_DIRS:
        if directory.exists():
            hits = sorted(directory.glob(f"{number:03d}-*.md"))
            if hits:
                return hits
    return []


#: Decision entries are regions of one file, not files. They are written to a cache so
#: the resolver can hand back paths uniformly, and so a wrong entry number is visible on
#: disk rather than being an empty string nobody sees.
_ENTRY_CACHE: dict[int, Path | None] = {}
_ENTRY_TEXT: dict[Path, str] = {}


def log_entry(number: int) -> Path | None:
    """The archived log region for one decision entry, as a pseudo-path with its text."""
    if number in _ENTRY_CACHE:
        return _ENTRY_CACHE[number]
    for log in LOGS:
        if not log.exists():
            continue
        text = log.read_text(encoding="utf-8")
        match = re.search(rf"^## {number}\. .*?(?=^## \d+\. |\Z)", text,
                          re.MULTILINE | re.DOTALL)
        if match:
            handle = log.with_name(f"{log.name}#{number}")
            _ENTRY_TEXT[handle] = match.group(0)
            _ENTRY_CACHE[number] = handle
            return handle
    _ENTRY_CACHE[number] = None
    return None


def source_text(paths: list[Path]) -> str:
    parts = []
    for path in paths:
        if path in _ENTRY_TEXT:
            parts.append(_ENTRY_TEXT[path])
        elif path.exists():
            parts.append(path.read_text(encoding="utf-8"))
    return "\n".join(parts)


def strip_config(entry: str) -> str:
    """The entry without its CONFIG block. The block states a configuration; it does not
    quote a result, so its numbers have nowhere to have come from."""
    lines = entry.splitlines()
    out, inside = [], False
    for line in lines:
        if re.match(r"^\s+CONFIG\s", line):
            inside = True
            continue
        if inside:
            if not line.strip():
                inside = False
            continue
        out.append(line)
    return "\n".join(out)


def missing_scripts(entry: str) -> list[str]:
    """Paths named in `script` that do not exist.

    A `script` field is what makes an entry re-runnable, so a path that has been renamed
    turns the field into decoration. Only tokens that LOOK like repo paths are checked --
    the field also carries arguments, and `--seeds 0 1 2` is not a file.

    **A path under a fetched dataset directory is EXEMPT, and that is not a loophole.**
    `data/*/` is gitignored on purpose -- `tools/fetch_*.py` pins each URL, size and
    sha256 rather than carrying the bytes forever -- so `data/fb15k237/train.txt` exists
    on a machine that has fetched it and nowhere else. Resolving it made this check
    **pass locally and fail in CI**, which `tests/test_check_provenance.py` already names
    as the worst direction for a check to be wrong in, about a different bug in this same
    function. It cost a red run on 2026-07-31.

    Naming the real data file is CORRECT documentation of what was run, so the fix is to
    stop checking a path that cannot be checked rather than to make records vaguer.
    """
    line = re.search(r"^\s+script\s+(.+)$", entry, re.MULTILINE)
    if not line:
        return []
    gone = []
    for token in re.split(r"[\s,;]+", line.group(1)):
        bare = token.strip("`'\"()[],.")
        if bare.startswith("data/") and bare.count("/") > 1:
            continue
        if "/" in bare and re.search(r"\.(py|txt|md|yml|json)$", bare):
            if not (ROOT / bare).exists():
                gone.append(bare)
    return gone


def unsourced(text: str) -> dict[str, list[str]]:
    """Every measurement in a record that its own entry's sources do not contain."""
    found: dict[str, list[str]] = {}
    for heading, entry in config_blocks(text):
        for gone in missing_scripts(entry):
            found.setdefault(heading, []).append(f"script {gone} does not exist")
        sources = source_text(entry_sources(entry))
        for number in MEASUREMENT.findall(strip_config(entry)):
            if number in sources:
                continue
            # `1,146` in a record against `1146` in a script's output is the same
            # number, and refusing it would teach people to strip the commas.
            if number.replace(",", "") in sources.replace(",", ""):
                continue
            found.setdefault(heading, [])
            if number not in found[heading]:
                found[heading].append(number)
    return found


def current() -> list[str]:
    return [p.relative_to(ROOT).as_posix()
            for p in sorted(OPTIONS.glob("*.md")) if p.name not in EXEMPT]


def main() -> int:
    if not OPTIONS.exists():
        print("no docs/options yet - nothing to check")
        return 0

    records = current()
    found: dict[str, list[str]] = {}
    for relative in records:
        text = (ROOT / relative).read_text(encoding="utf-8")
        found[relative] = [f"{heading} :: {number}"
                           for heading, numbers in unsourced(text).items()
                           for number in numbers]

    if "--write-baseline" in sys.argv:
        BASELINE.write_text(json.dumps(found, indent=2, ensure_ascii=False) + "\n",
                            encoding="utf-8")
        total = sum(len(v) for v in found.values())
        print(f"wrote {BASELINE.relative_to(ROOT)} with {total} exemption(s)")
        return 0

    new, stale = compare(found, read_baseline(BASELINE, records))
    problems = sum(len(v) for v in new.values()) + sum(len(v) for v in stale.values())
    if not problems:
        checked = sum(len(MEASUREMENT.findall(strip_config(entry)))
                      for relative in records
                      for _, entry in config_blocks(
                          (ROOT / relative).read_text(encoding="utf-8")))
        print(f"provenance ok - {checked} measurement(s) across {len(records)} record(s) "
              f"found in the sources their entries cite")
        return 0

    print("A NUMBER IS NOT IN THE SOURCE ITS ENTRY CITES.\n")
    for relative, items in new.items():
        for item in items:
            print(f"  {relative}: {item}")
    print()
    for relative, items in stale.items():
        for item in items:
            print(f"  STALE EXEMPTION {relative}: {item} is sourced now; remove it "
                  f"from {BASELINE.relative_to(ROOT).as_posix()}")
    print("\nEither the number was retyped wrong, or the `source` field names the wrong\n"
          "run, or it is a figure derived while writing -- and a derived figure goes in\n"
          "the baseline, where the exemption is visible and can only be removed.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
