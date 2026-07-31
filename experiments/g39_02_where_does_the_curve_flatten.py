"""g39-02: where does the grounding curve actually flatten?

`g39-01` found the curve steepest where the data ran out — `link` rising 0.4022
over the last doubling against a 0.05 refutation threshold — which makes every
absolute grounding figure a lower bound. This extends the stream to find where it
settles.

## Repeating the stream would be a NO-OP, and the arithmetic says so

The obvious extension is more passes over the same 3,000 occasions. **It cannot
work.** `conditional(x, y) = c(x,y) / c(y)`; an exact repeat multiplies both by
the number of passes and leaves every ratio identical, so every score, every
ranking and every `reach` result is bit-for-bit unchanged. The curve would be
perfectly flat and would mean nothing.

**So the stream is extended by RE-PAIRING, not by repeating.** FSDD has 3,000
recordings and MNIST supplies 4,000 images; each pass pairs every recording with
a DIFFERENT image of the same digit and redraws the noise. The underlying data is
the same and the co-occurrences are genuinely new, which is the distinction that
makes the extra occasions carry information.

This is also the honest model of the situation `GOALS.md` §3 describes: a learner
does not see new dogs forever, it sees the same dogs in new combinations.

## What is being watched

The same four quantities as `g39-01`, at 3,000 / 6,000 / 12,000 / 24,000, so the
first column reproduces that run exactly and anything else is the extension.

**`distractor` is the one to watch hardest.** It has been 1.0000 at every
checkpoint of every run, across three scalar dials, a search budget, and stream
length to 3,000. Eight times the data is the last cheap axis available.

## What this does NOT duplicate, and what was searched

Searched by capability — passes, repeats, stream length, saturation — across
`experiments/` and `openplexus/`.

- **`experiments/g39_01_...`** is the run being extended; its scorer is IMPORTED
  rather than restated so the first column is a genuine reproduction.
- **`experiments/g11_05_...` and `g11_06_...`** probed saturation on the TEXT
  corpus, which is a different task and a different mechanism.

Predictions: `experiments/sweeps/g39-02-where-does-the-curve-flatten.txt`
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
from experiments.g39_01_what_does_the_learning_curve_look_like import (  # noqa: E402
    CODES, _score)
from openplexus.grounding import CoOccurrence  # noqa: E402
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
SEEDS = (0, 1, 2)
ARM = "together"
CHECKPOINTS = (3000, 6000, 12000, 24000)


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

    print(f"g39-02  arm {ARM}, {CODES} codes, seeds {SEEDS}")
    print(f"        the stream is extended by RE-PAIRING, not repeating: an "
          f"exact repeat\n        multiplies every count and changes no ratio, "
          f"so it would be a no-op\n")
    header = (f"{'occasions':>10}{'per digit':>11}{'link':>8}{'covered':>9}"
              f"{'partition':>11}{'distractor':>12}{'surfaces':>10}")
    print(header)
    print("-" * len(header))

    rows: dict[int, dict[str, float]] = {}
    for seed in SEEDS:
        image_code = harness.quantise(pixels, CODES, seed)
        audio_code = harness.quantise(spectra, CODES, seed)
        _, image_major = harness.purity(image_code, digits.labels)
        rng = np.random.default_rng(seed)
        word = {d: 2 * CODES + d for d in range(len(mnist.WORDS))}
        spare = 2 * CODES + len(mnist.WORDS)
        # Per-digit cursors, advanced across passes, so each pass pairs a
        # recording with a DIFFERENT image rather than the same one again.
        used: Counter = Counter()

        index = CoOccurrence()
        position = 0
        while position < max(CHECKPOINTS):
            for audio_row, digit in enumerate(heard):
                rows_here = pool[digit]
                image_row = rows_here[used[digit] % len(rows_here)]
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
                    got = _score(index, image_major)
                    into = rows.setdefault(position, {})
                    for key, value in got.items():
                        into[key] = into.get(key, 0.0) + value / len(SEEDS)
                if position >= max(CHECKPOINTS):
                    break

    for position in CHECKPOINTS:
        got = rows.get(position)
        if not got:
            continue
        print(f"{position:>10}{position / len(mnist.WORDS):>11.0f}"
              f"{got['link']:>8.4f}{got['covered']:>9.4f}"
              f"{got['partition']:>11.4f}{got['distractor']:>12.4f}"
              f"{got['seen']:>10.0f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
