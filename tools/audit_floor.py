"""Which g8-01 cells were measured BETWEEN TWO FAILURES?

g7-04 already taught this lesson: its largest margin, +0.249 at seq 1536, sat
between two arms both at or below the trivial floor, and the honest figure was
+0.036. The project wrote that down.

g8-01 then computed recovery = (arm - none) / (oracle - none) at every length
without checking whether `none` was above the floor at all. If it is not, the
denominator is the distance between a working ceiling and a BROKEN floor, and
the ratio is not a recovery of anything.

Worse, the summariser picks the learning rate that MAXIMISES that gap -- and a
broken floor maximises it. So the selection rule actively prefers the cells where
the floor arm has failed.

Trivial floor for MQAR with n_pairs=4, n_values=8:
    1/n_pairs + (1 - 1/n_pairs)/n_values = 0.25 + 0.09375 = 0.34375
"""
import glob
import json
from collections import defaultdict

FLOOR = 1 / 4 + (1 - 1 / 4) / 8
BASE = ("C:/Users/John/AppData/Local/Temp/claude/D--repos-submenu/"
        "c14e22fe-06ee-43a1-83b7-05ae6d95924b/scratchpad/g801/*/*.json")

rows = [r for f in glob.glob(BASE) for r in json.load(open(f))]
cells = defaultdict(dict)
for r in rows:
    cells[(r["seq_len"], r["half_life"], r["lr"], r["arm"])][r["seed"]] = \
        r["accuracy"]

print(f"trivial floor = {FLOOR:.5f}\n")
print(f"{'seq_len':>8}{'half':>7}{'lr':>6}{'none (per seed)':>26}"
      f"{'mean':>8}{'above floor?':>14}")

usable = defaultdict(list)
for seq_len in sorted({k[0] for k in cells}):
    for half in sorted({k[1] for k in cells}, reverse=True):
        for lr in sorted({k[2] for k in cells}):
            by_seed = cells.get((seq_len, half, lr, "none"), {})
            if not by_seed:
                continue
            values = [by_seed[s] for s in sorted(by_seed)]
            mean = sum(values) / len(values)
            ok = mean > FLOOR
            usable[seq_len].append(ok)
            print(f"{seq_len:>8}{half:>7}{lr:>6}"
                  f"{'/'.join(f'{v:.3f}' for v in values):>26}"
                  f"{mean:>8.3f}{('yes' if ok else 'NO'):>14}")

print("\nlengths where ANY cell has a working floor arm:")
for seq_len in sorted(usable):
    n_ok = sum(usable[seq_len])
    verdict = "usable" if n_ok else "**EVERY CELL BROKEN**"
    print(f"  seq_len {seq_len:>5}: {n_ok}/{len(usable[seq_len])} cells  {verdict}")
