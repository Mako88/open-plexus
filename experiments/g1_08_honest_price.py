"""The honest price of locality — both sides tuned this time.

g3-02 found the width curve in g1-05 and g1-06 was measured with the local rule's
projection scale pinned at a value nobody checked, and that moving it takes a
width-32 model from 0.263 to 0.960. The 4–6x figure is therefore not a
measurement of the mechanism.

The attention baseline has the same exposure: its `init_scale` sat at 0.1
throughout and was never swept either.

So both get swept, each is taken at its own best scale, and the crossing of those
two curves is the honest number.

    python experiments/g1_08_honest_price.py --mode local --width 32 --scale 0.5 --seed 3 --json out/x.json
    python experiments/g1_08_honest_price.py --mode attention --width 8 --scale 0.1 --seed 3 --json out/y.json

**Tuning one arm of a comparison and not the other reproduces the original error
with a friendlier face.** That is why the attention side is here.
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.models.attention import (  # noqa: E402
    Adam, AttentionConfig, ShiftedAttention)
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

TASK = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
N_TRAIN, N_TEST = 400, 120
SEEDS = tuple(range(1, 6))

LOCAL_WIDTHS = (16, 24, 32, 48, 64)
LOCAL_SCALES = (0.25, 0.5, 0.71, 1.0, 1.41)
LOCAL_EPOCHS, LOCAL_LR = 8, 0.05

ATTENTION_WIDTHS = (4, 8, 16, 32)
ATTENTION_SCALES = (0.025, 0.05, 0.1, 0.2, 0.4)
ATTENTION_STEPS, ATTENTION_LR = 3000, 3e-3


def score_local(width: int, scale: float, seed: int) -> float:
    rng = np.random.default_rng(seed)
    train_set = dataset(TASK, N_TRAIN)
    test_set = dataset(replace(TASK, seed=TASK.seed + 99_991), N_TEST)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=width, lr=LOCAL_LR,
        key_scale=scale, seed=seed))
    order = np.arange(len(train_set))
    for _ in range(LOCAL_EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens = np.asarray(train_set[index].tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)

    correct = total = 0
    for sequence in test_set:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def score_attention(width: int, scale: float, seed: int) -> float:
    rng = np.random.default_rng(seed)
    train_set = dataset(TASK, N_TRAIN)
    test_set = dataset(replace(TASK, seed=TASK.seed + 99_991), N_TEST)
    model = ShiftedAttention(AttentionConfig(
        vocab_size=TASK.vocab_size, d_model=width, seed=seed, init_scale=scale))
    optimiser = Adam(model.params, lr=ATTENTION_LR)
    for _ in range(ATTENTION_STEPS):
        tokens = np.asarray(train_set[rng.integers(len(train_set))].tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[0] = scored[-1] = False
        logits, cache = model.forward(tokens)
        _, grads = model.loss_and_backward(logits, cache, targets, scored)
        optimiser.step(grads)

    correct = total = 0
    for sequence in test_set:
        tokens = np.asarray(sequence.tokens)
        predicted = model.predict(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    mode = args.mode or "local"
    if mode == "local":
        widths = (args.width,) if args.width else LOCAL_WIDTHS
        scales = (args.scale,) if args.scale else LOCAL_SCALES
        score = score_local
    else:
        widths = (args.width,) if args.width else ATTENTION_WIDTHS
        scales = (args.scale,) if args.scale else ATTENTION_SCALES
        score = score_attention

    records = []
    for width in widths:
        for scale in scales:
            for seed in seeds:
                accuracy = score(width, scale, seed)
                records.append(dict(
                    condition=f"{mode} d={width} s={scale}", seed=seed,
                    mode=mode, d_model=width, scale=scale, accuracy=accuracy))
                print(f"  {mode:<10} d={width:<4} scale={scale:<6} "
                      f"seed={seed:<3} {accuracy:.3f}", flush=True)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
