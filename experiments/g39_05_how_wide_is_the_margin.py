"""g39-05: is `forward` refusing the distractor by a MARGIN or by one position?

`g39-04` reports `distractor` 0.0000 at alpha 1.0 and closes a problem five
sweeps could not. **A clean result deserves suspicion rather than satisfaction**,
and there is a specific way this one could be luck.

The metric asks whether the distractor sits inside a word's top-`want`, and
`want` is about 5. The arithmetic puts a true partner near 1.0 and the distractor
near 0.28 — but if the *weakest* true partner is also near 0.3, the distractor is
sitting at rank 6 with nothing separating it, and any change tips it: more codes
per digit, more distractors, a different quantiser.

**A pass/fail column cannot show that.** This reports the RANK and the SCORE GAP,
which can.

## The second axis, and it is the one a deployment would hit

`g32-01`'s falsifier uses ONE ever-present distractor. A real stream has many —
the hum, the lamp, the word *the*. If the refusal degrades as they accumulate,
the result holds only for the toy condition it was measured in.

## What this does NOT duplicate, and what was searched

Searched by capability — margin, rank, gap, robustness — across `experiments/`.
`g39-04` reports the pass/fail verdict; nothing reports the distance to it.
Stream construction is reproduced from that run so the numbers are comparable.

Record: `experiments/sweeps/g39-05-how-wide-is-the-margin.txt`
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
    NOISE, _spectra)
from openplexus.grounding import STATISTICS, CoOccurrence  # noqa: E402
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
PASSES = 8
DISTRACTORS = (1, 2, 4, 8)
STATISTIC = STATISTICS["conditional"]


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

    words = len(mnist.WORDS)
    print(f"g39-05  forward at alpha 1.0, {CODES} codes, "
          f"{PASSES * len(heard)} occasions, seeds {SEEDS}")
    print(f"        rank is where the BEST-PLACED distractor sits in a word's "
          f"full ranking\n")
    header = (f"{'distractors':>12}{'rank':>7}{'want':>6}{'weakest true':>14}"
              f"{'best distr':>12}{'margin':>9}{'admitted':>10}")
    print(header)
    print("-" * len(header))

    for count in DISTRACTORS:
        cells = len(SEEDS) * words
        ranks = wants = weakest = best = admitted = 0.0
        for seed in SEEDS:
            image_code = harness.quantise(pixels, CODES, seed)
            audio_code = harness.quantise(spectra, CODES, seed)
            _, image_major = harness.purity(image_code, digits.labels)
            rng = np.random.default_rng(seed)
            word = {d: 2 * CODES + d for d in range(words)}
            spare = 2 * CODES + words
            used: Counter = Counter()

            index = CoOccurrence()
            for _ in range(PASSES):
                for audio_row, digit in enumerate(heard):
                    here = pool[digit]
                    image_row = here[used[digit] % len(here)]
                    used[digit] += 1
                    picture, sound = image_code[image_row], audio_code[audio_row]
                    if picture < 0 or sound < 0:
                        continue
                    present = {word[digit], picture, CODES + sound}
                    for other in rng.choice(words, NOISE, replace=False):
                        present.add(word[int(other)])
                    for extra in range(count):
                        present.add(spare + extra)
                    index.observe(present)

            for digit in range(words):
                token = 2 * CODES + digit
                scored = sorted(((STATISTIC(index, token, other), other)
                                 for other in index.partners(token)),
                                key=lambda pair: (-pair[0], pair[1]))
                order = [other for _, other in scored]
                by_id = {other: score for score, other in scored}
                true = [c for c, d in image_major.items() if d == digit]
                spares = [spare + extra for extra in range(count)]
                placed = min((order.index(x) + 1 for x in spares
                              if x in order), default=len(order) + 1)
                ranks += placed / cells
                wants += len(true) / cells
                weakest += (min(by_id.get(c, 0.0) for c in true)
                            if true else 0.0) / cells
                best += max(by_id.get(x, 0.0) for x in spares) / cells
                admitted += (1 if placed <= len(true) else 0) / cells

        print(f"{count:>12}{ranks:>7.1f}{wants:>6.1f}{weakest:>14.4f}"
              f"{best:>12.4f}{weakest - best:>9.4f}{admitted:>10.4f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
