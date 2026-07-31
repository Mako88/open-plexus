"""g32-01: can counting separate "always there" from "is the thing"?

The falsifier registered in `docs/options/time-bucket-join.md` and
`docs/options/identity-without-a-global-id.md`, run for the first time. Predictions
are in `experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt` and were
committed before this script existed.

**Local, not a sweep.** Counting over a few thousand occasions is seconds, and the
COST section records what it actually took. Nothing here trains anything.

**What a failure here settles, and what it does not.** The mechanism the grounding
design commits to is counting at `owner(surface)`; distribution can only lose
information relative to this, never add it, so an arm that cannot separate the
distractor with perfect single-process information cannot separate it spread over
machines. A pass is weaker and settles nothing about C1 — that is the container
run, and it is deliberately not built yet.
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
                                  equivalence_classes, score_classes)
from openplexus.tasks.occasions import (OccasionConfig, generate,  # noqa: E402
                                        shuffled)

#: Held constant across every cell, with where each value came from.
#:
#: `concepts` 64 gives 192 surfaces against an occasion of about six. The pin is
#: NOT inherited: `test_occasions.ShuffledControl` found that a small world hands
#: out same-concept co-occurrence by accident, so the control stops being a floor,
#: and 32x is chosen to sit well clear of that.
#:
#: `presence` 0.7 is what makes the falsifier an experiment rather than a tie —
#: see `occasions.py`. `k` 2 is `surfaces - 1`, so the mechanism is TOLD how large
#: a class is. That is generous on purpose: a failure under it is strong.
CONCEPTS = 64
SURFACES = 3
PRESENCE = 0.7
NOISE = 3
OCCASIONS = 8000
K = SURFACES - 1

SEEDS = (0, 1, 2)
ZIPFS = (0.0, 1.0, 2.0)
DISTRACTORS = (0, 1)
ARMS = ("count", "weighted", "conditional", "ppmi")


def _cell(config: OccasionConfig, control: bool) -> dict[str, dict[str, float]]:
    """Every arm scored on one stream."""
    stream = generate(config)
    if control:
        stream = shuffled(stream, seed=config.seed)
    index = CoOccurrence()
    for occasion in stream:
        index.observe(occasion.surfaces)

    truth = config.classes()
    marked = [s for s in range(config.concept_surfaces, config.vocabulary)]
    scores = {}
    for arm in ARMS:
        recovered = equivalence_classes(index, STATISTICS[arm], K)
        scores[arm] = score_classes(recovered, truth, distractors=marked)
    return scores


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()

    print(f"g32-01  concepts {CONCEPTS}  surfaces {SURFACES}  "
          f"presence {PRESENCE}  noise {NOISE}  occasions {OCCASIONS}  k {K}")
    print(f"        seeds {SEEDS}\n")

    header = (f"{'stream':<9}{'zipf':>6}{'distr':>7}{'arm':<13}"
              f"{'f1':>8}{'captured':>10}{'largest':>9}")
    print(header)
    print("-" * len(header))

    for control in (False, True):
        for zipf in ZIPFS:
            for distractors in DISTRACTORS:
                gathered: dict[str, list[dict[str, float]]] = {a: [] for a in ARMS}
                for seed in SEEDS:
                    config = OccasionConfig(
                        concepts=CONCEPTS, surfaces=SURFACES,
                        presence=PRESENCE, noise=NOISE,
                        distractors=distractors, zipf=zipf,
                        occasions=OCCASIONS, seed=seed)
                    for arm, result in _cell(config, control).items():
                        gathered[arm].append(result)
                for arm in ARMS:
                    runs = gathered[arm]
                    mean = {key: sum(r[key] for r in runs) / len(runs)
                            for key in ("f1", "captured", "largest")}
                    label = "shuffled" if control else "real"
                    print(f"{label:<9}{zipf:>6.1f}{distractors:>7}{arm:<13}"
                          f"{mean['f1']:>8.4f}{mean['captured']:>10.4f}"
                          f"{mean['largest']:>9.4f}")
            print()

    print(f"COST: {time.time() - started:.1f}s wall, one process, "
          f"{len(SEEDS) * len(ZIPFS) * len(DISTRACTORS) * 2} streams")


if __name__ == "__main__":
    main()
