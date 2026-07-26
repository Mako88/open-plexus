"""What cluster size do one-dimensional devices need, gated and ungated?

The quantity of interest is the smallest cluster reaching the bar, and whether it
grows with sequence length. A gated requirement that stays CONSTANT would be the
first quantity in this project that does not grow with difficulty -- so it is the
favourable outcome, and it gets checked rather than announced.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

sys.path.insert(0, ".")
from tools.grid import pinned  # noqa: E402

SOLVED = 0.9


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    lengths = sorted({r["seq_len"] for r in rows})
    clusters = sorted({r["cluster"] for r in rows})
    rates = sorted({r["lr"] for r in rows})
    modes = sorted({r["mode"] for r in rows})

    needed, chosen, spreads = {}, defaultdict(list), {}
    for mode in modes:
        print()
        print(f"=== {mode} : accuracy by cluster size, devices of ONE dimension ===")
        header = "".join(str(c).rjust(8) for c in clusters)
        print(f"{'seq_len':>8}{header}{'lr':>7}")
        for seq_len in lengths:
            best, best_lr = None, None
            for lr in rates:
                row = {}
                for c in clusters:
                    got = [r["accuracy"] for r in rows
                           if r["seq_len"] == seq_len and r["mode"] == mode
                           and r["lr"] == lr and r["cluster"] == c]
                    if got:
                        row[c] = sum(got) / len(got)
                if not row:
                    continue
                # Rank on the LARGEST cluster -- the arm's own best case -- so the
                # learning rate is not chosen to flatter one particular size.
                if best is None or row[max(row)] > best[max(best)]:
                    best, best_lr = row, lr
            if best is None:
                continue
            chosen[mode].append(best_lr)
            cells = "".join(f"{best.get(c, float('nan')):>8.3f}" for c in clusters)
            print(f"{seq_len:>8}{cells}{best_lr:>7}")
            passing = [c for c in sorted(best) if best[c] >= SOLVED]
            needed[(mode, seq_len)] = min(passing) if passing else None
            if passing:
                at = [r["accuracy"] for r in rows
                      if r["seq_len"] == seq_len and r["mode"] == mode
                      and r["lr"] == best_lr and r["cluster"] == min(passing)]
                spreads[(mode, seq_len)] = sorted(round(a, 3) for a in at)

    print()
    print("SMALLEST CLUSTER REACHING THE BAR")
    print(f"{'seq_len':>8}" + "".join(m.rjust(12) for m in modes))
    for seq_len in lengths:
        cells = []
        for mode in modes:
            value = needed.get((mode, seq_len))
            cells.append(("none" if value is None else str(value)).rjust(12))
        print(f"{seq_len:>8}" + "".join(cells))

    print()
    for (mode, seq_len), seeds in sorted(spreads.items()):
        if min(seeds) < SOLVED <= max(seeds):
            print(f"  STRADDLES THE BAR: {mode} seq={seq_len} at its smallest "
                  f"passing cluster gives {seeds} -- the mean clears {SOLVED} "
                  f"while individual seeds do not.")

    print()
    gated = [needed.get(("gated", s)) for s in lengths]
    if all(g is not None for g in gated):
        if len(set(gated)) == 1:
            print(f"  GATED REQUIREMENT IS CONSTANT at {gated[0]} devices across "
                  f"a {max(lengths) // min(lengths)}x range of sequence length.")
            print("  That would be the first quantity here that does not grow "
                  "with difficulty. Favourable, therefore suspect -- check the "
                  "per-seed spread above and that adjacent lengths are not one "
                  "grid step apart by luck.")
        else:
            print(f"  Gated requirement grows: {dict(zip(lengths, gated))}. The "
                  f"gate lowers the constant; it does not remove the growth.")
    else:
        print(f"  Gated requirement not located at every length: "
              f"{dict(zip(lengths, gated))}. Where it is None, the largest "
              f"cluster tested did not reach the bar, which is a bound and not "
              f"a value.")

    print()
    for mode in modes:
        message = pinned(chosen[mode], rates)
        print(f"  LEARNING-RATE GRID, {mode}: "
              + (message or "contained its answer."))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
