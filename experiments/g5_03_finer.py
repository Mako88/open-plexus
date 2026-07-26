"""How fast does the minimum machine size grow? With a ruler fine enough to say.

g5-02 held total width at 256 and could only test machine widths that divide it:
8, 16, 32, 64. Factor-of-two steps, measuring a quantity that moves by about a
factor of two across the range -- so the exponent came back 0.50 +/- 0.50, which
contains both the favourable answer and the unfavourable one.

Total width 240 has divisors 8, 10, 12, 15, 16, 20, 24, 30, 40, 48, 60. Same
capacity, worst step 1.33x instead of 2.00x. Adding seq_len 48 stretches the span
from 4x to 8x, which sharpens the exponent as much again and is the cheapest row
rather than the dearest.

    python experiments/g5_03_finer.py --seqlen 384 --partitions 15 --lr 0.05 --json out/x.json
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
# 240, not 256. A power of two has almost no divisors in the range that matters,
# so the tidy-looking default silently set the resolution of the whole sweep.
TOTAL_WIDTH = 240
# P=40 (machine width 6) is here because a pre-dispatch control found the seq_len
# 48 floor BELOW the original grid: width 8 still scored 0.975 alone. Without it
# that row would be a bound rather than a value, the span would collapse from 8x
# back to 4x, and the resolution with it. P=4 dropped to stay under GitHub's
# 256-job matrix cap -- width 60 is far above any floor observed.
PARTITIONS = (1, 6, 8, 10, 12, 15, 16, 20, 24, 30, 40)
SEQ_LENS = (48, 96, 192, 384)
KEY_SCALE = 0.5
EPOCHS = 8   # g5-01 measured that nothing moves after this


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
    print(f"  seq={seq_len:<4} P={groups:<3} w={TOTAL_WIDTH // groups:<4} "
          f"lr={lr:<5} seed={seed}  pooled {pooled:.3f}  alone {alone:.3f}",
          flush=True)
    return dict(
        condition=f"seq={seq_len} P={groups} lr={lr}", seed=seed,
        seq_len=seq_len, partitions=groups,
        machine_width=TOTAL_WIDTH // groups, d_model=TOTAL_WIDTH, lr=lr,
        accuracy=pooled, pooled=pooled, alone=alone)


def main() -> int:
    args = parse_args(__doc__)
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    groupings = (args.partitions,) if args.partitions else PARTITIONS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    for seq_len in seq_lens:
        for groups in groupings:
            if TOTAL_WIDTH % groups:
                raise ValueError(
                    f"{groups} machines do not divide a width of {TOTAL_WIDTH}")
            for lr in rates:
                for seed in seeds:
                    records.append(run(seq_len, groups, lr, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
