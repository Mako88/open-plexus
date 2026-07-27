"""Does a capacity over bindings beat a span over steps?

[g9-03](sweeps/g9-03-is-the-cliff-reach-or-cost.txt) measured the reward gate's
reach as a diagonal cliff: about 0.2 recovered wherever the window covers the
delay, about -0.22 wherever it does not, and a node does not know the delay.
Widening does not fix it — a window of 64 recovers 0.09 at every delay, because
sixty-two of those steps are filler.

[g9-04](sweeps/g9-04-is-there-a-local-signal.txt) then found the signal a mark
could hang on: retrieval strength separates a binding-write from a filler-write
at AUC 0.293 and 0.215, **inverted**, so the rule is *admit the weak retrievals*.

This runs the mechanism those two imply. A **tag** holds a fixed number of marks
over WRITES rather than a span over steps, admits on weak retrieval, and **fades**
— an old mark loses its slot to a newer one, which is what note 010 took from
Lehr et al. and what the first build of this left out.

Two dials, and they are separate here in a way the window's were not:

    slots   how many writes survive a capture      -- capacity
    fade    how fast a mark ages out of the pool   -- reach

`tag-strongest` is the control. Same capacity, same fade, same everything, with
only the end of the ranking that wins reversed. If it scores the same, the
capacity is doing the work and g9-04's signal is decoration.

`--mode relative` ranks the tag on retrieval strength divided by the size of the
store that produced it, which removes the term that made an un-faded tag prefer
the first writes of every interval. See g9-07.

    python experiments/g9_05_the_tag.py --sweep degrade          # control
    python experiments/g9_05_the_tag.py --slots 8 --fade 0.99 --scale 8 --lr 0.05
    python experiments/g9_05_the_tag.py --mode relative --slots 8 --fade 0.99
    python experiments/g9_05_the_tag.py --width 4 --slots 32 --fade 0.95
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments.g9_02_reward_gate import (  # noqa: E402
    BASE, D_MODEL, DECAY, EPOCHS, KEY_SCALE, N_TEST, N_TRAIN, REWARD_WINDOW,
    build)
from experiments.harness import emit, parse_args, spread  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.reward_recall import RewardConfig  # noqa: E402

import numpy as np  # noqa: E402

DELAYS = (1, 4, 8, 20)
LEARNING_RATES = (0.02, 0.05, 0.1)
SEEDS = (1, 2, 3)
#: Defaults for the two tag dials when a job does not pin them.
SLOTS, FADE = 8, 0.99

#: name -> (uses the oracle, runs the window gate, runs the tag, tag reversed)
#:
#: `reward` is the incumbent at the SAME fixed reach g9-02 used, so this sweep's
#: cells are comparable with that one rather than only with each other. The full
#: window x delay table is g9-03's and is not re-run here.
ARMS = {
    "none": (False, False, False, False),
    "oracle": (True, False, False, False),
    "reward": (False, True, False, False),
    "tag": (False, False, True, False),
    "tag-strongest": (False, False, True, True),
    # Both mechanisms at once, protecting the union of what each keeps. Note 023:
    # weak retrieval says "this write is a binding", recency says "this binding
    # is the rewarded one".
    #
    # It CAN capture less than either alone -- keeping more leaves a larger
    # store, which returns stronger retrievals, which changes what the tag marks
    # in the NEXT interval. Measured at slots 8 fade 0.95 delay 20: 6 of 32
    # against the tag's 8. The union is a set operation within an interval and a
    # feedback loop across them.
    "combined": (False, True, True, False),
}


def score(task: RewardConfig, arm: str, lr: float, seed: int, train_set,
          test_set, slots: int, fade: float, relative: bool = False,
          width: int = D_MODEL) -> tuple[float, float]:
    gated, window, tagged, strongest = ARMS[arm]
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=width, lr=lr,
        key_scale=KEY_SCALE, decay=DECAY,
        # A FIXED reach on both gated arms, not one derived from task.delay.
        # A node does not know how long ago the thing that mattered happened,
        # and handing it the delay is position_kinds() arriving as a parameter.
        reward_token=task.reward_token if (window or tagged) else -1,
        reward_window=REWARD_WINDOW if window else 0,
        tag_slots=slots if tagged else 0,
        tag_decay=fade if tagged else 1.0,
        tag_strongest=strongest,
        tag_relative=relative and tagged,
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
    delay, seed, rates, slots, fade, relative, width = work
    task = replace(BASE, delay=delay)
    train_set = build(task, N_TRAIN, seed)
    test_set = build(replace(task, seed=task.seed + 99_991), N_TEST, seed)
    records = []
    for lr in rates:
        for arm in ARMS:
            overall, first = score(task, arm, lr, seed, train_set, test_set,
                                   slots, fade, relative, width)
            records.append(dict(
                condition=f"delay={delay} width={width} slots={slots} "
                          f"fade={fade} lr={lr} relative={relative} arm={arm}",
                seed=seed, delay=delay, slots=slots, fade=fade, lr=lr, arm=arm,
                relative=relative, width=width,
                accuracy=first,        # first asks: retention, not echo
                accuracy_all=overall))
    return records


def control(relative: bool = False) -> int:
    """One delay, one rate, one seed, reduced training. Shape, not a result.

    Deliberately small. A control that holds the machine for ten minutes is a
    sweep wearing a different name, which this project has already done once.
    """
    task = replace(BASE, delay=8)
    train_set = build(task, 60, 1)
    test_set = build(replace(task, seed=task.seed + 99_991), 30, 1)
    print(f"trivial floor {task.trivial_floor:.3f}   (delay 8, one seed, "
          f"reduced training -- shape only, not a result)")
    print(f"{'arm':>15}{'first asks':>12}{'all asks':>10}")
    for arm in ARMS:
        overall, first = score(task, arm, 0.05, 1, train_set, test_set,
                               SLOTS, FADE, relative, D_MODEL)
        print(f"{arm:>15}{first:>12.3f}{overall:>10.3f}", flush=True)
    return 0


def main() -> int:
    args = parse_args(__doc__.splitlines()[0])
    if args.sweep == "degrade":
        return control(args.mode == "relative")
    delays = (int(args.scale),) if args.scale is not None else DELAYS
    rates = (args.lr,) if args.lr else LEARNING_RATES
    seeds = (args.seed,) if args.seed else SEEDS
    slots = args.slots if args.slots is not None else SLOTS
    fade = args.fade if args.fade is not None else FADE
    # `--mode relative` ranks the tag on retrieval strength divided by the size
    # of the store that produced it. One variant of one arm, so it is a mode on
    # this script rather than a second script that would drift from it.
    relative = args.mode == "relative"
    if args.mode not in (None, "relative"):
        raise SystemExit(f"--mode must be 'relative' if given, got {args.mode}")
    # Width is the node's own dimension count. Every g9 cell so far ran at
    # D_MODEL in one process, so no gating result here has been about node SIZE
    # -- which is the question g7-02 and g7-03 answered for the ORACLE gate and
    # nobody has asked of an implementable one.
    width = args.width if args.width is not None else D_MODEL
    work = [(delay, seed, tuple(rates), slots, fade, relative, width)
            for delay in delays for seed in seeds]
    records = [r for batch in spread(one_seed, work, args.workers) for r in batch]
    emit(records, Path(args.json) if args.json else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
