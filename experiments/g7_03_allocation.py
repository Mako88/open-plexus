"""Given a machine of a certain power, how should it be spent?

A machine holding `C` dimensions can run one node of width `C`, or `C` nodes of
width 1, or anything between. All use the same memory and the same arithmetic;
they differ in how many independent readouts the machine keeps, and therefore in
how its answers pool. Which is best is the deployment decision and it is
unmeasured -- every sweep so far pinned either the node width or the cluster size.

Nodes hosted by one machine pool for free, so a cluster is naturally a machine.

    python experiments/g7_03_allocation.py --width 8 --mode gated --lr 0.05 --json out/x.json

`--width` is the NODE width here, not the network width, which is fixed at 256.
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

# 256 deliberately: the question is about FACTORISATIONS, and powers of two
# factorise cleanly. g5-03 moved off 256 for the opposite reason -- its divisors
# are too sparse to resolve a threshold. Different question, opposite choice.
TOTAL_WIDTH = 256
NODE_WIDTHS = (1, 2, 4, 8, 16, 32, 64)
SEQ_LEN = 384
MODES = ("open", "gated")
LEARNING_RATES = (0.02, 0.05, 0.1, 0.2)
SEEDS = (1, 2, 3)
N_TRAIN, N_TEST, EPOCHS, KEY_SCALE = 400, 120, 8, 0.5

BASE = MqarConfig(n_pairs=4, seq_len=SEQ_LEN, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260726)


def prepare(count: int, seed: int, gated: bool):
    built = []
    for sequence in dataset(replace(BASE, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        kinds = sequence.position_kinds()
        keep = (np.array([i > 0 and kinds[i - 1] == "pair"
                          for i in range(len(tokens))]) if gated else None)
        built.append((tokens, targets, scored, keep, sequence.query_positions))
    return built


def run(node_width: int, mode: str, lr: float, seed: int) -> list[dict]:
    if TOTAL_WIDTH % node_width:
        raise ValueError(
            f"node width {node_width} does not divide {TOTAL_WIDTH}")
    nodes = TOTAL_WIDTH // node_width
    gated = mode == "gated"
    rng = np.random.default_rng(seed)
    train_set = prepare(N_TRAIN, BASE.seed, gated)
    test_set = prepare(N_TEST, BASE.seed + 99_991, gated)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=BASE.vocab_size, d_model=TOTAL_WIDTH, partitions=nodes,
        lr=lr, key_scale=KEY_SCALE, seed=seed))
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True, store=keep)

    records = []
    cluster = 1
    while cluster <= nodes:
        members = list(range(cluster))
        correct = total = 0
        for tokens, _, _, keep, queries in test_set:
            predicted = model.run(tokens, partition=members, store=keep)
            for q in queries:
                correct += predicted[q] == tokens[q + 1]
                total += 1
        accuracy = correct / total
        print(f"  node_w={node_width:<4} nodes={nodes:<4} {mode:<6} "
              f"lr={lr:<5} seed={seed} cluster={cluster:<4} "
              f"capacity={node_width * cluster:<5} {accuracy:.3f}", flush=True)
        records.append(dict(
            condition=f"w={node_width} {mode} lr={lr} cluster={cluster}",
            seed=seed, node_width=node_width, nodes=nodes, cluster=cluster,
            capacity=node_width * cluster, mode=mode, lr=lr,
            d_model=TOTAL_WIDTH, accuracy=accuracy))
        cluster *= 2
    return records


def main() -> int:
    args = parse_args(__doc__)
    widths = (args.width,) if args.width else NODE_WIDTHS
    modes = (args.mode,) if args.mode else MODES
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    for node_width in widths:
        for mode in modes:
            if mode not in MODES:
                raise ValueError(f"mode must be one of {MODES}, got {mode}")
            for lr in rates:
                for seed in seeds:
                    records.extend(run(node_width, mode, lr, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
