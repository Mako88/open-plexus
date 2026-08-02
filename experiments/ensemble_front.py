"""Several coarse hashes per item instead of one fine one. Does the graph walk?

Decision 1's open option is *spending the codes where the data is, without
fitting a codebook*, and the hash's deficit was measured today to be decisive:
at its purity nothing walks the senses graph, at k-means' purity both walks do.
Fitting a codebook is what C1 forbids, so the deficit had no legal repair.

**This is one.** Not moving the cuts — using several independent sets of them.

    one fine hash     6 bits, 63 codes, 63 items each, q_img 0.3625
    four coarse       4 families of 6 bits. Each family alone is still 62
                      codes at q 0.3635 with plenty of evidence behind every
                      one; their CONJUNCTION identifies at q 0.9845

The conjunction is never built. An item simply fires four surfaces at once, and
which combination means what is left to the counts — which is what a
co-occurrence graph is for.

**It is data-free.** Every family is a random draw from a shared seed, so two
nodes assign an identical input to identical surfaces with no communication.
Nothing is fitted and C1 is untouched, which is the whole difficulty k-means
could not get past.

## And it is the many-origins claim with enough origins to test

That claim was refuted at about four origins per occasion, several of which
were word hubs. Here an image alone supplies `families` genuine origins.

    python experiments/ensemble_front.py --json out/ensemble-front.json
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
from openplexus.broadcast import flood  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes)
from openplexus.surfaces import Hyperplanes, centred, purity  # noqa: E402

#: Families swept. 1 is the current front end exactly, and is the control.
FAMILIES = (1, 2, 4, 8)

#: Bits per family. Chosen here as the coarse end, where each code keeps enough
#: items behind it for a statistic to form -- 63 codes over 3,000 occasions.
BITS = 6

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Budget, carried from `senses_broadcast.py` where it was swept against the
#: measured per-step deficit. Not chosen here.
STAMINA = 0.02

#: How many of the best arrivals count. Carried from `senses_broadcast.py`.
TOP = 5


def build(corpus, families, bits, seed):
    """One graph. Image and audio never share an occasion, as in `alternating`.

    Layout: image families first, then audio families, then the distractor.
    Each family gets its own block, so family 0's code 3 and family 1's code 3
    are different surfaces — collapsing them would make the ensemble one hash
    with extra steps.
    """
    pixels, sounds = centred(corpus.pixels), centred(corpus.sounds)
    width = 1 << bits
    image = [Hyperplanes(pixels.shape[1], bits=bits, seed=1000 * seed + f)
             .codes(pixels) for f in range(families)]
    audio = [Hyperplanes(sounds.shape[1], bits=bits, seed=2000 * seed + f)
             .codes(sounds) for f in range(families)]
    span = families * width
    index = CoOccurrence()
    # THE WORD CHANNEL, WITHOUT WHICH THERE IS NO ROUTE AT ALL. In
    # `alternating` an image surface and an audio surface never share an
    # occasion, so the ONLY path between them is through a word. A first
    # version of this file omitted it and every arm scored 0.0000 -- not a
    # null, a graph with no connection in it.
    #
    # It is one clean surface per digit on purpose. That is a supervised
    # anchor and it is held IDENTICAL across every arm, so the only thing
    # varying is the image and audio front end, which is the question.
    words = 2 * span
    distractor = words + 10
    for position, (image_row, audio_row, digit) in enumerate(corpus.pairs):
        if position % 2 == 0:
            present = [f * width + image[f][image_row] for f in range(families)]
        else:
            present = [span + f * width + audio[f][audio_row]
                       for f in range(families)]
        index.observe(present + [words + digit, distractor])
    return index, image, audio, span, width


def score(index, corpus, image, audio, span, width, families, arm, top):
    """From an image's surfaces, do the audio surfaces reached agree?"""
    statistic = STATISTICS["conditional"]
    truth = list(corpus.digits.labels)
    said = corpus.said
    majors = [purity(a, said)[1] for a in audio]
    classes = (equivalence_classes(index, statistic, None)
               if arm == "classes" else None)
    arrived = agreed = asked = 0
    for position, (image_row, _, digit) in enumerate(corpus.pairs[:240]):
        if position % 2:
            continue
        asked += 1
        origins = [f * width + image[f][image_row] for f in range(families)]
        if arm == "classes":
            reached = set()
            for origin in origins:
                reached |= classes.get(origin, frozenset())
            heard = [s for s in reached if span <= s < 2 * span]
        else:
            got = flood(index, statistic, origins, stamina=STAMINA,
                        cost="best", combine="forward", ceiling=200_000)
            heard = sorted((s for s in got.reached if span <= s < 2 * span),
                           key=lambda s: (-got.reached[s].score, s))[:top]
        for surface in heard:
            family, code = (surface - span) // width, (surface - span) % width
            arrived += 1
            agreed += majors[family].get(code) == digit
    return (agreed / arrived if arrived else 0.0), arrived / max(asked, 1)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    # Carried from `surfaces_pipeline`, the count every table there was
    # measured at. Not chosen here -- matching it makes this the same corpus.
    parser.add_argument("--images", type=int, default=4000)
    parser.add_argument("--top", type=int, default=TOP)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit("REFUSING TO RUN: tools/mutate.py has the source "
                         "edited.\n" + "\n".join(str(p) for p in leftovers))

    started = time.time()
    corpus = read_corpus(args.images, 1)
    chance = corpus.chance
    print(f"{len(corpus.pairs)} occasions, {BITS} bits per family, "
          f"chance {chance:.4f}")
    print("families=1 is the current front end exactly, and is the control\n")
    header = (f"{'families':>9}{'surfaces':>10}{'arm':>10}{'cross':>9}"
              f"{'spread':>14}{'reached':>10}")
    print(header)
    print("-" * len(header))

    rows = []
    for families in FAMILIES:
        for arm in ("classes", "flood"):
            got, grabbed = [], []
            for seed in SEEDS:
                index, image, audio, span, width = build(
                    corpus, families, BITS, seed)
                cross, reached = score(index, corpus, image, audio, span,
                                       width, families, arm, args.top)
                got.append(cross)
                grabbed.append(reached)
                rows.append({"families": families, "arm": arm, "seed": seed,
                             "cross": cross, "reached": reached})
            print(f"{families:>9}{families * (1 << BITS):>10}{arm:>10}"
                  f"{sum(got) / len(got):>9.4f}"
                  f"{min(got):>7.3f}-{max(got):.3f}"
                  f"{sum(grabbed) / len(grabbed):>10.2f}")
        print()

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
