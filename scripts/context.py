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

`typed` keeps of the candidates those most like the word in how it sits in a sentence: a
name's slots are the parse features of every mention of it (its part of speech, its link
and its head's, the preposition it hangs from, the links of its children), from tellings for
a told name and from the questions it was heard in for the word, compared by Jaccard. It is
item 4's likeness by link labels, which is why it is read here first. Refuted if it pins
no more than the untyped intersection, or pins any word wrongly.
"""

from __future__ import annotations

import argparse
import json
import re
from collections import defaultdict
from datetime import UTC, datetime
from pathlib import Path

from unfused.graph import nlp
from unfused.exam.world import _OBJECT_SYNONYMS, _ROOM_SYNONYMS, generate_house

ROOT = Path(__file__).resolve().parents[1]
KIND = {"number": "number", "place": "room", "colour": "colour", "trade": "trade",
        "relation": "person"}
UNHEARD = {**{v: k for k, v in _OBJECT_SYNONYMS.items()},
           **{v: k for k, v in _ROOM_SYNONYMS.items()}}
HEARINGS = 5
NEAR = 0.5  # a candidate is of the word's kind within this share of the likest


def _says(text: str, phrase: str) -> bool:
    return re.search(rf"(?<![\w-]){re.escape(phrase)}(?![\w-])", text) is not None


def rows(fact) -> set[str]:
    return {x for x in (fact.subject, *fact.fields.values()) if x and x != fact.answer}


def slots(doc, phrase: str) -> set[str]:
    """The parse features of each mention of `phrase` in `doc`, by its last token."""
    out: set[str] = set()
    low = doc.text.lower()
    for m in re.finditer(rf"(?<![\w-]){re.escape(phrase)}(?![\w-])", low):
        span = doc.char_span(m.start(), m.end(), alignment_mode="expand")
        if span is None:
            continue
        t = span[-1]
        out |= {f"pos:{t.pos_}", f"dep:{t.dep_}", f"head:{t.head.pos_}",
                f"up:{t.dep_}>{t.head.dep_}"}
        if t.head.pos_ == "ADP":
            out.add(f"in:{t.head.lower_}")
        out |= {f"child:{c.dep_}" for c in t.children}
    return out


def like(a: set[str], b: set[str]) -> float:
    return len(a & b) / len(a | b) if a | b else 0.0


def read(seed: int) -> dict:
    house = generate_house(seed)
    names = sorted({x for f in house.facts for x in (*rows(f), f.answer)})
    parse = nlp("en_core_web_trf")
    turns = list(parse.pipe(house.turns))
    asked = [q for q in house.questions if q.answer is not None
             and any(_says(q.text.lower(), w) for w in UNHEARD)]
    asked_docs = dict(zip((q.text for q in asked), parse.pipe([q.text for q in asked])))
    told_slots: dict[str, set[str]] = defaultdict(set)
    told_upto = 0
    word_slots: dict[str, set[str]] = defaultdict(set)
    typed: dict[str, set[str] | None] = {}
    by_typed = defaultdict(lambda: {"pinned": 0, "wrong": 0, "lost": 0, "of": 0, "size": 0})
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
        while told_upto <= q.asked_at and told_upto < len(turns):
            for n in names:
                told_slots[n] |= slots(turns[told_upto], n.lower())
            told_upto += 1
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
            word_slots[w] |= slots(asked_docs[q.text], w)
            best = max((like(word_slots[w], told_slots[n]) for n in left), default=0.0)
            typed[w] = {n for n in left if like(word_slots[w], told_slots[n]) >= best * NEAR}
            if k <= HEARINGS:
                row = by_typed[k]
                row["of"] += 1
                row["size"] += len(typed[w])
                row["pinned"] += typed[w] == {UNHEARD[w]}
                row["wrong"] += len(typed[w]) == 1 and typed[w] != {UNHEARD[w]}
                row["lost"] += UNHEARD[w] not in typed[w]
            if k <= HEARINGS:
                row = by_hearing[k]
                row["of"] += 1
                row["size"] += len(left)
                row["pinned"] += pinned
                row["wrong"] += len(left) == 1 and not pinned
                row["lost"] += UNHEARD[w] not in left
        answerable["of"] += 1
        answerable["yes"] += all_pinned
    def table(by):
        return {k: {"pinned": round(r["pinned"] / r["of"], 3), "wrong": r["wrong"],
                    "lost": r["lost"], "mean_left": round(r["size"] / r["of"], 1),
                    "of": r["of"]} for k, r in sorted(by.items())}

    return {"seed": seed, "fingerprint": house.fingerprint(), "by_hearing": table(by_hearing),
            "typed": table(by_typed),
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
        for arm in ("by_hearing", "typed"):
            for k, row in r[arm].items():
                print(f"  {arm} {k}: {row}")
        out.append(r)
    taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = ROOT / "readings" / f"context-{taken}.json"
    path.write_text(json.dumps({
        "kind": "context", "taken_at": taken, "note": args.note,
        "question": "how many hearings pin a word the house never told, by its contexts alone",
        "seeds": out}, indent=1), encoding="utf-8")
    print("->", path.name)


if __name__ == "__main__":
    main()
