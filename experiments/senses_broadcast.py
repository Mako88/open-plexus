"""Does broadcasting SEVERAL surfaces reach across modalities better than one?

The measurement `openplexus/broadcast.py` was built for, and the one the README
records as untested. The typed walk on FB15k-237 discriminated by route KIND. A
senses graph has no kinds on its edges and no kind on its questions, so the
design's answer is that several surfaces fire together and their routes
converge on the same endpoints.

**An occasion supplies about four origins**, not the hundreds an earlier note
claimed:
`surfaces.spectra` returns one row per recording and one image is one code. So
what is being asked here is whether four beats one, not whether a crowd beats
one.

## The arms

The graph is the `alternating` stream, where an image code and an audio code
NEVER share an occasion — so the only route from a picture to a sound is
through a word, and a reach that happens by co-occurrence is impossible by
construction.

    classes        `grounding.equivalence_classes`, the incumbent walk
    flood-one      broadcast the image code alone
    flood-many     broadcast the image code AND the words said on that occasion

`flood-many` is the claim. Its origins are exactly what fired together, so
nothing is being handed to it that a node would not have had.

**The audio side is never an origin**, in any arm. Starting from the modality
being asked about would answer the question with the question.

## What is reported, and why cost is a column

    cross      of the audio codes reached, the share whose majority digit
               matches the occasion's
    crossed    how many audio codes were reached at all, so nothing-reached and
               reached-and-wrong are not read as one number
    messages   partners considered, which is one message per remote read
    busiest    what the single hardest-hit node had to do. A mean hides the
               hub, and the hub is what decides whether this is affordable

    python experiments/senses_broadcast.py --json out/senses-broadcast.json
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

from experiments.surfaces_pipeline import (ARM, NOISE, Words,  # noqa: E402
                                           quantise, read_corpus, renderings,
                                           stream)
from openplexus.broadcast import (COSTS, REFUELS, VALUES,  # noqa: E402
                                  flood)
from openplexus.grounding import STATISTICS, equivalence_classes  # noqa: E402
from openplexus.surfaces import purity  # noqa: E402
from openplexus.tasks import written  # noqa: E402

ARMS = ("classes", "flood-one", "flood-many")

#: Occasions sampled to broadcast from. Chosen here as what finishes in a few
#: minutes; the flood is the cost and it grows with this linearly.
OCCASIONS = 200


def reached_audio(reached, codes: int, audio_major, digit: int,
                  top: int | None = None):
    """Of the audio codes reached, how many arrived and how many agree.

    **`top` is what makes this able to see a RANKING.** Without it this is a
    set statistic: it counts membership and is completely blind to what any
    arrival scored. A valuation rule that reorders every candidate without
    changing which ones are reachable would move it by exactly nothing — which
    is what happened when `lift` was first measured against `strength` and the
    numbers barely moved.

    With `top`, only the best `top` audio codes by score are counted, so a rule
    that puts the right ones first is visible and one that does not is too.
    """
    audio = [s for s in reached if codes <= s < 2 * codes]
    if top is not None and isinstance(reached, dict):
        audio.sort(key=lambda s: (-reached[s].score, s))
        audio = audio[:top]
    agreed = sum(audio_major.get(s - codes) == digit for s in audio)
    return len(audio), agreed


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    # Carried from `surfaces_pipeline`, the count every table there was
    # measured at. Not chosen here -- matching it makes this the same graph.
    parser.add_argument("--images", type=int, default=4000)
    # Carried from `surfaces_pipeline`, where 2 is the cell that lets
    # `alternating` afford g40-01's ~300 occasions per digit.
    parser.add_argument("--repeats", type=int, default=2)
    # Carried from `surfaces_pipeline.BITS`, whose middle value is the cell the
    # word channel was measured in. Not chosen here.
    parser.add_argument("--bits", type=int, default=8)
    # Chosen here. It picks the hyperplanes, the occasion sample and the
    # stream's noise draw, and all three are reported.
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument("--occasions", type=int, default=OCCASIONS)
    # Swept from the command line rather than pinned: it is the budget, and the
    # whole claim of stamina over a floor is that a budget behaves differently.
    parser.add_argument("--stamina", type=float, default=0.05)
    # `best` is the only pricing measured to bound the walk; see
    # `broadcast.COSTS` for the sweep that refuted `local`. Selectable so the
    # refutation can be re-run rather than believed.
    parser.add_argument("--cost", choices=COSTS, default="best")
    # What a route is PAID for a step. `strength` funds the expected and can
    # therefore only surface what the counts already favour; `surprise` funds
    # the unlikely, which is decision 4's "walk toward surprise" in the only
    # form a broadcast can express it.
    parser.add_argument("--refuel", choices=REFUELS, default="strength")
    # How an ARRIVAL is valued, which is not how a route is funded. `lift`
    # funds on strength and values on rarity -- grounded routes, unexpected
    # destinations.
    parser.add_argument("--value", choices=VALUES, default="strength")
    # How many of the best-scoring arrivals are counted. **Without this the
    # score is not measured at all** -- agreement over the whole reached SET is
    # blind to any reordering. Chosen here as a handful, which is what an
    # answer would be.
    parser.add_argument("--top", type=int, default=5)
    # WHICH FRONT END. `lsh` is the deployable one and tops out at q_img 0.42;
    # `kmeans` reaches 0.90 at a matched code count and is decision 1's ruled-out
    # arm, kept because "that gap is the price" was never measured downstream.
    parser.add_argument("--front", choices=("lsh", "kmeans"), default="lsh")
    # A SAFETY, not part of the design, and chosen here as what returns in
    # about a minute per arm. `gave_up` reports how often it fired, because a
    # walk that gave up looks exactly like one that finished.
    parser.add_argument("--ceiling", type=int, default=200_000)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit("REFUSING TO RUN: tools/mutate.py has the source "
                         "edited.\n" + "\n".join(str(p) for p in leftovers))

    started = time.time()
    corpus = read_corpus(args.images, args.repeats)
    lsh_image = quantise("lsh", corpus.pixels, args.bits, 0, args.seed)
    k = max(len(set(lsh_image)), 1)
    image_code = (lsh_image if args.front == "lsh"
                  else quantise("kmeans", corpus.pixels, args.bits, k, args.seed))
    audio_code = quantise(args.front, corpus.sounds, args.bits, k, args.seed)

    channel = written.Channel()
    heard_words, word_names, word_slots = renderings(
        corpus.pairs, channel, random.Random(0), NOISE)
    word_rows = np.array([written.features(w) for w in heard_words],
                         dtype=np.float64)
    word_code = quantise(args.front, word_rows, args.bits, k, args.seed)

    codes = max(max(image_code) + 1, max(audio_code) + 1, max(word_code) + 1)
    words = Words(width=codes,
                  per_occasion=[[word_code[i] for i in slot]
                                for slot in word_slots],
                  named=word_names)
    _, audio_major = purity(audio_code, corpus.said)

    index = stream("alternating", corpus.pairs, codes, image_code, audio_code,
                   np.random.default_rng(args.seed), words)
    statistic = STATISTICS[ARM]

    # EVEN POSITIONS CARRY THE PICTURE, which is `stream`'s own rule for the
    # `alternating` arm. Broadcasting from an occasion that held a sound would
    # be seeding the modality the question is about.
    holding_images = [position for position in range(len(corpus.pairs))
                      if position % 2 == 0]
    sampled = random.Random(args.seed).sample(
        holding_images, min(args.occasions, len(holding_images)))

    print(f"{len(corpus.pairs)} occasions, {codes} codes per modality, "
          f"broadcasting from {len(sampled)} of them")
    print(f"stamina {args.stamina}, ceiling {args.ceiling}, statistic {ARM}, "
          f"gate forward\n")
    header = (f"{'arm':>12}{'cross':>9}{'crossed':>9}{'reached':>9}"
              f"{'messages':>11}{'busiest':>9}{'gave up':>9}{'sec':>8}")
    print(header)
    print("-" * len(header))

    rows = []
    for arm in ARMS:
        began = time.time()
        arrived = agreed = asked = 0
        messages = busiest = quit_early = 0
        recovered = (equivalence_classes(index, statistic, None)
                     if arm == "classes" else None)
        for position in sampled:
            image_row, _, digit = corpus.pairs[position]
            picture = image_code[image_row]
            if picture < 0:
                continue
            asked += 1
            if arm == "classes":
                got = recovered.get(picture, frozenset({picture}))
            else:
                origins = [picture]
                if arm == "flood-many":
                    origins += [2 * codes + local
                                for local in words.per_occasion[position]]
                result = flood(index, statistic, origins,
                               stamina=args.stamina, cost=args.cost,
                               refuel=args.refuel, value=args.value,
                               combine="forward",
                               ceiling=args.ceiling)
                messages += result.messages
                busiest = max(busiest, result.busiest())
                quit_early += result.gave_up
                got = result.reached
            here, same = reached_audio(got, codes, audio_major, digit,
                                       args.top)
            arrived += here
            agreed += same
        row = {"arm": arm,
               "cross": agreed / arrived if arrived else 0.0,
               "crossed": arrived / asked if asked else 0.0,
               "reached": arrived,
               "messages": messages / asked if asked else 0.0,
               "busiest": busiest,
               "gave_up": quit_early / asked if asked else 0.0,
               "asked": asked, "bits": args.bits, "seed": args.seed,
               "stamina": args.stamina, "cost": args.cost}
        rows.append(row)
        print(f"{arm:>12}{row['cross']:>9.4f}{row['crossed']:>9.4f}"
              f"{row['reached']:>9}{row['messages']:>11.0f}"
              f"{row['busiest']:>9}{row['gave_up']:>9.4f}"
              f"{time.time() - began:>8.1f}")

    print("\nflood-many against flood-one is the claim: several surfaces "
          "firing together in place of the edge kinds a senses graph has not "
          "got. `classes` is the incumbent walk on the same graph.")
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
