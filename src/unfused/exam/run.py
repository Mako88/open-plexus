"""Run one arm through one house and score it.

Scoring is a contains-match on a short canonical answer: generous to every arm
alike, which is the property that matters. Three failures are counted apart
from wrong, because averaging them in hides each:

- invented: a negative answered with anything but a refusal;
- stale: an update answered with the place that used to be right;
- misled: a denied or hedged fact answered with the room the denial or hedge named;
- echoed: the question handed back.
"""

from __future__ import annotations

import re
import time
from collections import defaultdict
from collections.abc import Callable

from .world import RIGHT, WRONG, House, Question

REFUSALS = (
    "don't know", "do not know", "not sure", "no idea", "unknown", "not mentioned",
    "never mentioned", "haven't told", "have not told", "wasn't mentioned",
    "was not mentioned", "don't have", "do not have", "no information", "not say",
    "doesn't say", "does not say", "no one", "nobody",
)


def is_refusal(said: str) -> bool:
    lowered = said.lower()
    return any(r in lowered for r in REFUSALS)


NUMBERS = ("zero one two three four five six seven eight nine ten eleven twelve thirteen "
           "fourteen fifteen sixteen seventeen eighteen nineteen twenty").split()


def judge(q: Question, said: str) -> dict:
    lowered = said.lower()
    echoed = q.text.lower().rstrip("?").strip() in lowered
    if q.answer is None:
        return {"correct": False, "invented": not is_refusal(said) and not echoed,
                "stale": False, "echoed": echoed}
    answer = q.answer.lower()
    if answer.isdigit() or answer in NUMBERS:
        # a number is matched whole, or "2" would be found in "12", and in either
        # form: bAbI writes "two" where a model and the system say "2"
        digit = answer if answer.isdigit() else str(NUMBERS.index(answer))
        word = NUMBERS[int(digit)] if int(digit) < len(NUMBERS) else None
        correct = (re.search(rf"(?<!\d){digit}(?!\d)", lowered) is not None
                   or (word is not None and re.search(rf"\b{word}\b", lowered) is not None))
    else:
        correct = answer in lowered
    stale = bool(q.stale) and q.stale.lower() in lowered and not correct
    return {"correct": correct, "invented": False, "stale": stale, "echoed": echoed}


def run(house: House, open_arm: Callable[[], object], reopen_every: int = 50,
        limit: int | None = None) -> dict:
    """Feed the conversation turn by turn and ask each question at its turn.

    The arm is closed and reopened every `reopen_every` turns, so what it knows
    must survive on disk. `limit` asks only the first `limit` questions, for a
    smoke run; a reading that counts leaves it unset.
    """
    arm = open_arm()
    asked_after = house.questions_after()
    rows: list[dict] = []
    reacted: dict[str, bool] = {}  # a reacted question's words, and whether it was right
    started = time.perf_counter()
    budget = limit if limit is not None else len(house.questions)
    for turn, text in enumerate(house.turns):
        if turn and turn % reopen_every == 0:
            arm.close()
            arm = open_arm()
        arm.hear(turn, text)
        for q in asked_after.get(turn, []):
            if len(rows) >= budget:
                break
            said = react(arm, turn, q, reacted) if q.form == "reacted" else arm.answer(q)
            row = {"form": q.form, "kind": q.kind, "delay": q.delay, "asked_at": q.asked_at,
                   "question": q.text, "answer": q.answer, "said": said, **judge(q, said)}
            if q.form == "corrected":
                row["first_correct"] = reacted.get(q.text)
            notes = getattr(arm, "last_notes", None)
            if notes is not None:
                row["notes"] = list(notes)
            rows.append(row)
    dials = arm.dials()
    name = arm.name
    arm.close()
    return {"arm": name, "dials": dials, "seconds": round(time.perf_counter() - started, 1),
            "summary": summarise(rows), "rows": rows}


def react(arm, turn: int, q: Question, reacted: dict[str, bool]) -> str:
    """A question asked as a lesson's is, the teacher reacting to the answer. An arm that
    sorts its own turns hears the reaction as the turn after its answer; any other hears
    the question, its answer and the reaction as three turns."""
    if hasattr(arm, "turn"):
        said = arm.turn(turn, q.text) or ""
    else:
        said = arm.answer(q)
    right = judge(q, said)["correct"]
    reaction = RIGHT if right else WRONG.format(answer=q.answer)
    if hasattr(arm, "turn"):
        arm.turn(turn, reaction)
    else:
        for text in (q.text, said, reaction):
            arm.hear(turn, text)
    reacted[q.text] = right
    return said


def converse(world: House, arm, untaught: set | frozenset = frozenset()) -> None:
    """A world as a conversation that teaches: every turn handed to the arm unlabelled,
    each question asked as a turn of its own, and the teacher's reaction to whatever the
    arm said as the turn after it. Nothing tells the arm which turn is which."""
    asked_after = world.questions_after()
    for turn, text in enumerate(world.turns):
        arm.turn(turn, text)
        for q in asked_after.get(turn, []):
            if q.answer is None or q.form in untaught:
                continue
            said = arm.turn(turn, q.text)
            right = judge(q, said or "")["correct"]
            arm.turn(turn, RIGHT if right else WRONG.format(answer=q.answer))


def summarise(rows: list[dict]) -> dict:
    positives = [r for r in rows if r["answer"] is not None]
    negatives = [r for r in rows if r["answer"] is None]

    def rate(rs, key):
        return round(sum(r[key] for r in rs) / len(rs), 3) if rs else None

    def by(key):
        groups: dict = defaultdict(list)
        for r in positives:
            groups[r[key]].append(r)
        return {str(k): {"score": rate(v, "correct"), "n": len(v)}
                for k, v in sorted(groups.items(), key=lambda kv: str(kv[0]))}

    updates = [r for r in positives if r["form"] == "update"]
    unsaid = [r for r in positives if r["form"] in ("denied", "hedged")]
    return {
        "score": rate(positives, "correct"),
        "asked": len(positives),
        "invented": rate(negatives, "invented"),
        "negatives": len(negatives),
        "stale": rate(updates, "stale"),
        "misled": rate(unsaid, "stale"),
        "echoed": rate(rows, "echoed"),
        "by_form": by("form"),
        "by_delay": by("delay"),
        "by_kind": by("kind"),
    }
