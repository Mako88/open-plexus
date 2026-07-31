"""g37-01: how wide is CLUTRR-symbolic's measurement band?

`GOALS.md`'s G0 acceptance test, applied to an instrument this project already
has on disk and has never scored a floor for:

    before any learning mechanism is written, the task must be shown to have
    substantial headroom between what a random frozen substrate achieves and
    what a strong non-local reference achieves, with both measured, multi-seed,
    and with the base rate of a constant predictor reported alongside

**This script computes the LAST of those three and nothing else.** The base rate
of a constant predictor is a property of the data, needs no model, no seed and no
training, and it is the number that decides whether the other two are worth
spending runner time on. `closure`'s band against its honest floor is 0.092
(`g14-01`), and that width is why kill-list item #1 is recorded as blocked.

**Why a majority-class floor and not chance.** Chance on 20 relations is 0.0500
and is the wrong floor for the same reason `g32-01`'s 0.5 was: a constant
predictor is free, so any number a mechanism produces has to be read against
*always answer the commonest relation*, not against a coin.

**Reported per hop bucket, and split on entity repetition**, because note 059
established that CLUTRR confounds depth with repetition — train and validation
hold **zero** puzzles where an entity appears in more than two edges, and test
holds 37.8% rising with depth. A single average across that split credits DEPTH
for an ADDRESSING failure. The `max_appearances <= 2` arm is the primary one and
its floor is reported beside the unconditional floor precisely so the two can be
seen to differ.

Record: `experiments/sweeps/g37-01-the-band-on-clutrr.txt`
"""

from __future__ import annotations

import pathlib
import sys
import time
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.tasks import clutrr  # noqa: E402

DATA = ROOT / "data" / "clutrr"
SPLITS = ("train", "validation", "test")


def _floor(puzzles) -> float:
    """The share the commonest answer takes. A constant predictor's score."""
    if not puzzles:
        return 0.0
    answers = Counter(puzzle.target for puzzle in puzzles)
    return answers.most_common(1)[0][1] / len(puzzles)


def main() -> None:
    harness.parse_args(__doc__)
    started = time.time()
    if not (DATA / "gen_train23_test2to10").exists():
        raise SystemExit(f"no data in {DATA}. Run: python tools/fetch_clutrr.py")

    chance = 1.0 / len(clutrr.RELATIONS)
    print(f"g37-01  CLUTRR gen_train23_test2to10, layout kinship, "
          f"{len(clutrr.RELATIONS)} relations")
    print(f"        chance for a uniform guess is {chance:.4f}. **That is NOT "
          f"the floor** -- a constant\n        predictor is free, so the floor "
          f"is the commonest answer's share\n")

    for split in SPLITS:
        puzzles = clutrr.load(clutrr.ClutrrConfig(root=DATA, split=split))
        buckets: dict[int, list] = {}
        for puzzle in puzzles:
            buckets.setdefault(puzzle.hops, []).append(puzzle)

        used = len({puzzle.target for puzzle in puzzles})
        print(f"=== {split}: {len(puzzles)} puzzles, {used} of "
              f"{len(clutrr.RELATIONS)} relations used as answers ===")
        header = (f"{'hops':>5}{'puzzles':>9}{'majority':>10}"
                  f"{'rep<=2':>9}{'maj|rep<=2':>12}{'n|rep<=2':>10}")
        print(header)
        print("-" * len(header))
        for hops in sorted(buckets):
            group = buckets[hops]
            plain = [p for p in group if p.max_appearances <= 2]
            print(f"{hops:>5}{len(group):>9}{_floor(group):>10.4f}"
                  f"{len(plain) / len(group):>9.4f}{_floor(plain):>12.4f}"
                  f"{len(plain):>10}")
        print(f"{'all':>5}{len(puzzles):>9}{_floor(puzzles):>10.4f}\n")

    print(f"COST: {time.time() - started:.1f}s wall, one process, no model")


if __name__ == "__main__":
    main()
