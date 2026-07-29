"""Score g18-02: can the store contribute a single positive bit at word level?

Decision 136 measured the tuned model at 9.185 bits/word and the same model with
nothing ever written to its store at 9.187. **Every word-level bit this model
earns is the readout bias.** So the question stopped being which address to use.

`single` keys make the store a bigram in vector form -- note 033's ceiling, and
here the ceiling is the point: a word bigram is 7.848, which beats the bias-only
model by 1.34 bits. If the store cannot approach that when addressed exactly the
way a bigram is addressed, the problem is the store rather than the address.

**Every arm is reported against `nostore` at its own width and key scheme**, not
against a single global baseline. A wider store with a wider bias is a different
control, and comparing across them would credit the bias for the width.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, require

#: P1's gate: bits below the matched `nostore` that count as the store
#: contributing something.
GATE = 0.10
#: P2's rail: how far `nostore` may move across width and key scheme before the
#: ablation is not ablating.
FLAT = 0.02


def dead(record: dict) -> bool:
    return bool(record["diverged"] or record.get("unstable"))


def main() -> None:
    records = require(load(), "kind", "keys", "width", "lr", "error",
                      "fit_error", "diverged")
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    reference = records[0]
    rates = sorted({r["lr"] for r in records}, reverse=True)
    print(f"records {len(records)}, rates {rates}")
    print(f"the bars: bigram {reference['bigram']:.3f}, "
          f"unigram {reference['unigram']:.3f}, "
          f"uniform {reference['uniform']:.3f}")

    gone = [r for r in records if dead(r)]
    print(f"\nRAILS\n  no measurement in {len(gone)} of {len(records)} cell(s)"
          + (f" -- {sorted({r['condition'] for r in gone})}" if gone else ""))

    cells: dict[tuple, list[dict]] = defaultdict(list)
    for record in records:
        cells[(record["keys"], record["width"], record["kind"],
               record["lr"])].append(record)

    print("\nbits per word on TEST text, by learning rate")
    print(f"  {'arm':<26}" + "".join(f"{r:>12}" for r in rates))
    configurations = sorted({(k[0], k[1]) for k in cells})
    for keys, width in configurations:
        for kind in ("floor", "nostore"):
            row = ""
            for rate in rates:
                rows = [r for r in cells.get((keys, width, kind, rate), [])
                        if not dead(r)]
                row += (f"{statistics.mean([r['error'] for r in rows]):>12.3f}"
                        if rows else f"{'--':>12}")
            print(f"  {f'{keys} d{width} {kind}':<26}{row}")

    def best(keys, width, kind):
        """Lowest bits at the rate chosen on held-out TRAINING text."""
        scored = [(rate, rows) for rate in rates
                  if (rows := [r for r in cells.get((keys, width, kind, rate),
                                                    []) if not dead(r)])]
        if not scored:
            return None, None, None
        rate, rows = min(
            scored,
            key=lambda pair: statistics.mean([r["fit_error"]
                                              for r in pair[1]]))
        return rate, statistics.mean([r["error"] for r in rows]), rows

    print("\nPREDICTIONS")
    print(f"  P1  THE GATE. some cell beats its matched `nostore` by more "
          f"than {GATE}:")
    passed = False
    for keys, width in configurations:
        rate, floor, _ = best(keys, width, "floor")
        _, ablated, _ = best(keys, width, "nostore")
        if floor is None or ablated is None:
            continue
        gain = ablated - floor
        passed = passed or gain > GATE
        print(f"        {keys:<7} d{width:<5} store {floor:.3f} against "
              f"nostore {ablated:.3f}   {gain:+.3f}"
              f"{'   BEATS IT' if gain > GATE else ''}   (lr {rate})")
    print(f"      -> {'CONFIRMED' if passed else 'REFUTED'}")
    if not passed:
        print("      The store contributes nothing at word level under any "
              "width or key scheme measured. That is not a result about "
              "addressing, and the architecture line needs a different "
              "question rather than a bigger sweep.")

    ablations = [statistics.mean([r["error"] for r in rows])
                 for keys, width in configurations
                 if (rows := [r for rate in rates
                              for r in cells.get((keys, width, "nostore",
                                                  rate), [])
                              if not dead(r)])]
    if len(ablations) > 1:
        spread = max(ablations) - min(ablations)
        print(f"  P2  THE RAIL. `nostore` is flat across width and key "
              f"scheme: spread {spread:.3f} -> "
              f"{'CONFIRMED' if spread <= FLAT else 'REFUTED'}")
        if spread > FLAT:
            print("      The ablation is not ablating -- something varies with "
                  "width or keys in a model that has no store, so every "
                  "comparison above is unreadable.")

    widest = max(w for _, w in configurations) if configurations else None
    _, single, _ = best("single", widest, "floor") if widest else (None, None,
                                                                   None)
    if single is not None:
        print(f"  P3  THE FALSIFIER. single keys at d{widest} do NOT reach the "
              f"bigram at {reference['bigram']:.3f}: {single:.3f} -> "
              f"{'CONFIRMED' if single > reference['bigram'] else 'REFUTED'}")
        if single <= reference["bigram"]:
            print("      The store reaches what its shape can hold, and pair "
                  "keys were the problem. That REOPENS the addressing line "
                  "rather than closing it.")


if __name__ == "__main__":
    main()
