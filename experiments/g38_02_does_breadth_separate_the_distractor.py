"""g38-02: does BREADTH separate the word from the distractor? It does not.

**A proposal, checked before it was built.** `g38-01` found that every scalar
combiner fails at one end or the other — `min` protects nothing, `max`
discriminates nothing, `mean` wins the link and admits the ever-present
distractor for every word. Three dials of that shape now, so the next thing
tried should not be a fourth.

The proposal was that the distractor is distinguishable **without consulting any
edge**, from a surface's own row and therefore at no network cost:

> A word is common but FOCUSED — its co-occurrence mass sits on its own
> concept's surfaces. A distractor is common and INDISCRIMINATE — its mass is
> spread evenly over everything. Frequency cannot tell those apart because both
> are common. A concentration measure can.

`CLAUDE.md`: *"before proposing a mechanism to repair a measured failure, check
the failure is repairable at all"*, and *"the tell is grammatical: `what would
fix it is ...`, written before anything was run."* This is that check.

## The quantity

    effective partners = exp(entropy of the partner counts)

How many partners a surface **behaves as though** it has. One whose mass is
spread evenly over a hundred partners scores about a hundred; one whose mass sits
on three scores about three, however many it has touched at least once. Raw
partner COUNT cannot express that distinction and is reported beside it.

Computed from `owner(surface)`'s own row alone — **no remote read** — which is
what would have made it attractive if it had worked.

## What this does NOT duplicate, and what was searched

Searched by capability — entropy, breadth, spread, concentration, indiscriminate
— across `openplexus/`, `experiments/` and `tools/`. Nothing measures a
surface's partner distribution.

- **`grounding.CoOccurrence.partners`** returns the list; nothing scores its
  shape.
- **`grounding.damped`** discounts by a candidate's frequency, which is the
  quantity this was proposed to go beyond.
- **`experiments/g36_04_...`** builds the streams and is IMPORTED, so the
  distributions measured are the ones the failure was measured on.

Record: `experiments/sweeps/g38-02-does-breadth-separate-the-distractor.txt`
"""

from __future__ import annotations

import math
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
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
ARMS = ("together", "alternating")


def effective_partners(index, surface: int) -> float:
    """`exp(entropy)` of a surface's partner counts. Local, no remote read."""
    counts = [index.together(surface, other)
              for other in index.partners(surface)]
    counts = [c for c in counts if c > 0]
    total = sum(counts)
    if total <= 0:
        return 0.0
    entropy = -sum((c / total) * math.log(c / total) for c in counts)
    return math.exp(entropy)


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

    spare = 2 * CODES + len(mnist.WORDS)
    print(f"g38-02  {CODES} codes, seeds {SEEDS}, {len(pairs)} occasions\n")
    header = (f"{'arm':<13}{'kind':<13}{'n':>4}{'frequency':>11}"
              f"{'partners':>10}{'effective':>11}")
    print(header)
    print("-" * len(header))

    for arm in ARMS:
        summary = {}
        for seed in SEEDS:
            image_code = harness.quantise(pixels, CODES, seed)
            audio_code = harness.quantise(spectra, CODES, seed)
            index = _stream(arm, pairs, CODES, image_code, audio_code,
                            np.random.default_rng(seed))
            kinds = {
                "image code": range(CODES),
                "audio code": range(CODES, 2 * CODES),
                "word": range(2 * CODES, 2 * CODES + len(mnist.WORDS)),
                "DISTRACTOR": [spare],
            }
            for name, span in kinds.items():
                rows = [s for s in span if index.seen(s)]
                if not rows:
                    continue
                got = summary.setdefault(name, [0, 0.0, 0.0, 0.0])
                got[0] = len(rows)
                got[1] += sum(index.seen(s) for s in rows) / len(rows) / len(SEEDS)
                got[2] += (sum(len(index.partners(s)) for s in rows)
                           / len(rows) / len(SEEDS))
                got[3] += (sum(effective_partners(index, s) for s in rows)
                           / len(rows) / len(SEEDS))
        for name, (n, freq, partners, effective) in summary.items():
            print(f"{arm:<13}{name:<13}{n:>4}{freq:>11.1f}"
                  f"{partners:>10.1f}{effective:>11.1f}")
        word = summary["word"]
        distractor = summary["DISTRACTOR"]
        print(f"{'':<13}{'SEPARATION':<13}{'':>4}"
              f"{distractor[1] / word[1]:>10.2f}x"
              f"{distractor[2] / word[2]:>9.2f}x"
              f"{distractor[3] / word[3]:>10.2f}x")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
