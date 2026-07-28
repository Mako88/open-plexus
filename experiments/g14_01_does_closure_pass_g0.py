"""Does the closure task pass G0, and is there headroom on the ENTAILED half?

**This is the gate the predecessor project failed, and it cost it a year.**
GOALS section 4:

> before any learning mechanism is written, the task must be shown to have
> substantial headroom between what a random frozen substrate achieves and what
> a strong non-local reference achieves, with both measured, multi-seed, and
> with the base rate of a constant predictor reported alongside.

`closure.py` was designed by this project rather than borrowed, which makes the
test more necessary rather than less: a task built to have a property tends to be
measured as though it has it.

## The arms

    majority    always answer the commonest relation      the base rate
    frozen      our model, random Wo, NO LEARNING         the substrate
    local       our model under the delta rule            the candidate
    attention   backprop, softmax over positions, Adam    the strong reference

`attention` is given every advantage the local rule does not have -- a real
optimiser, a softmax over positions, and gradients reaching every parameter.
That is the point: it measures what the task ADMITS, not what our rule achieves.

## The split is the measurement

Accuracy is reported separately on **stated** and **entailed** targets. A stated
fact's relation is one stored binding -- `key(S, O) -> R`, which g13-02 measured
at 1.000 for an unambiguous pair -- so the stated half is recall and every arm
should do well on it. **The entailed half is the task.** Its `key(S, O)` was
never written and the relation must be composed from two other facts.

Averaging them would hide exactly the thing G0 exists to check: a task can look
to have headroom while all of it sits on the half that is already solved.

## PREDICTIONS (registered before running)

  P1  On ENTAILED targets, `frozen` sits near `majority`. A random substrate that
      already composed would mean the task is answerable without learning, which
      is the predecessor's failure exactly.
  P2  On ENTAILED targets, `attention` clearly beats `majority` -- by more than
      0.15. If a model with every advantage cannot compose here, the task is not
      learnable and nothing downstream is worth running.
  P3  The headroom on ENTAILED (attention minus frozen) is larger than 0.15.
      This is the G0 acceptance criterion and the reason to run at all.
  P4  On STATED targets every arm except `majority` clears 0.60. Stated facts are
      single bindings; an arm that cannot recall them is broken rather than
      uninformative, and this is the rail that says so.
  P5  `local` lands between `frozen` and `attention` on ENTAILED. Above frozen
      because the delta rule learns something; below attention because it has no
      backward pass. Refuted either way is interesting -- above attention would
      be surprising, at frozen would say the local rule cannot use this task.

P3 is the gate. P4 is the rail: if it fails, the arms are not wired up and P1-P3
are unreadable.

COST: 4 arms x 8 seeds = 32 cells at one width. `attention` is the expensive arm
-- a real optimiser over 120-token sequences -- and the estimate comes from it.
Printed by `--cost`.

MEASURED ON: `openplexus/tasks/closure.py` at its calibrated defaults -- 10
people, 24 stated edges, 6 entailed, ~5.4 implied edges per sequence.

**Width 256 deliberately.** The task writes ~119 bindings per sequence, above
decision 109's ~96 capacity for width 64, so a narrow arm would be measuring
capacity rather than composition. That is recorded at the task's defaults.
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
from openplexus.models.attention import (  # noqa: E402
    Adam, AttentionConfig, ShiftedAttention)
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.closure import (  # noqa: E402
    ClosureConfig, dataset, stated_positions)
from openplexus.tasks.kinship import IGNORE  # noqa: E402

WIDTH = 256
N_TRAIN, N_TEST, EPOCHS = 300, 150, 4
SEEDS = tuple(range(8))
ARMS = ("majority", "frozen", "local", "attention")

#: THE REFERENCE HAD TO BE CONVERGED BEFORE IT COULD BE BELIEVED, and the first
#: version of this experiment was not. Entailed accuracy against a 0.198
#: majority floor:
#:
#:     width  64, 4 epochs    0.148    BELOW the floor
#:     width  64, 16 epochs   0.186    still below
#:     width 128, 16 epochs   0.277    clears it       <- chosen
#:     width 128, 40 epochs   0.284    saturating
#:
#: At the first setting this experiment reported the task FAILING G0 -- and the
#: failure was the reference being undersized and undertrained, not the task
#: being unlearnable. **A G0 verdict is a statement about the reference as much
#: as about the task**, which is decision 63's lesson (probe the bottom of a
#: range before spending on it) arriving from the opposite direction: there a
#: grid sat entirely above saturation, here a reference sat entirely below it.
#:
#: 40 epochs buys 0.007 over 16, so 16 is converged for this purpose and the
#: extra cost is not spent.
ATTENTION_WIDTH, ATTENTION_EPOCHS = 128, 16


def local_model(task: ClosureConfig, seed: int) -> LocalAssociativeMemory:
    """Pair keys, one hop. No search -- this measures the OBJECTIVE, not the
    search mechanism, and mixing them would make the result unattributable."""
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=WIDTH, vocab_size=task.vocab_size, seed=seed,
        derived_keys=True, context_keys=True))


def score(predictions, sequences) -> dict:
    """Accuracy on stated and entailed targets, kept apart."""
    hits = {"stated": [0, 0], "entailed": [0, 0]}
    for predicted, sequence in zip(predictions, sequences):
        entailed = set(sequence.entailed)
        for position, target in enumerate(sequence.targets):
            if target == IGNORE:
                continue
            bucket = "entailed" if position in entailed else "stated"
            hits[bucket][1] += 1
            hits[bucket][0] += int(predicted[position] == target)
    return {name: (n / total if total else None)
            for name, (n, total) in hits.items()}


def run_majority(train, test, task) -> list:
    """Always answer the commonest relation in TRAINING. The base rate."""
    counts: Counter = Counter()
    for sequence in train:
        for target in sequence.targets:
            if target != IGNORE:
                counts[target] += 1
    answer = counts.most_common(1)[0][0]
    return [np.full(len(s.tokens), answer, dtype=np.int64) for s in test]


def run_local(train, test, task, seed, learn: bool) -> list:
    model = local_model(task, seed)
    if learn:
        for _ in range(EPOCHS):
            for sequence in train:
                tokens = np.array(sequence.tokens, dtype=np.int64)
                targets = np.array(sequence.targets, dtype=np.int64)
                model.run(tokens, targets, targets != IGNORE, learn=True)
    return [model.run(np.array(s.tokens, dtype=np.int64)) for s in test]


def run_attention(train, test, task, seed) -> list:
    """The strong non-local reference, given every advantage.

    `value_offsets=(1,)` is left at its default, which hands the model the
    induction shape. That is a hint and it is deliberate: the reference is meant
    to measure what the TASK admits, so making it strong is the point.
    """
    model = ShiftedAttention(AttentionConfig(
        vocab_size=task.vocab_size, d_model=ATTENTION_WIDTH, seed=seed))
    optimiser = Adam(model.params, lr=3e-3)
    rng = np.random.default_rng(seed)
    order = np.arange(len(train))
    for _ in range(ATTENTION_EPOCHS):
        rng.shuffle(order)
        for index in order:
            sequence = train[index]
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            logits, cache = model.forward(tokens)
            _, grads = model.loss_and_backward(logits, cache, targets,
                                               targets != IGNORE)
            optimiser.step(grads)
    return [model.predict(np.array(s.tokens, dtype=np.int64)) for s in test]


def one_cell(arm: str, seed: int) -> dict:
    task = ClosureConfig(seed=seed * 100_000)
    train = dataset(task, N_TRAIN)
    test = dataset(replace(task, seed=task.seed + 500_000), N_TEST)

    started = time.time()
    if arm == "majority":
        predictions = run_majority(train, test, task)
    elif arm == "frozen":
        predictions = run_local(train, test, task, seed, learn=False)
    elif arm == "local":
        predictions = run_local(train, test, task, seed, learn=True)
    else:
        predictions = run_attention(train, test, task, seed)
    elapsed = time.time() - started

    result = score(predictions, test)
    result.update(
        arm=arm, width=WIDTH, seed=seed, seconds=round(elapsed, 1),
        entailed_per_sequence=round(
            sum(len(s.entailed) for s in test) / len(test), 2),
        condition=(f"{arm}|d{WIDTH}|seed{seed}|train{N_TRAIN}x{EPOCHS}"
                   f"|test{N_TEST}"))
    return result


def cost_probe() -> None:
    task = ClosureConfig(seed=0)
    train = dataset(task, 20)
    started = time.time()
    run_attention(train, train[:2], task, 0)
    per = (time.time() - started) / (len(train) * EPOCHS)
    print("most expensive arm: attention")
    print(f"  {per * 1000:.1f} ms per training sequence")
    print(f"  {per * N_TRAIN * EPOCHS / 60:.1f} min to train one cell")
    print(f"  4 arms per job, worst job "
          f"~{per * N_TRAIN * EPOCHS * 4 / 60:.0f} min if every arm were this "
          f"one -- majority and frozen are nearly free")


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
        print(f"{record['condition']}  stated {record['stated']:.3f}  "
              f"entailed {record['entailed']:.3f}  "
              f"({record['entailed_per_sequence']} implied/seq)")

    if args.json:
        Path(args.json).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")


if __name__ == "__main__":
    main()
