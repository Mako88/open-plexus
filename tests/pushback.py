"""Standing objections to what this branch does. Green, and it prints.

An entry is a choice that might be wrong and that nobody has measured, with the
reading that would settle it either way. It leaves by being settled, and the
count is asserted so that dropping one is visible.

  uv run python tests/pushback.py
"""

from __future__ import annotations

from dataclasses import dataclass

COUNT = 4


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
        what="Blind's table is the commonest answer among the test house's own answers.",
        why="So it has read the answer key's distribution, and on a form whose answers "
            "bunch it scores what the key gives it: seed 1's chain3 is 20 questions, 4 "
            "distinct, one answer, and blind reads 1.0 there and 0.0 with a table taken "
            "from the five practice houses the taught system learns from. Count is not "
            "affected ('1' is commonest everywhere), and it is the half of the checkpoint "
            "that is a real bar.",
        settled_by="John's call, since the checkpoint is his: if blind should be what can be "
                   "said without hearing the test house, its table comes from the practice "
                   "houses and the checkpoint is re-read against that. If blind is meant to "
                   "know the test's answer marginals, the docstring's account of it changes "
                   "and this entry goes.",
    ),
]


def test_count():
    assert len(OBJECTIONS) == COUNT


if __name__ == "__main__":
    for i, o in enumerate(OBJECTIONS, 1):
        print(f"{i}. {o.what}\n   why: {o.why}\n   settled by: {o.settled_by}\n")
