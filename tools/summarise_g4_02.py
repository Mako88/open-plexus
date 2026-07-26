"""Does the SHAPE of churn matter, or only how much of it there is?

Each arm is read at its own best learning rate. The equal-size control is
asserted inside the experiment rather than here, so by the time these records
exist the two arms are already known to have removed the same amount -- any gap
is shape.
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

    cells = defaultdict(list)
    for r in rows:
        cells[(r["shape"], r["d_model"], r["partitions"], r["churn"],
               r["lr"])].append(r)

    widths = sorted({w for _, w, _, _, _ in cells})
    parts = sorted({p for _, _, p, _, _ in cells})
    churns = sorted({c for _, _, _, c, _ in cells})
    rates = sorted({r for _, _, _, _, r in cells})

    def arm(shape, width, groups, churn):
        """This arm at its own best lr: (recovered, drop, lr, per-seed values)."""
        best = None
        for lr in rates:
            got = cells.get((shape, width, groups, churn, lr))
            if not got:
                continue
            recovered = sum(r["recovered"] for r in got) / len(got)
            if best is None or recovered > best[0]:
                best = (recovered,
                        sum(r["healthy"] - r["removed"] for r in got) / len(got),
                        lr, sorted(round(r["recovered"], 3) for r in got))
        return best

    print()
    print("RECOVERED ACCURACY AFTER PERMANENT REMOVAL, each arm at its own lr")
    print(f"{'width':>6}{'P':>4}{'churn':>7}{'scattered':>11}{'block':>9}"
          f"{'block-scat':>12}{'lr s/b':>12}")
    gaps = []
    for width in widths:
        for groups in parts:
            for churn in churns:
                s = arm("scattered", width, groups, churn)
                b = arm("block", width, groups, churn)
                if s is None:
                    continue
                if b is None:
                    print(f"{width:>6}{groups:>4}{churn:>7}{s[0]:>11.3f}"
                          f"{'-':>9}{'-':>12}{str(s[2]):>12}"
                          f"   (block impossible: not a whole machine)")
                    continue
                gap = b[0] - s[0]
                gaps.append((groups, gap))
                print(f"{width:>6}{groups:>4}{churn:>7}{s[0]:>11.3f}"
                      f"{b[0]:>9.3f}{gap:>+12.3f}{f'{s[2]}/{b[2]}':>12}")

    print()
    print("PER-SEED VALUES -- a mean hiding a split is worse than no number")
    for width in widths:
        for groups in parts:
            for churn in churns:
                for shape in ("scattered", "block"):
                    got = arm(shape, width, groups, churn)
                    if got:
                        print(f"  d={width:<4} P={groups:<2} churn={churn:<5} "
                              f"{shape:<10} lr={got[2]:<5} {got[3]}")

    print()
    if gaps:
        by_p = defaultdict(list)
        for groups, gap in gaps:
            by_p[groups].append(gap)
        print("DOES THE GAP GROW WITH P?  (prediction 4 said it would)")
        for groups in sorted(by_p):
            values = by_p[groups]
            print(f"  P={groups:<3} mean gap {sum(values)/len(values):+.3f}  "
                  f"over {len(values)} cells")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
