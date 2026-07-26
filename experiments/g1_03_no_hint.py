"""Does the predictive objective still bootstrap without the architectural hint?

g1-02 hardcoded the value source to offset +1 — attending to `s` retrieved the
token at `s+1`, which is the induction shape given rather than learned. This
makes it a learned mixture over candidate offsets and asks whether the objective
still finds the task when it has to discover *where to look*.

    python experiments/g1_03_no_hint.py

Reports the learned mixture alongside the score, because a high score with the
weight somewhere other than +1 would mean the model found a different route and
the mechanistic story is wrong.
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

TASK = MqarConfig(n_pairs=4, seq_len=64, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
STEPS, N_TRAIN, N_TEST, LR, D_MODEL = 4000, 600, 200, 3e-3, 64
SEEDS = (1, 2, 3, 4, 5)
CONDITIONS = ((1,), (0, 1), (-1, 0, 1), (-1, 0, 1, 2), (-2, -1, 0, 1, 2))


def prepare(sequence):
    tokens = np.asarray(sequence.tokens)
    targets = np.roll(tokens, -1)
    every = np.ones(len(tokens), dtype=bool)
    every[-1] = False
    return tokens, targets, every


def run(offsets, seed):
    rng = np.random.default_rng(seed)
    train_set = dataset(TASK, N_TRAIN)
    test_set = dataset(replace(TASK, seed=TASK.seed + 99_991), N_TEST)
    model = ShiftedAttention(AttentionConfig(
        vocab_size=TASK.vocab_size, d_model=D_MODEL, seed=seed,
        value_offsets=offsets))
    optimiser = Adam(model.params, lr=LR)

    for _ in range(STEPS):
        sequence = train_set[rng.integers(len(train_set))]
        tokens, targets, every = prepare(sequence)
        scored = every.copy()
        scored[:model.reach] = False        # nothing to attend to yet
        logits, cache = model.forward(tokens)
        _, grads = model.loss_and_backward(logits, cache, targets, scored)
        optimiser.step(grads)

    correct = total = 0
    for sequence in test_set:
        tokens, targets, _ = prepare(sequence)
        predicted = model.predict(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == targets[q]
            total += 1
    mix = np.abs(model.params["offset_mix"])
    return correct / total, offsets[int(mix.argmax())], mix / mix.sum()


def main() -> int:
    print("Does the objective bootstrap without the induction hint?")
    print(f"{STEPS} steps, {len(SEEDS)} seeds, objective=all, filler=random")
    print(f"trivial floor {TASK.trivial_floor:.3f}, base rate "
          f"{1/TASK.n_values:.3f}\n")
    header = (f"{'value_offsets':<18}{'query acc':>11}{'spread':>16}"
              f"{'argmax offset':>15}{'secs':>7}")
    print(header)
    print("-" * len(header))

    for offsets in CONDITIONS:
        started = time.time()
        runs = [run(offsets, s) for s in SEEDS]
        accs = [r[0] for r in runs]
        picks = [r[1] for r in runs]
        span = (f"{min(accs):.3f}-{max(accs):.3f}"
                if max(accs) - min(accs) > 5e-4 else "all equal")
        chosen = f"{picks.count(1)}/{len(SEEDS)} chose +1"
        print(f"{str(offsets):<18}{np.mean(accs):>11.3f}{span:>16}"
              f"{chosen:>15}{time.time()-started:>7.0f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
