"""Does the cliff follow the gate's reach, and is there an interior best?

[g9-02](../experiments/sweeps/g9-02-a-gate-that-reads-its-own-input.txt) fixed
the reach at 8 and found recovery of 0.21/0.20/0.23 at delays 1, 4, 8 and
**-0.13** at 20. This sweeps the reach, so the table is **window x delay** and
the diagonal is where reach just covers the delay.

Two readings, and they want different next projects:

- **Cliff on the diagonal, with an interior best** — reach must be *matched* to a
  delay nobody knows in advance, and a tag (which marks one binding rather than a
  span) is a mechanism.
- **Large window always fine** — reach is free if affordable, and a tag is a cost
  optimisation for tiny nodes rather than a capability.

The same two refusals as every other summariser here: no ratio when the floor arm
is at or below the trivial floor, and none when the oracle's advantage is not
larger than the seed spread. Both have already cost this project a result.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

ARMS = ("none", "oracle", "on-use", "salience", "reward")
#: reward_recall with n_values 8.
TRIVIAL_FLOOR = 1 / 8


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    cells: dict[tuple, dict[int, float]] = defaultdict(dict)
    for r in rows:
        cells[(r["window"], r["delay"], r["arm"])][r["seed"]] = r["accuracy"]

    windows = sorted({r["window"] for r in rows})
    delays = sorted({r["delay"] for r in rows})

    def recovery(window: int, delay: int) -> float | None:
        means, spread = {}, 0.0
        for arm in ARMS:
            by_seed = cells.get((window, delay, arm), {})
            if not by_seed:
                return None
            values = list(by_seed.values())
            means[arm] = sum(values) / len(values)
            spread = max(spread, max(values) - min(values))
        if means["none"] <= TRIVIAL_FLOOR:
            return None                      # two failures, not a difficulty
        gap = means["oracle"] - means["none"]
        if gap <= spread:
            return None                      # denominator inside the noise
        return (means["reward"] - means["none"]) / gap

    print(f"\ntrivial floor {TRIVIAL_FLOOR:.3f}")
    print("\n=== RECOVERY of the reward gate, by reach and delay ===")
    print("Rows are how far the gate can reach; columns are how far back the")
    print("binding is. On and above the diagonal the reach covers the delay.")
    print(f"{'window':>8}" + "".join(f"{d:>9}" for d in delays) + "   best delay")
    for window in windows:
        values = [recovery(window, d) for d in delays]
        cells_text = "".join("undefined".rjust(9) if v is None else f"{v:>9.2f}"
                             for v in values)
        known = [(v, d) for v, d in zip(values, delays) if v is not None]
        best = f"{max(known)[1]:>12}" if known else "         n/a"
        print(f"{window:>8}{cells_text}{best}")

    print("\n=== IS THERE AN INTERIOR BEST? ===")
    print("For each delay, the reach that recovers most. If it sits at roughly")
    print("the delay, reach must be MATCHED and a tag is a mechanism. If it is")
    print("always the largest window, reach is free and a tag is a cost saving.")
    print(f"{'delay':>7}{'best window':>13}{'recovery':>10}"
          f"{'at largest window':>19}")
    for delay in delays:
        known = [(recovery(w, delay), w) for w in windows
                 if recovery(w, delay) is not None]
        if not known:
            print(f"{delay:>7}   every cell undefined")
            continue
        value, window = max(known)
        largest = recovery(windows[-1], delay)
        largest_text = "undefined" if largest is None else f"{largest:.2f}"
        print(f"{delay:>7}{window:>13}{value:>10.2f}{largest_text:>19}")

    print("\nbest window ~ delay        -> reach must be matched to an unknown")
    print("                              lag. Build the tag; it is a mechanism.")
    print("best window always largest -> reach is free if affordable. The tag is")
    print("                              a cost optimisation for tiny nodes.")
    print("nothing positive anywhere  -> the gate cannot use a late signal, and")
    print("                              g9-02's 0.2 was about adjacency.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
