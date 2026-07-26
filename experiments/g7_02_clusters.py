"""How small can a node be, if a few of them may pool?

Devices are pinned at ONE dimension each -- the smallest a device can be -- and
the question is what cluster size is needed, with and without selective storage.

Cluster size is a read-time choice, so one trained model is evaluated at every
cluster size. That collapses the grid and removes a confound: every cluster size
in a row shares the identical trained model, so differences between them cannot
be training noise.

    python experiments/g7_02_clusters.py --seqlen 384 --mode gated --lr 0.05 --json out/x.json
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
TOTAL_WIDTH = 240
PARTITIONS = TOTAL_WIDTH          # every device holds exactly one dimension
CLUSTERS = (1, 2, 4, 8, 16, 32, 64, 240)
SEQ_LENS = (96, 192, 288, 384)
MODES = ("open", "gated")
LEARNING_RATES = (0.02, 0.05, 0.1, 0.2)
SEEDS = (1, 2, 3)
N_TRAIN, N_TEST, EPOCHS, KEY_SCALE = 400, 120, 8, 0.5


def prepare(config: MqarConfig, count: int, seed: int):
    """Sequences with an oracle storage mask attached.

    The mask keeps a binding only where the PREVIOUS position carried a pair, so
    what survives is exactly the key-to-value bindings the task can ask about.
    It reads `position_kinds()`, which a deployed system does not have -- this is
    a ceiling, not a mechanism.
    """
    built = []
    for sequence in dataset(replace(config, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        kinds = sequence.position_kinds()
        keep = np.array([i > 0 and kinds[i - 1] == "pair"
                         for i in range(len(tokens))])
        built.append((tokens, targets, scored, keep, sequence.query_positions))
    return built


def run(seq_len: int, mode: str, lr: float, seed: int) -> list[dict]:
    task = replace(BASE, seq_len=seq_len)
    rng = np.random.default_rng(seed)
    train_set = prepare(task, N_TRAIN, task.seed)
    test_set = prepare(task, N_TEST, task.seed + 99_991)
    gated = mode == "gated"

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=TOTAL_WIDTH,
        partitions=PARTITIONS, lr=lr, key_scale=KEY_SCALE, seed=seed))
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True,
                      store=keep if gated else None)

    records = []
    for size in CLUSTERS:
        if size > PARTITIONS:
            continue
        # A contiguous block, which is what physically neighbouring devices
        # would form. Members are distinct, which `run` enforces.
        members = list(range(size))
        correct = total = 0
        for tokens, _, _, keep, queries in test_set:
            predicted = model.run(tokens, partition=members,
                                  store=keep if gated else None)
            for q in queries:
                correct += predicted[q] == tokens[q + 1]
                total += 1
        accuracy = correct / total
        print(f"  seq={seq_len:<4} {mode:<6} lr={lr:<5} seed={seed} "
              f"cluster={size:<4} {accuracy:.3f}", flush=True)
        records.append(dict(
            condition=f"seq={seq_len} {mode} lr={lr} cluster={size}",
            seed=seed, seq_len=seq_len, mode=mode, lr=lr, cluster=size,
            d_model=TOTAL_WIDTH, partitions=PARTITIONS, accuracy=accuracy))
    return records


def main() -> int:
    args = parse_args(__doc__)
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    modes = (args.mode,) if args.mode else MODES
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    for seq_len in seq_lens:
        for mode in modes:
            if mode not in MODES:
                raise ValueError(f"mode must be one of {MODES}, got {mode}")
            for lr in rates:
                for seed in seeds:
                    records.extend(run(seq_len, mode, lr, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
