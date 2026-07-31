"""g36-05: WHY does putting two senses in one occasion lose to alternating them?

`g36-04` found that a picture and a sound sharing every occasion links WORSE than
a picture and a sound that never share one -- 0.6667 against 0.9850 at 50 codes.
That is a result. **The reason for it was a guess**, and `CLAUDE.md`'s standard
is that a diagnosis is a claim about behaviour and needs the same evidence as any
other, because a wrong diagnosis motivates building the wrong thing.

THE CLAIM UNDER TEST: when a picture and a sound arrive together every time, they
become each other's strongest partner, and the derived bound -- which keeps only
the partners above the biggest score drop -- has no room left for the WORD.
Mutuality then kills the word-to-picture edge, and the class survives only
transitively through the sound.

THE QUANTITY THAT SETTLES IT is the word's fate inside an image code's OWN
neighbour list, which is not a downstream proxy for the claim -- it is the claim.

    word in list    share of image codes whose own word survives the bound
    mean rank       where that word sits in the full ranking, 1 being best
    mean bound      how many partners the derived cliff keeps
    audio in list   audio codes kept, per image code

**A refuting outcome is available and is not contrived**: if the word's rank and
survival are unchanged between `together` and `alternating`, the eviction account
is wrong and the loss lives somewhere else -- most likely in the walk rather than
in the bound.

Record: `experiments/sweeps/g36-05-what-the-bound-evicts.txt`
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
from openplexus.grounding import STATISTICS, cliff, neighbours  # noqa: E402
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
ARMS = ("image+word", "together", "alternating")


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
    utterances = [spoken.read(path)
                  for path in spoken.available(FSDD_DATA)]
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

    statistic = STATISTICS["conditional"]
    print(f"g36-05  {CODES} codes, seeds {SEEDS}, {len(pairs)} occasions. "
          f"The bound is DERIVED, so it is not one number applied to every "
          f"surface\n")
    header = (f"{'arm':<16}{'word in list':>14}{'mean rank':>11}"
              f"{'mean bound':>12}{'audio in list':>15}")
    print(header)
    print("-" * len(header))

    for arm in ARMS:
        totals = [0.0, 0.0, 0.0, 0.0]
        for seed in SEEDS:
            image_code = harness.quantise(pixels, CODES, seed)
            audio_code = harness.quantise(spectra, CODES, seed)
            _, image_major = harness.purity(image_code, digits.labels)
            index = _stream(arm, pairs, CODES, image_code, audio_code,
                            np.random.default_rng(seed))

            present = ranks = bound = audio = counted = 0
            for picture, digit in image_major.items():
                candidates = index.partners(picture)
                if not candidates:
                    continue
                scored = sorted(
                    ((statistic(index, picture, other), other)
                     for other in candidates),
                    key=lambda pair: (-pair[0], pair[1]))
                counted += 1
                bound += cliff([score for score, _ in scored])
                want = 2 * CODES + digit
                kept = neighbours(index, picture, statistic, None)
                present += want in kept
                order = [other for _, other in scored]
                # Rank in the FULL ranking, so a word that fell out of the
                # bound is still located rather than scored as missing.
                ranks += order.index(want) + 1 if want in order else len(order)
                audio += sum(1 for other in kept if CODES <= other < 2 * CODES)
            for slot, value in enumerate((present, ranks, bound, audio)):
                totals[slot] += value / max(counted, 1)

        share, rank, kept_n, audio_n = [t / len(SEEDS) for t in totals]
        print(f"{arm:<16}{share:>14.4f}{rank:>11.2f}"
              f"{kept_n:>12.2f}{audio_n:>15.2f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
