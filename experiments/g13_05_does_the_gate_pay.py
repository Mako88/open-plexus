"""Does gating search on ambiguity beat searching everywhere?

g13-03 measured search as a wash overall -- +0.008 +/-0.018 against a greedy
walk -- while gaining +0.092 where the queried subject holds several relations
and losing 0.054 where it holds one. g13-04 measured the decode margin
separating those two cases at AUC 0.803 at width >= 128.

So a gate should keep the gain and give back the loss. This measures whether it
does.

## The threshold, which is the honest problem

AUC measures separability across ALL thresholds. A gate needs ONE, and choosing
it by trying values on the test set would be **fitting a number rather than
measuring one** -- the failure this project has a rule about and has committed
anyway (g11-05 swept a range chosen after seeing where the answer was).

**The threshold here is a QUANTILE OF THE TRAINING MARGINS**, computed after
training and before any test sequence is touched. It uses no labels at all: it
asks only "how wide is a typical margin for this model", which is a property of
the trained model rather than of the answers.

The quantile is the swept axis, so the sensitivity is visible rather than
assumed. A gate that only works at one quantile is a tuned constant.

## The arms

    walk       search_branches=1                    no search at all
    search4    search_branches=4                    search everywhere
    gate-q25   branch where margin < 25th pct of training margins
    gate-q50   ... 50th
    gate-q75   ... 75th

**The number to beat is `search4`, not `walk`.** A gate that merely matches
search-everywhere has bought compute savings and no accuracy -- worth having, and
not what this was for.

## PREDICTIONS (registered before running)

  P1  At least one gate arm beats `search4` overall. If none does, the +0.092 /
      -0.054 split is not separable by this signal in practice, whatever the AUC
      said in isolation.
  P2  Every gate arm beats `walk`. Gating cannot be worse than not searching,
      because at worst it searches nowhere and reduces to `walk`.
  P3  The best gate arm searches on fewer than half the positions. The measured
      out-degree mix is about 60/40, so a gate that fires everywhere has not
      gated anything.
  P4  gate-q25 searches least and gate-q75 most, monotonically. The rail: if the
      quantile does not order the firing rate, the threshold is not doing what
      its name says.
  P5  The gate's gain over `search4` is smaller than 0.03 -- the perfect-gate
      figure from g13-03's split -- because AUC 0.803 is not 1.000.

P1 is the decision. P4 is the rail that says the mechanism is wired up at all.

COST: 5 arms x 8 seeds at width 256 = 40 cells. Estimated from the MOST
EXPENSIVE arm, `search4`, which walks four branches at every answerable
position; the gate arms are strictly cheaper because they walk one branch
wherever they do not fire. Printed by `--cost`.

MEASURED ON: `openplexus/tasks/kinship.py`, hops 2, 12 people, 10 facts,
width 256 -- where g13-04 measured the signal at AUC 0.858. It is weak at 64 and
that is a scale property, registered in docs/SCALE.md.
"""

from __future__ import annotations

import argparse
import json
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
    IGNORE, KinshipConfig, dataset, shortcut_floors)

WIDTH = 256
N_TRAIN, N_TEST, EPOCHS = 400, 200, 4
SEEDS = tuple(range(8))
BRANCHES = 4

#: arm -> the training-margin quantile its threshold comes from, or None for an
#: ungated arm. `walk` is branches=1 and never searches.
ARMS = {
    "walk": None,
    "search4": None,
    "gate-q25": 0.25,
    "gate-q50": 0.50,
    "gate-q75": 0.75,
}


def build(task: KinshipConfig, seed: int, branches: int,
          threshold: float | None) -> LocalAssociativeMemory:
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=task.vocab_size, seed=seed, hops=2,
        hop_accumulate="concat", derived_keys=True, context_keys=True,
        search_branches=branches, search_fact_token=task.fact_token,
        search_query_token=task.query_token,
        search_gate_margin=threshold))


def training_margins(model: LocalAssociativeMemory, data) -> list[float]:
    """Decode margins over the TRAINING sequences, for setting a threshold.

    Read after training and before any test sequence is touched. Uses no labels:
    it asks how wide a typical margin is for this model, which is a property of
    the trained model rather than of the answers.
    """
    margins = []
    for sequence in data:
        trace: list[dict] = []
        model.run(np.array(sequence.tokens, dtype=np.int64), trace=trace)
        at = [e for e in trace
              if e.get("position") == sequence.answer_position]
        if at:
            margins.append(at[0]["search_decode_margin"])
    return margins


