"""One table of every component combination, and the three contrasts that matter.

g11-07 re-validates the comparison set against a composed readout. The grid is
`keys x retrieval x readout`, so the interesting quantities are not the raw cells
but the DIFFERENCES that change sign or size between readouts — which is what
decisions 74 and 76 found one mechanism at a time.

So this prints the table, then the same three contrasts under each readout:

    the cache's advantage       cache128 against plain
    sparsity's advantage        sparse4 against dense
    the pair-key penalty        pair against dense

A contrast that reverses or collapses between readouts is a mechanism that was
never a property of the mechanism.
"""

from __future__ import annotations

from tools.recovery import load, mean_and_error, require

#: Component order in an arm label, matching `experiments/components.py`.
PARTS = ("keys", "retrieval", "readout")


def split_arm(arm: str) -> dict[str, str]:
    """`keys=dense,retrieval=plain,readout=linear` -> its three choices."""
    out = {}
    for piece in arm.split(","):
        name, _, choice = piece.partition("=")
        out[name.strip()] = choice.strip()
    return out


def main() -> int:
    rows = require(load(), "arm", "bits_calibrated", "seed")
    if not rows:
        print("no records matched")
        return 1
    if not all("=" in r["arm"] for r in rows):
        print("this grid is named by composition; some arm is not a spec")
        return 1

    cells: dict[tuple[str, str, str], list[float]] = {}
    for row in rows:
        parts = split_arm(row["arm"])
        key = tuple(parts.get(p, "?") for p in PARTS)
        cells.setdefault(key, []).append(row["bits_calibrated"])

    readouts = sorted({k[2] for k in cells})
    keysets = sorted({k[0] for k in cells})
    retrievals = sorted({k[1] for k in cells})

    print(f"vocabulary {rows[0]['vocab_size']}, "
          f"{len(cells)} combinations, bits per character\n")
    for readout in readouts:
        print(f"== readout = {readout} ==")
        print(f"{'keys':>10}" + "".join(f"{r:>18}" for r in retrievals))
        for keyset in keysets:
            line = f"{keyset:>10}"
            for retrieval in retrievals:
                values = cells.get((keyset, retrieval, readout))
                if not values:
                    line += f"{'missing':>18}"
                    continue
                mean, error = mean_and_error(values)
                line += f"{mean:>12.3f} +/-{error:.3f}"
            print(line)
        print()

    def gap(readout, keyset, better, worse):
        a = cells.get((keyset, better, readout))
        b = cells.get((keyset, worse, readout))
        if not a or not b:
            return None
        return mean_and_error(b)[0] - mean_and_error(a)[0]

    print("== the contrasts, and whether they survive the readout ==")
    print("  positive means the first named is BETTER\n")
    for label, better, worse, axis in (
            ("cache128 over plain", "cache128", "plain", "retrieval"),
            ("sparse4 over dense", "sparse4", "dense", "keys"),
            ("pair over dense", "pair", "dense", "keys")):
        print(f"  {label}")
        for readout in readouts:
            parts = []
            others = keysets if axis == "retrieval" else retrievals
            for other in others:
                if axis == "retrieval":
                    value = gap(readout, other, better, worse)
                else:
                    a = cells.get((better, other, readout))
                    b = cells.get((worse, other, readout))
                    value = (mean_and_error(b)[0] - mean_and_error(a)[0]
                             if a and b else None)
                parts.append(f"{other}={value:+.3f}" if value is not None
                             else f"{other}=--")
            print(f"    {readout:>10}: " + "  ".join(parts))
        print()

    print("A contrast that reverses or collapses between readouts was never a")
    print("property of the mechanism. That is what decisions 74 and 76 found")
    print("one mechanism at a time, and what this grid asks all at once.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
