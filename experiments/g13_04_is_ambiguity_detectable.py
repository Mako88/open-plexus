"""Can the model tell, before searching, whether searching would help?

g13-03 measured search doing exactly what it was built for and damaging the case
it was not:

    search4 - walk at out-degree 1       -0.054
    search4 - walk at out-degree >= 2    +0.092

The test set is about half of each, so they cancel and the overall gain is
+0.008 -- inside 2 SE. **The tie is not evidence against search. It is evidence
that search should not run unconditionally.**

A gate would keep the +0.092 and give back the -0.054. It needs to decide BEFORE
walking, from something the model can actually see.

## The candidate, and why it is not just another confidence heuristic

`search.decode_margin` is the gap between the top two candidates of the first
decode -- `key(FACT, S)` read and scored against every token's value vector.

**This is not a confidence signal in decision 93's sense**, and the distinction
is the whole reason to expect anything. 93 measured norm, entropy, peak, gap and
kurtosis of a retrieval and found the best linear separator over all five,
*fitted with the labels*, reaching 0.628 against 0.500 for guessing -- those ask
"does this retrieval feel reliable". The margin asks something structural: when
one relation is bound to a key the decode is peaked, and when several are they
compete. It reads the superposition rather than guessing at it.

That is an argument, and this project's record on arguments is poor, which is why
it is being measured before a gate is built rather than after.

## What is measured

Accuracy is not measured here at all. For each test sequence the model runs with
a `trace`, the entry at the answer position is read, and its
`search_decode_margin` is compared against the queried subject's true out-degree
-- which is task metadata, exactly the thing a running system CANNOT see. The
margin is the proxy; the out-degree is the label it is scored against.

Reported as **AUC**: the probability that a random out-degree-1 sequence shows a
wider margin than a random out-degree-2+ one. 0.500 is chance.

`search_endpoint_margin` is recorded alongside as the rival signal -- the gap
between the best and second-best walk. It is strictly better informed and costs
the walks, so it can only be used by a gate that has already paid. If it is not
clearly better, the cheap signal wins on cost alone.

## PREDICTIONS (registered before running)

  P1  The decode margin separates out-degree 1 from 2+ at AUC > 0.75, well clear
      of decision 93's 0.628 for identity-free confidence signals.
  P2  The median margin at out-degree 1 is more than twice the median at 2+.
      Medians rather than means: one badly-behaved sequence should not carry it.
  P3  The endpoint margin does NOT beat the decode margin by more than 0.05 AUC.
      If it does, the expensive signal is worth its cost and a gate should be
      built the other way round -- search first, then decide whether to trust it.
  P4  Separation holds at every width tested. If it is a width artefact it is
      not a mechanism.
  P5  AUC stays below 0.95. The margin is a proxy for a structural property it
      cannot fully observe, and a near-perfect score would suggest the label is
      leaking into the measurement rather than being predicted.

P1 is the decision: below 0.75 the gate is not worth building on this signal.

COST: 3 widths x 8 seeds = 24 cells. No search sweep arms -- each cell trains one
model and then runs inference with a trace, which is the same shape as g13-01 and
measured there at 0.6 min per cell for the dearest. Printed by `--cost`.

MEASURED ON: `openplexus/tasks/kinship.py`, hops 2, 12 people, 10 facts.
"""

from __future__ import annotations

import argparse
import json
import random
import statistics
import sys
import time
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.kinship import (  # noqa: E402
    IGNORE, KinshipConfig, dataset)

WIDTHS = (64, 128, 256)
N_TRAIN, N_TEST, EPOCHS = 400, 200, 4
SEEDS = tuple(range(8))
BRANCHES = 4

#: Pairs drawn to estimate AUC. Enough that the estimate's own noise is far
#: below the effect being measured, and cheap.
AUC_PAIRS = 20_000


