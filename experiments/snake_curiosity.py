"""Does choosing what to do beat acting at random?

The question an interactive world was wanted for, and the one no recorded
corpus can ask. Everything measured on snake so far has been **random play**:
the mechanism watched a stream it had no part in choosing. This lets prediction
error choose instead.

## The arms

    random          the baseline every earlier snake run used
    least-seen      take the action with the least evidence behind it. Count
                    based, needs no error signal at all, and is here so
                    "prediction error helps" cannot be confused with "anything
                    other than random helps"
    most-surprising take the action whose recent prediction error is highest,
                    which is John's connection made concrete

`least-seen` is the control that matters. Without it, any gain from
`most-surprising` could be explained by the mere absence of randomness.

## The failure mode to watch, named before running

**Uncertainty sampling chases irreducible noise.** Snake's dynamics have none —
that was deliberate — but it has something adjacent: **dying resets the board**,
which is the single most surprising thing available. A policy maximising
surprise may learn to die repeatedly, which is the noise-seeking failure wearing
a different hat. `deaths` is therefore a reported column, not a footnote.

## What is measured

    hit         the strict prequential hit rate, as everywhere else
    states      distinct observations visited, which is what exploring MEANS
    deaths      the named failure mode
    food        taken, and nothing optimises it -- it is reported because a
                policy that eats more without being told to is worth knowing
                about

The representation is `exact`, so the state space is large enough for coverage
to mean something; with a coarse hash the alphabet saturates in a few hundred
steps and every policy looks identical.

    python experiments/snake_curiosity.py --json out/snake-curiosity.json
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

from openplexus.prediction import Predictor  # noqa: E402
from openplexus.tasks.snake import ACTIONS, Snake  # noqa: E402

POLICIES = ("random", "least-seen", "most-surprising",
            "learning-progress")

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Chosen here as what finishes in seconds and lets coverage separate; the
#: curve is reported in windows so a policy that stalled shows it.
STEPS = 4000

#: How much of the recent error a `most-surprising` policy averages over.
#: Chosen here. Too short and it chases one unlucky step; too long and it
#: cannot notice that a question has been answered.
MEMORY = 20


def choose(policy, predictor, recent, state, rng):
    """Which action to take. Ties broken at random so nothing drifts."""
    if policy == "random":
        return rng.randrange(len(ACTIONS))
    if policy == "least-seen":
        scores = [-predictor.seen(state, action)
                  for action in range(len(ACTIONS))]
    elif policy == "learning-progress":
        # HOW MUCH THE ERROR HAS FALLEN, not how large it is. A place that is
        # already perfectly predicted has zero error and zero progress, so it
        # attracts nothing -- which is the dark room closed by construction.
        # A place that is unpredictable and STAYS unpredictable also scores
        # zero, which is the irreducible-noise failure closed by the same rule.
        scores = []
        for action in range(len(ACTIONS)):
            seen = recent.get((state, action), [])
            if len(seen) < 4:
                scores.append(1.0)          # untried is worth trying once
                continue
            half = len(seen) // 2
            older = sum(seen[:half]) / half
            newer = sum(seen[half:]) / (len(seen) - half)
            scores.append(older - newer)
        return max(range(len(ACTIONS)), key=lambda a: (scores[a], rng.random()))
    else:
        scores = [sum(recent.get((state, action), [1.0]))
                  / len(recent.get((state, action), [1.0]))
                  for action in range(len(ACTIONS))]
    best = max(scores)
    return rng.choice([action for action, score in enumerate(scores)
                       if score == best])


def play(policy: str, seed: int, steps: int) -> dict:
    world = Snake(width=8, height=8, sight=2, seed=seed)
    predictor = Predictor(actions=len(ACTIONS))
    rng = random.Random(seed)
    recent: dict = {}
    state = hash(world.view())
    hits, visited, deaths, food = [], {state}, 0, 0
    for _ in range(steps):
        action = choose(policy, predictor, recent, state, rng)
        step = world.step(action)
        actual = hash(step.view)
        deaths += step.died
        food += step.ate
        visited.add(actual)
        hits.append(float(predictor.hit(state, action, actual)))
        cost = predictor.learn(state, action, actual)
        seen = recent.setdefault((state, action), [])
        seen.append(cost)
        del seen[:-MEMORY]
        state = actual
    # THE MEASURE THAT DECIDES WHETHER ANY OF THIS MATTERS. A policy that sat
    # in one corner predicts that corner perfectly and knows nothing, and its
    # own hit rate cannot tell the difference. So after the run, freeze the
    # model and score it on a FRESH RANDOM-PLAY stream it had no part in
    # choosing -- learning nothing from it. That asks what was learned about
    # the WORLD rather than about wherever the policy chose to sit.
    elsewhere = Snake(width=8, height=8, sight=2, seed=seed + 100)
    other = random.Random(seed + 100)
    at = hash(elsewhere.view())
    held = []
    for _ in range(1000):
        action = other.randrange(len(ACTIONS))
        following = hash(elsewhere.step(action).view)
        held.append(float(predictor.hit(at, action, following)))
        at = following
    span = steps // 8
    return {"hits": [sum(hits[i:i + span]) / span
                     for i in range(0, steps, span)],
            "held": sum(held) / len(held),
            "states": len(visited), "deaths": deaths, "food": food}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--steps", type=int, default=STEPS)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit("REFUSING TO RUN: tools/mutate.py has the source "
                         "edited.\n" + "\n".join(str(p) for p in leftovers))

    started = time.time()
    print(f"snake 8x8, sight 2, exact views, {args.steps} steps, "
          f"{len(SEEDS)} seeds\n")
    header = (f"{'policy':>16}{'seed':>6}{'hit first':>11}{'hit last':>10}"
              f"{'HELD-OUT':>10}{'states':>9}{'deaths':>8}{'food':>7}")
    print(header)
    print("-" * len(header))

    rows = []
    for policy in POLICIES:
        totals = {"hits": [], "held": [], "states": [], "deaths": [],
                  "food": []}
        for seed in SEEDS:
            got = play(policy, seed, args.steps)
            rows.append({"policy": policy, "seed": seed, **got})
            totals["hits"].append(got["hits"][-1])
            for key in ("held", "states", "deaths", "food"):
                totals[key].append(got[key])
            print(f"{policy:>16}{seed:>6}{got['hits'][0]:>11.3f}"
                  f"{got['hits'][-1]:>10.3f}{got['held']:>10.3f}"
                  f"{got['states']:>9}{got['deaths']:>8}{got['food']:>7}")
        mean = lambda key: sum(totals[key]) / len(totals[key])  # noqa: E731
        print(f"{'':>16}{'mean':>6}{'':>11}{mean('hits'):>10.3f}"
              f"{mean('held'):>10.3f}{mean('states'):>9.0f}"
              f"{mean('deaths'):>8.0f}{mean('food'):>7.0f}\n")

    print("`least-seen` is the control: without it, any gain from "
          "`most-surprising` could be the mere absence of randomness. "
          "`deaths` is the named failure mode -- a reset is the most "
          "surprising thing available.")
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
