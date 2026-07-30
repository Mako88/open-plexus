"""What the beam's per-hop rendezvous is worth, against what it costs in round trips.

## The question

Note 101 found the driver-free read path misses `d_max` past depth 7, and located the
remaining factor of two: `PairKeys.owner` sends a hop's look-up and the next hop's follow
to the **same concept**, so a walk that MIGRATED would visit one peer per hop instead of
returning to the caller between the two. The obstacle it named is **pruning** — ranking
all `width` partial walks against each other is a meeting, and the caller is where that
meeting happens today.

So the question is not whether migration is faster. It is **whether the meeting is
load-bearing**, and how often it has to happen.

    prune_every = 1     meet every hop. What every number to date was taken under
    prune_every = k     meet every k hops; between them each walk keeps its own top
                        `branches`, so the population grows by `branches` per hop
    prune_every = 0     never meet. `width` independent greedy walks, one rendezvous
                        at the very end to compare endpoints

## The round-trip arithmetic these are priced against

A migrating walk pays a one-way peer-to-peer hop, half a round trip, because the look-up
and the next follow are the same peer. A rendezvous pays a full round trip: back to the
caller and out again.

    rounds ~ depth/2 + depth/k        at 50 ms RTT, depth 10, d_max 640 ms

        k = 1   5 + 10 = 15 RTT   750 ms   OVER
        k = 2   5 +  5 = 10 RTT   500 ms   fits
        k = 5   5 +  2 =  7 RTT   350 ms   fits
        k = 0   5 +  1 =  6 RTT   300 ms   fits

**Even meeting every hop is faster migrated than the 1,000 ms note 101 measured** — and
still misses. So the accuracy question only matters from `k = 2` up.

## What is NOT settled here

This prices migration; it does not implement it. The arithmetic above is a design
estimate, not a measurement, and `tools/walk_rounds.py` counts rounds for the path that
actually exists. The bytes are also not counted: an unpruned hop reads
`population x 2` pairs, so `k = 2` moves four times the data per hop.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT / "tools"))

import clutrr_recovery as cr  # noqa: E402
import walk_rounds as wr  # noqa: E402

from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.search import beam  # noqa: E402
from openplexus.tasks.clutrr import (  # noqa: E402
    FACT, RELATIONS, ClutrrConfig, load)

#: `d_max` from C2, milliseconds.
D_MAX_MS = 640.0
#: Periods measured. `1` is the baseline every existing number was taken under and `0`
#: is the floor -- no meeting at all -- so the interesting arms are bracketed rather
#: than reported on their own.
PERIODS = (1, 2, 3, 5, 0)


def migrated_rounds(depth: int, period: int) -> float:
    """Round trips for a MIGRATING walk that meets every `period` hops.

    Half a round trip per hop, because the look-up and the next follow are the same
    peer; one full round trip per meeting. Kept as a function so the arithmetic has one
    home and the docstring's table cannot drift from what is printed.
    """
    meetings = 1.0 if period == 0 else float(depth) / period
    return depth / 2.0 + meetings


def measure(config: ClutrrConfig, seed: int, width: int, beam_width: int,
            branches: int, periods=PERIODS):
    """Chain recovery per prune period, plus the widest population each reached."""
    puzzles = load(config)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=width, seed=seed,
        context_keys=True, derived_keys=True, decay=1.0))
    allowed = np.arange(config.relation_base,
                        config.relation_base + len(RELATIONS))
    hits = dict.fromkeys(periods, 0)
    #: READS as well as accuracy. Skipping a rendezvous lets the population grow by
    #: `branches`, so an unpruned hop reads more -- and reporting only the accuracy
    #: column would be exactly the one-axis mistake note 100 exists to correct.
    reads = dict.fromkeys(periods, 0)
    scored = 0
    for puzzle in puzzles:
        chain = cr.true_chain(puzzle, config)
        if chain is None:
            continue
        scored += 1
        model.run(np.asarray(puzzle.tokens))
        subject = int(puzzle.tokens[puzzle.query_position - 1])
        target = model.wv[int(puzzle.tokens[puzzle.query_position])]
        for period in periods:
            # `walk_rounds.Counting` is the reader that counts, reused rather than
            # rewritten -- it already answers from a matrix and separates reads from
            # rounds, which is the whole distinction being measured.
            counting = wr.Counting(model._final, model.retrieval, model.key_source)
            walks = beam(None, model.retrieval, model.key_source, model.wv,
                         FACT, subject, target, len(chain), width=beam_width,
                         branches=branches, allowed=allowed, prune_every=period,
                         reader=counting)
            hits[period] += bool(walks) and walks[0].relations == chain
            reads[period] += counting.reads
    return ({period: hits[period] / scored for period in periods},
            {period: reads[period] / scored for period in periods}, scored)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT / "data" / "clutrr")
    parser.add_argument("--config", default="gen_train23_test2to10")
    parser.add_argument("--width", type=int, default=64)
    parser.add_argument("--beam-width", type=int, default=4)
    parser.add_argument("--branches", type=int, default=4)
    parser.add_argument("--seeds", type=int, nargs="+", default=[0, 1, 2])
    parser.add_argument("--depth", type=int, default=10,
                        help="depth the round-trip column is priced at")
    parser.add_argument("--rtt", type=float, default=50.0)
    args = parser.parse_args()

    # `root` is the data directory and `ClutrrConfig` appends the config name itself,
    # matching `clutrr_recovery.py` and `generation_delta.py` rather than inventing a
    # third convention.
    config = ClutrrConfig(root=args.root, split="test", layout="kinship")
    per_seed, per_seed_reads = [], []
    for seed in args.seeds:
        got, costs, scored = measure(config, seed, args.width, args.beam_width,
                                     args.branches)
        per_seed.append(got)
        per_seed_reads.append(costs)
        print(f"seed {seed}: {scored} puzzles scored")

    print(f"\nwidth {args.beam_width}, branches {args.branches}, "
          f"{len(args.seeds)} seeds. Round trips priced for a MIGRATING walk at "
          f"depth {args.depth}, RTT {args.rtt:.0f} ms, d_max {D_MAX_MS:.0f} ms\n")
    print(f"{'prune_every':>12s} {'recovery':>9s} {'sd':>7s} {'vs k=1':>8s} "
          f"{'reads':>7s} {'vs k=1':>7s} {'rounds':>7s} {'latency':>9s}  verdict")
    baseline = float(np.mean([got[1] for got in per_seed]))
    base_reads = float(np.mean([got[1] for got in per_seed_reads]))
    for period in PERIODS:
        values = np.array([got[period] for got in per_seed])
        cost = float(np.mean([got[period] for got in per_seed_reads]))
        rounds = migrated_rounds(args.depth, period)
        latency = rounds * args.rtt
        label = "never" if period == 0 else str(period)
        print(f"{label:>12s} {values.mean():9.4f} {values.std():7.4f} "
              f"{values.mean() - baseline:+8.4f} {cost:7.1f} "
              f"{cost / base_reads:6.2f}x {rounds:7.1f} {latency:8.0f}ms  "
              f"{'fits' if latency <= D_MAX_MS else 'OVER d_max'}")
    print("\n`vs k=1` is the accuracy the rendezvous buys. If it is near zero at a "
          "period\nwhose latency fits, the meeting is not load-bearing and a "
          "migrating walk is free.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
