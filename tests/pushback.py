"""Standing objections to what this branch does. Green, and it prints.

An entry is a choice that might be wrong and that nobody has measured, with the
reading that would settle it either way. It leaves by being settled, and the
count is asserted so that dropping one is visible.

  uv run python tests/pushback.py
"""

from __future__ import annotations

from dataclasses import dataclass

COUNT = 5


@dataclass(frozen=True)
class Objection:
    what: str
    why: str
    settled_by: str


OBJECTIONS = [
    Objection(
        what="An arm's own answers are never written to its memory.",
        why="In a real conversation what the machine said is part of what happened, and "
            "a wrong answer it recalls later is a failure the exam cannot currently see.",
        settled_by="Run the recall arm with answers written as episodes. If the score moves "
                   "by more than a seed's spread, the exam has been flattering every arm.",
    ),
    Objection(
        what="Refusal is decided by the faculty from the system prompt.",
        why="The invented rate then measures the prompt as much as the memory. The memory "
            "has its own evidence for 'not told': nothing surfaced, or surfaced weakly.",
        settled_by="The open fork 'confidence from the memory itself': an arm that refuses on "
                   "recall strength alone, compared on invented rate at equal score.",
    ),
    Objection(
        what="Scoring is a contains-match on the canonical answer.",
        why="An answer that lists two rooms is scored correct if either is right, and a "
            "hedge that names the answer counts. It is generous to every arm alike, but "
            "that can hide a difference between arms that hedge differently.",
        settled_by="Count answers naming more than one candidate of the right kind, per arm. "
                   "Above a few per cent for any arm, score those as wrong and re-read.",
    ),
    Objection(
        what="`graph.py` holds English words by hand: the pronouns, 'no' and 'nope' in a "
             "reaction, and the number words.",
        why="John's, 2026-10-01: a rule for a word does not generalise. The list it replaced, "
            "`System.clean`'s, went with the taught arm; these are what is left of the habit, "
            "and the next pronoun, refusal or numeral will need another.",
        settled_by="A learnt reading of each: a pronoun bound by centering as Phase 5 says, "
                   "'no' learnt from reactions that came before a corrected lesson, numbers "
                   "from counts a lesson taught. Each scored on the readings it touches.",
    ),
    Objection(
        what="A reading on a house the generator no longer makes counts for nothing, "
             "whole.",
        why="Adding a form changes the fingerprint, so every baseline goes and the red set "
            "reads red until scripts/baselines.sh has run for about 1h45m. That is a tax on "
            "exactly the move the exam's rules ask for: a form added when conversation has "
            "it.",
        settled_by="Score a reading on the forms it shares with today's house, by question "
                   "text and turn. Settled if the 9B's shared forms on the houses before and "
                   "after 9b76fc31 differ by less than a seed's spread; kept if they do not.",
    ),
]


def test_count():
    assert len(OBJECTIONS) == COUNT


if __name__ == "__main__":
    for i, o in enumerate(OBJECTIONS, 1):
        print(f"{i}. {o.what}\n   why: {o.why}\n   settled by: {o.settled_by}\n")
