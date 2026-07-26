"""Does recovery move when the filler statistics stop being adversarial?

Same recovery ratio as g8-01, `(arm - none) / (oracle - none)`, swept over the
filler exponent instead of over sequence length.

**The trap this tool exists to avoid is in the pre-registration as prediction 3.**
Skewed filler is predictable filler, and most positions are filler, so RAW
ACCURACY WILL RISE FOR EVERY ARM including the one with no mechanism at all. Read
off raw accuracy, this sweep produces a spurious success. So raw accuracy is
printed beside the ratio and never instead of it.

The asymmetry is the actual test: `salience` should improve with the exponent and
`on-use` should not, because confirmation never consults token frequency. If both
rise together the task got easier and nothing has been learned about gating,
which is why the `none` column is printed first and largest.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

ARMS = ("none", "oracle", "on-use", "salience")


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    cells: dict[tuple, dict[int, float]] = defaultdict(dict)
    for r in rows:
        cells[(r["zipf_s"], r["lr"], r["arm"])][r["seed"]] = r["accuracy"]

    exponents = sorted({r["zipf_s"] for r in rows})
    rates = sorted({r["lr"] for r in rows})

    print("\n=== accuracy per seed, by filler exponent ===")
    for zipf_s in exponents:
        print(f"\nzipf_s {zipf_s}")
        for lr in rates:
            line = [f"  lr={lr:<5}"]
            for arm in ARMS:
                by_seed = cells.get((zipf_s, lr, arm), {})
                if not by_seed:
                    line.append(f"{arm}=--")
                    continue
                values = [by_seed[s] for s in sorted(by_seed)]
                line.append(f"{arm}=" + "/".join(f"{v:.3f}" for v in values))
            print("  ".join(line))

    print("\n=== RECOVERY, and the floor it is measured against ===")
    print("If `none` rises with the exponent, the TASK got easier -- that is")
    print("prediction 3 and it is expected. Only the ratio speaks to gating.")
    print(f"{'zipf_s':>7}  {'none':>7}  {'oracle':>7}  {'gap':>7}  "
          f"{'spread':>7}  {'on-use':>8}  {'salience':>9}")
    for zipf_s in exponents:
        best = None
        for lr in rates:
            means, spread = {}, 0.0
            for arm in ARMS:
                by_seed = cells.get((zipf_s, lr, arm), {})
                if not by_seed:
                    means = {}
                    break
                values = list(by_seed.values())
                means[arm] = sum(values) / len(values)
                spread = max(spread, max(values) - min(values))
            if not means:
                continue
            gap = means["oracle"] - means["none"]
            if best is None or gap > best[0]:
                best = (gap, spread, means)
        if best is None:
            continue
        gap, spread, means = best
        if gap <= spread:
            verdict = f"{'undefined':>8}  {'undefined':>9}"
        else:
            verdict = (f"{(means['on-use'] - means['none']) / gap:>8.2f}  "
                       f"{(means['salience'] - means['none']) / gap:>9.2f}")
        print(f"{zipf_s:>7}  {means['none']:>7.3f}  {means['oracle']:>7.3f}  "
              f"{gap:>7.3f}  {spread:>7.3f}  {verdict}")

    print("\nsalience rises and on-use does not  -> the base-rate diagnosis was "
          "right,\n  and g8-01's negative result is about MQAR rather than about "
          "gating.\nboth rise together                  -> the task got easier; "
          "nothing learned.\nneither moves                       -> the "
          "diagnosis was wrong and note 013's\n  explanation must be withdrawn.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
