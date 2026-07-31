"""Does adding machines buy capability, or only capacity?

g4-01 grew the network against a fixed problem and both pooled and single-machine
accuracy rose. That shows capacity does not collapse; it does not show capability
grows, because the problem was held still while the model got bigger.

This grows both. Each machine is fixed at 16 dimensions, the network grows, and
the question is how many MACHINES are needed to hold 0.9 as the sequence
lengthens -- against g1-10's separately measured width exponent of 0.37.

    python experiments/g5_01_scaling.py --width 128 --seqlen 384 --lr 0.05 --json out/x.json
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
# Five wide, spanning both edges g4-01 pinned against. A parameter swept on every
# arm is still unswept if the sweep does not contain the optimum.
LEARNING_RATES = (0.01, 0.02, 0.05, 0.1, 0.2)
MACHINE_WIDTH = 16
WIDTHS = (16, 32, 64, 128, 256)     # P = width // MACHINE_WIDTH
SEQ_LENS = (48, 96, 192, 384)
KEY_SCALE = 0.5
CHECKPOINTS = (8, 16)


def score(model, sequences, partition) -> float:
    correct = total = 0
    for sequence in sequences:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens, partition=partition)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(width: int, seq_len: int, lr: float, seed: int,
        split: str = "dimension") -> list[dict]:
    groups = width // MACHINE_WIDTH
    task = replace(BASE, seq_len=seq_len)
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    # `split="concept"` swaps DIMENSION partitioning for CONCEPT partitioning and
    # changes nothing else, so the two arms differ only in how the store is cut.
    # **Default unchanged**, so g5-01's published grid still reproduces exactly.
    #
    # Why it is worth an arm (`g29-01`): this sweep's own conclusion is that *"the
    # wall is caused by the partitioning, not by the underlying rule"* -- and that
    # wall was measured on dimension splitting, where every read is a fragment
    # summed across machines. A concept-partitioned read is served whole by the one
    # node holding the fact. No sweep had ever varied a scale axis under it.
    #
    # **`alone` MEANS SOMETHING DIFFERENT under `concept` and must not be compared
    # across arms**: a lone dimension node holds a slice of the WIDTH, a lone
    # concept node holds a subset of the FACTS. The exponent is fitted on `pooled`
    # (`tools/summarise_g5_01.py` crosses on `best_pooled`), which is the whole
    # system either way and is the comparable quantity.
    partitioning = ({"concept_nodes": groups} if split == "concept"
                    else {"partitions": groups})
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=width,
        lr=lr, key_scale=KEY_SCALE, seed=seed, **partitioning))
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
        # One machine picked at random rather than group 0, and reported per
        # seed: a deployment gets whichever machine it gets.
        # **NOT AVAILABLE under concept partitioning, and not faked.** `score`'s
        # third argument is a DIMENSION group index passed to `run(partition=)`,
        # and a concept-partitioned model has one dimension group -- asking for
        # group 1 raises, which is the model telling the truth. A lone concept
        # node holds a subset of the FACTS and there is no API exposing that
        # score, so it is `nan` rather than a number that would be compared to
        # g5-01's `alone` by someone reading a column.
        alone = (float("nan") if split == "concept"
                 else score(model, test_set, int(rng.integers(groups))))
        records.append(dict(
            condition=f"{split} P={groups} seq={seq_len} lr={lr} e={epoch}",
            seed=seed, d_model=width, partitions=groups, split=split,
            seq_len=seq_len,
            lr=lr, epoch=epoch, accuracy=pooled, pooled=pooled, alone=alone))
        print(f"  P={groups:<3} d={width:<4} seq={seq_len:<4} lr={lr:<5} "
              f"seed={seed} e={epoch:<3} pooled {pooled:.3f}  alone {alone:.3f}",
              flush=True)
    return records


def main() -> int:
    args = parse_args(__doc__)
    widths = (args.width,) if args.width else WIDTHS
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    seeds = (args.seed,) if args.seed is not None else SEEDS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    # `--mode concept` selects CONCEPT partitioning (g29-01). Anything else keeps
    # the dimension splitting this sweep was published on.
    split = "concept" if getattr(args, "mode", None) == "concept" else "dimension"
    if split == "concept":
        print("SPLIT = CONCEPT. `alone` is one node's FACTS, not one width slice, "
              "and is not comparable to g5-01's `alone`.", flush=True)

    records = []
    for width in widths:
        for seq_len in seq_lens:
            for lr in rates:
                for seed in seeds:
                    records.extend(run(width, seq_len, lr, seed, split))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
