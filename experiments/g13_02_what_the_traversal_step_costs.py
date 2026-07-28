"""What is step 2 of the traversal actually worth, and does search now pay?

**The whole case for building a traversal rests on one number that has no
reproducible source.** Decision 107 measured the three steps by hand:

    step                      chained   given a perfect input
    1  key(FACT,S) -> R1        0.710                   0.710
    2  key(S,R1)   -> M         0.703                   0.960
    3  key(FACT,M) -> R2        0.497                   0.677

and derived the decision from the arithmetic: *"take the two entity-lookup steps
to the 0.95 that step 2 already reaches and the product is 0.87."* That 0.87 is
why traversal-plus-search is worth building at all, and **0.960 came from an
inline probe that left no script behind** -- the same gap that left the entire
relational line without a committed instrument until g13-01.

## What changed, and why this is now the gating measurement

Decision 111 refused search because *"you cannot search your way out of noisy
primitives, because the verifier is built from the primitives."* g13-01 measured
the primitive at **1.000 +/-0.000 at out-degree 1**, so that refusal has expired.

Search resolves the ambiguity at steps 1 and 3 by using the disambiguator the
question already contains -- it names the object. Try a candidate relation,
follow it, and check where it lands. **Every one of those follow-and-check
operations IS step 2**, so step 2's accuracy is simultaneously the traversal's
cost and the verifier's reliability. If it is 0.96 the build is justified; if it
is 0.70 the verifier is as unreliable as the thing it verifies and decision 111's
objection survives in a new form.

## What is measured

`generate_object_question` asks `S R -> ?`, which is step 2 end-to-end through
`model.run()`. A fact is laid out `FACT S R O` and the store binds the previous
position's key to the current position's value, so the write at `O` binds
`key(S, R)`; the question ends `... S R` to read exactly that binding. No
retrieval probe -- same discipline as g13-01, for the same reason.

Split by **how many stated facts share the asked `(S, R)` pair**. `generate`
rejects a repeated `(subject, object)` but nothing stops a subject holding one
relation to two people, so that ambiguity is real, left in, and counted.

## PREDICTIONS (registered before running)

  P1  CONTROL. Step 2 at width 64, over all sequences, lands near decision 107's
      0.960 -- inside 0.10 of it. If it does not, that number does not reproduce
      and the 0.87 ceiling it justifies goes with it.
  P2  At a UNIQUE (S, R) pair, step 2 clears 0.99 at every width, the way step 1
      did at out-degree 1. The pair key is the mechanism decision 104 built and
      it should be clean when nothing collides on it.
  P3  Where several facts share (S, R), accuracy falls toward 1/m -- the same
      ambiguity decision 108 found, arriving on the traversal step.
  P4  Step 2 gains less than 0.03 from width 64 to 256, as step 1 gained 0.020.
      Width is not the axis; g13-01 established that and this should agree.
  P5  The compounded traversal-with-search ceiling -- step 1 at out-degree 1,
      times step 2 at a unique pair, times step 3 at out-degree 1 -- clears
      0.87, the figure decision 107 derived by hand.

P1 is the control on the RECORD rather than on the instrument. P5 is the
decision: it either justifies building traversal-plus-search or it does not.

COST: 3 widths x 8 seeds = 24 cells, one arm. Estimated from the MOST EXPENSIVE
cell (width 256): the store is d x d and the per-step work is a matvec, so width
256 is SIXTEEN times width 64. g13-01 measured the same shape at 21.0 ms per
training sequence and 0.6 min per cell; this arm is cheaper, having one hop
rather than two. Printed by `--cost` before dispatch.

MEASURED ON: `openplexus/tasks/kinship.py`, `generate_object_question`, 12
people, 10 facts, hops 2 so the asked relation is the first of a real path.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from collections import Counter
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.kinship import (  # noqa: E402
    IGNORE, KinshipConfig, object_dataset)

WIDTHS = (64, 128, 256)
N_TRAIN, N_TEST, EPOCHS = 400, 200, 4
SEEDS = tuple(range(8))

#: Pair keys and derived keys, which is what makes `key(S, R)` a binding at all.
#: One hop: this measures a single retrieval, not composition.
CONFIG = dict(hops=1, derived_keys=True, context_keys=True)


def sharing(sequence) -> int:
    """How many stated facts share the asked `(subject, relation)` pair.

    1 means the pair key names exactly one object and the retrieval is
    determined. More means several objects were bound to the same key and their
    sum is what comes back -- decision 108's ambiguity, on the traversal step.
    """
    subject, _ = sequence.asked
    relation = sequence.path[0]
    return sum(1 for s, r, _ in sequence.facts
               if s == subject and r == relation)


def evaluate(model: LocalAssociativeMemory, data) -> dict:
    buckets: dict[str, list[int]] = {"unique": [], "shared": []}
    hits = 0
    shares: Counter = Counter()
    for sequence in data:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        predicted = model.run(tokens)
        correct = int(predicted[sequence.answer_position]
                      == sequence.targets[sequence.answer_position])
        hits += correct
        m = sharing(sequence)
        shares[m] += 1
        buckets["unique" if m == 1 else "shared"].append(correct)
    return {
        "accuracy": hits / len(data),
        "by_sharing": {
            k: {"n": len(v), "accuracy": (sum(v) / len(v)) if v else None}
            for k, v in buckets.items()},
        "share_counts": dict(sorted(shares.items())),
    }


def one_cell(width: int, seed: int) -> dict:
    task = KinshipConfig(hops=2, seed=seed * 100_000)
    train = object_dataset(task, N_TRAIN)
    test = object_dataset(replace(task, seed=task.seed + 500_000), N_TEST)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=width, vocab_size=task.vocab_size, seed=seed, **CONFIG))

    started = time.time()
    for _ in range(EPOCHS):
        for sequence in train:
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            model.run(tokens, targets, targets != IGNORE, learn=True)
    trained = time.time() - started

    result = evaluate(model, test)
    result.update(
        arm="step2-object", width=width, seed=seed,
        train_seconds=round(trained, 1),
        condition=(f"step2|d{width}|seed{seed}|train{N_TRAIN}x{EPOCHS}"
                   f"|test{N_TEST}"))
    return result


def cost_probe() -> None:
    """Time the most expensive cell. Prints, measures nothing."""
    width = max(WIDTHS)
    task = KinshipConfig(hops=2, seed=0)
    sample = object_dataset(task, 20)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=width, vocab_size=task.vocab_size, seed=0, **CONFIG))
    started = time.time()
    for sequence in sample:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        model.run(tokens, targets, targets != IGNORE, learn=True)
    per_sequence = (time.time() - started) / len(sample)
    train_cost = per_sequence * N_TRAIN * EPOCHS
    print(f"most expensive cell: width {width}")
    print(f"  {per_sequence * 1000:.1f} ms per training sequence")
    print(f"  {train_cost / 60:.1f} min to train one cell "
          f"({N_TRAIN} x {EPOCHS})")
    print(f"  3 cells per job (one per width), worst job "
          f"~{train_cost * 3 / 60:.0f} min if every cell were this one")


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
        by = record["by_sharing"]
        print(f"{record['condition']}  overall {record['accuracy']:.3f}  "
              + "  ".join(
                  f"{k} n={d['n']} "
                  + ("--" if d["accuracy"] is None else f"{d['accuracy']:.3f}")
                  for k, d in by.items())
              + f"  shares {record['share_counts']}")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


if __name__ == "__main__":
    main()
