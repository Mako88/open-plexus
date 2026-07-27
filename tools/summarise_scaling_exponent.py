"""Fit the scaling exponent, and say plainly what it does and does not mean.

`bits ≈ a · axis^b + c`. **`b` is the whole point.** Filipovich et al.
(arXiv:2210.14593) found local learning does not merely lose a constant against
backpropagation — it loses the exponent (−0.040 against −0.071, with a shallow
network at −0.019), so the gap widens with scale and is invisible small.

## The reading rule, stated before the numbers

A flat exponent for our arms is **not** by itself evidence that local learning
fails. The delta rule on `Wo` is the exact gradient for a single linear readout;
there is no hidden layer to propagate through, so nothing here approximates
backprop badly. What a flat exponent says is that **width buys nothing**, which
note 035 already explains for the single-token arm: the store holds a bigram
count table whose effective rank is about 3 at every width.

The arm that carries information about the learning rule is the CONTEXT-KEY one,
whose ceiling is a trigram. If width buys nothing even when the ceiling moves,
the ceiling was not what was binding.

**And the baseline is the control.** If the backprop arm is also flat, the range
is too narrow to fit anything and this tool should say so rather than report
three flat exponents as agreement. That is not hypothetical — it is what
happened to g11-04, whose backprop arm fitted `b = -0.0021` with an R² of 0.13.

## Which axis, and why this tool does not know in advance

g11-04 swept WIDTH at fixed data and its control came out flat because the
baseline was **data-limited, not width-limited**: 250,000 characters is not
enough text for a wider attention model to have more to learn. g11-05 sweeps
DATA at fixed width, which is the axis Filipovich et al. actually used and the
one where the reference still moves.

So the axis is a property of the grid, not of this tool. It is read from the
records — whichever of `chars` or `width` actually varies — because a summariser
that hard-codes one experiment's axis is wrong about the next one, and the
direction of that error is not predictable.
"""

from __future__ import annotations

import numpy as np

from tools.recovery import load, mean_and_error, require

#: Below this the fit is calling noise a trend.
FLAT = 0.02


def fit(widths: list[float], values: list[float]) -> tuple[float, float]:
    """Least squares on log(bits) against log(width): the slope, and R².

    An offset `c` is deliberately NOT fitted. Three or four widths cannot
    identify three parameters, and a free asymptote lets any curve be called
    steep. The slope of the log-log line is the conservative reading.
    """
    x, y = np.log(np.asarray(widths)), np.log(np.asarray(values))
    slope, intercept = np.polyfit(x, y, 1)
    predicted = slope * x + intercept
    spread = float(((y - y.mean()) ** 2).sum())
    residual = float(((y - predicted) ** 2).sum())
    return float(slope), (1.0 - residual / spread) if spread > 0 else 0.0


#: Candidate axes, most specific first. Whichever actually varies is the grid's.
AXES = ("chars", "width")


def axis_of(rows: list[dict]) -> str:
    """The field this grid moved, read from the records rather than assumed.

    Refuses when more than one candidate varies: a grid moving two axes at once
    cannot have a single exponent fitted through it, and picking one silently
    would report a slope confounded by the other.
    """
    moved = [name for name in AXES
             if len({row[name] for row in rows if name in row}) > 1]
    if len(moved) > 1:
        raise SystemExit(
            f"{' and '.join(moved)} both vary in these records. A single "
            f"exponent through a two-axis grid is confounded; sweep one.")
    return moved[0] if moved else "width"


def main() -> int:
    rows = require(load(), "arm", "bits_calibrated", "seed")
    if not rows:
        print("no records matched")
        return 1
    axis = axis_of(rows)
    rows = require(rows, axis)
    if not rows:
        print(f"no records carry {axis}")
        return 1

    arms = sorted({r["arm"] for r in rows})
    points_on_axis = sorted({r[axis] for r in rows})
    label = "d=" if axis == "width" else "n="
    print(f"vocabulary {rows[0]['vocab_size']}, axis {axis} "
          f"{points_on_axis}\n")

    print("== bits per character ==")
    print(f"{'arm':>12}" + "".join(f"{label + str(w):>16}"
                                   for w in points_on_axis))
    fits = {}
    for arm in arms:
        line, points = f"{arm:>12}", []
        for width in points_on_axis:
            values = [r["bits_calibrated"] for r in rows
                      if r["arm"] == arm and r[axis] == width]
            if not values:
                line += f"{'missing':>16}"
                continue
            mean, error = mean_and_error(values)
            points.append((width, mean))
            line += f"{mean:>10.3f} +/-{error:.3f}"
        print(line)
        if len(points) >= 3:
            fits[arm] = fit([p for p, _ in points], [v for _, v in points])

    print("\n== the exponent, and how well the line fits ==")
    print(f"  bits ~ {axis}^b; more negative means {axis} buys more\n")
    for arm in arms:
        if arm not in fits:
            print(f"  {arm:>12}: fewer than 3 points on {axis}, no fit")
            continue
        slope, quality = fits[arm]
        shape = "FLAT" if abs(slope) < FLAT else f"{slope:+.4f}"
        print(f"  {arm:>12}: b = {slope:+.4f}   R2 = {quality:.3f}   {shape}")

    print("\n== the verdict ==")
    reference = fits.get("backprop")
    if reference is None:
        print("  no backprop arm, so there is no reference exponent and the")
        print("  numbers above cannot be called steep or flat")
        return 0
    if abs(reference[0]) < FLAT:
        print("  **THE CONTROL FAILED.** The backprop baseline is flat too, so")
        print(f"  the {axis} range cannot resolve a trend and NOTHING here")
        print("  should be read as a finding about local learning.")
        return 0
    print(f"  backprop b = {reference[0]:+.4f} — the control holds, so the")
    print("  comparison below is meaningful\n")
    for arm in arms:
        if arm == "backprop" or arm not in fits:
            continue
        slope = fits[arm][0]
        if abs(slope) < FLAT:
            print(f"  {arm:>12}: FLAT. {axis} buys nothing.")
            if axis == "width":
                print(f"  {'':>12}  For the single-token arm note 035 predicts")
                print(f"  {'':>12}  exactly this (effective rank ~3 at every")
                print(f"  {'':>12}  width); for the context arm it would mean")
                print(f"  {'':>12}  the ceiling was not what was binding.")
            else:
                print(f"  {'':>12}  On DATA there is no rank argument to appeal")
                print(f"  {'':>12}  to. An arm that does not improve with more")
                print(f"  {'':>12}  text has stopped learning from text, and")
                print(f"  {'':>12}  that is a statement about the RULE.")
        elif abs(slope) < abs(reference[0]):
            print(f"  {arm:>12}: b = {slope:+.4f}, SHALLOWER than backprop's "
                  f"{reference[0]:+.4f}.")
            print(f"  {'':>12}  The gap widens with scale — this is the "
                  f"Filipovich shape.")
        else:
            print(f"  {arm:>12}: b = {slope:+.4f}, at least as steep as "
                  f"backprop's {reference[0]:+.4f}.")
            print(f"  {'':>12}  **The exponent argument does not apply to us.**")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
