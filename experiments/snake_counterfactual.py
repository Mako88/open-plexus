"""Can it predict the effect of an action it has never taken there?

John's criterion, 2026-08-02, for what would DEMONSTRATE understanding rather
than merely motivate it: not "what tends to go with what" but "what would have
happened if I had done X". That is the counterfactual rung, it needs no external
reward because the world scores it, and unlike compression or empowerment it is
measurable today with no new machinery.

## The design

Every `(state, action)` pair is assigned once, deterministically, to SEEN or
HELD OUT. A held-out pair is scored whenever it comes up and **never learned
from** — the model watches the outcome and is not told it. So a held-out
question asks exactly *what happens if I do this here*, about a thing it has no
record of.

## The prediction this experiment exists to test

**`bound` should collapse and `factored` should not**, and the reason is the
whole difference between them. A bound surface for `(state, action)` has zero
evidence when that pair was never counted — there is nothing to look up.
Factoring keeps `what follows this state` and `what follows this action`
separately, and both have evidence from elsewhere, so it can compose an answer
for a pair it has never seen.

**That is the exact reverse of `snake_prediction.py`**, where bound beat
factored 0.717 to 0.437 on pairs it HAD seen. If both hold, the two arms are
not better and worse — they are memory and generalisation, and the choice
between them is a choice about which question is being asked.

If instead factored also collapses, then nothing here generalises at all and
the counterfactual is out of reach for this representation, which is worth
knowing and is the more likely outcome.

## What makes a held-out score meaningful

`chance` is reported: one over the number of distinct outcomes seen. And the
SEEN column runs beside the held-out one in every row, because a model that is
bad at both is not demonstrating anything about counterfactuals.

    python experiments/snake_counterfactual.py --json out/snake-counterfactual.json
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

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Chosen here to match `snake_prediction.py`, so its 0.717 is a number this
#: table can be read against.
STEPS = 12000

#: Share of `(state, action)` pairs never learned from. Chosen here at a
#: quarter: large enough that held-out questions arrive often enough to score,
#: small enough that the model still has most of the world to learn from.
HELD = 0.25


def play(binding: str, seed: int, steps: int, held: float) -> dict:
    world = Snake(width=8, height=8, sight=2, seed=seed)
    predictor = Predictor(actions=len(ACTIONS), binding=binding)
    rng = random.Random(seed)
    chooser = random.Random(seed + 7)
    state = hash(world.view())
    seen_hits: list[float] = []
    held_hits: list[float] = []
    outcomes: set = set()

    def withheld(state, action):
        """Deterministic, so a pair is held out consistently for the whole run
        and cannot leak by being learned from on one visit and not the next."""
        return random.Random(f"{state}:{action}:{seed}").random() < held

    for _ in range(steps):
        action = chooser.randrange(len(ACTIONS))
        actual = hash(world.step(action).view)
        outcomes.add(actual)
        right = float(predictor.hit(state, action, actual))
        if withheld(state, action):
            held_hits.append(right)
        else:
            seen_hits.append(right)
            predictor.learn(state, action, actual)
        state = actual

    def tail(values):
        if not values:
            return 0.0
        window = max(len(values) // 4, 1)
        return sum(values[-window:]) / window

    return {"seen": tail(seen_hits), "held": tail(held_hits),
            "asked": len(held_hits), "outcomes": len(outcomes),
            "chance": 1.0 / max(len(outcomes), 1)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--steps", type=int, default=STEPS)
    parser.add_argument("--held", type=float, default=HELD)
    args = parser.parse_args()

    leftovers = sorted(ROOT.glob("**/*.py.bak"))
    if leftovers:
        raise SystemExit("REFUSING TO RUN: tools/mutate.py has the source "
                         "edited.\n" + "\n".join(str(p) for p in leftovers))

    started = time.time()
    print(f"snake 8x8, sight 2, random play, {args.steps} steps, "
          f"{args.held:.0%} of (state, action) pairs never learned from")
    print("HELD-OUT asks what happens if I do this HERE, about a pair with no "
          "record. SEEN runs beside it because a model bad at both shows "
          "nothing.\n")
    header = (f"{'binding':>10}{'seed':>6}{'seen':>9}{'HELD-OUT':>10}"
              f"{'chance':>9}{'asked':>8}")
    print(header)
    print("-" * len(header))

    rows = []
    for binding in BINDINGS:
        finals = []
        for seed in SEEDS:
            got = play(binding, seed, args.steps, args.held)
            finals.append(got["held"])
            rows.append({"binding": binding, "seed": seed, **got})
            print(f"{binding:>10}{seed:>6}{got['seen']:>9.3f}"
                  f"{got['held']:>10.3f}{got['chance']:>9.4f}"
                  f"{got['asked']:>8}")
        print(f"{'':>10}{'mean':>6}{'':>9}"
              f"{sum(finals) / len(finals):>10.3f}\n")

    print("The prediction: `bound` collapses because a pair never counted has "
          "nothing to look up, and `factored` does not because both halves "
          "have evidence from elsewhere. That is the reverse of "
          "snake_prediction.py, where bound won on pairs it HAD seen.")
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
