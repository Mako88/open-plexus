"""Does prediction survive losing exact-view identity to a real front end?

`snake_prediction.py` used `hash(view)`, so the state was the EXACT view and the
front end was identity. That measured the prediction mechanism and said nothing
about the architecture, which never gets exact identity — it gets whatever the
hash gives it.

This puts the real front end on. Three arms, same world, same seeds:

    exact      hash(view). No quantisation. The reference, and not a fair
               comparison: it has information the others do not
    whole      the whole view through `Hyperplanes`, one code per step
    patches    every overlapping 3x3 window through the same hash, one code
               each, and ONE PREDICTOR PER WINDOW POSITION
    neighbours the same windows, except each one also sees the code of the
               window it is moving TOWARDS. What a window lacks is what is
               about to enter it, and what is about to enter it comes from the
               direction of travel -- so this conditions on ONE neighbour
               rather than all eight, which would multiply the state space by
               the neighbour alphabet eight times over

`patches` is the column arrangement made concrete: small units, each seeing a
slice, each predicting only its own slice. It should win on recurrence — a 3x3
alphabet repeats far more than a 5x5 one — and it can lose, because a window
cannot see what is about to enter it from outside.

## The cells are CATEGORICAL and that decides the encoding

`EMPTY WALL BODY FOOD` are 0 1 2 3, and a hyperplane over those numbers would
treat wall-and-body as near and empty-and-food as far, which is arithmetic
nobody meant. Each cell is therefore one-hot over the four kinds, so no two
kinds are closer than any other pair.

## What is comparable and what is not

Each arm predicts the next state IN ITS OWN REPRESENTATION, so a coarser arm has
an easier task. **The alphabet size is printed beside every score** and the
scores must not be read across arms without it. What IS comparable across arms
is whether the curve rises at all, and the shuffled control that says it should
not.

    python experiments/snake_surfaces.py --json out/snake-surfaces.json
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
from openplexus.surfaces import Hyperplanes, centred  # noqa: E402
from openplexus.tasks.snake import ACTIONS, Snake, patches  # noqa: E402

ARMS = ("exact", "whole", "patches", "neighbours")

#: Seeds, chosen here as this project's floor of three.
SEEDS = (0, 1, 2)

#: Chosen here as what finishes in under a minute per arm and still lets the
#: hit rate settle; the curve is reported in windows so a run that had not
#: converged shows it rather than being averaged flat.
STEPS = 4000

#: Chosen here. Small enough that a 3x3 window's alphabet recurs heavily and
#: large enough that a 5x5 view is not collapsed to a handful of codes; both
#: alphabets are reported so the choice can be read off the table.
BITS = 8

#: Chosen here as the smallest window that still contains a centre and its
#: neighbours, which is what a local rule about movement needs.
PATCH = 3

#: How many cell kinds there are, from `tasks.snake`. Not chosen here.
KINDS = 4


def one_hot(cells: tuple[int, ...]) -> list[float]:
    """A categorical cell becomes `KINDS` dimensions, exactly one of them set."""
    row = [0.0] * (len(cells) * KINDS)
    for at, value in enumerate(cells):
        row[at * KINDS + value] = 1.0
    return row


def coder(rows: list[list[float]], seed: int) -> Hyperplanes:
    return Hyperplanes(len(rows[0]), bits=BITS, seed=seed)


def play(arm: str, seed: int, steps: int, shuffled: bool = False) -> dict:
    """One run, scored prequentially. Returns the windowed hit rate."""
    world = Snake(width=8, height=8, sight=2, seed=seed)
    rng = random.Random(seed)

    # THE HASH IS FIXED BEFORE THE RUN, from one pass of random play, so the
    # front end is not fitted to the stream it is then measured on. It needs
    # only the DIMENSION, which is why a single warm-up view is enough.
    sample = world.view()
    width = (len(one_hot(sample)) if arm == "whole"
             else len(one_hot(patches(sample, PATCH)[0])))
    hyperplanes = Hyperplanes(width, bits=BITS, seed=seed)

    def encode(view):
        if arm == "exact":
            return [hash(view)]
        if arm == "whole":
            rows = np.array([one_hot(view)], dtype=np.float64)
            return list(hyperplanes.codes(centred(rows)))
        rows = np.array([one_hot(p) for p in patches(view, PATCH)],
                        dtype=np.float64)
        return list(hyperplanes.codes(centred(rows)))

    columns = len(encode(sample))
    side = int(columns ** 0.5)

    def conditioned(codes, at, action):
        """What column `at` predicts FROM. Its own code, or its own and the
        code of the window it is heading towards.

        A window cannot see what is about to enter it, and what is about to
        enter it comes from the direction of travel — so one neighbour carries
        almost all of the missing information, where all eight would multiply
        the state space by the neighbour alphabet eight times over.
        """
        if arm != "neighbours":
            return codes[at]
        dx, dy = ACTIONS[action]
        left, top = at % side, at // side
        beside, below = left + dx, top + dy
        neighbour = (codes[below * side + beside]
                     if 0 <= beside < side and 0 <= below < side else -1)
        return hash((codes[at], neighbour))

    predictors = [Predictor(actions=len(ACTIONS)) for _ in range(columns)]
    state = encode(world.view())
    hits: list[float] = []
    whole_right: list[float] = []
    alphabet: set = set()
    for _ in range(steps):
        action = rng.randrange(len(ACTIONS))
        if shuffled:
            following = [rng.randrange(256) for _ in range(columns)]
        else:
            following = encode(world.step(action).view)
        alphabet.update(following)
        # EVERY COLUMN SCORED, and the run's hit rate is the share of columns
        # that were right. With one column that is the ordinary hit@1; with
        # nine it is how much of the next observation was foreseen, which is
        # the honest generalisation and not a max over columns.
        #
        # THE TARGET IS ALWAYS THE COLUMN'S OWN NEXT CODE, in every arm. Only
        # what it predicts FROM changes, so a richer condition cannot win by
        # being asked an easier question.
        right = 0
        for at, predictor in enumerate(predictors):
            from_here = conditioned(state, at, action)
            right += predictor.hit(from_here, action, following[at])
            predictor.learn(from_here, action, following[at])
        hits.append(right / columns)
        # THE STRICT MEASURE, and the only one comparable across arms. `share`
        # rewards being right about 8 of 9 easy sub-tasks; this asks the same
        # question every arm is asked -- was the whole next observation
        # foreseen. With one column the two are identical.
        whole_right.append(float(right == columns))
        state = following
    span = steps // 8
    return {"hits": [sum(hits[i:i + span]) / span
                     for i in range(0, steps, span)],
            "whole": [sum(whole_right[i:i + span]) / span
                      for i in range(0, steps, span)],
            "columns": columns, "alphabet": len(alphabet)}


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
          f"{BITS} bits, {PATCH}x{PATCH} patches, {len(SEEDS)} seeds")
    print("scores are NOT comparable across arms -- each predicts in its own "
          "representation, so read the alphabet column with them\n")
    header = (f"{'arm':>9}{'seed':>6}{'columns':>9}{'alphabet':>10}"
              f"{'share first':>13}{'share last':>12}{'ALL first':>11}"
              f"{'ALL last':>10}")
    print(header)
    print("-" * len(header))

    rows = []
    for arm in list(ARMS) + ["shuffled"]:
        finals = []
        for seed in SEEDS:
            got = play("patches" if arm == "shuffled" else arm, seed,
                       args.steps, shuffled=arm == "shuffled")
            finals.append(got["whole"][-1])
            rows.append({"arm": arm, "seed": seed, **got})
            print(f"{arm:>9}{seed:>6}{got['columns']:>9}{got['alphabet']:>10}"
                  f"{got['hits'][0]:>13.3f}{got['hits'][-1]:>12.3f}"
                  f"{got['whole'][0]:>11.3f}{got['whole'][-1]:>10.3f}")
        print(f"{'':>9}{'mean':>6}{'':>19}{'':>11}"
              f"{sum(finals) / len(finals):>10.3f}\n")

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(rows, indent=1), encoding="utf-8")
        print(f"{len(rows)} rows -> {args.json}")
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
