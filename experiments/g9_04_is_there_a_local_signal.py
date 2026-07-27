"""Before building the tag: is there anything local to hang it on?

[g9-03](sweeps/g9-03-is-the-cliff-reach-or-cost.txt) settled that a window has to
be MATCHED to a lag nobody knows in advance, which is what makes a tag a
mechanism rather than a saving. The obvious next move is to build it.

**Reading the generator first says one of the two things a tag could do is
impossible here, by construction.** `reward_recall` picks which cues get rewarded
with `rng.sample(cues, n_rewarded)` — uniformly, from cues drawn out of the same
alphabet as the filler. So a rewarded binding and an unrewarded one are
*statistically identical* until the reward token arrives `delay` steps later.
Nothing local can predict reward, and a tag that claimed to would be reading
`position_kinds()` in disguise.

That leaves the other thing a tag can do, and it is the one biology actually
describes: **set a cheap marker on everything worth marking, let it decay, and
let the late signal capture whatever is still tagged.** The tag is not selective
about value. It is selective about *being a binding at all*.

Which matters here more than it sounds, because of a number nobody has printed:

    body 744 steps / 24 bindings  ~=  31 steps per binding

A window of 64 steps holds about **two bindings and sixty-two steps of filler**.
That is the whole of g9-03's fall-off, and it says a span-based reach is doomed
at this density however it is tuned. A capacity of four *bindings* spans about
124 steps; a capacity of four *steps* spans four.

So the question this measures, and the only one a tag depends on:

**Can a node tell a binding-write from a filler-write, from its own signals?**

If yes, a fixed capacity over admitted items reaches an arbitrary delay and the
tag is worth building. If no, admission is a random subset and a small tag is
WORSE than a small window — and that is a result about this whole line of work,
arriving before the mechanism instead of after.

The labels come from `position_kinds()`, which is an **oracle**. It is used here
only to score signals that the model computed without it; nothing measured is fed
back. That distinction is the entire difference between this and cheating, so it
is worth saying twice.
"""

from __future__ import annotations

import json
from dataclasses import replace

import numpy as np

from experiments import harness
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.reward_recall import RewardConfig, dataset

#: The signals a gate could consult. `position` is a deliberate ringer -- it is
#: what a window uses, so it says how much of the answer recency alone carries.
SIGNALS = ("surprise", "strength", "deviation_from_mean", "hit", "position")
#: Six seeds and 32 sequences, because the quantity is an AUC over a few hundred
#: labelled steps and the pilot's shakiest number came from 32 positives. This
#: probe is cheap enough that being under-powered would be a choice.
SEEDS = (1, 2, 3, 4, 5, 6)
WIDTHS = (32, 64)
N_SEQUENCES = 32


def auc(positive: list[float], negative: list[float]) -> float:
    """P(a random positive outscores a random negative), ties counted as half.

    Rank-based rather than by threshold sweep, because a threshold would need
    choosing and the choice would be the result. 0.5 is no separation in either
    direction; below 0.5 means the signal separates the classes the other way
    round, which is still information and is why this is not folded to
    `max(a, 1 - a)`.
    """
    if not positive or not negative:
        return float("nan")
    values = sorted(positive + negative)
    ranks: dict[float, float] = {}
    i = 0
    while i < len(values):
        j = i
        while j < len(values) and values[j] == values[i]:
            j += 1
        for k in range(i, j):
            ranks[values[k]] = (i + j - 1) / 2 + 1
        i = j
    total = sum(ranks[v] for v in positive)
    n_pos, n_neg = len(positive), len(negative)
    return (total - n_pos * (n_pos + 1) / 2) / (n_pos * n_neg)


