"""Do tiny devices forget, and does clustering protect them?

g7-02 showed devices of ONE dimension work at every sequence length tested, in
clusters of a few dozen -- for a single body of data. This asks what happens when
a second arrives.

g6-01 cannot answer it: that sweep varies total width while holding partitions at
four, so per-device width moves with total width and the two cannot be separated.
Here total width is pinned at 240 and every device holds one number, so anything
observed is about per-device width alone.

    python experiments/g6_02_tiny_forgetting.py --mode gated --lr 0.05 --seed 1 --json out/x.json
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g6_01_forgetting import BASE, sequences  # noqa: E402
from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

TOTAL_WIDTH = 240
PARTITIONS = TOTAL_WIDTH          # one dimension per device
CLUSTERS = (1, 2, 4, 8, 16, 32, 64, 240)
MODES = ("open", "gated")
LEARNING_RATES = (0.02, 0.05, 0.1, 0.2)
SEEDS = (1, 2, 3)
N_TRAIN, N_TEST, EPOCHS, KEY_SCALE = 400, 120, 8, 0.5


def mask_for(tokens, kinds) -> np.ndarray:
    """Oracle storage mask: keep a binding only where the previous position was a
    pair. Reads task structure a deployed system would not have."""
    return np.array([i > 0 and kinds[i - 1] == "pair" for i in range(len(tokens))])


def build(half: int, count: int, seed: int, gated: bool):
    """Sequences for one task, with an oracle mask when the gate is on.

    `sequences` comes from g6-01 so the two sweeps share one definition of what
    the tasks are — disjoint keys AND values, by a bijection rather than a fold.
    """
    from dataclasses import replace

    from openplexus.tasks.mqar import dataset

    from experiments.g6_01_forgetting import GEN, remap
    built = []
    for sequence in dataset(replace(GEN, seed=seed), count):
        tokens = remap(np.asarray(sequence.tokens), half)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        keep = mask_for(tokens, sequence.position_kinds()) if gated else None
        built.append((tokens, targets, scored, keep, sequence.query_positions))
    return built


def train(model, data, rng) -> None:
    order = np.arange(len(data))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _ = data[index]
            model.run(tokens, targets, scored, learn=True, store=keep)


def score(model, data, members) -> float:
    correct = total = 0
    for tokens, _, _, keep, queries in data:
        predicted = model.run(tokens, partition=members, store=keep)
        for q in queries:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(mode: str, lr: float, seed: int) -> list[dict]:
    gated = mode == "gated"
    rng = np.random.default_rng(seed)
    train_a = build(0, N_TRAIN, BASE.seed, gated)
    train_b = build(1, N_TRAIN, BASE.seed + 4_242, gated)
    test_a = build(0, N_TEST, BASE.seed + 99_991, gated)
    test_b = build(1, N_TEST, BASE.seed + 99_991 + 4_242, gated)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=BASE.vocab_size, d_model=TOTAL_WIDTH, partitions=PARTITIONS,
        lr=lr, key_scale=KEY_SCALE, seed=seed))

    train(model, train_a, rng)
    before = {c: score(model, test_a, list(range(c))) for c in CLUSTERS}
    train(model, train_b, rng)

    records = []
    for cluster in CLUSTERS:
        members = list(range(cluster))
        after = score(model, test_a, members)
        b_after = score(model, test_b, members)
        print(f"  {mode:<6} lr={lr:<5} seed={seed} cluster={cluster:<4} "
              f"A {before[cluster]:.3f} -> {after:.3f}  B {b_after:.3f}",
              flush=True)
        records.append(dict(
            condition=f"{mode} lr={lr} cluster={cluster}", seed=seed,
            mode=mode, lr=lr, cluster=cluster, accuracy=after,
            a_before=before[cluster], a_after=after, b_after=b_after))
    return records


def main() -> int:
    args = parse_args(__doc__)
    modes = (args.mode,) if args.mode else MODES
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    for mode in modes:
        if mode not in MODES:
            raise ValueError(f"mode must be one of {MODES}, got {mode}")
        for lr in rates:
            for seed in seeds:
                records.extend(run(mode, lr, seed))
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
