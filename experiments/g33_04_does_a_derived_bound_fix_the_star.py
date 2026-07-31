"""g33-04: does a per-surface bound fix the star a global `k` cannot express?

`g33-02` measured a single global `k` failing on a star, and the failure is
arithmetic rather than statistical: the hub needs `k` at least its own degree
while a spoke needs 1, so at `k` 2 the hub cannot reach its spokes and at `k` 3
every unrelated surface admits noise until the graph is one class holding 0.98
of everything.

`neighbours(..., k=None)` derives the count per surface from where that
surface's own ranking falls off — `DECISIONS.md` §6's option, extracted from
`local_memory._cliff_candidates` so one implementation serves both.

**The reason to doubt it is measured and not hypothetical.** Note 058 found real
language co-occurrence decaying in steps of 0.02-0.03 where the families task
falls 0.45 at once, bimodal at no setting; and `cliff` on an even slope is
decided by floating point. This grid is a designed world with a real cliff in
it, so a pass here says the rule works WHERE THERE IS A CLIFF and says nothing
about anywhere else.

Predictions: `experiments/sweeps/g33-04-does-a-derived-bound-fix-the-star.txt`
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
OCCASIONS = 8000
SEEDS = (0, 1, 2)
SURFACES = (3, 4, 5)
PAIRINGS = ("complete", "chain", "star")
#: `None` is the derived bound. The fixed values are g33-02's, kept so the
#: comparison is like for like rather than against a remembered number.
BOUNDS = (2, 3, None)
ARM = "conditional"


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    print(f"g33-04  concepts {CONCEPTS}  occasions {OCCASIONS}  arm {ARM}  "
          f"seeds {SEEDS}")
    print("        f1 floor 0.5. `bridged` IS RECALL and reads 1.0000 under "
          "collapse -- read it with `largest`.\n")

    header = (f"{'pairing':<10}{'surfaces':>9}{'bound':>8}{'f1':>9}"
              f"{'bridged':>10}{'largest':>10}")
    print(header)
    print("-" * len(header))
    for pairings in PAIRINGS:
        for surfaces in SURFACES:
            for k in BOUNDS:
                f1s, bridges, largest = [], [], []
                for seed in SEEDS:
                    config = OccasionConfig(
                        concepts=CONCEPTS, surfaces=surfaces,
                        presence=PRESENCE, noise=NOISE,
                        distractors=DISTRACTORS, pairings=pairings,
                        occasions=OCCASIONS, seed=seed)
                    scored, bridged = harness.occasions_cell(
                        config, STATISTICS[ARM], k)
                    f1s.append(scored["f1"])
                    largest.append(scored["largest"])
                    if bridged is not None:
                        bridges.append(bridged)
                shown = (f"{sum(bridges) / len(bridges):>10.4f}" if bridges
                         else f"{'n/a':>10}")
                label = "derived" if k is None else str(k)
                print(f"{pairings:<10}{surfaces:>9}{label:>8}"
                      f"{sum(f1s) / len(f1s):>9.4f}{shown}"
                      f"{sum(largest) / len(largest):>10.4f}")
        print()
    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
