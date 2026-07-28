"""What counts as a passing testbed run, which differs when a node leaves.

**A run with a departure is not required to agree with the single-process
model.** Losing a quarter of the store's dimensions should change later answers.
Demanding agreement demands the wrong thing, and `testbed/run.py` demanded it:
it returned 1 whenever `agrees_with_one_process` was false, so **every churn run
reported failure for behaving correctly.** g12-02 lost all eighteen cells to it,
twice.

The rule that does apply is the driver's: `mismatches_before_departure == 0`. A
machine switching off may change what happens next; it cannot reach back and
change an answer already given.

These are cheap because `verdict` is a pure function of the report. The bug
survived two dispatches because the only thing exercising it was a container run
whose exit code was being swallowed by a pipe.
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from testbed.run import verdict  # noqa: E402


def report(**overrides) -> dict:
    base = {"agrees_with_one_process": True, "mismatches": 0,
            "leave_at": 0, "mismatches_before_departure": 0}
    base.update(overrides)
    return base


class WithoutADeparture(unittest.TestCase):

    def test_agreement_passes(self):
        self.assertEqual(verdict(report()), 0)

    def test_disagreement_fails(self):
        """With nobody leaving, a difference from the single-process model is a
        broken harness and there is nothing else it could be."""
        self.assertEqual(
            verdict(report(agrees_with_one_process=False, mismatches=3)), 1)


class WithADeparture(unittest.TestCase):

    def test_disagreement_after_the_departure_PASSES(self):
        """The case the old code got backwards. Losing dimensions should cost
        answers; a run that lost none would be the suspicious one."""
        self.assertEqual(
            verdict(report(leave_at=20, agrees_with_one_process=False,
                           mismatches=8, mismatches_before_departure=0)), 0)

    def test_divergence_BEFORE_the_departure_fails(self):
        """The C3 property, and the only thing a churn run can fail on. A
        departure reaching backwards refutes the constraint at the protocol
        level."""
        self.assertEqual(
            verdict(report(leave_at=20, agrees_with_one_process=False,
                           mismatches=8, mismatches_before_departure=2)), 1)

    def test_full_agreement_still_passes(self):
        """Possible when the departed node held nothing the answers needed --
        unlikely, but not a failure."""
        self.assertEqual(
            verdict(report(leave_at=20, mismatches_before_departure=0)), 0)


class TheTwoRulesAreDifferent(unittest.TestCase):

    def test_the_same_report_passes_or_fails_on_leave_at_alone(self):
        """The whole bug in one assertion: identical disagreement, opposite
        verdicts, decided only by whether a departure was expected."""
        disagreeing = dict(agrees_with_one_process=False, mismatches=8,
                           mismatches_before_departure=0)
        self.assertEqual(verdict(report(leave_at=0, **disagreeing)), 1)
        self.assertEqual(verdict(report(leave_at=20, **disagreeing)), 0)


if __name__ == "__main__":
    unittest.main()
