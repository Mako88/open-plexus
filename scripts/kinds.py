"""Whether the links a name is held by tell its kind, offline: item 4's ceiling.

    uv run python scripts/kinds.py --seed 1 --seed 2 --seed 3 --note "..."

Every answer the house's questions want is a name in the graph once the house is heard. A
name's profile is how many times each link label holds it ('prep:in', 'acomp', 'dobj').
Two rules are read, each with the house's true kinds as the classes, so this is a ceiling
on what a kind check by labels can do, not a mechanism:

set       a name shares a label with a kind's pooled labels; read as how often a name
          shares one with another kind's pool, which is how often a set check lets a
          wrong kind through
nearest   a name's kind is the one whose pooled profile is nearest by cosine, the name
          itself left out of the pool

Refuted, for a check by label distributions, if nearest is right no more often than the
commonest kind's share.
"""

from __future__ import annotations

import argparse
import json
import os
import tempfile
from collections import Counter
from pathlib import Path

import numpy as np

from unfused.reading import utc_stamp
from unfused.exam.world import generate_house
from unfused.graph import GraphArm

os.environ.setdefault("HF_HUB_OFFLINE", "1")
ROOT = Path(__file__).resolve().parents[1]


def cos(x, y) -> float:
    d = float(np.linalg.norm(x) * np.linalg.norm(y))
    return float(x @ y) / d if d else 0.0


def read(seed: int) -> dict:
    house = generate_house(seed=seed, n_facts=50, n_turns=300)
    arm = GraphArm(Path(tempfile.mkdtemp(prefix="unfused-kinds-")))
    for i, t in enumerate(house.turns):
        if not t.rstrip().endswith("?"):
            arm.hear(i, t)
    kind = {q.answer.lower(): q.kind for q in house.questions
            if q.answer and q.kind != "count" and arm.known(q.answer.lower())}
    held = {n: Counter(lab for (lab,) in arm.db.execute(
        "SELECT label FROM edges WHERE node = ?", (f"n:{n}",))) for n in kind}
    arm.close()
    labels = sorted({lab for p in held.values() for lab in p})
    vec = {n: np.array([held[n][lab] for lab in labels], float) for n in kind}
    kinds = sorted(set(kind.values()))
    right, through, pairs, wrong = 0, 0, 0, Counter()
    for n, k in kind.items():
        pool = {kk: sum((vec[m] / vec[m].sum() for m, km in kind.items()
                         if km == kk and m != n), np.zeros(len(labels))) for kk in kinds}
        best = max(kinds, key=lambda kk: cos(vec[n], pool[kk]))
        right += best == k
        if best != k:
            wrong[f"{k}->{best}"] += 1
        for kk in kinds:
            if kk != k:
                pairs += 1
                through += bool(set(held[n]) & {labels[i] for i in np.nonzero(pool[kk])[0]})
    share = max(Counter(kind.values()).values()) / len(kind)
    return {"seed": seed, "names": len(kind), "nearest_right": right,
            "commonest_share": round(share, 3), "set_lets_through": through,
            "set_pairs": pairs, "nearest_wrong": dict(wrong)}


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--seed", type=int, action="append", required=True)
    p.add_argument("--note", default="")
    args = p.parse_args()
    out = []
    for seed in args.seed:
        row = read(seed)
        out.append(row)
        print(row, flush=True)
    taken = utc_stamp()
    path = ROOT / "readings" / f"kinds-{taken}.json"
    path.write_text(json.dumps({
        "kind": "kinds", "taken_at": taken, "note": args.note,
        "question": "do the links a name is held by tell its kind", "seeds": out},
        indent=1), encoding="utf-8")
    print(path.name)


if __name__ == "__main__":
    main()
