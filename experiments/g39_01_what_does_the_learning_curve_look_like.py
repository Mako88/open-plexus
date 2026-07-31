"""g39-01: is the grounding result a PLATEAU or a snapshot of a moving curve?

**Every number in the grounding line is scored once, at the end of the stream.**
`GOALS.md` §3 is explicit that this is the wrong shape:

> Evaluation should be PREQUENTIAL — predict the next item, then learn from it,
> and score the predictions made along the way. **A train/test split measures a
> system that stops, which is the thing C4 forbids.**

and records that prequential evaluation *"is still the exception rather than the
norm"*. John's instruction, 2026-07-31, is the same point from the product side:
there is no training phase and no operating phase, there is one stream and the
thing learns from it forever.

**The mechanism is ALREADY continuous** — `CoOccurrence.observe` is incremental
and nothing about it stops. What is batch-shaped is the MEASUREMENT, and that is
what this changes: score at intervals through one pass, so every result becomes a
curve rather than a point.

## What that buys, beyond being the right shape

**It says whether the numbers are a plateau.** `g38-03` reports 0.9665 link
precision at full coverage. Nothing in the record establishes whether that is
where the mechanism settles or where it happened to be when the stream ran out —
and those have very different consequences for everything measured beside it.

`g11-05` is the calibration: five points of a scaling sweep, fifteen jobs, every
one of them past saturation, because nobody had probed where the arm stopped
moving. **A flat exponent was guaranteed by the grid.** The same defect at the
other end — a curve still climbing when the stream ends — would make every
grounding figure a lower bound quoted as a value.

## Scored WITHOUT a split, deliberately

There is no held-out set. The score at occasion `n` reads the table as it stands
after `n` occasions, and the ground truth is the quantiser's own majority label,
which is not part of the mechanism at any point. **Nothing is withheld and
nothing is replayed**, which is what makes this prequential rather than a
train/test curve wearing a different name.

## What this does NOT duplicate, and what was searched

Searched by capability — prequential, online, learning curve, checkpoint,
incremental scoring — across `openplexus/`, `experiments/` and `tools/`.

- **`openplexus/models/local_memory.py`** scores prequentially for the TEXT
  objective (decision 117). Different task, different mechanism, no shared code.
- **`experiments/g36_04_...` and `g38_01_...`** build the streams and the scorer
  and are IMPORTED rather than restated.
- **`grounding.reach`** is the query and is unchanged; only when it is called
  differs.

Predictions: `experiments/sweeps/g39-01-what-does-the-learning-curve-look-like.txt`
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
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes, reach)
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
BEAM, DEPTH, COMBINE = 16, 1, "mean"
STATISTIC = STATISTICS["conditional"]
ARMS = ("together", "alternating")

#: Where to stop and score. Geometric rather than even, because a learning curve
#: is expected to move most at the start and a linear grid spends its resolution
#: where nothing happens -- `g11-05`'s failure, from the other end.
CHECKPOINTS = (25, 50, 100, 200, 400, 800, 1500, 3000)


def _score(index: CoOccurrence, image_major: dict[int, int]) -> dict:
    """Link precision and coverage from the table AS IT STANDS."""
    spare = 2 * CODES + len(mnist.WORDS)
    hits = [0, 0]
    wanted = admitted = 0
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        found = reach(index, STATISTIC, word, beam=BEAM, depth=DEPTH,
                      combine=COMBINE)
        order = [s for _, s in sorted(((-v, k) for k, v in found.items()))]
        want = sum(1 for _, d in image_major.items() if d == digit)
        wanted += want
        for surface in [s for s in order if s < CODES][:want]:
            hits[1] += 1
            hits[0] += image_major.get(surface) == digit
        admitted += spare in order[:want] if want else 0
    classes = equivalence_classes(index, STATISTIC, None)
    partition = [0, 0]
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        for surface in classes.get(word, frozenset({word})):
            if surface < CODES:
                partition[1] += 1
                partition[0] += image_major.get(surface) == digit
    return {
        "link": hits[0] / hits[1] if hits[1] else 0.0,
        "covered": hits[1] / wanted if wanted else 0.0,
        "distractor": admitted / len(mnist.WORDS),
        "partition": partition[0] / partition[1] if partition[1] else 0.0,
        "seen": len(index.surfaces()),
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

    print(f"g39-01  {CODES} codes, beam {BEAM}, depth {DEPTH}, "
          f"combiner {COMBINE}, seeds {SEEDS}")
    print(f"        NO SPLIT. Each row reads the table as it stands after that "
          f"many occasions.\n")

    for arm in ARMS:
        header = (f"{'occasions':>10}{'per digit':>11}{'link':>8}"
                  f"{'covered':>9}{'partition':>11}{'distractor':>12}"
                  f"{'surfaces':>10}")
        print(f"=== {arm} ===")
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

            # ONE PASS, scored as it goes. The stream is built here rather than
            # by `_stream` because that function observes everything before
            # returning, which is precisely the batch shape this run exists to
            # stop doing.
            index = CoOccurrence()
            for position, (image_row, audio_row, digit) in enumerate(pairs, 1):
                picture, sound = image_code[image_row], audio_code[audio_row]
                if picture >= 0 and sound >= 0:
                    present = {word[digit]}
                    if arm == "together":
                        present.update({picture, CODES + sound})
                    else:
                        present.add(picture if position % 2 else CODES + sound)
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

        for position in CHECKPOINTS:
            got = rows.get(position)
            if not got:
                continue
            print(f"{position:>10}{position / len(mnist.WORDS):>11.0f}"
                  f"{got['link']:>8.4f}{got['covered']:>9.4f}"
                  f"{got['partition']:>11.4f}{got['distractor']:>12.4f}"
                  f"{got['seen']:>10.0f}")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
