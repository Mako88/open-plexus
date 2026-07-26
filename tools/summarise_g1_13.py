"""Report both crossings, with a convergence guard.

A crossing is only reported where the two largest training budgets agree to
within a tolerance. Otherwise the model was still climbing when the budget ran
out, and the honest output is a bound rather than a number — which is the exact
error g1-11 made and g1-12 caught.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

SOLVED = 0.9
CONVERGED = 0.02


def interpolate(scores: dict[int, float]) -> float | None:
    widths = sorted(scores)
    if widths and scores[widths[0]] >= SOLVED:
        return float(widths[0])
    for a, b in zip(widths, widths[1:]):
        if scores[a] < SOLVED <= scores[b]:
            return a + (SOLVED - scores[a]) / (scores[b] - scores[a]) * (b - a)
    return None


def main() -> int:
    pattern = sys.argv[1] if len(sys.argv) > 1 else "out/*.json"
    rows = [r for f in glob.glob(pattern) for r in json.load(open(f))]
    if not rows:
        print(f"no records matched {pattern}")
        return 1

    by = defaultdict(list)
    for r in rows:
        by[(r["mode"], r["seq_len"], r["d_model"], r["budget"])].append(r["accuracy"])

    crossings, warnings = {}, []
    for mode in ("attention", "local"):
        budgets = sorted({b for m, _, _, b in by if m == mode})
        if not budgets:
            continue
        lengths = sorted({s for m, s, _, _ in by if m == mode})
        widths = sorted({w for m, _, w, _ in by if m == mode})
        last, prev = budgets[-1], budgets[-2] if len(budgets) > 1 else budgets[-1]

        print()
        print(f"=== {mode} : accuracy at the largest budget ({last}) ===")
        print(f"{'seq_len':>9}" + "".join(f"d={w}".rjust(9) for w in widths))
        for s in lengths:
            final = {w: sum(by[(mode, s, w, last)]) / len(by[(mode, s, w, last)])
                     for w in widths if (mode, s, w, last) in by}
            earlier = {w: sum(by[(mode, s, w, prev)]) / len(by[(mode, s, w, prev)])
                       for w in widths if (mode, s, w, prev) in by}
            print(f"{s:>9}" + "".join(
                f"{final[w]:>9.3f}" if w in final else f"{'-':>9}" for w in widths))
            still_moving = [w for w in final
                            if w in earlier and abs(final[w] - earlier[w]) > CONVERGED]
            if still_moving:
                warnings.append(f"{mode} seq={s}: widths {still_moving} moved more "
                                f"than {CONVERGED} between budget {prev} and {last}")
            crossings[(mode, s)] = interpolate(final)

    print()
    print("THE PRICE, BOTH ARMS FED")
    print(f"{'seq_len':>9}{'d_local':>9}{'d_att':>8}{'width x':>10}"
          f"{'local mem':>11}{'att mem':>10}{'memory x':>11}")
    for s in sorted({s for _, s in crossings}):
        dl, da = crossings.get(("local", s)), crossings.get(("attention", s))
        if not dl or not da:
            print(f"{s:>9}" + "  crossing not located in the width range tested")
            continue
        lm, am = dl * dl, 2 * s * da
        print(f"{s:>9}{dl:>9.1f}{da:>8.1f}{dl/da:>9.1f}x"
              f"{lm:>11.0f}{am:>10.0f}{am/lm:>10.2f}x")

    print()
    if warnings:
        print("CONVERGENCE WARNINGS -- these crossings are bounds, not numbers:")
        for w in warnings:
            print(f"  {w}")
    else:
        print("Both arms converged: no cell moved more than "
              f"{CONVERGED} between the two largest budgets.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
