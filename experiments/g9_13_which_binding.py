"""Can anything identify WHICH binding, without being told the delay?

[Note 026](../docs/notes/026-the-tags-precision-comes-from-its-fade.md) puts a
ceiling on this line. One binding in six is rewarded and nothing local separates
them at write time, so a PERFECT binding-detector tops out at **16.7% precision**
— a property of the task. The tag at its best capacity reaches 70% of that and a
matched window 76%. So binding-detection is close to exhausted, and **the
remaining room is entirely in identifying WHICH of the six.**

Only two things do that today: a window, by being told the delay, and the tag's
fade, by guessing a time constant. Nothing does it from the data.

[g9-04](sweeps/g9-04-is-there-a-local-signal.txt) already scored
`rewarded_vs_unrewarded` and treated any separation as a LEAK, correctly: it
scored signals recorded at the WRITE, and the generator picks rewarded cues
uniformly, so at write time the two classes are identical by construction.

**This asks the other half of the question, at the other moment.** Not "could the
write have known" but "at the capture step, with the reward in hand, can the node
tell which of its pending writes the reward is for".

## Why every quantity the trace already carried is useless for this

At a capture step, `surprise`, `strength`, `mean` and `deviation` are properties
of THE STEP. Every candidate shares them, so none of them can rank candidates.
Only two candidate-specific things existed: what was recorded when the write
happened, and how long ago that was. `pending_now` is the third and it is why
this probe needed a model change — a node holds `pending`, so it can ask its own
store what each pending key retrieves NOW, and that is a different number per
candidate. It is observation-only; nothing reads it back.

## The delay has to be unknown, or the question answers itself

`RewardConfig.delay` is FIXED, so the rewarded binding is always exactly `delay`
steps before its reward and **age identifies it perfectly**. That is not a
finding about a mechanism, it is the structure a window exploits, and scoring it
inside one delay would report AUC 1.0 and mean nothing.

So the sequences are generated across SEVERAL delays and the AUCs are pooled.
A candidate at age 8 is the rewarded one in a delay-8 sequence and is not in a
delay-20 sequence, which is exactly the position a node that does not know the
delay is in. Per-delay AUCs print beside the pooled ones, and `age` scoring near
1.0 per-delay and near chance pooled is the check that the pooling did what it
claims.

**This needs no change to the generator.** BACKLOG item 1 — randomising the delay
per rewarded pair — changes what the task IS and is John's call. Pooling changes
only what the SCORER is allowed to assume, which is the actual question.

## What the answer means either way

Something well above 0.5 pooled -> there is a delay-agnostic signal for WHICH
binding, and the ceiling on this line is not where note 026 put it. That would be
the most valuable result the gating line could still produce.

Nothing above 0.5 pooled -> `reward_recall`'s ceiling for any delay-agnostic
gate is about 20% of the oracle's advantage, which is approximately what the tag
already scores. The tag would then be at the task's ceiling rather than at a
mechanism's, and **the line closes with a result about the task**. That is a
worse headline and an equally real finding.

The labels come from `position_kinds()`, which is an **oracle**. It is used only
to score signals the model computed without it; nothing measured is fed back.
"""

from __future__ import annotations

import json
from dataclasses import replace

import numpy as np

from experiments import harness
from experiments.g9_04_is_there_a_local_signal import auc
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.reward_recall import RewardConfig, dataset

#: Candidate signals available AT THE CAPTURE STEP, per candidate write.
#:
#: `age` and `write_order` are deliberate ringers -- both are recency, which is
#: what a window ranks on, so they say how much of the answer being told the
#: delay carries. Everything else is a property of the write or of the store.
SIGNALS = ("age", "write_order", "pending_now", "pending_now_rank",
           "strength", "surprise", "hit")
#: Pooled across these, so no single delay is the one the scorer assumes.
DELAYS = (1, 4, 8, 20)
SEEDS = (1, 2, 3, 4, 5, 6)
WIDTH = 64
N_SEQUENCES = 24