def quantile(values: list[float], fraction: float) -> float:
    ordered = sorted(values)
    index = min(len(ordered) - 1, int(fraction * len(ordered)))
    return ordered[index]


def evaluate(model, data, gated: bool) -> dict:
    buckets: dict[str, list[int]] = {"1": [], "2+": []}
    hits = 0
    fired = 0
    counted = 0
    for sequence in data:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        trace: list[dict] = [] if gated else None
        predicted = (model.run(tokens, trace=trace) if gated
                     else model.run(tokens))
        correct = int(predicted[sequence.answer_position]
                      == sequence.targets[sequence.answer_position])
        hits += correct
        degree = sum(1 for s, _, _ in sequence.facts
                     if s == sequence.asked[0])
        buckets["1" if degree <= 1 else "2+"].append(correct)
        if gated:
            at = [e for e in trace
                  if e.get("position") == sequence.answer_position]
            if at:
                counted += 1
                if at[0]["search_decode_margin"] < model.config.search_gate_margin:
                    fired += 1
    return {
        "accuracy": hits / len(data),
        "by_out_degree": {
            k: {"n": len(v), "accuracy": (sum(v) / len(v)) if v else None}
            for k, v in buckets.items()},
        # How often the gate chose to branch. A gate that fires everywhere is
        # `search4` wearing a threshold; one that never fires is `walk`.
        "fired": (fired / counted) if counted else None,
    }


def one_cell(arm: str, seed: int) -> dict:
    task = KinshipConfig(hops=2, seed=seed * 100_000)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 500_000), N_TEST)
    fraction = ARMS[arm]
    branches = 1 if arm == "walk" else BRANCHES

    started = time.time()
    # Trained WITHOUT the gate in every arm, so the threshold is chosen for a
    # model that already exists rather than shaping how it learned. That keeps
    # the arms' training identical and makes the gate purely an inference-time
    # decision, which is what a deployed system would be choosing.
    model = build(task, seed, branches, None)
    for _ in range(EPOCHS):
        for sequence in train:
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            model.run(tokens, targets, targets != IGNORE, learn=True)
    trained = time.time() - started

    threshold = None
    if fraction is not None:
        threshold = quantile(training_margins(model, train), fraction)
        model.config = replace(model.config, search_gate_margin=threshold)

    result = evaluate(model, test, gated=fraction is not None)
    result.update(
        arm=arm, width=WIDTH, seed=seed, quantile=fraction,
        threshold=threshold, train_seconds=round(trained, 1),
        floors=shortcut_floors(task),
        condition=(f"{arm}|d{WIDTH}|seed{seed}|branches{branches}"
                   f"|train{N_TRAIN}x{EPOCHS}|test{N_TEST}"))
    return result


def cost_probe() -> None:
    task = KinshipConfig(hops=2, seed=0)
    sample = dataset(task, 20)
    model = build(task, 0, BRANCHES, None)
    started = time.time()
    for sequence in sample:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        model.run(tokens, targets, targets != IGNORE, learn=True)
    per = (time.time() - started) / len(sample)
    print(f"most expensive arm: search4 at width {WIDTH}")
    print(f"  {per * 1000:.1f} ms per training sequence")
    print(f"  {per * N_TRAIN * EPOCHS / 60:.1f} min to train one cell")
    print(f"  5 arms per job, worst job "
          f"~{per * N_TRAIN * EPOCHS * 5 / 60:.0f} min if every arm were this "
          f"one -- the gate arms are strictly cheaper")


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
    records = [one_cell(arm, seed) for seed in seeds for arm in ARMS]

    for record in records:
        by = record["by_out_degree"]
        fired = ("--" if record["fired"] is None
                 else f"{record['fired']:.0%}")
        print(f"{record['condition']}  overall {record['accuracy']:.3f}  "
              f"fired {fired}  "
              + "  ".join(f"k={k} {d['accuracy']:.3f}" for k, d in by.items()))

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


if __name__ == "__main__":
    main()
