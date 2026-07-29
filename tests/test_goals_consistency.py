"""The three top-level documents must not do each other's jobs.

The original version of this file guarded individual numbers inside `GOALS.md`,
because an audit found it presenting `T^0.67` as the answer for minimum machine
width while quoting `T^0.82` for the same quantity two paragraphs later, with the
consequences still computed from the older figure. Nothing was wrong in either
sweep; the document grew a second answer and kept the first.

Guarding the numbers one at a time was the weaker fix, and it was the one
available at the time. **The structural fix is that `GOALS.md` does not carry
measurements at all** — so there is no second answer for it to grow. That
narrative now lives in `docs/archive/goals-results-log.md`, where
`test_archive_consistency.py` still guards the figures.

CLAUDE.md rule 14b is the standard these enforce. Rule 18: prefer a rule that
makes the mistake structurally impossible over one that asks for more care.
"""

from __future__ import annotations

import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parent.parent
GOALS = (ROOT / "GOALS.md").read_text(encoding="utf-8")
DECISIONS = (ROOT / "DECISIONS.md").read_text(encoding="utf-8")
#: The log DECISIONS.md used to be. Kept as the reference every attempt in the tree
#: cites, so the reasoning stays one lookup away.
LOG = (ROOT / "docs" / "archive"
       / "decisions-log-083-171.md").read_text(encoding="utf-8")

#: A sweep record is where every measurement this project has made actually
#: lives, so a citation of one is the reliable tell that a document has started
#: carrying results. Matches the `g11-06` naming every sweep uses.
SWEEP_ID = re.compile(r"\bg\d+-\d{2}\b")

#: A markdown link INTO the sweep records. The bare path may be named — rule 14b
#: itself has to say where measurements live — but linking one is a citation.
SWEEP_LINK = re.compile(r"\]\([^)]*experiments/sweeps/[^)]*\)")


class GoalsHoldsNoMeasurements(unittest.TestCase):
    """`GOALS.md` opens with "nothing below is a measurement" and once closed
    with 405 lines of running results. These make the opening line true.

    The permitted numbers are arithmetic (a 150 ms round trip) and results
    inherited from the predecessor project, which §6.1 tabulates and labels. Both
    are stable. What is forbidden is this project's own sweep output, because
    that is the thing that moves and then disagrees with itself.
    """

    def test_no_sweep_is_cited(self):
        found = sorted(set(SWEEP_ID.findall(GOALS)))
        self.assertEqual(
            found, [],
            f"GOALS.md cites sweep records {found}. Measurements belong in "
            "STATE.md (if live) or docs/archive/ (if not) — GOALS states intent")

    def test_no_link_into_the_sweep_records(self):
        found = SWEEP_LINK.findall(GOALS)
        self.assertEqual(
            found, [],
            f"GOALS.md links into experiments/sweeps/: {found}. A document that "
            "links a measurement is a document that will outlive it")

    def test_it_still_says_so_out_loud(self):
        """The rule is only enforceable if a reader can see it is the rule."""
        self.assertIn("Nothing below is a measurement", GOALS)


class TheDocumentsPointAtEachOther(unittest.TestCase):
    """A reader landing on any one of the three has to be able to find the
    other two, and has to be told which one wins when they disagree.

    Without that, the log's newest entries get read as the current state — which
    is what happened, because they usually ARE, right up until they are not.
    """

    def test_goals_sends_a_reader_to_the_current_state(self):
        self.assertIn("DECISIONS.md", GOALS,
                      "GOALS.md should point at the decision tree for what is "
                      "live. It pointed at STATE.md until 2026-07-29, when three "
                      "documents became one")

    def test_the_archived_log_declares_itself_history(self):
        # The sentence used to live in DECISIONS.md, because DECISIONS.md WAS the
        # log. Now the log is archived and the tree is current, so the disclaimer
        # has to move with the content rather than staying on the filename.
        self.assertIn(
            "ARCHIVED", LOG,
            "the archived log must say so in its header, or its newest entry "
            "gets read as the current state -- which is exactly what happened")

    def test_the_archived_log_names_what_replaced_it(self):
        self.assertIn(
            "DECISIONS.md", LOG,
            "the log should name the document that supersedes it, at the point "
            "where a reader is standing in the log")

    def test_the_tree_says_detail_leaves_for_the_archive(self):
        """The failure mode is accumulation, not absence — the tree has to carry
        the instruction that keeps it small, exactly as STATE.md did."""
        self.assertIn(
            "the tree is authoritative; the log is the footnotes",
            " ".join(DECISIONS.split()).lower(),
            "DECISIONS.md should say which document wins and where detail goes; "
            "that sentence is the whole reason it stays readable")

    def test_the_tree_is_not_a_log(self):
        # The specific regression to guard: someone appends entry 172 and the tree
        # starts growing back into the thing it replaced.
        self.assertNotIn(
            "## 172.", DECISIONS,
            "a numbered entry was appended to the tree. New findings update an "
            "OPTION's state and its attempt list; the log is archived")


class EveryGateHasAVerdict(unittest.TestCase):
    """A gate table with a blank row is a gate nobody noticed was unanswered."""

    def test_no_gate_row_is_left_empty(self):
        rows = re.findall(r"^\| \*\*G\d.*$", GOALS, re.MULTILINE)
        self.assertGreaterEqual(len(rows), 5, "the gate table is missing rows")
        for row in rows:
            cells = [c.strip() for c in row.strip("|").split("|")]
            self.assertTrue(all(cells),
                            f"a gate row has an empty cell: {row}")

    def test_the_gate_table_is_the_only_place_a_verdict_is_written(self):
        """G4 and G5 both had their verdicts restated elsewhere in GOALS at one
        point, and restating a verdict is how two of them drift apart."""
        self.assertIn("only place a gate verdict is written", GOALS)


if __name__ == "__main__":
    unittest.main()
