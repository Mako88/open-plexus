"""Swap the front end in the three-modality pipeline. Does the linking survive?

`surfaces_bits.py` measures the front end against itself: how pure its codes are
and whether two nodes agree about them. **Purity is not the objective**, and this
project has measured the two coming apart before — g36-04's audio quantiser was
0.185 worse at its own job than the image one and produced the strongest link in
that table. So a front end that scores worse on purity has not been shown to cost
anything until the walk is run on it.

This runs it. A picture, a sound and a word arrive together; the counts
accumulate; `grounding.equivalence_classes` walks the graph, and the columns are
what the walk recovered:

    link_img / link_aud   of the codes a word's class holds, the share whose own
                          majority digit is that word's
    cross                 of the AUDIO codes an image code's class holds, the
                          share that agree with it. The cross-sensory question
    crossed               how many image codes reached any audio code at all, so
                          a `cross` of 0 from a collapse and one from nothing
                          being reached are not read as the same answer

Four arms, and the third is the one that matters:

    image+word            one sense
    audio+word            the other
    together              both senses in every occasion
    alternating           both senses, NEVER in the same occasion — an image code
                          and an audio code share zero occasions, so the only
                          route between them is through the word

Every arm is run twice, once per front end, with the code count matched: k-means
gets `k` equal to the number of codes the hash actually used, so the comparison
is at the same granularity rather than at the same dial.

    python experiments/surfaces_pipeline.py --json out/surfaces-pipeline.json
"""

from __future__ import annotations

import argparse
import json
import pathlib
import sys
import time
from collections import Counter, defaultdict

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes)
from openplexus.grouping import codes as kmeans_codes  # noqa: E402
from openplexus.surfaces import (Hyperplanes, centred, purity,  # noqa: E402
                                 spectra)
from openplexus.tasks import mnist, spoken  # noqa: E402

MNIST_DATA = ROOT / "data" / "mnist"
FSDD_DATA = ROOT / "data" / "fsdd"

#: Images, noise words per occasion, and ever-present distractors. All three are
#: carried from g36-04 unchanged and none is swept here — this run varies the
#: FRONT END and holds the stream fixed, so any move in the columns is the front
#: end. Changing one of these would make the arms incomparable with that table.
IMAGES, NOISE, DISTRACTORS = 4000, 2, 1

#: The axis, swept. Bracketed at both ends by `surfaces_bits.py`: 6 bits is
#: coarser than ten digits and 10 gives a few hundred codes over 3,000 items.
BITS = (6, 8, 10)

#: Seeds. Three is this project's floor and it is chosen here as that floor; the
#: seed spread is printed so a difference smaller than it is not read as one.
SEEDS = (0, 1, 2)

#: The statistic. `conditional` carried from g36-04, where it was the arm run;
#: the statistic is not the question here and sweeping it would confound the two.
ARM = "conditional"

ARMS = ("image+word", "audio+word", "together", "alternating")
FRONTS = ("lsh", "kmeans")


