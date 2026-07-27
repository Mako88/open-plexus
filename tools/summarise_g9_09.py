"""Does an implementable gate get BETTER as the node gets smaller?

The table is **node width x delay**, where the node holds `width // partitions`
of a network that stays 64 wide. That distinction is the whole point:
[g9-08](../experiments/sweeps/g9-08-how-small-a-node-can-run-the-gate.txt) swept
the network instead and refused nine of fifteen cells because the task became
impossible.

Three things are reported separately because a single headline merges them:

- **height** — how much of the oracle's advantage the gate recovers
- **flatness across delay** — the property that makes the tag a gate rather than
  a saving, and which splitting a readout should not touch
- **the ceiling** — `oracle` itself, because the pre-dispatch control has it
  falling to 0.608 at a node of four. `tools/recovery.py` refuses on the FLOOR
  arm and on the seed spread; **it does not refuse a broken ceiling**, so a cell
  whose oracle has collapsed still prints a number, and that number is a fraction
  of an advantage that is itself failing.

That last one is registered as prediction 2 in the sweep file, and this prints
the oracle column beside every ratio so nobody has to go looking for it.
"""

from __future__ import annotations

from tools.recovery import (
    REWARD_RECALL_FLOOR, assess, best_by, by_cell, load)

ARMS = ("none", "oracle", "reward", "tag", "tag-strongest")
#: Below this the ceiling is not doing the task either, so a ratio taken against
#: it is arithmetic on two failures. Not a refusal -- a warning printed loudly,
#: because the refusals live in tools/recovery.py and are about the floor.
CEILING = 0.9


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "node_width", "delay")
    nodes = sorted({r["node_width"] for r in rows}, reverse=True)
    delays = sorted({r["delay"] for r in rows})
    floor = REWARD_RECALL_FLOOR

    def cell(n, d):
        return assess(cells, (n, d), ARMS, floor)

    print(f"trivial floor {floor:.3f}; network width "
          f"{sorted({r['width'] for r in rows})}\n")

    for arm in ("tag", "reward", "tag-strongest"):
        print(f"== {arm} ==")
        print(f"{'node':>6}" + "".join(f"{'delay ' + str(d):>10}" for d in delays)
              + f"{'spread':>9}")
        for n in nodes:
            got = [cell(n, d) for d in delays]
            line = f"{n:>6}" + "".join(
                "   missing" if c is None else c.text(arm, 10) for c in got)
            usable = [c.ratios[arm] for c in got
                      if c is not None and c.refused is None]
            line += (f"{max(usable) - min(usable):>9.2f}"
                     if len(usable) == len(delays) else f"{'--':>9}")
            print(line)
        print()

    print("== the floor and the ceiling, which decide whether a cell counts ==")
    print(f"{'node':>6}{'delay':>7}{'none':>9}{'oracle':>9}{'gap':>8}   verdict")
    for n in nodes:
        for d in delays:
            got = cell(n, d)
            if got is None:
                print(f"{n:>6}{d:>7}   missing")
                continue
            notes = []
            if got.means["none"] <= floor:
                notes.append("FLOOR BROKEN")
            if got.means["oracle"] < CEILING:
                notes.append("CEILING BROKEN -- ratio is a fraction of a "
                             "failing advantage")
            print(f"{n:>6}{d:>7}{got.means['none']:>9.3f}"
                  f"{got.means['oracle']:>9.3f}{got.gap:>8.3f}   "
                  f"{'; '.join(notes) if notes else 'ok'}")

    print("\n== refusals ==")
    quiet = True
    for n in nodes:
        for d in delays:
            got = cell(n, d)
            if got is None:
                print(f"  node {n} delay {d}: no records")
                quiet = False
            elif got.refused:
                print(f"  node {n} delay {d}: {got.refused}")
                quiet = False
    if quiet:
        print("  none")

    print("\n== does it get better as the node shrinks? ==")
    for n in nodes:
        got = [cell(n, d) for d in delays]
        usable = [c for c in got if c is not None and c.refused is None]
        if len(usable) != len(delays):
            print(f"  node {n:>3}: not every delay usable")
            continue
        tag = [c.ratios["tag"] for c in usable]
        direction = [c.ratios["tag"] - c.ratios["tag-strongest"] for c in usable]
        ceiling = min(c.means["oracle"] for c in usable)
        flag = "" if ceiling >= CEILING else "   (CEILING BROKEN, do not quote)"
        print(f"  node {n:>3}: tag mean {sum(tag) / len(tag):+.2f}, "
              f"spread {max(tag) - min(tag):.2f}, "
              f"direction worth {sum(direction) / len(direction):+.2f}{flag}")
    print("  rising as the node shrinks -> the mechanism is better on the")
    print("    hardware this project is FOR than on the hardware it was")
    print("    measured on. Check that hardest, it is the best outcome available")
    print("  direction growing as the node shrinks -> a small node is a fourth")
    print("    kind of scarcity, and g9-04's signal pays under all of them")

    best = best_by((((n, d), cell(n, d)) for n in nodes for d in delays), "tag")
    if best is not None:
        (n, d), got = best
        print(f"\n  best tag cell: node {n} delay {d} at {got.ratios['tag']:.2f}"
              f" (oracle {got.means['oracle']:.3f})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
