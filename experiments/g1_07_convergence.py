"""Is the crossing a capacity limit or just a convergence limit?

g3-01 found a model ablated down to width 32 recovering to 0.924, where one
trained from scratch at width 32 reaches 0.225 — same architecture, epochs and
data. Only the readout learns and the projections are frozen random, so both face
the same problem with the same tools. The difference must be the readout's
starting state.

Which means the crossing g1-05 and g1-06 located may be about how long training
takes rather than about how much room the mechanism needs — and "locality costs
4–6x in width" would be measuring the wrong thing.

    python experiments/g1_07_convergence.py --width 32 --epochs 64 --seed 3 --json out/x.json

Reports accuracy at every checkpoint along the way, not just the endpoint,
because "still climbing when the budget ran out" and "plateaued" are different
answers and only the trajectory tells them apart.
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

TASK = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260725)
N_TRAIN, N_TEST, LR = 400, 120, 0.05
SEEDS = tuple(range(1, 7))
WIDTHS = (24, 32, 48)
#: One run to the longest budget, scored at checkpoints along the way. Running
#: separate 8/16/32/64-epoch jobs would repeat the same trajectory four times for
#: nothing — the 64-epoch run already contains the others.
BUDGET = 64
#: Checkpoints rather than an endpoint, because a run still climbing at the last
#: one has not demonstrated a plateau, and that distinction is the whole question.
CHECKPOINTS = (1, 2, 4, 8, 16, 24, 32, 48, 64)


def run(width: int, budget: int, seed: int) -> list[dict]:
    rng = np.random.default_rng(seed)
    train_set = dataset(TASK, N_TRAIN)
    test_set = dataset(replace(TASK, seed=TASK.seed + 99_991), N_TEST)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=width, lr=LR, seed=seed))

    def score() -> float:
        correct = total = 0
        for sequence in test_set:
            tokens = np.asarray(sequence.tokens)
            predicted = model.run(tokens)
            for q in sequence.query_positions:
                correct += predicted[q] == tokens[q + 1]
                total += 1
        return correct / total

    order = np.arange(len(train_set))
    trajectory = []
    for epoch in range(1, budget + 1):
        rng.shuffle(order)
        for index in order:
            sequence = train_set[index]
            tokens = np.asarray(sequence.tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)
        if epoch in CHECKPOINTS or epoch == budget:
            accuracy = score()
            trajectory.append(dict(
                condition=f"d={width} e={epoch}", seed=seed, d_model=width,
                epoch=epoch, budget=budget, accuracy=accuracy))
            print(f"  d={width:<4} seed={seed:<3} epoch {epoch:<4} "
                  f"{accuracy:.3f}", flush=True)
    return trajectory


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    widths = (args.width,) if args.width is not None else WIDTHS
    budget = args.epochs or BUDGET

    records = []
    for width in widths:
        for seed in seeds:
            records.extend(run(width, budget, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
