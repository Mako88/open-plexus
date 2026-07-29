"""What does putting the relation in the address cost?

[Note 051](../docs/notes/051-typed-edges-a-ground-up-pass.md) proposes storing
`key(subject, relation) -> object` so that two kinds of edge about one subject
stop colliding — decision 155's failure, and rows D2/D3/E4 of
[ARCHITECTURE.md](../ARCHITECTURE.md). A3 is the prediction that decides whether
any of it is affordable, and it is run **before** the mechanism, because if
typing costs what a naive reading of note 035 suggests then nothing else matters.

## The two things a careless version of this would conflate

Note 035 measured interference as `O(N * rho)` — **`N` writes** at mean key
cosine `rho`. Typing multiplies the **address space** by the number of relation
types. Those are not the same quantity, and the naive `1/r` fear quietly assumes
they are.

    AXIS 1  the same facts, spread over more relation types.
            N is UNCHANGED. Only the space grows.
    AXIS 2  the same total facts arranged as FEW subjects under MANY relations
            rather than many subjects under one. This is the case that
            previously COLLIDED, so the untyped store was not paying less for
            it -- it was losing it.

            **The docstring first said "N grows with r" and the code does not
            do that**: `subjects = load // relations` holds the total at
            `load`. Corrected to describe what is measured rather than what was
            intended, which makes A3b untested as written -- see the scoring
            below.

**Axis 1 is the real A3.** If it costs nothing, typing is free and the fear was
misplaced. Axis 2 is the honest accounting of what typing buys and what it then
costs, measured rather than assumed.

## The setup

Sequences of `RELATION subject value` triples with `context_keys` on, so the key
at the subject position is the pair `(relation, subject)` — a typed address,
formed from two token ids the node already holds, so C1 is untouched. `r = 1` is
the untyped baseline: one relation token, so the address is the subject and
nothing else.

Recall is then asked as `RELATION subject ?` at the end of the sequence, which is
the same shape `harness.mqar_batch` uses and the same convention decision 138
corrected.

## PREDICTIONS, registered before the sweep was run

  A3a  AXIS 1 IS FLAT. Holding the number of facts fixed, accuracy at `r = 8`
       stays within 0.05 of `r = 1`. Capacity is spent by WRITES, not by the
       size of the address space, so enlarging the space should cost nothing.
       **This is the prediction note 051's A3 should have been** — the `1/r`
       version conflated the two axes.

  A3b  AXIS 2 DEGRADES LIKE ANY OTHER LOAD. With `r` facts per subject, accuracy
       falls with the TOTAL number of facts along the same curve `r = 1` follows
       at that same total. Typing adds no interference of its own; it just lets
       more be stored.

  A3c  THE FALSIFIER, and it is A3a failing. If spreading the same facts over
       more relation types costs accuracy, then something in the key
       construction makes typed addresses interfere more than untyped ones —
       `PairKeys` hashing pairs into a smaller effective space than it appears
       to, most likely — and typed edges are dearer than note 051 claims.

  A3d  THE RAIL. `r = 1` reproduces the untyped store's behaviour rather than
       merely resembling it: it must clear 0.90 at the smallest load, or the
       harness is measuring something other than recall and no column means
       anything.

**SCORED — DECISION 156. Three seeds.**

    axis 1 -- same facts, spread over more relation types (N fixed)
      load     r=1      r=2      r=4      r=8
        16   0.8333   0.8333   0.9375   0.9792
        32   0.6562   0.6771   0.7083   0.7604
        64   0.4896   0.4844   0.4791   0.4844
        96   0.3264   0.3507   0.3542   0.3507

    axis 2 -- few subjects under many relations, same total
        16   0.8333   0.8958   0.9375   0.9375
        32   0.6562   0.6354   0.7500   0.7083
        64   0.4896   0.4740   0.5105   0.4844
        96   0.3264   0.3368   0.2951   0.3750

**A3a CONFIRMED, and in the opposite direction to the fear.** Typing never costs
and at low load it PAYS: +0.146 at load 16 and +0.104 at load 32, going from one
relation type to eight. Note 035's own formula explains it — interference is
`O(N * rho)`, and spreading keys over more distinct pair-hashes lowers `rho`. At
loads 64 and 96 the effect washes out because capacity is saturated and every
column degrades together. **A3c did not fire.**

**A3b IS UNTESTED**, because the code holds the total at `load` rather than
growing it with `r`. What axis 2 does measure is worth having and is flat:
re-using one subject across many relation types costs nothing against spreading
the same facts over many subjects — which is the D2 collision case, and it says
the collision was never a capacity problem.

**A3d MISSED: 0.8333 at `r = 1`, load 16, against a predicted 0.90.** The rail
was a guess rather than a reproduction, and that is the defect. **This harness
has never reproduced a known number**, so its ABSOLUTE values should not be
quoted anywhere. Everything A3a rests on is a comparison ACROSS `r` within the
same harness, seed and load, which is internally controlled and unaffected.
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
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

WIDTH = 64
KEY_SCALE = 0.5
#: 1.0, NOT 0.99. This is a capacity probe: every fact must still be held when
#: the query arrives, so a decay that fades early writes would measure the decay
#: rather than the capacity. Note 046's mistake was inheriting a decay across
#: task lines, and this is the case where the right value is the unusual one.
DECAY = 1.0
SEEDS = (0, 1, 2)
RELATIONS = (1, 2, 4, 8)
#: Total facts held at once, on axis 1. Chosen to straddle the wall at d = 64
#: rather than to sit comfortably below it -- a capacity probe that never fails
#: measures nothing.
LOADS = (16, 32, 64, 96)
SUBJECTS = 128


def build(facts: list[tuple[int, int, int]], rng) -> tuple:
    """`RELATION subject value` triples, then one query per fact.

    Queries come after every fact so the store holds the whole load when the
    first is asked, which is what makes this a capacity measurement rather than
    a recency one.
    """
    tokens: list[int] = []
    for relation, subject, value in facts:
        tokens.extend((relation, subject, value))
    asked = list(facts)
    rng.shuffle(asked)
    positions, answers = [], []
    for relation, subject, value in asked:
        tokens.extend((relation, subject))
        positions.append(len(tokens) - 1)
        answers.append(value)
        tokens.append(value)
    return np.asarray(tokens), positions, answers


def one_cell(relations: int, load: int, seed: int, per_subject: bool) -> dict:
    started = time.time()
    rng = np.random.default_rng(seed)
    # Token layout: [0, relations) are relation markers, then subjects, then
    # values. Vocabulary grows with `relations` and that is the honest cost --
    # a relation type is a token like any other.
    subject_base = relations
    value_base = subject_base + SUBJECTS
    n_values = 32
    vocab = value_base + n_values

    if per_subject:
        # AXIS 2: every subject under every relation. N = subjects * relations.
        subjects = rng.choice(SUBJECTS, size=max(load // relations, 1),
                              replace=False)
        facts = [(r, subject_base + int(s), value_base + int(rng.integers(n_values)))
                 for s in subjects for r in range(relations)]
    else:
        # AXIS 1: `load` facts however many relations there are. N is fixed.
        subjects = rng.choice(SUBJECTS, size=load, replace=False)
        facts = [(int(rng.integers(relations)), subject_base + int(s),
                  value_base + int(rng.integers(n_values)))
                 for s in subjects]

    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=vocab, d_model=WIDTH, lr=0.05, key_scale=KEY_SCALE,
        decay=DECAY, seed=seed,
        # THE TYPED ADDRESS. The key at the subject position is the pair
        # (relation, subject), formed from two ids the node already holds.
        context_keys=True, derived_keys=True))
    model.wo[:] = model.wv

    tokens, positions, answers = build(facts, rng)
    predictions = model.run(tokens)
    right = sum(int(predictions[at] == answer)
                for at, answer in zip(positions, answers))

    return dict(
        relations=relations, load=load, seed=seed, per_subject=per_subject,
        facts=len(facts),
        accuracy=round(right / max(len(answers), 1), 4),
        trivial=round(1.0 / n_values, 4),
        width=WIDTH, vocab=vocab,
        seconds=round(time.time() - started, 1),
        condition=f"{'axis2' if per_subject else 'axis1'}"
                  f"|r{relations}|load{load}|d{WIDTH}|seed{seed}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--axis", type=int, choices=(1, 2), default=None)
    parser.add_argument("--json", type=str, default=None)
    args = parser.parse_args()

    harness.refuse_if_mutating()
    seeds = (args.seed,) if args.seed is not None else SEEDS
    axes = ((args.axis,) if args.axis else (1, 2))

    records = []
    for axis in axes:
        for load in LOADS:
            for relations in RELATIONS:
                for seed in seeds:
                    record = one_cell(relations, load, seed, axis == 2)
                    records.append(record)
            print(f"  axis {axis} load {load} done", file=sys.stderr, flush=True)

    if args.json:
        Path(args.json).write_text(json.dumps(records, indent=2),
                                   encoding="utf-8")
        print(f"wrote {len(records)} records to {args.json}")

    for axis in axes:
        print(f"\n=== axis {axis}: "
              + ("few subjects under MANY relations, same total"
                 if axis == 2 else
                 "the same facts spread over more relations (N fixed)") + " ===")
        print(f"{'load':>6s}" + "".join(f"{'r=' + str(r):>10s}"
                                        for r in RELATIONS))
        for load in LOADS:
            row = f"{load:>6d}"
            for relations in RELATIONS:
                cells = [r for r in records
                         if r["relations"] == relations and r["load"] == load
                         and r["per_subject"] == (axis == 2)]
                if cells:
                    row += f"{sum(c['accuracy'] for c in cells) / len(cells):>10.4f}"
            print(row)
    print(f"\nchance {1.0 / 32:.4f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
