"""Does consolidation ever beat simply not forgetting?

Consolidate-on-use works -- it lifts later answers by 0.18 while barely touching
the first, which is its signature -- but it loses to not fading at all, 0.694
against 0.981 at seq_len 192. Fading costs more than consolidation recovers.

Fading exists to stop the memory saturating, and on a short problem there is
nothing to saturate. So the question is whether a length exists at which
not-fading drowns in its own notes.

    python experiments/g7_04_crossover.py --seqlen 768 --decay 0.95 --lr 0.05 --json out/x.json
"""

from __future__ import annotations

import sys
from collections import defaultdict
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import emit, parse_args  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

BASE = MqarConfig(n_pairs=4, seq_len=192, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260726,
                  queries_per_pair=3)
SEQ_LENS = (192, 384, 768, 1536)
# HALF-LIVES, as a fraction of the sequence -- not absolute decay rates.
#
# A pre-dispatch control found the original grid meaningless. It used absolute
# decays of 0.99 and 0.95 at every length, and 0.95 over 768 steps wipes the
# memory every twenty steps: the arm scored 0.191 against 0.526 for no decay,
# which measures the grid rather than the mechanism. A sweep whose whole point is
# varying length cannot hold a per-step rate fixed, because the same rate is a
# gentle fade at 192 and an erasure at 1536.
#
# `None` means no decay at all. The others set decay so the memory halves after
# that fraction of the sequence has passed.
HALF_LIVES = (None, 0.5, 0.25, 0.125)
# 0.1 included because a control found rate 1.0 actively harmful (0.482 against
# 0.625 with none) while 0.02 and 0.1 were indistinguishable from none. If
# consolidation pays anywhere it will be at a gentle rate in a harshly-forgetting
# arm, so the grid has to reach there.
CONSOLIDATIONS = (0.0, 0.1, 1.0)
LEARNING_RATES = (0.02, 0.05, 0.1, 0.2)
SEEDS = (1, 2, 3)
D_MODEL, N_TRAIN, N_TEST, EPOCHS, KEY_SCALE = 32, 400, 120, 8, 0.5


def decay_for(seq_len: int, half_life: float | None) -> float:
    """Per-step decay giving a half-life of `half_life * seq_len` steps.

    Solving `decay ** (half_life * seq_len) = 0.5`. Expressing forgetting in
    units of the sequence is what makes it comparable across lengths.
    """
    if half_life is None:
        return 1.0
    return float(0.5 ** (1.0 / (half_life * seq_len)))


def run(seq_len: int, decay: float, consolidation: float, lr: float,
        seed: int) -> dict:
    task = replace(BASE, seq_len=seq_len)
    rng = np.random.default_rng(seed)
    train_set = dataset(task, N_TRAIN)
    test_set = dataset(replace(task, seed=task.seed + 99_991), N_TEST)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=D_MODEL, lr=lr,
        key_scale=KEY_SCALE, decay=decay, consolidation=consolidation,
        seed=seed))
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens = np.asarray(train_set[index].tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)

    # Split by which ask it is: consolidation can only act on repeats, so a
    # uniform lift would mean something other than consolidation is happening.
    buckets = defaultdict(lambda: [0, 0])
    for sequence in test_set:
        tokens = np.asarray(sequence.tokens)
        predicted = model.run(tokens)
        seen = defaultdict(int)
        for q in sequence.query_positions:
            key = tokens[q]
            slot = buckets[seen[key]]
            slot[0] += predicted[q] == tokens[q + 1]
            slot[1] += 1
            seen[key] += 1
    by_ask = {k: v[0] / v[1] for k, v in sorted(buckets.items())}
    overall = sum(v[0] for v in buckets.values()) / sum(v[1] for v in buckets.values())

    print(f"  seq={seq_len:<5} decay={decay:<5} consol={consolidation:<4} "
          f"lr={lr:<5} seed={seed}  overall {overall:.3f}  "
          f"by ask {[round(by_ask.get(i, float('nan')), 3) for i in range(3)]}",
          flush=True)
    return dict(
        condition=f"seq={seq_len} decay={decay} consol={consolidation} lr={lr}",
        seed=seed, seq_len=seq_len, decay=decay, consolidation=consolidation,
        lr=lr, accuracy=overall, overall=overall,
        first_ask=by_ask.get(0, float("nan")),
        last_ask=by_ask.get(max(by_ask), float("nan")) if by_ask else float("nan"))


def main() -> int:
    args = parse_args(__doc__)
    seq_lens = (args.seqlen,) if args.seqlen else SEQ_LENS
    # `--decay` carries the HALF-LIFE fraction here; 0 means no decay.
    half_lives = ((None if args.decay == 0 else args.decay,)
                  if args.decay is not None else HALF_LIVES)
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed is not None else SEEDS

    records = []
    for seq_len in seq_lens:
        for half_life in half_lives:
            decay = decay_for(seq_len, half_life)
            for consolidation in CONSOLIDATIONS:
                if consolidation and decay >= 1.0:
                    continue        # refused by the config, and correctly
                for lr in rates:
                    for seed in seeds:
                        record = run(seq_len, decay, consolidation, lr, seed)
                        record["half_life"] = half_life
                        records.append(record)
    emit(records, args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
