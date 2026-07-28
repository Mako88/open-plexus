"""Score g13-01 against its five registered predictions.

The question is whether WIDTH fixes retrieval fidelity on the task, and the
decision it gates is whether search gets rebuilt: decision 111 refused search
because *"you cannot search your way out of noisy primitives, because the
verifier is built from the primitives."* If width makes the primitives reliable
and only decision 108's AMBIGUITY remains, that refusal expires.

**Paired within seed, never across.** A seed whose data happened to be easy
inflates every arm in it, and dividing once at the end charges the mechanism for
that. CLAUDE.md's *per-seed values, not means* rule exists because the ratio was
the one place it had not been done.
"""

from __future__ import annotations

import glob
import json
import statistics
from collections import defaultdict

#: P3's tolerance. Decision 108 says "correct" TRACKS 1/k; within this of the
#: bound is tracking, outside it is something else happening.
NEAR_ONE_OVER_K = 0.10


def load(pattern: str = "out/*.json") -> list[dict]:
    records: list[dict] = []
    for path in sorted(glob.glob(pattern)):
        with open(path, encoding="utf-8") as handle:
            records.extend(json.load(handle))
    return records


def by_cell(records: list[dict]) -> dict[tuple[str, int], list[dict]]:
    cells: dict[tuple[str, int], list[dict]] = defaultdict(list)
    for record in records:
        cells[(record["arm"], record["width"])].append(record)
    return cells


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

    cells = by_cell(records)
    arms = sorted({arm for arm, _ in cells})
    widths = sorted({width for _, width in cells})
    seeds = sorted({r["seed"] for r in records})
    print(f"cells {len(cells)}, records {len(records)}, seeds {seeds}")

    # Every record carries the parameters it ACTUALLY ran with. Rule 11b: assert
    # on that before reading a number off it.
    conditions = {r["condition"] for r in records}
    if len(conditions) != len(records):
        print(f"!! {len(records) - len(conditions)} duplicate condition "
              f"strings -- two runs wrote the same cell, and one of them is "
              f"being silently dropped")

    print("\noverall accuracy, paired within seed")
    for arm in arms:
        row = "  ".join(
            f"d{width} {spread([r['accuracy'] for r in cells[(arm, width)]])}"
            for width in widths)
        print(f"  {arm:14s} {row}")

    # PER ARM. The two arms run the task at DIFFERENT DEPTHS, so they do not
    # share a floor -- and taking floors from records[0] scored P5 against
    # 1.000, which nothing can clear and which therefore passed vacuously.
    #
    # At hops=1 the path IS a single relation, so "guess from the first
    # relation" is the answer by construction and `first` is 1.000. That is a
    # property of the depth, not a leak, and it makes `first` meaningless as a
    # floor for the one-hop arm -- there `majority` is the floor that bites.
    floors_by_arm = {arm: cells[(arm, widths[0])][0]["floors"] for arm in arms}
    print("\nfloors, PER ARM -- the arms run at different depths and do not "
          "share one")
    for arm, floors in floors_by_arm.items():
        note = ("  <- first is 1.000 BY CONSTRUCTION at hops=1; use majority"
                if floors["first"] > 0.999 else "")
        print(f"  {arm:14s} first {floors['first']:.3f}  "
              f"majority {floors['majority']:.3f}  "
              f"(last {floors['last']:.3f}, ends {floors['ends']:.3f} are "
              f"BOUNDS not floors){note}")

    print("\nby out-degree of the queried subject")
    for arm in arms:
        for width in widths:
            parts = []
            for k in ("1", "2", "3+"):
                values = [r["by_out_degree"][k]["accuracy"]
                          for r in cells[(arm, width)]
                          if r["by_out_degree"][k]["accuracy"] is not None]
                bound = cells[(arm, width)][0]["by_out_degree"][k]["one_over_k"]
                parts.append(f"k={k} {spread(values) if values else '--':>16s} "
                             f"(1/k {bound:.3f})")
            print(f"  {arm:14s} d{width:<4d} " + "  ".join(parts))

    print("\nPREDICTIONS")

    def paired_gain(arm: str, lo: int, hi: int, key=None) -> list[float]:
        """Width gain computed INSIDE each seed, never between means."""
        low = {r["seed"]: r for r in cells.get((arm, lo), [])}
        high = {r["seed"]: r for r in cells.get((arm, hi), [])}

        def value(record):
            if key is None:
                return record["accuracy"]
            return record["by_out_degree"][key]["accuracy"]

        return [value(high[s]) - value(low[s])
                for s in sorted(set(low) & set(high))
                if value(high[s]) is not None and value(low[s]) is not None]

    k1_256 = [r["by_out_degree"]["1"]["accuracy"]
              for r in cells.get(("hop1-pair", 256), [])
              if r["by_out_degree"]["1"]["accuracy"] is not None]
    k1_64 = [r["by_out_degree"]["1"]["accuracy"]
             for r in cells.get(("hop1-pair", 64), [])
             if r["by_out_degree"]["1"]["accuracy"] is not None]
    if k1_256 and k1_64:
        rose = statistics.mean(k1_256) - statistics.mean(k1_64)
        clears = statistics.mean(k1_256) >= 0.99
        print(f"  P1 CONTROL  out-degree 1 rises with width and clears 0.99 "
              f"at 256: rose {rose:+.3f}, at 256 "
              f"{statistics.mean(k1_256):.3f} -> "
              f"{'CONFIRMED' if (rose > 0 and clears) else 'REFUTED'}")
        if clears and rose <= 0:
            print("     NOTE: already at ceiling at width 64. The prediction "
                  "assumed 0.915 there, from decision 112's isolated ablation. "
                  "A TRAINED READOUT is the difference and it is not a defect "
                  "in either measurement -- see the sweep file.")

    overall_256 = [r["accuracy"] for r in cells.get(("hop1-pair", 256), [])]
    if overall_256:
        print(f"  P2  hop1-pair overall does not reach 1.000 at 256: "
              f"{statistics.mean(overall_256):.3f} -> "
              f"{'CONFIRMED' if statistics.mean(overall_256) < 0.999 else 'REFUTED'}")

    within = True
    detail = []
    for width in widths:
        for k in ("2", "3+"):
            values = [r["by_out_degree"][k]["accuracy"]
                      for r in cells.get(("hop1-pair", width), [])
                      if r["by_out_degree"][k]["accuracy"] is not None]
            if not values:
                continue
            bound = cells[("hop1-pair", width)][0]["by_out_degree"][k]["one_over_k"]
            gap = statistics.mean(values) - bound
            detail.append(f"d{width} k={k} {gap:+.3f}")
            if abs(gap) > NEAR_ONE_OVER_K:
                within = False
    print(f"  P3  out-degree >=2 within {NEAR_ONE_OVER_K:.2f} of 1/k at every "
          f"width: {', '.join(detail)} -> "
          f"{'CONFIRMED' if within else 'REFUTED'}")
    print("      P3 IS THE DECISION-RELEVANT ONE. Confirmed means decision "
          "108's ambiguity account holds and SEARCH is the next build, because "
          "its verifier is no longer built from noisy primitives. Refuted "
          "means the fidelity story is incomplete and search is not next.")

    gain1 = paired_gain("hop1-pair", min(widths), max(widths))
    gain2 = paired_gain("hop2-concat", min(widths), max(widths))
    if gain1 and gain2:
        print(f"  P4  hop2-concat gains less from width than hop1-pair: "
              f"{statistics.mean(gain2):+.3f} vs {statistics.mean(gain1):+.3f} "
              f"-> {'CONFIRMED' if statistics.mean(gain2) < statistics.mean(gain1) else 'REFUTED'}")

    hop2_256 = [r["accuracy"] for r in cells.get(("hop2-concat", 256), [])]
    if hop2_256:
        hop2_floors = floors_by_arm["hop2-concat"]
        floor = max(hop2_floors["majority"], hop2_floors["first"])
        scored = statistics.mean(hop2_256)
        print(f"  P5  hop2-concat at 256 does not clear its OWN shortcut floor "
              f"{floor:.3f}: {scored:.3f} -> "
              f"{'CONFIRMED' if scored <= floor else 'REFUTED'}")
        if scored < hop2_floors["majority"]:
            print("      AND IT IS BELOW THE MAJORITY FLOOR, which is worse "
                  "than the prediction says: a model scoring under 'always "
                  "answer the commonest relation' is not weakly composing, it "
                  "is actively mispredicting.")


if __name__ == "__main__":
    main()
