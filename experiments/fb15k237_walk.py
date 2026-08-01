"""Can a two-step walk reach what a one-step count cannot?

`fb15k237_counted.py` measured the count graph below the marginal — margin
−0.0480 — and measured why: over 3,000 test triples the two endpoints are
**0.0000 one hop apart in training and 0.7373 two hops apart**. The entity half
of a one-step query is not a weak signal, it is an empty one.

That is README §4's revival condition for walking further than one step, written
before this run and met by it. This is the walk.

## What is walked, and what deliberately is not

`grounding.reach` walks a count graph best-first, multiplies path strength along
the route, and bounds the SEARCH with a beam and a depth rather than bounding the
representation. It is run here over **entity-to-entity co-occurrence**: each
training triple says its two entities turned up together, and the relation is
left out of the graph on purpose.

The relation-typed version of a two-step path is rule mining, and
`fb15k237_audit.py` already ran it: `r1(h, x) & r2(x, t) => r(h, t)`, mined at
confidence, scoring 0.0460. So the untyped walk is the different question — not
*which composition of relations licenses this edge*, but *is the answer simply
NEAR the question, by weighted paths*. Two mechanisms, one already measured, and
they are not each other.

The relation half comes from `Composition` exactly as before, so the floor is
still the same program with a half switched off and the margin still means what
it meant.

## The axis is depth, and depth 1 is a check on the harness

At depth 1 this must reproduce the counted arm's empty entity signal, because a
one-step walk over the entity graph IS that signal. If depth 1 comes back healthy,
the walk is reaching something the count could not, which would mean the two runs
disagree about the same quantity and one of them is wrong.

    python experiments/fb15k237_walk.py --json out/fb15k237-walk.json
    python experiments/fb15k237_walk.py --queries 500
"""

from __future__ import annotations

import argparse
import json
import pathlib
import random
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments.fb15k237_audit import (PUBLISHED, Ranker,  # noqa: E402
                                        load, metrics)
from openplexus.composition import Composition  # noqa: E402
from openplexus.grounding import COMBINERS, CoOccurrence, STATISTICS, reach  # noqa: E402

#: Depths to walk. 1 is the harness check described above, 2 is where 0.7373 of
#: the answers live, 3 is where the remaining 0.2597 do. Swept, not pinned.
DEPTHS = (1, 2, 3)

#: Search beam. Swept, because `reach`'s own docstring says a beam is a search
#: budget rather than a representation budget -- raising it costs time and
#: changes no stored value, so the sweep is the honest way to find out whether
#: the walk is beam-limited or reach-limited.
#:
#: **Extended to 256 after 4 and 16 pinned at the top edge**: walk-only rose
#: 0.0033 to 0.0076 between them at depth 2, and a sweep that is still climbing
#: where it stops has not established anything about the mechanism.
BEAMS = (4, 16, 64, 256)

#: Cells above this are skipped, and the skip is PRINTED. A beam-256 walk to
#: depth 3 expands 256 * 256 frontier entries against a mean degree near 37,
#: which is minutes per query rather than milliseconds. Chosen here as the
#: largest budget that finishes overnight; a silent cap would let the table read
#: as a completed grid.
BUDGET = 20000

#: The statistic for both the walk and the relation half. `conditional` is the
#: one measured to refuse an ever-present distractor (g39-04), and this graph
#: has genuine hubs in it -- a popular entity is in thousands of triples.
STATISTIC = "conditional"

