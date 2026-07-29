"""Score g18-01: does storing by CONCEPT make word-level text learnable?

g17-01 found the model 2.65 bits worse than a word unigram at word level, and
found the reason: with pair keys over surfaces almost every address is written
once, so the store memorises hapaxes. This scores the fix -- addressing the store
by groups of words rather than by words.

**Two controls, because there are two ways to be wrong.** `permuted` matches the
address-space statistics and destroys the meaning; `shuffled` destroys the
meaning at the source. An arm that beats `floor` but not its controls is a much
smaller finding: fewer addresses however chosen.

**Two rails, and they are read FIRST.** A diverged cell has no bits to report,
and a cell whose temperature pinned at the edge of the grid understates its own
error -- neither may be compared with anything.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, require

KIND_ORDER = ("floor", "concept", "stratified", "permuted", "shuffled")
#: P1's gate: bits below `floor` that count as the mechanism doing something.
GATE = 0.10
#: P2's margin: bits by which a real grouping must beat a structureless one.
MARGIN = 0.05


def mean(rows: list[dict], field: str = "error") -> float:
    return statistics.mean([r[field] for r in rows])


def main() -> None:
    records = require(load(), "kind", "groups", "error", "addresses",
                      "diverged", "pinned", "bias", "cap")
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    seeds = sorted({r["seed"] for r in records})
    print(f"records {len(records)}, seeds {seeds}, "
          f"biases {sorted({r['bias'] for r in records})}, "
          f"caps {sorted({r['cap'] for r in records})}")
    if len({r["condition"] for r in records}) != len(records):
        print("!! duplicate condition strings -- two jobs wrote the same cell")

    # THE RAILS FIRST. Everything below is unreadable where these fire.
    diverged = [r for r in records if r["diverged"]]
    unstable = [r for r in records if r.get("unstable")]
    pinned = [r for r in records if r["pinned"]]
    print(f"\nRAILS")
    print(f"  diverged {len(diverged)} of {len(records)}"
          + (f" -- all at cap "
             f"{sorted({r['cap'] for r in diverged})}" if diverged else ""))
    print(f"  UNSTABLE (finite, and worse than uniform) {len(unstable)}"
          + (f" -- {sorted({r['condition'] for r in unstable})}"
             if unstable else ""))
    print(f"  temperature pinned at a grid edge in {len(pinned)} cell(s)"
          + (" -- those bits are an OVERSTATEMENT of the error by an unknown "
             "amount" if pinned else ""))

    # Both rails drop the cell. An unstable cell has a number and it is not a
    # measurement -- averaging it into an arm would move that arm by tens of
    # bits and the mean would still look like an ordinary result.
    live = [r for r in records if not r["diverged"] and not r.get("unstable")]
    for cap in sorted({r["cap"] for r in live}):
        for bias in sorted({r["bias"] for r in live}):
            block = [r for r in live
                     if r["cap"] == cap and r["bias"] == bias]
            if block:
                report(block, cap, bias)


def report(block: list[dict], cap: float, bias: bool) -> None:
    cells: dict[tuple[str, int], list[dict]] = defaultdict(list)
    for record in block:
        cells[(record["kind"], record["groups"])].append(record)
    groups = sorted({k[1] for k in cells if k[0] != "floor"})
    kinds = [k for k in KIND_ORDER if any(c[0] == k for c in cells)]

    print(f"\n=== store cap {cap}, readout bias {int(bias)} "
          f"{'(the comparison set config)' if not bias else ''}")
    print("bits per WORD, lower is better")
    print(f"  {'arm':<12}" + "".join(f"{g:>10}" for g in groups))
    for kind in kinds:
        if kind == "floor":
            continue
        row = "".join(f"{mean(cells[(kind, g)]):>10.3f}"
                      if (kind, g) in cells else f"{'--':>10}"
                      for g in groups)
        print(f"  {kind:<12}{row}")
    reference = block[0]
    floor = mean(cells[("floor", 0)]) if ("floor", 0) in cells else None
    print(f"  {'floor':<12}"
          + (f"{floor:>10.3f}   one concept per word" if floor is not None
             else f"{'--':>10}   NOT RUN in this block"))
    print(f"  {'unigram':<12}{reference['unigram']:>10.3f}   the bar")
    print(f"  {'bigram':<12}{reference['bigram']:>10.3f}")
    print(f"  {'uniform':<12}{reference['uniform']:>10.3f}")

    print("\naddresses in training, and mean times each is written")
    print(f"  {'arm':<12}" + "".join(f"{g:>10}" for g in groups))
    for kind in kinds:
        if kind == "floor":
            continue
        row = "".join(f"{mean(cells[(kind, g)], 'addresses'):>10,.0f}"
                      if (kind, g) in cells else f"{'--':>10}" for g in groups)
        print(f"  {kind:<12}{row}")
    if floor is not None:
        print(f"  {'floor':<12}"
              f"{mean(cells[('floor', 0)], 'addresses'):>10,.0f}")

    if floor is None:
        print("\nno floor in this block, so P1 and P2 cannot be scored here")
        return
    predictions(cells, groups, floor, reference)


def predictions(cells, groups, floor: float, reference: dict) -> None:
    def best(kind: str) -> tuple[int, float]:
        scored = [(g, mean(cells[(kind, g)])) for g in groups
                  if (kind, g) in cells]
        return min(scored, key=lambda pair: pair[1])

    print("\nPREDICTIONS")
    k, value = best("concept")
    gain = floor - value
    print(f"  P1  THE GATE. the best concept arm beats floor by more than "
          f"{GATE}: K={k} at {value:.3f} against {floor:.3f}, "
          f"{gain:+.3f} -> {'CONFIRMED' if gain > GATE else 'REFUTED'}")
    if gain <= GATE:
        print("      Storing by concept does not make word-level text "
              "learnable. The address space collapsed as designed (the table "
              "above), so sparsity was not the binding constraint -- which is "
              "a refutation of the account, not of the wiring.")

    verdicts = []
    for control in ("permuted", "shuffled"):
        if (control, k) not in cells:
            continue
        against = mean(cells[(control, k)])
        beat = against - value
        verdicts.append(beat > MARGIN)
        print(f"  P2  CONTROL. concept-{k} beats {control}-{k} by more than "
              f"{MARGIN}: {value:.3f} against {against:.3f}, "
              f"{beat:+.3f} -> {'CONFIRMED' if beat > MARGIN else 'REFUTED'}")
    if verdicts and not all(verdicts):
        print("      A control matching the arm says the gain is the address "
              "space shrinking rather than the grouping meaning anything. "
              "That is a real finding and a smaller one: the cheap fix is "
              "fewer addresses, however chosen.")

    values = [mean(cells[("concept", g)]) for g in groups
              if ("concept", g) in cells]
    interior = (len(values) > 2
                and min(values) < values[0] and min(values) < values[-1])
    print(f"  P3  bits per word is non-monotonic in K, with an interior "
          f"minimum: {[round(v, 3) for v in values]} -> "
          f"{'CONFIRMED' if interior else 'REFUTED'}")

    floor_addresses = mean(cells[("floor", 0)], "addresses")
    print(f"  P4  RAIL. the grouping reaches the store: addresses fall and "
          f"recurrence rises at every K.")
    print(f"      Falls LINEARLY in the grouping ratio, not as its square -- "
          f"the square was predicted and the pre-dispatch measurement had "
          f"already refuted it. Both columns are printed so the shape is "
          f"readable rather than asserted.")
    for g in groups:
        if ("concept", g) not in cells:
            continue
        rows = cells[("concept", g)]
        ratio = mean(rows, "concepts") / reference["vocab"]
        seen = mean(rows, "addresses") / floor_addresses
        print(f"        K={g:<5} concepts {ratio:.3f} of vocab, "
              f"addresses {seen:.3f} of floor, "
              f"the ratio squared would be {ratio ** 2:.3f}  "
              f"recurrence {mean(rows, 'recurrence'):.1f}")
    collapsed = all(mean(cells[("concept", g)], "addresses") < floor_addresses
                    for g in groups if ("concept", g) in cells)
    print(f"      -> {'CONFIRMED' if collapsed else 'REFUTED'}"
          + ("" if collapsed else
             "  -- the grouping is NOT reaching the store, so every row above "
             "is measuring the same model and nothing here is readable"))

    reached = [f"{kind}-{g}" for kind in KIND_ORDER for g in groups
               if (kind, g) in cells
               and mean(cells[(kind, g)]) <= reference["unigram"]]
    print(f"  P5  no arm reaches the word unigram at "
          f"{reference['unigram']:.3f} -> "
          f"{'CONFIRMED' if not reached else 'REFUTED'}")
    if reached:
        print(f"      REACHED BY {', '.join(reached)}. That is the headline, "
              f"and every claim about what this model cannot do at word level "
              f"needs rewriting.")


if __name__ == "__main__":
    main()
