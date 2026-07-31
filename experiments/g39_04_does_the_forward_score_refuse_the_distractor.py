"""g39-04: does the FORWARD score keep the link and refuse the distractor?

`g39-03` found 0 of 24 settings doing both — three exponents by four combiners by
two stream lengths — and identified why: the thing that had been refusing the
ever-present distractor was never the statistic, it was the hard cut plus
mutuality, which a ranked walk removes.

**The arithmetic then pointed somewhere the grid did not contain.** For a word
`w`, its own image code `c`, and a distractor `d` present on every occasion:

    conditional(w, c) ~ 1.00       conditional(c, w) ~ 0.07
    conditional(w, d) ~ 0.28       conditional(d, w) ~ 1.00

The **forward** view separates them cleanly. Every symmetrising rule mixes in the
backward direction, where the distractor's 1.00 is genuinely true, and inverts
the order — `min` 0.07 against 0.28, `mean` 0.53 against 0.64, `max` tied at
1.00. **So symmetrising is what admitted it**, and `forward` is not a new
mechanism but the absence of one.

## What a probe already showed, and what this adds

A scratch probe measured `forward` at link **0.9800**, coverage **1.0000**,
distractor **0.0000** — the first setting in this line to do both — at one arm,
one exponent, one stream length. **That is one cell and one seed set.**

This asks whether it survives the axes that killed everything else: both arms,
three exponents, both stream lengths. **P1 is therefore weak and marked so; P2
through P5 are genuine commitments**, because nothing has measured `forward`
anywhere but that single cell.

## The obvious objection, stated before the run

`forward` discards half of every edge, so it drops the mutuality-like property
that `strength` was introduced for. **A one-sided rule is exactly what
`grounding.py`'s own header argues against**: *"a one-sided rule lets a hub —
which is precisely what a distractor present every time is — attach itself to
every surface in the world."*

That argument is about the DISTRACTOR'S list, not the word's. Here the query
starts at the word and reads the word's own view, so the distractor never gets to
propose itself. **Whether that holds once the walk goes deeper than one hop is
exactly what P4 tests**, and it is the place this is most likely to fail.

## What this does NOT duplicate, and what was searched

Searched by capability — forward, directional, asymmetric, one-sided — across
`openplexus/` and `experiments/`.

- **`grounding.local_conditional`** is the other one-sided statistic and is the
  opposite direction: `c_xy / c_x`, which ranks a distractor FIRST by
  construction. `forward` is `c_xy / c_y` read from the query's side.
- **`experiments/g39_03_...`** is the run this extends; its stream construction
  is reproduced so the 3,000 column stays comparable.

Predictions: `experiments/sweeps/g39-04-does-the-forward-score-refuse-the-distractor.txt`
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
BEAM = 16
CHECKPOINTS = (3000, 24000)
ALPHAS = (0.5, 0.75, 1.0)
DEPTHS = (1, 2)
ARMS = ("together", "alternating")


def _score(index, image_major, statistic, depth: int) -> dict:
    spare = 2 * CODES + len(mnist.WORDS)
    hits = [0, 0]
    wanted = admitted = 0
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        found = reach(index, statistic, word, beam=BEAM, depth=depth,
                      combine="forward")
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

    print(f"g39-04  combiner FORWARD, {CODES} codes, beam {BEAM}, "
          f"seeds {SEEDS}\n")

    rows: dict[tuple, dict[str, float]] = {}
    for arm in ARMS:
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
                        present = {word[digit]}
                        if arm == "together":
                            present.update({picture, CODES + sound})
                        else:
                            present.add(picture if position % 2
                                        else CODES + sound)
                        for other in rng.choice(len(mnist.WORDS), NOISE,
                                                replace=False):
                            present.add(word[int(other)])
                        for extra in range(DISTRACTORS):
                            present.add(spare + extra)
                        index.observe(present)
                    if position in CHECKPOINTS:
                        for alpha in ALPHAS:
                            statistic = damped(alpha)
                            for depth in DEPTHS:
                                got = _score(index, image_major, statistic,
                                             depth)
                                into = rows.setdefault(
                                    (arm, position, alpha, depth), {})
                                for key, value in got.items():
                                    into[key] = (into.get(key, 0.0)
                                                 + value / len(SEEDS))
                    if position >= max(CHECKPOINTS):
                        break

    header = (f"{'arm':<13}{'occasions':>10}{'alpha':>7}{'depth':>6}"
              f"{'link':>8}{'covered':>9}{'distractor':>12}{'BOTH?':>7}")
    print(header)
    print("-" * len(header))
    solved = 0
    for arm in ARMS:
        for position in CHECKPOINTS:
            for alpha in ALPHAS:
                for depth in DEPTHS:
                    got = rows.get((arm, position, alpha, depth))
                    if not got:
                        continue
                    both = (got["link"] >= 0.80 and got["covered"] >= 0.80
                            and got["distractor"] <= 0.05)
                    solved += both
                    print(f"{arm:<13}{position:>10}{alpha:>7.2f}{depth:>6}"
                          f"{got['link']:>8.4f}{got['covered']:>9.4f}"
                          f"{got['distractor']:>12.4f}"
                          f"{'YES' if both else '-':>7}")
        print()

    print(f"settings meeting BOTH: {solved} of {len(rows)}")
    print(f"  (g39-03 measured 0 of 24 for every symmetrising combiner)")
    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
