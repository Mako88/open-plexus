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

# BASE describes the MODEL's vocabulary: two tasks' worth of keys and values.
# GEN describes what is actually generated -- one task's worth -- which is then
# shifted into whichever half it belongs to. Generating small and shifting is
# what keeps the two alphabets disjoint without folding anything together.
BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=16,
                  autoregressive=True, filler="random", seed=20260726)
GEN = replace(BASE, n_keys=16, n_values=8)
N_TRAIN, N_TEST, EPOCHS = 400, 120, 8
D_MODEL, PARTITIONS, KEY_SCALE = 64, 4, 0.5
SEEDS = (1, 2, 3)
LEARNING_RATES = (0.02, 0.05, 0.1)
SPARSITIES = (0, 32, 16, 8, 4)


def remap(tokens: np.ndarray, half: int) -> np.ndarray:
    """Translate generator tokens into one task's private half of the vocabulary.

    **A bijection, not a fold, and that distinction is the whole correctness of
    this experiment.** The first version folded the full 32-key alphabet down to
    16 with a modulo, which quietly made keys `k` and `k + 16` the same token. In
    600 sampled sequences, *every one* contained two distinct keys that collided,
    and 82 of 2400 queries ended up with two different correct answers.

    That is [g1-01](../experiments/sweeps/g1-01-predictability.txt)'s bug exactly:
    a benchmark that cannot be answered, producing numbers that look like a
    result. Caught by checking well-posedness before dispatching, which is the
    rule that experience wrote.

    So sequences are GENERATED over a small alphabet (`GEN`) and then shifted
    into a task-specific range of the model's larger vocabulary. Nothing folds,
    nothing collides, and the two tasks share no token at all.
    """
    out = np.array(tokens)
    is_key = tokens < GEN.n_keys
    lo, hi = GEN.n_keys, GEN.n_keys + GEN.n_values
    is_value = (tokens >= lo) & (tokens < hi)

    out[is_key] = tokens[is_key] + half * GEN.n_keys
    out[is_value] = (BASE.n_keys + half * GEN.n_values
                     + (tokens[is_value] - lo))
    # Anything else is padding and belongs to neither task.
    out[~(is_key | is_value)] = BASE.pad_token
    return out


def sequences(half: int, count: int, seed: int):
    """Sequences for one task, as (tokens, targets, scored, query positions)."""
    built = []
    for sequence in dataset(replace(GEN, seed=seed), count):
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
