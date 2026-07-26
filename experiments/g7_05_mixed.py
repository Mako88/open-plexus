"""Does a weak machine drag down a strong one?

Our machines combine by adding their answers. A tiny machine's answer is mostly
noise, and adding noise to a good answer can make it worse. If it does, a real
deployment cannot simply let everyone join -- it needs to weight or exclude, which
is coordination, which is what this project exists to avoid.

Heterogeneity needs no new mechanism: nodes stay identical at one dimension each
and machines differ in how many they host, which `run(partition=[...])` already
supports. Two-level pooling adds nothing, since addition is associative -- so what
is being tested is membership, not hierarchy.

    python experiments/g7_05_mixed.py --mode gated --lr 0.05 --seed 1 --json out/x.json
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

TOTAL_WIDTH = 256
PARTITIONS = TOTAL_WIDTH            # one dimension per node
# Capped at 16 so that even the strong+strong control (32 nodes) stays under the
# point where pooling stops paying. A pre-dispatch control found that ungated,
# 32 nodes score 0.558 and 64 score 0.521 -- adding a second strong machine makes
# things WORSE. Past saturation, membership cannot be measured because nothing
# changes anything.
SIZES = (1, 2, 4, 8, 16)
MODES = ("open", "gated")
LEARNING_RATES = (0.02, 0.05, 0.1, 0.2)
SEEDS = (1, 2, 3)
N_TRAIN, N_TEST, EPOCHS, KEY_SCALE = 400, 60, 8, 0.5

BASE = MqarConfig(n_pairs=4, seq_len=384, n_keys=32, n_values=8,
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


def run(mode: str, lr: float, seed: int) -> list[dict]:
    gated = mode == "gated"
    rng = np.random.default_rng(seed)
    train_set = prepare(N_TRAIN, BASE.seed, gated)
    test_set = prepare(N_TEST, BASE.seed + 99_991, gated)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=BASE.vocab_size, d_model=TOTAL_WIDTH, partitions=PARTITIONS,
        lr=lr, key_scale=KEY_SCALE, seed=seed))
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True, store=keep)

    def score(members):
        correct = total = 0
        for tokens, _, _, keep, queries in test_set:
            predicted = model.run(tokens, partition=members, store=keep)
            for q in queries:
                correct += predicted[q] == tokens[q + 1]
                total += 1
        return correct / total

    records = []
    for strong in SIZES:
        if 2 * strong > PARTITIONS:
            continue
        # Both of these depend only on , so they are computed once per
        # strong rather than once per pair. The first draft recomputed the
        # strong+strong control six times over, which made evaluation cost three
        # times training for no information.
        alone = score(list(range(strong)))
        doubled = score(list(range(strong)) + list(range(strong, 2 * strong)))
        for weak in SIZES:
            if strong + weak > PARTITIONS:
                continue
            # Disjoint membership, and the strong+strong control uses a DIFFERENT
            # second block so it is not the same nodes counted twice -- `run`
            # refuses duplicates, which is how that mistake was caught before.
            mixed = score(list(range(strong)) + list(range(strong, strong + weak)))
            print(f"  {mode:<6} lr={lr:<5} seed={seed} strong={strong:<3} "
                  f"weak={weak:<3}  alone {alone:.3f}  +weak {mixed:.3f}  "
                  f"+strong {doubled:.3f}", flush=True)
            records.append(dict(
                condition=f"{mode} lr={lr} S={strong} W={weak}", seed=seed,
                mode=mode, lr=lr, strong=strong, weak=weak,
                accuracy=mixed, alone=alone, mixed=mixed, doubled=doubled))
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
