"""How much of the oracle's advantage is selectivity, and how much is retention?

[Note 019](../docs/notes/019-the-oracle-also-slows-forgetting.md): the fade lives
inside the `store[t]` guard, so a masked-out position is not merely un-written —
it is un-faded. On MQAR with 92% filler an oracle-gated arm skips the fade on 92%
of steps, running at an effective half-life roughly an order of magnitude longer
than the ungated arm at the same nominal `decay`.

So the oracle stores less **and** forgets more slowly, and every gating result
has described only the first. Six mechanisms have failed to match it, all aimed
at selectivity alone.

Three arms:

    none              no mask, fades every step
    oracle            masked, and skips the fade on masked steps    as measured
    oracle-decayed    masked, fades every step                      selectivity only

The gap between the two oracles is the part of the advantage that is retention —
and retention is a dial any node can turn, not a signal it has to derive.

    python experiments/g8_05_which_advantage.py
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g8_01_real_gate import (  # noqa: E402
    BASE, D_MODEL, EPOCHS, KEY_SCALE, LEARNING_RATES, N_TEST, N_TRAIN,
    build, decay_for)
from experiments.harness import emit, parse_args, spread  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

SEQ_LENS = (192, 384, 768)
HALF_LIVES = (0.5, 0.25, 0.125)
SEEDS = (1, 2, 3)
#: MQAR at n_pairs 4, n_values 8. A cell whose ungated arm is at or below this is
#: measuring two failures rather than a difficulty.
TRIVIAL_FLOOR = 1 / 4 + (1 - 1 / 4) / 8

#: name -> (masked, fades on masked steps)
ARMS = {
    "none": (False, False),
    "oracle": (True, False),            # as every previous sweep measured it
    "oracle-decayed": (True, True),     # selectivity without the retention
}


def score(seq_len: int, half_life: float, lr: float, arm: str, seed: int,
          train_set, test_set) -> float:
    masked, decay_masked = ARMS[arm]
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=BASE.vocab_size, d_model=D_MODEL, lr=lr,
        key_scale=KEY_SCALE, decay=decay_for(seq_len, half_life),
        decay_when_masked=decay_masked, seed=seed))
    rng = np.random.default_rng(seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True,
                      store=keep if masked else None)

    right = total = 0
    for tokens, _, _, keep, queries in test_set:
        predicted = model.run(tokens, store=keep if masked else None)
        for q in queries:
            right += predicted[q] == tokens[q + 1]
            total += 1
    return right / total


def one_seed(work: tuple) -> list[dict]:
    seq_len, seed, rates = work
    task = replace(BASE, seq_len=seq_len)
    train_set = build(task, N_TRAIN, seed)
    test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)
    return [dict(condition=f"seq={seq_len} half={half} lr={lr} arm={arm}",
                 seed=seed, seq_len=seq_len, half_life=half, lr=lr, arm=arm,
                 accuracy=score(seq_len, half, lr, arm, seed,
                                train_set, test_set))
            for half in HALF_LIVES for lr in rates for arm in ARMS]


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS
    work = [(seq_len, seed, tuple(rates))
            for seq_len in seq_lens for seed in seeds]
    records = [r for batch in spread(one_seed, work, args.workers) for r in batch]

    if args.json:
        emit(records, Path(args.json))
        return 0

    print(f"trivial floor {TRIVIAL_FLOOR:.3f}")
    print("\nAccuracy, averaged over seeds, at each cell's best learning rate")
    print(f"{'seq':>6}{'half':>7}{'none':>9}{'oracle':>9}"
          f"{'oracle-dec':>12}{'retention':>11}")
    for seq_len in seq_lens:
        for half in HALF_LIVES:
            best = None
            for lr in rates:
                means = {}
                for arm in ARMS:
                    values = [r["accuracy"] for r in records
                              if r["seq_len"] == seq_len and r["half_life"] == half
                              and r["lr"] == lr and r["arm"] == arm]
                    means[arm] = sum(values) / len(values)
                if means["none"] <= TRIVIAL_FLOOR:
                    continue     # a broken floor is not a candidate
                gap = means["oracle"] - means["none"]
                if best is None or gap > best[0]:
                    best = (gap, means)
            if best is None:
                print(f"{seq_len:>6}{half:>7}   every cell has a broken floor")
                continue
            _, m = best
            # How much of the oracle's lead over `none` disappears when its
            # retention bonus is removed.
            lead = m["oracle"] - m["none"]
            retention = (m["oracle"] - m["oracle-decayed"]) / lead if lead else 0
            print(f"{seq_len:>6}{half:>7}{m['none']:>9.3f}{m['oracle']:>9.3f}"
                  f"{m['oracle-decayed']:>12.3f}{retention:>11.2f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
