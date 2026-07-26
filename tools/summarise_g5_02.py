"""How finely can a fixed capacity be divided, and does that get harder?

Total width is held at 256 throughout, so P and machine width are one variable
seen from two ends. The quantity of interest is the largest P that still reaches
the bar, and whether it falls as sequences lengthen.
"""

from __future__ import annotations

import glob
import json
import math
import sys
from collections import defaultdict

sys.path.insert(0, ".")
from tools.grid import pinned  # noqa: E402

SOLVED = 0.9
# Read from the records rather than hard-coded: g5-02 used 256 and g5-03 uses
# 240, and a total width baked into the reporting tool would silently mislabel
# every machine width in the second sweep.
TOTAL_WIDTH = None
WIDTH_EXPONENT = 0.37     # g1-10, for TOTAL width, measured at P=1



def fit_one(criterion, fits, width_exponent):
    """Fit and report one criterion, or say why it cannot be fitted."""
    print()
    if len(fits) < 2:
        print(f"'{criterion}': fewer than two rows had a LOCATED minimum width, "
              f"so no exponent can be fitted. The rest hit the grid floor, which "
              f"bounds the minimum width without measuring it.")
        return
    print(f"'{criterion}', {len(fits)} located rows: "
          + ", ".join(f"{s}:({lo},{hi}]" for s, (hi, lo) in sorted(fits.items())))
    order = sorted(fits)
    xs = [math.log(s) for s in order]
    ys = [math.log(fits[s][0]) for s in order]
    n = len(xs)
    mx, my = sum(xs) / n, sum(ys) / n
    alpha = (sum((x - mx) * (y - my) for x, y in zip(xs, ys))
             / sum((x - mx) ** 2 for x in xs))
    span = math.log(max(order) / min(order))
    bracket = max(hi / lo for hi, lo in fits.values() if lo)
    slack = math.log(bracket) / span if span else float("inf")
    print(f"  minimum machine width grows as seq_len^{alpha:.2f}, resolved to "
          f"+/-{slack:.2f}, so the exponent lies in "
          f"[{max(0.0, alpha - slack):.2f}, {alpha + slack:.2f}]")
    print(f"  against seq_len^{width_exponent} for TOTAL width (g1-10), so the "
          f"usable machine COUNT goes as seq_len^{width_exponent - alpha:+.2f}")
    if alpha - slack <= width_exponent <= alpha + slack:
        print("  UNRESOLVED -- that range spans g1-10's exponent, so this grid "
              "cannot say whether the machine count is flat or shrinking.")
    elif alpha > width_exponent:
        print("  MACHINE COUNT SHRINKS as problems grow: capability is bought by "
              "making machines bigger, not by adding more.")
    else:
        print("  MACHINE COUNT GROWS. Favourable, therefore suspect -- check the "
              "floors are spread across grid points rather than bunched.")


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    global TOTAL_WIDTH
    widths = {r["d_model"] for r in rows}
    if len(widths) != 1:
        print(f"records mix total widths {sorted(widths)}; machine width is not "
              f"comparable across them")
        return 1
    TOTAL_WIDTH = widths.pop()
    print(f"total width {TOTAL_WIDTH} throughout")

    cells = defaultdict(list)
    for r in rows:
        cells[(r["seq_len"], r["partitions"], r["lr"])].append(r)
    lengths = sorted({s for s, _, _ in cells})
    parts = sorted({p for _, p, _ in cells})
    rates = sorted({r for _, _, r in cells})

    chosen, max_useful, ceiling_failed = [], {}, []
    min_width, min_width_alone, by_row = {}, {}, {}
    for seq_len in lengths:
        print()
        print(f"=== seq_len {seq_len} : total width {TOTAL_WIDTH} split P ways ===")
        print(f"{'P':>4}{'machine w':>11}{'pooled':>9}{'alone':>8}{'lr':>7}   per seed")
        best, best_alone, row_rates = {}, {}, []
        for groups in parts:
            top = None
            for lr in rates:
                got = cells.get((seq_len, groups, lr))
                if not got:
                    continue
                pooled = sum(r["pooled"] for r in got) / len(got)
                if top is None or pooled > top[0]:
                    top = (pooled, sum(r["alone"] for r in got) / len(got), lr,
                           sorted(round(r["pooled"], 3) for r in got))
            if top is None:
                continue
            best[groups] = top[0]
            best_alone[groups] = top[1]
            chosen.append(top[2])
            row_rates.append(top[2])
            print(f"{groups:>4}{TOTAL_WIDTH // groups:>11}{top[0]:>9.3f}"
                  f"{top[1]:>8.3f}{top[2]:>7}   {top[3]}")

        by_row[seq_len] = row_rates
        if 1 in best and best[1] < SOLVED:
            ceiling_failed.append(seq_len)
            print(f"  CEILING CONTROL FAILED: undivided scores {best[1]:.3f}, "
                  f"under {SOLVED}. This row measures capacity, not division.")
            continue

        for label, scores, store in (("pooled", best, min_width),
                                     ("alone", best_alone, min_width_alone)):
            usable = [p for p in sorted(scores) if scores[p] >= SOLVED]
            if not usable:
                print(f"  {label}: no P reached the bar")
                continue
            top = max(usable)
            if top == parts[-1]:
                # The grid ran out before the model did. This is a BOUND on the
                # minimum width, not a measurement of it, and fitting a bound as
                # though it were a value is exactly what produced this sweep's
                # first reported exponent of 1.00.
                print(f"  {label}: usable to P={top} (machine width "
                      f"{TOTAL_WIDTH // top}) <- AT THE GRID FLOOR, so the "
                      f"minimum width is only known to be <= "
                      f"{TOTAL_WIDTH // top}. NOT USABLE FOR A FIT.")
                continue
            # The interval comes from the ADJACENT GRID POINT, not from halving.
            # Assuming factor-of-two steps was right for g5-02's grid and wrong
            # for g5-03's, where the steps near the crossings are 1.25-1.33x --
            # and it made the tool overstate its own uncertainty by more than
            # twofold, reporting UNRESOLVED for data that resolves.
            beaten = [q for q in parts if q > top]
            floor = TOTAL_WIDTH // beaten[0] if beaten else 0
            store[seq_len] = (TOTAL_WIDTH // top, floor)
            max_useful[seq_len] = top
            print(f"  {label}: largest usable P {top}, so minimum machine width "
                  f"is in ({floor}, {TOTAL_WIDTH // top}]")

    print()
    # Report BOTH criteria wherever the grid resolved them, rather than picking.
    # Picking meant a sweep aimed at one criterion could silently be fitted on
    # the other, and it put a choice in the tool that belongs in the sweep note.
    for criterion, source in (("alone", min_width_alone), ("pooled", min_width)):
        fit_one(criterion, {s: w for s, w in source.items()
                            if s not in ceiling_failed}, WIDTH_EXPONENT)

    print()
    # Per row, not pooled across rows. One interior choice anywhere would
    # otherwise clear a grid that is pinned in every individual comparison, and
    # the row is the unit of comparison here -- each is one sequence length
    # whose arms are read against one another.
    clean = True
    for seq_len, row in by_row.items():
        message = pinned(row, rates)
        if message:
            clean = False
            print(f"  LEARNING-RATE GRID, seq_len {seq_len}: {message}")
    if clean:
        print("  LEARNING-RATE GRID: every row contained its answer.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
