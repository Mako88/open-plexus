"""What does it actually COST when two nodes name the same thing differently?

Decision 1 rules out a trained quantiser because two nodes fitted on different
samples of one stream agree about almost no item — under 0.12. That is a
measurement about CODE ASSIGNMENT, and it has always been treated as fatal
without anyone measuring what it does to the walk.

**It may not be.** A concept here is not a code, it is what you reach by
walking, and decision 3 says so. If a digit's evidence is split across two
disjoint sets of codes but both sets still co-occur with the same word, the walk
may cross between them anyway — through the word, which is the same route the
cross-modal claim already relies on. Or it may not, and then the refutation is
sound for a reason nobody had actually established.

## The simulation

Two codebooks, fitted on disjoint halves of the images, exactly as two nodes
would. Each occasion is quantised by ONE of them, chosen at random — which is
what happens when the node that happened to see that input owns the write.

    shared     one codebook for everything. Not achievable in the design, and
               here as the ceiling
    split      two codebooks, occasions divided between them. This is the
               arrangement C1 actually forces on a fitted front end
    hash       the untrained hash, which needs no agreement at all

`split` against `shared` is the price of disagreement. `split` against `hash`
is whether paying it is still worth it.

    python experiments/codebook_disagreement.py --json out/codebook-disagreement.json
"""

from __future__ import annotations

import argparse
import json
import pathlib
import random
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import numpy as np  # noqa: E402

from experiments.surfaces_pipeline import read_corpus  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes)
from openplexus.grouping import codes as kmeans_codes  # noqa: E402
from openplexus.surfaces import (Hyperplanes, centred,  # noqa: E402
                                 purity)

ARMS = ("shared", "split", "hash")

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Codes per modality. Chosen here to match the hash at 6 bits, so the three
#: arms partition at the same granularity and the comparison is about WHERE the
#: boundaries are rather than how many there are.
CODES = 63

#: Questions asked. Chosen here at a thousand because 150 gives a standard
#: error of 0.025 and the effects here may be a few hundredths -- the senses
#: runs before this one were underpowered five to six times over.
ASKED = 1000


def fitted(rows, k, seed, halves=1):
    """One codebook, or `halves` codebooks each fitted on its own slice.

    Fitting on disjoint slices is the whole point: two nodes never see the same
    sample, and a codebook fitted on one is not the codebook fitted on another.
    """
    if halves == 1:
        return [kmeans_codes(rows, k, seed)]
    cut = len(rows) // halves
    books = []
    for half in range(halves):
        slice_ = rows[half * cut:(half + 1) * cut]
        # Assign EVERY row using a codebook fitted on this slice alone. The
        # slice decides the boundaries; the whole stream is then named by them.
        centres = kmeans_codes(slice_, k, seed + half)
        lookup = {}
        for row, code in zip(slice_, centres):
            lookup.setdefault(code, []).append(row)
        means = {code: np.mean(np.array(rs), axis=0)
                 for code, rs in lookup.items()}
        keys = sorted(means)
        table = np.array([means[key] for key in keys])
        assigned = np.argmin(
            ((rows[:, None, :] - table[None, :, :]) ** 2).sum(axis=2), axis=1)
        books.append([keys[a] + half * (max(keys) + 1) for a in assigned])
    return books


def build(corpus, arm, seed):
    """The graph, and what each image surface is."""
    pixels, sounds = centred(corpus.pixels), centred(corpus.sounds)
    if arm == "hash":
        image = [Hyperplanes(pixels.shape[1], bits=6, seed=seed).codes(pixels)]
        audio = [Hyperplanes(sounds.shape[1], bits=6, seed=seed).codes(sounds)]
    else:
        halves = 1 if arm == "shared" else 2
        image = fitted(pixels, CODES, seed, halves)
        audio = fitted(sounds, CODES, seed, halves)
    width = max(max(max(book) for book in image),
                max(max(book) for book in audio)) + 1
    index = CoOccurrence()
    words, distractor = 2 * width, 2 * width + 10
    picker = random.Random(seed)
    chosen = []
    for position, (image_row, audio_row, digit) in enumerate(corpus.pairs):
        book = picker.randrange(len(image))
        chosen.append(book)
        if position % 2 == 0:
            index.observe([image[book][image_row], words + digit, distractor])
        else:
            index.observe([width + audio[book][audio_row], words + digit,
                           distractor])
    return index, image, audio, width, chosen


def score(index, corpus, image, audio, width, chosen, asked):
    """From an image surface, do the audio surfaces its class holds agree?"""
    statistic = STATISTICS["conditional"]
    majors = [purity(book, corpus.said)[1] for book in audio]
    classes = equivalence_classes(index, statistic, None)
    arrived = agreed = questions = 0
    for position, (image_row, _, digit) in enumerate(corpus.pairs):
        if position % 2 or questions >= asked:
            continue
        questions += 1
        book = chosen[position]
        for surface in classes.get(image[book][image_row], frozenset()):
            if width <= surface < 2 * width:
                arrived += 1
                code = surface - width
                agreed += any(major.get(code) == digit for major in majors)
    return {"cross": agreed / arrived if arrived else 0.0,
            "reached": arrived / max(questions, 1), "asked": questions}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    # Carried from `surfaces_pipeline`, the count every table there was
    # measured at. Not chosen here -- matching it makes this the same corpus.
    parser.add_argument("--images", type=int, default=4000)
    parser.add_argument("--asked", type=int, default=ASKED)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit("REFUSING TO RUN: tools/mutate.py has the source "
                         "edited.\n" + "\n".join(str(p) for p in leftovers))

    started = time.time()
    corpus = read_corpus(args.images, 1)
    print(f"{len(corpus.pairs)} occasions, chance {corpus.chance:.4f}, "
          f"{args.asked} questions")
    print("`shared` is a ceiling the design cannot reach. `split` is what C1 "
          "forces on a fitted front end. `hash` needs no agreement at all.\n")
    header = f"{'arm':>9}{'cross':>9}{'spread':>16}{'reached':>10}"
    print(header)
    print("-" * len(header))

    rows = []
    for arm in ARMS:
        got = []
        for seed in SEEDS:
            index, image, audio, width, chosen = build(corpus, arm, seed)
            result = score(index, corpus, image, audio, width, chosen,
                           args.asked)
            got.append(result["cross"])
            rows.append({"arm": arm, "seed": seed, **result})
        print(f"{arm:>9}{sum(got) / len(got):>9.4f}"
              f"{min(got):>9.3f}-{max(got):.3f}"
              f"{rows[-1]['reached']:>10.2f}")

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"\n{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
