"""Score g15-01: does a persistent slow store break decision 63's wall?

Note 042's falsifier. If persistence does not move the 16,000-character wall,
the account that the model has nowhere to keep a concept map is wrong, and the
architectural proposal resting on it goes with it.

**P4 is what makes a refuted P3 interpretable.** Consolidation fires on
`predictions[t-1] == token` -- it promotes what the model already got right --
so "persistence does not help" and "the gate never opened" would be the same
number without the counter.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, spread

ARM_ORDER = ("baseline", "consolidate", "persist", "persist-slow",
             "persist-slow-decay")

#: Where the wall is. **Movement is split here rather than measured across the
#: whole range**, because the first pass asked for total movement and counted
#: the pre-wall improvement decision 63 never disputed -- the control looked
#: refuted while reproducing exactly. A statistic has to be chosen for what it
#: would detect.
WALL = 16_000

#: Decision 63's own figures, at the same width, chunk and corpus. The control
#: is a REPRODUCTION, not a re-derivation.
DECISION_63 = {4_000: 5.570, 8_000: 5.543, 16_000: 5.527,
               32_000: 5.523, 62_500: 5.531, 125_000: 5.531}

#: Its measured seed spread. A movement under this is noise.
SEED_SPREAD = 0.04

#: The backprop baseline at 1,000,000 characters (g11-05).
BACKPROP = 4.049


def main() -> None:
    records = load()
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    cells: dict[tuple[str, int], list[dict]] = defaultdict(list)
    for record in records:
        cells[(record["arm"], record["chars"])].append(record)
    arms = [a for a in ARM_ORDER if any(k[0] == a for k in cells)]
    points = sorted({k[1] for k in cells})
    print(f"arms {arms}, points {points}, records {len(records)}, "
          f"seeds {sorted({r['seed'] for r in records})}")
    if len({r["condition"] for r in records}) != len(records):
        print("!! duplicate condition strings -- two runs wrote the same cell")

    print("\nbits per character, lower is better")
    header = "".join(f"{c:>12,}" for c in points)
    print(f"  {'arm':<12}{header}")
    for arm in arms:
        row = "".join(
            f"{statistics.mean([r['bits'] for r in cells[(arm, c)]]):>12.4f}"
            if (arm, c) in cells else f"{'--':>12}" for c in points)
        print(f"  {arm:<12}{row}")
    print(f"  {'decision 63':<12}"
          + "".join(f"{DECISION_63.get(c, float('nan')):>12.4f}"
                    for c in points))

    def mean(arm: str, chars: int) -> float:
        return statistics.mean([r["bits"] for r in cells[(arm, chars)]])

    def movement(arm: str, low: int, high: int) -> float:
        """Bits GAINED going from `low` to `high`. Positive is improvement."""
        return mean(arm, low) - mean(arm, high)

    print("\nPREDICTIONS")

    # PAST THE WALL, not across it, and measured as a TREND rather than as one
    # pairwise difference. The first pass did both the other way and got a
    # sound control reading as refuted and a trendless row reading as
    # confirmed -- see decision 131.
    after = [c for c in points if c >= WALL]

    def past_the_wall(arm: str) -> float:
        """Bits gained from the wall to the largest point. Positive improves."""
        return mean(arm, after[0]) - mean(arm, after[-1])

    def monotone(arm: str) -> bool:
        """Does it improve at EVERY step past the wall, not just end to end?

        A single pair can be satisfied by one outlier. This cannot.
        """
        values = [mean(arm, c) for c in after]
        return all(b <= a + 1e-9 for a, b in zip(values, values[1:]))

    if ("baseline", after[0]) in cells:
        moved = past_the_wall("baseline")
        print(f"  P1  CONTROL. baseline is flat PAST the wall "
              f"({after[0]:,} to {after[-1]:,}): {moved:+.4f} -> "
              f"{'CONFIRMED' if abs(moved) < SEED_SPREAD else 'REFUTED'}")
        print(f"      Decision 63's shape is that movement is FRONT-LOADED. "
              f"Before the wall this arm moves "
              f"{movement('baseline', points[0], after[0]):+.4f}.")

    if ("consolidate", after[0]) in cells:
        moved = past_the_wall("consolidate")
        print(f"  P2  consolidate is also flat past the wall: {moved:+.4f} -> "
              f"{'CONFIRMED' if abs(moved) < SEED_SPREAD else 'REFUTED'}")

    print(f"  P3  THE GATE. a persistent arm improves past the wall, "
          f"MONOTONICALLY and by more than the {SEED_SPREAD} seed spread:")
    broke = False
    for arm in [a for a in arms if a.startswith("persist")]:
        moved, steady = past_the_wall(arm), monotone(arm)
        good = moved > SEED_SPREAD and steady
        broke = broke or good
        print(f"        {arm:<18} {moved:+.4f}  "
              f"{'monotone' if steady else 'NOT monotone'}  "
              f"{'BREAKS THE WALL' if good else ''}")
    print(f"      -> {'CONFIRMED' if broke else 'REFUTED'}")
    print("      Monotone is required because one high outlier satisfies an "
          "end-to-end difference -- which is how the first pass reported a "
          "trendless row as confirmed (decision 131).")

    counts = {c: statistics.mean([r["consolidations"]
                                  for r in cells[("persist", c)]])
              for c in points if ("persist", c) in cells}
    if counts:
        print("  P4  RAIL. consolidations grow with data and are well above "
              "zero: "
              + ", ".join(f"{c:,}->{v:,.0f}" for c, v in counts.items()))
        smallest = min(counts.values())
        rising = all(counts[a] <= counts[b]
                     for a, b in zip(points, points[1:]) if a in counts
                     and b in counts)
        ok = smallest > 0 and rising
        print(f"      -> {'CONFIRMED' if ok else 'REFUTED'}")
        if smallest <= 0:
            print("      **THE GATE NEVER OPENED.** Consolidation fires on "
                  "`predictions[t-1] == token`, so it promotes only what the "
                  "model already got right. A refuted P3 above says nothing "
                  "about persistence -- it says the store stayed empty, and "
                  "the next experiment is about the GATE, not the store.")

    if ("persist", points[-1]) in cells:
        best = mean("persist", points[-1])
        print(f"  P5  persist does not reach the backprop baseline "
              f"{BACKPROP}: {best:.4f} -> "
              f"{'CONFIRMED' if best > BACKPROP else 'REFUTED'}")

    # THE DIAGNOSTIC THAT SAVED THE FIRST PASS. Printed for every persistent
    # arm, because the whole point of the cap sweep is watching where the norm
    # stops tracking the corpus.
    print("\n  slow-store norm at the end of training, per arm:")
    for arm in [a for a in arms if a.startswith("persist")]:
        norms = {c: statistics.mean([r["lasting_norm"] for r in cells[(arm, c)]])
                 for c in points if (arm, c) in cells}
        if not norms:
            continue
        values = list(norms.values())
        pinned = max(values) - min(values) < 1e-6
        print(f"    {arm:<18} "
              + ", ".join(f"{c // 1000}k->{v:.1f}" for c, v in norms.items())
              + ("   PINNED -- full before the smallest data point" if pinned
                 else ""))
    if arms:
        print("  A norm that stops growing while characters keep rising means "
              "`lasting_cap` is binding, which caps what persistence can buy "
              "regardless of how much text arrives.")


if __name__ == "__main__":
    main()
