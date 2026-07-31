"""g36-06: is there a damping exponent that keeps the word AND kills the distractor?

**John's question, 2026-07-31**: *"I suspect the reason the words are
disappearing from the connection is the difference in volume — LOTS of images and
LOTS of audio, but only 10 words. I'm wondering if we need to do some kind of
scaling so connections matter more than frequency of a thing."*

The diagnosis is measured and correct. In `g36-04`'s three-modality stream a word
is present **845.4** times on average against **60.0** for any single image or
audio code, because ten words carry the occasions that fifty codes split.
`conditional` divides by the candidate's own count, so a word takes a
fourteen-fold handicap for being shared across fewer types.

**THE TENSION THIS EXISTS TO RESOLVE, and it may have no solution.** Two measured
results pull in opposite directions along one axis:

    g32-01   alpha = 1 is what KILLS a distractor present every occasion.
             Raw counting loses 0.3044 of f1 to one; conditional loses 0.0000
    g36-05   alpha = 1 is also what EVICTS the word, which survives the derived
             bound for 0.0200 of image codes against 0.9797 when alternating

So an intermediate exponent either does both or neither, and nothing here assumes
which. `grounding.damped(alpha)` exposes it as one axis instead of a choice
between named statistics; alpha 0, 0.5 and 1 reproduce `count`, `weighted` and
`conditional` exactly, which `tests/test_damped.py` asserts against those
implementations rather than against the formula.

**BOTH failure modes are measured in the same run**, which is the design point --
a sweep that only watched the word would report a low alpha as a triumph while
the distractor walked back in.

    word kept      share of image codes whose OWN word survives the bound.
                   `g36-05`'s quantity, and the thing John's fix must move
    distractor     share of image codes that admit the ever-present distractor.
                   `g32-01`'s falsifier, and the thing it must not break
    link_img       the end-to-end consequence, from `g36-04`
    cross          the cross-sensory bridge, from `g36-04`

Predictions: `experiments/sweeps/g36-06-can-a-softer-denominator-save-the-word.txt`
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
from openplexus.grounding import (CoOccurrence, damped,  # noqa: E402
                                  equivalence_classes, neighbours)
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
ALPHAS = (0.0, 0.25, 0.5, 0.75, 1.0)
ARMS = ("together", "alternating")


def _score(index: CoOccurrence, statistic, image_major,
           audio_major) -> dict[str, float]:
    """Word survival, distractor admission, and the two end-to-end quantities."""
    spare = 2 * CODES + len(mnist.WORDS)
    recovered = equivalence_classes(index, statistic, None)

    kept = admitted = counted = 0
    for picture, digit in image_major.items():
        if not index.partners(picture):
            continue
        counted += 1
        neighbourhood = neighbours(index, picture, statistic, None)
        kept += (2 * CODES + digit) in neighbourhood
        admitted += spare in neighbourhood

    hit = seen = 0
    for digit in range(len(mnist.WORDS)):
        token = 2 * CODES + digit
        for surface in recovered.get(token, frozenset({token})):
            if surface < CODES:
                seen += 1
                hit += image_major.get(surface) == digit

    reached = agreed = 0
    for picture, digit in image_major.items():
        for surface in recovered.get(picture, frozenset({picture})):
            if CODES <= surface < 2 * CODES:
                reached += 1
                agreed += audio_major.get(surface - CODES) == digit

    return {
        "word": kept / max(counted, 1),
        "distractor": admitted / max(counted, 1),
        "link_img": hit / seen if seen else 0.0,
        "cross": agreed / reached if reached else 0.0,
        "crossed": reached / max(len(image_major), 1),
        "classes": sum(len(v) for v in recovered.values()) / max(len(recovered), 1),
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

    print(f"g36-06  {CODES} codes, seeds {SEEDS}, {len(pairs)} occasions")
    print(f"        alpha 0.0 = count, 0.5 = weighted, 1.0 = conditional\n")

    header = (f"{'arm':<13}{'alpha':>7}{'word':>8}{'distractor':>12}"
              f"{'link_img':>10}{'cross':>8}{'crossed':>9}{'classes':>9}")
    print(header)
    print("-" * len(header))

    for arm in ARMS:
        for alpha in ALPHAS:
            totals: dict[str, float] = {}
            for seed in SEEDS:
                image_code = harness.quantise(pixels, CODES, seed)
                audio_code = harness.quantise(spectra, CODES, seed)
                _, image_major = harness.purity(image_code, digits.labels)
                _, audio_major = harness.purity(audio_code, heard)
                index = _stream(arm, pairs, CODES, image_code, audio_code,
                                np.random.default_rng(seed))
                for key, value in _score(index, damped(alpha),
                                         image_major, audio_major).items():
                    totals[key] = totals.get(key, 0.0) + value / len(SEEDS)
            print(f"{arm:<13}{alpha:>7.2f}{totals['word']:>8.4f}"
                  f"{totals['distractor']:>12.4f}{totals['link_img']:>10.4f}"
                  f"{totals['cross']:>8.4f}{totals['crossed']:>9.4f}"
                  f"{totals['classes']:>9.2f}")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
