"""G1 — what does a local rule need, against what backprop needs?

The bet the project rests on. Everything that has solved MQAR did it with
attention: a softmax over every position, and a backward pass through the whole
sequence. Neither survives C1.

This sweeps `d_model` for the locality-respecting associative memory on the same
task, at the same criterion, so the crossing point can be compared against the
attention model's — which g1-04 put between 8 and 16.

    python experiments/g1_05_local.py                    # every seed, serial
    python experiments/g1_05_local.py --seed 3 --json out/3.json

Reports SOLVED / STUCK counts, not means (g1-03: outcomes are bimodal).
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
EPOCHS, N_TRAIN, N_TEST = 8, 400, 120
SEEDS = tuple(range(1, 9))
WIDTHS = (16, 32, 64, 128, 256, 512)


def run(task: MqarConfig, d_model: int, seed: int, decay: float = 1.0,
        lr: float = 0.05) -> float:
    """Train the local rule online and return held-out query accuracy."""
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=d_model, lr=lr, decay=decay,
        seed=seed))

    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            sequence = train_set[index]
            tokens = np.asarray(sequence.tokens)
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


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    for d_model in WIDTHS:
        for seed in seeds:
            accuracy = run(BASE, d_model=d_model, seed=seed)
            records.append(dict(condition=f"d_model={d_model}", seed=seed,
                                d_model=d_model, accuracy=accuracy,
                                floor=BASE.trivial_floor))
            print(f"  d_model={d_model:<5} seed={seed:<3} {accuracy:.3f}",
                  flush=True)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
