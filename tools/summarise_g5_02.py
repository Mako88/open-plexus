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

    chosen, max_useful, min_width, ceiling_failed = [], {}, {}, []
    for seq_len in lengths:
        print()
        print(f"=== seq_len {seq_len} : total width {TOTAL_WIDTH} split P ways ===")
        print(f"{'P':>4}{'machine w':>11}{'pooled':>9}{'alone':>8}{'lr':>7}   per seed")
        best = {}
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
            chosen.append(top[2])
            print(f"{groups:>4}{TOTAL_WIDTH // groups:>11}{top[0]:>9.3f}"
                  f"{top[1]:>8.3f}{top[2]:>7}   {top[3]}")

        if 1 in best and best[1] < SOLVED:
            ceiling_failed.append(seq_len)
            print(f"  CEILING CONTROL FAILED: undivided scores {best[1]:.3f}, "
                  f"under {SOLVED}. This row measures capacity, not division.")
            continue

        usable = [p for p in sorted(best) if best[p] >= SOLVED]
        if usable:
            max_useful[seq_len] = max(usable)
            min_width[seq_len] = TOTAL_WIDTH // max(usable)
            edge = " <- AT THE EDGE OF THE GRID, breaking point not located" \
                if max(usable) == parts[-1] else ""
            print(f"  largest usable P: {max(usable)} "
                  f"(machine width {TOTAL_WIDTH // max(usable)}){edge}")
        else:
            print("  no P reached the bar")

    print()
    fits = {s: w for s, w in min_width.items() if s not in ceiling_failed}
    if len(fits) < 2:
        print("Fewer than two usable rows: no exponent can be fitted.")
    else:
        order = sorted(fits)
        xs = [math.log(s) for s in order]
        ys = [math.log(fits[s]) for s in order]
        n = len(xs)
        mx, my = sum(xs) / n, sum(ys) / n
        denom = sum((x - mx) ** 2 for x in xs)
        alpha = (sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / denom
                 if denom else 0.0)
        print(f"MINIMUM MACHINE WIDTH grows as seq_len^{alpha:.2f}")
        print(f"  against seq_len^{WIDTH_EXPONENT} for TOTAL width (g1-10)")
        print(f"  so usable machine COUNT goes as seq_len^"
              f"{WIDTH_EXPONENT - alpha:+.2f}")
        print()
        if alpha <= WIDTH_EXPONENT + 0.1:
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
    message = pinned(chosen, rates)
    if message:
        print(f"  LEARNING-RATE GRID: {message}")
    else:
        interior = sum(1 for c in chosen if rates[0] < c < rates[-1])
        print(f"  LEARNING-RATE GRID: contained its answer ({interior} of "
              f"{len(chosen)} arms chose an interior value).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
