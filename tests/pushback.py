"""Standing objections to what this branch does. Green, and it prints.

An entry is a choice that might be wrong and that nobody has measured, with the
reading that would settle it either way. It leaves by being settled, and the
count is asserted so that dropping one is visible.

  uv run python tests/pushback.py
"""

from __future__ import annotations

from dataclasses import dataclass

COUNT = 12


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
        what="Aliases carry from practice to the test house because every house shares "
             "one synonym table.",
        why="'Chipped dishes' means cracked plates in every house the generator makes, so "
            "an alias learnt in practice is memory of the table as much as a meaning learnt "
            "in conversation. Part of oblique's rise from about 0.5 to 0.8 may not survive "
            "a word the practice houses never used.",
        settled_by="A test house whose oblique synonyms no practice house uses, its lessons "
                   "inside the test house's own conversation. Oblique rising above its 0.5 "
                   "says the mechanism learns; falling back to it says the gain was memory.",
    ),
    Objection(
        what="Practice and test houses share every telling wording and every name list; "
             "only question frames are ever held out.",
        why="The `reworded` form asks in frames no lesson used, but every way a fact is "
            "told in the test house was also told in practice, with the same ten objects, "
            "rooms and trades. A plan is a path through one telling's wording, so the exam "
            "cannot see the cost of a telling the system was never taught to read.",
        settled_by="A form whose fact is told in a wording no practice house uses, asked in "
                   "a taught frame. Settled if it scores within a seed's spread of `direct`; "
                   "kept, with plans per wording named as the cause, if it does not.",
    ),
    Objection(
        what="How many lessons a relation needs is not measured.",
        why="Five practice houses teach about 330 answered questions each, for five "
            "relations. A child learns a question shape from a handful. If the system "
            "needs hundreds a relation, a conversation's thousands of relations need more "
            "teaching than any conversation holds.",
        settled_by="The first house read after one, two and five practice houses, and "
                   "after lessons capped per shape. The curve says how many a relation "
                   "needs; a score near today's at one house settles it.",
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
                   "lists in THE ORDER's first item.",
    ),
]


def test_count():
    assert len(OBJECTIONS) == COUNT


if __name__ == "__main__":
    for i, o in enumerate(OBJECTIONS, 1):
        print(f"{i}. {o.what}\n   why: {o.why}\n   settled by: {o.settled_by}\n")
