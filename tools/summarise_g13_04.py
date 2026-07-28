"""Score g13-04: is ambiguity detectable before searching?

g13-03 left search as a wash overall while gaining +0.092 exactly where
ambiguity lives and losing 0.054 where it does not. A gate would keep the gain
and give back the loss -- if the model can tell the cases apart from something it
can see.

The bar is decision 93's **0.628**, the best any identity-free confidence signal
reached there when fitted with the labels. A margin that cannot beat that is a
confidence heuristic wearing a new name.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load

#: Below this, the gate is not worth building on this signal (P1).
USABLE_AUC = 0.75

#: The best any identity-free confidence signal reached in decision 93, fitted
#: WITH the labels. Anything at or near this is not a new kind of signal.
DECISION_93_CEILING = 0.628


def spread(values: list[float]) -> str:
    clean = [v for v in values if v == v]        # drop NaN
    if not clean:
        return "--"
    if len(clean) < 2:
        return f"{clean[0]:.3f} (1 seed)"
    return (f"{statistics.mean(clean):.3f} "
            f"+/-{statistics.stdev(clean) / len(clean) ** 0.5:.3f}")


def main() -> None:
    records = load()
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    cells: dict[int, list[dict]] = defaultdict(list)
    for record in records:
        cells[record["width"]].append(record)
    widths = sorted(cells)
    print(f"widths {widths}, records {len(records)}, "
          f"seeds {sorted({r['seed'] for r in records})}")

    if len({r["condition"] for r in records}) != len(records):
        print("!! duplicate condition strings -- two runs wrote the same cell")

    print("\nAUC: P(a random out-degree-1 margin exceeds a random 2+ margin). "
          "0.500 is chance.")
    for signal in ("decode", "endpoint"):
        print(f"\n  {signal} margin")
        for width in widths:
            rows = cells[width]
            print(f"    d{width:<5d} AUC {spread([r[signal]['auc'] for r in rows]):>18s}"
                  f"   median one {spread([r[signal]['median_one'] for r in rows]):>18s}"
                  f"   median 2+ {spread([r[signal]['median_many'] for r in rows]):>18s}")

    every_decode = [r["decode"]["auc"] for r in records
                    if r["decode"]["auc"] == r["decode"]["auc"]]
    every_endpoint = [r["endpoint"]["auc"] for r in records
                      if r["endpoint"]["auc"] == r["endpoint"]["auc"]]

    print("\nPREDICTIONS")

    mean_decode = statistics.mean(every_decode)
    print(f"  P1  THE DECISION. decode margin AUC > {USABLE_AUC}: "
          f"{mean_decode:.3f} -> "
          f"{'CONFIRMED' if mean_decode > USABLE_AUC else 'REFUTED'}")
    print(f"      Against decision 93's {DECISION_93_CEILING} for "
          f"identity-free confidence signals fitted WITH the labels. Below "
          f"{USABLE_AUC} the gate is not worth building on this signal.")

    ratios = [r["decode"]["median_one"] / r["decode"]["median_many"]
              for r in records
              if r["decode"]["median_many"] not in (None, 0)
              and r["decode"]["median_one"] is not None]
    if ratios:
        worst = min(ratios)
        print(f"  P2  median at out-degree 1 is more than 2x the median at 2+: "
              f"smallest ratio {worst:.1f}x -> "
              f"{'CONFIRMED' if worst > 2 else 'REFUTED'}")

    if every_endpoint:
        mean_endpoint = statistics.mean(every_endpoint)
        lead = mean_endpoint - mean_decode
        print(f"  P3  the ENDPOINT margin does not beat the decode margin by "
              f"more than 0.05 AUC: {lead:+.3f} -> "
              f"{'CONFIRMED' if lead <= 0.05 else 'REFUTED'}")
        print("      Refuted would mean the expensive signal earns its cost, "
              "and the gate should be built the other way round: search first, "
              "then decide whether to trust the result.")

    by_width = {w: statistics.mean([r["decode"]["auc"] for r in cells[w]
                                    if r["decode"]["auc"] == r["decode"]["auc"]])
                for w in widths}
    holds = all(value > USABLE_AUC for value in by_width.values())
    print("  P4  separation holds at every width: "
          + ", ".join(f"d{w} {v:.3f}" for w, v in by_width.items())
          + f" -> {'CONFIRMED' if holds else 'REFUTED'}")

    print(f"  P5  AUC stays below 0.95: {max(every_decode):.3f} at the best "
          f"cell -> {'CONFIRMED' if max(every_decode) < 0.95 else 'REFUTED'}")
    print("      Refuted would suggest the label is leaking into the "
          "measurement rather than being predicted -- the margin is a proxy "
          "for a property it cannot fully observe.")

    if mean_decode > USABLE_AUC:
        print("\n  WHAT A PERFECT GATE WOULD BE WORTH, from g13-03's split: "
              "+0.092 kept on the ambiguous half and -0.054 given back on the "
              "other. At the measured out-degree mix that is roughly +0.03 "
              "over search-everywhere, and it also stops paying for walks "
              "where they cannot help.")
        print("  This AUC is not 1.000, so the realised gain is less. The gate "
              "is worth building; the number to beat is search4's overall.")


if __name__ == "__main__":
    main()
