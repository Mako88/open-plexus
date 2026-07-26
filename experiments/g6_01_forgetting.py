"""Does sparsity protect old learning from new?

g1-06-era work measured sparsity INSIDE one problem and found it negative. That
is not what sparse codes are known for: their classic job is keeping new learning
from wiping out old, which is a question about moving between bodies of data.

Train on A, then on B, then re-test A. A and B share everything except the value
alphabet, which is disjoint -- the delta rule pushes non-target rows down, so
training on B actively suppresses the rows A depends on.

    python experiments/g6_01_forgetting.py --scale 8 --lr 0.05 --seed 1 --json out/x.json

`--scale` carries key_active here: 0 is the dense signed projection.
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

# n_values=16 so the alphabet splits cleanly into two disjoint halves of 8.
BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=16,
                  autoregressive=True, filler="random", seed=20260726)
HALF = 8
N_TRAIN, N_TEST, EPOCHS = 400, 120, 8
D_MODEL, PARTITIONS, KEY_SCALE = 64, 4, 0.5
SEEDS = (1, 2, 3)
LEARNING_RATES = (0.02, 0.05, 0.1)
SPARSITIES = (0, 32, 16, 8, 4)


def remap(tokens: np.ndarray, half: int) -> np.ndarray:
    """Fold a sequence into one half of BOTH alphabets -- keys and values.

    **Keys as well as values, and that is the whole design.** The first version
    split only the values, leaving A and B sharing the key alphabet. A control
    run showed both dense and sparse forgetting equally and completely, 0.990 ->
    0.008 and 0.781 -> 0.010, and the reason is that sparsity separates
    ADDRESSES: if two tasks address the same keys they retrieve through the same
    columns, and no amount of sparsity keeps their learning apart.

    Splitting the keys is what gives the two tasks distinct active columns, which
    is the only condition under which the mechanism can work at all. Caught by
    running the control before dispatching, at a cost of two thirty-second runs.

    Keys occupy `[0, n_keys)` and values `[n_keys, n_keys + n_values)`; each is
    folded into its own half. Done outside the generator, so the task itself is
    untouched and every earlier result still describes the same MQAR.
    """
    out = np.array(tokens)
    key_half = BASE.n_keys // 2
    is_key = out < BASE.n_keys
    out[is_key] = (out[is_key] % key_half) + half * key_half

    lo, hi = BASE.n_keys, BASE.n_keys + BASE.n_values
    is_value = (tokens >= lo) & (tokens < hi)
    out[is_value] = lo + (tokens[is_value] - lo) % HALF + half * HALF
    return out


def sequences(half: int, count: int, seed: int):
    """Sequences for one task, as (tokens, targets, scored, query positions)."""
    built = []
    for sequence in dataset(replace(BASE, seed=seed), count):
        tokens = remap(np.asarray(sequence.tokens), half)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        built.append((tokens, targets, scored, sequence.query_positions))
    return built


def train(model, data, rng, epochs: int = EPOCHS) -> None:
    order = np.arange(len(data))
    for _ in range(epochs):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, _ = data[index]
            model.run(tokens, targets, scored, learn=True)


def score(model, data) -> float:
    correct = total = 0
    for tokens, _, _, queries in data:
        predicted = model.run(tokens)
        for q in queries:
            correct += predicted[q] == tokens[q + 1]
            total += 1
    return correct / total


def run(active: int, lr: float, seed: int) -> dict:
    rng = np.random.default_rng(seed)
    train_a = sequences(0, N_TRAIN, BASE.seed)
    train_b = sequences(1, N_TRAIN, BASE.seed + 4_242)
    test_a = sequences(0, N_TEST, BASE.seed + 99_991)
    test_b = sequences(1, N_TEST, BASE.seed + 99_991 + 4_242)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=BASE.vocab_size, d_model=D_MODEL, partitions=PARTITIONS,
        lr=lr, key_scale=KEY_SCALE, key_active=active, seed=seed))

    train(model, train_a, rng)
    a_before = score(model, test_a)
    train(model, train_b, rng)
    a_after, b_after = score(model, test_a), score(model, test_b)

    print(f"  active={active:<4} lr={lr:<5} seed={seed}  A {a_before:.3f} -> "
          f"{a_after:.3f} (kept {a_after / a_before if a_before else 0:.2f})  "
          f"B {b_after:.3f}", flush=True)
    return dict(condition=f"d={D_MODEL} active={active} lr={lr}", seed=seed,
                d_model=D_MODEL, key_active=active, lr=lr, accuracy=a_after,
                a_before=a_before, a_after=a_after, b_after=b_after,
                retained=a_after / a_before if a_before else 0.0)


def main() -> int:
    args = parse_args(__doc__)
    actives = (int(args.scale),) if args.scale is not None else SPARSITIES
    global D_MODEL
    if args.width:
        D_MODEL = args.width
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = [run(a, lr, s) for a in actives for lr in rates for s in seeds]
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
