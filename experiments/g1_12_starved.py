"""Is attention undertrained at long sequences, or genuinely too small?

g1-11 produced a favourable result — the local rule using a fifth the working
memory at long sequences — and that result rests entirely on attention scoring
0.252 at width 8, seq_len 384. g1-11 pre-registered this check for exactly that
circumstance.

Attention trained for a fixed 3000 steps at every length, while the number of
scored positions per sequence stayed at `n_pairs` = 4. So the task-relevant
gradient signal is constant while distractors grow eightfold. Width 8 may be
incapable, or merely starved.

    python experiments/g1_12_starved.py --seqlen 384 --width 8 --seed 1 --json out/x.json
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
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

SCALE, N_TRAIN, N_TEST, LR = 0.4, 400, 120, 3e-3
BUDGET = 12000
CHECKPOINTS = (1500, 3000, 6000, 12000)
SEQ_LENS = (192, 384)
WIDTHS = (8, 16)
SEEDS = (1, 2, 3)
BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)


def run(seq_len: int, width: int, seed: int) -> list[dict]:
    task = replace(BASE, seq_len=seq_len)
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)
    model = ShiftedAttention(AttentionConfig(
        vocab_size=task.vocab_size, d_model=width, seed=seed, init_scale=SCALE))
    optimiser = Adam(model.params, lr=LR)

    def score() -> float:
        correct = total = 0
        for sequence in test_set:
            tokens = np.asarray(sequence.tokens)
            predicted = model.predict(tokens)
            for q in sequence.query_positions:
                correct += predicted[q] == tokens[q + 1]
                total += 1
        return correct / total

    trajectory = []
    for step in range(1, BUDGET + 1):
        tokens = np.asarray(train_set[rng.integers(len(train_set))].tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[0] = scored[-1] = False
        logits, cache = model.forward(tokens)
        _, grads = model.loss_and_backward(logits, cache, targets, scored)
        optimiser.step(grads)
        if step in CHECKPOINTS:
            accuracy = score()
            trajectory.append(dict(
                condition=f"seq={seq_len} d={width} step={step}", seed=seed,
                seq_len=seq_len, d_model=width, step=step, accuracy=accuracy))
            print(f"  seq={seq_len:<5} d={width:<4} seed={seed} "
                  f"step {step:<6} {accuracy:.3f}", flush=True)
    return trajectory


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    widths = (args.width,) if args.width else WIDTHS

    records = []
    for seq_len in seq_lens:
        for width in widths:
            for seed in seeds:
                records.extend(run(seq_len, width, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
