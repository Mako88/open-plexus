"""Do tiny devices forget, and does clustering protect them?

Total width is pinned at 240 and every device holds one dimension, so anything
here is about PER-DEVICE width alone -- the confound g6-01 cannot separate.

The figure of merit is retained ABSOLUTE accuracy on task A, as in g6-01: a
fraction-kept ranking rewards whichever arm learned least, and would crown an arm
that scored 0.02 and held all of it.
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

    clusters = sorted({r["cluster"] for r in rows})
    rates = sorted({r["lr"] for r in rows})
    modes = sorted({r["mode"] for r in rows})

    chosen, retained = defaultdict(list), {}
    for mode in modes:
        print()
        print(f"=== {mode} : devices of ONE dimension, by cluster size ===")
        print(f"{'cluster':>8}{'A before':>10}{'A after':>9}{'kept':>7}"
              f"{'B':>7}{'lr':>7}   per seed (A after)")
        best_lr, best_rows = None, None
        for lr in rates:
            got = [r for r in rows if r["mode"] == mode and r["lr"] == lr]
            if not got:
                continue
            # Rank on the largest cluster, the arm's own best case, so the rate
            # is not chosen to flatter one cluster size.
            top = max(r["a_after"] for r in got if r["cluster"] == max(clusters))
            if best_lr is None or top > best_rows:
                best_lr, best_rows = lr, top
        chosen[mode].append(best_lr)
        for cluster in clusters:
            got = [r for r in rows if r["mode"] == mode and r["lr"] == best_lr
                   and r["cluster"] == cluster]
            if not got:
                continue
            before = sum(r["a_before"] for r in got) / len(got)
            after = sum(r["a_after"] for r in got) / len(got)
            b_after = sum(r["b_after"] for r in got) / len(got)
            kept = after / before if before else 0.0
            per_seed = sorted(round(r["a_after"], 3) for r in got)
            print(f"{cluster:>8}{before:>10.3f}{after:>9.3f}{kept:>7.2f}"
                  f"{b_after:>7.3f}{best_lr:>7}   {per_seed}")
            retained[(mode, cluster)] = (before, after, b_after)

    print()
    print("DOES CLUSTERING PROTECT AGAINST FORGETTING?")
    for mode in modes:
        alone = retained.get((mode, min(clusters)))
        pooled = retained.get((mode, max(clusters)))
        if not (alone and pooled):
            continue
        print(f"  {mode:<6}: one device keeps {alone[1]:.3f} of {alone[0]:.3f}; "
              f"{max(clusters)} devices keep {pooled[1]:.3f} of {pooled[0]:.3f}")
        if pooled[1] > alone[1] + 0.05 and pooled[1] > 0.5:
            print("           Clustering PROTECTS -- pooling recovers what a "
                  "lone device lost, so total width governs forgetting and "
                  "tiny devices survive a task switch.")
        elif pooled[1] < 0.1:
            print("           Everything forgot. Per-device width governs "
                  "forgetting: a cluster pools members that have each been "
                  "overwritten, and pooling forgotten answers gives a forgotten "
                  "answer. Tiny devices cannot hold two bodies of data.")
        else:
            print("           Partial. Read the curve rather than the ends.")

    print()
    for mode in modes:
        message = pinned(chosen[mode], rates)
        print(f"  LEARNING-RATE GRID, {mode}: " + (message or "contained its answer."))

    print()
    for mode in modes:
        b = retained.get((mode, max(clusters)))
        if b and b[2] < 0.5:
            print(f"  CONTROL FAILED, {mode}: task B ends at {b[2]:.3f}, so this "
                  f"arm never learned the second task and its retention number "
                  f"means nothing.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
