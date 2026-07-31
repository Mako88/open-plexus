"""g36-02: does adding modalities make a concept learnable from FEWER occasions?

John's claim, 2026-08-01: *"inherently the more different types of inputs that
can co-occur, the more differentiation between things is possible, and the
quicker/better the model will be able to learn."*

There is a mechanism that would make it true. One occasion showing `s` surfaces
of a concept yields `C(s, 2)` pairwise observations, so evidence per occasion
grows QUADRATICALLY in the modality count while the occasion count grows
linearly. If the graph needs a fixed amount of pairwise evidence, more modalities
should reach it sooner.

**And there is a measured reason it could go the other way**, which is why this
runs rather than being agreed with. `g33-02` found a hub-and-spoke concept —
which is exactly the many-modalities shape — FAILING as spokes were added, under
a fixed bound: the hub could not hold all its spokes and bridging fell to 0.1667.
`g33-04`'s derived bound repaired it. So the intuition is right only if the bound
adapts, and the counterexample is one this project has already produced.

Predictions:
`experiments/sweeps/g36-02-do-more-modalities-learn-faster.txt`
"""

from __future__ import annotations

import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.grounding import STATISTICS  # noqa: E402
from openplexus.tasks.occasions import OccasionConfig  # noqa: E402

CONCEPTS = 64
PRESENCE = 0.7
NOISE = 3
DISTRACTORS = 1
SEEDS = (0, 1, 2)
SURFACES = (2, 3, 5, 8)
#: Occasions per concept, which is the axis `g32-02` measured the threshold on.
PER_CONCEPT = (2, 4, 8, 16, 32, 64)
BOUNDS = (2, None)
ARM = "conditional"
#: What counts as recovered. `g32-02` read its curve by eye; a stated bar makes
#: "the threshold" a number rather than a judgement.
BAR = 0.95


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    print(f"g36-02  concepts {CONCEPTS}  presence {PRESENCE}  noise {NOISE}  "
          f"distractors {DISTRACTORS}  arm {ARM}  seeds {SEEDS}")
    print(f"        `connected` is floor-free; threshold is the first "
          f"occasions/concept reaching {BAR}\n")

    for bound in BOUNDS:
        label = "derived" if bound is None else f"fixed {bound}"
        header = (f"  bound {label:<9}" +
                  "".join(f"{n:>8}" for n in PER_CONCEPT) + f"{'thresh':>9}")
        print(header)
        print("  " + "-" * (len(header) - 2))
        for surfaces in SURFACES:
            row, threshold = [], None
            for per in PER_CONCEPT:
                scores = []
                for seed in SEEDS:
                    config = OccasionConfig(
                        concepts=CONCEPTS, surfaces=surfaces,
                        presence=PRESENCE, noise=NOISE,
                        distractors=DISTRACTORS,
                        occasions=per * CONCEPTS, seed=seed)
                    scored, _ = harness.occasions_cell(
                        config, STATISTICS[ARM], bound)
                    scores.append(scored["connected"])
                mean = sum(scores) / len(scores)
                row.append(mean)
                if threshold is None and mean >= BAR:
                    threshold = per
            shown = str(threshold) if threshold else ">64"
            print(f"  {surfaces} surfaces   " +
                  "".join(f"{v:>8.4f}" for v in row) + f"{shown:>9}")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
