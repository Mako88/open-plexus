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
TOTAL_WIDTH = 256
WIDTH_EXPONENT = 0.37     # g1-10, for TOTAL width, measured at P=1


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

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
            store[seq_len] = TOTAL_WIDTH // top
            max_useful[seq_len] = top
            print(f"  {label}: largest usable P {top}, so minimum machine width "
                  f"is in ({TOTAL_WIDTH // top // 2}, {TOTAL_WIDTH // top}]")

    print()
    # Prefer whichever criterion the grid actually resolved. `alone` is the one
    # C1 needs -- a machine that cannot afford to pool -- and being the harder
    # bar it saturates less, so it is usually the better resolved of the two.
    fits = {s: w for s, w in min_width_alone.items() if s not in ceiling_failed}
    criterion = "alone"
    if len(fits) < 2:
        fits = {s: w for s, w in min_width.items() if s not in ceiling_failed}
        criterion = "pooled"
    if len(fits) < 2:
        print("Fewer than two rows had a LOCATED minimum width: no exponent can "
              "be fitted. The others hit the grid floor, which bounds the "
              "minimum width without measuring it.")
    else:
        print(f"Fitting on the '{criterion}' criterion, "
              f"{len(fits)} located rows: {fits}")
        order = sorted(fits)
        xs = [math.log(s) for s in order]
        ys = [math.log(fits[s]) for s in order]
        n = len(xs)
        mx, my = sum(xs) / n, sum(ys) / n
        denom = sum((x - mx) ** 2 for x in xs)
        alpha = (sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / denom
                 if denom else 0.0)
        # Each width is known only to within a factor of two, so the exponent
        # carries a range rather than a value. Quoting the point estimate alone
        # is how a factor-of-two grid gets published as a scaling law.
        span = math.log(max(order) / min(order))
        slack = math.log(2.0) / span if span else float("inf")
        print(f"MINIMUM MACHINE WIDTH grows as seq_len^{alpha:.2f}, and this "
              f"grid resolves it only to +/-{slack:.2f}")
        print(f"  so the exponent lies in "
              f"[{max(0.0, alpha - slack):.2f}, {alpha + slack:.2f}]")
        print(f"  against seq_len^{WIDTH_EXPONENT} for TOTAL width (g1-10)")
        print(f"  so usable machine COUNT goes as seq_len^"
              f"{WIDTH_EXPONENT - alpha:+.2f}")
        print()
        if alpha - slack <= WIDTH_EXPONENT <= alpha + slack:
            print("  UNRESOLVED. That range spans g1-10's 0.37, so this grid "
                  "cannot say whether the usable machine count is flat or "
                  "shrinking. A finer width grid, or a longer lever arm in "
                  "seq_len, is the follow-up -- not a claim.")
        elif alpha <= WIDTH_EXPONENT + 0.1:
            print("  Machine width scales like total width: the usable machine "
                  "count is roughly CONSTANT with problem size. Favourable, so "
                  "check the crossings are spread rather than bunched at one "
                  "grid point.")
        elif WIDTH_EXPONENT - alpha < -0.15:
            print("  USABLE MACHINE COUNT SHRINKS as problems grow. Past some "
                  "size, capability is bought by making machines bigger, not by "
                  "adding more -- which is the outcome G5 exists to detect.")
        else:
            print("  Machine count is close to flat; the grid cannot separate "
                  "shrinking from constant.")

        if len(fits) == len({v for v in fits.values()}) and len(set(fits.values())) == 1:
            print("  WARNING: every row gave the same machine width, so the "
                  "exponent is an artefact of grid resolution.")

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
