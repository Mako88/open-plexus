"""What did freezing the learning rate at 0.05 for seven sweeps cost?

The table is **node width x learning rate**, and it exists because `lr 0.05,
FIXED on every arm` appears in the grid of g9-05 through g9-11 without ever being
swept. It was chosen for g9-03 at `d_model` 32 and carried through every change
of width, node count, capacity, fade and reach since
([note 028](../docs/notes/028-the-learning-rate-has-been-frozen-for-seven-sweeps.md)).

Three questions, reported separately because a single headline merges them:

- **does the best rate move with node width** — if it does, one rate cannot be
  right for a grid that sweeps width
- **does the RATIO move**, which is the only one that matters. The rate is known
  to move the floor arm by a factor of three (g8-01). The ratio divides by the
  gap, so it can be stable while both ends move, and that would make the frozen
  constant a real risk with a negligible cost
- **does the ORDERING of arms move**. Note 028 asserted that ordinal findings
  survive a wrong rate because every arm in a cell shares it. That is an
  argument, not a measurement. This checks it, and it is the one that would hurt:
  a changed ordering invalidates comparisons, not just scales.

The floor and ceiling print beside every ratio for the same reason as g9-09:
`tools/recovery.py` refuses on the floor arm and on the seed spread, and **does
not refuse a broken ceiling**.
"""

from __future__ import annotations

from tools.recovery import (
    REWARD_RECALL_FLOOR, assess, best_by, by_cell, load, margin, winner)

ARMS = ("none", "oracle", "reward", "tag", "tag-strongest", "combined")
#: The rate every published number in the g9 line was taken at.
INCUMBENT = 0.05
#: Below this the ceiling is not doing the task either. Warned, not refused.
CEILING = 0.9
#: Prediction 2's threshold, pre-registered in the sweep file.
MATTERS = 0.05
#: Prediction 4's threshold for the floor arm moving with the rate.
FLOOR_MOVES = 0.15


def ordering(cell) -> tuple[str, ...]:
    """The arms best-first, which is what a comparison between arms rests on."""
    return tuple(sorted(cell.ratios, key=lambda a: -cell.ratios[a]))


