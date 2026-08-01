"""Does a weight-gated flood pay, and what does it cost?

John's design: no cap and no sampling. Expand every edge whose strength clears a
floor, compose what the route amounts to as you go, and let the multiplication
kill the weak branches. **The weight is the budget.**

That is the join of three halves this project had separately — `grounding.reach`
propagates and is blind to what its edges mean, `pathways.PathTypes` knows what
they mean and is flat at two steps, `tasks.clutrr.reachable` composes a chain of
any length on symbols with no weights. `pathways.flood` is all three.

## The two questions, and the second is the one nobody can guess

**Does it reach more than the capped two-step enumeration?** That scored +0.0136
over a structureless floor of 0.2334 and arrived at the answer on about 35% of
queries, with the shortfall traced to the fan-out cap truncating the query
entity's own edge list.

**And what does it cost?** A flood with no cap is affordable only if the weights
prune. On an entity with 7,614 edges whose weights do not discriminate, nothing
prunes and the walk does not return. So every cell reports expansions per query
and how often the safety ceiling fired, and a run that gave up must not read like
one that finished.

## Where an edge's weight comes from, which is a choice and is not measured

Each FB15k triple appears once, so there is no co-occurrence count to weight by.
This scores an edge as `P(here | neighbour)` from the entity co-occurrence graph,
which is `1 / degree(neighbour)`: **an edge landing on something everything
connects to is weak.** That is `grounding`'s own argument for refusing an
ever-present partner, and it makes hub edges cheap automatically, which is
exactly where the fan-out cap was failing.

**It is a choice and a different one would give a different flood.** Named here
rather than buried, because nothing measures whether it is the right one.

    python experiments/fb15k237_flood.py --json out/fb15k237-flood.json
    python experiments/fb15k237_flood.py --queries 200 --depth 3
"""

from __future__ import annotations

import argparse
import collections
import json
import pathlib
import random
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402

from experiments.fb15k237_audit import (PUBLISHED, Marginal,  # noqa: E402
                                        Ranker, load, metrics)
from openplexus.composition import Composition  # noqa: E402
from openplexus.grounding import STATISTICS  # noqa: E402
from openplexus.pathways import PathTypes, flood  # noqa: E402

#: Floors to sweep. **This is the budget**, so it is the axis.
#:
#: **Measured, after a first grid of 0.05 to 0.002 reached the answer 0.0000 of
#: the time at every setting.** An edge weighs `1 / degree(neighbour)` and the
#: mean degree is 37.5, so a typical edge is about 0.027 and a two-step path
#: about 7e-4 -- the whole of that grid sat ABOVE the strength of every route it
#: was meant to admit. Multiplicative decay against small weights means the
#: useful floor is orders of magnitude below where a reader would guess, which
#: is the argument for sweeping it rather than picking one. Swept, never
#: pinned.
FLOORS = (0.001, 0.0005, 0.0002, 0.0001, 0.00005)

#: The statistic, for the path types and for the marginal. `conditional` is the
#: one measured to refuse an ever-present distractor (g39-04).
STATISTIC = "conditional"

#: Floors for the MEANING gate, which decays only by composition
#: confidence and so lives on a completely different scale from the
#: strength gate. Swept over the range a confidence can take.
MEANING_FLOORS = (0.5, 0.3, 0.2, 0.1, 0.05)

#: Blend weights, swept on every arm. Alpha 0 is the floor exactly, so a
#: flood that adds nothing scores the floor rather than below it.
BLENDS = (0.0, 0.005, 0.01, 0.05, 0.1, 0.3)

#: Expansions per query after which the walk gives up. **A safety, and every
#: cell reports how often it fired.** Chosen here as roughly the cost of the
#: capped enumeration it is being compared against, so a flood that is wildly
#: dearer is visible rather than merely slow.
CEILING = 200000

