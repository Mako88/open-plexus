"""Standing objections to what this branch does. Green, and it prints.

An entry is a choice that might be wrong and that nobody has measured, with the
reading that would settle it either way. It leaves by being settled, and the
count is asserted so that dropping one is visible.

  uv run python tests/pushback.py
"""

from __future__ import annotations

from dataclasses import dataclass

COUNT = 9


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
        what="The graph is walked one SQL query a step, and borrowing scans every taught "
             "shape.",
        why="One practice house makes about 180,000 queries. `reaching` tries every "
            "taught shape under every order of a question's names, and shapes grow with "
            "every wording heard. Nothing has run a house longer than 300 turns, and the "
            "primer fork wants a million edges.",
        settled_by="The first house at 3,000 and 30,000 turns, as a size the house already "
                   "has: seconds a question and score. Settled if seconds a question grow "
                   "slower than the graph; kept, with the walk named, if they do not.",
    ),
    Objection(
        what="`replaced` drops an event when a later one has the same arguments, whatever "
             "its verb.",
        why="It was built on bAbI, where every verb on a person and a thing changes who "
            "has it. 'Mary saw the football' has the arguments of 'Mary picked up the "
            "football', so it replaces the picking up as a drop does, and a question about "
            "the present then loses what Mary holds.",
        settled_by="bAbI-shaped stories with verbs that change nothing ('saw', 'looked at') "
                   "between the moves. If present-tense answers fall, which verbs replace "
                   "must be learnt from lessons, as the order of events already is.",
    ),
    Objection(
        what="Every lesson is the teacher's reaction in one wording, the turn straight "
             "after the question.",
        why="`converse` always answers with 'Yes, that's right.' or 'No, it's X.', and "
            "`turn` reads the next turn with no named subject as the reaction. A person "
            "corrects later, in their own words, and often with a subject: 'No, Ada keeps "
            "them in the shed' is read as a telling and teaches nothing.",
        settled_by="A teacher whose reactions vary in wording, some with a named subject, "
                   "some a few turns late. Settled if the first house scores within a "
                   "seed's spread of today's; kept, with `turn`'s reading named, if not.",
    ),
    Objection(
        what="Walks are cut to their first few hundred or thousand steps in the order "
             "SQLite returns them, which is oldest first.",
        why="`reaches` keeps 200, `follow` and `solve` 2000, `walks` 5000, and `around` "
            "has no ORDER BY. At 300 turns no room has that many events. At 30,000 a room "
            "everyone passes through does, and the cut keeps the oldest while an answer "
            "is chosen as the latest, so a size reading would fall for a reason other "
            "than the walk's speed.",
        settled_by="The 3,000- and 30,000-turn reading counts how often each cut binds. "
                   "Settled if none binds; if one does, the cut goes or keeps the latest "
                   "before the reading is taken as the cost of size.",
    ),
    Objection(
        what="Of several answers, the one resting on the latest event wins.",
        why="On the house a second answer is always an update, so recency is right. In "
            "conversation two can both be true ('Ada keeps jars in the cellar and in the "
            "pantry'), and `replaced`, which ignores prepositions, and `latest` drop the "
            "first. The contains-match scorer cannot see a list cut to one.",
        settled_by="A form with two places told for one thing and no move between them, "
                   "asked for both. Kept until the system says both, which is the mouth's "
                   "lists in the mouth's item of THE ORDER.",
    ),
    Objection(
        what="The stream's cloze asks 'who' for a proper name and 'what' for anything else.",
        why="So 'hugged what back?' wants 'mommy', and the wh-word carries the part of "
            "speech, not the meaning. Focus's mark factor reads that rule straight off, and "
            "no kind that means 'a person' can beat it: WordNet's true categories read 0.433 "
            "where the mark reads 0.491 (021256Z). A reader would ask 'who' of mommy.",
        settled_by="John's call, since it changes the world: a cloze asking 'who' of a "
                   "person however written. Then the mark and the oracle kinds read again; "
                   "if the oracle wins, learnt kinds are worth building for 5b.",
    ),
]


def test_count():
    assert len(OBJECTIONS) == COUNT


if __name__ == "__main__":
    for i, o in enumerate(OBJECTIONS, 1):
        print(f"{i}. {o.what}\n   why: {o.why}\n   settled by: {o.settled_by}\n")
