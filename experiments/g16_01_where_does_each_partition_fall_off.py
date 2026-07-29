"""At fixed per-node memory, where does each way of splitting the store fall off?

Note 043's falsifier, re-pointed after the arithmetic refuted its first version.

## The question, and the one it is NOT

The first version of note 043 claimed concept partitioning is the only proposal
that adds capacity as the corpus grows. **Worked through, that is false.** Using
decision 109's measured `d^2` scaling, at equal per-node memory:

    concept     N nodes of w x w                     total  N x cap(w)
    dimension   N nodes of (d/N) x d, d = w sqrt(N)  total  cap(w sqrt(N)) ~ N x cap(w)

Capacity per unit of memory does not distinguish them. So this does not ask
whether capacity grows -- it asks **where each arrangement breaks**:

> At fixed per-node memory, dimension partitioning forces `d = w sqrt(N)`, so
> each node holds `w / sqrt(N)` DIMENSIONS, which SHRINKS as nodes are added.
> g4-01 measured the floor: below ~16 dimensions a node has no standalone
> opinion (16 -> 0.949, 8 -> 0.681, 4 -> 0.412).

**Dimension partitioning trades per-node WIDTH for node count; concept
partitioning trades per-node CONCEPT COUNT and keeps width whole.** Width has a
hard floor. Concept count does not.

## What is measured

Decision 109's probe, rebuilt -- it was inline and left no script, which is the
same gap that left the whole relational line without instruments.

Random `(key, value)` pairs are written directly as outer products, no decay, no
cap, no task and no learning. Then each key is read back and the retrieval is
scored against the value it should return. **Capacity is the load at which 90% of
reads still recover the right value.**

    dimension    one store of width d, split N ways; a read needs the WHOLE key
                 and every node contributes a slice
    concept      N independent stores of width w; a key belongs to exactly one,
                 and only that store answers

Per-node memory is held equal, which is what makes the comparison mean anything:
`w^2` numbers per node in both arrangements.

> **The first version of this probe concluded the OPPOSITE of the truth** --
> decision 109 records it sampling keys WITH REPLACEMENT and so writing
> contradictory bindings, and the tell was widths 128 and 256 agreeing to three
> decimals. Keys here are sampled without replacement and a test pins it.

## PREDICTIONS (registered before running)

  P1  CONTROL. At N=1 the two arrangements are the SAME STORE and must agree to
      within noise. They differ only in how a store is split, and at one node
      there is no split. If they disagree, the harness is measuring its own
      bookkeeping.
  P2  Concept capacity grows roughly linearly with N. N independent stores of
      fixed width hold N times what one holds.
  P3  Dimension capacity grows too -- and then COLLAPSES once `w / sqrt(N)`
      drops under ~16 dimensions, which at w=64 is N=16.
  P4  At the largest N tested, concept beats dimension by more than 2x.
  P5  Neither reaches the sum of its parts exactly. Superposition interferes,
      so N stores hold somewhat less than N times one store's load.

P3 is the decision. **If both curves fall off together, the floor argument is
wrong** and concept partitioning keeps only its read-cost, concurrency and churn
arguments -- real, but engineering rather than capability, and that is worth
knowing before building it.

COST: pure numpy, no training, no task. 5 node counts x 2 arrangements x 8 loads
x 5 seeds. The largest cell writes a few thousand outer products at width 256.
Printed by `--cost`.

MEASURED ON: nothing -- this is a property of the data structure, deliberately
measured without a task so a task's quirks cannot enter it.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402

#: Per-node width for the concept arrangement, and the unit both arrangements
#: are held equal in: each node gets `PER_NODE_WIDTH ** 2` numbers.
PER_NODE_WIDTH = 64

NODES = (1, 2, 4, 8, 16)
SEEDS = (0, 1, 2, 3, 4)

#: Bindings per node to try. Read as a fraction of what one node can hold, so
#: the same grid is informative at every node count.
LOADS = (8, 16, 32, 48, 64, 96, 128, 192)

#: Decision 109's criterion, kept identical so the numbers are comparable.
RECOVERED = 0.90


def recovery(width: int, keys: np.ndarray, values: np.ndarray) -> float:
    """Share of keys whose read returns the right value, in ONE store.

    The store is the plain sum of outer products -- no decay, no cap, no
    learning -- because this measures the data structure and nothing else.
    """
    store = values.T @ keys                      # sum of outer(value, key)
    retrieved = store @ keys.T                   # every key read at once
    # Which stored value each read is closest to, by cosine. Nearest-value
    # rather than a threshold: a threshold would need a scale, and the scale is
    # exactly what changes with load.
    similarity = values @ retrieved
    similarity /= (np.linalg.norm(values, axis=1)[:, None] + 1e-12)
    return float((similarity.argmax(axis=0) == np.arange(len(keys))).mean())


def recovery_alone(width: int, slice_width: int, keys: np.ndarray,
                   values: np.ndarray) -> float:
    """Share recovered when ONE NODE answers by itself.

    **This is the quantity the pooled measure misses, and it is the whole
    argument.** Pooled capacity turned out identical between the two
    arrangements -- 128, 256, 512, 1024, 2048 at 1 to 16 nodes -- so total
    bindings held says nothing about which split is better.

    What differs is whether a node can answer WITHOUT the others. g4-01
    measured a lone node at 0.949 with 16 dimensions, 0.681 with 8 and 0.412
    with 4, and amended C1 is about not needing a collective: a read that
    requires every node is the barrier the constraint forbids.

    Under dimension splitting a node holds `slice_width` of the `width`
    dimensions, so it sees a fraction of every value. Under concept splitting a
    node holds whole values for its own keys, so `slice_width == width` and this
    is just `recovery`.
    """
    store = values.T @ keys
    part = store[:slice_width]                   # this node's rows only
    retrieved = part @ keys.T
    similarity = values[:, :slice_width] @ retrieved
    similarity /= (np.linalg.norm(values[:, :slice_width], axis=1)[:, None]
                   + 1e-12)
    return float((similarity.argmax(axis=0) == np.arange(len(keys))).mean())


def draw(rng, count: int, width: int) -> tuple[np.ndarray, np.ndarray]:
    """`count` distinct keys and their values, both random unit-ish vectors.

    **Distinct by construction.** Decision 109's first probe sampled keys with
    replacement, wrote contradictory bindings, and concluded capacity plateaus
    when it does not -- the tell was two widths agreeing to three decimals.
    Continuous draws are distinct with probability 1, and `test_capacity_probe`
    pins that the harness does not reuse one.
    """
    spread = 1.0 / np.sqrt(width)
    return (rng.normal(0.0, spread, (count, width)),
            rng.normal(0.0, spread, (count, width)))


def capacity(arrangement: str, nodes: int, seed: int) -> dict:
    """Total bindings held at `RECOVERED`, and the load curve behind it."""
    rng = np.random.default_rng(seed)
    if arrangement == "concept":
        # N independent stores of full width. A key belongs to exactly one.
        width, stores = PER_NODE_WIDTH, nodes
    else:
        # One store, split N ways, with per-node memory held equal: a node
        # holds (d / N) x d numbers, so d = w * sqrt(N).
        width, stores = int(round(PER_NODE_WIDTH * np.sqrt(nodes))), 1

    # How much of a value one node sees. Whole, under concept splitting; a
    # fraction under dimension splitting, and that fraction SHRINKS with nodes.
    slice_width = width if arrangement == "concept" else max(1, width // nodes)

    curve = []
    held = 0
    held_alone = 0
    for load in LOADS:
        # Total load is per-node load x nodes in both arrangements, so the two
        # are asked to hold the SAME NUMBER of bindings at each grid point.
        total = load * nodes
        per_store = total // stores
        if per_store < 1:
            continue
        pooled, alone = [], []
        for _ in range(stores):
            keys, values = draw(rng, per_store, width)
            pooled.append(recovery(width, keys, values))
            alone.append(recovery_alone(width, slice_width, keys, values))
        share, share_alone = float(np.mean(pooled)), float(np.mean(alone))
        curve.append({"load": total, "recovery": round(share, 4),
                      "recovery_alone": round(share_alone, 4)})
        if share >= RECOVERED:
            held = total
        if share_alone >= RECOVERED:
            held_alone = total
    return {
        "slice_width": slice_width,
        "held_alone": held_alone,
        "arrangement": arrangement, "nodes": nodes, "seed": seed,
        "width": width, "stores": stores,
        # Numbers per node, held equal between arrangements by construction --
        # printed so a reader can check that rather than trust it.
        "numbers_per_node": (width * width if arrangement == "concept"
                             else width * width // nodes),
        "held": held, "curve": curve,
        "condition": f"{arrangement}|nodes{nodes}|w{width}|seed{seed}",
    }


def cost_probe() -> None:
    started = time.time()
    capacity("dimension", max(NODES), 0)
    print(f"most expensive cell: dimension at {max(NODES)} nodes, "
          f"width {int(round(PER_NODE_WIDTH * np.sqrt(max(NODES))))}")
    print(f"  {time.time() - started:.1f} s")
    print(f"  {len(NODES) * 2 * len(SEEDS)} cells total, all cheaper")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--json", type=str, default=None)
    parser.add_argument("--cost", action="store_true")
    args = parser.parse_args()

    harness.refuse_if_mutating()
    if args.cost:
        cost_probe()
        return

    seeds = (args.seed,) if args.seed is not None else SEEDS
    records = [capacity(arrangement, nodes, seed)
               for seed in seeds for arrangement in ("concept", "dimension")
               for nodes in NODES]

    for record in records:
        print(f"{record['condition']}  held {record['held']:>5}  "
              f"ALONE {record['held_alone']:>5}  "
              f"(node sees {record['slice_width']:>3} of "
              f"{record['width']:>3} dims)  "
              f"per-node numbers {record['numbers_per_node']:,}")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


if __name__ == "__main__":
    main()