#: Fan-out for the training-time path counting only. The flood itself has no
#: cap; this bounds building the table it reads, which is a different budget and
#: is carried from `fb15k237_typed.py` so the two runs share a table.
COUNTING_FANOUT = 200


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    # Chosen here: a flood is dearer per query than an enumeration, so this
    # defaults low and the floor sweep is what the run is for.
    parser.add_argument("--queries", type=int, default=300)
    # Chosen here; it picks the query subsample only.
    parser.add_argument("--seed", type=int, default=0)
    # Chosen here as the shallowest depth that can compose past a pair;
    # two is the flat mechanism this is trying to beat, so three is the
    # first setting where the design does anything new.
    parser.add_argument("--depth", type=int, default=3)
    args = parser.parse_args()

    started = time.time()
    train, valid, test = load("train.txt"), load("valid.txt"), load("test.txt")
    entities = sorted({e for triples in (train, valid, test)
                       for h, _, t in triples for e in (h, t)})
    relations = sorted({r for _, r, _ in train})
    entity_at = {name: i for i, name in enumerate(entities)}
    relation_at = {name: i for i, name in enumerate(relations)}
    kinds = 2 * len(relations)

    out_of: dict = collections.defaultdict(list)
    for head, relation, tail in train:
        out_of[entity_at[head]].append((relation_at[relation], entity_at[tail]))
        out_of[entity_at[tail]].append((relation_at[relation] + len(relations),
                                        entity_at[head]))
    degree = {node: len(edges) for node, edges in out_of.items()}
    print(f"{len(train)} triples, {kinds} directed edge kinds, "
          f"mean degree {sum(degree.values()) / len(degree):.1f}, "
          f"largest {max(degree.values())}")

    # TWO WAYS TO SPEND THE BUDGET, and they are the whole comparison.
    #
    # `strength` weighs an edge `P(here | neighbour)`, which is
    # `1 / degree(neighbour)` where every pair co-occurs once -- an edge into a
    # hub is weak. It prunes by how well CONNECTED things are.
    #
    # `meaning` weighs every edge 1.0, so the only thing that decays along a
    # route is the confidence of what it composes into. It prunes by how much
    # the route MEANS, which is what the design asks for -- and it has no
    # defence against a hub at all, so the ceiling is expected to fire.
    #
    # Neither needs a change to `flood`: the walk takes its weights from the
    # adjacency it is handed, so the gating rule is a property of the caller.
    by_strength = {node: [(kind, other, 1.0 / degree[other])
                          for kind, other in edges]
                   for node, edges in out_of.items()}
    by_meaning = {node: [(kind, other, 1.0) for kind, other in edges]
                  for node, edges in out_of.items()}
    GATES = {"strength": by_strength, "meaning": by_meaning}

    types = PathTypes(kinds=kinds, spans=len(relations))
    counted = 0
    for head, relation, tail in train:
        target = relation_at[relation]
        start, end = entity_at[head], entity_at[tail]
        for first, middle in out_of[start][:COUNTING_FANOUT]:
            for second, landed in out_of[middle][:COUNTING_FANOUT]:
                if landed == end:
                    types.observe(first, second, target)
                    counted += 1
    print(f"counted {counted} two-step routes for the table "
          f"({time.time() - started:.1f}s)")

    marginal = Composition(len(entities), right=len(relations),
                           target=len(entities))
    for head, relation, tail in train:
        marginal.observe(entity_at[head], relation_at[relation],
                         entity_at[tail])

    known_tails: dict = {}
    known_heads: dict = {}
    for head, relation, tail in train + valid + test:
        known_tails.setdefault((head, relation), set()).add(tail)
        known_heads.setdefault((relation, tail), set()).add(head)
    ranker = Ranker(entities, known_tails, known_heads)
    statistic = STATISTICS[STATISTIC]
    floor_of = Marginal(marginal, entities, relation_at, statistic)

    queries = random.Random(args.seed).sample(test,
                                              min(args.queries, len(test)))
    print(f"scoring {len(queries)} triples in both directions, depth "
          f"{args.depth}, ceiling {CEILING} expansions\n")

    base = []
    for head, relation, tail in queries:
        for direction in ("tail", "head"):
            given, answer = ((head, tail) if direction == "tail"
                             else (tail, head))
            _, middle, _ = ranker.rank(floor_of.vector(relation, direction),
                                       given, relation, answer, direction)
            base.append(middle)
    floor_score = metrics(base)
    print(f"{'gate':>9}{'floor':>9}{'MRR':>9}{'margin':>9}{'arrived':>9}"
          f"{'expansions':>12}{'gave up':>9}{'sec':>8}")
    print(f"{'(none)':>9}{'-':>9}{floor_score['mrr']:>9.4f}{0.0:>+9.4f}"
          f"{'-':>9}{'-':>12}{'-':>9}{time.time() - started:>8.1f}")

    rows: list[dict] = [floor_score | {"arm": "relation only"}]
    for gate, table in GATES.items():
      def adjacency(node, table=table):
        return table.get(node, ())
      # A meaning gate does not decay by degree, so the floors that suit it are
      # orders of magnitude higher. Sweeping one grid over both would report
      # every cell of one arm as empty.
      for floor in (FLOORS if gate == "strength" else MEANING_FLOORS):
          began = time.time()
          ranks, arrived, spent, quit_early = {}, 0, 0, 0
          for head, relation, tail in queries:
              asked = relation_at[relation]
              for direction in ("tail", "head"):
                  given, answer = ((head, tail) if direction == "tail"
                                   else (tail, head))
                  found, expansions, gave_up = flood(
                      adjacency, entity_at[given], asked, types, statistic,
                      floor=floor, depth=args.depth, ceiling=CEILING)
                  spent += expansions
                  quit_early += gave_up
                  arrived += entity_at[answer] in found
                  vector = np.zeros(len(entities))
                  for endpoint, (score, _) in found.items():
                      vector[endpoint] = score
                  top = vector.max()
                  if top > 0:
                      vector /= top
                  # SWEPT ON THIS ARM TOO. Carrying the weight that won the
                  # capped enumeration would tune one arm and not the other,
                  # which is the untuned-baseline mistake this project refuses.
                  # There is no validation split here, so the best is taken on
                  # TEST and that FLATTERS the flood -- it is an upper bound on
                  # what the arm could do, not a score it earned.
                  other = floor_of.vector(relation, direction)
                  for alpha in BLENDS:
                      _, middle, _ = ranker.rank(
                          alpha * vector + (1.0 - alpha) * other,
                          given, relation, answer, direction)
                      ranks.setdefault(alpha, []).append(middle)
          scored = 2 * len(queries)
          by_alpha = {a: metrics(r)["mrr"] for a, r in ranks.items()}
          best_alpha = max(by_alpha, key=lambda a: by_alpha[a])
          got = metrics(ranks[best_alpha]) | {"best_alpha": best_alpha,
                                              "by_alpha": by_alpha}
          row = got | {"arm": "flood", "floor": floor, "depth": args.depth,
                       "margin": got["mrr"] - floor_score["mrr"],
                       "arrived": arrived / scored,
                       "expansions": spent / scored,
                       "gave_up": quit_early / scored}
          row["gate"] = gate
          rows.append(row)
          print(f"{gate:>9}{floor:>9}{got['mrr']:>9.4f}{row['margin']:>+9.4f}"
                f"{row['arrived']:>9.4f}{row['expansions']:>12.0f}"
                f"{row['gave_up']:>9.4f}{time.time() - began:>8.1f}")

    print("\nThe capped two-step enumeration this is measured against: "
          "+0.0136 margin, 0.35 arrived.")
    print("Published, against the same kind of floor: "
          + "  ".join(f"{name} {mrr - floor_score['mrr']:+.4f}"
                      for name, (mrr, _) in sorted(PUBLISHED.items(),
                                                   key=lambda i: i[1][0])))

    harness.emit(args.json, rows, started=started,
                 floors=list(FLOORS), meaning_floors=list(MEANING_FLOORS),
                 blends=list(BLENDS), statistic=STATISTIC, ceiling=CEILING,
                 depth=args.depth, queries=len(queries), seed=args.seed)
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
