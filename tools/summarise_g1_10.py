"""Report the crossing width per sequence length, and the scaling exponent."""

from __future__ import annotations

import glob
import json
import math
import sys
from collections import defaultdict

SOLVED = 0.9


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
    print("mean accuracy by sequence length and width")
    print(f"{'seq_len':>9}{'bindings':>10}" + "".join(f"d={w}".rjust(9) for w in widths))
    crossings = {}
    for s in lengths:
        cells = []
        for w in widths:
            accs = by.get((s, w))
            cells.append(f"{sum(accs)/len(accs):>9.3f}" if accs else f"{'-':>9}")
        print(f"{s:>9}{s-1:>10}" + "".join(cells))
        crossings[s] = next(
            (w for w in widths
             if (a := by.get((s, w))) and sum(a) / len(a) >= SOLVED), None)

    print()
    print(f"crossing width (mean >= {SOLVED}):")
    for s in lengths:
        c = crossings[s]
        print(f"  seq_len={s:<5} -> {c if c else 'not reached in range'}")

    known = [(s, c) for s, c in crossings.items() if c]
    if len(known) >= 2:
        (s0, c0), (s1, c1) = known[0], known[-1]
        if c1 != c0 and s1 != s0:
            exponent = math.log(c1 / c0) / math.log(s1 / s0)
            print()
            print(f"  seq_len x{s1/s0:.0f} -> width x{c1/c0:.1f}   "
                  f"exponent ~{exponent:.2f}")
            print("  1.0 is linear; below is sub-linear and better news; 0 is flat")
        else:
            print()
            print(f"  seq_len x{s1/s0:.0f} -> width x{c1/c0:.1f}  (FLAT: a third "
                  "explanation has failed)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
