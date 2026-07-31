"""g33-01: does rounding the clock keep enough signal to learn identity?

`g32-01` and `g32-02` measured the statistic with the co-occurrence set handed
over intact. **The design does not get it handed over.** It has to reconstruct
which things happened together from rounded clocks on machines that disagree
about the time, and everything the reconstruction gets wrong is signal lost.

The single-process score on the same stream is the ceiling. This measures the
gap, over the three failures the option record named before anything was built:
a window too narrow to hold one moment, a window wide enough to merge two, and
clocks that disagree.

**Still one process.** A pass here says nothing about C1; a failure would settle
it, for the reason `openplexus/buckets.py` gives in its docstring. Containers are
`testbed/run.py` and are next, not here.

Predictions:
`experiments/sweeps/g33-01-does-the-bucket-join-keep-the-signal.txt`, committed
before this ran.
"""

from __future__ import annotations

import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.buckets import BucketConfig, Join, observations  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes, score_classes)
from openplexus.tasks.occasions import OccasionConfig, generate  # noqa: E402

#: Carried from g32-01 AT ITS OWN CONFIGURATION, and named as carried. 64
#: concepts gives 125 occasions each at this stream length, far above g32-02's
#: measured threshold of about 16 — deliberately, so that anything lost here is
#: the join losing it rather than the concept being starved.
CONCEPTS = 64
SURFACES = 3
PRESENCE = 0.7
NOISE = 3
DISTRACTORS = 1
OCCASIONS = 8000
K = SURFACES - 1

#: True time between one occasion and the next. Every width below is read
#: against this and against `spread_within`, not in absolute units.
TEMPO = 100

SEEDS = (0, 1, 2)
WIDTHS = (5, 20, 50, 100, 200, 500)
#: How long one moment lasts. 0 is instantaneous — the easy case. At TEMPO a
#: moment is as long as the gap to the next one, so no window can separate them.
DURATIONS = (0, 20, 60, 100)
SKEWS = (0, 50)
SPREADS = (0, 2)
ARMS = ("conditional", "local", "ppmi")

NODES = 8
OBSERVERS = SURFACES


def _ceiling(occasions: OccasionConfig, stream, arm: str) -> float:
    """The single-process score on this exact stream."""
    direct = CoOccurrence()
    for occasion in stream:
        direct.observe(occasion.surfaces)
    recovered = equivalence_classes(direct, STATISTICS[arm], K)
    return score_classes(recovered, occasions.classes(),
                         distractors=[occasions.concept_surfaces])["f1"]


def _joined(occasions: OccasionConfig, stream, arm: str, *, width: int,
            duration: int, skew: int, spread: int, lateness: int = 0,
            grace: int = 0, seed: int = 0) -> dict[str, float]:
    config = BucketConfig(width=width, spread=spread, skew=skew,
                          lateness=lateness, grace=grace, nodes=NODES,
                          observers=OBSERVERS, seed=seed)
    join = Join(config)
    join.run(observations(stream, config, tempo=TEMPO,
                          spread_within=duration))
    recovered = equivalence_classes(join.index, STATISTICS[arm], K)
    scored = score_classes(recovered, occasions.classes(),
                           distractors=[occasions.concept_surfaces])
    scored["messages"] = join.messages_per_observation
    scored["lost"] = (join.lost_late /
                      max(join.delivered + join.lost_late, 1))
    return scored


def _stream(seed: int):
    config = OccasionConfig(concepts=CONCEPTS, surfaces=SURFACES,
                            presence=PRESENCE, noise=NOISE,
                            distractors=DISTRACTORS, occasions=OCCASIONS,
                            seed=seed)
    return config, generate(config)


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()

    print(f"g33-01  concepts {CONCEPTS}  occasions {OCCASIONS}  tempo {TEMPO}  "
          f"nodes {NODES}  observers {OBSERVERS}  k {K}  seeds {SEEDS}")
    print("        f1 floor is 0.5; the ceiling is the single-process score "
          "on the same stream\n")

    built = [_stream(seed) for seed in SEEDS]

    for arm in ARMS:
        ceiling = sum(_ceiling(cfg, stream, arm)
                      for cfg, stream in built) / len(built)
        print(f"=== {arm} ===   single-process ceiling {ceiling:.4f}\n")

        for spread in SPREADS:
            for skew in SKEWS:
                header = (f"  spread {spread}, skew {skew}"
                          f"      " + "".join(f"{w:>9}" for w in WIDTHS))
                print(header)
                print("  " + "-" * (len(header) - 2))
                for duration in DURATIONS:
                    row = []
                    for width in WIDTHS:
                        got = [
                            _joined(cfg, stream, arm, width=width,
                                    duration=duration, skew=skew,
                                    spread=spread, seed=seed)["f1"]
                            for seed, (cfg, stream) in zip(SEEDS, built)]
                        row.append(sum(got) / len(got))
                    print(f"  moment lasts {duration:>4}   "
                          + "".join(f"{v:>9.4f}" for v in row))
                # Message cost is a property of `spread` alone, so it is
                # reported once per block rather than per cell.
                sample = _joined(built[0][0], built[0][1], arm, width=50,
                                 duration=0, skew=skew, spread=spread, seed=0)
                print(f"    messages per observation {sample['messages']:.1f}\n")

    print("=== LATENESS, at the best clean width, spread 0, skew 0 ===\n")
    print(f"  {'lateness':>10}{'grace':>8}{'lost':>9}{'f1':>9}")
    print("  " + "-" * 34)
    for lateness, grace in ((0, 0), (50, 0), (200, 0), (200, 200),
                            (500, 0), (500, 500), (2000, 500)):
        got = [_joined(cfg, stream, "conditional", width=50, duration=0,
                       skew=0, spread=0, lateness=lateness, grace=grace,
                       seed=seed)
               for seed, (cfg, stream) in zip(SEEDS, built)]
        lost = sum(g["lost"] for g in got) / len(got)
        f1 = sum(g["f1"] for g in got) / len(got)
        print(f"  {lateness:>10}{grace:>8}{lost:>9.4f}{f1:>9.4f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
