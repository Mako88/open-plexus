"""Does predicting the next observation get better, and does binding beat factoring?

The first measurement of `openplexus/prediction.py`, on the first interactive
world this project has had. Two questions, and the second is the design choice:

    does the error fall at all        the connection test. Snake's dynamics
                                      never change, so a mechanism connected to
                                      them has to get better at them
    bound against factored            predicting from (state, action) is a
                                      triple, and the two ways of holding one
                                      differ in whether they can express an
                                      INTERACTION

Snake is the case that separates them, because its dynamics are pure
interaction: going right beside a wall and going right in open space are the
same action doing entirely different things.

## What this does NOT show, and it is the next job

The state here is the exact view, so the front end is IDENTITY — no hash, no
quantisation, no generalisation. What is measured is therefore that the
mechanism learns a deterministic transition table, not that the architecture's
own front end works on it. Putting `surfaces.Hyperplanes` in front is the next
step and it is where generalisation would have to come from.

## Why hit@1 and not only surprise

A growing alphabet raises surprise on its own, because an unseen outcome
competes with more of them, so a rising curve does not mean the model is getting
worse. `hit@1` does not move with the alphabet. Both are printed, because a
number that only goes one way is not a measurement.

    python experiments/snake_prediction.py --json out/snake-prediction.json
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

from openplexus.prediction import BINDINGS, Predictor  # noqa: E402
from openplexus.tasks.snake import ACTIONS, Snake  # noqa: E402

#: Seeds, chosen here as this project's floor of three. It is three rather than
#: one because a one-seed peak regressed on confirmation three times in the
#: session before this was written.
SEEDS = (0, 1, 2)

#: Chosen here as what finishes in seconds and still lets the hit rate settle;
#: the curve is printed in windows so a run that had not converged shows it.
STEPS = 4000

#: How many windows the run is reported in. Chosen here; the first and last are
#: what the comparison uses and the middle ones are there so a curve that went
#: up and came back down cannot look like one that rose.
WINDOWS = 8


def play(binding: str, seed: int, steps: int, shuffled: bool = False):
    """One run. Returns the hit rate and mean surprise per window.

    `shuffled` replaces the world with random transitions and is the control
    that tests the DATA: with no structure there is nothing to learn, and a
    mechanism reporting improvement anyway is reporting its own smoothing.
    """
    world = Snake(width=8, height=8, sight=2, seed=seed)
    predictor = Predictor(actions=len(ACTIONS), binding=binding)
    rng = random.Random(seed)
    state = hash(world.view())
    hits: list[float] = []
    costs: list[float] = []
    for _ in range(steps):
        action = rng.randrange(len(ACTIONS))
        if shuffled:
            state, actual = rng.randrange(300), rng.randrange(300)
        else:
            actual = hash(world.step(action).view)
        hits.append(float(predictor.hit(state, action, actual)))
        costs.append(predictor.learn(state, action, actual))
        state = actual
    span = steps // WINDOWS
    return ([sum(hits[i:i + span]) / span for i in range(0, steps, span)],
            [sum(costs[i:i + span]) / span for i in range(0, steps, span)],
            len(predictor._states))


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
    print(f"snake 8x8, sight 2, random play, {args.steps} steps, "
          f"{len(SEEDS)} seeds\n")
    header = (f"{'binding':>10}{'seed':>6}{'hit first':>11}{'hit last':>10}"
              f"{'bits first':>12}{'bits last':>11}{'states':>8}")
    print(header)
    print("-" * len(header))

    rows = []
    for binding in list(BINDINGS) + ["shuffled"]:
        finals = []
        for seed in SEEDS:
            hits, costs, states = play(
                "bound" if binding == "shuffled" else binding, seed,
                args.steps, shuffled=binding == "shuffled")
            finals.append(hits[-1])
            rows.append({"binding": binding, "seed": seed, "hits": hits,
                         "bits": costs, "states": states})
            print(f"{binding:>10}{seed:>6}{hits[0]:>11.3f}{hits[-1]:>10.3f}"
                  f"{costs[0]:>12.2f}{costs[-1]:>11.2f}{states:>8}")
        print(f"{'':>10}{'mean':>6}{'':>11}{sum(finals) / len(finals):>10.3f}\n")

    print("bound against factored is the design choice; shuffled is the "
          "control on the DATA, and a hit rate near zero there is what makes "
          "the other two mean anything.")
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
