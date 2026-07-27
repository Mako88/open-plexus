"""Does a reward token recover what six stream-derived gates could not?

Recovery is `(arm - none) / (oracle - none)`, the same quantity as g8-01 and
g8-03 so the numbers are comparable across tasks — and with the same two
refusals, because both have already cost this project a result:

- **refuse when the floor arm is at or below the trivial floor.** A ratio whose
  denominator is the gap between a working ceiling and a broken floor is not a
  recovery of anything (g8-01's seq-1536 row, withdrawn).
- **refuse when the oracle's advantage is not larger than the seed spread.**

**Accuracy here is FIRST ASKS.** In autoregressive mode the first query of a cue
re-binds it, so later queries about that cue measure short-term echo rather than
retention. Both are reported, and the ratio uses the first.

`on-use` and `salience` are carried over unchanged from the sweeps they failed,
so this grid contains the mechanisms it is trying to beat rather than a
description of them. If they suddenly work here, suspect the task before
celebrating.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

ARMS = ("none", "oracle", "on-use", "salience", "reward")
#: reward_recall with n_values 8: guessing uniformly among values.
TRIVIAL_FLOOR = 1 / 8


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    cells: dict[tuple, dict[int, tuple]] = defaultdict(dict)
    for r in rows:
        cells[(r["delay"], r["lr"], r["arm"])][r["seed"]] = (
            r["accuracy"], r["accuracy_all"])

    delays = sorted({r["delay"] for r in rows})
    rates = sorted({r["lr"] for r in rows})

    print(f"\ntrivial floor {TRIVIAL_FLOOR:.3f}")
    print("\n=== FIRST-ASK accuracy per seed ===")
    for delay in delays:
        print(f"\ndelay {delay}")
        for lr in rates:
            line = [f"  lr={lr:<5}"]
            for arm in ARMS:
                by_seed = cells.get((delay, lr, arm), {})
                if not by_seed:
                    line.append(f"{arm}=--")
                    continue
                values = [by_seed[s][0] for s in sorted(by_seed)]
                line.append(f"{arm}=" + "/".join(f"{v:.3f}" for v in values))
            print("  ".join(line))

    print("\n=== RECOVERY of the oracle's advantage, by delay ===")
    print("The delay is the point: at 1 the marker is adjacent and the rule is")
    print("'keep the step before the obvious token', which learns nothing about")
    print("value. At 20 the binding is long past when the reward arrives.")
    print(f"{'delay':>7}{'none':>8}{'oracle':>8}{'gap':>7}{'spread':>8}"
          f"{'on-use':>9}{'salience':>10}{'reward':>9}")
    for delay in delays:
        best = None
        for lr in rates:
            means, spread = {}, 0.0
            for arm in ARMS:
                by_seed = cells.get((delay, lr, arm), {})
                if not by_seed:
                    means = {}
                    break
                values = [by_seed[s][0] for s in sorted(by_seed)]
                means[arm] = sum(values) / len(values)
                spread = max(spread, max(values) - min(values))
            if not means or means["none"] <= TRIVIAL_FLOOR:
                # A broken floor is not a candidate at any gap -- and choosing
                # the LARGEST gap would actively prefer it.
                continue
            gap = means["oracle"] - means["none"]
            if best is None or gap > best[0]:
                best = (gap, spread, means)
        if best is None:
            print(f"{delay:>7}   every cell has a floor arm at or below the "
                  f"trivial floor")
            continue
        gap, spread, means = best
        if gap <= spread:
            verdict = f"{'undefined':>9}{'undefined':>10}{'undefined':>9}"
        else:
            verdict = "".join(
                f"{(means[arm] - means['none']) / gap:>9.2f}" if arm != "salience"
                else f"{(means[arm] - means['none']) / gap:>10.2f}"
                for arm in ("on-use", "salience", "reward"))
        print(f"{delay:>7}{means['none']:>8.3f}{means['oracle']:>8.3f}"
              f"{gap:>7.3f}{spread:>8.3f}{verdict}")

    print("\nreward high at delay 1 and falling  -> the signal works and the")
    print("  LATENESS is the unsolved part, which is what tagging and capture")
    print("  exists to close.")
    print("reward high at every delay          -> suspect the task. A marker 20")
    print("  steps later should not be free.")
    print("reward near zero                    -> the gate cannot use relevance")
    print("  even when handed it, which is worse news than six failed")
    print("  inference mechanisms.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
