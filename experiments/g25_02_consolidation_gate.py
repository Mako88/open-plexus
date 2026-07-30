"""What does the consolidation gate cost? Recurrence gap against the fast store's horizon.

**This does not duplicate `g8_03_capture.py` or `g8_04_capped.py`.** Those measured
`capture_slots` -- the POOL that bounds consolidation -- and returned a null:
*"bounding the lasting store cannot reproduce a mechanism that gates the FAST one"*, every
pool recovering approximately zero. This measures the GATE itself, which is the
`elif fires:` branch as much as the pooled one, and it sweeps a different axis entirely:
how far apart a fact's occurrences are.

## The mechanism, read from the code rather than assumed

`local_memory.py` fires consolidation on `predictions[t-1] == token`, and says why it
counts them:

    COUNTED so a null can be attributed ... without this counter, "the persistent store
    did not help" and "the gate never opened" are the same number.

`model.consolidations` is therefore the quantity here, already exposed.

**On an ARBITRARY fact, "already got it right" means "already in the fast store and still
retrievable".** The fast store decays -- `consolidation` requires `decay < 1.0`, enforced
in `LocalMemoryConfig` -- so retrievability has a horizon, and the gate is about a fact
RECURRING INSIDE A WINDOW rather than about it being predictable from a pattern.

## The confound this is built around

A wider gap in a fixed-length sequence means fewer recurrences, so fewer chances to
consolidate whatever the horizon does. **Recurrence COUNT is held constant and the
sequence grows instead**, so gap and opportunity are separated by construction rather than
by adjustment afterwards.

Predictions are in `experiments/sweeps/g25-02-what-the-consolidation-gate-costs.txt`,
committed at `7be6f13` before this file existed.
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

WIDTH, VOCAB = 64, 40
#: Recurrences per fact, HELD CONSTANT across gaps. The sequence length grows with
#: the gap instead, which is what stops "wider gap" from meaning "fewer chances".
REPEATS = 6
FACTS = 8
GAPS = (2, 4, 8, 16, 32, 64)
#: Two decays so P3 can separate "the horizon is the cause" from "wide gaps are
#: harder for some other reason". 0.9 has a half-life near 6.6 steps, 0.95 near 13.5.
DECAYS = (0.9, 0.95)
IGNORE = -1


def stream(gap: int, seed: int) -> tuple[np.ndarray, np.ndarray]:
    """`FACTS` arbitrary pairs, each recurring `REPEATS` times `gap` apart.

    A fact is `(subject, object)` with the object drawn at random, so there is
    nothing to predict from a pattern -- the only route to predicting it is having
    stored it. That is the point: it isolates the fast store's horizon from any
    regularity the readout could learn instead.
    """
    rng = np.random.default_rng(seed)
    subjects = rng.choice(np.arange(2, VOCAB // 2), size=FACTS, replace=False)
    objects = rng.choice(np.arange(VOCAB // 2, VOCAB), size=FACTS, replace=False)

    # LENGTH IS HELD CONSTANT, sized for the widest gap. The first version grew
    # the sequence with the gap, so a wider gap meant more positions and more
    # chances for the gate to fire anywhere -- consolidations ROSE with gap and
    # `per_scored` exceeded 1.0, which is the tell that the count was not about
    # the facts at all. Holding recurrence count constant was not enough: length
    # was the binding confound, and this is rule 8's shape, a statistic gathered
    # over the whole sequence being read as one about eight facts.
    length = FACTS * REPEATS * max(GAPS) * 2
    tokens = rng.integers(2, VOCAB, size=length)
    targets = np.full(length, IGNORE, dtype=np.int64)
    for index in range(FACTS):
        # Occurrences of one fact are `gap` apart; facts are interleaved so no
        # single fact owns a contiguous stretch of the stream.
        start = 2 * index
        for repeat in range(REPEATS):
            at = start + repeat * gap * 2 * FACTS
            if at + 1 >= length:
                break
            tokens[at] = subjects[index]
            tokens[at + 1] = objects[index]
            targets[at + 1] = objects[index]
    return tokens.astype(np.int64), targets


def one_cell(gap: int, decay: float, seed: int) -> dict:
    config = LocalMemoryConfig(
        d_model=WIDTH, vocab_size=VOCAB, seed=seed, decay=decay,
        derived_keys=True, context_keys=True,
        consolidation=0.5, lasting_cap=5.0)
    model = LocalAssociativeMemory(config)
    tokens, targets = stream(gap, seed)
    model.run(tokens, targets, targets != IGNORE, learn=True)
    scored = int((targets != IGNORE).sum())
    return dict(gap=gap, decay=decay, seed=seed,
                consolidations=model.consolidations,
                scored=scored, length=len(tokens),
                per_scored=model.consolidations / scored if scored else 0.0,
                condition=f"gap{gap}|decay{decay}|seed{seed}|repeats{REPEATS}")


def one_shot(decay: float, seed: int) -> dict:
    """P4's arm: every fact appears ONCE. Nothing can recur inside any window."""
    global REPEATS
    was, REPEATS = REPEATS, 1
    try:
        result = one_cell(8, decay, seed)
    finally:
        REPEATS = was
    result["condition"] = f"ONE-SHOT|decay{decay}|seed{seed}"
    return result


def main() -> None:
    args = harness.parse_args(__doc__)
    seeds = (0, 1, 2) if args.seed is None else (args.seed,)
    records = [one_cell(gap, decay, seed)
               for seed in seeds for decay in DECAYS for gap in GAPS]
    records += [one_shot(decay, seed) for seed in seeds for decay in DECAYS]
    if args.json:
        harness.emit(records, Path(args.json))

    print(f"\n{FACTS} facts x {REPEATS} recurrences, held constant across gaps")
    print(f"{'decay':>7}{'gap':>6}{'consolidations':>16}{'per scored':>13}")
    for decay in DECAYS:
        for gap in GAPS:
            rows = [r for r in records if r["gap"] == gap and r["decay"] == decay
                    and "ONE-SHOT" not in r["condition"]]
            mean = sum(r["consolidations"] for r in rows) / len(rows)
            each = sum(r["per_scored"] for r in rows) / len(rows)
            print(f"{decay:>7}{gap:>6}{mean:>16.2f}{each:>13.4f}")
    print()
    for decay in DECAYS:
        rows = [r for r in records if "ONE-SHOT" in r["condition"]
                and r["decay"] == decay]
        mean = sum(r["consolidations"] for r in rows) / len(rows)
        print(f"  ONE-SHOT, decay {decay}: {mean:.2f} consolidations "
              f"(P4 predicts exactly 0)")


if __name__ == "__main__":
    main()
