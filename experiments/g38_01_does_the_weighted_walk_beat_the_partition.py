"""g38-01: does bounding the SEARCH beat bounding the REPRESENTATION?

`g36-05` measured the failure this is aimed at. When a picture and a sound share
every occasion they evict the word from each other's bounded neighbour list —
the word survives for **0.0200** of image codes — and `g36-04` measured the
end-to-end consequence at **0.6667** link purity against 0.8476 when the senses
alternate.

**The word is not gone. It is at rank 6.70.** A bound that keeps two partners
cannot see it; a search that may look past two can.

`grounding.reach` is the alternative: every edge stays in the table and `beam`
and `depth` limit how far one question travels. This asks whether that is
actually better, on the cell where the difference should be largest.

## The combiner is the real axis, and the doubt is registered

`strength` turns two directional scores into one edge weight, and **which
combiner is right is unmeasured**. The tension is specific:

    min   the weaker direction. On a HUB edge that is the small one -- a word's
          edge to an image code is near 1.0 from the word's side and small from
          the code's -- so `min` reproduces the eviction the bound caused
    max   keeps the lopsided hub edge. And keeps an ever-present distractor,
          whose backward direction is likewise 1.0

So the two failure modes sit at opposite ends of one axis, which is the same
shape `damped` had. There it had no interior. **This asks whether this one
does**, and `distractor` is measured in the same run so a win on one cannot hide
a loss on the other.

## The tripwire that is LOST, and its replacement

A partition has mean class size as a collapse alarm. **A ranking cannot
collapse**, which sounds like an improvement and is a hazard: a recall-shaped
metric will read well here for reasons having nothing to do with the mechanism
working. So a `shuffled` arm ranks by a permuted score, and every number is read
against it rather than against zero.

## What this does NOT duplicate, and what was searched

Searched by capability — walk, traversal, ranked retrieval, beam — across
`openplexus/`, `experiments/` and `tools/`.

- **`openplexus/search.py`** walks a *store* by keyed retrieval, committing to a
  token per step. There is no store here; the graph is the counts themselves.
- **`experiments/g36_04_...`** builds the streams and is IMPORTED rather than
  restated, so the arms cannot drift from the run being improved on.
- **`grounding.equivalence_classes`** is the incumbent and is an ARM here, not a
  thing replaced.

Predictions: `experiments/sweeps/g38-01-does-the-weighted-walk-beat-the-partition.txt`
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
    _spectra, _stream)
from openplexus.grounding import (STATISTICS, equivalence_classes,  # noqa: E402
                                  reach)
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
BEAM, DEPTH = 8, 2
ARMS = ("together", "alternating")
METHODS = ("classes", "min", "geometric", "mean", "max", "shuffled")
STATISTIC = STATISTICS["conditional"]


def _ranked(index, word: int, method: str, rng) -> list[int]:
    """Every surface this word reaches, strongest first.

    `classes` is the incumbent, which returns a SET rather than a ranking. It is
    ordered arbitrarily-but-deterministically so the same `@k` scorer can read
    both — **and that is a handicap the incumbent did not sign up for**, so
    `link@k` is reported beside a set-based `link` that does not depend on order.
    """
    if method == "classes":
        found = equivalence_classes(index, STATISTIC, None).get(
            word, frozenset({word}))
        return sorted(found - {word})
    if method == "shuffled":
        candidates = sorted(index.surfaces())
        candidates.remove(word) if word in candidates else None
        rng.shuffle(candidates)
        return candidates
    found = reach(index, STATISTIC, word, beam=BEAM, depth=DEPTH,
                  combine=method)
    return [s for _, s in sorted(((-v, k) for k, v in found.items()))]


def _score(index, image_major, audio_major, method: str, rng) -> dict:
    spare = 2 * CODES + len(mnist.WORDS)
    at_k = [0, 0]
    whole = [0, 0]
    admitted = 0
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        order = _ranked(index, word, method, rng)
        pictures = [s for s in order if s < CODES]
        # PRECISION AT K, with k the number of image codes whose majority IS
        # this digit -- so the bar is the same for every arm and does not
        # reward a longer list.
        want = sum(1 for c, d in image_major.items() if d == digit)
        for surface in pictures[:want]:
            at_k[1] += 1
            at_k[0] += image_major.get(surface) == digit
        for surface in pictures:
            whole[1] += 1
            whole[0] += image_major.get(surface) == digit
        admitted += spare in order[:want] if want else 0
    return {
        "link@k": at_k[0] / at_k[1] if at_k[1] else 0.0,
        "link": whole[0] / whole[1] if whole[1] else 0.0,
        "reached": at_k[1] / max(len(mnist.WORDS), 1),
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
    used: Counter = Counter()
    pairs = []
    for audio_row, digit in enumerate(heard):
        rows = pool[digit]
        pairs.append((rows[used[digit] % len(rows)], audio_row, digit))
        used[digit] += 1

    chance = max(Counter(heard).values()) / len(heard)
    print(f"g38-01  {CODES} codes, beam {BEAM}, depth {DEPTH}, seeds {SEEDS}")
    print(f"        chance for both link columns is {chance:.4f}\n")

    header = (f"{'arm':<13}{'method':<11}{'link@k':>9}{'link':>8}"
              f"{'reached':>9}{'distractor':>12}")
    print(header)
    print("-" * len(header))

    for arm in ARMS:
        for method in METHODS:
            totals: dict[str, float] = {}
            for seed in SEEDS:
                image_code = harness.quantise(pixels, CODES, seed)
                audio_code = harness.quantise(spectra, CODES, seed)
                _, image_major = harness.purity(image_code, digits.labels)
                _, audio_major = harness.purity(audio_code, heard)
                index = _stream(arm, pairs, CODES, image_code, audio_code,
                                np.random.default_rng(seed))
                got = _score(index, image_major, audio_major, method,
                             np.random.default_rng(seed + 9000))
                for key, value in got.items():
                    totals[key] = totals.get(key, 0.0) + value / len(SEEDS)
            print(f"{arm:<13}{method:<11}{totals['link@k']:>9.4f}"
                  f"{totals['link']:>8.4f}{totals['reached']:>9.2f}"
                  f"{totals['distractor']:>12.4f}")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
