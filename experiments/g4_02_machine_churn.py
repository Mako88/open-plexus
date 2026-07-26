"""Churn shaped like a machine, against churn shaped like a random mask.

G3 removed dimensions uniformly at random, which was right when no dimension
belonged to anybody. With `partitions` each machine owns a contiguous block, so a
departing machine removes a block -- and a scatter and a block are not the same
damage.

    python experiments/g4_02_machine_churn.py --width 64 --lr 0.05 --seed 1 --json out/x.json
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
                  autoregressive=True, filler="random", seed=20260726)
N_TRAIN, N_TEST = 400, 120
SEEDS = (1, 2, 3)
LEARNING_RATES = (0.02, 0.05, 0.1)
PARTITIONS = (1, 4, 8)
WIDTHS = (64, 128)
CHURN = (0.0, 0.25, 0.5)
KEY_SCALE = 0.5
REMOVE_AFTER, TOTAL_EPOCHS = 8, 24   # G3's protocol, so the numbers compare


def block_is_possible(width: int, groups: int, fraction: float) -> bool:
    """Can this churn fraction be expressed as a whole number of machines?

    **Block churn has a granularity that scattered churn does not: 1/P.** At P=1
    a machine is the entire model, so "remove half a machine" has no meaning --
    the only available levels are none and all.

    The first version of this experiment ignored that and rounded. At P=1 with
    churn 0.5 it rounded 0.5 machines up to 1 and removed the whole model,
    scoring 0.000 against scattered's 0.969 -- and would have been reported as
    block churn being catastrophically worse than scattered, which is not a
    result about churn at all. Caught by running the connection control before
    dispatching rather than after.
    """
    removed = width * fraction
    per_group = width // groups
    return removed > 0 and abs(removed / per_group - round(removed / per_group)) < 1e-9


def doomed(shape: str, width: int, groups: int, fraction: float,
           rng: np.random.Generator) -> np.ndarray:
    """Which dimensions leave, and in what shape.

    BLOCK removes whole groups, which is what a machine leaving actually does.
    SCATTERED removes the same COUNT uniformly at random, which is what G3 did.
    Holding the count equal is what makes the two comparable -- the difference
    under test is the shape of the damage, not its size. `run` asserts the two
    arms leave the same width standing, so a divergence is an error rather than
    a finding.
    """
    n = int(round(width * fraction))
    if n == 0:
        return np.array([], dtype=int)
    if shape == "scattered":
        return rng.choice(width, size=n, replace=False)
    per_group = width // groups
    if n % per_group:
        raise ValueError(
            f"churn {fraction} of width {width} removes {n} dimensions, which "
            f"is not a whole number of {per_group}-wide machines")
    chosen = rng.choice(groups, size=n // per_group, replace=False)
    return np.concatenate([np.arange(g * per_group, (g + 1) * per_group)
                           for g in chosen])


def score(model, sequences) -> float:
    correct = total = 0
    for sequence in sequences:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(shape: str, width: int, groups: int, fraction: float, lr: float,
        seed: int) -> dict:
    task = BASE
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=width, partitions=groups,
        lr=lr, key_scale=KEY_SCALE, seed=seed))
    order = np.arange(len(train_set))
    healthy = removed = None
    for epoch in range(1, TOTAL_EPOCHS + 1):
        rng.shuffle(order)
        for index in order:
            tokens = np.asarray(train_set[index].tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)
        if epoch == REMOVE_AFTER:
            healthy = score(model, test_set)
            model.ablate(doomed(shape, width, groups, fraction, rng))
            removed = score(model, test_set)
    recovered = score(model, test_set)

    survivors = model.surviving_width()
    expected = width - int(round(width * fraction))
    if survivors != expected:
        raise AssertionError(
            f"{shape} at churn {fraction} left {survivors} of {width} standing, "
            f"expected {expected} -- the two arms are not removing the same "
            f"amount, so any difference between them is size and not shape")

    print(f"  {shape:<10} d={width:<4} P={groups:<2} churn={fraction:<5} "
          f"lr={lr:<5} seed={seed}  healthy {healthy:.3f} -> "
          f"removed {removed:.3f} -> recovered {recovered:.3f} "
          f"(width {survivors})", flush=True)
    return dict(
        condition=f"{shape} d={width} P={groups} churn={fraction} lr={lr}",
        seed=seed, shape=shape, d_model=width, partitions=groups,
        churn=fraction, lr=lr, accuracy=recovered, healthy=healthy,
        removed=removed, recovered=recovered, surviving_width=survivors)


def main() -> int:
    args = parse_args(__doc__)
    widths = (args.width,) if args.width else WIDTHS
    seeds = (args.seed,) if args.seed is not None else SEEDS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    groupings = (args.partitions,) if args.partitions else PARTITIONS
    fractions = (args.churn,) if args.churn is not None else CHURN

    records = []
    for width in widths:
        for groups in groupings:
            if width % groups:
                continue
            for fraction in fractions:
                shapes = ["scattered"]
                if block_is_possible(width, groups, fraction):
                    shapes.append("block")
                for shape in shapes:
                    for lr in rates:
                        for seed in seeds:
                            records.append(
                                run(shape, width, groups, fraction, lr, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
