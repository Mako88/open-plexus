"""g36-04: does a THIRD modality help, and can two senses reach each other?

`g36-01` put a word beside a picture and found they reach the same concept. This
adds a SOUND — the same ten concepts spoken aloud, 3,000 real recordings by six
speakers — and asks the two questions that only appear once there are three.

**John's claim, 2026-07-31:** *"inherently the more different types of inputs
that can co-occur, the more differentiation between things is possible, and the
quicker/better the model will be able to learn."* `g36-02` measured a version of
this on the synthetic stream and found it holds in DIRECTION but at 1.6x rather
than 20x, and is **false under a fixed bound**. This is the same claim on real
sensory data under the derived bound.

**And the star, which is the shape the hub problem has.** A picture and a
recording need never occur together — you see the digit, or you hear it said.
The `alternating` arm enforces that: an image code and an audio code share zero
occasions, so the only route between them is through the word. `g33-02` found a
single global `k` cannot express that shape and `g33-04` found the derived bound
repairs it, both on symbols this project generated. Here neither is true by
construction.

Five arms, one quantiser pair per `(codes, seed)` shared across all of them so no
arm is compared against a different front end:

    image+word      one sense
    audio+word      the other sense, whose front end is measurably worse
    together        both senses in every occasion. Adding, at equal exposure
    alternating     both senses, never in the same occasion. Splitting, at HALF
                    the exposure each -- which is the honest cost of splitting
    alternating x2  the same, at twice the occasions, so each modality gets the
                    exposure `together` gives it. Separates "bridging is harder"
                    from "half the data is less data"

Predictions: `experiments/sweeps/g36-04-a-picture-a-sound-and-a-word.txt`
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
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes)
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"
IMAGES = 4000
CODES = (20, 50, 100)
NOISE = 2
DISTRACTORS = 1
SEEDS = (0, 1, 2)
ARM = "conditional"

#: Spectral summary shape. Eight time segments by sixteen frequency bands is
#: **deliberately crude**, the audio counterpart of `g36-01`'s raw pixels: the
#: point is a front end whose quality is known and reported, not a good one.
SEGMENTS, BANDS = 8, 16

ARMS = ("image+word", "audio+word", "together", "alternating", "alternating x2")


def _spectra(utterances):
    """Log energy in `BANDS` frequency bands across `SEGMENTS` time segments.

    Recordings differ in length, so the segments are proportional rather than
    fixed — which throws away speaking RATE and keeps the spectral shape. That is
    a real loss and is the honest crude choice: a fixed window would instead make
    a long recording a different feature vector from a short one saying the same
    word, which is worse for this question.
    """
    rows = []
    for utterance in utterances:
        signal = np.asarray(utterance.samples, dtype=np.float64)
        if len(signal) < SEGMENTS * 2:
            signal = np.pad(signal, (0, SEGMENTS * 2 - len(signal)))
        row = []
        for segment in np.array_split(signal, SEGMENTS):
            magnitude = np.abs(np.fft.rfft(segment * np.hanning(len(segment))))
            edges = np.linspace(0, len(magnitude), BANDS + 1).astype(int)
            row.extend(np.log1p(magnitude[a:b].sum())
                       for a, b in zip(edges[:-1], edges[1:]))
        rows.append(row)
    return np.asarray(rows)


def _stream(arm, pairs, codes, image_code, audio_code, rng):
    """One arm's occasions.

    Surfaces are laid out so a range test identifies the modality: image codes
    `[0, codes)`, audio codes `[codes, 2*codes)`, words next, distractors last.

    Args:
        pairs: `(image row, audio row, digit)` per occasion, already ordered.
    """
    word = {digit: 2 * codes + digit for digit in range(len(mnist.WORDS))}
    spare = 2 * codes + len(mnist.WORDS)

    index = CoOccurrence()
    for position, (image_row, audio_row, digit) in enumerate(pairs):
        picture, sound = image_code[image_row], audio_code[audio_row]
        if picture < 0 or sound < 0:
            continue
        present = {word[digit]}
        if arm == "image+word":
            present.add(picture)
        elif arm == "audio+word":
            present.add(codes + sound)
        elif arm == "together":
            present.update({picture, codes + sound})
        else:
            # THE STAR. Odd occasions carry the sound, even ones the picture, so
            # an image code and an audio code never once share an occasion and
            # the word is the only route between them.
            present.add(picture if position % 2 == 0 else codes + sound)
        # Noise is OTHER WORDS, matching `g36-01`: things said in the room that
        # are not about what is being shown.
        for other in rng.choice(len(mnist.WORDS), NOISE, replace=False):
            present.add(word[int(other)])
        for extra in range(DISTRACTORS):
            present.add(spare + extra)
        index.observe(present)
    return index


def _score(index, codes, image_major, audio_major):
    """Link purity per modality, the cross-sensory bridge, and a collapse guard."""
    recovered = equivalence_classes(index, STATISTICS[ARM], None)
    word = {digit: 2 * codes + digit for digit in range(len(mnist.WORDS))}

    hits = {"image": [0, 0], "audio": [0, 0]}
    for digit, token in word.items():
        found = recovered.get(token, frozenset({token}))
        for surface in found:
            if surface < codes:
                hits["image"][1] += 1
                hits["image"][0] += image_major.get(surface) == digit
            elif surface < 2 * codes:
                hits["audio"][1] += 1
                hits["audio"][0] += audio_major.get(surface - codes) == digit

    # THE CROSS-SENSORY QUESTION. Of the audio codes an image code's class
    # holds, the share whose majority digit matches the image code's own.
    reached = agreed = 0
    for picture, digit in image_major.items():
        found = recovered.get(picture, frozenset({picture}))
        for surface in found:
            if codes <= surface < 2 * codes:
                reached += 1
                agreed += audio_major.get(surface - codes) == digit

    return {
        "link_img": hits["image"][0] / hits["image"][1] if hits["image"][1] else 0.0,
        "link_aud": hits["audio"][0] / hits["audio"][1] if hits["audio"][1] else 0.0,
        "cross": agreed / reached if reached else 0.0,
        # A `cross` of 0.0 from a collapse and one from nothing being reached at
        # all are different answers. Reported so no row is read as the wrong one.
        "crossed": reached / max(len(image_major), 1),
        "classes": sum(len(v) for v in recovered.values()) / max(len(recovered), 1),
    }


def main():
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

    paths = spoken.available(FSDD_DATA)
    utterances = [spoken.read(path) for path in paths]
    spectra = _spectra(utterances)
    heard = [u.digit for u in utterances]

    # One occasion per recording, paired with an image of the SAME digit taken
    # round-robin from that digit's pool -- so every image is used about equally
    # and no draw is random, which keeps the arms comparable seed for seed.
    pool = defaultdict(list)
    for row, label in enumerate(digits.labels):
        pool[label].append(row)
    used = Counter()
    pairs = []
    for audio_row, digit in enumerate(heard):
        rows = pool[digit]
        pairs.append((rows[used[digit] % len(rows)], audio_row, digit))
        used[digit] += 1

    chance = max(Counter(heard).values()) / len(heard)
    print(f"g36-04  {len(digits)} images, {len(utterances)} recordings, "
          f"{len(spoken.speakers(paths))} speakers, {len(mnist.WORDS)} words")
    print(f"        {len(pairs)} occasions, noise {NOISE}, "
          f"distractors {DISTRACTORS}, spectra {SEGMENTS}x{BANDS}")
    print(f"        chance for every purity is the largest class share, "
          f"{chance:.4f}\n")

    header = (f"{'codes':>6}  {'arm':<15}{'q_img':>8}{'q_aud':>8}"
              f"{'link_img':>10}{'link_aud':>10}{'cross':>8}"
              f"{'crossed':>9}{'classes':>9}")
    print(header)
    print("-" * len(header))

    for codes in CODES:
        rows = {arm: [] for arm in ARMS}
        quality = []
        for seed in SEEDS:
            image_code = harness.quantise(pixels, codes, seed)
            audio_code = harness.quantise(spectra, codes, seed)
            q_img, image_major = harness.purity(image_code, digits.labels)
            q_aud, audio_major = harness.purity(audio_code, heard)
            quality.append((q_img, q_aud))

            for arm in ARMS:
                # `alternating x2` is the SAME stream run twice, so each
                # modality sees the exposure `together` gives it. Doubling the
                # occasions rather than the data is deliberate: a second pass
                # over the same pairings isolates exposure from variety.
                stream = pairs * 2 if arm == "alternating x2" else pairs
                index = _stream(arm, stream, codes, image_code, audio_code,
                                np.random.default_rng(seed))
                rows[arm].append(_score(index, codes, image_major, audio_major))

        mean = lambda values: sum(values) / len(values)   # noqa: E731 - local
        for arm in ARMS:
            got = rows[arm]
            print(f"{codes:>6}  {arm:<15}"
                  f"{mean([q for q, _ in quality]):>8.4f}"
                  f"{mean([q for _, q in quality]):>8.4f}"
                  f"{mean([r['link_img'] for r in got]):>10.4f}"
                  f"{mean([r['link_aud'] for r in got]):>10.4f}"
                  f"{mean([r['cross'] for r in got]):>8.4f}"
                  f"{mean([r['crossed'] for r in got]):>9.4f}"
                  f"{mean([r['classes'] for r in got]):>9.2f}")
        print()

    print(f"COST: {time.time() - started:.1f}s wall, one process")


if __name__ == "__main__":
    main()
