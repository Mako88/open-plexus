"""Score g13-02, and compute the traversal-with-search ceiling from it.

The number this exists to check is decision 107's **0.960** for step 2, which the
0.87 traversal ceiling was derived from and which came from an inline probe that
left no script behind.

The ceiling is computed here rather than by hand because the hand version is what
went unrecorded last time.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load

#: Decision 107's figure for step 2, and the tolerance P1 was registered at.
RECORDED_STEP2, TOLERANCE = 0.960, 0.10

#: From g13-01, run 30389532519: steps 1 and 3 are the same operation,
#: `key(FACT, X) -> X's relation`, measured at out-degree 1 across 8 seeds.
#: Search's job is to put steps 1 and 3 INTO that regime by trying a candidate
#: and checking where it lands, so this is the right factor for the ceiling.
STEP_1_AND_3_AT_OUT_DEGREE_1 = 1.000

#: Decision 107's hand-derived target, the number that justified the build.
TARGET_CEILING = 0.87


def spread(values: list[float]) -> str:
    if len(values) < 2:
        return f"{values[0]:.3f} (1 seed)"
    return (f"{statistics.mean(values):.3f} "
            f"+/-{statistics.stdev(values) / len(values) ** 0.5:.3f}")


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
    print(f"cells {len(cells)}, records {len(records)}, "
          f"seeds {sorted({r['seed'] for r in records})}")

    conditions = {r["condition"] for r in records}
    if len(conditions) != len(records):
        print(f"!! {len(records) - len(conditions)} duplicate condition "
              f"strings -- two runs wrote the same cell")

    print("\nstep 2, key(S, R) -> O")
    for width in widths:
        overall = [r["accuracy"] for r in cells[width]]
        unique = [r["by_sharing"]["unique"]["accuracy"] for r in cells[width]
                  if r["by_sharing"]["unique"]["accuracy"] is not None]
        shared = [r["by_sharing"]["shared"]["accuracy"] for r in cells[width]
                  if r["by_sharing"]["shared"]["accuracy"] is not None]
        n_shared = sum(r["by_sharing"]["shared"]["n"] for r in cells[width])
        n_all = sum(r["by_sharing"]["shared"]["n"]
                    + r["by_sharing"]["unique"]["n"] for r in cells[width])
        print(f"  d{width:<4d} overall {spread(overall):>18s}   "
              f"unique {spread(unique):>18s}   "
              f"shared {spread(shared) if shared else '--':>18s}   "
              f"(shared is {n_shared}/{n_all} = {n_shared / n_all:.1%} "
              f"of sequences)")

    print("\nPREDICTIONS")

    base = [r["accuracy"] for r in cells[widths[0]]]
    off = abs(statistics.mean(base) - RECORDED_STEP2)
    print(f"  P1 CONTROL  step 2 at width {widths[0]} within {TOLERANCE:.2f} of "
          f"decision 107's {RECORDED_STEP2:.3f}: {statistics.mean(base):.3f}, "
          f"off by {off:.3f} -> {'CONFIRMED' if off <= TOLERANCE else 'REFUTED'}")

    clears = []
    for width in widths:
        unique = [r["by_sharing"]["unique"]["accuracy"] for r in cells[width]
                  if r["by_sharing"]["unique"]["accuracy"] is not None]
        clears.append((width, statistics.mean(unique)))
    all_clear = all(value >= 0.99 for _, value in clears)
    print("  P2  unique (S, R) clears 0.99 at every width: "
          + ", ".join(f"d{w} {v:.3f}" for w, v in clears)
          + f" -> {'CONFIRMED' if all_clear else 'REFUTED'}")

    shared_all = [r["by_sharing"]["shared"]["accuracy"] for width in widths
                  for r in cells[width]
                  if r["by_sharing"]["shared"]["accuracy"] is not None]
    if shared_all:
        # 1/m where m is the sharing count. Two is overwhelmingly the case that
        # occurs, so 0.5 is the bound to read against -- printed rather than
        # asserted, because the bucket is small.
        print(f"  P3  shared (S, R) falls toward 1/m: "
              f"{statistics.mean(shared_all):.3f} against a 1/2 bound of 0.500 "
              f"-> {'CONFIRMED' if statistics.mean(shared_all) < 0.75 else 'REFUTED'}")
        print(f"      SMALL BUCKET -- {len(shared_all)} cell-values. Directional "
              f"only, and it is not the number the build rests on.")

    gain = statistics.mean([r["accuracy"] for r in cells[widths[-1]]]) - \
        statistics.mean(base)
    print(f"  P4  step 2 gains less than 0.03 from d{widths[0]} to "
          f"d{widths[-1]}: {gain:+.3f} -> "
          f"{'CONFIRMED' if abs(gain) < 0.03 else 'REFUTED'}")

    best_unique = max(value for _, value in clears)
    ceiling = (STEP_1_AND_3_AT_OUT_DEGREE_1 * best_unique
               * STEP_1_AND_3_AT_OUT_DEGREE_1)
    print(f"\n  P5  THE DECISION. Traversal-with-search ceiling = "
          f"step1@k=1 ({STEP_1_AND_3_AT_OUT_DEGREE_1:.3f}) x "
          f"step2@unique ({best_unique:.3f}) x "
          f"step3@k=1 ({STEP_1_AND_3_AT_OUT_DEGREE_1:.3f}) = {ceiling:.3f}")
    print(f"      against decision 107's hand-derived {TARGET_CEILING:.2f} -> "
          f"{'CONFIRMED' if ceiling >= TARGET_CEILING else 'REFUTED'}")
    print("      This is the RETRIEVAL CHAIN only. Composition on top of clean "
          "retrievals was measured at 1.000 by decision 102 (concat, over the "
          "whole rule table), so it is not expected to be the binding term -- "
          "but it is a separate factor and is NOT measured here.")
    print("      Steps 1 and 3 are held at their out-degree-1 value because "
          "that is precisely what search is for. If search does not reach that "
          "regime, this ceiling is not reached either.")


if __name__ == "__main__":
    main()
