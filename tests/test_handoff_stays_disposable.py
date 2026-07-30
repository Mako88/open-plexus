"""`HANDOFF.md` must stay throwaway, and a rule saying so would not keep it that way.

John's concern when he asked for the file, 2026-07-30: *"I wanna make sure we don't
create a precedent that could result in too many docs and getting out of hand again."*
The precedent that bites is not the file existing — it is the file becoming
**load-bearing**, which happens the moment something durable cites it. A scratch document
that three notes point at cannot be thrown away, and then it grows, and then it is a
second decisions log. That is exactly the failure `DECISIONS.md` was rebuilt to escape:
6,040 append-only lines, read selectively, producing three wrong recommendations in a row.

So the contract is enforced rather than asked for, which is CLAUDE.md rule 18's whole
argument — prefer a check to a reminder:

- **Nothing may cite it** except `DECISIONS.md`, which holds the pointer telling a new
  session it exists, and `CLAUDE.md`, which records that a PREVIOUS `HANDOFF.md` carried
  a wrong headline result for weeks -- history, not dependency.
- **It must stay short.** Length is the early symptom of a file that is accumulating
  instead of being replaced.

Neither check can tell whether the CONTENT was rewritten rather than appended to. That
part is a habit, and the header says so in the file itself.
"""

from __future__ import annotations

import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
HANDOFF = ROOT / "HANDOFF.md"

#: Generous, because the point is to catch accumulation and not to police editing. The
#: file was 88 lines when written; at three times that it is being appended to.
MAX_LINES = 260

#: The permitted references, and both are deliberate.
#:
#: `DECISIONS.md` holds the one pointer that tells a new session this file exists, since
#: it is what a session reads first.
#:
#: `CLAUDE.md` mentions it in rule 14b's CALIBRATION -- the record that a previous
#: `HANDOFF.md` carried a wrong headline result for weeks before decision 118 caught it.
#: That is a citation of the file's HISTORY, not a dependency on its contents, and
#: deleting the file would not invalidate it. Excluding the record of why this guard
#: exists would be the wrong way to make the guard pass.
ALLOWED = {"DECISIONS.md", "CLAUDE.md"}

#: Where a search would find spurious matches that are not citations: this test's own
#: source, and anything git or tooling generated.
SKIP_DIRS = {".git", "__pycache__", ".github", "data", "docs/archive"}


class TheHandoffStaysDisposable(unittest.TestCase):

    def test_it_exists_or_it_does_not_but_the_rules_hold_either_way(self):
        """Absent is FINE. Between swaps there may be nothing to hand off, and a file
        kept alive only to satisfy a test is the accumulation this guards against."""
        if not HANDOFF.exists():
            self.skipTest("no handoff in flight, which is a valid state")

    def test_nothing_durable_cites_it(self):
        if not HANDOFF.exists():
            self.skipTest("no handoff in flight")
        citing = []
        for path in ROOT.rglob("*"):
            if not path.is_file() or path.suffix not in {".md", ".py", ".yml", ".yaml"}:
                continue
            relative = path.relative_to(ROOT).as_posix()
            if any(relative.startswith(skip) or f"/{skip}/" in f"/{relative}"
                   for skip in SKIP_DIRS):
                continue
            if path == HANDOFF or path == Path(__file__):
                continue
            try:
                text = path.read_text(encoding="utf-8")
            except (OSError, UnicodeDecodeError):
                continue
            if "HANDOFF.md" in text and relative not in ALLOWED:
                citing.append(relative)
        self.assertEqual(
            citing, [],
            f"{citing} cite HANDOFF.md. It is scratch context for a session swap and "
            f"is OVERWRITTEN each time, so anything depending on it breaks silently "
            f"when it is replaced -- and a scratch file that cannot be thrown away "
            f"becomes a second decisions log. Move what is worth keeping into "
            f"DECISIONS.md or a numbered note and cite that instead.")

    def test_it_has_not_started_accumulating(self):
        if not HANDOFF.exists():
            self.skipTest("no handoff in flight")
        lines = len(HANDOFF.read_text(encoding="utf-8").splitlines())
        self.assertLessEqual(
            lines, MAX_LINES,
            f"HANDOFF.md is {lines} lines against a {MAX_LINES} budget. It is meant to "
            f"be REPLACED at each swap, not extended -- growth means sections are being "
            f"added to a file whose whole value is that it can be discarded.")

    def test_it_says_what_it_is(self):
        """A reader who opens it cold must learn the contract from the file itself."""
        if not HANDOFF.exists():
            self.skipTest("no handoff in flight")
        head = HANDOFF.read_text(encoding="utf-8")[:1200]
        for expected in ("TEMPORARY", "OVERWRITTEN"):
            self.assertIn(
                expected, head,
                f"the header does not say it is {expected}, so a session that finds "
                f"this file has no way to know it is scratch rather than a record")


if __name__ == "__main__":
    unittest.main()
