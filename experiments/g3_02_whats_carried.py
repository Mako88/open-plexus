"""What survives ablation? — the deciding experiment.

g1-07 ruled out convergence: a from-scratch model at width 32 is flat at 0.18
from one epoch to sixty-four. Yet g3-01's model, ablated *down* to width 32,
reaches 0.924. Same architecture, same live dimensions, same data.

Two candidates, and one run separates them.

    (a) INITIALISATION SCALE. Projections are drawn with standard deviation
        1/sqrt(d_model), so a d=64 model ablated to 32 live dimensions has keys
        of norm ~0.71 where a native d=32 model has ~1.0.
    (b) THE TRAINED READOUT. The ablated model learned for four epochs at width
        64 before losing half its input, and kept whatever that put in the
        surviving columns.

The separating condition is `cold`: a d=64 model ablated **at initialisation**,
before any training. It has (a)'s scale and (b)'s cold readout.

    python experiments/g3_02_whats_carried.py --mode cold --seed 3 --json out/x.json
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
N_TRAIN, N_TEST, LR, EPOCHS = 400, 120, 0.05, 8
SEEDS = tuple(range(1, 7))
SURVIVING = 32

#: native  -- a real d=32 model. The from-scratch baseline. Expect ~0.18.
#: cold    -- d=64 ablated to 32 BEFORE training. Isolates initialisation scale.
#: warm    -- d=64 ablated to 32 halfway through. Reproduces g3-01. Expect ~0.92.
#: rescaled-- native d=32 with its projections scaled to match cold's norms.
#:            Included so that if `cold` explains it, the mechanism is pinned to
#:            SCALE rather than to something else about being born at 64.
MODES = ("native", "cold", "warm", "rescaled")


def build(mode: str, seed: int):
    if mode == "native":
        return LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=TASK.vocab_size, d_model=SURVIVING, lr=LR, seed=seed)), None
    if mode == "rescaled":
        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=TASK.vocab_size, d_model=SURVIVING, lr=LR, seed=seed))
        factor = np.sqrt(SURVIVING / 64.0)
        model.wk *= factor
        model.wv *= factor
        return model, None
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=64, lr=LR, seed=seed))
    gone = np.random.default_rng(seed).choice(64, size=64 - SURVIVING,
                                              replace=False)
    return model, gone


def run(mode: str, seed: int) -> dict:
    rng = np.random.default_rng(seed + 5000)
    train_set = dataset(TASK, N_TRAIN)
    test_set = dataset(replace(TASK, seed=TASK.seed + 99_991), N_TEST)
    model, gone = build(mode, seed)
    if mode == "cold":
        model.ablate(gone)

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

    for e in range(EPOCHS):
        if mode == "warm" and e == EPOCHS // 2:
            model.ablate(gone)
        epoch()

    correct = total = 0
    for sequence in test_set:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens)
        for q in sequence.query_positions:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    key_norm = float(np.linalg.norm(model.wk, axis=1).mean())
    return dict(condition=mode, seed=seed, accuracy=correct / total,
                surviving_width=model.surviving_width(), key_norm=key_norm)


def main() -> int:
    args = parse_args(__doc__)
    seeds = (args.seed,) if args.seed is not None else SEEDS
    modes = (args.mode,) if args.mode else MODES
    records = []
    for mode in modes:
        for seed in seeds:
            record = run(mode, seed)
            records.append(record)
            print(f"  {mode:<10} seed={seed:<3} width={record['surviving_width']:<3} "
                  f"key_norm={record['key_norm']:.3f} acc={record['accuracy']:.3f}",
                  flush=True)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
