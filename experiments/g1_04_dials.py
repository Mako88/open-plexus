"""The difficulty curve — do the dials bite a model that can do the task?

Note 001's P3 asked for a curve rather than a point. It has been measured for the
trivial floor (g0-01) and a frozen substrate (g0-02), both of which fail
regardless of the setting. It has never been measured against a model that
succeeds, and "harder for something that always fails" is not the same claim as
"harder".

    python experiments/g1_04_dials.py

Reports SOLVED / STUCK counts, not means. g1-03 established outcomes are bimodal:
a mean describes a mixture of two populations and no actual run.
"""

from __future__ import annotations

import sys
import time
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus.models.attention import Adam, AttentionConfig, ShiftedAttention  # noqa: E402
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
STEPS, N_TRAIN, N_TEST, LR = 3000, 400, 120, 3e-3
SEEDS = tuple(range(1, 9))
SOLVED, STUCK = 0.9, 0.2


def run(task: MqarConfig, seed: int, d_model: int = 64) -> float:
    """Train one model and return its held-out accuracy at query positions."""
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)
    model = ShiftedAttention(AttentionConfig(
        vocab_size=task.vocab_size, d_model=d_model, seed=seed))
    optimiser = Adam(model.params, lr=LR)

    for _ in range(STEPS):
        sequence = train_set[rng.integers(len(train_set))]
        tokens = np.asarray(sequence.tokens)
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


def sweep(label, tasks, d_model=64):
    print(f"\n--- {label} ---")
    header = (f"{'setting':<16}{'floor':>7}{'solved':>10}{'stuck':>10}"
              f"{'between':>10}{'worst':>8}{'best':>7}{'secs':>7}")
    print(header)
    print("-" * len(header))
    for name, task in tasks:
        started = time.time()
        accs = [run(task, s, d_model) for s in SEEDS]
        n = len(accs)
        solved = sum(a >= SOLVED for a in accs)
        stuck = sum(a <= STUCK for a in accs)
        print(f"{name:<16}{task.trivial_floor:>7.3f}"
              f"{f'{solved}/{n}':>10}{f'{stuck}/{n}':>10}"
              f"{f'{n-solved-stuck}/{n}':>10}"
              f"{min(accs):>8.3f}{max(accs):>7.3f}{time.time()-started:>7.0f}")


def main() -> int:
    print("Do the difficulty dials bite a model that can do the task?")
    print(f"{STEPS} steps, {len(SEEDS)} seeds, value_offsets=(1,) so any failure")
    print("is attributable to the dial rather than to search luck (g1-03).")
    print(f"solved >= {SOLVED}, stuck <= {STUCK}")

    sweep("n_pairs (seq_len fixed at 96)",
          [(f"n_pairs={k}", replace(BASE, n_pairs=k)) for k in (2, 4, 8, 16)])
    sweep("seq_len (n_pairs fixed at 4)",
          [(f"seq_len={n}", replace(BASE, seq_len=n)) for n in (32, 64, 128)])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
