"""The price, with both arms actually finished training.

g1-12 found attention starved at a fixed 3000 steps: width 8 went from 0.252 to
1.000 when the budget doubled. That retracted g1-11. But the local rule was
measured under the same fixed-budget condition, and repairing one arm of a
comparison is what CLAUDE.md forbids.

So both are re-measured with four times the budget and checkpoints, and a
crossing is only read where the two largest checkpoints agree.

    python experiments/g1_13_both_fed.py --mode attention --seqlen 384 --width 8 --seed 1 --json out/x.json
    python experiments/g1_13_both_fed.py --mode local --seqlen 384 --width 32 --seed 1 --json out/y.json
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

BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
N_TRAIN, N_TEST = 400, 120
SEEDS = (1, 2, 3)
SEQ_LENS = (48, 96, 192, 384)

ATTENTION_WIDTHS = (2, 4, 6, 8)
ATTENTION_SCALE, ATTENTION_LR = 0.4, 3e-3
ATTENTION_BUDGET = 12000
ATTENTION_CHECKS = (1500, 3000, 6000, 12000)

LOCAL_WIDTHS = (16, 24, 32, 48, 64)
LOCAL_SCALE, LOCAL_LR = 0.5, 0.05
LOCAL_BUDGET = 32
LOCAL_CHECKS = (1, 2, 4, 8, 16, 32)


def score(predict, sequences) -> float:
    correct = total = 0
    for sequence in sequences:
        tokens = np.asarray(sequence.tokens)
        predicted = predict(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(mode: str, seq_len: int, width: int, seed: int) -> list[dict]:
    task = replace(BASE, seq_len=seq_len)
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)
    out = []

    if mode == "attention":
        model = ShiftedAttention(AttentionConfig(
            vocab_size=task.vocab_size, d_model=width, seed=seed,
            init_scale=ATTENTION_SCALE))
        optimiser = Adam(model.params, lr=ATTENTION_LR)
        for step in range(1, ATTENTION_BUDGET + 1):
            tokens = np.asarray(train_set[rng.integers(len(train_set))].tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[0] = scored[-1] = False
            logits, cache = model.forward(tokens)
            _, grads = model.loss_and_backward(logits, cache, targets, scored)
            optimiser.step(grads)
            if step in ATTENTION_CHECKS:
                out.append(_record(mode, seq_len, width, seed, step,
                                   score(model.predict, test_set)))
    else:
        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=task.vocab_size, d_model=width, lr=LOCAL_LR,
            key_scale=LOCAL_SCALE, seed=seed))
        order = np.arange(len(train_set))
        for epoch in range(1, LOCAL_BUDGET + 1):
            rng.shuffle(order)
            for index in order:
                tokens = np.asarray(train_set[index].tokens)
                targets = np.roll(tokens, -1)
                scored = np.ones(len(tokens), dtype=bool)
                scored[-1] = False
                model.run(tokens, targets, scored, learn=True)
            if epoch in LOCAL_CHECKS:
                out.append(_record(mode, seq_len, width, seed, epoch,
                                   score(model.run, test_set)))
    return out


def _record(mode, seq_len, width, seed, budget, accuracy):
    print(f"  {mode:<10} seq={seq_len:<5} d={width:<4} seed={seed} "
          f"budget {budget:<6} {accuracy:.3f}", flush=True)
    return dict(condition=f"{mode} seq={seq_len} d={width} b={budget}",
                seed=seed, mode=mode, seq_len=seq_len, d_model=width,
                budget=budget, accuracy=accuracy)


def main() -> int:
    args = parse_args(__doc__)
    mode = args.mode or "local"
    seeds = (args.seed,) if args.seed is not None else SEEDS
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    widths = ((args.width,) if args.width
              else (ATTENTION_WIDTHS if mode == "attention" else LOCAL_WIDTHS))

    records = []
    for seq_len in seq_lens:
        for width in widths:
            for seed in seeds:
                records.extend(run(mode, seq_len, width, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
