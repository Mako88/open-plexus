"""Can the GRAPH predict the next observation, now that it holds order?

`prediction.Predictor` works, and it works because the experiment hands it
`(state, action, next)` explicitly. The ordering never came from the graph,
because until `moments.Window` the graph held none — an occasion was a set and
"A then B" was indistinguishable from "A with B".

So this asks the question that answers: **with a one-way window, can plain
co-occurrence do what the predictor does?** Nothing here is told an ordering.
The stream is moments; the only thing that changes between arms is whether the
window reaches back.

    span 0    the old behaviour. Every moment isolated, so the graph has no
              temporal edge at all and can only guess from the marginal
    span 1    the previous moment writes one-way edges into this one
    span 2    two moments back

The prediction is `argmax over candidates of conditional(candidate, current)`,
read straight off the graph with no model beside it.

**The action is in the moment, not conditioned on.** A moment holds the view
surface and the action surface together, so "what follows this view AND this
action" is available to the graph as an ordinary co-occurrence — which is the
same binding `Predictor` does explicitly, arrived at from the other side.

## What a fair comparison needs

`Predictor` scores prequentially and so does this: the guess is taken before the
moment is written. The number to beat is its 0.717 on the same world, and it is
not a fair fight in one direction — the predictor gets an exact
`(state, action)` key where this gets whatever the graph's edges happen to say.

    python experiments/snake_sequence.py --json out/snake-sequence.json
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

from openplexus.grounding import STATISTICS, CoOccurrence  # noqa: E402
from openplexus.moments import Window  # noqa: E402
from openplexus.tasks.snake import ACTIONS, Snake  # noqa: E402

#: Spans swept. 0 is the old behaviour and is the control: with no temporal
#: edge the graph cannot do better than the marginal, and if it does the
#: measurement is reading something other than what it claims.
SPANS = (0, 1, 2)

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Chosen here to match `snake_prediction.py`, so 0.717 is a number this table
#: can be read against.
STEPS = 4000


def play(span: int, seed: int, steps: int) -> dict:
    world = Snake(width=8, height=8, sight=2, seed=seed)
    rng = random.Random(seed)
    index = CoOccurrence()
    window = Window(index, span=span)
    statistic = STATISTICS["conditional"]

    views: dict = {}

    def surface(view):
        return views.setdefault(view, len(views) * 2)

    def act(action):
        return action * 2 + 1

    state = surface(hash(world.view()))
    hits = []
    for _ in range(steps):
        action = rng.randrange(len(ACTIONS))
        # THE GUESS, BEFORE ANYTHING IS WRITTEN. Candidates are every view
        # surface the graph has ever seen following this one; with span 0 there
        # are none, which is the control working rather than failing.
        candidates = [p for p in index.partners(state) if p % 2 == 0]
        best, score = None, 0.0
        for candidate in candidates:
            got = statistic(index, candidate, state)
            if got > score or (got == score and best is not None
                               and candidate < best):
                best, score = candidate, got
        following = surface(hash(world.step(action).view))
        hits.append(float(best == following))
        window.observe([state, act(action)])
        state = following
    span_size = steps // 8
    return {"hits": [sum(hits[i:i + span_size]) / span_size
                     for i in range(0, steps, span_size)],
            "surfaces": len(views)}


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
          f"{len(SEEDS)} seeds")
    print("the graph alone, no Predictor. `Predictor` scores 0.717 here with "
          "an exact (state, action) key.\n")
    header = f"{'span':>6}{'seed':>6}{'hit first':>11}{'hit last':>10}{'surfaces':>10}"
    print(header)
    print("-" * len(header))

    rows = []
    for span in SPANS:
        finals = []
        for seed in SEEDS:
            got = play(span, seed, args.steps)
            finals.append(got["hits"][-1])
            rows.append({"span": span, "seed": seed, **got})
            print(f"{span:>6}{seed:>6}{got['hits'][0]:>11.3f}"
                  f"{got['hits'][-1]:>10.3f}{got['surfaces']:>10}")
        print(f"{'':>6}{'mean':>6}{'':>11}{sum(finals) / len(finals):>10.3f}\n")

    print("span 0 is the control: with no temporal edge the graph has no "
          "candidate to offer, so anything above zero there would mean this "
          "is reading something other than order.")
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
