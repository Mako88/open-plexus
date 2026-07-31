"""g39-03: at CONVERGENCE, does any exponent and combiner separate them?

`g39-01` found the grounding curve still climbing at 3,000 occasions, which puts
every *"X does not help"* conclusion in this line at risk — `CLAUDE.md` names
discarding a good idea on an invalid measurement as the most expensive error
available. `g39-02` found the curve flat from 12,000.

**So the refuted comparisons have to be re-run at the converged length**, and
this is that re-run for the one failure nothing has fixed.

## The specific claim being re-tested

Two sweeps concluded there is no setting that keeps the word AND refuses the
ever-present distractor:

    g36-06   the damping exponent, at 3,000 occasions. No alpha does both
    g38-01   the edge combiner, at 3,000 occasions. `mean` wins the link and
             admits the distractor for every word

**Both were measured before the mechanism had converged.** `g39-02` showed the
incumbent partition gaining **0.2549** from eight times the data where the walk
gained 0.0202 — so a short stream does not penalise every arm equally, and an
arm that looked refuted may simply have been further from its own plateau.

This crosses the two axes at 3,000 and at 24,000, so what MOVED is visible rather
than inferred.

## Why this is the right shape of re-check

It is not a sixth knob. `g38-01` and `g39-02` between them argue the conflict is
structural, and **this is the measurement that either supports that or refutes
it.** If nothing separates them at convergence either, "structural" stops being a
summary of five failures and becomes a claim with a converged measurement behind
it. If something does, five conclusions get revised.

## What this does NOT duplicate, and what was searched

Searched by capability — alpha, exponent, combiner, distractor admission —
across `experiments/` and `openplexus/`.

- **`experiments/g36_06_...`** swept the exponent and **`g38_01_...`** the
  combiner, each alone and each at 3,000. Neither crossed them and neither ran
  past one pass.
- **`experiments/g39_01_...`** supplies the scorer, IMPORTED, so the columns
  mean exactly what they meant there.

Predictions: `experiments/sweeps/g39-03-is-the-distractor-failure-structural.txt`
"""

from __future__ import annotations

import pathlib
import sys
import time
from collections import Counter, defaultdict

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from experiments.g36_04_a_picture_a_sound_and_a_word import (  # noqa: E402
    DISTRACTORS, NOISE, _spectra)
from openplexus.grounding import CoOccurrence, damped, reach  # noqa: E402
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
BEAM, DEPTH = 16, 1
CHECKPOINTS = (3000, 24000)

#: **Alpha 0 and 0.25 are excluded and the reason is arithmetic, not cost.**
#: `reach` multiplies path strength along a path and `raw_count` is unbounded, so
#: a score above 1 would make a longer path stronger than a short one. At
#: `DEPTH = 1` nothing is multiplied, so they would in fact be safe here — but
#: `g36-06` already measured both as admitting the distractor at 1.0000 and
#: 0.9333, which is the failing end of the axis this run is probing.
ALPHAS = (0.5, 0.75, 1.0)
COMBINERS = ("min", "geometric", "mean", "max")


