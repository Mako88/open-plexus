"""Does the tag's best capacity track the node's width?

The table is **node x slots**, one per delay, and the question is where each row
peaks. [g9-09](../experiments/sweeps/g9-09-a-small-node-in-a-wide-network.txt)
measured recovery falling from +0.16 at node 16 to +0.11 at node 8 with `slots`
frozen at 32; a pre-dispatch control then had `slots` 8 recovering 0.48 at node 8
against 32's 0.30, so those cells measured a mistuned tag.

Retrieval goes as `sqrt(d / N)` and `N` is exactly what the tag bounds, so the
capacity that pays should scale with the width the node reads through. **If the
peak tracks the node, the tag has a tuning rule instead of a constant** — and
every node can set it from something it already knows about itself.

Three things are reported, because merging them would hide the one that matters:

- **where each row peaks**, against the node's own width
- **what the peak is worth**, which is whether g9-09's decline survives retuning
- **the spread between delays at the peak**, which is whether the tag's central
  property belongs to the mechanism or to one setting

`tools/grid.py` runs on the chosen capacities, because a peak at an edge is a
bound and this whole run exists because a frozen value was believed.
"""

from __future__ import annotations

from tools.grid import pinned
from tools.recovery import REWARD_RECALL_FLOOR, assess, by_cell, load

ARMS = ("none", "oracle", "reward", "tag", "tag-strongest", "combined")


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "node_width", "slots", "delay")
    nodes = sorted({r["node_width"] for r in rows}, reverse=True)
    slots = sorted({r["slots"] for r in rows})
    delays = sorted({r["delay"] for r in rows})
    floor = REWARD_RECALL_FLOOR

    def cell(n, s, d):
        return assess(cells, (n, s, d), ARMS, floor)

    print(f"trivial floor {floor:.3f}\n")

    for delay in delays:
        for arm in ("tag", "combined", "tag-strongest", "reward"):
            print(f"== {arm}, delay {delay} ==")
            print(f"{'node':>6}" + "".join(f"{'slots ' + str(s):>10}"
                                           for s in slots) + f"{'best':>8}")
            for n in nodes:
                got = [cell(n, s, delay) for s in slots]
                line = f"{n:>6}" + "".join(
                    "   missing" if c is None else c.text(arm, 10) for c in got)
                usable = [(c.ratios[arm], s) for c, s in zip(got, slots)
                          if c is not None and c.refused is None]
                line += f"{max(usable)[1]:>8}" if usable else f"{'--':>8}"
                print(line)
            print()

    print("== DOES THE PEAK TRACK THE NODE? ==")
    print(f"{'node':>6}{'delay':>7}{'best slots':>12}{'recovery':>10}"
          f"{'node/best':>11}")
    chosen = []
    for n in nodes:
        for delay in delays:
            usable = [(cell(n, s, delay).ratios["tag"], s) for s in slots
                      if cell(n, s, delay) is not None
                      and cell(n, s, delay).refused is None]
            if not usable:
                print(f"{n:>6}{delay:>7}   every capacity refused or missing")
                continue
            value, best = max(usable)
            chosen.append(best)
            print(f"{n:>6}{delay:>7}{best:>12}{value:>10.2f}{n / best:>11.2f}")
    print("  node/best near 1 at every row -> the best capacity IS the node's")
    print("    width, and the tag tunes itself from sqrt(d/N)")
    print("  node/best varying -> there is no rule, and the constant has to be")
    print("    found per node size, which is a much weaker result")

    if chosen:
        verdict = pinned(chosen, slots)
        print(f"\n  grid check on the chosen capacities: "
              f"{verdict or 'interior, so these are values'}")

    print("\n== DOES THE DECLINE SURVIVE RETUNING? ==")
    print("g9-09 had +0.16 at node 16 and +0.11 at node 8 with slots frozen at")
    print("32. At each node's OWN best capacity:")
    for n in nodes:
        best_per_delay = []
        for delay in delays:
            usable = [cell(n, s, delay).ratios["tag"] for s in slots
                      if cell(n, s, delay) is not None
                      and cell(n, s, delay).refused is None]
            if usable:
                best_per_delay.append(max(usable))
        if len(best_per_delay) == len(delays):
            spread = max(best_per_delay) - min(best_per_delay)
            print(f"  node {n:>3}: {sum(best_per_delay) / len(delays):+.2f} "
                  f"mean, spread across delay {spread:.2f}"
                  f"{'   FLATNESS LOST' if spread > 0.10 else ''}")
        else:
            print(f"  node {n:>3}: not every delay usable")
    print("  flat across node -> the decline was a mistuned tag, and g9-09's")
    print("    conclusion about small nodes needs correcting")
    print("  still declining -> the decline is real and the working point was")
    print("    not the problem")

    print("\n== refusals ==")
    quiet = True
    for n in nodes:
        for s in slots:
            for d in delays:
                got = cell(n, s, d)
                if got is None:
                    print(f"  node {n} slots {s} delay {d}: no records")
                    quiet = False
                elif got.refused:
                    print(f"  node {n} slots {s} delay {d}: {got.refused}")
                    quiet = False
    if quiet:
        print("  none")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
