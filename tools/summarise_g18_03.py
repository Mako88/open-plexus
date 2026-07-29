"""Score g18-03: has the store ever contributed anything on TEXT at all?

Decision 137 measured the store contributing nothing at word level under three
rates, two widths and two key schemes. At character level the model reaches 5.17
against a 6.00 uniform and that looks like the store working -- **but every
character-level run had `readout_bias` off**, so its contribution there was never
measured against a model that can express a prior, and `Wo` can carry a prior by
itself.

If character-level `nostore` with the bias on lands near the floor, the store has
never contributed on text and the whole text line is a statement about a linear
readout.

**Every arm is scored against `nostore` at its own unit and bias.** A character
model and a word model do not share a scale, and a biased model and an unbiased
one do not share a baseline.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, require

#: P1's gate: bits by which `floor` must beat its matched ablation for the store
#: to be contributing.
GATE = 0.30
#: P2's rail: how close an unbiased, storeless model must sit to uniform.
AT_UNIFORM = 0.05


def dead(record: dict) -> bool:
    return bool(record["diverged"] or record.get("unstable"))


def main() -> None:
    records = require(load(), "kind", "units", "bias", "lr", "error",
                      "fit_error", "diverged")
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    rates = sorted({r["lr"] for r in records}, reverse=True)
    cells: dict[tuple, list[dict]] = defaultdict(list)
    for record in records:
        cells[(record["units"], record["bias"], record["kind"],
               record["lr"])].append(record)
    configurations = sorted({(k[0], k[1]) for k in cells})

    print(f"records {len(records)}, rates {rates}")
    for units in sorted({r["units"] for r in records}):
        one = next(r for r in records if r["units"] == units)
        print(f"  {units:<11} vocab {one['vocab']:>5}   "
              f"bigram {one['bigram']:.3f}   unigram {one['unigram']:.3f}   "
              f"uniform {one['uniform']:.3f}")

    gone = [r for r in records if dead(r)]
    print(f"\nRAILS\n  no measurement in {len(gone)} of {len(records)} cell(s)"
          + (f" -- {sorted({r['condition'] for r in gone})}" if gone else ""))

    print("\nbits per TOKEN on TEST text, by learning rate")
    print(f"  {'arm':<28}" + "".join(f"{r:>12}" for r in rates))
    for units, bias in configurations:
        for kind in ("floor", "nostore"):
            row = ""
            for rate in rates:
                rows = [r for r in cells.get((units, bias, kind, rate), [])
                        if not dead(r)]
                row += (f"{statistics.mean([r['error'] for r in rows]):>12.3f}"
                        if rows else f"{'--':>12}")
            print(f"  {f'{units} bias{int(bias)} {kind}':<28}{row}")

    def best(units, bias, kind):
        scored = [(rate, rows) for rate in rates
                  if (rows := [r for r in cells.get((units, bias, kind, rate),
                                                    []) if not dead(r)])]
        if not scored:
            return None, None
        rate, rows = min(scored, key=lambda pair: statistics.mean(
            [r["fit_error"] for r in pair[1]]))
        return rate, statistics.mean([r["error"] for r in rows])

    print("\nPREDICTIONS")
    print(f"  P1  THE GATE. with the bias ON, `floor` beats its matched "
          f"`nostore` by more than {GATE}:")
    passed = {}
    for units, bias in configurations:
        rate, floor = best(units, bias, "floor")
        _, ablated = best(units, bias, "nostore")
        if floor is None or ablated is None:
            continue
        gain = ablated - floor
        passed[(units, bias)] = gain
        print(f"        {units:<11} bias{int(bias)}   store {floor:.3f} "
              f"against nostore {ablated:.3f}   {gain:+.3f}"
              f"{'   THE STORE CONTRIBUTES' if gain > GATE else ''}"
              f"   (lr {rate})")
    biased = [g for (units, bias), g in passed.items()
              if bias and units == "characters"]
    if biased:
        good = max(biased) > GATE
        print(f"      -> {'CONFIRMED' if good else 'REFUTED'}")
        if not good:
            print("      THE LARGEST RESULT THIS LINE COULD HAVE PRODUCED. The "
                  "store contributes nothing on text at EITHER unit, and every "
                  "text number this project holds is a statement about a linear "
                  "readout rather than about the memory.")

    unbiased = [(units, best(units, False, "nostore")[1])
                for units in sorted({r["units"] for r in records})]
    print(f"  P2  THE RAIL. with no store AND no prior, the model sits at "
          f"uniform (within {AT_UNIFORM}):")
    steady = True
    for units, value in unbiased:
        if value is None:
            continue
        one = next(r for r in records if r["units"] == units)
        off = abs(value - one["uniform"])
        steady = steady and off <= AT_UNIFORM
        print(f"        {units:<11} {value:.3f} against uniform "
              f"{one['uniform']:.3f}   off by {off:.3f}")
    print(f"      -> {'CONFIRMED' if steady else 'REFUTED'}")
    if not steady:
        print("      Something is learning that is neither the store nor the "
              "bias, so every ablation in this line is misattributed.")

    _, chars = best("characters", True, "floor")
    if chars is not None:
        one = next(r for r in records if r["units"] == "characters")
        print(f"  P3  THE FALSIFIER. character `floor` does NOT reach the "
              f"bigram at {one['bigram']:.3f}: {chars:.3f} -> "
              f"{'CONFIRMED' if chars > one['bigram'] else 'REFUTED'}")
        if chars <= one["bigram"]:
            print("      The store does at character level exactly what it "
                  "refused to do at word level, and the difference between the "
                  "two units becomes the whole question.")


if __name__ == "__main__":
    main()
