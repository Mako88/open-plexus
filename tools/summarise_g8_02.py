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

from tools.recovery import MQAR_FLOOR, assess, by_cell, load

ARMS = ("none", "oracle", "on-use", "salience")


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "zipf_s", "lr")

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
    print(f"{'zipf_s':>7}{'lr':>7}  {'none':>7}  {'oracle':>7}  {'gap':>7}  "
          f"{'spread':>7}  {'on-use':>9}  {'salience':>9}")
    refusals: list[str] = []
    for zipf_s in exponents:
        for lr in rates:
            verdict = assess(cells, (zipf_s, lr), ARMS, MQAR_FLOOR)
            if verdict is None:
                print(f"{zipf_s:>7}{lr:>7}   no result returned")
                continue
            print(f"{zipf_s:>7}{lr:>7}  {verdict.means['none']:>7.3f}  "
                  f"{verdict.means['oracle']:>7.3f}  {verdict.gap:>7.3f}  "
                  f"{verdict.spread:>7.3f}  {verdict.text('on-use')}  "
                  f"{verdict.text('salience')}")
            if verdict.refused:
                refusals.append(f"  zipf_s {zipf_s} lr {lr}: {verdict.refused}")

    # Every learning rate is printed rather than the best one. An earlier
    # version kept the lr with the largest `oracle - none`, which is the single
    # rule guaranteed to prefer cells whose floor arm had collapsed -- collapse
    # IS a large gap. It also had no floor check at all, under a heading that
    # named one. See tools/recovery.py.
    if refusals:
        print("\nrefused, and why:")
        print("\n".join(refusals))

    print("\nsalience rises and on-use does not  -> the base-rate diagnosis was "
          "right,\n  and g8-01's negative result is about MQAR rather than about "
          "gating.\nboth rise together                  -> the task got easier; "
          "nothing learned.\nneither moves                       -> the "
          "diagnosis was wrong and note 013's\n  explanation must be withdrawn.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
