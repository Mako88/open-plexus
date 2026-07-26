"""Which cells were measured BETWEEN TWO FAILURES?

A recovery ratio divides by `oracle - none`. If the ungated arm sits at or below
the **trivial floor**, it is not doing the task at all, and that denominator is
the distance between a working ceiling and a broken floor — the gap between
something and nothing, not an advantage a mechanism could recover.

[g7-04](../experiments/sweeps/g7-04-when-does-forgetting-pay.txt) caught this and
the project recorded it: *"its largest margin, +0.249 at 1536, is between two
failures, both at/below the 0.344 floor"*. g8-01 repeated it one sweep later, and
its summariser made it worse by choosing the learning rate that **maximises** the
gap — which is exactly what a broken floor arm does.

So this exists to be run over every sweep's artefacts, not only the one where the
problem happened to be noticed.

    python tools/audit_floor.py "out/*.json"
    python tools/audit_floor.py "out/*.json" --floor 0.125

**The floor is an argument, not a constant.** It depends on the task: MQAR at
`n_pairs 4, n_values 8` gives `1/4 + (3/4)/8 = 0.34375`; reward-recall gives
`1/n_values`. A tool that hard-codes one experiment's property will be wrong about
the next one and the direction of the error is not predictable, so the default is
derived in the open and overridable.
"""

from __future__ import annotations

import argparse
import glob
import json
from collections import defaultdict

#: MQAR at n_pairs 4, n_values 8. Derived here so it can be checked.
MQAR_FLOOR = 1 / 4 + (1 - 1 / 4) / 8


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("pattern", help="glob for the sweep's JSON artefacts")
    parser.add_argument("--floor", type=float, default=MQAR_FLOOR,
                        help=f"trivial floor for the task (default "
                             f"{MQAR_FLOOR:.5f}, MQAR n_pairs=4 n_values=8)")
    parser.add_argument("--arm", default="none",
                        help="name of the floor arm (default 'none')")
    args = parser.parse_args()

    rows = [r for f in glob.glob(args.pattern) for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    # Group by every field that is not the seed, the arm or the accuracy, so this
    # works on any sweep without being told its axes.
    axes = [k for k in rows[0]
            if k not in ("seed", "arm", "accuracy", "condition")]
    cells: dict[tuple, dict[int, float]] = defaultdict(dict)
    for r in rows:
        if r["arm"] != args.arm:
            continue
        cells[tuple(r[a] for a in axes)][r["seed"]] = r["accuracy"]

    if not cells:
        print(f"no records for arm {args.arm!r}; arms present: "
              f"{sorted({r['arm'] for r in rows})}")
        return 1

    print(f"floor = {args.floor:.5f}   floor arm = {args.arm!r}")
    print(f"axes  = {axes}\n")
    print("".join(f"{a:>12}" for a in axes)
          + f"{'per seed':>26}{'mean':>8}{'usable?':>10}")

    broken = 0
    for key in sorted(cells):
        values = [cells[key][s] for s in sorted(cells[key])]
        mean = sum(values) / len(values)
        ok = mean > args.floor
        broken += not ok
        print("".join(f"{v!s:>12}" for v in key)
              + f"{'/'.join(f'{v:.3f}' for v in values):>26}"
              + f"{mean:>8.3f}{('yes' if ok else 'NO'):>10}")

    print(f"\n{broken} of {len(cells)} cells have a floor arm at or below the "
          f"trivial floor.")
    if broken:
        print("Recovery ratios in those cells divide by the gap between a "
              "working\nceiling and a broken floor. They must be WITHDRAWN "
              "rather than caveated:\na caveat printed next to a number does "
              "not attach to the number.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