def candidates(sequence, width: int, seed: int) -> list[dict]:
    """One row per (capture step, pending BINDING write) pair.

    Bindings only. g9-04 asked binding-vs-filler and answered it; asking it
    again here would let a signal score well by re-finding that separation
    instead of the one this probe is about.
    """
    config = LocalMemoryConfig(
        vocab_size=sequence.config.vocab_size, d_model=width, lr=0.05,
        key_scale=0.5, decay=0.97, seed=seed,
        reward_token=sequence.config.reward_token,
        # Protect everything, so the run is behaviourally the UNGATED model and
        # the store a candidate is scored against is not one some earlier
        # capture already pruned. The capture step still fires, which is the
        # only thing this needs from the gate.
        reward_window=len(sequence.tokens))
    model = LocalAssociativeMemory(config)
    # A decoder, so surprise reads the memory rather than an untrained readout.
    # Same choice as g9-04, for the same reason.
    model.wo[:] = model.wv

    trace: list[dict] = []
    model.run(np.asarray(sequence.tokens), trace=trace)

    kinds = sequence.position_kinds()          # ORACLE. Scoring only.
    body = len(kinds) - len(sequence.query_positions) * 2

    at_write: dict[int, dict] = {}             # pending index -> what we know
    rows: list[dict] = []
    for entry in trace:
        if entry["write_index"] >= 0:
            at_write[entry["write_index"]] = {
                "t": entry["t"],
                "strength": entry["strength"],
                "surprise": entry["surprise"],
                "hit": 1.0 if entry["hit"] else 0.0,
            }
        if not entry["pending_now"]:
            continue
        now = entry["pending_now"]
        # Rank rather than magnitude, because the magnitudes drift with how
        # full the store is and a pooled AUC would then be reading the store's
        # size across sequences instead of the candidate's place within one.
        order = sorted(range(len(now)), key=lambda i: now[i])
        rank = {index: place / max(1, len(now) - 1)
                for place, index in enumerate(order)}
        for index, wrote in at_write.items():
            step = wrote["t"]
            if step >= body or kinds[step] != "value":
                continue
            rows.append({
                "age": float(entry["t"] - step),
                # How late among the candidates, normalised, so it is comparable
                # across captures holding different numbers of writes.
                "write_order": index / max(1, len(now) - 1),
                "pending_now": now[index] if index < len(now) else 0.0,
                "pending_now_rank": rank.get(index, 0.0),
                "strength": wrote["strength"],
                "surprise": wrote["surprise"],
                "hit": wrote["hit"],
                # THE LABEL. A binding write sits at the value position, so the
                # cue is the step before it, and `rewarded` marks a cue that
                # will be asked about.
                "rewarded": kinds[step - 1] == "rewarded",
            })
        at_write.clear()
    return rows


def run_one(args) -> list[dict]:
    delay, seed = args
    config = replace(RewardConfig(seed=seed), delay=delay)
    rows = [row for sequence in dataset(config, n_sequences=N_SEQUENCES)
            for row in candidates(sequence, WIDTH, seed)]
    return [{"delay": delay, "seed": seed, "width": WIDTH, **row}
            for row in rows]


def score(rows: list[dict], label: str) -> list[dict]:
    """One AUC per signal over whatever rows were handed in."""
    positive = [r for r in rows if r["rewarded"]]
    negative = [r for r in rows if not r["rewarded"]]
    return [{
        "scope": label,
        "signal": signal,
        "auc": auc([r[signal] for r in positive], [r[signal] for r in negative]),
        "n_rewarded": len(positive),
        "n_unrewarded": len(negative),
    } for signal in SIGNALS]


def main() -> int:
    args = harness.parse_args(__doc__.splitlines()[0])
    seeds = [args.seed] if args.seed is not None else list(SEEDS)
    delays = [int(args.scale)] if args.scale is not None else list(DELAYS)

    jobs = [(delay, seed) for delay in delays for seed in seeds]
    rows = [r for batch in harness.spread(run_one, jobs, args.workers)
            for r in batch]

    records = score(rows, "POOLED across delays")
    for delay in delays:
        records += score([r for r in rows if r["delay"] == delay],
                         f"delay {delay}")

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(records, indent=1))
        return 0

    print(f"\n{len(rows)} candidate writes, "
          f"{sum(1 for r in rows if r['rewarded'])} of them rewarded")
    print(f"\n{'scope':>22}{'signal':>20}{'AUC':>9}")
    for record in records:
        print(f"{record['scope']:>22}{record['signal']:>20}"
              f"{record['auc']:>9.3f}")
    print("\nage near 1.0 WITHIN a delay and near 0.5 POOLED -> the pooling did")
    print("   what it claims, and being told the delay is the whole of a window")
    print("anything else above 0.5 POOLED -> a delay-agnostic signal for WHICH")
    print("   binding. The most valuable result this line could still produce")
    print("nothing above 0.5 POOLED -> the tag is at the TASK's ceiling, not a")
    print("   mechanism's, and this line closes with a result about the task")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
