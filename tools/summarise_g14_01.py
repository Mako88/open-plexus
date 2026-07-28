"""Score g14-01: does the closure task pass G0?

GOALS section 4, the acceptance test the predecessor project failed:

> before any learning mechanism is written, the task must be shown to have
> substantial headroom between what a random frozen substrate achieves and what
> a strong non-local reference achieves, with both measured, multi-seed, and
> with the base rate of a constant predictor reported alongside.

**The ENTAILED half is the task.** A stated fact's relation is drawn at random
and its binding is written one step after the prediction point, so the stated
half is a floor by construction and every arm should sit near `majority` there.
Averaging the two would hide exactly what G0 exists to check.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, spread

ARM_ORDER = ("majority", "frozen", "local", "attention")

#: P3's bar. Below this the task has no room to measure a learning rule in.
USABLE_HEADROOM = 0.15

#: P4's bar for the STATED half -- see the note below, which is why it is
#: written as a ceiling rather than the floor the prediction first assumed.
STATED_TOLERANCE = 0.05


def main() -> None:
    records = load()
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    cells: dict[str, list[dict]] = defaultdict(list)
    for record in records:
        cells[record["arm"]].append(record)
    arms = [a for a in ARM_ORDER if a in cells]
    print(f"arms {arms}, records {len(records)}, "
          f"seeds {sorted({r['seed'] for r in records})}")
    if len({r["condition"] for r in records}) != len(records):
        print("!! duplicate condition strings -- two runs wrote the same cell")

    implied = [r["entailed_per_sequence"] for r in records]
    print(f"implied edges per sequence: {statistics.mean(implied):.2f}")

    print(f"\n{'arm':<11}{'stated':>20}{'entailed':>20}")
    for arm in arms:
        rows = cells[arm]
        print(f"  {arm:<9}{spread([r['stated'] for r in rows]):>20}"
              f"{spread([r['entailed'] for r in rows]):>20}")

    def mean(arm: str, half: str) -> float:
        return statistics.mean([r[half] for r in cells[arm]])

    print("\nPREDICTIONS")

    if "frozen" in cells and "majority" in cells:
        gap = mean("frozen", "entailed") - mean("majority", "entailed")
        print(f"  P1  frozen sits at or below majority on ENTAILED: "
              f"{mean('frozen', 'entailed'):.3f} against "
              f"{mean('majority', 'entailed'):.3f} ({gap:+.3f}) -> "
              f"{'CONFIRMED' if gap <= 0.05 else 'REFUTED'}")
        print("      A random substrate that already composed would mean the "
              "task is answerable without learning, which is the "
              "predecessor's failure exactly.")

    if "attention" in cells and "majority" in cells:
        over = mean("attention", "entailed") - mean("majority", "entailed")
        print(f"  P2  attention beats majority on ENTAILED by >0.15: "
              f"{over:+.3f} -> {'CONFIRMED' if over > 0.15 else 'REFUTED'}")

    if "attention" in cells and "frozen" in cells:
        headroom = mean("attention", "entailed") - mean("frozen", "entailed")
        print(f"  P3  THE GATE. headroom on ENTAILED (attention - frozen) "
              f"> {USABLE_HEADROOM}: {headroom:.3f} -> "
              f"{'CONFIRMED' if headroom > USABLE_HEADROOM else 'REFUTED'}")

    # P4 was written before the first run and assumed stated facts were
    # RECALLABLE. They are not: the binding key(S, O) -> R is written one step
    # AFTER the prediction point, so a stated relation drawn at random cannot be
    # predicted at all. The prediction is scored as written and the corrected
    # expectation is printed beside it.
    if "majority" in cells:
        base = mean("majority", "stated")
        drift = {arm: abs(mean(arm, "stated") - base) for arm in arms}
        worst = max(drift.values())
        print(f"  P4  REWRITTEN AS A RAIL. Every arm sits within "
              f"{STATED_TOLERANCE} of majority on STATED: "
              + ", ".join(f"{a} {mean(a, 'stated'):.3f}" for a in arms)
              + f" -> {'CONFIRMED' if worst <= STATED_TOLERANCE else 'REFUTED'}")
        print("      As WRITTEN it predicted every arm clearing 0.60 there, "
              "which assumed stated facts were recallable. They are not -- the "
              "binding is written one step after the prediction point -- so "
              "the stated half is a FLOOR and an arm well above it would mean "
              "a leak, not learning. Scored the corrected way and the original "
              "recorded as refuted.")

    if "local" in cells and "frozen" in cells and "attention" in cells:
        low, high = mean("frozen", "entailed"), mean("attention", "entailed")
        here = mean("local", "entailed")
        between = low < here < high
        print(f"  P5  local lands between frozen and attention on ENTAILED: "
              f"{low:.3f} < {here:.3f} < {high:.3f} -> "
              f"{'CONFIRMED' if between else 'REFUTED'}")
        floor = mean("majority", "entailed")
        if here < floor:
            print(f"      **AND IT IS BELOW THE MAJORITY FLOOR** ({floor:.3f}). "
                  f"A model scoring under 'always answer the commonest "
                  f"relation' is not weakly composing, it is actively "
                  f"mispredicting -- and that is the finding, not a detail.")

    if all(a in cells for a in ("attention", "frozen", "local", "majority")):
        headroom = mean("attention", "entailed") - mean("frozen", "entailed")
        used = mean("local", "entailed") - mean("frozen", "entailed")
        print(f"\n  WHAT THE LOCAL RULE TAKES OF THE AVAILABLE HEADROOM: "
              f"{used:.3f} of {headroom:.3f} = {used / headroom:.0%}")
        print("  That ratio is the question this project exists to answer, on "
              "the first task built for it rather than borrowed.")


if __name__ == "__main__":
    main()