#: How the walk's evidence and the relation's are combined. Swept: `min` demands
#: both, `mean` lets one carry, and the counted run found that combining an
#: empty half with a working one under `min` is what produced a NEGATIVE margin.
COMBINERS_SWEPT = ("min", "mean")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--queries", type=int, default=None)
    # Chosen here; it picks the query subsample only, and the full run uses
    # every test triple.
    parser.add_argument("--seed", type=int, default=0)
    args = parser.parse_args()

    started = time.time()
    train, valid, test = load("train.txt"), load("valid.txt"), load("test.txt")
    entities = sorted({e for triples in (train, valid, test)
                       for h, _, t in triples for e in (h, t)})
    relations = sorted({r for _, r, _ in train})
    entity_at = {name: i for i, name in enumerate(entities)}
    relation_at = {name: i for i, name in enumerate(relations)}

    # THE WALKED GRAPH: entities only, undirected co-occurrence.
    walked = CoOccurrence()
    for head, _, tail in train:
        walked.observe((entity_at[head], entity_at[tail]))
    # THE RELATION HALF: the same `Composition` the counted run used, so the
    # floor here is the floor there rather than a second implementation of it.
    counts = Composition(len(entities), right=len(relations),
                         target=len(entities))
    for head, relation, tail in train:
        counts.observe(entity_at[head], relation_at[relation], entity_at[tail])
    print(f"walked graph: {len(walked.surfaces())} entity surfaces, "
          f"{len(train)} triples ({time.time() - started:.1f}s)")

    known_tails: dict = {}
    known_heads: dict = {}
    for head, relation, tail in train + valid + test:
        known_tails.setdefault((head, relation), set()).add(tail)
        known_heads.setdefault((relation, tail), set()).add(head)
    ranker = Ranker(entities, known_tails, known_heads)

    queries = test if args.queries is None else random.Random(
        args.seed).sample(test, min(args.queries, len(test)))
    print(f"scoring {len(queries)} triples in both directions\n")

    statistic = STATISTICS[STATISTIC]
    marginal_cache: dict = {}

    def relation_vector(relation: str, direction: str) -> np.ndarray:
        """`P(candidate | relation)` as a dense vector. The floor, cached."""
        key = (relation, direction)
        if key not in marginal_cache:
            want = "target" if direction == "tail" else "left"
            vector = np.zeros(len(entities))
            for score, candidate in counts.given(
                    {"right": relation_at[relation]}, want, statistic):
                vector[candidate] = score
            marginal_cache[key] = vector
        return marginal_cache[key]

    rows: list[dict] = []
    header = (f"{'arm':<30}{'MRR':>9}{'hits@1':>9}{'hits@10':>9}"
              f"{'margin':>9}{'sec':>8}")
    print(header)
    print("-" * len(header))

    floor_ranks = []
    for head, relation, tail in queries:
        for direction in ("tail", "head"):
            given, answer = ((head, tail) if direction == "tail"
                             else (tail, head))
            _, middle, _ = ranker.rank(relation_vector(relation, direction),
                                       given, relation, answer, direction)
            floor_ranks.append(middle)
    floor = metrics(floor_ranks)
    rows.append(floor | {"arm": "relation only"})
    print(f"{'relation only (the floor)':<30}{floor['mrr']:>9.4f}"
          f"{floor['hits1']:>9.4f}{floor['hits10']:>9.4f}{0.0:>9.4f}"
          f"{time.time() - started:>8.1f}")

    skipped = []
    for depth in DEPTHS:
        for beam in BEAMS:
            if beam ** max(depth - 1, 1) > BUDGET:
                skipped.append((depth, beam))
                continue
            for combine in ("walk only", *COMBINERS_SWEPT):
                began = time.time()
                ranks = []
                for head, relation, tail in queries:
                    for direction in ("tail", "head"):
                        given, answer = ((head, tail) if direction == "tail"
                                         else (tail, head))
                        vector = np.zeros(len(entities))
                        for surface, strength in reach(
                                walked, statistic, entity_at[given],
                                beam=beam, depth=depth).items():
                            vector[surface] = strength
                        if combine != "walk only":
                            other = relation_vector(relation, direction)
                            rule = COMBINERS[combine]
                            vector = np.array([rule(a, b) for a, b
                                               in zip(vector, other)])
                        _, middle, _ = ranker.rank(vector, given, relation,
                                                   answer, direction)
                        ranks.append(middle)
                got = metrics(ranks) | {"arm": combine, "depth": depth,
                                        "beam": beam, "statistic": STATISTIC}
                got["margin"] = got["mrr"] - floor["mrr"]
                rows.append(got)
                label = f"depth {depth} beam {beam:>2} / {combine}"
                print(f"{label:<30}{got['mrr']:>9.4f}{got['hits1']:>9.4f}"
                      f"{got['hits10']:>9.4f}{got['margin']:>+9.4f}"
                      f"{time.time() - began:>8.1f}")

    if skipped:
        # SAID OUT LOUD. A grid with cells quietly missing reads as a grid that
        # was covered, and the missing cells here are the largest searches --
        # exactly the ones a reader would want before believing a null.
        print("\nNOT RUN, over the search budget: "
              + ", ".join(f"depth {d} beam {b}" for d, b in skipped))
        rows.append({"arm": "skipped",
                     "cells": [{"depth": d, "beam": b} for d, b in skipped]})

    best = max((row for row in rows
                if row["arm"] not in ("relation only", "skipped")),
               key=lambda row: row["mrr"])
    print(f"\nBest walk arm: {best['mrr']:.4f} at depth {best['depth']}, "
          f"beam {best['beam']}, {best['arm']} — margin "
          f"{best['margin']:+.4f} over the floor")
    print("Published, for the margin each holds over this same floor:")
    for name, (mrr, _) in sorted(PUBLISHED.items(), key=lambda item: item[1][0]):
        print(f"  {name:>10}  {mrr:.4f}   margin {mrr - floor['mrr']:+.4f}")

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"\n{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
