"""g33-02: can the walk reach a modality it never saw the concept in?

Every run in g32 and g33 showed all of a concept's surfaces together, so learning
that they belong took one direct co-occurrence and **the walk never had to bridge
anything.** That is easier than the world and easier than
`identity-without-a-global-id.md` claims — *"the equivalence class reached by
starting at any member and WALKING"* only means something when some members are
not directly connected.

`occasions.pairings` builds the harder stream. Under `chain`, modality `m` shares
an occasion only with `m±1`, so the ends are never seen together and can only be
linked through what sits between them. Under `star`, every spoke meets the hub
and no spoke ever meets another.

Scored on the pairs that were never observed together — `reached_together` over
`config.apart()` — because averaging over every surface would let a class that is
mostly right hide the one link that had to be inferred.

**This is `GOALS.md` gate G7's shape at symbol level.** It is not G7: there is no
second modality here in any real sense, only a stream built so that some of a
concept's surfaces never co-occur. What it tests is whether the WALK can close a
gap, which is the mechanism G7 would rest on.

Predictions: `experiments/sweeps/g33-02-can-the-walk-bridge-two-modalities-that-never-meet.txt`
"""

from __future__ import annotations

import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes, reached_together,
                                  score_classes)
from openplexus.tasks.occasions import OccasionConfig, generate  # noqa: E402

#: Carried from g33-01 at its own concept count.
CONCEPTS = 64
PRESENCE = 0.7
NOISE = 3
DISTRACTORS = 1
OCCASIONS = 8000

SEEDS = (0, 1, 2)
SURFACES = (3, 4, 5)
PAIRINGS = ("complete", "chain", "star")
#: `k` is the axis P3 is about: a longer chain needs each middle link to spend
#: its slots on the right partners, so the question is whether `k` has to grow.
KS = (2, 3, 4)
ARM = "conditional"


def _cell(surfaces: int, pairings: str, k: int, seed: int):
    config = OccasionConfig(concepts=CONCEPTS, surfaces=surfaces,
                            presence=PRESENCE, noise=NOISE,
                            distractors=DISTRACTORS, pairings=pairings,
                            occasions=OCCASIONS, seed=seed)
    index = CoOccurrence()
    for occasion in generate(config):
        index.observe(occasion.surfaces)
    recovered = equivalence_classes(index, STATISTICS[ARM], k)
    f1 = score_classes(recovered, config.classes(),
                       distractors=[config.concept_surfaces])["f1"]

    apart = config.apart()
    if not apart:
        return f1, None
    pairs = [(concept * surfaces + one, concept * surfaces + other)
             for concept in range(CONCEPTS) for one, other in apart]
    return f1, reached_together(recovered, pairs)


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()

    print(f"g33-02  concepts {CONCEPTS}  presence {PRESENCE}  noise {NOISE}  "
          f"distractors {DISTRACTORS}  occasions {OCCASIONS}  arm {ARM}")
    print("        f1 floor is 0.5. `bridged` is scored ONLY over surface pairs "
          "whose modalities never shared an occasion\n")

    header = (f"{'pairing':<10}{'surfaces':>9}{'k':>4}{'never met':>11}"
              f"{'f1':>9}{'bridged':>10}")
    print(header)
    print("-" * len(header))

    for pairings in PAIRINGS:
        for surfaces in SURFACES:
            for k in KS:
                f1s, bridges = [], []
                for seed in SEEDS:
                    f1, bridged = _cell(surfaces, pairings, k, seed)
                    f1s.append(f1)
                    if bridged is not None:
                        bridges.append(bridged)
                apart = OccasionConfig(surfaces=surfaces,
                                       pairings=pairings).apart()
                shown = (f"{sum(bridges) / len(bridges):>10.4f}" if bridges
                         else f"{'n/a':>10}")
                print(f"{pairings:<10}{surfaces:>9}{k:>4}{len(apart):>11}"
                      f"{sum(f1s) / len(f1s):>9.4f}{shown}")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
