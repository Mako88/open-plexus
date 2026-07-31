"""g36-01: can a word and a PICTURE reach the same concept?

Gate G7, at the smallest honest size. Every grounding measurement in `g32`–`g35`
ran on a symbol stream this project generated, where a modality is an integer and
two integers either match or do not — so the hard half never arose.

Here a concept is one word and **hundreds of different pictures**, none identical.

`GOALS.md` §1.2b insists the two halves are separate problems and must not be
budgeted as one, so this reports them apart:

    quantiser purity   agreement WITHIN a modality: do pictures of one digit
                       land on the same code
    link purity        alignment ACROSS modalities: does a word's recovered
                       class hold image codes of ITS digit
    reach              share of words that reach any image code at all

Chance for both purities is the largest class share, about 0.11 — reported so no
row is read against zero.

Predictions: `experiments/sweeps/g36-01-a-picture-and-a-word.txt`
"""

from __future__ import annotations

import pathlib
import sys
import time
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments import harness  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes)
from openplexus.grouping import cluster  # noqa: E402
from openplexus.tasks import mnist  # noqa: E402

DATA = ROOT / "data" / "mnist"
IMAGES = 4000
CODES = (20, 50, 100)
NOISE = 2
DISTRACTORS = 1
SEEDS = (0, 1, 2)
ARM = "conditional"


def _quantise(digits: mnist.Digits, codes: int, seed: int) -> list[int]:
    """Pixels to a discrete code, by spherical k-means over unit rows.

    **The quantiser is BORROWED, not invented** — `grouping.cluster` already does
    this and `DECISIONS.md` §1 records John's ruling that a borrowed feature
    space is acceptable and possibly preferred. Raw normalised pixels are a
    deliberately weak feature space: the point is to measure the linking against
    a quantiser whose quality is known, not to build a good one.
    """
    flat = np.frombuffer(b"".join(digits.images), dtype=np.uint8)
    vectors = flat.reshape(len(digits), digits.pixels).astype(np.float64)
    norms = np.linalg.norm(vectors, axis=1, keepdims=True)
    norms[norms == 0.0] = 1.0
    groups = cluster(vectors / norms, k=codes, seed=seed)

    assigned = [-1] * len(digits)
    for code, members in enumerate(groups):
        for row in members:
            assigned[row] = code
    return assigned


def _purity(assigned: list[int], labels: list[int]) -> tuple[float, dict[int, int]]:
    """Share of images sitting in a code whose MAJORITY digit is their own."""
    holders: dict[int, Counter] = {}
    for code, label in zip(assigned, labels):
        if code < 0:
            continue
        holders.setdefault(code, Counter())[label] += 1
    majority = {code: counts.most_common(1)[0][0]
                for code, counts in holders.items()}
    agreed = sum(counts[majority[code]] for code, counts in holders.items())
    total = sum(sum(counts.values()) for counts in holders.values())
    return (agreed / total if total else 0.0), majority


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    if not (DATA / "train-images-idx3-ubyte.gz").exists():
        raise SystemExit(f"no data in {DATA}. Run: python tools/fetch_mnist.py")

    digits = mnist.read(DATA, limit=IMAGES)
    chance = max(Counter(digits.labels).values()) / len(digits)
    print(f"g36-01  {len(digits)} images, {digits.pixels} pixels, "
          f"{len(mnist.WORDS)} words, noise {NOISE}, distractors {DISTRACTORS}")
    print(f"        chance for both purities is the largest class share, "
          f"{chance:.4f}\n")

    header = (f"{'codes':>7}{'per code':>10}{'quantiser':>11}{'link':>8}"
              f"{'grounded':>10}{'reach':>8}{'classes':>9}")
    print(header)
    print("-" * len(header))

    for codes in CODES:
        quant, links, reaches, sizes, grounded = [], [], [], [], []
        for seed in SEEDS:
            assigned = _quantise(digits, codes, seed)
            purity, majority = _purity(assigned, digits.labels)
            quant.append(purity)

            words = {d: codes + d for d in range(len(mnist.WORDS))}
            index = CoOccurrence()
            rng = np.random.default_rng(seed)
            for row, (code, label) in enumerate(zip(assigned, digits.labels)):
                if code < 0:
                    continue
                present = {code, words[label]}
                # Noise is OTHER WORDS -- things said in the room that are not
                # about the picture. Drawing it from image codes instead would
                # put two pictures in one moment, which is not what a learner
                # looking at one thing sees.
                for other in rng.choice(len(mnist.WORDS), NOISE, replace=False):
                    present.add(words[int(other)])
                for extra in range(DISTRACTORS):
                    present.add(codes + len(mnist.WORDS) + extra)
                index.observe(present)

            recovered = equivalence_classes(index, STATISTICS[ARM], None)
            hit = seen = reached = 0
            for digit, token in words.items():
                found = recovered.get(token, frozenset({token}))
                pictures = [s for s in found if s < codes]
                if pictures:
                    reached += 1
                for picture in pictures:
                    seen += 1
                    if majority.get(picture) == digit:
                        hit += 1
            links.append(hit / seen if seen else 0.0)

            # PER IMAGE, so it is comparable with quantiser purity. `link` is
            # per CODE and the two have different denominators -- a code that is
            # 60% threes counts as one correct code while its 40% wrong images
            # drag the quantiser figure down. Comparing them directly would be
            # g35-02's floor confound in a new costume.
            wanted = {digit: recovered.get(token, frozenset({token}))
                      for digit, token in words.items()}
            correct = sum(
                1 for code, label in zip(assigned, digits.labels)
                if code >= 0 and code in wanted[label]
                and majority.get(code) == label)
            grounded.append(correct / len(digits))
            reaches.append(reached / len(words))
            sizes.append(sum(len(v) for v in recovered.values())
                         / max(len(recovered), 1))

        mean = lambda v: sum(v) / len(v)          # noqa: E731 - local
        print(f"{codes:>7}{len(digits) / codes:>10.0f}{mean(quant):>11.4f}"
              f"{mean(links):>8.4f}{mean(grounded):>10.4f}"
              f"{mean(reaches):>8.4f}{mean(sizes):>9.2f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
