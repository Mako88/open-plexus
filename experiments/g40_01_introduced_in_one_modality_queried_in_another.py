"""g40-01: GATE G7 — a concept introduced in one modality, queried in another.

`GOALS.md` §4 states G7 as *"does a concept introduced through one modality
answer when queried through another?"* and its falsifier as *"the same concept
cannot be reached from two modalities under these constraints — then §1.2b's
answer to symbol grounding fails, and the relational structure is a closed
symbol system."*

`g36-01` and `g36-04` both stop short of it, and both records say so in the same
words: **the word is present in every occasion of every arm**, so what was
measured is ALIGNMENT rather than introduce-then-query.

## The gate is easier than it sounds, and saying so is the point

**A co-occurrence table is ORDER-INSENSITIVE.** `count(x, y)` is the same
whatever sequence the occasions arrived in, so presenting all the pictures first
and all the sounds afterwards produces a byte-identical table to interleaving
them. **Phasing alone cannot fail**, and a run that only phased the stream would
be passing a gate by construction rather than by evidence.

So the honest content of G7 for this mechanism is not ORDER, it is **EXPOSURE**:
how few occasions of the second modality are enough before the cross-modal link
forms? That is a threshold, it is not free, and `g32-02` put the analogous
concept threshold at about 16 occasions.

**This run therefore does both**: a `phased` arm to demonstrate the
order-insensitivity rather than assume it, and a swept `share` axis that is the
real test.

## The query that answers the gate

`cross` — from an IMAGE code, does the walk reach an AUDIO code of the same
digit? In the phased arms an image code and an audio code **never share an
occasion**, and in the `late` arms the audio arrives only at the end. The word is
the sole route.

## What this does NOT duplicate, and what was searched

Searched by capability — phase, held out, introduce, exposure, share — across
`experiments/` and `openplexus/`.

- **`g36_04`'s `alternating` arm** interleaves the two senses and is the
  control here; its stream builder is not reused because that one observes
  everything before returning and this needs the ORDER to be a variable.
- **`g39_01`** made the line prequential; this reuses that shape rather than
  restating it.

Predictions: `experiments/sweeps/g40-01-introduced-in-one-modality-queried-in-another.txt`
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
from openplexus.grounding import STATISTICS, CoOccurrence, reach  # noqa: E402
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = 50
SEEDS = (0, 1, 2)
BEAM, DEPTH, COMBINE = 16, 2, "forward"
STATISTIC = STATISTICS["conditional"]
PASSES = 4
#: What share of the stream carries the SOUND. The rest carries the picture.
#: `0.50` phased is the fair-exposure case; the small values are the gate.
SHARES = (0.01, 0.02, 0.05, 0.10, 0.25, 0.50)


def _score(index, image_major, audio_major) -> dict:
    """The cross-modal question, plus what each modality reaches from the word."""
    reached = agreed = 0
    for picture, digit in image_major.items():
        found = reach(index, STATISTIC, picture, beam=BEAM, depth=DEPTH,
                      combine=COMBINE)
        order = [s for _, s in sorted(((-v, k) for k, v in found.items()))]
        sounds = [s for s in order if CODES <= s < 2 * CODES]
        want = sum(1 for _, d in audio_major.items() if d == digit)
        for surface in sounds[:want]:
            reached += 1
            agreed += audio_major.get(surface - CODES) == digit

    hits = [0, 0]
    for digit in range(len(mnist.WORDS)):
        word = 2 * CODES + digit
        found = reach(index, STATISTIC, word, beam=BEAM, depth=1,
                      combine=COMBINE)
        order = [s for _, s in sorted(((-v, k) for k, v in found.items()))]
        want = sum(1 for _, d in audio_major.items() if d == digit)
        for surface in [s for s in order if CODES <= s < 2 * CODES][:want]:
            hits[1] += 1
            hits[0] += audio_major.get(surface - CODES) == digit
    return {
        "cross": agreed / reached if reached else 0.0,
        "crossed": reached / max(len(image_major), 1),
        "word_aud": hits[0] / hits[1] if hits[1] else 0.0,
        "heard": hits[1] / max(sum(1 for _ in audio_major), 1),
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
    words = len(mnist.WORDS)

    total = PASSES * len(heard)
    print(f"g40-01  GATE G7. {CODES} codes, beam {BEAM}, depth {DEPTH}, "
          f"combiner {COMBINE}, seeds {SEEDS}")
    print(f"        {total} occasions. In every arm an image code and an audio "
          f"code share ZERO\n        occasions -- the word is the only route\n")
    header = (f"{'arm':<12}{'share':>7}{'sound occ':>11}{'per digit':>11}"
              f"{'cross':>8}{'crossed':>9}{'word_aud':>10}{'heard':>8}")
    print(header)
    print("-" * len(header))

    arms = [("phased", share) for share in SHARES]
    arms += [("interleaved", 0.50)]

    for arm, share in arms:
        totals: dict[str, float] = {}
        for seed in SEEDS:
            image_code = harness.quantise(pixels, CODES, seed)
            audio_code = harness.quantise(spectra, CODES, seed)
            _, image_major = harness.purity(image_code, digits.labels)
            _, audio_major = harness.purity(audio_code, heard)
            rng = np.random.default_rng(seed)
            word = {d: 2 * CODES + d for d in range(words)}
            spare = 2 * CODES + words
            used: Counter = Counter()
            # PHASED puts every picture occasion first and every sound occasion
            # last. INTERLEAVED mixes them. The table cannot tell the
            # difference -- which is the point of having both.
            sound_from = int(total * (1.0 - share))

            index = CoOccurrence()
            position = 0
            for _ in range(PASSES):
                for audio_row, digit in enumerate(heard):
                    here = pool[digit]
                    image_row = here[used[digit] % len(here)]
                    used[digit] += 1
                    picture = image_code[image_row]
                    sound = audio_code[audio_row]
                    if picture < 0 or sound < 0:
                        continue
                    if arm == "phased":
                        carries_sound = position >= sound_from
                    else:
                        carries_sound = rng.random() < share
                    position += 1
                    present = {word[digit]}
                    present.add(CODES + sound if carries_sound else picture)
                    for other in rng.choice(words, NOISE, replace=False):
                        present.add(word[int(other)])
                    for extra in range(DISTRACTORS):
                        present.add(spare + extra)
                    index.observe(present)

            got = _score(index, image_major, audio_major)
            for key, value in got.items():
                totals[key] = totals.get(key, 0.0) + value / len(SEEDS)

        occasions = int(total * share)
        print(f"{arm:<12}{share:>7.2f}{occasions:>11}"
              f"{occasions / words:>11.0f}{totals['cross']:>8.4f}"
              f"{totals['crossed']:>9.2f}{totals['word_aud']:>10.4f}"
              f"{totals['heard']:>8.2f}")

    print(f"\nCOST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
