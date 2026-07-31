"""g38-03: how deep may a query walk before it costs too much?

**John's question, 2026-07-31**, and the one axis `g38-01` pinned without
sweeping. That run fixed `beam` at 8 and `depth` at 2 so a word could reach an
image code directly or through an audio code, and found `mean` beating the
incumbent partition by **0.29** on the cell where the bound had failed. Whether
that win grows or shrinks with a larger budget was not measured.

**The budget is on SEARCH, not on storage**, which is what makes the question
worth asking at all: raising `beam` costs time and changes no stored value, so
the trade is latency against accuracy rather than accuracy against capacity.

## Cost is COUNTED, not estimated

Every `strength` evaluation is one candidate scored, and under the sharded design
that is **one remote read** — `owner(x)` holds `count(x,y)` and `count(x)` and
needs `count(y)` from one peer. So the count of scorings is the message count,
and it is measured here rather than reasoned about.

`g33-03` measured 439 messages per walk at 192 surfaces on the BOUNDED version.
This is the unbounded figure, which was named as unknown in `g38-01`'s own
"what this does not settle".

## What this does NOT duplicate, and what was searched

Searched by capability — beam, depth, budget, traversal cost, message count —
across `openplexus/`, `experiments/` and `tools/`.

- **`experiments/g33_03_...`** counted peer messages for the bounded walk. Same
  question, different mechanism, and its number is the comparison.
- **`experiments/g38_01_...`** is the run this extends; its streams and scorer
  are IMPORTED rather than restated so the arms cannot drift.

Predictions: `experiments/sweeps/g38-03-how-deep-may-a-query-walk.txt`
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
from openplexus import grounding  # noqa: E402
from openplexus.grounding import STATISTICS, equivalence_classes  # noqa: E402
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
BEAMS = (2, 4, 8, 16, 32)
DEPTHS = (1, 2, 3)
ARM = "together"
COMBINE = "mean"
STATISTIC = STATISTICS["conditional"]


class _Counted:
    """Wraps a statistic and counts how many times it is asked.

    **One scoring is one remote read**, so this is the message count. Wrapping
    rather than instrumenting `reach` keeps the mechanism free of a counter that
    only an experiment needs — and means the count cannot silently drift from
    what the mechanism actually does.
    """

    def __init__(self, statistic) -> None:
        self._statistic = statistic
        self.calls = 0

    def __call__(self, index, surface, other) -> float:
        self.calls += 1
        return self._statistic(index, surface, other)


def _score(index, image_major, beam: int, depth: int) -> dict:
    """Link precision, HOW MANY it was computed over, admission, and messages.

    **`reached` is not decoration and its absence broke the first version of
    this run.** A `link@k` of 1.0000 over two image codes and one over five are
    different answers, and `g38-01`'s own record says so in as many words about
    a different column. Dropping the companion in the follow-up is exactly the
    failure that record warned about.
    """
    spare = 2 * CODES + len(mnist.WORDS)
    counted = _Counted(STATISTIC)
    hits = [0, 0]
    admitted = wanted = 0
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        found = grounding.reach(index, counted, word, beam=beam, depth=depth,
                                combine=COMBINE)
        order = [s for _, s in sorted(((-v, k) for k, v in found.items()))]
        want = sum(1 for _, d in image_major.items() if d == digit)
        wanted += want
        for surface in [s for s in order if s < CODES][:want]:
            hits[1] += 1
            hits[0] += image_major.get(surface) == digit
        admitted += spare in order[:want] if want else 0
    # `strength` asks the statistic TWICE per candidate and both directions
    # reuse the same three counts, so the messages are half the calls.
    return {
        "link": hits[0] / hits[1] if hits[1] else 0.0,
        # Share of the codes it SHOULD have found that it had a chance to score.
        # A precision figure over a tenth of them is not comparable with one
        # over all of them.
        "covered": hits[1] / wanted if wanted else 0.0,
        "distractor": admitted / len(mnist.WORDS),
        "messages": counted.calls // 2 // len(mnist.WORDS),
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

    print(f"g38-03  arm {ARM}, combiner {COMBINE}, {CODES} codes, "
          f"seeds {SEEDS}")
    print(f"        messages are per WORD, and one scored candidate is one "
          f"remote read\n")
    header = (f"{'depth':>6}{'beam':>6}{'link@k':>9}{'covered':>9}"
              f"{'distractor':>12}{'messages':>10}")
    print(header)
    print("-" * len(header))

    indexes = []
    for seed in SEEDS:
        image_code = harness.quantise(pixels, CODES, seed)
        audio_code = harness.quantise(spectra, CODES, seed)
        _, image_major = harness.purity(image_code, digits.labels)
        indexes.append((_stream(ARM, pairs, CODES, image_code, audio_code,
                                np.random.default_rng(seed)), image_major))

    for depth in DEPTHS:
        for beam in BEAMS:
            totals: dict[str, float] = {}
            for index, image_major in indexes:
                for key, value in _score(index, image_major, beam,
                                         depth).items():
                    totals[key] = totals.get(key, 0.0) + value / len(SEEDS)
            print(f"{depth:>6}{beam:>6}{totals['link']:>9.4f}"
                  f"{totals['covered']:>9.4f}{totals['distractor']:>12.4f}"
                  f"{totals['messages']:>10.0f}")
        print()

    baseline: dict[str, float] = {}
    for index, image_major in indexes:
        for key, value in _reference(index, image_major).items():
            baseline[key] = baseline.get(key, 0.0) + value / len(SEEDS)
    print(f"the incumbent partition, same cell and same seeds: "
          f"link@k {baseline['link']:.4f}, covered {baseline['covered']:.4f}")
    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


def _reference(index, image_major) -> dict:
    """`equivalence_classes` on the same cell, so the table has its baseline.

    **Averaged over the same seeds as everything else**, which the first version
    was not — it scored seed 0 only and printed 1.0000 beside three-seed means.
    """
    hits = [0, 0]
    wanted = 0
    classes = equivalence_classes(index, STATISTIC, None)
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        found = sorted(classes.get(word, frozenset({word})) - {word})
        want = sum(1 for _, d in image_major.items() if d == digit)
        wanted += want
        for surface in [s for s in found if s < CODES][:want]:
            hits[1] += 1
            hits[0] += image_major.get(surface) == digit
    return {
        "link": hits[0] / hits[1] if hits[1] else 0.0,
        "covered": hits[1] / wanted if wanted else 0.0,
    }


if __name__ == "__main__":
    main()
