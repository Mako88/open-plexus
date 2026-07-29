"""Does the read gate cost anything on a task with no families in it?

Decision 148's `inherit` rule is measured entirely on the families task, which
was built to ask the question it answers. **Every number in 148 and 149 is one
task**, and the first thing to know about a new mechanism is not how well it does
where it was designed to win — it is what it costs where it should do nothing.

MQAR is the right place to ask. Every queried key **was written**, in this very
sequence, a few tokens earlier. So:

    the gate should never fire
    accuracy should be exactly the plain arm's

## Why this is a cross-check on the families numbers and not just a rail

`inherit` treats a sketch count of 0.0 as *"nothing was ever written here"*. If
`AddressSketch` can produce a **false negative** — an address that was written
reading as empty — then on the families task an entity with its own stated fact
would silently inherit its family's answer instead, and the EXCEPTION column
would be quietly wrong in a way no amount of re-running it would reveal.

**MQAR makes that failure loud.** Here the correct deferral rate is 0.0000 by
construction rather than by argument, so any deferral at all is a false negative
and is visible immediately.

## The arms

    plain      `index_branches=0`. The store as decisions 138+ measure it
    indexed    branches on, neighbours SUMMED. Decision 146's arm
    inherit    branches on, gate on. The mechanism under test

`indexed` is here because it is the honest comparison: `inherit` and `indexed`
pay the same extra reads, so a difference between them is the RULE, while a
difference from `plain` is the reads.

**The content index is fitted on MQAR's own sequences**, where there is no family
structure to find — so whatever it proposes is arbitrary by construction. That is
the point. A gate that never consults an arbitrary neighbour is a gate that is
doing what it says.

## PREDICTIONS, registered before the arms were run

  M1  THE RAIL. `inherit` is within 0.01 of `plain`. The gate never fires, so
      nothing it could do can change the answer.

  M2  THE GATE. The deferral rate at query positions is exactly 0.0000. Not
      "low" — every queried key was written a few tokens earlier, so a single
      deferral is a sketch miss.

  M3  THE FALSIFIER, and it reaches backwards. If M2 fails, `AddressSketch`
      produces false negatives, and decisions 148 and 149 are measuring a gate
      that sometimes throws away a fact the model has. Those numbers would need
      re-reading rather than re-running.

  M4  THE CONTRAST. `indexed` falls BELOW `plain`, because summing arbitrary
      neighbours into a read that was already correct can only add noise here.
      This is what makes M1 a result rather than a tautology: it shows the extra
      reads do have a cost and the gate is what avoids it.

**SCORED — DECISION 150. All four hold.** Three seeds:

    plain      accuracy 0.9950   deferred      -
    indexed    accuracy 0.8817   deferred      -
    inherit    accuracy 0.9950   deferred 0.0000

M1 CONFIRMED, and not approximately: `inherit` matches `plain` seed for seed
(0.9950/0.9950, 0.9975/0.9975, 0.9925/0.9925). M2 CONFIRMED at exactly 0.0000 --
no queried key ever read as unwritten. M3 DID NOT FIRE, so the sketch produces no
false negatives and decisions 148/149 are not measuring a gate that discards
facts the model has. M4 CONFIRMED at **0.113 below plain**: summing arbitrary
neighbours costs real accuracy here, which is what makes M1 a result rather than
a tautology.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import harness  # noqa: E402
from openplexus.content import ContentIndex  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig  # noqa: E402

#: g10-01's configuration, unchanged, so an accuracy here is comparable to the
#: MQAR line rather than being a fresh baseline. `autoregressive=True` and
#: `filler="random"` are load-bearing -- see `harness.mqar_batch`, where getting
#: this wrong put both arms of g18-05 below chance.
TASK = MqarConfig(n_pairs=4, seq_len=64, n_keys=16, n_values=8,
                  autoregressive=True, filler="random", seed=11)
WIDTH = 64
KEY_SCALE = 0.5
#: 0.99, from g10-01. Note 046's mistake was inheriting 1.0 from the word-level
#: work, and it has cost this project four separate nights.
DECAY = 0.99
EPOCHS = 4
TRAIN = 400
TEST = 100
SEEDS = (0, 1, 2)
#: Decision 148's settings, so the gate under test is the gate that was measured
#: rather than a re-tuned one.
BRANCHES = 3
CONTENT_WIDTH = 64
INDEX_WINDOW = 1
INDEX_POWER = 0.0
TRIVIAL = 1.0 / TASK.n_values

ARMS = ("plain", "indexed", "inherit")


def build(count: int, seed: int) -> list:
    return harness.mqar_batch(TASK, count, seed)


def one_cell(arm: str, seed: int) -> dict:
    started = time.time()
    train_set = build(TRAIN, seed + 100)
    test_set = build(TEST, seed + 900)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=WIDTH, lr=0.05,
        key_scale=KEY_SCALE, decay=DECAY, seed=seed,
        index_branches=BRANCHES if arm != "plain" else 0,
        index_prefer="inherit" if arm == "inherit" else False))

    if arm != "plain":
        # FITTED ON MQAR'S OWN SEQUENCES, where there is no family structure to
        # find. Whatever it proposes is arbitrary, which is exactly the
        # condition this experiment wants: a gate that never consults an
        # arbitrary neighbour is a gate doing what it claims.
        index = ContentIndex(TASK.vocab_size, width=CONTENT_WIDTH, seed=seed,
                             power=INDEX_POWER, window=INDEX_WINDOW)
        for tokens, _, _, _ in train_set:
            index.observe(tokens)
        model.content = index

    rng = np.random.default_rng(seed)
    order = np.arange(len(train_set))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for position in order:
            tokens, targets, scored, _ = train_set[int(position)]
            model.run(tokens, targets, scored, learn=True)

    right = total = 0
    deferred = gated = 0
    for tokens, targets, _, queries in test_set:
        model.deferrals.clear()
        predictions = model.run(tokens)
        chose = dict(model.deferrals)
        for position in queries:
            right += int(predictions[position] == targets[position])
            total += 1
            if position in chose:
                deferred += int(chose[position])
                gated += 1

    return dict(
        arm=arm, seed=seed,
        accuracy=round(right / max(total, 1), 4),
        # None rather than 0.0 for the arms that have no gate, so "never
        # deferred" and "could not defer" are not the same number.
        deferred=round(deferred / gated, 4) if gated else None,
        gated=int(gated),
        trivial=round(TRIVIAL, 4),
        scored=int(total),
        width=WIDTH, epochs=EPOCHS, train=TRAIN, branches=BRANCHES,
        vocab=TASK.vocab_size, n_pairs=TASK.n_pairs, seq_len=TASK.seq_len,
        seconds=round(time.time() - started, 1),
        condition=f"{arm}|d{WIDTH}|seed{seed}"
                  f"|pairs{TASK.n_pairs}|len{TASK.seq_len}|epochs{EPOCHS}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=ARMS, default=None)
    parser.add_argument("--json", type=str, default=None)
    args = parser.parse_args()

    harness.refuse_if_mutating()
    seeds = (args.seed,) if args.seed is not None else SEEDS
    arms = (args.arm,) if args.arm else ARMS

    records = []
    for seed in seeds:
        for arm in arms:
            record = one_cell(arm, seed)
            deferral = ("     -" if record["deferred"] is None
                        else f"{record['deferred']:.4f}")
            print(f"  {record['condition']:44s} "
                  f"accuracy {record['accuracy']:.4f}  deferred {deferral}",
                  file=sys.stderr, flush=True)
            records.append(record)

    if args.json:
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")
        print(f"wrote {len(records)} records to {args.json}")

    print()
    for arm in arms:
        cells = [r for r in records if r["arm"] == arm]
        accuracy = sum(r["accuracy"] for r in cells) / len(cells)
        gated = [r["deferred"] for r in cells if r["deferred"] is not None]
        deferral = (f"{sum(gated) / len(gated):.4f}" if gated else "     -")
        print(f"{arm:10s} accuracy {accuracy:.4f}  deferred {deferral}")
    print(f"trivial {TRIVIAL:.4f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
