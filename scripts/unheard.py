"""Words the house never used, resolved to what it did use, offline.

    uv run python scripts/unheard.py --seed 1 --seed 2 --seed 3 --note "..."

Every oblique question names a thing, and for a count a room too, in words no telling
used ("canes" for walking sticks). Each arm picks a stored filler for each such word.
The stored fillers are every filler of every fact in the house, and the rows are the
facts, each a set of fillers with the kind of answer it gives.

bare    the nearest stored filler by cosine, word against word
fitted  the nearest among the fillers that fit the rest of the question: they sit in one
        row with the question's other fillers, heard or resolved together, and that row
        answers the kind of thing the question asks for

A pick counts when it is the filler the oblique word stands for. Margin is the cosine
gap between an arm's best and second-best pick, read to see whether it separates the
right picks from the wrong ones.
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from itertools import product
from pathlib import Path

import numpy as np

from unfused.exam.world import _OBJECT_SYNONYMS, _ROOM_SYNONYMS, generate_house

ROOT = Path(__file__).resolve().parents[1]
KIND = {"number": "number", "place": "room", "colour": "colour"}


def stored(house) -> tuple[list[str], list[tuple[set, str]]]:
    rows = []
    for f in house.facts:
        fillers = {f.subject, f.answer, *f.fields.values()}
        rows.append(({x for x in fillers if x}, KIND.get(f.kind, f.kind)))
    names = sorted({x for r, _ in rows for x in r})
    return names, rows


def unheard(fact) -> tuple[dict[str, str], set[str]]:
    """The question's words the house never used, each with the filler it stands for, and
    the fillers it names as told."""
    if fact.kind == "number":
        return ({_OBJECT_SYNONYMS[fact.subject]: fact.subject,
                 _ROOM_SYNONYMS[fact.fields["room"]]: fact.fields["room"]}, set())
    if fact.kind == "place":
        thing = fact.fields["thing"]
        return {_OBJECT_SYNONYMS[thing]: thing}, {fact.subject}
    return {_OBJECT_SYNONYMS[fact.subject]: fact.subject}, set()


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--seed", type=int, action="append", required=True)
    p.add_argument("--note", default="")
    args = p.parse_args()

    from unfused.store.embed import MiniLmEmbedder

    embedder = MiniLmEmbedder()
    out = []
    for seed in args.seed:
        house = generate_house(seed)
        names, rows = stored(house)
        vectors = dict(zip(names, embedder.encode(names)))
        tally = {"bare": [], "fitted": []}
        for fact in house.facts:
            if fact.kind not in ("number", "place", "colour"):
                continue
            words, known = unheard(fact)
            asked = KIND[fact.kind]
            sims = {w: {n: float(vectors[n] @ v) for n in names}
                    for w, v in zip(words, embedder.encode(list(words)))}
            for w, gold in words.items():
                ranked = sorted(names, key=lambda n: -sims[w][n])
                margin = sims[w][ranked[0]] - sims[w][ranked[1]]
                tally["bare"].append({"word": w, "gold": gold, "pick": ranked[0],
                                      "right": ranked[0] == gold, "margin": round(margin, 3)})
            # every joint pick whose fillers share one row of the asked kind with what the
            # question names as told, scored by the sum of the words' cosines
            fits = [(r, k) for r, k in rows if k == asked and known <= r]
            order = list(words)
            joint = []
            for picks in product(names, repeat=len(order)):
                if any(set(picks) | known <= r for r, _ in fits) and len(set(picks)) == len(picks):
                    joint.append((sum(sims[w][n] for w, n in zip(order, picks)), picks))
            joint.sort(reverse=True)
            for i, w in enumerate(order):
                pick = joint[0][1][i] if joint else None
                margin = (joint[0][0] - joint[1][0]) if len(joint) > 1 else 1.0
                tally["fitted"].append({"word": w, "gold": words[w], "pick": pick,
                                        "right": pick == words[w], "margin": round(margin, 3)})
        summary = {}
        for arm, picks in tally.items():
            right = [x["margin"] for x in picks if x["right"]]
            wrong = [x["margin"] for x in picks if not x["right"]]
            summary[arm] = {"right": len(right), "of": len(picks),
                            "margin_right": round(float(np.median(right)), 3) if right else None,
                            "margin_wrong": round(float(np.median(wrong)), 3) if wrong else None}
        print(seed, summary)
        out.append({"seed": seed, "summary": summary, "picks": tally})

    taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = ROOT / "readings" / f"unheard-{taken}.json"
    path.write_text(json.dumps({
        "kind": "unheard", "taken_at": taken, "note": args.note,
        "question": "which stored filler does a word the house never used stand for",
        "embedder": "all-MiniLM-L6-v2", "seeds": out}, indent=1), encoding="utf-8")
    print("->", path.name)


if __name__ == "__main__":
    main()
