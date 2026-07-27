"""How much of the oracle's advantage did a real mechanism recover?

The reported quantity is a RATIO, `(arm - none) / (oracle - none)`, because raw
accuracy hides the question: every arm rises and falls together with sequence
length and learning rate, so four columns of accuracy look like four columns of
the same thing. 0 means the mechanism bought nothing the floor did not already
have; 1 means it matched a gate that cheats.

**A ratio needs its denominator checked before it is printed.** Where the
oracle's advantage is not larger than the spread across seeds, recovery is
reported as `undefined` -- not as a number with a caveat beside it, which is the
failure this project has produced twice. At seq 192 the pre-dispatch control put
that advantage at 0.043, so that row is expected to be undefined and its being
undefined is a result rather than a gap.

The refusals and the MQAR floor come from `tools/recovery.py` rather than being
copied here.

> **Two reporting changes, and both can move the table.**
>
> **The learning rate is chosen differently.** This used to pick, per cell, the
> rate with the largest `oracle - none`. It skipped collapsed floors first, but
> among surviving cells maximising the gap still prefers whichever rate left the
> floor arm lowest. Unlike g8-03 there is no arm here that no prediction is about
> -- `on-use` and `salience` are both under test -- so the rate is chosen where
> the **floor arm scores highest**. That is a baseline choice rather than a
> mechanism choice, and it is the exact opposite bias to maximising the gap.
>
> **Refused and missing rows are now printed.** They used to be skipped
> entirely, so a cell whose denominator was noise vanished from the table rather
> than appearing as `undefined`, and a reader could not tell it from a
> combination that was never run.
>
> **The g8-01 sweep file is not edited to match.** It records what was reported
> at the time.
"""

from __future__ import annotations

from tools.recovery import MQAR_FLOOR, assess, by_cell, load

ARMS = ("none", "oracle", "on-use", "salience")


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    cells = by_cell(rows, "seq_len", "half_life", "lr")
    seq_lens = sorted({r["seq_len"] for r in rows})
    half_lives = sorted({r["half_life"] for r in rows}, reverse=True)
    rates = sorted({r["lr"] for r in rows})

    print("\n=== accuracy per seed, by sequence length and half-life ===")
    print("(each cell is one seed per column, at the learning rate named)")
    for seq_len in seq_lens:
        print(f"\nseq_len {seq_len}")
        for half in half_lives:
            for lr in rates:
                line = [f"  half={half:<6} lr={lr:<5}"]
                for arm in ARMS:
                    by_seed = cells.get((seq_len, half, lr, arm), {})
                    if not by_seed:
                        line.append(f"{arm}=--")
                        continue
                    line.append(f"{arm}=" + "/".join(
                        f"{by_seed[s]:.3f}" for s in sorted(by_seed)))
                print("  ".join(line))

    print("\n=== RECOVERY of the oracle's advantage ===")
    print("(arm - none) / (oracle - none), at the rate where the FLOOR arm is")
    print("highest -- a baseline choice, so the rate cannot be picked to suit a")
    print("mechanism under test.")
    print(f"{'seq_len':>8}  {'half-life':>9}  {'lr':>5}  {'oracle gap':>10}  "
          f"{'seed spread':>11}  {'on-use':>8}  {'salience':>8}")
    for seq_len in seq_lens:
        for half in half_lives:
            graded = [(lr, assess(cells, (seq_len, half, lr), ARMS, MQAR_FLOOR))
                      for lr in rates]
            usable = [(lr, got) for lr, got in graded
                      if got is not None and got.refused is None]
            if not usable:
                # Printed rather than skipped. A cell that cannot be interpreted
                # is a result; one that was never run is a dispatch failure; and
                # a row that simply vanishes is indistinguishable from both.
                why = next((got.refused for _, got in graded if got is not None),
                           "no records")
                print(f"{seq_len:>8}  {half:>9}  {'--':>5}  {why}")
                continue
            # Highest floor arm, NOT the largest gap. The two disagree exactly
            # where it matters: the largest gap is produced by the rate that
            # broke the floor arm most.
            lr, got = max(usable, key=lambda pair: pair[1].means["none"])
            print(f"{seq_len:>8}  {half:>9}  {lr:>5}  {got.gap:>10.3f}  "
                  f"{got.spread:>11.3f}  {got.ratios['on-use']:>8.2f}  "
                  f"{got.ratios['salience']:>8.2f}")

    print("\nRecovery near 0 for both arms means selective storage is not "
          "reachable\nby any local rule tried, and every result that depends on "
          "the gate must be\nlabelled a CEILING rather than a finding.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