def best_rate(usable: dict, arm: str):
    """`winner` with this sweep's incumbent, since every caller here uses it."""
    return winner(usable, arm, INCUMBENT)


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "node_width", "lr")
    nodes = sorted({r["node_width"] for r in rows}, reverse=True)
    rates = sorted({r["lr"] for r in rows})
    floor = REWARD_RECALL_FLOOR

    def cell(n, lr):
        return assess(cells, (n, lr), ARMS, floor)

    delays = sorted({r["delay"] for r in rows})
    print(f"trivial floor {floor:.3f}; delay {delays}; "
          f"network width {sorted({r['width'] for r in rows})}; "
          f"slots {sorted({r['slots'] for r in rows})}\n")
    if INCUMBENT not in rates:
        print(f"  !! {INCUMBENT} is not in {rates}: nothing to compare against\n")

    for arm in ("tag", "combined", "reward", "tag-strongest"):
        print(f"== {arm} ==")
        print(f"{'node':>6}" + "".join(f"{'lr ' + str(r):>10}" for r in rates)
              + f"{'best':>8}{'vs .05':>9}")
        for n in nodes:
            got = [cell(n, r) for r in rates]
            line = f"{n:>6}" + "".join(
                "   missing" if c is None else c.text(arm, 10) for c in got)
            usable = {r: c for r, c in zip(rates, got)
                      if c is not None and c.refused is None}
            won = best_rate(usable, arm) if usable else None
            if won is None:
                line += f"{'--':>8}{'--':>9}"
            elif won[1] <= won[2]:
                # A lead inside the seed spread is not a lead. Naming a winner
                # here is how three equal numbers become a trend.
                line += f"{'tied':>8}{'noise':>9}"
            else:
                line += f"{won[0]:>8}{won[1]:>+9.2f}"
            print(line)
        print()

    print("== the floor and the ceiling, which decide whether a cell counts ==")
    print(f"{'node':>6}{'lr':>7}{'none':>9}{'oracle':>9}{'gap':>8}   verdict")
    for n in nodes:
        for r in rates:
            got = cell(n, r)
            if got is None:
                print(f"{n:>6}{r:>7}   missing")
                continue
            notes = []
            if got.means["none"] <= floor:
                notes.append("FLOOR BROKEN")
            if got.means["oracle"] < CEILING:
                notes.append("CEILING BROKEN -- ratio is a fraction of a "
                             "failing advantage")
            print(f"{n:>6}{r:>7}{got.means['none']:>9.3f}"
                  f"{got.means['oracle']:>9.3f}{got.gap:>8.3f}   "
                  f"{'; '.join(notes) if notes else 'ok'}")

    print("\n== refusals ==")
    quiet = True
    for n in nodes:
        for r in rates:
            got = cell(n, r)
            if got is None:
                print(f"  node {n} lr {r}: no records")
                quiet = False
            elif got.refused:
                print(f"  node {n} lr {r}: {got.refused}")
                quiet = False
    if quiet:
        print("  none")

    print("\n== prediction 1: does the best rate move with node width? ==")
    print("   (a rate only counts as better if its lead beats the seed spread)")
    chosen = {}
    for n in nodes:
        best = best_by((((n, r), cell(n, r)) for r in rates), "tag")
        if best is None:
            print(f"  node {n:>3}: no usable cell")
            continue
        usable = {r: c for r in rates
                  if (c := cell(n, r)) is not None and c.refused is None}
        won = best_rate(usable, "tag")
        if won is not None and won[1] <= won[2]:
            print(f"  node {n:>3}: TIED -- best lead {won[1]:+.2f} is inside "
                  f"the noise floor {won[2]:.2f}")
            continue
        chosen[n] = best[0][1]
        print(f"  node {n:>3}: best rate {best[0][1]} "
              f"at tag {best[1].ratios['tag']:+.2f}"
              + (f", lead {won[1]:+.2f} over {INCUMBENT}" if won else ""))
    if len(set(chosen.values())) > 1:
        print("  -> the best rate MOVES with node width. One rate cannot be")
        print("     right for a grid that sweeps width, and g9-09 swept width")
    elif chosen:
        held = set(chosen.values()) == {INCUMBENT}
        print(f"  -> one rate ({next(iter(chosen.values()))}) wins at every "
              f"width where the lead is real"
              + ("; the carried constant was lucky" if held else ""))
    else:
        print("  -> no width has a rate that beats the incumbent by more than")
        print("     the seed spread. The frozen constant costs nothing here")

    print("\n== prediction 2, THE ONE THAT MATTERS: does the RATIO move? ==")
    worst, real = 0.0, 0.0
    for n in nodes:
        got = {r: cell(n, r) for r in rates}
        usable = {r: c for r, c in got.items()
                  if c is not None and c.refused is None}
        if INCUMBENT not in usable or len(usable) < 2:
            print(f"  node {n:>3}: not comparable")
            continue
        for arm in ("tag", "combined"):
            rate, cost, noise = best_rate(usable, arm)
            worst = max(worst, cost)
            if cost > noise:
                real = max(real, cost)
            print(f"  node {n:>3} {arm:>9}: {cost:+.2f} left on the table by "
                  f"holding {INCUMBENT} (noise {noise:.2f})"
                  + ("" if cost > noise else "   <- inside the noise"))
    print(f"  largest cost anywhere: {worst:+.2f}; largest cost that BEATS its "
          f"noise floor: {real:+.2f} (threshold {MATTERS})")
    if real > MATTERS:
        print("  -> every published fraction in the g9 line is off by an")
        print("     unknown amount. Re-baseline before quoting any of them")
    else:
        print("  -> note 028's concern is real in principle and NEGLIGIBLE in")
        print("     practice. Say that as plainly as the warning was said")

    print("\n== prediction 3: does the arm ORDERING move? ==")
    for n in nodes:
        seen = {}
        for r in rates:
            got = cell(n, r)
            if got is not None and got.refused is None:
                seen.setdefault(ordering(got), []).append(r)
        if not seen:
            print(f"  node {n:>3}: no usable cell")
            continue
        if len(seen) == 1:
            print(f"  node {n:>3}: stable -- {' > '.join(next(iter(seen)))}")
            continue
        print(f"  node {n:>3}: ORDERING CHANGES WITH THE RATE")
        for order, at in seen.items():
            print(f"        lr {at}: {' > '.join(order)}")
        # A swap between two arms that sit within the seed spread of each other
        # is not evidence that the ordering moved -- it is the same tie being
        # broken twice. Only a swap where the two arms are separated by more
        # than the noise floor at one of the rates says anything.
        usable = {r: c for r in rates
                  if (c := cell(n, r)) is not None and c.refused is None}
        real = []
        for a in ARMS:
            for b in ARMS:
                if a >= b:
                    continue
                signs = {c.ratios[a] > c.ratios[b] for c in usable.values()}
                if len(signs) < 2:
                    continue
                apart = max(abs(c.ratios[a] - c.ratios[b]) - margin(c)
                            for c in usable.values())
                if apart > 0:
                    real.append(f"{a}/{b}")
        if real:
            print(f"        swaps wider than the noise floor: {', '.join(real)}")
            print("        -> note 028's central reassurance is wrong, and")
            print("           COMPARISONS need re-checking, not just scales")
        else:
            print("        every swap is between arms within the seed spread")
            print("        of each other: a tie broken twice, not a reordering")

    print("\n== prediction 4: does the rate move the floor arm? ==")
    for n in nodes:
        vals = [c.means["none"] for c in (cell(n, r) for r in rates)
                if c is not None]
        if len(vals) < 2:
            print(f"  node {n:>3}: not comparable")
            continue
        moved = max(vals) - min(vals)
        flag = "  <- g8-01 reproduced" if moved > FLOOR_MOVES else ""
        print(f"  node {n:>3}: floor spans {min(vals):.3f}..{max(vals):.3f}, "
              f"moves {moved:.3f}{flag}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
