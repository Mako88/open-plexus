"""G3 — what happens when a machine leaves and never comes back?

C3 has been a principle for the whole project and has never been tested, because
nothing has ever left. This is different from G2's lost messages: a dropped
message is transient, a departed machine takes its share of the state and does
not return.

    python experiments/g3_01_churn.py --churn 0.25 --seed 3 --json out/x.json

Measures accuracy at three points: before the machines leave, immediately after
(with no further training), and after the remaining epochs. The gap between the
last two is recovery, and the sharp question is whether the recovered value
matches the width curve at the *surviving* width — if it does, churn tolerance is
arithmetic rather than something that must be measured each time.
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
D_MODEL, EPOCHS, N_TRAIN, N_TEST, LR = 64, 8, 400, 120, 0.05
SEEDS = tuple(range(1, 7))
CHURNS = (0.0, 0.125, 0.25, 0.375, 0.5, 0.75)


def score(model, sequences) -> float:
    correct = total = 0
    for sequence in sequences:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(churn: float, seed: int) -> dict:
    rng = np.random.default_rng(seed)
    train_set = dataset(TASK, N_TRAIN)
    test_set = dataset(replace(TASK, seed=TASK.seed + 99_991), N_TEST)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=D_MODEL, lr=LR, seed=seed))

    def epoch():
        order = np.arange(len(train_set))
        rng.shuffle(order)
        for index in order:
            sequence = train_set[index]
            tokens = np.asarray(sequence.tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)

    for _ in range(EPOCHS // 2):
        epoch()
    before = score(model, test_set)

    # The machines leave. Which dimensions go is drawn per seed rather than
    # always taking the first ones — a fixed slice could coincide with whatever
    # structure the initialisation happened to put there.
    n_gone = int(round(churn * D_MODEL))
    gone = rng.choice(D_MODEL, size=n_gone, replace=False) if n_gone else []
    model.ablate(gone)
    immediately_after = score(model, test_set)

    for _ in range(EPOCHS - EPOCHS // 2):
        epoch()
    recovered = score(model, test_set)

    return dict(condition=f"churn={churn}", seed=seed, churn=churn,
                surviving_width=model.surviving_width(),
                before=before, immediately_after=immediately_after,
                accuracy=recovered)


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    churns = (args.churn,) if args.churn is not None else CHURNS

    records = []
    for churn in churns:
        for seed in seeds:
            record = run(churn, seed)
            records.append(record)
            print(f"  churn={churn:<6} seed={seed:<3} "
                  f"width {record['surviving_width']:<3} "
                  f"before={record['before']:.3f} "
                  f"after={record['immediately_after']:.3f} "
                  f"recovered={record['accuracy']:.3f}", flush=True)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
