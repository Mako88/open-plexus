"""Score g13-05: does gating search on ambiguity beat searching everywhere?

**The number to beat is `search4`, not `walk`.** A gate that merely matches
search-everywhere has bought compute savings and no accuracy -- worth having, and
not what this was for.

Paired within seed throughout: a seed whose data ran easy inflates every arm in
it, and dividing once at the end charges the mechanism for that.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, paired_difference as paired, spread

ARM_ORDER = ("walk", "search4", "gate-q25", "gate-q50", "gate-q75")
GATES = ("gate-q25", "gate-q50", "gate-q75")

#: g13-03's split says a PERFECT gate keeps +0.092 and gives back -0.054, which
#: at the measured out-degree mix is about +0.03 over search-everywhere. P5 says
#: the real gate lands under that, because AUC 0.803 is not 1.000.
PERFECT_GATE_GAIN = 0.03


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

    floors = records[0]["floors"]
    floor = max(floors["majority"], floors["first"])
    print(f"\nfloor to clear: {floor:.3f}")

    print("\naccuracy, and where the gate fired")
    for arm in arms:
        rows = cells[arm]
        fired = [r["fired"] for r in rows if r["fired"] is not None]
        thresholds = [r["threshold"] for r in rows
                      if r.get("threshold") is not None]
        print(f"  {arm:10s} {spread([r['accuracy'] for r in rows]):>18s}"
              f"   k=1 {spread([r['by_out_degree']['1']['accuracy'] for r in rows]):>18s}"
              f"   k>=2 {spread([r['by_out_degree']['2+']['accuracy'] for r in rows]):>18s}"
              f"   fired {(f'{statistics.mean(fired):.0%}' if fired else '--'):>5s}"
              f"   thr {(f'{statistics.mean(thresholds):.3f}' if thresholds else '--'):>6s}")

    print("\nPREDICTIONS")

    beats = []
    for gate in GATES:
        if gate in cells:
            gain, err = paired(cells, gate, "search4")
            beats.append((gate, gain, err))
            print(f"      {gate} - search4: {gain:+.3f} +/-{err:.3f}")
    any_beats = any(gain > 0 for _, gain, _ in beats)
    print(f"  P1  THE DECISION. at least one gate beats search4 -> "
          f"{'CONFIRMED' if any_beats else 'REFUTED'}")
    if beats:
        best_arm, best_gain, best_err = max(beats, key=lambda row: row[1])
        decisive = best_gain > 2 * best_err if best_err > 0 else best_gain > 0
        print(f"      best is {best_arm} at {best_gain:+.3f} +/-{best_err:.3f}, "
              f"{'above' if decisive else 'INSIDE'} 2 SE")

    over_walk = [(gate, paired(cells, gate, "walk")[0]) for gate in GATES
                 if gate in cells]
    all_over = all(gain > 0 for _, gain in over_walk)
    print(f"  P2  every gate beats walk: "
          + ", ".join(f"{g} {v:+.3f}" for g, v in over_walk)
          + f" -> {'CONFIRMED' if all_over else 'REFUTED'}")

    if beats:
        best_arm = max(beats, key=lambda row: row[1])[0]
        fired = [r["fired"] for r in cells[best_arm] if r["fired"] is not None]
        if fired:
            rate = statistics.mean(fired)
            print(f"  P3  the best gate ({best_arm}) fires on under half the "
                  f"positions: {rate:.0%} -> "
                  f"{'CONFIRMED' if rate < 0.5 else 'REFUTED'}")

    rates = [(g, statistics.mean([r["fired"] for r in cells[g]
                                  if r["fired"] is not None]))
             for g in GATES if g in cells]
    monotone = all(rates[i][1] <= rates[i + 1][1] for i in range(len(rates) - 1))
    print("  P4  RAIL. firing rate rises with the quantile: "
          + ", ".join(f"{g} {v:.0%}" for g, v in rates)
          + f" -> {'CONFIRMED' if monotone else 'REFUTED'}")
    print("      If the quantile does not order the firing rate, the threshold "
          "is not doing what its name says and nothing above is readable.")

    if beats:
        best_gain = max(gain for _, gain, _ in beats)
        print(f"  P5  the gain over search4 is under the perfect-gate "
              f"{PERFECT_GATE_GAIN}: {best_gain:+.3f} -> "
              f"{'CONFIRMED' if best_gain < PERFECT_GATE_GAIN else 'REFUTED'}")
        print("      Refuted would mean the gate beat a ceiling derived from "
              "g13-03's own split, which would put one of the two "
              "measurements in question rather than being good news.")

    # The mechanism's whole claim, stated where it can be checked at a glance.
    if "walk" in cells and "search4" in cells and beats:
        best_arm = max(beats, key=lambda row: row[1])[0]
        for bucket, want in (("1", "walk"), ("2+", "search4")):
            values = {arm: statistics.mean(
                [r["by_out_degree"][bucket]["accuracy"] for r in cells[arm]])
                for arm in (best_arm, "walk", "search4")}
            print(f"\n  out-degree {bucket}: {best_arm} {values[best_arm]:.3f}, "
                  f"walk {values['walk']:.3f}, search4 {values['search4']:.3f}"
                  f"  -- the gate should track {want} here")


if __name__ == "__main__":
    main()
