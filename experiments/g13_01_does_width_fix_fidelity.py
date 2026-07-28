"""Does width fix retrieval fidelity on the task, and does that unblock composition?

**This is the gate on whether search gets rebuilt**, and it is the first
committed instrument the relational line has ever had.

## The situation it is measuring

Four mechanisms have failed against retrieval fidelity -- the accumulator (102),
pair keys with hops (105), traversal (107) and search (111) -- each correct in
itself and each capped by how often a single lookup is right. Decision 112 then
ablated that number in isolation and found it is a **width** limit:

    as configured (width 64)   0.915      width 128   1.000      width 256   1.000

Nothing has re-run the task itself at those widths. That is the whole of this
experiment.

## Why it decides the next build rather than merely improving a number

Decision 111 refused to build search on the grounds that **"you cannot search
your way out of noisy primitives, because the verifier is built from the
primitives."** That refusal is conditional on the primitives being noisy.

Decision 108 says the residual problem at out-degree > 1 is not noise at all but
**ambiguity**: the store returns a relation the subject genuinely holds 96% of
the time, and "correct" tracks 1/k. It is answering *"what relation does S
hold"*; the question needs *"which of S's relations leads to T"*. No width fixes
underdetermination.

If both hold, then at width 256 the primitives become **reliable but ambiguous**
-- which is precisely the regime where search is the right answer and its
verifier is trustworthy. If instead width fixes out-degree 2 as well, decision
108 is wrong and the whole line reopens further back.

Either way this run says what to build next, which is why it is worth a matrix.

## What is measured, and what is deliberately NOT

Measured **through `model.run()` only**, plus the task's own metadata. Accuracy
is split by the out-degree of the queried subject among the stated facts, which
is computable from `sequence.facts` and needs nothing from inside the store.

**No retrieval probe.** Decisions 108 and 112 read per-step fidelity out of the
store by hand, in inline probes that left no script behind -- which is why this
file exists. Reimplementing that probe is the specific mistake `run()`'s own
docstring warns about: *"that is how the 150/300 cap values came from a
reimplementation whose store never bound."* The out-degree split gets the same
question answered from the outside.

## The two arms

    hop1-pair     hops=1, context_keys        the fidelity primitive itself
    hop2-concat   hops=2, hop_accumulate=concat   composition, decision 102's config

They cannot be one arm: the model refuses `hops > 1` with `context_keys`, because
a hop re-encodes through `Wk` while the store keys on pairs (decision 105,
measured cosine -0.069). That refusal is correct and is not being worked around.

## PREDICTIONS (registered before running)

  P1  CONTROL -- `hop1-pair` at out-degree 1 rises with width and clears 0.99 at
      256, reproducing decision 112 end-to-end. If it does not, this instrument
      disagrees with the record and NOTHING ELSE IN THE RUN IS READABLE.
  P2  `hop1-pair` overall accuracy rises with width but does NOT reach 1.000,
      because the out-degree > 1 sequences are ambiguous rather than noisy.
  P3  `hop1-pair` at out-degree >= 2 stays within 0.10 of its 1/k bound at EVERY
      width -- the direct test of decision 108, and the one that decides search.
  P4  `hop2-concat` gains less from width than `hop1-pair` does, because
      composition compounds two lookups and the second is the ambiguous one.
  P5  `hop2-concat` at width 256 still does not clear `shortcut_floor`. Decision
      102 measured concat matching the one-hop model exactly, and width does not
      supply a mechanism that was missing.

P1 is the control. P3 is the decision-relevant one: if it is REFUTED -- if
out-degree 2 climbs with width -- then decision 108's ambiguity account is wrong,
search is not the next build, and the fidelity story is simply incomplete.

COST: 2 arms x 3 widths x 8 seeds = 48 cells, estimated from the MOST EXPENSIVE
cell (width 256, hop2-concat), which is where the d^2 store cost and the doubled
hop count meet. Measured by `--cost`: 21.0 ms per training sequence, 0.6 min to
train one cell at 400 x 4. One job per seed is 6 cells, so a job is ~3 min even
if every cell were the dear one. Eight jobs.

g11-03 lost four of six cells to a timeout by estimating from a cheap cell
instead, which is why the probe exists and why the figure above is the dear one.

MEASURED ON: `openplexus/tasks/kinship.py`, hops 1 and 2, 12 people, 10 facts.
"""

from __future__ import annotations

import argparse
import json
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

#: Widths. 64 is where every relational number in the project was measured, so
#: it is the control rather than a data point; 128 and 256 are the two decision
#: 112 took to 1.000 in isolation.
WIDTHS = (64, 128, 256)

#: Held apart from the model width on purpose. Kinship writes 44 bindings at
#: these settings (decision 112, measured not inferred), so the load is fixed
#: across the width axis and width is the only thing moving.
N_TRAIN, N_TEST, EPOCHS = 400, 200, 4

#: EIGHT, not three. The cost probe put the most expensive cell at 0.6 min, so
#: the whole matrix is minutes -- and this project has been bitten by three-seed
#: grids twice. The retired backlog's item 0b put it plainly: *"any published
#: difference in this line smaller than about 0.15 is inside the noise and was
#: never distinguishable from zero"*, and seeds are the cheapest axis there is.
#: The effect being looked for here is 0.02 wide on the smoke seed.
SEEDS = tuple(range(8))

