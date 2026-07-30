"""Does a gate work when the relevance signal is real and arrives late?

Six mechanisms have failed to recover the oracle, and every one of them tried to
infer relevance from the statistics of the stream. [Note 016](../docs/archive/notes/016-who-supplies-relevance.md)
argues that is a harder question than biology solves: neuromodulatory signals are
not derived by cortex from its inputs, they arrive from elsewhere.

MQAR cannot test that — a sequence of random symbols contains nothing that is
good or bad for anything. `reward_recall` can: a reward token arrives **in the
stream**, **after** the binding it refers to, and only rewarded bindings are ever
queried.

And per [g8-03](sweeps/g8-03-a-pool-you-have-to-win.txt) the gate must act on the
**fast store**, which is the only thing the oracle does. `reward_token` and
`reward_window` do exactly that: everything is written, and when a reward arrives
everything outside its window is taken back out.

    the oracle   keeps 1 binding per rewarded pair, from position_kinds()
    the reward   keeps window+1 bindings per reward, from a token in the input

So at `delay` d with `window` d, the gate holds `(d + 1) * n_rewarded` bindings
against a sequence of 768 — a reduction of the same KIND as the oracle's, from a
signal a deployed node actually receives.

    python experiments/g9_02_reward_gate.py --sweep degrade      # control
    python experiments/g9_02_reward_gate.py --scale 4 --lr 0.05 --workers 3 --json out/x.json
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.harness import emit, parse_args, spread  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.reward_recall import RewardConfig, dataset  # noqa: E402

#: The configuration g9-01 verified: ungated sits AT the trivial floor on first
#: asks (0.119 against 0.125) and the oracle reaches 1.000. The first set tried
#: had a gap of 0.000 and would have made every arm read 1.000.
BASE = RewardConfig(n_pairs=24, n_rewarded=4, n_cues=64, n_values=8,
                    seq_len=768, delay=8, queries_per_reward=3, seed=20260726)
D_MODEL, N_TRAIN, N_TEST, EPOCHS, KEY_SCALE, DECAY = 32, 200, 80, 6, 0.5, 0.997
DELAYS = (1, 4, 8, 20)
#: How far back the gate can reach, FIXED rather than derived from the delay.
#: A node does not know how long ago the thing that mattered happened, and that
#: is the entire difficulty tagging and capture exists to address.
#:
#: 8 covers delays 1 and 4 comfortably, covers 8 exactly, and CANNOT reach 20 --
#: so the delay decides whether the reach is enough, which is the question.
REWARD_WINDOW = 8
LEARNING_RATES = (0.02, 0.05, 0.1)
SEEDS = (1, 2, 3)

#: name -> (uses the oracle, consolidation, salience, lasting cap, reward gate)
ARMS = {
    "none": (False, 0.0, 0.0, 0.0, False),
    "oracle": (True, 0.0, 0.0, 0.0, False),
    "on-use": (False, 0.1, 0.0, 0.0, False),
    "salience": (False, 0.1, 2.5, 0.2, False),
    "reward": (False, 0.0, 0.0, 0.0, True),
}


def build(task: RewardConfig, count: int, seed: int):
    """Sequences, their oracle masks, and where the first ask of each cue is."""
    built = []
    for sequence in dataset(replace(task, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        kinds = sequence.position_kinds()
        # The oracle: keep a binding only where the previous position was the
        # cue of a REWARDED pair. Reads what no running system can read.
        keep = np.array([i > 0 and kinds[i - 1] == "rewarded"
                         for i in range(len(tokens))])
        seen: set[int] = set()
        firsts = []
        for q in sequence.query_positions:
            cue = int(tokens[q])
            if cue not in seen:
                firsts.append(q)
                seen.add(cue)
        built.append((tokens, targets, scored, keep,
                      sequence.query_positions, tuple(firsts)))
    return built


def score(task: RewardConfig, arm: str, lr: float, seed: int,
          train_set, test_set, window: int = REWARD_WINDOW) -> tuple[float, float]:
    gated, consolidation, salience, lasting, reward = ARMS[arm]
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=D_MODEL, lr=lr,
        key_scale=KEY_SCALE, decay=DECAY,
        consolidation=consolidation, salience=salience, lasting_cap=lasting,
        # A FIXED reach, not task.delay. The first version used the delay,
        # which told the gate exactly how far back the binding was -- a property
        # of the generator no deployed node has, and the same class of error as
        # position_kinds() arriving through a parameter instead of a mask. It
        # made the delay axis measure nothing: the reach always matched, so the
        # curve was flat by construction.
        reward_token=task.reward_token if reward else -1,
        reward_window=window if reward else 0,
        seed=seed))
    rng = np.random.default_rng(seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for index in order:
            tokens, targets, scored, keep, _, _ = train_set[index]
            model.run(tokens, targets, scored, learn=True,
                      store=keep if gated else None)

    right = total = first_right = first_total = 0
    for tokens, _, _, keep, queries, firsts in test_set:
        predicted = model.run(tokens, store=keep if gated else None)
        for q in queries:
            hit = predicted[q] == tokens[q + 1]
            right += hit
            total += 1
            if q in firsts:
                first_right += hit
                first_total += 1
    return right / total, first_right / max(1, first_total)


def one_seed(work: tuple) -> list[dict]:
    delay, seed, rates, window = work
    task = replace(BASE, delay=delay)
    train_set = build(task, N_TRAIN, seed)
    test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)
    records = []
    for lr in rates:
        for arm in ARMS:
            overall, first = score(task, arm, lr, seed, train_set, test_set,
                                   window)
            records.append(dict(
                condition=f"delay={delay} window={window} lr={lr} arm={arm}",
                seed=seed, delay=delay, window=window, lr=lr, arm=arm,
                accuracy=first,        # first asks: retention, not echo
                accuracy_all=overall))
    return records


def control() -> int:
    """Cheap, and it is the cheap version of the whole experiment.

    Runs ONE delay, ONE learning rate, ONE seed. If the reward arm does not beat
    `none` there, it will not beat it across a grid, and the grid is not worth
    runner time. Deliberately small: a control that holds the machine for ten
    minutes is a sweep wearing a different name.
    """
    task = replace(BASE, delay=8)
    train_set = build(task, 60, 1)
    test_set = build(replace(task, seed=task.seed + 99_991), 30, 1)
    print(f"trivial floor {task.trivial_floor:.3f}   (delay 8, one seed, "
          f"reduced training -- shape only, not a result)")
    print(f"{'arm':>10}{'first asks':>12}{'all asks':>10}")
    for arm in ARMS:
        first, overall = score(task, arm, 0.05, 1, train_set, test_set)[::-1]
        print(f"{arm:>10}{first:>12.3f}{overall:>10.3f}", flush=True)
    return 0


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    if args.sweep == "degrade":
        return control()
    delays = (int(args.scale),) if args.scale is not None else DELAYS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS
    window = args.window if args.window is not None else REWARD_WINDOW
    work = [(delay, seed, tuple(rates), window)
            for delay in delays for seed in seeds]
    records = [r for batch in spread(one_seed, work, args.workers) for r in batch]
    emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
