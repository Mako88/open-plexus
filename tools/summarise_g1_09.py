"""Report the crossing width for each alphabet size, and how it scales."""

from __future__ import annotations

import glob
import json
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
        by[(r["n_keys"], r["d_model"])].append(r["accuracy"])

    keys = sorted({k for k, _ in by})
    widths = sorted({w for _, w in by})

    print()
    print("mean accuracy by alphabet size and width")
    print(f"{'n_keys':>8}" + "".join(f"d={w}".rjust(9) for w in widths))
    crossings = {}
    for k in keys:
        cells = []
        for w in widths:
            accs = by.get((k, w))
            cells.append(f"{sum(accs)/len(accs):>9.3f}" if accs else f"{'-':>9}")
        print(f"{k:>8}" + "".join(cells))
        crossings[k] = next(
            (w for w in widths
             if (a := by.get((k, w))) and sum(a) / len(a) >= SOLVED), None)

    print()
    print(f"crossing width (mean >= {SOLVED}) by alphabet size:")
    for k in keys:
        c = crossings[k]
        print(f"  n_keys={k:<5} -> {c if c else 'not reached in range'}")

    known = [(k, c) for k, c in crossings.items() if c]
    if len(known) >= 2:
        (k0, c0), (k1, c1) = known[0], known[-1]
        print()
        print(f"  alphabet x{k1/k0:.0f} -> width x{c1/c0:.1f}")
        print("  linear would be an equal multiple; less is sub-linear and is")
        print("  the good news for any large-vocabulary target.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
