"""g39-06: does the refusal survive a distractor present ALMOST always?

`g39-05` measured the margin at **0.4464** and found it identical at 1, 2, 4 and
8 distractors — but every one of them was present on **100%** of occasions, which
is `g32-01`'s falsifier condition and the EASIEST case for `conditional` to
reject. **A lamp is in most rooms, not all of them.**

## The arithmetic makes a sharp prediction, which is why this is worth running

If a distractor is present INDEPENDENTLY with probability `p`, then over `N`
occasions where a word appears `W` times:

    count(distractor)        = N * p
    count(word, distractor)  = W * p
    conditional(word, d)     = W*p / N*p = W / N

**The `p` cancels.** So partial presence should change nothing at all, and the
refusal should be flat in `p` from 1.0 down to 0.5. If that holds, `g39-05`'s
caveat dissolves rather than being answered.

**If it does not hold, the account of why `forward` works is wrong**, because
that account is the same cancellation read from the other side.

## And the case the cancellation does NOT cover, which is the real threat

The derivation assumes the distractor's presence is independent of which concept
is being shown. A surface that is present MORE OFTEN with one concept is not a
distractor at all — it is a **confound**, and it genuinely does co-occur with
that concept more than with others.

**No statistic over co-occurrence can separate a confound from a true partner**,
because from the data's point of view there is no difference; that is why
`g32-01` names intervention as the only escape and does not claim to have tested
it. The `correlated` arm here is present to show where the boundary is, not to
be passed.

## What this does NOT duplicate, and what was searched

Searched by capability — presence, partial, confound, correlated — across
`experiments/` and `openplexus/`. `openplexus/tasks/occasions.py` has a
`presence` knob for CONCEPT surfaces; nothing varies a DISTRACTOR's presence.
`g39-05` reports the margin at full presence and its stream is reproduced here.

Predictions: `experiments/sweeps/g39-06-what-about-a-thing-present-almost-always.txt`
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
PASSES = 4
PRESENCES = (0.5, 0.7, 0.9, 0.95, 1.0)
STATISTIC = STATISTICS["conditional"]
#: The confound arm: present on every occasion of ONE digit and rarely
#: otherwise. **Not expected to be refused, and that is the point.**
FAVOURED = 3
CORRELATED_ELSEWHERE = 0.1


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

    print(f"g39-06  forward at alpha 1.0, {CODES} codes, "
          f"{PASSES * len(heard)} occasions, seeds {SEEDS}")
    print(f"        `correlated` is present on EVERY occasion of digit "
          f"{FAVOURED} and {CORRELATED_ELSEWHERE:.0%} elsewhere\n")
    header = (f"{'presence':>12}{'rank':>7}{'want':>6}{'weakest true':>14}"
              f"{'distractor':>12}{'margin':>9}{'admitted':>10}")
    print(header)
    print("-" * len(header))

    arms = [(f"{p:.2f}", p, False, None) for p in PRESENCES]
    arms.append(("correlated", None, True, None))
    # **THE FAVOURED DIGIT, SCORED ALONE.** A mean over ten words dilutes the
    # correlated arm by the nine it does not touch, which is exactly the defect
    # `g39-05`'s own caveats named one run earlier -- "a single bad word could
    # sit far lower without moving it". Reported apart rather than averaged in.
    arms.append((f"  -> digit {FAVOURED}", None, True, FAVOURED))

    for label, presence, correlated, only in arms:
        watching = [only] if only is not None else list(range(words))
        cells = len(SEEDS) * len(watching)
        ranks = wants = weakest = worst = admitted = 0.0
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
                    if correlated:
                        chance = 1.0 if digit == FAVOURED else CORRELATED_ELSEWHERE
                    else:
                        chance = presence
                    if rng.random() < chance:
                        present.add(spare)
                    index.observe(present)

            for digit in watching:
                token = 2 * CODES + digit
                scored = sorted(((STATISTIC(index, token, other), other)
                                 for other in index.partners(token)),
                                key=lambda pair: (-pair[0], pair[1]))
                order = [other for _, other in scored]
                by_id = {other: score for score, other in scored}
                true = [c for c, d in image_major.items() if d == digit]
                placed = order.index(spare) + 1 if spare in order else len(order) + 1
                ranks += placed / cells
                wants += len(true) / cells
                weakest += (min(by_id.get(c, 0.0) for c in true)
                            if true else 0.0) / cells
                worst += by_id.get(spare, 0.0) / cells
                admitted += (1 if placed <= len(true) else 0) / cells

        print(f"{label:>12}{ranks:>7.1f}{wants:>6.1f}{weakest:>14.4f}"
              f"{worst:>12.4f}{weakest - worst:>9.4f}{admitted:>10.4f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
