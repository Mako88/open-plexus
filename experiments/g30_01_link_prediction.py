"""The raw superposed store on FB15k-237's own task: standard tail-side link prediction.

**What this does not duplicate.** The data, the filter, the ranking and the metrics all
live in `tools/link_prediction.py` and are imported. They were inline here until `g30-02`
needed the identical protocol for a different scorer; two copies of a filtered-ranking
convention is the exact shape rule 9 warns about, so the protocol moved and this file kept
only what is specific to the store. `tools/fetch_fb15k237.py` owns the data.

## Why this is reading the store rather than redesigning anything

Link prediction asks: given a head and a relation, rank candidate tails. **The store is
addressed by `(entity, relation)` and returns a value** — the same pair in, the same kind
of thing out. John, 2026-07-30: *"evaluate what we have on their task."*

## The arithmetic that made the outcome predictable

Store capacity is about `0.023 * d^2` bindings. At width 256 that is **~1,500** against
FB15k-237's **272,115** triples — **181x over capacity**. `g30-01` P1 therefore treated
*beating chance* as a gate rather than a formality.

Predictions and the scored result are in
`experiments/sweeps/g30-01-link-prediction-on-their-task.txt`, committed at `2ec4f7e`
before this file existed.
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from tools import link_prediction as lp  # noqa: E402

WIDTHS = (256, 512)


def store_scorer(task: lp.Task, width: int, seed: int):
    """A scorer reading one `d x d` matrix of summed outer products, built from TRAIN.

    A key per `(entity, relation)` is 14,505 x 237 vectors, which does not fit, so the
    key is DERIVED: the elementwise product of an entity key and a relation key,
    normalised. That is `PairKeys`' shape without its token alphabet, which FB15k-237
    does not have.
    """
    rng = np.random.default_rng(seed)
    values = rng.normal(0.0, 1.0, (task.n_entities, width))
    values /= np.linalg.norm(values, axis=1, keepdims=True)
    ent_key = rng.normal(0.0, 1.0, (task.n_entities, width))
    rel_key = rng.normal(0.0, 1.0, (len(task.rel), width))

    def keys(heads, rels):
        k = ent_key[heads] * rel_key[rels]
        return k / (np.linalg.norm(k, axis=1, keepdims=True) + 1e-12)

    heads, rels, tails = task.train_indices()
    store = np.zeros((width, width))
    for start in range(0, len(heads), 4096):
        stop = start + 4096
        store += keys(heads[start:stop], rels[start:stop]).T @ values[tails[start:stop]]
    return lambda h, r: (keys(h, r) @ store) @ values.T


def main() -> None:
    harness.parse_args(__doc__)
    task = lp.Task()
    print(f"\ntest {len(task.heads):,} scored, {task.unanswerable} unanswerable "
          f"(entity or relation unseen in train); {task.n_entities:,} candidates")
    print("\n" + lp.header())
    frequency = task.evaluate(task.frequency_scorer())
    for width in WIDTHS:
        ranks = task.evaluate(store_scorer(task, width, seed=0))
        print(lp.row(f"store, width {width}", ranks["filtered"]))
        print(lp.row(f"  same, UNFILTERED", ranks["unfiltered"]))
    print(lp.row("frequency", frequency["filtered"]))
    print(f"\n  chance MRR {1 / task.n_entities:.6f}; capacity 0.023*d^2 is "
          f"{0.023 * 256 ** 2:,.0f} bindings at width 256 against "
          f"{len(task.train):,} triples")


if __name__ == "__main__":
    main()