def build(width: int, task: KinshipConfig, seed: int):
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=width, vocab_size=task.vocab_size, seed=seed, hops=2,
        hop_accumulate="concat", derived_keys=True, context_keys=True,
        search_branches=BRANCHES, search_fact_token=task.fact_token,
        search_query_token=task.query_token))


def auc(wider: list[float], narrower: list[float], seed: int) -> float:
    """P(a random `wider` exceeds a random `narrower`). 0.5 is chance.

    Sampled rather than computed exactly because the exact form is quadratic in
    the group sizes and this is called six times per cell. Ties count as half,
    which is what a rank statistic does and what stops a signal that is constant
    everywhere from scoring 0 or 1.
    """
    if not wider or not narrower:
        return float("nan")
    rng = random.Random(seed)
    hits = 0.0
    for _ in range(AUC_PAIRS):
        a, b = rng.choice(wider), rng.choice(narrower)
        hits += 1.0 if a > b else (0.5 if a == b else 0.0)
    return hits / AUC_PAIRS


def one_cell(width: int, seed: int) -> dict:
    task = KinshipConfig(hops=2, seed=seed * 100_000)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 500_000), N_TEST)
    model = build(width, task, seed)

    started = time.time()
    for _ in range(EPOCHS):
        for sequence in train:
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            model.run(tokens, targets, targets != IGNORE, learn=True)
    trained = time.time() - started

    groups: dict[str, dict[str, list[float]]] = {
        "decode": {"one": [], "many": []},
        "endpoint": {"one": [], "many": []},
    }
    for sequence in test:
        trace: list[dict] = []
        model.run(np.array(sequence.tokens, dtype=np.int64), trace=trace)
        # The search entries carry `position`; the older gate-signal entries do
        # not, so this filters by presence rather than by index.
        at = [e for e in trace
              if e.get("position") == sequence.answer_position]
        if not at:
            continue
        degree = sum(1 for s, _, _ in sequence.facts
                     if s == sequence.asked[0])
        bucket = "one" if degree <= 1 else "many"
        groups["decode"][bucket].append(at[0]["search_decode_margin"])
        groups["endpoint"][bucket].append(at[0]["search_endpoint_margin"])

    result: dict = {
        "arm": "margin", "width": width, "seed": seed,
        "train_seconds": round(trained, 1),
        "condition": (f"margin|d{width}|seed{seed}|branches{BRANCHES}"
                      f"|train{N_TRAIN}x{EPOCHS}|test{N_TEST}"),
    }
    for signal, buckets in groups.items():
        one, many = buckets["one"], buckets["many"]
        result[signal] = {
            "n_one": len(one), "n_many": len(many),
            "median_one": statistics.median(one) if one else None,
            "median_many": statistics.median(many) if many else None,
            "auc": auc(one, many, seed),
        }
    return result


def cost_probe() -> None:
    width = max(WIDTHS)
    task = KinshipConfig(hops=2, seed=0)
    sample = dataset(task, 20)
    model = build(width, task, 0)
    started = time.time()
    for sequence in sample:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        model.run(tokens, targets, targets != IGNORE, learn=True)
    per = (time.time() - started) / len(sample)
    print(f"most expensive cell: width {width}")
    print(f"  {per * 1000:.1f} ms per training sequence")
    print(f"  {per * N_TRAIN * EPOCHS / 60:.1f} min to train one cell")
    print(f"  3 cells per job, worst job "
          f"~{per * N_TRAIN * EPOCHS * 3 / 60:.0f} min")


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
    records = [one_cell(width, seed) for seed in seeds for width in WIDTHS]

    for record in records:
        decode, endpoint = record["decode"], record["endpoint"]
        print(f"{record['condition']}  decode AUC {decode['auc']:.3f} "
              f"(med {decode['median_one']:.3f} vs {decode['median_many']:.3f}, "
              f"n {decode['n_one']}/{decode['n_many']})  "
              f"endpoint AUC {endpoint['auc']:.3f}")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


if __name__ == "__main__":
    main()
