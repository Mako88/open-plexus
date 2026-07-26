"""Report the price of dropping the global readout.

Each arm is read at ITS OWN best learning rate, never at a shared one. A
comparison where one side is pinned to the other side's optimum is the error
g1-11 made and g1-12 paid for, and P changes the error term, so lr and P are not
independent.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict


def main() -> int:
    pattern = sys.argv[1] if len(sys.argv) > 1 else "out/*.json"
    rows = [r for f in glob.glob(pattern) for r in json.load(open(f))]
    if not rows:
        print(f"no records matched {pattern}")
        return 1

    last = max(r["epoch"] for r in rows)
    prev = max((r["epoch"] for r in rows if r["epoch"] < last), default=last)

    # (width, seq_len, partitions, lr, epoch) -> mean over seeds
    grouped = defaultdict(lambda: defaultdict(list))
    for r in rows:
        key = (r["d_model"], r["seq_len"], r["partitions"], r["lr"], r["epoch"])
        for field in ("pooled", "alone_mean", "alone_worst"):
            grouped[key][field].append(r[field])
    mean = {k: {f: sum(v) / len(v) for f, v in d.items()}
            for k, d in grouped.items()}

    widths = sorted({w for w, _, _, _, _ in mean})
    lengths = sorted({s for _, s, _, _, _ in mean})
    parts = sorted({p for _, _, p, _, _ in mean})
    rates = sorted({r for _, _, _, r, _ in mean})

    def best(width, seq_len, groups, epoch, field):
        """This arm's score at this arm's own best learning rate."""
        cells = [(mean[(width, seq_len, groups, lr, epoch)][field], lr)
                 for lr in rates
                 if (width, seq_len, groups, lr, epoch) in mean]
        return max(cells) if cells else (None, None)

    warnings = []
    for seq_len in lengths:
        print()
        print(f"=== seq_len {seq_len} : pooled / one group alone / worst group ===")
        print(f"{'width':>7}" + "".join(f"P={p}".rjust(22) for p in parts))
        for width in widths:
            cells = []
            for groups in parts:
                if width % groups:
                    cells.append(f"{'-':>22}")
                    continue
                pooled, lr = best(width, seq_len, groups, last, "pooled")
                if pooled is None:
                    cells.append(f"{'-':>22}")
                    continue
                alone = mean[(width, seq_len, groups, lr, last)]["alone_mean"]
                worst = mean[(width, seq_len, groups, lr, last)]["alone_worst"]
                cells.append(f"{pooled:.3f}/{alone:.3f}/{worst:.3f}".rjust(22))
                earlier = mean.get((width, seq_len, groups, lr, prev))
                if earlier and abs(earlier["pooled"] - pooled) > 0.02:
                    warnings.append(
                        f"seq={seq_len} d={width} P={groups} lr={lr}: pooled "
                        f"moved {abs(earlier['pooled'] - pooled):.3f} between "
                        f"epoch {prev} and {last}")
            print(f"{width:>7}" + "".join(cells))

    print()
    print("THE PRICE OF DROPPING THE GLOBAL READOUT (pooled, vs P=1)")
    print(f"{'seq_len':>9}{'width':>7}" + "".join(f"P={p}".rjust(11) for p in parts))
    for seq_len in lengths:
        for width in widths:
            baseline, _ = best(width, seq_len, 1, last, "pooled")
            if baseline is None:
                continue
            cells = []
            for groups in parts:
                if width % groups:
                    cells.append(f"{'-':>11}")
                    continue
                pooled, _ = best(width, seq_len, groups, last, "pooled")
                cells.append(f"{'-':>11}" if pooled is None
                             else f"{pooled - baseline:+.3f}".rjust(11))
            print(f"{seq_len:>9}{width:>7}" + "".join(cells))

    print()
    print("EACH ARM'S BEST LEARNING RATE -- a column that never varies means "
          "the grid is one-sided")
    for seq_len in lengths:
        for width in widths:
            chosen = []
            for groups in parts:
                if width % groups:
                    continue
                _, lr = best(width, seq_len, groups, last, "pooled")
                chosen.append(f"P={groups}:{lr}")
            print(f"  seq={seq_len:<5} d={width:<5} " + "  ".join(chosen))

    print()
    if warnings:
        print("CONVERGENCE WARNINGS -- these are bounds, not numbers:")
        for w in warnings:
            print(f"  {w}")
    else:
        print(f"Converged: no cell moved more than 0.02 between epoch "
              f"{prev} and {last}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
