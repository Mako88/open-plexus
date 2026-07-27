"""Does a capacity over bindings beat a span over steps, and does direction matter?

Three tables, because there are three separable questions and reading them off one
grid is how a summariser ends up answering the easy one.

**Is the cliff gone?** The tag against delay. A window's recovery is a step
function of the delay it was not told
([g9-03](../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt)); the tag's
reach is a fade rather than a span, so its row should be flat or nearly so. A
tag row with a cliff in it is a soft window and should be called one.

**Does the signal matter, or only the capacity?** `tag` against
`tag-strongest` at every cell. Identical scores mean g9-04's inversion is
decoration and the mechanism is a capacity argument wearing a signal's clothes —
which would refute the reason this was built while the headline number still
looked fine.

**Does it beat the incumbent?** The tag against `reward` at the fixed reach of 8
that g9-02 used, so these cells are comparable with that sweep rather than only
with each other.

Uses the shared refusals in `tools/recovery.py`: no ratio when the floor arm is
at or below the trivial floor, and none when the oracle's advantage does not beat
the seed spread. Both have already cost this project a result.
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

    cells = by_cell(rows, "slots", "fade", "delay")
    slots = sorted({r["slots"] for r in rows})
    fades = sorted({r["fade"] for r in rows}, reverse=True)
    delays = sorted({r["delay"] for r in rows})
    floor = REWARD_RECALL_FLOOR

    def cell(s, f, d):
        return assess(cells, (s, f, d), ARMS, floor)

    print(f"trivial floor {floor:.3f};  recovery is (arm - none) / "
          f"(oracle - none)\n")

    for arm in ("tag", "tag-strongest", "reward"):
        print(f"== {arm} ==")
        header = f"{'slots':>6}{'fade':>7}" + "".join(
            f"{'delay ' + str(d):>10}" for d in delays)
        print(header)
        for s in slots:
            for f in fades:
                line = f"{s:>6}{f:>7}"
                for d in delays:
                    got = cell(s, f, d)
                    line += ("   missing" if got is None
                             else got.text(arm, 10))
                print(line)
        print()

    print("== refusals ==")
    quiet = True
    for s in slots:
        for f in fades:
            for d in delays:
                got = cell(s, f, d)
                if got is None:
                    print(f"  slots {s} fade {f} delay {d}: no records")
                    quiet = False
                elif got.refused:
                    print(f"  slots {s} fade {f} delay {d}: {got.refused}")
                    quiet = False
    if quiet:
        print("  none")

    print("\n== the three questions ==")
    best = best_by((((s, f, d), cell(s, f, d))
                    for s in slots for f in fades for d in delays), "tag")
    if best is None:
        print("  no cell survived the refusals, so there is no best cell")
        return 0
    (s, f, d), got = best
    print(f"  best tag cell: slots {s} fade {f} delay {d} at "
          f"{got.ratios['tag']:.2f}")

    flat = []
    for s in slots:
        for f in fades:
            values = [cell(s, f, d) for d in delays]
            usable = [c.ratios["tag"] for c in values
                      if c is not None and c.refused is None]
            if len(usable) == len(delays):
                flat.append((max(usable) - min(usable), s, f, usable))
    if flat:
        spread, s, f, usable = min(flat)
        print(f"  flattest tag row across delay: slots {s} fade {f}, "
              f"spread {spread:.2f} over {[f'{v:.2f}' for v in usable]}")
        print("  a flat row is the claim; a cliff in it means the fade is a "
              "window with extra arithmetic")
    else:
        print("  no (slots, fade) row survived the refusals at every delay, so "
              "flatness cannot be read")

    gaps = [got.ratios["tag"] - got.ratios["tag-strongest"]
            for s in slots for f in fades for d in delays
            if (got := cell(s, f, d)) is not None and got.refused is None]
    if gaps:
        print(f"  weakest minus strongest: mean {sum(gaps) / len(gaps):+.3f}, "
              f"worst {min(gaps):+.3f}, best {max(gaps):+.3f}")
        print("  near zero everywhere means the capacity is doing the work and "
              "the signal is decoration")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
