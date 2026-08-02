"""A graph with the answer PLANTED in it. Does the instrument find it?

Every cross-modal measurement on the senses graph has come back at chance —
`broadcast.flood` under every gate, pricing, budget, valuation and metric
tried, and `grounding.equivalence_classes` reaching between 1 and 24 audio
codes across 150 questions. **Nothing walks that graph well, including the
mechanism that is not on trial.** So there are two explanations and no
measurement separating them: the walks cannot find what is there, or there is
nothing there.

This separates them. The graph here is built to contain the answer, at a noise
level that can be turned down to zero. **At zero the route is unambiguous and a
working walk has to score near 1.0.** If it scores at chance, the instrument is
broken and every earlier null was about the instrument.

## The missing half of the control set

CLAUDE.md says a control tests the DATA, not the code: a shuffle asks whether a
pattern is real and says nothing about whether the measurement is right. Every
control in this project so far is that kind — shuffled streams, structureless
floors. **This is the complementary one and the project has never had it:**
known data, to test the code. A shuffle can only tell you a null is honest; it
cannot tell you a null is not an artefact.

## The shape is the senses graph's shape

`concepts` concepts, each with `codes` image surfaces, `codes` audio surfaces
and `codes` word surfaces — mirroring the roughly seventy to a hundred codes per
digit the real front end produces. An ever-present distractor sits on every
occasion, as it does there.

**An image surface and an audio surface never share an occasion**, exactly as
in the `alternating` arm, so the only route from a picture to a sound is
through a word and no reach can happen by direct co-occurrence.

`noise` is the share of occasions whose word belongs to the WRONG concept. At
0.0 the mapping is perfect; the sweep says how much confusion each walk
survives, which is what places the real graph on the same axis.

    python experiments/planted_control.py --json out/planted-control.json
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

from openplexus.broadcast import flood  # noqa: E402
from openplexus.grounding import (STATISTICS, CoOccurrence,  # noqa: E402
                                  equivalence_classes)

#: Concepts, chosen here to match the ten digits the real task has, so chance
#: is the same 0.1 and the two tables sit on one axis.
CONCEPTS = 10

#: Surfaces per concept per modality. Chosen here to mirror the seventy to a
#: hundred codes per digit the real front end was measured to produce.
CODES = 10

#: Occasions. Chosen here as enough for every planted pair to recur many times;
#: the point of this graph is that nothing is starved.
OCCASIONS = 20000

#: Word-channel confusion. **Pinned at zero, chosen here**, because the sweep
#: over 0.0 to 0.5 found no cell where either walk dropped below 0.98 — a
#: perfect code with a noisy name is a far easier problem than the real one, so
#: this axis was the wrong one and `PURITY` replaced it.
NOISE = (0.0,)

#: Code purity swept. **0.42 is the real operating point** — `q_img` for the
#: hash at 8 bits. 1.0 is the first version of this control, which measured a
#: problem far easier than the real one.
PURITY = (1.0, 0.9, 0.7, 0.5, 0.42)

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Budgets swept. Chosen here to span the per-step deficit under `strength`
#: pricing, which was measured at about 0.08 on the real graph — a grid that
#: does not span it is not a sweep.
STAMINA = (0.005, 0.02, 0.1)

#: How many of the best arrivals are counted. Chosen here to match
#: `senses_broadcast.py` so the two are comparable.
TOP = 5


def build(concepts, codes, occasions, noise, rng,
          purity: float = 1.0) -> CoOccurrence:
    """A graph whose answer is known. Image and audio never share an occasion.

    Args:
        noise: Share of occasions whose WORD names the wrong concept.
        purity: Share of occasions whose sensed code comes from its own
            concept's block. **This is the axis the real front end sits on**,
            and the first version of this control did not have it: `q_img` was
            measured at 0.4205 for the hash at 8 bits, meaning a code's own
            majority concept accounts for well under half its firings. Noise in
            the word channel was the wrong thing to sweep, because a perfect
            code with a noisy name is a far easier problem than a code that
            does not mean one thing.
    """
    index = CoOccurrence()
    distractor = 3 * concepts * codes

    def block(concept):
        owner = (concept if rng.random() < purity
                 else rng.randrange(concepts))
        return owner * codes + rng.randrange(codes)

    for step in range(occasions):
        concept = rng.randrange(concepts)
        named = (rng.randrange(concepts) if rng.random() < noise else concept)
        word = 2 * concepts * codes + named * codes + rng.randrange(codes)
        sensed = (block(concept) if step % 2 == 0
                  else concepts * codes + block(concept))
        index.observe((sensed, word, distractor))
    return index


def score(index, concepts, codes, statistic, arm, stamina, top):
    """Broadcast each image surface; how many of the top audio arrivals agree."""
    low, high = concepts * codes, 2 * concepts * codes
    arrived = agreed = asked = 0
    classes = (equivalence_classes(index, statistic, None)
               if arm == "classes" else None)
    for picture in range(concepts * codes):
        concept = picture // codes
        asked += 1
        if arm == "classes":
            audio = [s for s in classes.get(picture, frozenset())
                     if low <= s < high]
        else:
            got = flood(index, statistic, [picture], stamina=stamina,
                        cost="best", combine="forward", ceiling=200_000)
            audio = sorted((s for s in got.reached if low <= s < high),
                           key=lambda s: (-got.reached[s].score, s))[:top]
        arrived += len(audio)
        agreed += sum((s - low) // codes == concept for s in audio)
    return {"cross": agreed / arrived if arrived else 0.0,
            "reached": arrived / max(asked, 1)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--occasions", type=int, default=OCCASIONS)
    parser.add_argument("--top", type=int, default=TOP)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit("REFUSING TO RUN: tools/mutate.py has the source "
                         "edited.\n" + "\n".join(str(p) for p in leftovers))

    started = time.time()
    statistic = STATISTICS["conditional"]
    print(f"{CONCEPTS} concepts x {CODES} codes x 3 modalities, "
          f"{args.occasions} occasions, chance {1 / CONCEPTS:.4f}")
    print("AT noise 0.0 THE ANSWER IS UNAMBIGUOUS. A walk at chance there is "
          "a broken instrument, not a null.\n")
    header = (f"{'purity':>7}{'arm':>22}{'cross':>9}{'spread':>12}")
    print(header)
    print("-" * len(header))

    rows = []
    for purity in PURITY:
      for noise in NOISE:
        arms = [("classes", None), ("flood", 0.02)]
        for arm, stamina in arms:
            crosses = []
            for seed in SEEDS:
                rng = random.Random(seed)
                index = build(CONCEPTS, CODES, args.occasions, noise,
                              rng, purity)
                got = score(index, CONCEPTS, CODES, statistic, arm, stamina,
                            args.top)
                crosses.append(got["cross"])
                rows.append({"noise": noise, "purity": purity, "arm": arm,
                             "stamina": stamina, "seed": seed, **got})
            name = arm if stamina is None else f"flood s={stamina}"
            spread = f"{min(crosses):.3f}-{max(crosses):.3f}"
            print(f"{purity:>7.2f}{name:>22}"
                  f"{sum(crosses) / len(crosses):>9.4f}{spread:>12}")
      print()

    print("`classes` is the incumbent walk and is here so a flood failure and "
          "a graph failure cannot be confused.")
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
