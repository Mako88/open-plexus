"""Can the read gate tell "I have this fact" from "I must compose it"?

Decision 150 showed `inherit` is inert on MQAR, where every queried address was
written. Decision 148 showed it fires correctly on families, where transfer
entities have none. **Kinship is the case that decides what the mechanism
actually is**, because there the answer requires composing two stated facts and
the address is written anyway.

## Why this is a bound rather than a hope

`inherit` asks one question: **has anything been written at this address?** On
families that coincides with *do I know a fact about this entity*, because the
address is per-entity and a fact is what writes it.

Kinship's question ends `... QUERY target FACT subject`, so the scored position's
pair key is `(FACT, subject)` — **the key a fact wrote for that person as a
subject.** The asked subject is the start of the path, so it is the subject of at
least one stated fact **at every hop count**. The address is occupied whether the
answer is a single stated fact or a composition of three.

So the prediction is that the gate is blind here, and it is blind for a
structural reason worth writing down: **occupancy is a property of the ADDRESS,
not of the knowledge.** Those coincide only when addresses are per-fact.

## The arms

    plain      `index_branches=0`. The store as the kinship line measures it
    indexed    branches on, neighbours SUMMED
    inherit    branches on, gate on

`hops=1` and `hops=2` are both run. At one hop the answer IS a stated fact; at
two it is not. If the gate distinguished knowledge rather than occupancy, that is
where it would show.

## PREDICTIONS, registered before the arms were run

  K1  THE BOUND. The deferral rate is near 0.0 at BOTH hop counts, and the
      difference between them is under 0.05. The asked subject is a stated
      subject either way, so the address is occupied either way.

  K2  THE RAIL. `inherit` is within 0.02 of `plain` at both hop counts. A gate
      that never fires cannot change an answer — the same rail decision 150
      confirmed on MQAR, checked here because the key structure is different
      (`context_keys` pair keys rather than single ones).

  K3  THE CONTRAST. `indexed` falls below `plain`. Summing neighbours proposed
      by an index fitted where there is no similarity structure to find should
      cost accuracy, as it cost 0.113 on MQAR.

  K4  THE FALSIFIER, and the interesting outcome. **If the deferral rate differs
      between hop counts by more than 0.05**, the gate is picking up something
      about composition that this reasoning says it cannot, and the reasoning is
      wrong in a way worth chasing rather than a result worth recording.

**What this settles either way:** whether "the read gate knows what it knows" is
a fair description of decision 148, or whether the honest description is the
narrower "the read gate knows which addresses it has written" — which is the same
thing only on tasks that address by fact.

**SCORED — DECISION 151. K1, K2 and K3 all hold; K4 did not fire.** Three seeds:

    hops 1   plain 0.7767   indexed 0.7067   inherit 0.7767   deferred 0.0000
    hops 2   plain 0.4433   indexed 0.4067   inherit 0.4433   deferred 0.0000

K1 CONFIRMED, and at the limit: the deferral rate is 0.0000 at BOTH hop counts,
a difference of exactly 0.0000 against a predicted 0.05. K2 CONFIRMED — `inherit`
matches `plain` to four decimals at both. K3 CONFIRMED — `indexed` loses 0.070
and 0.037. So the honest description of decision 148 is **"the gate knows which
addresses it has written"**, which is "knows what it knows" only where addresses
are per-fact.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from dataclasses import replace
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import harness  # noqa: E402
from openplexus.content import ContentIndex  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.kinship import (  # noqa: E402
    IGNORE, KinshipConfig, dataset)

TASK = KinshipConfig(n_people=12, n_facts=10, seq_len=96, hops=2, seed=0)
WIDTH = 64
KEY_SCALE = 0.5
DECAY = 0.99
EPOCHS = 4
TRAIN = 400
TEST = 100
SEEDS = (0, 1, 2)
#: Decision 148's settings, so the gate under test is the one that was measured.
BRANCHES = 3
CONTENT_WIDTH = 64
INDEX_WINDOW = 1
INDEX_POWER = 0.0

ARMS = ("plain", "indexed", "inherit")
HOPS = (1, 2)


def build(task: KinshipConfig, count: int, seed: int) -> list:
    built = []
    for sequence in dataset(replace(task, seed=seed), count):
        tokens = np.asarray(sequence.tokens)
        targets = np.asarray(sequence.targets)
        built.append((tokens, targets, targets != IGNORE,
                      sequence.answer_position))
    return built


def one_cell(arm: str, hops: int, seed: int) -> dict:
    started = time.time()
    task = replace(TASK, hops=hops)
    train_set = build(task, TRAIN, seed + 100)
    test_set = build(task, TEST, seed + 900)

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=task.vocab_size, d_model=WIDTH, lr=0.05,
        key_scale=KEY_SCALE, decay=DECAY, seed=seed,
        # THE KINSHIP LINE'S KEYS, not the families line's. The scored position
        # is keyed on `(FACT, subject)`, which only exists with pair keys --
        # decision 100 measured the single-key version of this question at 0.020
        # against 0.713, so getting it wrong is not a small error.
        derived_keys=True, context_keys=True,
        index_branches=BRANCHES if arm != "plain" else 0,
        index_prefer="inherit" if arm == "inherit" else False))

    if arm != "plain":
        index = ContentIndex(task.vocab_size, width=CONTENT_WIDTH, seed=seed,
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
    for tokens, targets, _, answer_at in test_set:
        model.deferrals.clear()
        predictions = model.run(tokens)
        chose = dict(model.deferrals)
        right += int(predictions[answer_at] == targets[answer_at])
        total += 1
        if answer_at in chose:
            deferred += int(chose[answer_at])
            gated += 1

    return dict(
        arm=arm, hops=hops, seed=seed,
        accuracy=round(right / max(total, 1), 4),
        deferred=round(deferred / gated, 4) if gated else None,
        gated=int(gated),
        scored=int(total),
        width=WIDTH, epochs=EPOCHS, train=TRAIN, branches=BRANCHES,
        vocab=task.vocab_size, n_people=task.n_people, n_facts=task.n_facts,
        seconds=round(time.time() - started, 1),
        condition=f"{arm}|hops{hops}|d{WIDTH}|seed{seed}"
                  f"|people{task.n_people}|facts{task.n_facts}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--arm", choices=ARMS, default=None)
    parser.add_argument("--hops", type=int, default=None)
    parser.add_argument("--json", type=str, default=None)
    args = parser.parse_args()

    harness.refuse_if_mutating()
    seeds = (args.seed,) if args.seed is not None else SEEDS
    arms = (args.arm,) if args.arm else ARMS
    hop_counts = (args.hops,) if args.hops else HOPS

    records = []
    for seed in seeds:
        for hops in hop_counts:
            for arm in arms:
                record = one_cell(arm, hops, seed)
                deferral = ("     -" if record["deferred"] is None
                            else f"{record['deferred']:.4f}")
                print(f"  {record['condition']:46s} "
                      f"accuracy {record['accuracy']:.4f}  "
                      f"deferred {deferral}", file=sys.stderr, flush=True)
                records.append(record)

    if args.json:
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")
        print(f"wrote {len(records)} records to {args.json}")

    print()
    for hops in hop_counts:
        for arm in arms:
            cells = [r for r in records
                     if r["arm"] == arm and r["hops"] == hops]
            if not cells:
                continue
            accuracy = sum(r["accuracy"] for r in cells) / len(cells)
            gated = [r["deferred"] for r in cells if r["deferred"] is not None]
            deferral = (f"{sum(gated) / len(gated):.4f}" if gated else "     -")
            print(f"hops {hops}  {arm:10s} accuracy {accuracy:.4f}  "
                  f"deferred {deferral}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
