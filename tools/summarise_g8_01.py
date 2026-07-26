"""How much of the oracle's advantage did a real mechanism recover?

The reported quantity is a RATIO, `(arm - none) / (oracle - none)`, because raw
accuracy hides the question: every arm rises and falls together with sequence
length and learning rate, so four columns of accuracy look like four columns of
the same thing. 0 means the mechanism bought nothing the floor did not already
have; 1 means it matched a gate that cheats.

**A ratio needs its denominator checked before it is printed.** Where the
oracle's advantage is not larger than the spread across seeds, recovery is
reported as `undefined` -- not as a number with a caveat beside it, which is the
failure this project has produced twice. At seq 192 the pre-dispatch control put
that advantage at 0.043, so that row is expected to be undefined and its being
undefined is a result rather than a gap.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

ARMS = ("none", "oracle", "on-use", "salience")


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    # (seq_len, half_life, lr, arm) -> {seed: accuracy}
    cells: dict[tuple, dict[int, float]] = defaultdict(dict)
    for r in rows:
        cells[(r["seq_len"], r["half_life"], r["lr"], r["arm"])][r["seed"]] = \
            r["accuracy"]

    seq_lens = sorted({r["seq_len"] for r in rows})
    half_lives = sorted({r["half_life"] for r in rows}, reverse=True)
    rates = sorted({r["lr"] for r in rows})

    print("\n=== accuracy per seed, by sequence length and half-life ===")
    print("(each cell is one seed per column, at the learning rate named)")
    for seq_len in seq_lens:
        print(f"\nseq_len {seq_len}")
        for half in half_lives:
            for lr in rates:
                line = [f"  half={half:<6} lr={lr:<5}"]
                for arm in ARMS:
                    by_seed = cells.get((seq_len, half, lr, arm), {})
                    if not by_seed:
                        line.append(f"{arm}=--")
                        continue
                    values = [by_seed[s] for s in sorted(by_seed)]
                    line.append(f"{arm}=" + "/".join(f"{v:.3f}" for v in values))
                print("  ".join(line))

    print("\n=== RECOVERY of the oracle's advantage ===")
    print("(arm - none) / (oracle - none), at each cell's best learning rate")
    print(f"{'seq_len':>8}  {'half-life':>9}  {'oracle gap':>10}  "
          f"{'seed spread':>11}  {'on-use':>8}  {'salience':>8}")
    for seq_len in seq_lens:
        for half in half_lives:
            best = None
            for lr in rates:
                means = {}
                spread = 0.0
                for arm in ARMS:
                    by_seed = cells.get((seq_len, half, lr, arm), {})
                    if not by_seed:
                        means = {}
                        break
                    values = list(by_seed.values())
                    means[arm] = sum(values) / len(values)
                    spread = max(spread, max(values) - min(values))
                if not means:
                    continue
                gap = means["oracle"] - means["none"]
                if best is None or gap > best[0]:
                    best = (gap, spread, means)
            if best is None:
                continue
            gap, spread, means = best
            if gap <= spread:
                # The denominator is not larger than the noise. Printing a ratio
                # here would be inventing precision the grid does not have.
                verdict = f"{'undefined':>8}  {'undefined':>8}"
            else:
                verdict = "".join(
                    f"{(means[arm] - means['none']) / gap:>10.2f}"
                    for arm in ("on-use", "salience"))
            print(f"{seq_len:>8}  {half:>9}  {gap:>10.3f}  {spread:>11.3f}  "
                  f"{verdict}")

    print("\nRecovery near 0 for both arms means selective storage is not "
          "reachable\nby any local rule tried, and every result that depends on "
          "the gate must be\nlabelled a CEILING rather than a finding.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
