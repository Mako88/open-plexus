"""Report each width at its BEST scale, for both models.

Kept as a script rather than inlined in the workflow YAML so that it can be run
against downloaded artifacts locally, and so the aggregation logic is under the
same review as everything else.
"""

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
        by[(r["mode"], r["d_model"], r["scale"])].append(r["accuracy"])

    for mode in ("local", "attention"):
        widths = sorted({w for m, w, _ in by if m == mode})
        scales = sorted({s for m, _, s in by if m == mode})
        if not widths:
            continue
        print()
        print(f"=== {mode} : mean accuracy by width and scale ===")
        print(f"{'width':>7}" + "".join(f"{s:>9}" for s in scales) + f"{'BEST':>9}")
        for w in widths:
            cells, best = [], 0.0
            for s in scales:
                accs = by.get((mode, w, s))
                if accs:
                    mean = sum(accs) / len(accs)
                    best = max(best, mean)
                    cells.append(f"{mean:>9.3f}")
                else:
                    cells.append(f"{'-':>9}")
            print(f"{w:>7}" + "".join(cells) + f"{best:>9.3f}")

        crossing = next((w for w in widths
                         if max((sum(by[(mode, w, s)]) / len(by[(mode, w, s)])
                                 for s in scales if (mode, w, s) in by),
                                default=0.0) >= SOLVED), None)
        print(f"  crossing at best scale (>= {SOLVED}): "
              f"{crossing if crossing else 'not reached'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