def stream(arm: str, pairs, codes: int, image_code, audio_code, rng):
    """One arm's occasions, laid out so a range test identifies the modality.

    Image codes `[0, codes)`, audio codes `[codes, 2*codes)`, then the words,
    then the distractors. Carried from g36-04 so the stream is the one that
    table measured and only the front end differs.
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
            # THE STAR. Odd occasions carry the sound and even ones the picture,
            # so an image code and an audio code never once share an occasion.
            present.add(picture if position % 2 == 0 else codes + sound)
        # Noise is OTHER WORDS: things said in the room that are not about what
        # is being shown.
        for other in rng.choice(len(mnist.WORDS), NOISE, replace=False):
            present.add(word[int(other)])
        for extra in range(DISTRACTORS):
            present.add(spare + extra)
        index.observe(present)
    return index


def score(index, codes: int, image_major, audio_major) -> dict:
    """What the walk recovered, per modality and across them."""
    recovered = equivalence_classes(index, STATISTICS[ARM], None)
    word = {digit: 2 * codes + digit for digit in range(len(mnist.WORDS))}

    hits = {"image": [0, 0], "audio": [0, 0]}
    for digit, token in word.items():
        for surface in recovered.get(token, frozenset({token})):
            if surface < codes:
                hits["image"][1] += 1
                hits["image"][0] += image_major.get(surface) == digit
            elif surface < 2 * codes:
                hits["audio"][1] += 1
                hits["audio"][0] += audio_major.get(surface - codes) == digit

    reached = agreed = 0
    for picture, digit in image_major.items():
        for surface in recovered.get(picture, frozenset({picture})):
            if codes <= surface < 2 * codes:
                reached += 1
                agreed += audio_major.get(surface - codes) == digit

    return {
        "link_img": hits["image"][0] / hits["image"][1] if hits["image"][1] else 0.0,
        "link_aud": hits["audio"][0] / hits["audio"][1] if hits["audio"][1] else 0.0,
        "cross": agreed / reached if reached else 0.0,
        "crossed": reached / max(len(image_major), 1),
        "classes": sum(len(v) for v in recovered.values()) / max(len(recovered), 1),
    }


def quantise(front: str, rows: np.ndarray, bits: int, k: int,
             seed: int) -> list[int]:
    if front == "lsh":
        rows = centred(rows)
        return Hyperplanes(rows.shape[1], bits=bits, seed=seed).codes(rows)
    return kmeans_codes(rows, k, seed)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--quick", action="store_true",
                        help="one seed and one bit count, for a smoke run")
    parser.add_argument("--images", type=int, default=IMAGES)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit(
            "REFUSING TO RUN: tools/mutate.py has the source edited.\n"
            + "\n".join(f"  {p.relative_to(ROOT)}" for p in leftovers))
    if not (MNIST_DATA / "train-images-idx3-ubyte.gz").exists():
        raise SystemExit(f"no data in {MNIST_DATA}: python tools/fetch_mnist.py")

    started = time.time()
    digits = mnist.read(MNIST_DATA, limit=args.images)
    pixels = (np.frombuffer(b"".join(digits.images), dtype=np.uint8)
              .reshape(len(digits), digits.pixels).astype(np.float64))
    paths = spoken.available(FSDD_DATA)
    heard = [spoken.read(path) for path in paths]
    sounds = spectra(heard)
    said = [u.digit for u in heard]

    # One occasion per recording, paired with an image of the SAME digit taken
    # round-robin from that digit's pool -- every image is used about equally
    # and no draw is random, so the arms stay comparable seed for seed.
    pool = defaultdict(list)
    for row, label in enumerate(digits.labels):
        pool[label].append(row)
    used: Counter = Counter()
    pairs = []
    for audio_row, digit in enumerate(said):
        rows = pool[digit]
        pairs.append((rows[used[digit] % len(rows)], audio_row, digit))
        used[digit] += 1

    chance = max(Counter(said).values()) / len(said)
    print(f"{len(digits)} images, {len(heard)} recordings, "
          f"{len(spoken.speakers(paths))} speakers, {len(pairs)} occasions")
    print(f"noise {NOISE}, distractors {DISTRACTORS}, statistic {ARM}, "
          f"chance for every purity is {chance:.4f}\n")

    header = (f"{'bits':>5}{'front':>8}{'arm':>15}{'codes':>7}{'q_img':>8}"
              f"{'q_aud':>8}{'link_img':>10}{'link_aud':>10}{'cross':>8}"
              f"{'crossed':>9}{'classes':>9}")
    print(header)
    print("-" * len(header))

    emitted: list[dict] = []
    for bits in (BITS[1:2] if args.quick else BITS):
        for seed in (SEEDS[:1] if args.quick else SEEDS):
            # `k` is what the hash used on THIS data at THIS bit count, so both
            # front ends partition into the same number of codes.
            image_lsh = quantise("lsh", pixels, bits, 0, seed)
            audio_lsh = quantise("lsh", sounds, bits, 0, seed)
            k = max(len(set(image_lsh)), len(set(audio_lsh)), 1)
            for front in FRONTS:
                image_code = (image_lsh if front == "lsh"
                              else quantise(front, pixels, bits, k, seed))
                audio_code = (audio_lsh if front == "lsh"
                              else quantise(front, sounds, bits, k, seed))
                # The surface layout needs one width for both modalities, so it
                # takes the larger. An unused id costs nothing; an overlapping
                # one would silently make an image code and an audio code the
                # same surface.
                codes = max(max(image_code) + 1, max(audio_code) + 1)
                q_img, image_major = purity(image_code, list(digits.labels))
                q_aud, audio_major = purity(audio_code, said)
                for arm in ARMS:
                    index = stream(arm, pairs, codes, image_code, audio_code,
                                   np.random.default_rng(seed))
                    row = score(index, codes, image_major, audio_major)
                    row.update({"bits": bits, "seed": seed, "front": front,
                                "arm": arm, "codes": codes, "k": k,
                                "q_img": q_img, "q_aud": q_aud,
                                "chance": chance,
                                # `codes` is the LAYOUT width, which for the
                                # hash is 2**bits with most ids unused. What
                                # the arms are matched on is these two.
                                "used_img": len({c for c in image_code if c >= 0}),
                                "used_aud": len({c for c in audio_code if c >= 0})})
                    emitted.append(row)
                    print(f"{bits:>5}{front:>8}{arm:>15}{codes:>7}"
                          f"{q_img:>8.4f}{q_aud:>8.4f}{row['link_img']:>10.4f}"
                          f"{row['link_aud']:>10.4f}{row['cross']:>8.4f}"
                          f"{row['crossed']:>9.4f}{row['classes']:>9.2f}")
            print()

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(emitted, indent=1), encoding="utf-8")
        print(f"{len(emitted)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
