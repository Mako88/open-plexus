"""Does the union need a long reach, or does it only need a short one?

The table is **reach x delay**, and the comparison that matters is `combined`
against `reward` **in the same cell** — both use the same window, so the
difference is exactly what the tag's marks add.

[g9-10](../experiments/sweeps/g9-10-does-the-best-capacity-track-the-node.txt)
measured combined at +0.26 against the window's +0.23 with the reach frozen at 8.
A counting control ranks them the other way round, so whatever the union adds is
in WHICH writes it keeps rather than how many — which is why this needs training
and not another count.

Two wiring checks are free and are printed as such:

- **reach 8 reproduces every earlier combined cell.** If it does not, something
  about the arms changed when the reach became a parameter.
- **the `tag` row must be flat across reach**, because the tag arm never reads
  it. If it moves, the arms share state and no other row counts.

Refusals come from `tools/recovery.py`. Delay 1 is a control on note 027's leak
— the task has a trivial exact solution there — and is labelled rather than
quietly averaged in.
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

    cells = by_cell(rows, "reach", "delay")
    reaches = sorted({r["reach"] for r in rows})
    delays = sorted({r["delay"] for r in rows})
    floor = REWARD_RECALL_FLOOR

    def cell(reach, delay):
        return assess(cells, (reach, delay), ARMS, floor)

    print(f"trivial floor {floor:.3f}\n")

    for arm in ("combined", "reward", "tag", "tag-strongest"):
        print(f"== {arm} ==")
        print(f"{'reach':>7}" + "".join(f"{'delay ' + str(d):>10}"
                                        for d in delays))
        for reach in reaches:
            got = [cell(reach, d) for d in delays]
            print(f"{reach:>7}" + "".join(
                "   missing" if c is None else c.text(arm, 10) for c in got))
        print()

    print("== WHAT THE TAG'S MARKS ADD: combined minus reward, same cell ==")
    print(f"{'reach':>7}" + "".join(f"{'delay ' + str(d):>10}" for d in delays))
    for reach in reaches:
        line = f"{reach:>7}"
        for d in delays:
            got = cell(reach, d)
            if got is None or got.refused:
                line += "   --".rjust(10)
            else:
                line += f"{got.ratios['combined'] - got.ratios['reward']:>+10.2f}"
        print(line)
    print("  positive at short reach -> the union is cheap and every combined")
    print("    cell so far paid eight writes a capture for nothing")
    print("  positive only at long reach -> the union adds SPAN, not signal,")
    print("    and the combined gate is a window with a longer arm")

    print("\n== WIRING CHECKS ==")
    tag_row = []
    for reach in reaches:
        usable = [cell(reach, d).ratios["tag"] for d in delays
                  if cell(reach, d) is not None and cell(reach, d).refused is None]
        if len(usable) == len(delays):
            tag_row.append(sum(usable) / len(usable))
    if len(tag_row) == len(reaches):
        spread = max(tag_row) - min(tag_row)
        print(f"  tag across reach: spread {spread:.3f} "
              f"{'ok' if spread < 0.03 else '*** THE TAG ARM MOVES WITH A DIAL '
                                           'IT DOES NOT READ ***'}")
    else:
        print("  tag across reach: not every reach usable at every delay")

    print("  reach 8 is the cell every earlier combined result used; compare it")
    print("  against g9-10's +0.26 at slots 4, node 32, delay 8.")

    best = []
    for d in delays:
        usable = [(cell(r, d).ratios["combined"], r) for r in reaches
                  if cell(r, d) is not None and cell(r, d).refused is None]
        if usable:
            value, reach = max(usable)
            best.append(reach)
            print(f"  best combined reach at delay {d}: {reach} ({value:+.2f})")
    if best:
        print(f"  grid check: {pinned(best, reaches) or 'interior, so these are values'}")

    print("\n== refusals ==")
    quiet = True
    for reach in reaches:
        for d in delays:
            got = cell(reach, d)
            if got is None:
                print(f"  reach {reach} delay {d}: no records")
                quiet = False
            elif got.refused:
                print(f"  reach {reach} delay {d}: {got.refused}")
                quiet = False
    if quiet:
        print("  none")
    print("\n  delay 1 is a CONTROL on note 027's leak, not evidence about")
    print("  short delays: the task has a trivial exact solution there.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
