"""Score g22-01: does the relational objective pay against a sequential one?

**This scores GOALS section 1's own refutation condition**, which had never been
run before this sweep:

> a system whose training objective is *relational* rather than *sequential* will
> reason rather than continue. If a model with a good concept map turns out to
> reason no better than a next-token model of the same size, the central premise
> is wrong.

## The three-way verdict, and why it is not two-way

`MIN_DIFFERENCE` is registered in the sweep record before dispatch, so this tool
reports **relational wins / sequential wins / below the instrument's
resolution** rather than forcing a binary.

That third outcome is the one a summariser normally throws away, and this project
has a calibration saying what that costs: g5-02 fitted an exponent through two
rows its own output had labelled *"AT THE EDGE OF THE GRID"*, and g5-03 was
reported `UNRESOLVED` by a tool that had a factor of two hard-coded from a
different sweep. **A bound is not a value**, in either direction.

g14-01 measured this task's usable band at 0.092 with a standard error of 0.011
at 8 seeds. A difference under 0.030 is not a null result here -- it is a
statement that this instrument cannot see the effect, and the answer to that is a
task with a wider band rather than more seeds.

## `seq_probe` is not compared to anything

It carries 8 epochs of readout fitting on top of the full sequential budget, so
it has more compute than the arms beside it. It is printed apart from the
comparison for that reason, and P4 is the only prediction that reads it.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, require, spread

ARM_ORDER = ("majority", "relational", "sequential", "seq_probe")

#: Registered in experiments/sweeps/g22-01-does-the-relational-objective-pay.txt
#: BEFORE dispatch. A difference below this is reported as unresolved, never as a
#: null and never as a confirmation.
MIN_DIFFERENCE = 0.030

#: g14-01's entailed reading for the same arm under the same settings. P1 is the
#: control that this harness still produces it.
G14_01_ATTENTION_ENTAILED = 0.282
CONTROL_TOLERANCE = 0.020

#: g14-01's base rate on the entailed half.
MAJORITY_FLOOR = 0.190


def _mean(values: list[float]) -> float | None:
    clean = [v for v in values if v is not None and v == v]
    return statistics.fmean(clean) if clean else None


def main() -> None:
    records = require(load(), "arm", "seed", "stated", "entailed")
    by_arm: dict[str, list[dict]] = defaultdict(list)
    for record in records:
        by_arm[record["arm"]].append(record)

    seeds = sorted({r["seed"] for r in records})
    print(f"arms {sorted(by_arm)}, records {len(records)}, "
          f"seeds {len(seeds)}")

    entailed: dict[str, float | None] = {}
    print(f"\n{'arm':<14}{'stated':>20}{'entailed':>20}")
    for arm in ARM_ORDER:
        rows = by_arm.get(arm, [])
        if not rows:
            print(f"  {arm:<12}{'MISSING':>20}{'MISSING':>20}")
            entailed[arm] = None
            continue
        stated_values = [r["stated"] for r in rows]
        entailed_values = [r["entailed"] for r in rows]
        entailed[arm] = _mean(entailed_values)
        print(f"  {arm:<12}{spread(stated_values):>20}"
              f"{spread(entailed_values):>20}")

    print("\nPREDICTIONS")

    relational, sequential = entailed.get("relational"), entailed.get("sequential")

    if relational is None:
        print("  P1  UNSCORABLE -- the relational arm returned nothing")
    else:
        drift = abs(relational - G14_01_ATTENTION_ENTAILED)
        verdict = "CONFIRMED" if drift <= CONTROL_TOLERANCE else "REFUTED"
        print(f"  P1  relational reproduces g14-01 within {CONTROL_TOLERANCE}: "
              f"{relational:.3f} against {G14_01_ATTENTION_ENTAILED} "
              f"(drift {drift:.3f}) -> {verdict}")
        if verdict == "REFUTED":
            print("      THE CONTROL FAILED. The harness is not producing "
                  "g14-01's arm, so nothing below can be read.")

    if relational is None or sequential is None:
        print("  P2  UNSCORABLE -- an arm is missing")
    else:
        difference = relational - sequential
        print(f"  P2  THE TEST. relational - sequential on ENTAILED: "
              f"{difference:+.3f}, against a registered "
              f"MIN_DIFFERENCE of {MIN_DIFFERENCE}")
        if abs(difference) < MIN_DIFFERENCE:
            print("      -> BELOW THE RESOLUTION OF THIS INSTRUMENT.")
            print("      NOT a null and NOT a confirmation. The usable band on "
                  "this task is 0.092 wide; an effect this size cannot be")
            print("      separated from noise here. The next move is a task "
                  "with a wider band, not more seeds.")
        elif difference > 0:
            print("      -> CONFIRMED. Training relationally beats training "
                  "sequentially on the reasoning half.")
            print("      Read with P5 before quoting it: if sequential sits "
                  "below the base rate, this says more about the cost of a")
            print("      dense objective than about relational structure.")
        else:
            print("      -> REFUTED, AND THIS IS A FINDING ABOUT THE PROJECT.")
            print("      GOALS section 1's stated bet is that the relational "
                  "objective reasons better. On this task it does not.")

    for arm in ("relational", "sequential"):
        rows = by_arm.get(arm, [])
        if rows:
            stated = _mean([r["stated"] for r in rows])
            near = abs(stated - 0.100) <= 0.05
            print(f"  P3  {arm} sits near the stated floor: {stated:.3f} "
                  f"-> {'CONFIRMED' if near else 'REFUTED -- CHECK FOR A LEAK'}")

    probe = entailed.get("seq_probe")
    if probe is None or sequential is None:
        print("  P4  UNSCORABLE -- an arm is missing")
    else:
        ok = probe >= sequential
        print(f"  P4  seq_probe at or above sequential: {probe:.3f} against "
              f"{sequential:.3f} -> {'CONFIRMED' if ok else 'REFUTED'}")
        if not ok:
            print("      A refit readout cannot destroy information, so this "
                  "means the fit did not converge. The arm is uninterpretable.")
        else:
            print(f"      DIAGNOSTIC ONLY -- it carries extra compute and is "
                  f"not comparable to the arms above it.")
            if sequential is not None and probe - sequential >= MIN_DIFFERENCE:
                print("      And the gap is real: the sequential objective "
                      "LEARNED relational structure its own readout does not")
                print("      emit. That weakens any reading of P2 as 'the "
                      "objective cannot represent relations'.")

    if sequential is None:
        print("  P5  UNSCORABLE -- the sequential arm returned nothing")
    else:
        above = sequential >= MAJORITY_FLOOR
        print(f"  P5  sequential at or above the {MAJORITY_FLOOR} base rate: "
              f"{sequential:.3f} -> {'CONFIRMED' if above else 'REFUTED'}")
        if not above:
            print("      BELOW THE BASE RATE. Predicting everything is not a "
                  "weaker objective here, it is a harmful one -- so a")
            print("      confirmed P2 may have nothing to do with relational "
                  "structure being good. Say so wherever P2 is quoted.")


if __name__ == "__main__":
    main()