def collect(sequences, width: int, seed: int) -> dict[str, list]:
    """Every traced step, labelled by what the generator says it was."""
    buckets: dict[str, list] = {
        "binding": [], "filler": [], "rewarded": [], "unrewarded": []}
    density: list[float] = []

    for sequence in sequences:
        config = LocalMemoryConfig(
            vocab_size=sequence.config.vocab_size, d_model=width, lr=0.05,
            key_scale=0.5, decay=0.97, seed=seed)
        model = LocalAssociativeMemory(config)
        # A decoder, so predictions track the memory rather than an untrained
        # readout. Measuring surprise through random weights would describe the
        # initialisation, which is the mistake that made vote quality meaningless.
        model.wo[:] = model.wv

        trace: list[dict] = []
        model.run(np.asarray(sequence.tokens), trace=trace)

        kinds = sequence.position_kinds()          # ORACLE. Scoring only.
        body = len(kinds) - len(sequence.query_positions) * 2
        bindings = sum(1 for k in kinds[:body] if k == "value")
        if bindings:
            density.append(body / bindings)

        for entry in trace:
            t = entry["t"]
            if t >= body or kinds[t] != "value" and kinds[t] != "filler":
                continue
            row = {
                "surprise": entry["surprise"],
                "strength": entry["strength"],
                "deviation_from_mean": abs(entry["surprise"] - entry["mean"]),
                # PREDICT THE FUTURE AND COMPARE, in its literal form: did the
                # guess made one step ago name the token that arrived. This is
                # what `consolidate-on-use` fires on, and it is scored here as a
                # signal in its own right rather than assumed to be surprise
                # under another name -- one is a binary hit on the argmax, the
                # other is continuous over the whole prediction.
                "hit": 1.0 if entry["hit"] else 0.0,
                # Recency as a signal: how close this step is to the end. A
                # window ranks on exactly this and nothing else.
                "position": float(t),
            }
            if kinds[t] == "filler" and kinds[t - 1] == "filler":
                buckets["filler"].append(row)
            elif kinds[t] == "value":
                buckets["binding"].append(row)
                side = "rewarded" if kinds[t - 1] == "rewarded" else "unrewarded"
                buckets[side].append(row)

    buckets["density"] = density
    return buckets


def run_one(args) -> list[dict]:
    width, seed = args
    config = RewardConfig(seed=seed)
    sequences = dataset(config, n_sequences=N_SEQUENCES)
    buckets = collect(sequences, width, seed)

    density = buckets.pop("density")
    records = []
    for signal in SIGNALS:
        pick = lambda name: [row[signal] for row in buckets[name]]
        records.append({
            "width": width,
            "seed": seed,
            "signal": signal,
            # The question the tag depends on.
            "binding_vs_filler": auc(pick("binding"), pick("filler")),
            # The question the task makes impossible on purpose. Anything far
            # from 0.5 here is a LEAK, not a finding.
            "rewarded_vs_unrewarded": auc(pick("rewarded"), pick("unrewarded")),
            "n_binding": len(buckets["binding"]),
            "n_filler": len(buckets["filler"]),
            "steps_per_binding": sum(density) / len(density) if density else 0.0,
        })
    return records


def main() -> int:
    args = harness.parse_args(__doc__.splitlines()[0])
    widths = [args.width] if args.width else list(WIDTHS)
    seeds = [args.seed] if args.seed is not None else list(SEEDS)

    jobs = [(width, seed) for width in widths for seed in seeds]
    records = [r for batch in harness.spread(run_one, jobs, args.workers)
               for r in batch]

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(records, indent=1))
        return 0

    print(f"\nsteps per binding: {records[0]['steps_per_binding']:.1f}")
    print(f"\n{'width':>6}{'seed':>5}  {'signal':>20}"
          f"{'binding vs filler':>19}{'rewarded vs not':>17}")
    for r in records:
        print(f"{r['width']:>6}{r['seed']:>5}  {r['signal']:>20}"
              f"{r['binding_vs_filler']:>19.3f}"
              f"{r['rewarded_vs_unrewarded']:>17.3f}")
    print("\nbinding vs filler well above 0.5 -> a fixed capacity over ADMITTED")
    print("                                    items reaches any delay. Build it.")
    print("near 0.5 for every signal        -> admission is a random subset and a")
    print("                                    small tag is worse than a small")
    print("                                    window. The line of work is done.")
    print("rewarded vs not away from 0.5    -> a LEAK. The generator chooses")
    print("                                    rewarded cues uniformly, so any")
    print("                                    separation is a bug in this probe.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
