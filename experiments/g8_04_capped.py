"""Does bounding the fast store make skewed input measurable?

[Note 018](../docs/notes/018-the-fast-store-has-no-brakes.md): the fast store is
a geometric series in `decay`, repetition drives it toward `1 / (1 - decay)`, and
the delta-rule update is quadratic in it, so it diverges.

Measured **through the model** by bisection, the store's norm is 2-5 at uniform
filler and 10-50 at `zipf_s` 2.0. (`g8_02_runaway.py` reports 114 and 967 for the
same thing; it reimplements the store with its own scales, so its ratios transfer
and its absolute numbers do not. Cap values taken from them were fifty times too
large and never bound -- caught by this file's own control.)

[g8-02](sweeps/g8-02-when-the-statistics-are-real.txt) could use only two of its
five cells for exactly that reason: at `zipf_s` 1.0 and above the ungated arm
falls below the trivial floor, and **word frequencies sit near 1.0**.

**The measurement is how many cells become usable**, not an accuracy. A cell is
usable when the ungated arm clears the trivial floor, because a recovery ratio
whose denominator is the gap between a working ceiling and a broken floor is not
a recovery of anything.

Run with `--sweep degrade` for the pre-dispatch control, which asks only whether
each candidate cap binds where it should and nowhere else.

    python experiments/g8_04_capped.py --sweep degrade
    python experiments/g8_04_capped.py --scale 1.0 --lr 0.05 --workers 3 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g8_01_real_gate import (  # noqa: E402
    ARMS, BASE, D_MODEL, EPOCHS, KEY_SCALE, N_TEST, N_TRAIN, build, decay_for)
from experiments.harness import emit, parse_args, spread  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

SEQ_LEN, HALF_LIFE = 768, 0.25
EXPONENTS = (0.0, 0.5, 1.0, 1.5, 2.0)
LEARNING_RATES = (0.02, 0.05, 0.1)
SEEDS = (1, 2, 3)

# Measured THROUGH THE MODEL, after the first attempt failed.
#
# The first values, 150 and 300, came from experiments/g8_02_runaway.py -- which
# builds its own `wk`/`wv` and its own loop. **A reimplementation has its own
# scales, and those did not transfer.** The control ran and found caps of 150 and
# 300 changing nothing at any exponent: identical accuracy to three decimals,
# NaN still firing. The cap was never binding.
#
# Bisected through the model's public interface instead, which is the only
# measurement that can be wrong in the same way the model is:
#
#     zipf_s 0.0   binds at 2.0, not at 5.0     -> store norm is 2-5
#     zipf_s 2.0   binds at 10.0, not at 50.0   -> store norm is 10-50
#
# So the runaway probe was out by roughly 50x in absolute terms while its RATIO
# -- the store growing several-fold under repetition -- held. The ratio was the
# finding; the absolute numbers were never the model's.
#
# 5.0 therefore cannot bind at uniform and must bind under skew, which is what
# prediction 3 requires. 10.0 binds only at the skewed end. 0 is off.
CAPS = (0.0, 5.0, 10.0)

#: MQAR at n_pairs 4, n_values 8. A cell whose ungated arm is at or below this is
#: not measuring a difficulty, it is measuring two failures.
TRIVIAL_FLOOR = 1 / 4 + (1 - 1 / 4) / 8


def train_and_score(task, cap: float, arm: str, lr: float, seed: int,
                    train_set, test_set) -> float:
    gated, consolidation, salience, lasting, slots = ARMS[arm]
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=D_MODEL, lr=lr,
        key_scale=KEY_SCALE, decay=decay_for(SEQ_LEN, HALF_LIFE),
        memory_cap=cap, consolidation=consolidation, salience=salience,
        lasting_cap=lasting, capture_slots=slots, seed=seed))
    rng = np.random.default_rng(seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True,
                      store=keep if gated else None)

    right = total = 0
    for tokens, _, _, keep, queries in test_set:
        predicted = model.run(tokens, store=keep if gated else None)
        for q in queries:
            right += predicted[q] == tokens[q + 1]
            total += 1
    return right / total


def task_for(zipf_s: float):
    if zipf_s == 0.0:
        return replace(BASE, seq_len=SEQ_LEN, filler="random")
    return replace(BASE, seq_len=SEQ_LEN, filler="zipf", zipf_s=zipf_s)


def one_seed(work: tuple) -> list[dict]:
    """Everything for one (exponent, seed). Module-level for `spread`'s spawn."""
    zipf_s, seed, rates = work
    task = task_for(zipf_s)
    train_set = build(task, N_TRAIN, seed)
    test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)

    records = []
    for cap in CAPS:
        for lr in rates:
            for arm in ARMS:
                accuracy = train_and_score(task, cap, arm, lr, seed,
                                           train_set, test_set)
                print(f"  zipf={zipf_s:<4} cap={cap:<6} lr={lr:<5} "
                      f"arm={arm:<9} seed={seed}  {accuracy:.3f}", flush=True)
                records.append(dict(
                    condition=f"zipf={zipf_s} cap={cap} lr={lr} arm={arm}",
                    seed=seed, zipf_s=zipf_s, memory_cap=cap, lr=lr, arm=arm,
                    seq_len=SEQ_LEN, accuracy=accuracy))
    return records


def control() -> int:
    """Does each candidate cap bind where it should, and nowhere else?

    Cheap, and it is also the cheap version of the whole experiment: if a cap
    cannot lift the ungated arm above the trivial floor at zipf_s 1.0, then
    prediction 2 fails and the grid is not worth dispatching.
    """
    print(f"trivial floor {TRIVIAL_FLOOR:.5f}")
    print(f"{'zipf_s':>7}{'cap':>8}{'ungated':>10}{'usable?':>10}")
    for zipf_s in EXPONENTS:
        task = task_for(zipf_s)
        train_set = build(task, N_TRAIN, 1)
        test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, 1)
        for cap in CAPS:
            accuracy = train_and_score(task, cap, "none", 0.02, 1,
                                       train_set, test_set)
            usable = "yes" if accuracy > TRIVIAL_FLOOR else "NO"
            print(f"{zipf_s:>7}{cap:>8}{accuracy:>10.3f}{usable:>10}",
                  flush=True)
    return 0


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    if args.sweep == "degrade":
        return control()

    exponents = (args.scale,) if args.scale is not None else EXPONENTS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS
    work = [(zipf_s, seed, tuple(rates))
            for zipf_s in exponents for seed in seeds]
    records = [r for batch in spread(one_seed, work, args.workers) for r in batch]
    emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
