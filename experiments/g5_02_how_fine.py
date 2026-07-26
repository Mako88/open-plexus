"""How finely can a fixed amount of capacity be divided?

g5-01 found that adding 16-wide machines stops helping at 384 steps. Re-reading
g4-01 by machine width rather than machine count showed why: the same 128
dimensions score 0.741 as eight machines and 0.992 as four. The wall is machine
width, not machine count.

So this holds TOTAL width fixed and varies only how finely it is cut -- which
removes g5-01's confound, where the network and its capacity grew together.

    python experiments/g5_02_how_fine.py --seqlen 384 --partitions 8 --lr 0.05 --json out/x.json
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
LEARNING_RATES = (0.01, 0.02, 0.05, 0.1, 0.2)
TOTAL_WIDTH = 256
PARTITIONS = (1, 2, 4, 8, 16, 32)
SEQ_LENS = (96, 192, 384)
KEY_SCALE = 0.5
# g5-01 checkpointed every arm at 8 and 16 epochs and nothing moved more than
# 0.014, so 8 is where this rule has finished learning. Measured, not assumed.
EPOCHS = 8


def score(model, sequences, partition) -> float:
    correct = total = 0
    for sequence in sequences:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens, partition=partition)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(seq_len: int, groups: int, lr: float, seed: int) -> dict:
    task = replace(BASE, seq_len=seq_len)
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=TOTAL_WIDTH, partitions=groups,
        lr=lr, key_scale=KEY_SCALE, seed=seed))
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens = np.asarray(train_set[index].tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)

    pooled = score(model, test_set, None)
    alone = score(model, test_set, int(rng.integers(groups)))
    machine_width = TOTAL_WIDTH // groups
    print(f"  seq={seq_len:<4} P={groups:<3} w={machine_width:<4} lr={lr:<5} "
          f"seed={seed}  pooled {pooled:.3f}  alone {alone:.3f}", flush=True)
    return dict(
        condition=f"seq={seq_len} P={groups} lr={lr}", seed=seed,
        seq_len=seq_len, partitions=groups, machine_width=machine_width,
        d_model=TOTAL_WIDTH, lr=lr, accuracy=pooled, pooled=pooled, alone=alone)


def main() -> int:
    args = parse_args(__doc__)
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    groupings = (args.partitions,) if args.partitions else PARTITIONS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    for seq_len in seq_lens:
        for groups in groupings:
            for lr in rates:
                for seed in seeds:
                    records.append(run(seq_len, groups, lr, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
