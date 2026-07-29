"""Before building a two-level reader: is there anything for it to pick?

[Note 049](../docs/notes/049-specific-beats-general-is-a-read-policy.md) proposes
writing at both the surface and concept addresses and reading the surface first.
That touches `run`, which is the one file where a change invalidates the
comparison set, so it is worth knowing whether the ceiling exists before paying
for it.

**This builds nothing.** It runs the two arms that already exist, position by
position, and asks:

    ungrouped right         surface addressing alone
    concept right           concept addressing alone
    EITHER right            the ceiling for ANY selection rule
    both wrong              what no reader can recover

If `EITHER` is barely above the better single arm, the two are right in the same
places and a selector has nothing to choose between — note 049's mechanism would
be machinery for no gain, and the honest move is not to build it.

This is the project's own habit applied one more time: four mechanisms were
measured before being built, and three were never written.

## What it CANNOT tell us

The two arms are **separate models with separate stores and separate readouts**.
A single model reading two addresses is not the same system, so `EITHER` is an
upper bound rather than a prediction. A low ceiling refutes the mechanism; a high
one licenses building it and no more.

## PREDICTION, registered before running

  C1  On EXCEPTION positions, `EITHER` exceeds `concept` by more than 0.30 —
      the surface arm is right where the concept arm is wrong, and it is right
      about the same questions rather than merely as often. If the two arms fail
      together, note 049 is dead before it is built.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from experiments.g19_01_can_grouping_answer_what_was_never_stated import (  # noqa: E402
    BACKGROUND, CONTENT_WIDTH, EPOCHS, GROUPS, INDEX_POWER, INDEX_WINDOW,
    TEST, TRAIN, WIDTH, silent, surfaces_for)
from openplexus.keys import ByConcept  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.families import (  # noqa: E402
    FamilyConfig, dataset)

SEEDS = (0, 1, 2)
EXCEPTIONS = 1


def trained(arm: str, config: FamilyConfig, seed: int):
    surfaces, index, _ = surfaces_for(arm, config, seed)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=config.vocab_size, d_model=WIDTH, lr=0.05,
        key_scale=0.5, decay=0.99, seed=seed))
    if arm not in ("ungrouped", "nostore"):
        model.key_source = ByConcept(model.key_source, surfaces,
                                     config.vocab_size)
        model.surfaces = surfaces
    model.content = index

    rng = np.random.default_rng(seed)
    train = dataset(config, TRAIN)
    order = np.arange(len(train))
    for _ in range(EPOCHS):
        rng.shuffle(order)
        for position in order:
            tokens = np.asarray(train[int(position)].tokens)
            targets = np.roll(tokens, -1)
            scored = np.ones(len(tokens), dtype=bool)
            scored[-1] = False
            model.run(tokens, targets, scored, learn=True)
    return model


def main() -> int:
    harness.refuse_if_mutating()
    from dataclasses import replace

    totals: dict[str, list[int]] = {}
    for seed in SEEDS:
        config = FamilyConfig(seed=seed, exceptions_per_family=EXCEPTIONS)
        surface = trained("ungrouped", config, seed)
        grouped = trained("concept", config, seed)
        test = dataset(replace(config, seed=seed + 5000), TEST)

        for sequence in test:
            tokens = np.asarray(sequence.tokens)
            a = surface.run(tokens)
            b = grouped.run(tokens)
            for where, transfer, exception in zip(sequence.query_positions,
                                                  sequence.is_transfer,
                                                  sequence.is_exception):
                kind = ("exception" if exception
                        else "transfer" if transfer else "direct")
                answer = int(tokens[where + 1])
                right_a = int(a[where]) == answer
                right_b = int(b[where]) == answer
                row = totals.setdefault(kind, [0, 0, 0, 0])
                row[0] += int(right_a)
                row[1] += int(right_b)
                row[2] += int(right_a or right_b)
                row[3] += 1

    print(f"{'kind':<11}{'ungrouped':>11}{'concept':>9}{'EITHER':>9}"
          f"{'both wrong':>12}")
    for kind in ("direct", "transfer", "exception"):
        if kind not in totals:
            continue
        a, b, either, n = totals[kind]
        print(f"{kind:<11}{a / n:>11.4f}{b / n:>9.4f}{either / n:>9.4f}"
              f"{1 - either / n:>12.4f}")

    if "exception" in totals:
        a, b, either, n = totals["exception"]
        gain = either / n - b / n
        print(f"\nC1: EITHER exceeds concept on EXCEPTION by {gain:+.4f} -> "
              f"{'CONFIRMED' if gain > 0.30 else 'REFUTED'}")
        if gain <= 0.30:
            print("    The two arms fail in the same places, so a selector has "
                  "nothing to choose between. Note 049 is dead before it is "
                  "built, which is the cheapest way for a mechanism to die.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