def _score(index: CoOccurrence, image_major, statistic, combine: str) -> dict:
    """Link precision, coverage and distractor admission, at one setting."""
    spare = 2 * CODES + len(mnist.WORDS)
    hits = [0, 0]
    wanted = admitted = 0
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        found = reach(index, statistic, word, beam=BEAM, depth=DEPTH,
                      combine=combine)
        order = [s for _, s in sorted(((-v, k) for k, v in found.items()))]
        want = sum(1 for _, d in image_major.items() if d == digit)
        wanted += want
        for surface in [s for s in order if s < CODES][:want]:
            hits[1] += 1
            hits[0] += image_major.get(surface) == digit
        admitted += spare in order[:want] if want else 0
    return {
        "link": hits[0] / hits[1] if hits[1] else 0.0,
        "covered": hits[1] / wanted if wanted else 0.0,
        "distractor": admitted / len(mnist.WORDS),
    }


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    if not (MNIST_DATA / "train-images-idx3-ubyte.gz").exists():
        raise SystemExit(
            f"no data in {MNIST_DATA}. Run: python tools/fetch_mnist.py")
    if not FSDD_DATA.exists():
        raise SystemExit(
            f"no data in {FSDD_DATA}. Run: python tools/fetch_fsdd.py")

    digits = mnist.read(MNIST_DATA, limit=IMAGES)
    pixels = (np.frombuffer(b"".join(digits.images), dtype=np.uint8)
              .reshape(len(digits), digits.pixels).astype(np.float64))
    utterances = [spoken.read(path) for path in spoken.available(FSDD_DATA)]
    spectra = _spectra(utterances)
    heard = [u.digit for u in utterances]

    pool = defaultdict(list)
    for row, label in enumerate(digits.labels):
        pool[label].append(row)

    print(f"g39-03  arm together, {CODES} codes, beam {BEAM}, depth {DEPTH}, "
          f"seeds {SEEDS}")
    print(f"        3,000 is one pass and reproduces g36-06/g38-01's condition; "
          f"24,000 is converged\n")

    rows: dict[tuple, dict[str, float]] = {}
    for seed in SEEDS:
        image_code = harness.quantise(pixels, CODES, seed)
        audio_code = harness.quantise(spectra, CODES, seed)
        _, image_major = harness.purity(image_code, digits.labels)
        rng = np.random.default_rng(seed)
        word = {d: 2 * CODES + d for d in range(len(mnist.WORDS))}
        spare = 2 * CODES + len(mnist.WORDS)
        used: Counter = Counter()

        index = CoOccurrence()
        position = 0
        while position < max(CHECKPOINTS):
            for audio_row, digit in enumerate(heard):
                here = pool[digit]
                image_row = here[used[digit] % len(here)]
                used[digit] += 1
                position += 1
                picture, sound = image_code[image_row], audio_code[audio_row]
                if picture >= 0 and sound >= 0:
                    present = {word[digit], picture, CODES + sound}
                    for other in rng.choice(len(mnist.WORDS), NOISE,
                                            replace=False):
                        present.add(word[int(other)])
                    for extra in range(DISTRACTORS):
                        present.add(spare + extra)
                    index.observe(present)
                if position in CHECKPOINTS:
                    for alpha in ALPHAS:
                        statistic = damped(alpha)
                        for combine in COMBINERS:
                            got = _score(index, image_major, statistic, combine)
                            into = rows.setdefault(
                                (position, alpha, combine), {})
                            for key, value in got.items():
                                into[key] = (into.get(key, 0.0)
                                             + value / len(SEEDS))
                if position >= max(CHECKPOINTS):
                    break

    header = (f"{'occasions':>10}{'alpha':>7}{'combiner':<11}{'link':>8}"
              f"{'covered':>9}{'distractor':>12}{'BOTH?':>7}")
    print(header)
    print("-" * len(header))
    solved = []
    for position in CHECKPOINTS:
        for alpha in ALPHAS:
            for combine in COMBINERS:
                got = rows.get((position, alpha, combine))
                if not got:
                    continue
                # BOTH means: usable link at usable coverage, AND the distractor
                # kept out. The coverage clause is not decoration -- `g38-03`
                # reported 1.0000 precision over 8% coverage as a top cell.
                both = (got["link"] >= 0.80 and got["covered"] >= 0.80
                        and got["distractor"] <= 0.05)
                if both:
                    solved.append((position, alpha, combine))
                print(f"{position:>10}{alpha:>7.2f}{combine:<11}"
                      f"{got['link']:>8.4f}{got['covered']:>9.4f}"
                      f"{got['distractor']:>12.4f}{'YES' if both else '-':>7}")
        print()

    print(f"settings meeting BOTH: {len(solved)} of "
          f"{len(CHECKPOINTS) * len(ALPHAS) * len(COMBINERS)}")
    for entry in solved:
        print(f"  {entry}")
    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
