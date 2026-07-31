"""g37-03: can the reference fit CLUTRR's TRAINING set at all?

**This run was registered before `g37-02` produced a number.** That record's P2
says, in advance:

> REFUTED IF it does not. Then the first thing to check is the carried constants,
> not the task: d128 and 16 epochs came from `closure`, and `g14-01` reported
> that task failing G0 at an undersized reference before the reference was fixed.
> The response to a refutation here is a width/epoch probe on THIS task, and only
> a second failure would say anything about CLUTRR.

P2 was refuted. This is that probe.

## The quantity that decides it, and why it is not a downstream proxy

**TRAIN accuracy.** A model that cannot fit examples it has already seen many
times is undersized or undertrained, and the verdict belongs to the reference. A
model that fits train and fails test is being asked to generalise and failing,
and the verdict belongs to the task. `g37-02` measured only test accuracy, which
cannot tell those apart — `CLAUDE.md` rule 2: observe the quantity the claim is
about, not a downstream summary of it.

## And a second thing, because it was cheap and the first result was odd

`g37-02`'s `majority` arm scored **0.0000** at hops 2 and 3 against a bucket
majority of 0.5000 and 0.4286. A gap that large between "the commonest training
answer" and "the commonest test answer" is either a real distribution shift or a
defect, and the two look identical from a results table. This prints the
distributions so it is one or the other on the record.

## What this does NOT duplicate, and what was searched

Searched by capability — reference, baseline, attention width, epochs, train
accuracy — across `experiments/`, `openplexus/` and `tools/`.

- **`experiments/g14_01_does_closure_pass_g0.py`** ran the same four arms on
  `closure` and is where these constants come from. It has a comment block
  recording ITS width/epoch probe; this is the same probe on a different task,
  and the two cannot share code because the tasks load differently.
- **`experiments/g37_02_does_clutrr_pass_g0.py`** is the run being diagnosed.
  `_targets` is IMPORTED from it rather than restated, so the probe cannot drift
  from the thing it explains.

Record: `experiments/sweeps/g37-03-can-the-reference-compose.txt`
"""

from __future__ import annotations

import pathlib
import sys
import time
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from experiments.g37_02_does_clutrr_pass_g0 import _targets  # noqa: E402
from openplexus.models.attention import (  # noqa: E402
    Adam, AttentionConfig, ShiftedAttention)
from openplexus.tasks import clutrr  # noqa: E402
from openplexus.tasks.kinship import IGNORE  # noqa: E402

DATA = ROOT / "data" / "clutrr"
#: A subsample, and it is the RIGHT choice here rather than a shortcut: the
#: question is whether the model can fit examples it has seen, and a smaller
#: training set makes that EASIER. A failure to fit 2,000 is a stronger result
#: than a failure to fit 9,074.
SUBSAMPLE = 2000
SETTINGS = ((128, 16), (128, 48), (256, 16), (256, 48))


def main() -> None:
    harness.refuse_if_mutating()
    started = time.time()
    if not (DATA / "gen_train23_test2to10").exists():
        raise SystemExit(f"no data in {DATA}. Run: python tools/fetch_clutrr.py")

    config = clutrr.ClutrrConfig(root=DATA, split="train")
    train = clutrr.load(config)
    test = clutrr.load(clutrr.ClutrrConfig(root=DATA, split="test"))

    counts = Counter(puzzle.target for puzzle in train)
    answer, seen = counts.most_common(1)[0]
    name = clutrr.RELATIONS[answer - config.relation_base]
    print(f"g37-03  train majority relation: {name} "
          f"({seen}/{len(train)} = {seen / len(train):.4f})\n")
    print("WHERE THE TRAIN MAJORITY LANDS IN TEST")
    for hops in (2, 3, 4, 10):
        bucket = [p for p in test if p.hops == hops]
        bucket_counts = Counter(p.target for p in bucket)
        top, n = bucket_counts.most_common(1)[0]
        print(f"  {hops:>2} hops: `{name}` appears "
              f"{bucket_counts.get(answer, 0):>3}/{len(bucket):<4}  "
              f"bucket's own top = "
              f"{clutrr.RELATIONS[top - config.relation_base]:<12} "
              f"{n}/{len(bucket)} = {n / len(bucket):.4f}")

    sub = train[:SUBSAMPLE]
    shallow = [p for p in test if p.hops in (2, 3)]
    print(f"\nFITTING on {len(sub)} training puzzles; "
          f"in-distribution test = {len(shallow)} puzzles at hops 2-3")
    header = f"{'width':>7}{'epochs':>8}{'train acc':>11}{'test 2-3':>10}{'sec':>8}"
    print(header)
    print("-" * len(header))

    for width, epochs in SETTINGS:
        cell = time.time()
        model = ShiftedAttention(AttentionConfig(
            vocab_size=config.vocab_size, d_model=width, seed=0))
        optimiser = Adam(model.params, lr=3e-3)
        rng = np.random.default_rng(0)
        order = np.arange(len(sub))
        for _ in range(epochs):
            rng.shuffle(order)
            for index in order:
                puzzle = sub[index]
                tokens = np.asarray(puzzle.tokens, dtype=np.int64)
                targets = _targets(puzzle)
                logits, cache = model.forward(tokens)
                _, grads = model.loss_and_backward(logits, cache, targets,
                                                   targets != IGNORE)
                optimiser.step(grads)

        def accuracy(puzzles) -> float:
            hit = 0
            for puzzle in puzzles:
                predicted = model.predict(
                    np.asarray(puzzle.tokens, dtype=np.int64))
                hit += int(predicted[puzzle.query_position]) == puzzle.target
            return hit / len(puzzles)

        print(f"{width:>7}{epochs:>8}{accuracy(sub):>11.4f}"
              f"{accuracy(shallow):>10.4f}{time.time() - cell:>8.0f}")

    print(f"\nCOST: {time.time() - started:.0f}s wall, one process")


if __name__ == "__main__":
    main()
