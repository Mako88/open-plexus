"""Does a bounded pool flatten the recovery-versus-length curve?

**The prediction is about a SHAPE, so this reports the shape.** g8-01 measured
recovery falling with sequence length -- 0.05 at 192 to -0.00 at 1536 -- and note
015 attributes that to a threshold setting a RATE while the oracle sets a
QUANTITY. A pool of `k` sets a quantity, so its curve should be flat whatever
height it sits at.

A tool that printed only the best cell would answer a different question and
would answer it convincingly, so the table is laid out as pool x length and the
last column is the thing prediction 1 is actually about: **the slope**, recovery
at the longest length minus recovery at the shortest.

Flat and low is a confirmed mechanism that does not help. Falling is the
mechanism not working. Those must not be merged, and a single headline number
merges them.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict


#: Trivial floor for the MQAR configuration these sweeps use: n_pairs 4,
#: n_values 8, so `1/n_pairs + (1 - 1/n_pairs)/n_values`.
#:
#: A tool that hard-codes a property of one experiment will be wrong about the
#: next one, and the direction of the error is not predictable -- which this
#: repository has recorded happening three times in one reporting tool. So it is
#: named, derived in the open, and checked against the grid it is used on.
TRIVIAL_FLOOR = 1 / 4 + (1 - 1 / 4) / 8


def floor_arm_works(mean_none: float, floor: float = TRIVIAL_FLOOR) -> bool:
    """Is this cell measuring a difficulty, or two failures?

    A recovery ratio divides by `oracle - none`. If `none` sits at or below the
    trivial floor, the model with no gate is not doing the task at all, and the
    denominator is the distance between a working ceiling and a broken floor.
    That is not an advantage a mechanism could recover; it is the gap between
    something and nothing.
    """
    return mean_none > floor


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    cells: dict[tuple, dict[int, float]] = defaultdict(dict)
    for r in rows:
        cells[(r["seq_len"], r["lr"], r["arm"])][r["seed"]] = r["accuracy"]

    seq_lens = sorted({r["seq_len"] for r in rows})
    rates = sorted({r["lr"] for r in rows})
    pools = sorted({r["arm"] for r in rows if r["arm"].startswith("capture-")},
                   key=lambda name: int(name.split("-")[1]))

    print("\n=== accuracy per seed ===")
    for seq_len in seq_lens:
        print(f"\nseq_len {seq_len}")
        for lr in rates:
            line = [f"  lr={lr:<5}"]
            for arm in ["none", "oracle"] + pools:
                by_seed = cells.get((seq_len, lr, arm), {})
                if not by_seed:
                    line.append(f"{arm}=--")
                    continue
                values = [by_seed[s] for s in sorted(by_seed)]
                line.append(f"{arm}=" + "/".join(f"{v:.3f}" for v in values))
            print("  ".join(line))

    # recovery[pool][seq_len], at each length's best learning rate by oracle gap
    recovery: dict[str, dict[int, float | None]] = defaultdict(dict)
    for seq_len in seq_lens:
        best = None
        for lr in rates:
            means, spread = {}, 0.0
            arms = ["none", "oracle"] + pools
            for arm in arms:
                by_seed = cells.get((seq_len, lr, arm), {})
                if not by_seed:
                    means = {}
                    break
                values = list(by_seed.values())
                means[arm] = sum(values) / len(values)
                spread = max(spread, max(values) - min(values))
            if not means:
                continue
            gap = means["oracle"] - means["none"]
            if not floor_arm_works(means["none"]):
                # Not a candidate at any gap. Selecting the largest gap
                # would otherwise PREFER this cell, because a broken
                # floor arm is what maximises it.
                continue
            if best is None or gap > best[0]:
                best = (gap, spread, means)
        for pool in pools:
            if best is None:
                recovery[pool][seq_len] = None
                continue
            gap, spread, means = best
            recovery[pool][seq_len] = (
                None if gap <= spread
                else (means[pool] - means["none"]) / gap)

    print("\n=== RECOVERY by pool size and sequence length ===")
    print("Prediction 1: the bounded pools stay flat and capture-0 falls.")
    header = "".join(f"{s:>9}" for s in seq_lens)
    print(f"{'pool':>12}{header}{'slope':>9}")
    for pool in pools:
        values = [recovery[pool][s] for s in seq_lens]
        cells_text = "".join("undefined".rjust(9) if v is None else f"{v:>9.2f}"
                             for v in values)
        ends = [values[0], values[-1]]
        slope = ("      n/a" if any(v is None for v in ends)
                 else f"{ends[1] - ends[0]:>9.2f}")
        print(f"{pool:>12}{cells_text}{slope}")

    print("\nslope near 0  -> the pool holds N constant, which is what it is for.")
    print("slope negative-> recovery still decays with length; for capture-0 that")
    print("                 reproduces g8-01, and for a bounded pool it refutes")
    print("                 note 015's argument.")
    print("HEIGHT AND SHAPE ARE SEPARATE FINDINGS. Flat at 0.02 means the")
    print("mechanism works and does not help; do not report it as either alone.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
