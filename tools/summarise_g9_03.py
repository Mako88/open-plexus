"""Does the cliff follow the gate's reach, and is there an interior best?

[g9-02](../experiments/sweeps/g9-02-a-gate-that-reads-its-own-input.txt) fixed
the reach at 8 and found recovery of 0.21/0.20/0.23 at delays 1, 4, 8 and
**-0.13** at 20. This sweeps the reach, so the table is **window x delay** and
the diagonal is where reach just covers the delay.

Two readings, and they want different next projects:

- **Cliff on the diagonal, with an interior best** — reach must be *matched* to a
  delay nobody knows in advance, and a tag (which marks one binding rather than a
  span) is a mechanism.
- **Large window always fine** — reach is free if affordable, and a tag is a cost
  optimisation for tiny nodes rather than a capability.

The two refusals come from `tools/recovery.py` rather than being copied here: no
ratio when the floor arm is at or below the trivial floor, and none when the
oracle's advantage does not beat the seed spread. Both have already cost this
project a result, and five hand-copies had already drifted before the shared
version existed.
"""

from __future__ import annotations

from tools.recovery import (
    REWARD_RECALL_FLOOR, assess, best_by, by_cell, load)

ARMS = ("none", "oracle", "on-use", "salience", "reward")


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "window", "delay")
    windows = sorted({r["window"] for r in rows})
    delays = sorted({r["delay"] for r in rows})

    def cell(window: int, delay: int):
        return assess(cells, (window, delay), ARMS, REWARD_RECALL_FLOOR)

    print(f"\ntrivial floor {REWARD_RECALL_FLOOR:.3f}")
    print("\n=== RECOVERY of the reward gate, by reach and delay ===")
    print("Rows are how far the gate can reach; columns are how far back the")
    print("binding is. On and above the diagonal the reach covers the delay.")
    print(f"{'window':>8}" + "".join(f"{d:>9}" for d in delays) + "   best delay")
    for window in windows:
        row = [(delay, cell(window, delay)) for delay in delays]
        # `missing` and `undefined` are printed differently on purpose: a job
        # that did not return is a dispatch failure, and a cell that cannot be
        # interpreted is a result. The hand-rolled version printed both as
        # "undefined", which hides the first inside the second.
        text = "".join("  missing" .rjust(9) if got is None else got.text("reward")
                       for _, got in row)
        best = best_by(row, "reward")
        print(f"{window:>8}{text}" + (f"{best[0]:>12}" if best else "         n/a"))

    print("\n=== IS THERE AN INTERIOR BEST? ===")
    print("For each delay, the reach that recovers most. If it sits at roughly")
    print("the delay, reach must be MATCHED and a tag is a mechanism. If it is")
    print("always the largest window, reach is free and a tag is a cost saving.")
    print(f"{'delay':>7}{'best window':>13}{'recovery':>10}"
          f"{'at largest window':>19}")
    for delay in delays:
        best = best_by(((window, cell(window, delay)) for window in windows),
                       "reward")
        if best is None:
            print(f"{delay:>7}   every cell undefined or missing")
            continue
        window, got = best
        largest = cell(windows[-1], delay)
        largest_text = ("missing" if largest is None
                        else f"{largest.ratios['reward']:.2f}"
                        if largest.refused is None else "undefined")
        print(f"{delay:>7}{window:>13}{got.ratios['reward']:>10.2f}"
              f"{largest_text:>19}")

    print("\n=== REFUSALS ===")
    quiet = True
    for window in windows:
        for delay in delays:
            got = cell(window, delay)
            if got is None:
                print(f"  window {window} delay {delay}: no records")
                quiet = False
            elif got.refused:
                print(f"  window {window} delay {delay}: {got.refused}")
                quiet = False
    if quiet:
        print("  none")

    print("\nbest window ~ delay        -> reach must be matched to an unknown")
    print("                              lag. Build the tag; it is a mechanism.")
    print("best window always largest -> reach is free if affordable. The tag is")
    print("                              a cost optimisation for tiny nodes.")
    print("nothing positive anywhere  -> the gate cannot use a late signal, and")
    print("                              g9-02's 0.2 was about adjacency.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
