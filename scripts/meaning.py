"""Whether a relation's meaning can be built from the rows it sits in, offline.

    uv run python scripts/meaning.py --source readings/ears-...json --note "..."

Every assertion one ear read from one house, grouped by the relation's wording. A
wording's kind is the kind of fact it was most often read out of. The score is the AUC of
a similarity over wordings: how often a pair of the same kind is closer than a pair of
different kinds, ties counting a half. 0.5 is chance.

profile    the five features `System.properties` reads, compared by Jaccard
identity   each filler a random bipolar vector seeded from its word, bound to its slot by
           a rotation and summed over the wording's rows
borrowed   each filler's MiniLM vector, bound to its slot and summed the same way
"""

from __future__ import annotations

import argparse
import hashlib
import json
from collections import Counter, defaultdict
from datetime import UTC, datetime
from itertools import combinations
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SLOTS = ("subject", "object", "place", "quantity")
DIMS = 2048


def identity(word: str, dims: int = DIMS) -> np.ndarray:
    """A bipolar vector every process derives alike from the word alone."""
    seed = int.from_bytes(hashlib.sha256(word.encode()).digest()[:8], "little")
    return np.random.default_rng(seed).choice([-1.0, 1.0], dims)


def bound(vector: np.ndarray, slot: str) -> np.ndarray:
    """A filler's vector placed in its slot: a rotation per slot, so 'Mira' as subject and
    'Mira' as object add to different directions."""
    return np.roll(vector, 1 + SLOTS.index(slot))


def cosine(a: np.ndarray, b: np.ndarray) -> float:
    na, nb = np.linalg.norm(a), np.linalg.norm(b)
    return float(a @ b / (na * nb)) if na and nb else 0.0


def profiles(rows: list[dict]) -> dict[str, set[str]]:
    """`System.properties`, on the ear's raw assertions."""
    agents = {r["subject"] for r in rows}
    props: dict[str, set[str]] = {}
    for r in rows:
        p = props.setdefault(r["relation"], set())
        p |= {f"fills:{k}" for k in ("object", "place") if r.get(k)}
        if r.get("quantity") and any(c.isdigit() for c in str(r["quantity"])):
            p.add("fills:number")
        if r.get("object") in agents:
            p.add("object:agent")
        if r.get("subject") and any(r["subject"] == x.get("object") for x in rows):
            p.add("subject:named-elsewhere")
    return props


def summed(rows: list[dict], vector_of) -> dict[str, np.ndarray]:
    out: dict[str, np.ndarray] = {}
    for r in rows:
        for slot in SLOTS:
            if r.get(slot):
                v = bound(vector_of(str(r[slot]).lower()), slot)
                out[r["relation"]] = out.get(r["relation"], 0) + v
    return out


def sloted(rows: list[dict], vector_of, dims: int) -> dict[str, np.ndarray]:
    """Each slot's fillers averaged to a unit vector, the slots side by side: summed, the
    subject slot drowns the rest, because every relation's subjects are the same people."""
    parts: dict[str, dict[str, list]] = defaultdict(lambda: defaultdict(list))
    for r in rows:
        for slot in SLOTS:
            if r.get(slot):
                parts[r["relation"]][slot].append(vector_of(str(r[slot]).lower()))
    out = {}
    for relation, by_slot in parts.items():
        pieces = []
        for slot in SLOTS:
            v = np.mean(by_slot[slot], axis=0) if by_slot[slot] else np.zeros(dims)
            n = np.linalg.norm(v)
            pieces.append(v / n if n else v)
        out[relation] = np.concatenate(pieces)
    return out


def auc(kinds: dict[str, str], similarity) -> float:
    pairs = [(similarity(a, b), kinds[a] == kinds[b]) for a, b in combinations(sorted(kinds), 2)]
    same = [s for s, k in pairs if k]
    other = [s for s, k in pairs if not k]
    wins = sum((s > o) + 0.5 * (s == o) for s in same for o in other)
    return round(wins / (len(same) * len(other)), 3)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--source", required=True, action="append")
    p.add_argument("--note", default="")
    args = p.parse_args()

    readings = []
    for source in args.source:
        d = json.loads((ROOT / source).read_text(encoding="utf-8"))
        rows = [{**a, "kind": r["kind"]} for r in d["rows"] for a in r["assertions"]
                if a.get("relation")]
        votes: dict[str, Counter] = defaultdict(Counter)
        for r in rows:
            votes[r["relation"]][r["kind"]] += 1
        kinds = {w: c.most_common(1)[0][0] for w, c in votes.items()}

        from unfused.store.embed import MiniLmEmbedder

        embedder = MiniLmEmbedder()
        fillers = sorted({str(r[s]).lower() for r in rows for s in SLOTS if r.get(s)})
        borrowed = dict(zip(fillers, embedder.encode(fillers)))

        props = profiles(rows)
        built = {"identity": summed(rows, identity),
                 "borrowed": summed(rows, lambda w: borrowed[w]),
                 "identity by slot": sloted(rows, identity, DIMS),
                 "borrowed by slot": sloted(rows, lambda w: borrowed[w], embedder.dims)}
        scores = {"profile": auc(kinds, lambda a, b: len(props[a] & props[b])
                                 / max(1, len(props[a] | props[b])))}
        for arm, vectors in built.items():
            scores[arm] = auc(kinds, lambda a, b, v=vectors: cosine(v[a], v[b]))
        readings.append({"source": source, "wordings": len(kinds),
                         "kinds": len(set(kinds.values())), "auc": scores})
        print(source, len(kinds), "wordings", scores)

    taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    out = ROOT / "readings" / f"meaning-{taken}.json"
    out.write_text(json.dumps({
        "kind": "meaning", "taken_at": taken, "note": args.note,
        "question": "can a relation's meaning be built from the rows it sits in",
        "refutes_if": "a built arm's AUC is not above the profile's on every source",
        "dims": DIMS, "readings": readings}, indent=1), encoding="utf-8")
    print("->", out.name)


if __name__ == "__main__":
    main()
