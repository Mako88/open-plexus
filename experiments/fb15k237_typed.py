"""A ranked walk that knows which relations it walked through.

Three things have now been measured on FB15k-237 and none of them clears the
marginal floor of 0.2334:

    the marginal itself      0.2334   rank tails by the relation alone
    the one-step count      -0.0480   the answer is never one step away
    the untyped walk        -0.0265   it reaches the answer and cannot rank it
    the rule miner           0.0460   typed two-hop paths, thresholded lookup

**The untyped walk's refutation named its own revival condition: give the walk
the relation types along the path.** This is that, and it is not the rule miner:

- the miner keeps a path type only if its confidence clears 0.5, and answers
  from the best surviving rule. One path decides.
- this counts every path type without a threshold, and a candidate accumulates
  evidence from EVERY path that reaches it. Many weak agreeing paths can
  outrank one strong path, which is the thing a ranked walk is for and the thing
  a thresholded lookup structurally cannot do.

## The mechanism is `Composition` again, over relation pairs

For a training triple `(h, r, t)`, every two-step route from `h` to `t` says
*walking `r1` then `r2` got to the same place `r` does*. That is exactly a
composition fact, so it is counted by the same class the CLUTRR work used:

    Composition(left=relations, right=relations, target=relations)
    observe(r1, r2, r)

At query time a candidate is scored by how well the paths that reach it predict
the relation being asked about — `P(r | r1, r2)` from those counts — accumulated
over paths, and then combined with the relation marginal as every other arm has
been.

Reusing `Composition` is the point rather than a convenience: **the null it
returned on CLUTRR was bounded by having only three observations per
relation-role**, and here the same mechanism gets 272,115 triples. If the bound
was the data, this is where it shows.

    python experiments/fb15k237_typed.py --json out/fb15k237-typed.json
    python experiments/fb15k237_typed.py --queries 500
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

from experiments.fb15k237_audit import (PUBLISHED, Marginal,  # noqa: E402
                                        Ranker, load, metrics)
from openplexus.composition import Composition  # noqa: E402
from openplexus.grounding import COMBINERS, STATISTICS  # noqa: E402

#: The statistic, for the path-type counts and for the relation marginal alike.
#: `conditional` is the one measured to refuse an ever-present distractor
#: (g39-04), and a knowledge graph is mostly hubs.
STATISTIC = "conditional"

#: How a candidate's path evidence is accumulated. Swept, because it is the one
#: axis that distinguishes this from the rule miner: `max` keeps the single best
#: path and is what a thresholded lookup does, `sum` lets agreeing paths add up.
ACCUMULATORS = ("max", "sum")

#: How path evidence and the relation marginal are combined. Swept for the same
#: reason as everywhere else in this run.
COMBINERS_SWEPT = ("min", "mean")

#: Blend weights for `alpha * structure + (1 - alpha) * marginal`. **`min` and
#: `mean` are arbitrary fixed mixes and both landed below the floor**, which
#: says as much about the mix as about the signal. This asks the question
#: properly: alpha 0 IS the floor, exactly, so any alpha that beats it is the
#: structural signal adding something and no alpha beating it is a clean null.
#:
#: Swept on VALIDATION and read on test, because choosing the weight on the
#: thing being reported is how a null becomes a positive result.
#:
#: **Extended below 0.05 after 0.05 won and was the smallest non-zero value
#: tried** — a grid whose winner sits at its edge has not established where the
#: optimum is, and the first run's winning margin was +0.0107 on 150 triples,
#: which is inside the noise of a sample that size. Both are fixed here.
ALPHAS = (0.0, 0.01, 0.02, 0.05, 0.1, 0.2, 0.3, 0.5, 0.7, 1.0)

#: Cap on the branching factor when enumerating two-step routes, and it is
#: PRINTED. The mean out-degree is about 37 and the largest is 7,614, so a
#: handful of hub entities would otherwise dominate the cost of every query they
#: appear in. Chosen here; a cap that is not reported lets a partial enumeration
#: read as a complete one.
#:
#: **What it takes is now a random subset rather than the first N.** The edges
#: were in insertion order, so a hub's slice was whichever triples happened to
#: be read first -- an arbitrary and systematically biased sample, and the
#: reached/never-reached split traced most of the mechanism's losses to answers
#: no path arrived at. Each entity's list is shuffled ONCE at build time with a
#: seeded generator, so the prefix is a uniform sample, the cost per query is
#: unchanged, and two runs at one seed still agree exactly.
FANOUT = 200


def routes(out_of, start, fanout):
    """Every `(first relation, second relation, end)` two steps from `start`."""
    for first, middle in out_of.get(start, ())[:fanout]:
        for second, end in out_of.get(middle, ())[:fanout]:
            yield first, second, end


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    # Chosen here as a default that finishes in minutes rather than hours; the
    # route enumeration is the fixed cost and the per-query work scales with
    # this. A margin as small as the one this run is chasing needs it raised —
    # 150 queries put the first positive margin inside its own noise.
    parser.add_argument("--queries", type=int, default=1000)
    # Chosen here; it picks the query subsample and the training sample for the
    # path counts, and both are reported.
    parser.add_argument("--seed", type=int, default=0)
    # Swept from the command line rather than pinned in the file, because the
    # reached/never-reached split says this is the binding constraint and cost
    # grows as its square -- so it is a budget to be spent deliberately.
    parser.add_argument("--fanout", type=int, default=FANOUT)
    args = parser.parse_args()

    started = time.time()
    train, valid, test = load("train.txt"), load("valid.txt"), load("test.txt")
    entities = sorted({e for triples in (train, valid, test)
                       for h, _, t in triples for e in (h, t)})
    relations = sorted({r for _, r, _ in train})
    entity_at = {name: i for i, name in enumerate(entities)}
    relation_at = {name: i for i, name in enumerate(relations)}

    # Typed adjacency, in BOTH directions. A route may traverse an edge against
    # its stated direction, and refusing that would make the graph a DAG it is
    # not -- so a reversed traversal gets its own relation id, `r + relations`,
    # rather than being conflated with the forward one.
    width = 2 * len(relations)
    out_of: dict = collections.defaultdict(list)
    for head, relation, tail in train:
        out_of[head].append((relation_at[relation], tail))
        out_of[tail].append((relation_at[relation] + len(relations), head))
    # SHUFFLED ONCE, so the cap below samples rather than taking whichever
    # triples were read first. Seeded, so the run stays reproducible.
    shuffler = random.Random(args.seed)
    for edges in out_of.values():
        shuffler.shuffle(edges)
    print(f"{len(train)} triples, {len(relations)} relations, "
          f"{width} directed relation ids")
    print(f"fan-out capped at {args.fanout}, sampled not prefixed; "
          f"mean out-degree "
          f"{sum(len(v) for v in out_of.values()) / max(len(out_of), 1):.1f}, "
          f"largest {max(len(v) for v in out_of.values())}")

    # THE PATH-TYPE COUNTS. Composition again, over relation pairs.
    paths = Composition(width, right=width, target=len(relations))
    counted = 0
    for head, relation, tail in train:
        target = relation_at[relation]
        for first, second, end in routes(out_of, head, args.fanout):
            if end == tail:
                paths.observe(first, second, target)
                counted += 1
    print(f"counted {counted} two-step routes that land where a stated "
          f"relation does ({time.time() - started:.1f}s)")

    # THE RELATION MARGINAL, the same floor as every other run.
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
    queries = random.Random(args.seed).sample(
        test, min(args.queries, len(test)))
    print(f"scoring {len(queries)} triples in both directions\n")

    statistic = STATISTICS[STATISTIC]
    # The floor, shared with every other FB15k run rather than written again.
    floor_of = Marginal(marginal, entities, relation_at, statistic)

    #: `P(asked relation | first, second)` for a path type, cached because the
    #: same pair recurs across queries constantly.
    predicts: dict = {}

    def path_weight(first, second, asked):
        key = (first, second, asked)
        if key not in predicts:
            answer = paths.surface("target", asked)
            score = min(statistic(paths.index, answer,
                                  paths.surface("left", first)),
                        statistic(paths.index, answer,
                                  paths.surface("right", second)))
            predicts[key] = float(score)
        return predicts[key]

    rows: list[dict] = []
    header = (f"{'arm':<34}{'MRR':>9}{'hits@1':>9}{'hits@10':>9}"
              f"{'margin':>9}{'sec':>8}")

    floor_ranks = []
    for head, relation, tail in queries:
        for direction in ("tail", "head"):
            given, answer = ((head, tail) if direction == "tail"
                             else (tail, head))
            _, middle, _ = ranker.rank(floor_of.vector(relation, direction),
                                       given, relation, answer, direction)
            floor_ranks.append(middle)
    floor = metrics(floor_ranks)
    rows.append(floor | {"arm": "relation only"})
    print(header)
    print("-" * len(header))
    print(f"{'relation only (the floor)':<34}{floor['mrr']:>9.4f}"
          f"{floor['hits1']:>9.4f}{floor['hits10']:>9.4f}{0.0:>+9.4f}"
          f"{time.time() - started:>8.1f}")

    for accumulate in ACCUMULATORS:
        for combine in ("paths only", *COMBINERS_SWEPT):
            began = time.time()
            ranks = []
            for head, relation, tail in queries:
                asked = relation_at[relation]
                for direction in ("tail", "head"):
                    given, answer = ((head, tail) if direction == "tail"
                                     else (tail, head))
                    vector = np.zeros(len(entities))
                    for first, second, end in routes(out_of, given, args.fanout):
                        weight = path_weight(first, second, asked)
                        if weight <= 0.0:
                            continue
                        at = entity_at[end]
                        vector[at] = (max(vector[at], weight)
                                      if accumulate == "max"
                                      else vector[at] + weight)
                    if combine != "paths only":
                        rule = COMBINERS[combine]
                        other = floor_of.vector(relation, direction)
                        top = vector.max() or 1.0
                        vector = np.array([rule(a / top, b) for a, b
                                           in zip(vector, other)])
                    _, middle, _ = ranker.rank(vector, given, relation, answer,
                                               direction)
                    ranks.append(middle)
            got = metrics(ranks) | {"arm": combine, "accumulate": accumulate,
                                    "statistic": STATISTIC, "fanout": args.fanout}
            got["margin"] = got["mrr"] - floor["mrr"]
            rows.append(got)
            label = f"{accumulate} over paths / {combine}"
            print(f"{label:<34}{got['mrr']:>9.4f}{got['hits1']:>9.4f}"
                  f"{got['hits10']:>9.4f}{got['margin']:>+9.4f}"
                  f"{time.time() - began:>8.1f}")

    # THE BLEND SWEEP. Chosen on validation, read on test, and alpha 0 is the
    # floor by construction rather than by a second implementation of it.
    def structure_vector(given, asked, accumulate):
        """The path evidence, normalised, and how CONCENTRATED it was.

        The concentration is the largest candidate's share of the total, which
        is high when the paths agree on one answer and low when they spray over
        hundreds. It is a property of the query rather than a fitted constant,
        and it is what a per-query blend weight needs: the global blend mixes
        structure in at the same strength whether or not it has anything to say,
        and the mechanism wins 7,375 queries while losing 11,302.

        Returned alongside rather than folded in, so the global arm and the
        per-query arm read the same vector and differ only in the weight.
        """
        vector = np.zeros(len(entities))
        for first, second, end in routes(out_of, given, args.fanout):
            weight = path_weight(first, second, asked)
            if weight <= 0.0:
                continue
            at = entity_at[end]
            vector[at] = (max(vector[at], weight) if accumulate == "max"
                          else vector[at] + weight)
        top, total = vector.max(), vector.sum()
        if top <= 0.0:
            return vector, 0.0
        return vector / top, float(top / total)

    def sweep(triples, accumulate):
        """Every alpha's per-query ranks over one split, from one pass.

        The ranks are kept rather than reduced to an MRR immediately, because
        **the margin is a difference between two arms scored on the SAME
        queries** and its error bar is the paired one. Reducing first throws
        away the pairing and leaves the difference to be eyeballed against a
        sample size.
        """
        totals = {alpha: [] for alpha in ALPHAS}
        totals.update({("per query", alpha): [] for alpha in ALPHAS})
        #: Per scored query: did ANY path reach the true answer? This splits the
        #: losses into two different failures. Where the answer was never
        #: reached the structure can only push other candidates above it, so the
        #: blend is pure harm and no weighting can rescue it -- a convex blend
        #: and an additive bonus rank identically, so "do not penalise what was
        #: not reached" is not available as a repair. Where it WAS reached and
        #: still lost, the ranking is wrong and that is a different problem.
        totals["reached"] = []
        for head, relation, tail in triples:
            asked = relation_at[relation]
            for direction in ("tail", "head"):
                given, answer = ((head, tail) if direction == "tail"
                                 else (tail, head))
                structure, concentration = structure_vector(given, asked,
                                                            accumulate)
                other = floor_of.vector(relation, direction)
                totals["reached"].append(
                    bool(structure[ranker.at[answer]] > 0.0))
                for alpha in ALPHAS:
                    for weight, key in ((alpha, alpha),
                                        (alpha * concentration,
                                         ("per query", alpha))):
                        _, middle, _ = ranker.rank(
                            weight * structure + (1.0 - weight) * other,
                            given, relation, answer, direction)
                        totals[key].append(middle)
        return totals

    def paired(ranks, floor_ranks):
        """`(mean gain, standard error, better, worse)` per query.

        A margin of 0.0124 over 5,000 queries is either a result or it is the
        noise of a difference nobody bounded, and only this can say which.
        """
        gains = np.array([1.0 / a - 1.0 / b
                          for a, b in zip(ranks, floor_ranks)])
        error = float(gains.std(ddof=1) / np.sqrt(len(gains))) if len(
            gains) > 1 else float("inf")
        return (float(gains.mean()), error,
                int((gains > 0).sum()), int((gains < 0).sum()))

    held = random.Random(args.seed).sample(
        valid, min(args.queries // 2, len(valid)))
    print(f"\nBLEND SWEEP. alpha 0 is the floor exactly. Chosen on "
          f"{len(held)} validation triples, read on test.")
    for accumulate in ACCUMULATORS:
        valid_ranks = sweep(held, accumulate)
        reached = valid_ranks.pop("reached")
        on_valid = {a: metrics(r)["mrr"] for a, r in valid_ranks.items()}
        test_ranks = sweep(queries, accumulate)
        reached = test_ranks.pop("reached")
        on_test = {a: metrics(r)["mrr"] for a, r in test_ranks.items()}
        # THE TWO WEIGHTINGS ARE CHOSEN SEPARATELY on validation and reported
        # side by side. Sharing one alpha would compare them at a setting picked
        # for the other, which is the untuned-baseline mistake in miniature.
        for label, keys in (("global", list(ALPHAS)),
                            ("per query", [("per query", a) for a in ALPHAS])):
            chosen = max(keys, key=lambda key: on_valid[key])
            gain, error, better, worse = paired(test_ranks[chosen],
                                                test_ranks[0.0])
            alpha = chosen[1] if isinstance(chosen, tuple) else chosen
            rows.append({"arm": "blend", "weighting": label,
                         "accumulate": accumulate, "chosen_alpha": alpha,
                         "test": {str(k): v for k, v in on_test.items()},
                         "margin": on_test[chosen] - on_test[0.0],
                         "paired_gain": gain, "standard_error": error,
                         "better": better, "worse": worse,
                         # Kept only until the stratification below has read
                         # them, then deleted -- 40,000 ranks per alpha is a
                         # working value and not a record.
                         "_test_ranks": test_ranks, "_chosen": chosen})
            print(f"  {accumulate:>4} over paths, {label:<9} weight: "
                  f"validation picks alpha {alpha}, test MRR "
                  f"{on_test[chosen]:.4f} against the floor's "
                  f"{on_test[0.0]:.4f}  margin "
                  f"{on_test[chosen] - on_test[0.0]:+.4f}")
            print(f"       PAIRED on the same queries: {gain:+.4f} +/- "
                  f"{error:.4f} (one standard error), {better} better and "
                  f"{worse} worse")
            # SPLIT BY WHETHER ANY PATH REACHED THE TRUE ANSWER. Two different
            # failures hide inside one loss count.
            for label, want in (("answer reached", True),
                                ("never reached", False)):
                at = [i for i, got in enumerate(reached) if got is want]
                if not at:
                    continue
                blended = metrics([test_ranks[chosen][i] for i in at])
                base = metrics([test_ranks[0.0][i] for i in at])
                rows.append({"arm": "reached", "weighting": label,
                             "accumulate": accumulate, "n": len(at),
                             "floor": base["mrr"], "blend": blended["mrr"],
                             "margin": blended["mrr"] - base["mrr"]})
                print(f"         {label:<15} n={len(at):>6}  floor "
                      f"{base['mrr']:.4f}  blend {blended['mrr']:.4f}  margin "
                      f"{blended['mrr'] - base['mrr']:+.4f}")
        print("       test by alpha, global:    "
              + "  ".join(f"{a}:{on_test[a]:.4f}" for a in ALPHAS))
        print("       test by alpha, per query: "
              + "  ".join(f"{a}:{on_test[('per query', a)]:.4f}"
                          for a in ALPHAS))

    # IS THE MARGIN JUST POPULARITY? The floor is a popularity ranking, so a
    # gain concentrated on the answers that are already common would be the
    # marginal being reinforced rather than structure being added. Split by how
    # many training triples the ANSWER appears in and read the margin per band.
    #
    # The idea is PROBE's (arXiv 2606.08921), which evaluates knowledge-graph
    # completion in a popularity-aware way. Its own weighting is not
    # reimplemented here: the paper has smoothing constants this run has not
    # read, and a metric named after a paper nobody opened is the borrowed claim
    # CLAUDE.md puts first. A stratification needs no constants.
    degree: dict = collections.Counter()
    for head, _, tail in train:
        degree[head] += 1
        degree[tail] += 1
    for accumulate in ACCUMULATORS:
        row = next(r for r in rows
                   if r["arm"] == "blend" and r["accumulate"] == accumulate
                   and r["weighting"] == "per query")
        chosen = row["_chosen"]
        bands: dict = {}
        for index, (head, relation, tail) in enumerate(queries):
            asked = relation_at[relation]
            for offset, direction in enumerate(("tail", "head")):
                given, answer = ((head, tail) if direction == "tail"
                                 else (tail, head))
                at = 2 * index + offset
                popularity = degree[answer]
                band = ("rare (<10)" if popularity < 10 else
                        "middling (10-49)" if popularity < 50 else
                        "common (50+)")
                bands.setdefault(band, []).append(at)
        print(f"\n  {accumulate} over paths, margin by how common the ANSWER is:")
        for band in ("rare (<10)", "middling (10-49)", "common (50+)"):
            at = bands.get(band, [])
            if not at:
                continue
            blended = metrics([row["_test_ranks"][chosen][i] for i in at])
            base = metrics([row["_test_ranks"][0.0][i] for i in at])
            print(f"    {band:<18} n={len(at):>6}  floor {base['mrr']:.4f}  "
                  f"blend {blended['mrr']:.4f}  margin "
                  f"{blended['mrr'] - base['mrr']:+.4f}")
            rows.append({"arm": "band", "accumulate": accumulate, "band": band,
                         "n": len(at), "floor": base["mrr"],
                         "blend": blended["mrr"],
                         "margin": blended["mrr"] - base["mrr"]})
    # CLEARED ONCE, AFTER EVERY ACCUMULATOR HAS READ THEM. Clearing inside the
    # loop emptied the second accumulator's ranks before its own bands ran, and
    # the run died on the last line of a fifty-minute job.
    for other in rows:
        other.pop("_test_ranks", None)
        other.pop("_chosen", None)

    # THE BLEND ROWS ARE THE ARM. Reporting the best FIXED combiner as "best"
    # printed a negative margin directly under a positive one and invited the
    # wrong line to be quoted; the fixed combiners are a swept axis that lost.
    blends = [row for row in rows if row["arm"] == "blend"]
    best = max(blends, key=lambda row: row["margin"])
    fixed = max((row for row in rows
                 if row["arm"] not in ("relation only", "blend", "band")),
                key=lambda row: row["mrr"])
    print(f"\nBest arm: {best['accumulate']} over paths, "
          f"{best['weighting']} weight at alpha {best['chosen_alpha']} - "
          f"margin {best['margin']:+.4f} +/- {best['standard_error']:.4f}, "
          f"{best['better']} better and {best['worse']} worse")
    print(f"Best FIXED combiner, which is the axis that lost: {fixed['mrr']:.4f} "
          f"({fixed['accumulate']} over paths, {fixed['arm']}) - margin "
          f"{fixed['margin']:+.4f}")
    print("Published, against this same floor:")
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
