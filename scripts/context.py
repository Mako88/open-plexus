"""How many hearings a word the house never told takes to be pinned by its contexts, offline.

    uv run python scripts/context.py --seed 1 --seed 2 --seed 3 --note "..."

THE ORDER's item 1 measure, in John's form: a word's meaning built from its context with
the word left out. The house says a thing or a room by another word only in questions
("lamps" for lanterns), and says the same word again in later questions with different
names around it. Each hearing is one context.

A context's candidates for the word are the fillers that share a fact with every name the
question says as told, among facts told before it was asked whose answer is of the kind
asked, other than the told names and that fact's answer. No vectors and no types: a person
can be a candidate, as nothing has said the word is not one. Hearings accumulate by
intersection, as cross-situational learning has it.

The reading at hearing k says whether the word is pinned: exactly one candidate left, and
the right one. Hearing 1 is the control, the context alone. What the hearings after it add
is the learning. `answerable` is over questions holding such a word: every unheard word in
it pinned by its hearings so far, this one included.

Refuted if pinned at the third hearing is no higher than at the first, on every seed.
"""

from __future__ import annotations

import argparse
import json
import re
from collections import defaultdict
from pathlib import Path

from unfused.reading import utc_stamp
from unfused.exam.world import _OBJECT_SYNONYMS, _ROOM_SYNONYMS, generate_house

ROOT = Path(__file__).resolve().parents[1]
KIND = {"number": "number", "place": "room", "colour": "colour", "trade": "trade",
        "relation": "person"}
UNHEARD = {**{v: k for k, v in _OBJECT_SYNONYMS.items()},
           **{v: k for k, v in _ROOM_SYNONYMS.items()}}
HEARINGS = 5


def _says(text: str, phrase: str) -> bool:
    return re.search(rf"(?<![\w-]){re.escape(phrase)}(?![\w-])", text) is not None


def rows(fact) -> set[str]:
    return {x for x in (fact.subject, *fact.fields.values()) if x and x != fact.answer}


def read(seed: int) -> dict:
    house = generate_house(seed)
    names = sorted({x for f in house.facts for x in (*rows(f), f.answer)})
    candidates: dict[str, set[str] | None] = {}
    heard: dict[str, int] = defaultdict(int)
    by_hearing = defaultdict(lambda: {"pinned": 0, "wrong": 0, "lost": 0, "of": 0,
                                      "size": 0})
    answerable = {"yes": 0, "of": 0}
    for q in sorted(house.questions, key=lambda q: q.asked_at):
        text = q.text.lower()
        words = [w for w in UNHEARD if _says(text, w)]
        if not words or q.answer is None:
            continue
        told = {n for n in names if _says(text, n.lower())}
        facts = [f for f in house.facts
                 if house.told_at[f.id] <= q.asked_at and KIND[f.kind] == q.kind
                 and told <= rows(f)]
        here = set().union(*(rows(f) for f in facts)) - told if facts else set()
        all_pinned = True
        for w in words:
            heard[w] += 1
            k = heard[w]
            candidates[w] = here if candidates.get(w) is None else candidates[w] & here
            left = candidates[w]
            pinned = left == {UNHEARD[w]}
            all_pinned &= pinned
            if k <= HEARINGS:
                row = by_hearing[k]
                row["of"] += 1
                row["size"] += len(left)
                row["pinned"] += pinned
                row["wrong"] += len(left) == 1 and not pinned
                row["lost"] += UNHEARD[w] not in left
        answerable["of"] += 1
        answerable["yes"] += all_pinned
    table = {k: {"pinned": round(r["pinned"] / r["of"], 3), "wrong": r["wrong"],
                 "lost": r["lost"], "mean_left": round(r["size"] / r["of"], 1), "of": r["of"]}
             for k, r in sorted(by_hearing.items())}
    return {"seed": seed, "fingerprint": house.fingerprint(), "by_hearing": table,
            "answerable": round(answerable["yes"] / answerable["of"], 3),
            "questions": answerable["of"], "names": len(names)}


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--seed", type=int, action="append", required=True)
    p.add_argument("--note", default="")
    args = p.parse_args()
    out = []
    for seed in args.seed:
        r = read(seed)
        print(seed, "answerable", r["answerable"], "of", r["questions"])
        for k, row in r["by_hearing"].items():
            print(f"  hearing {k}: {row}")
        out.append(r)
    taken = utc_stamp()
    path = ROOT / "readings" / f"context-{taken}.json"
    path.write_text(json.dumps({
        "kind": "context", "taken_at": taken, "note": args.note,
        "question": "how many hearings pin a word the house never told, by its contexts alone",
        "seeds": out}, indent=1), encoding="utf-8")
    print("->", path.name)


if __name__ == "__main__":
    main()
