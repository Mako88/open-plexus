"""g32-02: is `zipf`'s damage skew, or is it just too few occasions?

`g32-01` found every arm collapsing toward the floor at zipf 2.0, distractor or
no distractor — a larger effect than the distractor the falsifier was aimed at,
and one nobody had registered. **That grid cannot say what caused it.** At zipf
2.0 over 64 concepts the commonest concept takes about 61% of the stream and the
rarest takes about one occasion, so skew and starvation move together in every
cell.

This separates them by scoring **per concept** against how many occasions that
concept was actually the subject of.

    uniform stream, varying length    recovery as a function of occasions alone
    skewed stream, one length         the same curve, read per concept

If the skewed points lie ON the uniform curve, skew has no effect beyond
starvation and the honest statement is a **minimum occasions per concept**. If
they lie below it, a common concept is doing something to a rare one — flooding
neighbour lists — and skew is its own problem.

Predictions: `experiments/sweeps/g32-02-how-many-occasions-does-a-concept-need.txt`,
committed before this ran.
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
                                  class_f1, equivalence_classes)
from openplexus.tasks.occasions import OccasionConfig, generate  # noqa: E402

#: Carried from g32-01 and named as carried, per CLAUDE.md. `concepts` 64,
#: `surfaces` 3, `presence` 0.7, `noise` 3, `k` 2 are that sweep's pins, chosen
#: for a 64-concept world and used here at the SAME concept count — so the pin is
#: at its own configuration rather than transplanted.
CONCEPTS = 64
SURFACES = 3
PRESENCE = 0.7
NOISE = 3
K = SURFACES - 1

SEEDS = (0, 1, 2)
#: Uniform streams, chosen so occasions-per-concept spans well under to well over
#: whatever the threshold turns out to be: 64 concepts, so these are about
#: 4, 8, 16, 31, 62, 125 and 250 occasions each.
LENGTHS = (256, 512, 1024, 2000, 4000, 8000, 16000)
#: One skewed stream at g32-01's own length, read per concept.
SKEW_LENGTH = 8000
SKEWS = (1.0, 2.0)
ARMS = ("count", "ppmi")

#: Buckets for occasions-per-concept, so a skewed stream's rare concepts can be
#: compared against uniform concepts seen the same number of times.
BUCKETS = (2, 4, 8, 16, 32, 64, 128, 256, 1024, 10 ** 9)


def _per_concept(config: OccasionConfig, arm: str) -> list[tuple[int, float]]:
    """(occasions this concept was the subject of, mean f1 over its surfaces)."""
    stream = generate(config)
    index = CoOccurrence()
    subject_count = [0] * config.concepts
    for occasion in stream:
        index.observe(occasion.surfaces)
        subject_count[occasion.subject] += 1

    truth = config.classes()
    recovered = equivalence_classes(index, STATISTICS[arm], K)

    out = []
    for concept in range(config.concepts):
        members = range(concept * config.surfaces,
                        (concept + 1) * config.surfaces)
        scores = [class_f1(recovered.get(s, frozenset({s})), truth[s])
                  for s in members]
        out.append((subject_count[concept], sum(scores) / len(scores)))
    return out


def _bucketed(points: list[tuple[int, float]]) -> dict[int, tuple[int, float]]:
    """Group (count, f1) points by occasion bucket. Returns bucket -> (n, mean)."""
    grouped: dict[int, list[float]] = {}
    for count, score in points:
        for edge in BUCKETS:
            if count < edge:
                grouped.setdefault(edge, []).append(score)
                break
    return {edge: (len(v), sum(v) / len(v)) for edge, v in grouped.items()}


def _report(title: str, points: list[tuple[int, float]]) -> None:
    print(f"  {title}")
    table = _bucketed(points)
    for edge in BUCKETS:
        if edge not in table:
            continue
        n, mean = table[edge]
        label = "1024+" if edge == 10 ** 9 else f"under {edge}"
        print(f"    {label:<12}{n:>6} concepts{mean:>10.4f}")
    print()


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()

    print(f"g32-02  concepts {CONCEPTS}  surfaces {SURFACES}  "
          f"presence {PRESENCE}  noise {NOISE}  k {K}  seeds {SEEDS}")
    print("        f1 is per concept, and the floor for a 3-surface concept "
          "recovered alone is 0.5\n")

    for arm in ARMS:
        print(f"=== {arm} ===\n")
        print("UNIFORM STREAMS, whole-stream mean and occasions per concept")
        for length in LENGTHS:
            points: list[tuple[int, float]] = []
            for seed in SEEDS:
                points.extend(_per_concept(OccasionConfig(
                    concepts=CONCEPTS, surfaces=SURFACES, presence=PRESENCE,
                    noise=NOISE, distractors=0, zipf=0.0,
                    occasions=length, seed=seed), arm))
            each = length / CONCEPTS
            mean = sum(s for _, s in points) / len(points)
            print(f"    {length:>6} occasions  ~{each:>6.1f} each   f1 {mean:.4f}")
        print()

        for zipf in SKEWS:
            points = []
            for seed in SEEDS:
                points.extend(_per_concept(OccasionConfig(
                    concepts=CONCEPTS, surfaces=SURFACES, presence=PRESENCE,
                    noise=NOISE, distractors=0, zipf=zipf,
                    occasions=SKEW_LENGTH, seed=seed), arm))
            _report(f"SKEWED zipf {zipf}, {SKEW_LENGTH} occasions, "
                    f"by occasions per concept", points)

        uniform: list[tuple[int, float]] = []
        for length in LENGTHS:
            for seed in SEEDS:
                uniform.extend(_per_concept(OccasionConfig(
                    concepts=CONCEPTS, surfaces=SURFACES, presence=PRESENCE,
                    noise=NOISE, distractors=0, zipf=0.0,
                    occasions=length, seed=seed), arm))
        _report("UNIFORM, pooled across lengths, by occasions per concept "
                "-- THE CURVE THE SKEWED ROWS ARE COMPARED AGAINST", uniform)

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
