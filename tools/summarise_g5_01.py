"""Fit the machine-count exponent and compare it to g1-10's width exponent.

g1-10 measured that a single unpartitioned model needs width proportional to
SEQ_LEN^0.37. If machines are simply width in pieces, the machine-count exponent
matches it. If splitting taxes scale, it is larger -- and G5's refutation
condition is that it approaches 1.0, where doubling the problem doubles the
network.
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
WIDTH_EXPONENT = 0.37     # g1-10, measured at P=1
RESOLUTION = 0.07         # stated in the sweep note BEFORE the run


def crossing(scores):
    """Machines needed to reach SOLVED, interpolated between grid points."""
    points = sorted(scores)
    if points and scores[points[0]] >= SOLVED:
        return float(points[0]), True     # at the floor: not resolved
    for a, b in zip(points, points[1:]):
        if scores[a] < SOLVED <= scores[b]:
            return a + (SOLVED - scores[a]) / (scores[b] - scores[a]) * (b - a), False
    return None, False


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    last = max(r["epoch"] for r in rows)
    rates = sorted({r["lr"] for r in rows})
    cells = defaultdict(list)
    for r in rows:
        if r["epoch"] == last:
            cells[(r["seq_len"], r["partitions"], r["lr"])].append(r)

    lengths = sorted({s for s, _, _ in cells})
    machines = sorted({p for _, p, _ in cells})

    chosen, crossings, floored = [], {}, []
    for seq_len in lengths:
        best_pooled, best_alone, per_seed = {}, {}, {}
        for groups in machines:
            top = None
            for lr in rates:
                got = cells.get((seq_len, groups, lr))
                if not got:
                    continue
                pooled = sum(r["pooled"] for r in got) / len(got)
                if top is None or pooled > top[0]:
                    top = (pooled, sum(r["alone"] for r in got) / len(got), lr,
                           sorted(round(r["pooled"], 3) for r in got))
            if top:
                best_pooled[groups] = top[0]
                best_alone[groups] = top[1]
                per_seed[groups] = top[3]
                chosen.append(top[2])
        print()
        print(f"=== seq_len {seq_len} : pooled / one machine alone / per seed ===")
        for groups in machines:
            if groups in best_pooled:
                print(f"  {groups:>3} machines  {best_pooled[groups]:.3f} / "
                      f"{best_alone[groups]:.3f}  {per_seed[groups]}")
        point, at_floor = crossing(best_pooled)
        crossings[seq_len] = point
        if at_floor:
            floored.append(seq_len)
        located = "not located in the range tested" if point is None else f"{point:.2f} machines"
        print(f"  crossing at {SOLVED}: {located}"
              f"{'  <- AT THE FLOOR, not resolved' if at_floor else ''}")

    usable = {s: p for s, p in crossings.items() if p and s not in floored}
    print()
    if len(usable) < 2:
        print("Fewer than two usable crossings: no exponent can be fitted.")
        return 0

    order = sorted(usable)
    xs = [math.log(s) for s in order]
    ys = [math.log(usable[s]) for s in order]
    n = len(xs)
    mx, my = sum(xs) / n, sum(ys) / n
    alpha = (sum((x - mx) * (y - my) for x, y in zip(xs, ys))
             / sum((x - mx) ** 2 for x in xs))

    intercept = my - alpha * mx

    # A crossing that was NOT located is the most informative point there is: it
    # says the requirement ran off the end of the grid. Fitting only the lengths
    # that happened to cross, and then reporting the fit as a scaling law, is how
    # a saturating curve gets published as an exponent.
    missing = [s for s in lengths if crossings.get(s) is None]
    print(f"MACHINE-COUNT EXPONENT: {alpha:.2f}  (from {n} crossings)")
    print(f"  g1-10's width exponent, measured at P=1: {WIDTH_EXPONENT}")
    print(f"  resolution of this grid, stated before the run: +/-{RESOLUTION}")
    print()
    broken = False
    for seq_len in missing:
        predicted = math.exp(intercept) * seq_len ** alpha
        top = max(m for m in machines if (seq_len, m, rates[0]) in cells
                  or any((seq_len, m, r) in cells for r in rates))
        broken = True
        print(f"  HELD-OUT CHECK FAILS at seq_len {seq_len}: the fit predicts "
              f"{predicted:.1f} machines, and {top} machines did not reach "
              f"{SOLVED}. The power law does not extrapolate one step.")

    for seq_len in lengths:
        scores = {}
        for groups in machines:
            got = [cells[(seq_len, groups, r)] for r in rates
                   if (seq_len, groups, r) in cells]
            if got:
                scores[groups] = max(
                    sum(x["pooled"] for x in g) / len(g) for g in got)
        points = sorted(scores)
        if len(points) < 3:
            continue
        gains = [scores[b] - scores[a] for a, b in zip(points, points[1:])]
        # Saturation: the last doubling bought less than a twentieth of the
        # first, and the curve never reached the bar. That is the shape G5 names
        # as its refutation -- the margin shrinking with scale.
        if gains[0] > 0 and gains[-1] < gains[0] / 20 and scores[points[-1]] < SOLVED:
            broken = True
            print(f"  SATURATION at seq_len {seq_len}: doubling {points[-2]} -> "
                  f"{points[-1]} machines bought {gains[-1]:+.3f}, against "
                  f"{gains[0]:+.3f} for the first doubling, and the curve tops "
                  f"out at {scores[points[-1]]:.3f} without reaching {SOLVED}.")

    if broken:
        print()
        print("  G5 DOES NOT PASS AS MEASURED. Machines compound at the shorter "
              "lengths and stop compounding at the longest one, so the fitted "
              "exponent describes a regime rather than a law. Report the wall, "
              "not the exponent.")
    elif alpha >= 0.9:
        print("  G5 REFUTED: machines do not compound. Doubling the problem "
              "roughly doubles the network.")
    elif abs(alpha - WIDTH_EXPONENT) <= RESOLUTION:
        print("  CONSISTENT WITH 0.37, NOT RESOLVED. Partitioning may be free "
              "with respect to scale; this grid cannot tell. A finer P grid is "
              "the follow-up, not a claim.")
    elif alpha > WIDTH_EXPONENT:
        print(f"  Partitioning taxes scale: {alpha:.2f} against "
              f"{WIDTH_EXPONENT}. G5 passes, and the tax is quantified.")
    else:
        print(f"  Machines compound BETTER than width does ({alpha:.2f} < "
              f"{WIDTH_EXPONENT}). Favourable, therefore suspect -- check the "
              "crossings are spread across grid points rather than bunched.")

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
