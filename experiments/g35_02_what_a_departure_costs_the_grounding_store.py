"""g35-02: what does losing a node cost, when nothing is replicated?

`partitioned.ConceptStore` holds each concept on `replicas` nodes, so a
departure is survivable and the measured cost is depletion. **The grounding
store has no replicas at all.** A node that leaves takes every row it held,
permanently, and nothing falls through to a survivor.

Two costs, and only the first is obvious:

  - the rows it held are gone;
  - **every surviving surface loses those as candidates**, because ranking needs
    `count(y)` from `owner(y)` and a departed peer cannot supply it.

So a concept is damaged if ANY of its surfaces was owned by the departed node,
which is a larger share than the ring gave that node. This measures how much
larger, and whether more modalities help or hurt.

Predictions:
`experiments/sweeps/g35-02-what-a-departure-costs-the-grounding-store.txt`
"""

from __future__ import annotations

import sys
import time
from itertools import combinations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.federated import Federation  # noqa: E402
from openplexus.grounding import (STATISTICS, class_f1,  # noqa: E402
                                  score_classes)
from openplexus.tasks.occasions import OccasionConfig, generate  # noqa: E402

CONCEPTS = 32
PRESENCE = 0.7
NOISE = 3
DISTRACTORS = 1
OCCASIONS = 4000
NODES = 8
K = None                      # the derived bound, g33-04's winner
ARM = "conditional"
SEEDS = (0, 1, 2)
SURFACES = (2, 3, 5)
LOST = (0, 1, 2, 4)


def _fill(surfaces: int, seed: int):
    config = OccasionConfig(concepts=CONCEPTS, surfaces=surfaces,
                            presence=PRESENCE, noise=NOISE,
                            distractors=DISTRACTORS, occasions=OCCASIONS,
                            seed=seed)
    federation = Federation(nodes=NODES, seed=seed)
    for occasion in generate(config):
        for surface in occasion.surfaces:
            federation.note(surface)
        for one, other in combinations(sorted(occasion.surfaces), 2):
            federation.link(one, other)
    return config, federation


def _score(config, federation) -> dict[str, float]:
    """Recover every class by walking, and score against the truth."""
    truth = config.classes()
    statistic = STATISTICS[ARM]
    total, biggest = 0.0, 0
    scored = 0
    for surface in range(config.concept_surfaces):
        if not federation.present(surface):
            # Its owner is gone, so nothing can be recovered for it. Scored as
            # zero rather than skipped: skipping would report the average over
            # the surfaces that SURVIVED, which is a different question and a
            # flattering one.
            scored += 1
            continue
        found = federation.walk(surface, statistic, K)
        total += class_f1(found, truth[surface])
        biggest = max(biggest, len(found))
        scored += 1
    return {"f1": total / max(scored, 1),
            "largest": biggest / config.vocabulary}


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    print(f"g35-02  concepts {CONCEPTS}  nodes {NODES}  occasions {OCCASIONS}  "
          f"arm {ARM}  bound derived  seeds {SEEDS}")
    print("        NOTHING IS REPLICATED: a departed node's rows are gone\n")

    header = (f"{'surfaces':>9}{'lost':>6}{'ring share':>12}"
              f"{'surfaces gone':>15}{'concepts hit':>14}{'f1':>9}{'largest':>9}")
    print(header)
    print("-" * len(header))

    for surfaces in SURFACES:
        for lost in LOST:
            shares, gone, hit, f1s, larges = [], [], [], [], []
            for seed in SEEDS:
                config, federation = _fill(surfaces, seed)
                for node in range(lost):
                    federation.lose(node)
                total = config.concept_surfaces
                missing = [s for s in range(total) if not federation.present(s)]
                damaged = {s // surfaces for s in missing}
                shares.append(lost / NODES)
                gone.append(len(missing) / total)
                hit.append(len(damaged) / CONCEPTS)
                result = _score(config, federation)
                f1s.append(result["f1"])
                larges.append(result["largest"])
            mean = lambda v: sum(v) / len(v)          # noqa: E731 - local
            print(f"{surfaces:>9}{lost:>6}{mean(shares):>12.3f}"
                  f"{mean(gone):>15.3f}{mean(hit):>14.3f}"
                  f"{mean(f1s):>9.4f}{mean(larges):>9.4f}")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