ARMS = {
    # The fidelity primitive. One hop is a stated fact, so this measures
    # retrieval and nothing else -- which is exactly the quantity decision 112
    # ablated to 1.000 at these widths.
    "hop1-pair": dict(task_hops=1,
                      config=dict(hops=1, derived_keys=True,
                                  context_keys=True)),
    # Composition, in decision 102's configuration. `concat` holds both
    # retrievals where `replace` overwrites the first; pair keys are OFF because
    # the model refuses them alongside hops, correctly (decision 105).
    "hop2-concat": dict(task_hops=2,
                        config=dict(hops=2, hop_accumulate="concat",
                                    derived_keys=True)),
}


def out_degree(sequence, subject: int) -> int:
    """How many stated facts have `subject` as their SUBJECT.

    This is the quantity decision 108 found "correct" tracking 1/k against, and
    it is readable from the generated sequence without touching the store.
    """
    return sum(1 for s, _, _ in sequence.facts if s == subject)


def evaluate(model: LocalAssociativeMemory, data) -> dict:
    """Accuracy overall and split by the queried subject's out-degree."""
    buckets: dict[str, list[int]] = {"1": [], "2": [], "3+": []}
    hits = 0
    for sequence in data:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        predicted = model.run(tokens)
        correct = int(predicted[sequence.answer_position]
                      == sequence.targets[sequence.answer_position])
        hits += correct
        degree = out_degree(sequence, sequence.asked[0])
        key = "1" if degree <= 1 else ("2" if degree == 2 else "3+")
        buckets[key].append(correct)
    return {
        "accuracy": hits / len(data),
        "by_out_degree": {
            k: {"n": len(v), "accuracy": (sum(v) / len(v)) if v else None,
                # The 1/k bound decision 108 says "correct" tracks. Reported
                # beside the accuracy so P3 can be scored without arithmetic.
                "one_over_k": 1.0 / (1 if k == "1" else (2 if k == "2" else 3))}
            for k, v in buckets.items()},
    }


def one_cell(arm: str, width: int, seed: int) -> dict:
    """Train and evaluate one (arm, width, seed)."""
    spec = ARMS[arm]
    task = KinshipConfig(hops=spec["task_hops"], seed=seed * 100_000)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 500_000), N_TEST)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=width, vocab_size=task.vocab_size, seed=seed,
        **spec["config"]))

    started = time.time()
    for _ in range(EPOCHS):
        for sequence in train:
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            model.run(tokens, targets, targets != IGNORE, learn=True)
    trained = time.time() - started

    result = evaluate(model, test)
    result.update(
        arm=arm, width=width, seed=seed, train_seconds=round(trained, 1),
        floors=shortcut_floors(task),
        # Rule 11b: assert on what the run ACTUALLY did, never on the directory
        # it was fetched into or the workflow that was meant to dispatch it.
        condition=(f"{arm}|d{width}|seed{seed}|hops{spec['task_hops']}"
                   f"|train{N_TRAIN}x{EPOCHS}|test{N_TEST}"))
    return result


def cost_probe() -> None:
    """Time the MOST EXPENSIVE cell and extrapolate. Prints, measures nothing.

    The store is `d x d` and the per-step work is a matvec, so width 256 is
    SIXTEEN times width 64 rather than four. g11-03 lost four of six cells to a
    timeout because its estimate came from a cheap cell that had been run
    locally, which is exactly the wrong one to take it from.
    """
    arm, width = "hop2-concat", max(WIDTHS)
    spec = ARMS[arm]
    task = KinshipConfig(hops=spec["task_hops"], seed=0)
    sample = dataset(task, 20)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        d_model=width, vocab_size=task.vocab_size, seed=0, **spec["config"]))
    started = time.time()
    for sequence in sample:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        model.run(tokens, targets, targets != IGNORE, learn=True)
    per_sequence = (time.time() - started) / len(sample)
    train_cost = per_sequence * N_TRAIN * EPOCHS
    print(f"most expensive cell: {arm} at width {width}")
    print(f"  {per_sequence * 1000:.1f} ms per training sequence")
    print(f"  {train_cost / 60:.1f} min to train one cell "
          f"({N_TRAIN} x {EPOCHS})")
    print(f"  + evaluation, {N_TEST} sequences, no learning")
    print(f"  18 cells, one job per seed -> 6 cells per job, "
          f"worst job ~{train_cost * 6 / 60:.0f} min if every cell were this one")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--json", type=str, default=None)
    parser.add_argument("--cost", action="store_true",
                        help="time the most expensive cell and exit")
    args = parser.parse_args()

    harness.refuse_if_mutating()

    if args.cost:
        cost_probe()
        return

    seeds = (args.seed,) if args.seed is not None else SEEDS
    records = [one_cell(arm, width, seed)
               for seed in seeds for arm in ARMS for width in WIDTHS]

    for record in records:
        degrees = record["by_out_degree"]
        floors = record["floors"]
        print(f"{record['condition']}  overall {record['accuracy']:.3f}  "
              f"[floor first {floors['first']:.3f} "
              f"majority {floors['majority']:.3f}]  "
              + "  ".join(
                  f"k={k} n={d['n']} "
                  + ("--" if d["accuracy"] is None else f"{d['accuracy']:.3f}")
                  + f" (1/k {d['one_over_k']:.3f})"
                  for k, d in degrees.items()))

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


if __name__ == "__main__":
    main()
