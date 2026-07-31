"""Our store on FB15k-237's own task: standard tail-side link prediction.

**What this does not duplicate.** Searched `openplexus/`, `tools/`, `tests/` and
`experiments/`: nothing here does link prediction, and nothing reads FB15k-237 except
`tools/relation_contrastive.py --graph`, which asks a different question (RULE prediction:
given two composed relations, name the third). `tools/fetch_fb15k237.py` owns the data and
its checksums. The store, the keys and the value table come from `openplexus`; what is new
is the ranking and the metrics.

## Why this is reading the store rather than redesigning anything

Link prediction asks: given a head and a relation, rank candidate tails. **The store is
addressed by `(entity, relation)` and returns a value** — that is the same pair in and the
same kind of thing out. John, 2026-07-30: *"evaluate what we have on their task."*

## The arithmetic that makes the outcome predictable

Store capacity is about `0.023 * d^2` bindings (`134`'s probe). At width 256 that is
**~1,500**, against FB15k-237's **272,115** triples — roughly **180x over capacity** — and
retrieval quality goes as `sqrt(d / stored)`, about **0.03** here. `g30-01` P1 therefore
treats *beating chance* as a gate rather than a formality.

## Filtered is the convention and both are printed

Other known-true tails for `(h, r)` are removed from the ranking, using train + valid +
test — the standard convention, and **not** the same as training on them. The store is
built from TRAIN only. Unfiltered is printed beside it, because a number that does not say
which it is, is not a number.

Predictions are in `experiments/sweeps/g30-01-link-prediction-on-their-task.txt`,
committed at `2ec4f7e` before this file existed.
"""

from __future__ import annotations

import sys
from collections import defaultdict
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "data" / "fb15k237"
WIDTHS = (256, 512)


def triples(split: str):
    for line in (DATA / f"{split}.txt").read_text(encoding="utf-8").splitlines():
        parts = line.split("\t")
        if len(parts) == 3:
            yield parts[0], parts[1], parts[2]


def build(width: int, seed: int):
    """Entity values, relation keys, and the store, from TRAIN only."""
    train = list(triples("train"))
    entities = sorted({e for h, _, t in train for e in (h, t)})
    relations = sorted({r for _, r, _ in train})
    ent = {e: i for i, e in enumerate(entities)}
    rel = {r: i for i, r in enumerate(relations)}

    rng = np.random.default_rng(seed)
    values = rng.normal(0.0, 1.0, (len(entities), width))
    values /= np.linalg.norm(values, axis=1, keepdims=True)
    # A key per (entity, relation) is 14,505 x 237 vectors, which does not fit. The
    # store's own convention is a DERIVED key: hash the pair into the width. This is
    # `PairKeys`' shape without its token alphabet, which FB15k-237 does not have.
    ent_key = rng.normal(0.0, 1.0, (len(entities), width))
    rel_key = rng.normal(0.0, 1.0, (len(relations), width))

    def key(h: int, r: int) -> np.ndarray:
        k = ent_key[h] * rel_key[r]
        return k / (np.linalg.norm(k) + 1e-12)

    store = np.zeros((width, width))
    for h, r, t in train:
        store += np.outer(key(ent[h], rel[r]), values[ent[t]])
    return ent, rel, values, key, store, train


def main() -> None:
    harness.parse_args(__doc__)
    seed = 0
    for width in WIDTHS:
        ent, rel, values, key, store, train = build(width, seed)

        # The FILTER uses every split, which is the convention: it removes OTHER
        # true answers from the ranking so a correct alternative is not counted as
        # beating the target. The STORE saw train only.
        known: dict = defaultdict(set)
        for split in ("train", "valid", "test"):
            for h, r, t in triples(split):
                if h in ent and r in rel and t in ent:
                    known[(ent[h], rel[r])].add(ent[t])

        # The cheap opponent: rank entities by how often they are a tail of this
        # relation. No capacity, no learning.
        tail_counts: dict = defaultdict(lambda: np.zeros(len(ent)))
        for h, r, t in train:
            tail_counts[rel[r]][ent[t]] += 1.0

        ranks = {"store": [], "frequency": []}
        ranks_unfiltered = []
        unanswerable = 0
        for h, r, t in triples("test"):
            if h not in ent or r not in rel or t not in ent:
                unanswerable += 1
                continue
            hi, ri, ti = ent[h], rel[r], ent[t]
            scores = store.T @ key(hi, ri) @ values.T
            ranks_unfiltered.append(int((scores > scores[ti]).sum()) + 1)
            others = np.array(sorted(known[(hi, ri)] - {ti}), dtype=int)
            for name, raw in (("store", scores),
                              ("frequency", tail_counts[ri].copy())):
                s = raw.copy()
                if others.size:
                    s[others] = -np.inf
                ranks[name].append(int((s > s[ti]).sum()) + 1)

        print(f"\n=== width {width} ===  test {len(ranks['store']):,} scored, "
              f"{unanswerable} unanswerable (entity or relation unseen in train)")
        print(f"{'arm':<12}{'MRR':>9}{'Hits@1':>9}{'Hits@3':>9}{'Hits@10':>9}")
        for name in ("store", "frequency"):
            a = np.array(ranks[name], dtype=float)
            print(f"{name:<12}{(1/a).mean():>9.4f}{(a<=1).mean():>9.4f}"
                  f"{(a<=3).mean():>9.4f}{(a<=10).mean():>9.4f}")
        u = np.array(ranks_unfiltered, dtype=float)
        print(f"{'store, UNFIL':<12}{(1/u).mean():>9.4f}{(u<=1).mean():>9.4f}"
              f"{(u<=3).mean():>9.4f}{(u<=10).mean():>9.4f}")
        print(f"  chance MRR ~ {1/len(ent):.6f};  store holds ~{0.023*width**2:,.0f} "
              f"bindings against {len(train):,} triples "
              f"({len(train)/(0.023*width**2):.0f}x over capacity)")


if __name__ == "__main__":
    main()
