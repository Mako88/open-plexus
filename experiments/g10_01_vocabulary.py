"""Does a bigger vocabulary cost width, and does it cost log(vocabulary)?

[Note 020](../docs/archive/notes/020-the-capacity-equation-checked.md) checked this
project's central equation against Clarkson, Ubaru & Yang. Theorem 20 covers
exactly our object — a bundle of key-value pairs, asking whether a binding is in
it — and gives

    m = O(n log(d / delta))

with `m` our width, `n` our stored-binding count, and `d` the **universe size**.

The linear dependence on `n` agrees with the empirical `SNR = sqrt(d_model / N)`.
**The `log d` term appears in neither the fit nor anywhere else in this project,
because every sweep has held the vocabulary at 41 tokens.** A fit cannot see a
logarithm in a variable that never moved.

`n_values` is pinned at 8 throughout. The trivial floor is
`1/n_pairs + (1 - 1/n_pairs)/n_values`, so moving the value alphabet would move
the bar the crossing point is measured against — and a crossing-point comparison
against a moving bar is not a comparison. Growing `n_keys` grows the universe and
leaves the floor at 0.34375.

`n_pairs` and `seq_len` are pinned too, so `N` is constant across the grid. **The
whole point is to vary `d` with `n` fixed**, which is the axis the theorem
separates and this project never has.

    python experiments/g10_01_vocabulary.py --sweep degrade
    python experiments/g10_01_vocabulary.py --keys 128 --lr 0.05 --workers 3 --json out/x.json
"""

from __future__ import annotations

import math
import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from experiments.harness import emit, parse_args, spread  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

BASE = MqarConfig(n_pairs=4, seq_len=192, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260726)
KEY_ALPHABETS = (32, 128, 512, 2048)
WIDTHS = (8, 16, 32, 64, 128)
LEARNING_RATES = (0.02, 0.05, 0.1)
SEEDS = (1, 2, 3)
N_TRAIN, N_TEST, EPOCHS, KEY_SCALE, DECAY = 200, 80, 6, 0.5, 0.99
#: Unchanged across the grid, because n_values is pinned.
TRIVIAL_FLOOR = 1 / BASE.n_pairs + (1 - 1 / BASE.n_pairs) / BASE.n_values
#: Where the crossing is measured. Comfortably clear of the floor, and low
#: enough that a narrow model can reach it at the smallest vocabulary.
BAR = 0.60


def build(task: MqarConfig, count: int, seed: int):
    """Now `harness.mqar_batch`. Behaviour identical; the body was one of three
    byte-identical copies."""
    return harness.mqar_batch(task, count, seed)


def score(task: MqarConfig, width: int, lr: float, seed: int,
          train_set, test_set) -> float:
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=width, lr=lr,
        key_scale=KEY_SCALE, decay=DECAY, seed=seed))
    rng = np.random.default_rng(seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True)

    right = total = 0
    for tokens, _, _, queries in test_set:
        predicted = model.run(tokens)
        for q in queries:
            right += predicted[q] == tokens[q + 1]
            total += 1
    return right / total


def one_seed(work: tuple) -> list[dict]:
    n_keys, seed, rates = work
    task = replace(BASE, n_keys=n_keys)
    train_set = build(task, N_TRAIN, seed)
    test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)
    return [dict(condition=f"keys={n_keys} width={width} lr={lr}",
                 seed=seed, n_keys=n_keys, vocab=task.vocab_size,
                 width=width, lr=lr,
                 accuracy=score(task, width, lr, seed, train_set, test_set))
            for width in WIDTHS for lr in rates]


def control() -> int:
    """Does the width grid BRACKET the crossing at both ends of the vocabulary?

    Prediction 4, and the cheapest thing that can invalidate the sweep: if every
    width clears the bar at the largest vocabulary, or none clears it at the
    smallest, the grid does not contain the crossing point and the comparison
    has nothing to compare. One seed, one learning rate, reduced training.
    """
    print(f"trivial floor {TRIVIAL_FLOOR:.3f}   bar {BAR:.2f}   "
          f"(one seed, reduced training -- bracketing only, not a result)")
    print(f"{'n_keys':>8}{'vocab':>7}{'log':>6}" + "".join(f"{w:>8}" for w in WIDTHS))
    for n_keys in (KEY_ALPHABETS[0], KEY_ALPHABETS[-1]):
        task = replace(BASE, n_keys=n_keys)
        train_set = build(task, 60, 1)
        test_set = build(replace(task, seed=task.seed + 99_991), 30, 1)
        row = [f"{n_keys:>8}{task.vocab_size:>7}{math.log(task.vocab_size):>6.1f}"]
        for width in WIDTHS:
            row.append(f"{score(task, width, 0.05, 1, train_set, test_set):>8.3f}")
        print("".join(row), flush=True)
    print(f"\nThe grid brackets the crossing only if each row has a width below "
          f"{BAR}\nAND a width above it. A row entirely on one side measures "
          f"nothing.")
    return 0


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    if args.sweep == "degrade":
        return control()
    alphabets = (args.keys,) if args.keys else KEY_ALPHABETS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS
    work = [(n_keys, seed, tuple(rates))
            for n_keys in alphabets for seed in seeds]
    records = [r for batch in spread(one_seed, work, args.workers) for r in batch]
    emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
