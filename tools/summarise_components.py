"""One table of every component combination, and the contrasts that matter.

A grid named by composition — `keys=X,retrieval=Y,readout=Z,write=W` — is not
best read as raw cells. The interesting quantity is how a contrast CHANGES
between readouts, which is what decisions 74, 76 and 77 found: sparse keys
reversed, the cache shrank with dense keys and held with sparse, and pair keys
and settling partly recovered.

So this prints the table grouped by readout, then every non-baseline choice
against its baseline, under each readout.

**The component axes are read from the labels rather than hard-coded**, because
g11-07 swept three and g11-08 sweeps four. A tool that hard-codes one
experiment's shape is wrong about the next one, and this repository has paid for
that three times in a single reporting tool already.
"""

from __future__ import annotations

from tools.recovery import load, mean_and_error, require

#: The choice each component falls back to, so a contrast has something to be
#: measured against. Mirrors `experiments/components.py`.
BASELINE = {"keys": "dense", "retrieval": "plain", "readout": "linear",
            "write": "plain"}


def split_arm(arm: str) -> dict[str, str]:
    """`keys=dense,retrieval=plain` -> its choices, by component."""
    out = {}
    for piece in arm.split(","):
        name, _, choice = piece.partition("=")
        if choice:
            out[name.strip()] = choice.strip()
    return out


def main() -> int:
    rows = require(load(), "arm", "bits_calibrated", "seed")
    if not rows:
        print("no records matched")
        return 1
    if not all("=" in row["arm"] for row in rows):
        print("this grid is named by composition; some arm is not a spec")
        return 1

    parsed = [(split_arm(row["arm"]), row["bits_calibrated"]) for row in rows]
    parts = sorted({part for spec, _ in parsed for part in spec})
    cells: dict[tuple, list[float]] = {}
    for spec, value in parsed:
        cells.setdefault(tuple(spec.get(p, "?") for p in parts), []).append(value)

    # Group by `readout` when present, because that is the component every
    # re-validation is against.
    group = "readout" if "readout" in parts else parts[0]
    index = parts.index(group)
    others = [p for p in parts if p != group]
    groupings = sorted({key[index] for key in cells})

    print(f"vocabulary {rows[0]['vocab_size']}, {len(cells)} combinations, "
          f"grouped by {group}")
    print()
    for choice in groupings:
        print(f"== {group} = {choice} ==")
        for key in sorted(k for k in cells if k[index] == choice):
            spec = ", ".join(f"{p}={key[parts.index(p)]}" for p in others)
            mean, error = mean_and_error(cells[key])
            print(f"   {mean:>7.3f} +/-{error:.3f}   {spec}")
        print()

    print("== every non-baseline choice against its baseline ==")
    print("   positive means the choice is BETTER than the baseline")
    print()
    for part in others:
        at = parts.index(part)
        base = BASELINE.get(part)
        choices = sorted({key[at] for key in cells} - {base})
        if not choices:
            continue
        print(f"  {part}  (baseline {base})")
        for choice in choices:
            line = f"    {choice:>16}: "
            for grouping in groupings:
                gaps = []
                for key in cells:
                    if key[at] != choice or key[index] != grouping:
                        continue
                    reference = tuple(base if i == at else v
                                      for i, v in enumerate(key))
                    if reference in cells:
                        gaps.append(mean_and_error(cells[reference])[0]
                                    - mean_and_error(cells[key])[0])
                if gaps:
                    line += (f"{grouping}={sum(gaps) / len(gaps):+.3f} "
                             f"(n={len(gaps)})  ")
            print(line)
        print()

    dead = disconnected(cells, parts, others, index, groupings)
    if dead:
        print("== A CHOICE THAT CHANGED NOTHING, ANYWHERE ==")
        for part, choice in dead:
            print(f"  {part}={choice} is bit-identical to its baseline in every")
            print(f"  cell. **That is what a DISCONNECTED flag looks like**, and")
            print(f"  what a small real effect does not -- a null has noise in")
            print(f"  it. Check the mechanism is reachable before reading this")
            print(f"  as a result.")
        print()

    print("A contrast that reverses or collapses between readouts was never a")
    print("property of the mechanism -- decisions 74, 76 and 77.")
    return 0


def disconnected(cells, parts, others, index, groupings) -> list[tuple]:
    """Choices whose every cell exactly equals its baseline's.

    **g11-08's first run reported the write gate as having no effect at all**,
    +0.000 in four cells across two seeds. It was not a null: `write_gate` does
    nothing unless `corrective_writes` is on, which the field's own docstring
    says, and the arm never enabled it. A real null wobbles; an exact zero
    everywhere is a flag that never reached the model.

    Reported rather than raised, because a genuinely inert setting is possible
    and the reader should decide -- but it must never pass unremarked.
    """
    found = []
    for part in others:
        at = parts.index(part)
        base = BASELINE.get(part)
        for choice in sorted({key[at] for key in cells} - {base}):
            gaps = []
            for key in cells:
                if key[at] != choice:
                    continue
                reference = tuple(base if i == at else v
                                  for i, v in enumerate(key))
                if reference in cells:
                    gaps.append(mean_and_error(cells[reference])[0]
                                - mean_and_error(cells[key])[0])
            if gaps and all(gap == 0.0 for gap in gaps):
                found.append((part, choice))
    return found


if __name__ == "__main__":
    raise SystemExit(main())
