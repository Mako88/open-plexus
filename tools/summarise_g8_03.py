"""Does a bounded pool flatten the recovery-versus-length curve?

**The prediction is about a SHAPE, so this reports the shape.** g8-01 measured
recovery falling with sequence length -- 0.05 at 192 to -0.00 at 1536 -- and note
015 attributes that to a threshold setting a RATE while the oracle sets a
QUANTITY. A pool of `k` sets a quantity, so its curve should be flat whatever
height it sits at.

A tool that printed only the best cell would answer a different question and
would answer it convincingly, so the table is laid out as pool x length and the
last column is the thing prediction 1 is actually about: **the slope**, recovery
at the longest length minus recovery at the shortest.

Flat and low is a confirmed mechanism that does not help. Falling is the
mechanism not working. Those must not be merged, and a single headline number
merges them.

The two refusals and the floor come from `tools/recovery.py` rather than being
copied here. The floor is `1/n_pairs + (1 - 1/n_pairs)/n_values` for this MQAR
configuration, and it is a PARAMETER: freezing a property of one experiment into
a reporting tool is a mistake this repository has recorded happening three times
in the same tool.

> **The learning rate is now chosen differently, and it can move the numbers.**
> This used to pick, per length, the rate with the largest `oracle - none`. It
> skipped collapsed floors first, so it was not the worst version of that
> mistake, but among surviving cells maximising the gap still prefers whichever
> rate left the floor arm lowest. It now picks by what **`capture-0`** recovers:
> the UNBOUNDED arm, which is g8-01's failing mechanism and the one this sweep's
> prediction is not about. Choosing the rate on the arm under test would let the
> rate be picked to flatter it. **The table may therefore differ from the one in
> the g8-03 sweep file**, which records what was reported at the time and is not
> edited to match.
"""

from __future__ import annotations

from tools.recovery import MQAR_FLOOR, assess, best_by, by_cell, load

#: The rate is chosen on this arm: unbounded capture, which the prediction is
#: not about. Named rather than inlined so the choice is visible to anyone
#: reading the table.
TUNED_ON = "capture-0"


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "seq_len", "lr")
    seq_lens = sorted({r["seq_len"] for r in rows})
    rates = sorted({r["lr"] for r in rows})
    pools = sorted({r["arm"] for r in rows if r["arm"].startswith("capture-")},
                   key=lambda name: int(name.split("-")[1]))
    arms = ["none", "oracle"] + pools

    print("\n=== accuracy per seed ===")
    for seq_len in seq_lens:
        print(f"\nseq_len {seq_len}")
        for lr in rates:
            line = [f"  lr={lr:<5}"]
            for arm in arms:
                by_seed = cells.get((seq_len, lr, arm), {})
                if not by_seed:
                    line.append(f"{arm}=--")
                    continue
                line.append(f"{arm}=" + "/".join(
                    f"{by_seed[s]:.3f}" for s in sorted(by_seed)))
            print("  ".join(line))

    chosen: dict[int, tuple] = {}
    for seq_len in seq_lens:
        best = best_by(((lr, assess(cells, (seq_len, lr), arms, MQAR_FLOOR))
                        for lr in rates), TUNED_ON)
        if best is not None:
            chosen[seq_len] = best

    print(f"\n=== RECOVERY by pool size and sequence length ===")
    print(f"Prediction 1: the bounded pools stay flat and capture-0 falls.")
    print(f"Rate chosen per length on {TUNED_ON}, the arm under no prediction.")
    print(f"{'pool':>12}" + "".join(f"{s:>9}" for s in seq_lens) + f"{'slope':>9}")
    print(f"{'lr used':>12}" + "".join(
        f"{chosen[s][0]:>9}" if s in chosen else "  missing".rjust(9)
        for s in seq_lens))
    for pool in pools:
        values = [chosen[s][1].ratios.get(pool) if s in chosen else None
                  for s in seq_lens]
        text = "".join("undefined".rjust(9) if v is None else f"{v:>9.2f}"
                       for v in values)
        ends = [values[0], values[-1]]
        slope = ("      n/a" if any(v is None for v in ends)
                 else f"{ends[1] - ends[0]:>9.2f}")
        print(f"{pool:>12}{text}{slope}")

    print("\n=== REFUSALS ===")
    quiet = True
    for seq_len in seq_lens:
        for lr in rates:
            got = assess(cells, (seq_len, lr), arms, MQAR_FLOOR)
            if got is None:
                print(f"  seq_len {seq_len} lr {lr}: no records")
                quiet = False
            elif got.refused:
                print(f"  seq_len {seq_len} lr {lr}: {got.refused}")
                quiet = False
    if quiet:
        print("  none")

    print("\nslope near 0  -> the pool holds N constant, which is what it is for.")
    print("slope negative-> recovery still decays with length; for capture-0 that")
    print("                 reproduces g8-01, and for a bounded pool it refutes")
    print("                 note 015's argument.")
    print("HEIGHT AND SHAPE ARE SEPARATE FINDINGS. Flat at 0.02 means the")
    print("mechanism works and does not help; do not report it as either alone.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
