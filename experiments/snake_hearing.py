"""Does a sense that reaches further than sight close the dark room?

John's suggestion, 2026-08-02. `snake_curiosity.py` measured every directed
policy losing to random, and the worst of them found a corner of open space
where every view is identical and sat there for 4,000 steps predicting
perfectly. **The dark room exists because there genuinely is one**: a centred
view of a featureless region is the same view whichever way you moved.

A sound the food makes, audible beyond the visual window, means nowhere is
featureless. That closes the room by changing the world rather than by patching
the policy — which is the more honest repair, because the policy was not wrong
about anything.

## The sound is a real recording, and distance TILTS it rather than quieting it

The call is an FSDD utterance, a different sample each time, so what the sound
IS has to be discovered across speakers exactly as in the senses pipeline.

**Loudness cannot carry distance here and that is not a choice.**
`surfaces.centred` subtracts each row's own mean, and attenuating a signal
shifts every log-band energy by the same constant — so a quieter call and a
nearer one are the same feature vector after centring. What survives a mean
subtraction is a change in SHAPE, so distance is rendered the way it physically
works: high frequencies fall off faster than low ones, which tilts the spectrum.

## The arms, and the measure that is fair across them

    silent    vision only, as every earlier snake run
    hearing   vision and the call, in one occasion

Adding a channel enlarges the observation alphabet, so "distinct observations
visited" would rise mechanically and prove nothing. **The measure is distinct
board POSITIONS visited** — a fact about where the policy went, independent of
how anything is represented — plus held-out prediction on a stream the policy
did not choose.

    python experiments/snake_hearing.py --json out/snake-hearing.json
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

from openplexus.prediction import Predictor  # noqa: E402
from openplexus.surfaces import (BANDS, SEGMENTS, Hyperplanes,  # noqa: E402
                                 centred, spectra)
from openplexus.tasks import spoken  # noqa: E402
from openplexus.tasks.snake import ACTIONS, Snake  # noqa: E402

FSDD_DATA = ROOT / "data" / "fsdd"

POLICIES = ("random", "least-seen", "learning-progress")
ARMS = ("silent", "hearing")

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Chosen here, matching `snake_curiosity.py` so the two tables are comparable.
STEPS = 4000

#: Chosen here, matching `snake_curiosity.py`.
MEMORY = 20

#: Chosen here as the number of bits the vision and the call each get.
BITS = 8

#: How fast the high bands fall away with distance. Chosen here, and it is the
#: dial that decides whether the call carries distance at all: at zero every
#: distance sounds identical and the arm collapses to `silent` with extra
#: surfaces. Swept from the command line.
TILT = 0.25

#: Recordings loaded for the call. Chosen here as enough for the sample to vary
#: between steps without the load dominating the run.
CALLS = 60


def tilted(row: np.ndarray, distance: float, tilt: float) -> np.ndarray:
    """Attenuate the high bands more than the low ones, by distance.

    A constant attenuation is invisible after `centred`; a slope is not. This is
    also how distance actually works on sound, which is why it is the honest
    rendering rather than a trick to defeat the front end.
    """
    slope = np.tile(np.arange(BANDS, dtype=np.float64), SEGMENTS)
    return row - tilt * distance * slope / BANDS


def choose(policy, predictor, recent, state, rng):
    if policy == "random":
        return rng.randrange(len(ACTIONS))
    if policy == "least-seen":
        scores = [-predictor.seen(state, action)
                  for action in range(len(ACTIONS))]
    else:
        scores = []
        for action in range(len(ACTIONS)):
            seen = recent.get((state, action), [])
            if len(seen) < 4:
                scores.append(1.0)
                continue
            half = len(seen) // 2
            scores.append(sum(seen[:half]) / half
                          - sum(seen[half:]) / (len(seen) - half))
    best = max(scores)
    return rng.choice([action for action, score in enumerate(scores)
                       if score == best])


def play(arm, policy, seed, steps, tilt, calls) -> dict:
    world = Snake(width=8, height=8, sight=2, seed=seed)
    predictor = Predictor(actions=len(ACTIONS))
    rng = random.Random(seed)
    ears = Hyperplanes(SEGMENTS * BANDS, bits=BITS, seed=seed)

    def observe(here):
        """`(what to predict FROM, what to predict)`.

        **The call is a state and never a target.** A different recording is
        drawn every step, so which one arrives is irreducibly unpredictable —
        asking the model to foresee it would measure noise. What hearing is for
        is predicting what will be SEEN, so the target stays the view in both
        arms and only the condition changes. That also keeps the two arms'
        scores comparable, which they are not if the targets differ.
        """
        view = hash(here.view())
        if arm == "silent":
            return view, view
        row = tilted(calls[rng.randrange(len(calls))],
                     here.distance_to_food(), tilt)
        heard = ears.codes(centred(np.array([row])))[0]
        return hash((view, int(heard))), view

    recent: dict = {}
    state, _ = observe(world)
    hits, places, deaths, food = [], {world.body[0]}, 0, 0
    for _ in range(steps):
        action = choose(policy, predictor, recent, state, rng)
        step = world.step(action)
        deaths += step.died
        food += step.ate
        places.add(world.body[0])
        following, target = observe(world)
        hits.append(float(predictor.hit(state, action, target)))
        cost = predictor.learn(state, action, target)
        seen = recent.setdefault((state, action), [])
        seen.append(cost)
        del seen[:-MEMORY]
        state = following

    elsewhere = Snake(width=8, height=8, sight=2, seed=seed + 100)
    other = random.Random(seed + 100)
    at, _ = observe(elsewhere)
    held = []
    for _ in range(1000):
        action = other.randrange(len(ACTIONS))
        elsewhere.step(action)
        following, target = observe(elsewhere)
        held.append(float(predictor.hit(at, action, target)))
        at = following
    span = steps // 8
    return {"hits": [sum(hits[i:i + span]) / span
                     for i in range(0, steps, span)],
            "held": sum(held) / len(held), "places": len(places),
            "deaths": deaths, "food": food}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--steps", type=int, default=STEPS)
    parser.add_argument("--tilt", type=float, default=TILT)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit("REFUSING TO RUN: tools/mutate.py has the source "
                         "edited.\n" + "\n".join(str(p) for p in leftovers))
    paths = spoken.available(FSDD_DATA)
    if not paths:
        raise SystemExit(f"no recordings in {FSDD_DATA}: "
                         "python tools/fetch_fsdd.py")

    started = time.time()
    heard = [spoken.read(path) for path in paths[:CALLS]]
    calls = spectra(heard)
    print(f"snake 8x8, sight 2, {args.steps} steps, {len(SEEDS)} seeds, "
          f"{len(calls)} recordings, tilt {args.tilt}")
    print("PLACES is distinct board positions -- a fact about where the policy "
          "went, so adding a channel cannot inflate it\n")
    header = (f"{'arm':>9}{'policy':>18}{'hit':>8}{'HELD-OUT':>10}"
              f"{'places':>8}{'deaths':>8}{'food':>7}")
    print(header)
    print("-" * len(header))

    rows = []
    for arm in ARMS:
        for policy in POLICIES:
            totals: dict = {"hits": [], "held": [], "places": [],
                            "deaths": [], "food": []}
            for seed in SEEDS:
                got = play(arm, policy, seed, args.steps, args.tilt, calls)
                rows.append({"arm": arm, "policy": policy, "seed": seed,
                             "tilt": args.tilt, **got})
                totals["hits"].append(got["hits"][-1])
                for key in ("held", "places", "deaths", "food"):
                    totals[key].append(got[key])
            mean = lambda k: sum(totals[k]) / len(totals[k])  # noqa: E731
            print(f"{arm:>9}{policy:>18}{mean('hits'):>8.3f}"
                  f"{mean('held'):>10.3f}{mean('places'):>8.0f}"
                  f"{mean('deaths'):>8.0f}{mean('food'):>7.0f}")
        print()

    print("The dark room shows as `least-seen` holding a tiny PLACES count. "
          "If hearing closes it, that number rises and its held-out score "
          "rises with it.")
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
