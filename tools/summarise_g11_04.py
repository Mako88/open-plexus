"""Fit the scaling exponent, and say plainly what it does and does not mean.

`bits ≈ a · width^b + c`. **`b` is the whole point.** Filipovich et al.
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

**And the baseline is the control.** If the backprop arm is also flat, the width
range is too narrow to fit anything and this tool should say so rather than
report three flat exponents as agreement.
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


def main() -> int:
    rows = require(load(), "width", "arm", "bits_calibrated", "seed")
    if not rows:
        print("no records matched")
        return 1

    arms = sorted({r["arm"] for r in rows})
    widths = sorted({r["width"] for r in rows})
    print(f"vocabulary {rows[0]['vocab_size']}, widths {widths}\n")

    print("== bits per character ==")
    print(f"{'arm':>12}" + "".join(f"{'d=' + str(w):>16}" for w in widths))
    fits = {}
    for arm in arms:
        line, points = f"{arm:>12}", []
        for width in widths:
            values = [r["bits_calibrated"] for r in rows
                      if r["arm"] == arm and r["width"] == width]
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
    print("  bits ~ width^b; more negative means width buys more\n")
    for arm in arms:
        if arm not in fits:
            print(f"  {arm:>12}: fewer than 3 widths, no fit")
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
        print("  the width range cannot resolve a trend and NOTHING here should")
        print("  be read as a finding about local learning.")
        return 0
    print(f"  backprop b = {reference[0]:+.4f} — the control holds, so the")
    print("  comparison below is meaningful\n")
    for arm in arms:
        if arm == "backprop" or arm not in fits:
            continue
        slope = fits[arm][0]
        if abs(slope) < FLAT:
            print(f"  {arm:>12}: FLAT. Width buys nothing. For the single-token")
            print(f"  {'':>12}  arm note 035 predicts exactly this (effective")
            print(f"  {'':>12}  rank ~3 at every width); for the context arm it")
            print(f"  {'':>12}  would mean the ceiling was not what was binding.")
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
