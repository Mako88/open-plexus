"""Does an implementable gate survive being run on a small node?

[g7-02](../experiments/sweeps/g7-02-tiny-nodes-and-clusters.txt) found that with
an ORACLE gate, node size almost stops mattering — devices of one dimension score
identically to three decimals across four sequence lengths. That is a ceiling.
Every cell in the g9 line runs at `d_model` 32 in one process, so nothing there
says whether an implementable gate keeps any of it.

The table is **width x delay**, and there are two separate questions in it that a
single headline would merge:

- **height** — how much recovery survives as the node narrows
- **flatness across delay** — the property that makes the tag a gate rather than
  a saving, which should be a property of counting in bindings and therefore
  indifferent to width

A gate that keeps its flatness while losing its height is a working mechanism on
hardware too small for it. A gate that keeps its height and loses its flatness
has stopped being the mechanism that was measured. Those want different next
steps and must not be reported as one number.

The oracle row is the wiring check: g7-02 got near-identical scores at width 1,
so an oracle that degrades sharply here means this task differs from that sweep
in a way nothing has named, and no other row counts.

Refusals come from `tools/recovery.py`. At small width the floor arm is expected
to approach the trivial floor — and when it does, the cell is reporting that the
TASK became impossible, not that the gate failed.
"""

from __future__ import annotations

from tools.recovery import (
    REWARD_RECALL_FLOOR, assess, best_by, by_cell, load)

ARMS = ("none", "oracle", "reward", "tag", "tag-strongest")


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "width", "delay")
    widths = sorted({r["width"] for r in rows})
    delays = sorted({r["delay"] for r in rows})
    floor = REWARD_RECALL_FLOOR

    def cell(w, d):
        return assess(cells, (w, d), ARMS, floor)

    print(f"trivial floor {floor:.3f};  recovery is (arm - none) / "
          f"(oracle - none)\n")

    for arm in ("tag", "reward", "tag-strongest", "oracle"):
        print(f"== {arm} ==")
        print(f"{'width':>6}" + "".join(f"{'delay ' + str(d):>10}" for d in delays)
              + f"{'spread':>9}")
        for w in widths:
            got = [cell(w, d) for d in delays]
            line = f"{w:>6}" + "".join(
                "   missing" if c is None else c.text(arm, 10) for c in got)
            usable = [c.ratios[arm] for c in got
                      if c is not None and c.refused is None]
            line += (f"{max(usable) - min(usable):>9.2f}"
                     if len(usable) == len(delays) else f"{'--':>9}")
            print(line)
        print()

    print("== the floor arm, which is what decides whether a cell means anything ==")
    print(f"{'width':>6}" + "".join(f"{'delay ' + str(d):>10}" for d in delays))
    for w in widths:
        line = f"{w:>6}"
        for d in delays:
            got = cell(w, d)
            line += "   missing" if got is None else f"{got.means['none']:>10.3f}"
        print(line)
    print(f"  anything at or below {floor:.3f} is the TASK failing, not the gate")

    print("\n== refusals ==")
    quiet = True
    for w in widths:
        for d in delays:
            got = cell(w, d)
            if got is None:
                print(f"  width {w} delay {d}: no records")
                quiet = False
            elif got.refused:
                print(f"  width {w} delay {d}: {got.refused}")
                quiet = False
    if quiet:
        print("  none")

    print("\n== the two questions ==")
    best = best_by((((w, d), cell(w, d)) for w in widths for d in delays), "tag")
    if best is None:
        print("  every cell refused or missing; there is no best cell")
        return 0
    (w, d), got = best
    print(f"  best tag cell: width {w} delay {d} at {got.ratios['tag']:.2f}")

    for w in widths:
        got = [cell(w, dd) for dd in delays]
        usable = [c.ratios["tag"] for c in got
                  if c is not None and c.refused is None]
        if len(usable) != len(delays):
            continue
        height, spread = sum(usable) / len(usable), max(usable) - min(usable)
        verdict = ("flat" if spread < 0.10 else "CLIFFED")
        print(f"  width {w:>3}: mean {height:+.2f}, spread {spread:.2f} -> "
              f"{verdict}")
    print("  height falling while spread stays low -> the mechanism survives and")
    print("    is simply weaker on a small node, which is a tuning question")
    print("  spread rising -> it has stopped being a capacity over bindings, and")
    print("    the g9-06 result does not transfer to hardware of this size")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
