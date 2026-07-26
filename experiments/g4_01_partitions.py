"""What does dropping the global readout cost?

Note 009 §4 identified the single global readout as the largest untested
assumption in this project: `y = Wo r` sums over every dimension, so once the
width is spread across machines it is the globally synchronised step C1 forbids.

This measures the alternative. Each group learns from its own error only, and two
numbers come out: the POOLED answer (groups summed) and the ALONE answer (one
group, which is what a machine that cannot afford the pool actually has).

    python experiments/g4_01_partitions.py --width 64 --seqlen 96 --json out/x.json
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
PARTITIONS = (1, 2, 4, 8)
WIDTHS = (32, 64, 128)
SEQ_LENS = (96, 384)
KEY_SCALE = 0.5
CHECKPOINTS = (4, 8, 16)


def score(model, sequences, partition) -> float:
    correct = total = 0
    for sequence in sequences:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens, partition=partition)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(width: int, seq_len: int, groups: int, lr: float, seed: int) -> list[dict]:
    task = replace(BASE, seq_len=seq_len)
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=width, partitions=groups,
        lr=lr, key_scale=KEY_SCALE, seed=seed))
    order = np.arange(len(train_set))
    records = []
    for epoch in range(1, CHECKPOINTS[-1] + 1):
        rng.shuffle(order)
        for index in order:
            tokens = np.asarray(train_set[index].tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)
        if epoch not in CHECKPOINTS:
            continue
        pooled = score(model, test_set, None)
        # Every group, not group 0. One group happening to be lucky is not the
        # quantity of interest -- a deployment gets whichever machine it gets.
        alone = [score(model, test_set, g) for g in range(groups)]
        records.append(dict(
            condition=f"d={width} seq={seq_len} P={groups} lr={lr} e={epoch}",
            seed=seed, d_model=width, seq_len=seq_len, partitions=groups,
            lr=lr, epoch=epoch, accuracy=pooled, pooled=pooled,
            alone_mean=float(np.mean(alone)), alone_worst=float(np.min(alone)),
            alone_best=float(np.max(alone))))
        print(f"  d={width:<4} seq={seq_len:<4} P={groups:<2} lr={lr:<5} "
              f"seed={seed} e={epoch:<3} pooled {pooled:.3f}  "
              f"alone {np.mean(alone):.3f} (worst {np.min(alone):.3f})",
              flush=True)
    return records


def main() -> int:
    args = parse_args(__doc__)
    widths = (args.width,) if args.width else WIDTHS
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    seeds = (args.seed,) if args.seed is not None else SEEDS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    groupings = (args.partitions,) if args.partitions else PARTITIONS

    records = []
    for width in widths:
        for seq_len in seq_lens:
            for groups in groupings:
                if width % groups:
                    continue
                for lr in rates:
                    for seed in seeds:
                        records.extend(run(width, seq_len, groups, lr, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
