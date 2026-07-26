"""Report the price in BOTH units: width, and working memory."""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

SOLVED = 0.9
LOCAL_CROSSINGS = {48: 21.8, 96: 26.0, 192: 34.0, 384: 47.5}


def interpolated_crossing(scores: dict[int, float]) -> float | None:
    widths = sorted(scores)
    for a, b in zip(widths, widths[1:]):
        if scores[a] < SOLVED <= scores[b]:
            return a + (SOLVED - scores[a]) / (scores[b] - scores[a]) * (b - a)
    return widths[0] if scores and scores[widths[0]] >= SOLVED else None


def main() -> int:
    pattern = sys.argv[1] if len(sys.argv) > 1 else "out/*.json"
    rows = [r for f in glob.glob(pattern) for r in json.load(open(f))]
    if not rows:
        print(f"no records matched {pattern}")
        return 1

    by = defaultdict(list)
    for r in rows:
        by[(r["seq_len"], r["d_model"])].append(r["accuracy"])
    lengths = sorted({s for s, _ in by})
    widths = sorted({w for _, w in by})

    print()
    print("attention: mean accuracy by sequence length and width")
    print(f"{'seq_len':>9}" + "".join(f"d={w}".rjust(9) for w in widths))
    crossings = {}
    for s in lengths:
        scores = {w: sum(by[(s, w)]) / len(by[(s, w)])
                  for w in widths if (s, w) in by}
        print(f"{s:>9}" + "".join(
            f"{scores[w]:>9.3f}" if w in scores else f"{'-':>9}" for w in widths))
        crossings[s] = interpolated_crossing(scores)

    print()
    print("THE PRICE IN BOTH UNITS")
    print(f"{'seq_len':>9}{'d_local':>9}{'d_att':>8}{'width x':>10}"
          f"{'local mem':>11}{'att mem':>10}{'memory x':>11}")
    for s in lengths:
        dl, da = LOCAL_CROSSINGS.get(s), crossings[s]
        if dl is None or da is None:
            print(f"{s:>9}{'-':>9}{'-':>8}{'-':>10}{'-':>11}{'-':>10}{'-':>11}")
            continue
        lm, am = dl * dl, 2 * s * da
        print(f"{s:>9}{dl:>9.1f}{da:>8.1f}{dl/da:>9.1f}x"
              f"{lm:>11.0f}{am:>10.0f}{am/lm:>10.2f}x")
    print()
    print("  width x   : how much wider the local rule must be. >1 favours attention")
    print("  memory x  : attention working state over local. >1 favours the LOCAL rule")
    print()
    print("  attention holds keys and values for every position (2*T*d).")
    print("  the local rule holds one d*d matrix whatever T is.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
