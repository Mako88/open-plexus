"""Run one arm through one house and score it.

Scoring is a contains-match on a short canonical answer: generous to every arm
alike, which is the property that matters. Three failures are counted apart
from wrong, because averaging them in hides each:

- invented: a negative answered with anything but a refusal;
- stale: an update answered with the place that used to be right;
- echoed: the question handed back.
"""

from __future__ import annotations

import re
import time
from collections import defaultdict
from collections.abc import Callable

from .world import House, Question

REFUSALS = (
    "don't know", "do not know", "not sure", "no idea", "unknown", "not mentioned",
    "never mentioned", "haven't told", "have not told", "wasn't mentioned",
    "was not mentioned", "don't have", "do not have", "no information", "not say",
    "doesn't say", "does not say", "no one", "nobody",
)


def is_refusal(said: str) -> bool:
    lowered = said.lower()
    return any(r in lowered for r in REFUSALS)


def judge(q: Question, said: str) -> dict:
    lowered = said.lower()
    echoed = q.text.lower().rstrip("?").strip() in lowered
    if q.answer is None:
        return {"correct": False, "invented": not is_refusal(said) and not echoed,
                "stale": False, "echoed": echoed}
    if q.answer.isdigit():
        # a number is matched whole, or "2" would be found in "12"
        correct = re.search(rf"(?<!\d){q.answer}(?!\d)", lowered) is not None
    else:
        correct = q.answer.lower() in lowered
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
            said = arm.answer(q)
            row = {"form": q.form, "kind": q.kind, "delay": q.delay, "asked_at": q.asked_at,
                   "question": q.text, "answer": q.answer, "said": said, **judge(q, said)}
            notes = getattr(arm, "last_notes", None)
            if notes is not None:
                row["notes"] = list(notes)
            rows.append(row)
    dials = arm.dials()
    name = arm.name
    arm.close()
    return {"arm": name, "dials": dials, "seconds": round(time.perf_counter() - started, 1),
            "summary": summarise(rows), "rows": rows}


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
    return {
        "score": rate(positives, "correct"),
        "asked": len(positives),
        "invented": rate(negatives, "invented"),
        "negatives": len(negatives),
        "stale": rate(updates, "stale"),
        "echoed": rate(rows, "echoed"),
        "by_form": by("form"),
        "by_delay": by("delay"),
        "by_kind": by("kind"),
    }
