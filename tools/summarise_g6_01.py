"""Where does forgetting stop, and does sparsity move it?

The figure of merit is RETAINED ABSOLUTE accuracy on task A, pre-registered
before the run. A fraction-kept ranking would favour whichever arm learned least
in the first place: an arm scoring 0.20 and keeping all of it would top the table
while being useless.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

sys.path.insert(0, ".")
from tools.grid import pinned  # noqa: E402

HELD = 0.9      # "forgetting has stopped" -- 90% of what was learned survives


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    widths = sorted({r["d_model"] for r in rows})
    actives = sorted({r["key_active"] for r in rows})
    rates = sorted({r["lr"] for r in rows})

    print()
    print("RETAINED ABSOLUTE ACCURACY ON A, after training on B "
          "(each arm at its own best lr)")
    print(f"{'width':>7}{'key_active':>11}{'A before':>10}{'A after':>9}"
          f"{'kept':>7}{'B':>7}{'lr':>7}   per seed (A after)")
    chosen, table = [], {}
    for width in widths:
      for active in actives:
        best = None
        for lr in rates:
            got = [r for r in rows if r["d_model"] == width
                   and r["key_active"] == active and r["lr"] == lr]
            if not got:
                continue
            after = sum(r["a_after"] for r in got) / len(got)
            if best is None or after > best[0]:
                best = (after,
                        sum(r["a_before"] for r in got) / len(got),
                        sum(r["b_after"] for r in got) / len(got), lr,
                        sorted(round(r["a_after"], 3) for r in got))
        if best is None:
            continue
        chosen.append(best[3])
        table[(width, active)] = best[0]
        label = "dense" if active == 0 else str(active)
        kept = best[0] / best[1] if best[1] else 0.0
        print(f"{width:>7}{label:>11}{best[1]:>10.3f}{best[0]:>9.3f}"
              f"{kept:>7.2f}{best[2]:>7.3f}{best[3]:>7}   {best[4]}")

    print()
    print("BEST ARM AT EACH WIDTH, on retained absolute accuracy")
    sparsity_ever_wins = False
    for width in widths:
        row = {a: v for (w, a), v in table.items() if w == width}
        if not row:
            continue
        best_active = max(row, key=row.get)
        margin = row[best_active] - row.get(0, 0.0)
        if best_active != 0 and margin > 0.02:
            sparsity_ever_wins = True
        label = "dense" if best_active == 0 else f"key_active={best_active}"
        print(f"  width {width:>4}: {label:<16} {row[best_active]:.3f}"
              f"   (dense {row.get(0, float('nan')):.3f}, margin {margin:+.3f})")

    print()
    if sparsity_ever_wins:
        print("  Sparsity beats dense by more than 0.02 at some width, so it "
              "buys retention width alone did not -- which matters, because "
              "capacity is what G5 says we run out of.")
    else:
        print("  Dense wins or ties everywhere. Sparsity does not pay for task "
              "switching either: capacity already solves the problem before "
              "sparsity gets a chance to.")

    print()
    message = pinned(chosen, rates)
    print(f"  LEARNING-RATE GRID: {message}" if message
          else "  LEARNING-RATE GRID: contained its answer.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
