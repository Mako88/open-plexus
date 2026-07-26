"""How should a machine of a given capacity spend it?

Rows are machine capacity, columns are node width. A cell is the allocation that
spends that capacity on nodes of that width -- so reading across a row compares
the ways to spend one machine, which is the deployment decision.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

sys.path.insert(0, ".")
from tools.grid import pinned  # noqa: E402


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    widths = sorted({r["node_width"] for r in rows})
    caps = sorted({r["capacity"] for r in rows})
    rates = sorted({r["lr"] for r in rows})
    modes = sorted({r["mode"] for r in rows})

    chosen, best_at = defaultdict(list), {}
    for mode in modes:
        print()
        print(f"=== {mode} : accuracy by machine capacity and node width ===")
        print(f"{'capacity':>9}" + "".join(f"w={w}".rjust(9) for w in widths)
              + f"{'best':>13}{'spread':>8}")
        for capacity in caps:
            cells = {}
            for width in widths:
                top = None
                for lr in rates:
                    got = [r["accuracy"] for r in rows
                           if r["mode"] == mode and r["capacity"] == capacity
                           and r["node_width"] == width and r["lr"] == lr]
                    if got and (top is None or sum(got) / len(got) > top[0]):
                        top = (sum(got) / len(got), lr)
                if top:
                    cells[width] = top[0]
                    chosen[mode].append(top[1])
            if not cells:
                continue
            winner = max(cells, key=cells.get)
            spread = max(cells.values()) - min(cells.values())
            best_at[(mode, capacity)] = (winner, spread)
            line = "".join(f"{cells[w]:>9.3f}" if w in cells else f"{'-':>9}"
                           for w in widths)
            print(f"{capacity:>9}{line}{f'w={winner}':>13}{spread:>8.3f}")

    print()
    print("DOES IT MATTER HOW A MACHINE SPENDS ITS CAPACITY?")
    for mode in modes:
        spreads = [s for (m, _), (_, s) in best_at.items() if m == mode]
        winners = [w for (m, _), (w, _) in best_at.items() if m == mode]
        if not spreads:
            continue
        worst = max(spreads)
        print(f"  {mode:<6}: largest spread across allocations {worst:.3f}; "
              f"winning node width is "
              + ("always " + str(winners[0]) if len(set(winners)) == 1
                 else f"not constant -- {sorted(set(winners))}"))
        if worst < 0.05:
            print("           Allocation barely matters. Heterogeneous hardware "
                  "is fine and deployment needs no policy.")
        elif len(set(winners)) > 1:
            print("           The best allocation DEPENDS on capacity, so "
                  "deployment needs a rule rather than a constant.")

    print()
    for mode in modes:
        message = pinned(chosen[mode], rates)
        print(f"  LEARNING-RATE GRID, {mode}: " + (message or "contained its answer."))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
