"""Score g13-03: does search beat traversal, and does traversal beat what we had?

Two refusals are being re-tested at once. Decision 107 declined the pair-key
traversal (*"a perfect traversal buys 0.05"*) and decision 111 declined search
(*"you cannot search your way out of noisy primitives"*). Both were correct
arithmetic on the numbers available then, and g13-01 and g13-02 measured both
conditions away.

**`walk` is the control that matters**, not `concat`. It is traversal with no
search on top, so without it any gain would be credited to search when traversal
might have supplied all of it.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load

ARM_ORDER = ("concat", "walk", "search4", "search8")


def spread(values: list[float]) -> str:
    if not values:
        return "--"
    if len(values) < 2:
        return f"{values[0]:.3f} (1 seed)"
    return (f"{statistics.mean(values):.3f} "
            f"+/-{statistics.stdev(values) / len(values) ** 0.5:.3f}")


def paired(cells: dict, a: str, b: str) -> tuple[float, float]:
    """`a` minus `b`, computed INSIDE each seed and then averaged.

    A seed whose data ran easy inflates every arm in it; dividing once at the end
    charges the mechanism for that. CLAUDE.md's per-seed-values rule.
    """
    left = {r["seed"]: r["accuracy"] for r in cells.get(a, [])}
    right = {r["seed"]: r["accuracy"] for r in cells.get(b, [])}
    shared = sorted(set(left) & set(right))
    if not shared:
        return 0.0, 0.0
    diffs = [left[s] - right[s] for s in shared]
    if len(diffs) < 2:
        return diffs[0], 0.0
    return (statistics.mean(diffs),
            statistics.stdev(diffs) / len(diffs) ** 0.5)


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
    seeds = sorted({r["seed"] for r in records})
    print(f"arms {arms}, records {len(records)}, seeds {seeds}")

    conditions = {r["condition"] for r in records}
    if len(conditions) != len(records):
        print(f"!! {len(records) - len(conditions)} duplicate condition strings")

    floors = records[0]["floors"]
    floor = max(floors["majority"], floors["first"])
    print(f"\nfloors: first {floors['first']:.3f}  "
          f"majority {floors['majority']:.3f}  -> the bar is {floor:.3f}")

    print("\naccuracy")
    for arm in arms:
        overall = [r["accuracy"] for r in cells[arm]]
        one = [r["by_out_degree"]["1"]["accuracy"] for r in cells[arm]
               if r["by_out_degree"]["1"]["accuracy"] is not None]
        many = [r["by_out_degree"]["2+"]["accuracy"] for r in cells[arm]
                if r["by_out_degree"]["2+"]["accuracy"] is not None]
        flag = "  CLEARS FLOOR" if statistics.mean(overall) > floor else ""
        print(f"  {arm:9s} overall {spread(overall):>18s}   "
              f"k=1 {spread(one):>18s}   k>=2 {spread(many):>18s}{flag}")

    print("\nPREDICTIONS")

    gain, err = paired(cells, "walk", "concat")
    print(f"  P1  walk beats concat by more than 0.10: {gain:+.3f} "
          f"+/-{err:.3f} -> {'CONFIRMED' if gain > 0.10 else 'REFUTED'}")
    print("      Decision 107 declined traversal on the arithmetic of its day. "
          "This says whether that verdict survives the primitives moving.")

    gain, err = paired(cells, "search4", "walk")
    beats = gain > 0
    decisive = gain > 2 * err if err > 0 else beats
    print(f"  P2  search4 beats walk: {gain:+.3f} +/-{err:.3f} -> "
          f"{'CONFIRMED' if beats else 'REFUTED'}")
    print(f"      THE DECISION-RELEVANT ONE, and the margin matters as much as "
          f"the sign: {'above' if decisive else 'INSIDE'} 2 SE. If search only "
          f"ties walk, decision 107's traversal was the whole gain and the "
          f"search on top is decoration.")

    if "search4" in cells:
        best = statistics.mean([r["accuracy"] for r in cells["search4"]])
        print(f"  P3  search4 clears the shortcut floor {floor:.3f}: "
              f"{best:.3f} -> {'CONFIRMED' if best > floor else 'REFUTED'}")
        print("      No mechanism on this task has ever cleared it.")

    gain, err = paired(cells, "search8", "search4")
    print(f"  P4  search8 within 0.02 of search4: {gain:+.3f} +/-{err:.3f} -> "
          f"{'CONFIRMED' if abs(gain) < 0.02 else 'REFUTED'}")

    tops = {arm: statistics.mean([r["accuracy"] for r in cells[arm]])
            for arm in arms}
    highest = max(tops.values())
    print(f"  P5  every arm falls short of 1.000: best is {highest:.3f} -> "
          f"{'CONFIRMED' if highest < 0.999 else 'REFUTED'}")
    print("      g13-02's 1.000 ceiling holds steps 1 and 3 at their "
          "out-degree-1 value. Search has to FIND that regime and will not "
          "always, so the gap between this and 1.000 is how often it fails to.")

    # The split that says whether search did the job it was built for. Gaining
    # only at out-degree 1 would mean the branches are not resolving ambiguity,
    # which is the entire claim.
    if "search4" in cells and "walk" in cells:
        for bucket in ("1", "2+"):
            left = {r["seed"]: r["by_out_degree"][bucket]["accuracy"]
                    for r in cells["search4"]}
            right = {r["seed"]: r["by_out_degree"][bucket]["accuracy"]
                     for r in cells["walk"]}
            pairs = [(left[s], right[s]) for s in sorted(set(left) & set(right))
                     if left[s] is not None and right[s] is not None]
            if pairs:
                diff = statistics.mean([a - b for a, b in pairs])
                print(f"\n  search4 - walk at out-degree {bucket}: {diff:+.3f}")
        print("  Search exists for the out-degree >= 2 case. A gain that sits "
              "at out-degree 1 instead would mean the branches are not "
              "resolving ambiguity and something else moved.")


if __name__ == "__main__":
    main()
