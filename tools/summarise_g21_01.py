"""Score g21-01: does the beam beat root-only search where `run()` actually runs?

`run()` has called `search` since decision 123 and has never called `beam`. The gap
usually quoted for the two is **0.6588 against 0.8877 chain recovery** -- CLUTRR, chain
lengths 2 to 10, scoring whether the true relation sequence was recovered. **None of
those three things is what `run()` does**, so that number is not evidence about this
wiring, and this sweep exists to get evidence that is.

Note 064's diagnosis is why the depth matters: the relation decode is 0.974 at the root
and about 0.91 mid-chain, and `search` hedges at the root while committing after. At
`hops = 2` there is exactly ONE mid-chain decode to get wrong, so most of the room the
beam exploits is absent by construction.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, paired_difference as paired, spread

ARM_ORDER = ("walk", "search4", "beam4", "beam4-k2")


def main() -> None:
    records = load()
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure of "
              "the run rather than a result")
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
    print(f"\nfloors: first {floors['first']:.3f}  "
          f"majority {floors['majority']:.3f}  -> the bar is {floor:.3f}")

    print("\naccuracy")
    for arm in arms:
        rows = cells[arm]
        overall = [r["accuracy"] for r in rows]
        one = [r["by_out_degree"]["1"]["accuracy"] for r in rows
               if r["by_out_degree"]["1"]["accuracy"] is not None]
        many = [r["by_out_degree"]["2+"]["accuracy"] for r in rows
                if r["by_out_degree"]["2+"]["accuracy"] is not None]
        flag = "  CLEARS FLOOR" if statistics.mean(overall) > floor else ""
        print(f"  {arm:9s} overall {spread(overall):>18s}   "
              f"k=1 {spread(one):>18s}   k>=2 {spread(many):>18s}{flag}")

    print("\nPREDICTIONS")

    gain, err = paired(cells, "beam4", "search4")
    decisive = gain > 2 * err if err > 0 else gain > 0
    print(f"  GATE  beam4 beats search4 by >= 0.01: {gain:+.3f} +/-{err:.3f} -> "
          f"{'CONFIRMED' if gain >= 0.01 else 'REFUTED'}")
    print(f"        {'Above' if decisive else 'INSIDE'} 2 SE. Decides whether "
          f"`search_beam_width` defaults to 4 or stays 0. **A refusal is not a null "
          f"result**: it says the CLUTRR gap was about chain DEPTH rather than about "
          f"the mechanism, which belongs in the tree either way.")

    gain, err = paired(cells, "beam4", "walk")
    print(f"  RAIL  beam4 beats walk by > 0.05: {gain:+.3f} +/-{err:.3f} -> "
          f"{'CONFIRMED' if gain > 0.05 else 'REFUTED'}")
    print("        If this fails the branching is inert in `run()`, and the GATE "
          "above is then a comparison between two arms that both did nothing.")

    gain, err = paired(cells, "beam4-k2", "beam4")
    print(f"  FALS  beam4-k2 within 0.02 of beam4: {gain:+.3f} +/-{err:.3f} -> "
          f"{'CONFIRMED' if abs(gain) < 0.02 else 'REFUTED'}")
    print("        Note 102 measured the rendezvous PERIOD as worth nothing "
          "measurable on CLUTRR, which is what lets a migrating walk meet d_max. A "
          "larger gap here refutes that on a second task and `prune_every=2` stops "
          "being free.")

    # The split that says whether branching did the job it exists for. A gain
    # sitting at out-degree 1 would be luck in the endpoint score rather than
    # ambiguity resolved -- and the overall column cannot tell the two apart.
    if "beam4" in cells and "search4" in cells:
        print()
        for bucket in ("1", "2+"):
            left = {r["seed"]: r["by_out_degree"][bucket]["accuracy"]
                    for r in cells["beam4"]}
            right = {r["seed"]: r["by_out_degree"][bucket]["accuracy"]
                     for r in cells["search4"]}
            pairs = [(left[s], right[s])
                     for s in sorted(set(left) & set(right))
                     if left[s] is not None and right[s] is not None]
            if pairs:
                diff = statistics.mean([a - b for a, b in pairs])
                print(f"  beam4 - search4 at out-degree {bucket}: {diff:+.3f}")
        print("  The beam exists for the out-degree >= 2 case, where "
              "`key(FACT, e)` holds a SUM of relations.")


if __name__ == "__main__":
    main()
