"""Score g18-00: where is the learning rate at WORD level?

**This exists because a local probe found the standing account may be an
artefact.** g17-01 concluded the model does not learn word-level text at all --
90,000 words buying 0.038 bits over uniform. Every one of those cells ran at
`lr=0.05`, which is the value every character-level sweep used. At 20,000 words
with a readout bias, dropping it moved the surface floor from 10.186 to 9.804,
and it was still improving.

If the same holds at 90,000 words, "the model cannot learn word-level text" is a
statement about one hyper-parameter rather than about the model, and g18-01's
whole comparison would have been run against a handicapped floor.

**Sorted by `fit_error`, never by `error`.** `fit_error` is measured on held-out
TRAINING text; `error` is the test number this run exists to protect. A learning
rate chosen by the test set is a learning rate fitted to the test set.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, require


def dead(record: dict) -> bool:
    """Cells that carry no measurement, for either reason.

    `diverged` is a NaN. `unstable` is finite and worse than uniform, which is
    the case that nearly got into a table: no error anywhere, 36.9 bits against
    a 10.759 uniform, and an arm mean that would absorb it and still look
    ordinary.
    """
    return bool(record["diverged"] or record.get("unstable"))


def main() -> None:
    records = require(load(), "kind", "groups", "lr", "cap", "error",
                      "fit_error", "diverged")
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    rates = sorted({r["lr"] for r in records}, reverse=True)
    # THE ARM IS (kind, K, cap). A cap is not a nuisance parameter here: the
    # whole question is what each arm NEEDS in order to run at all, so a capped
    # arm and an uncapped one are two candidates rather than one arm measured
    # twice.
    arms = sorted({(r["kind"], r["groups"], r["cap"]) for r in records},
                  key=lambda a: (a[0] != "floor", a))
    cells: dict[tuple, list[dict]] = defaultdict(list)
    for record in records:
        cells[(record["kind"], record["groups"], record["cap"],
               record["lr"])].append(record)

    reference = records[0]
    print(f"records {len(records)}, rates {rates}, "
          f"train words {reference['train_words']:,}")
    print(f"the bar: unigram {reference['unigram']:.3f}, "
          f"bigram {reference['bigram']:.3f}, "
          f"uniform {reference['uniform']:.3f}")

    for label, field in (("held-out TRAINING text (what a rate is chosen by)",
                          "fit_error"),
                         ("TEST text (reported, never sorted by)", "error")):
        print(f"\nbits per word, {label}")
        print(f"  {'arm':<16}" + "".join(f"{r:>10}" for r in rates))
        for kind, groups, cap in arms:
            row = ""
            for rate in rates:
                rows = cells.get((kind, groups, cap, rate))
                if not rows:
                    row += f"{'--':>10}"
                elif all(dead(r) for r in rows):
                    row += (f"{'DIVERGED':>10}"
                            if all(r["diverged"] for r in rows)
                            else f"{'UNSTABLE':>10}")
                else:
                    row += (f"{statistics.mean([r[field] for r in rows
                                                if not dead(r)]):>10.3f}")
            name = (kind if kind == "floor" else f"{kind}-{groups}")
            print(f"  {name + f' cap{cap:g}':<16}{row}")

    print("\nTHE CHOICE, by held-out training text")
    for kind, groups, cap in arms:
        scored = [(rate,
                   statistics.mean([r["fit_error"]
                                    for r in cells[(kind, groups, cap, rate)]
                                    if not dead(r)]))
                  for rate in rates
                  if cells.get((kind, groups, cap, rate))
                  and not all(dead(r)
                              for r in cells[(kind, groups, cap, rate)])]
        if not scored:
            continue
        best, value = min(scored, key=lambda pair: pair[1])
        name = (kind if kind == "floor" else f"{kind}-{groups}") + f" cap{cap:g}"
        edge = " AT THE EDGE OF THE GRID -- extend it before trusting this" \
            if best in (rates[0], rates[-1]) else ""
        print(f"  {name:<16} lr {best:<8} fit {value:.3f}{edge}")

    # AGAINST g17-01's OWN CONFIGURATION, which is cap 0 -- the model's default.
    # Comparing its floor against a capped one would be two changes.
    floors = [(rate, statistics.mean([r["error"] for r in rows
                                      if not dead(r)]))
              for (kind, groups, cap, rate), rows in sorted(cells.items())
              if kind == "floor" and not cap
              and not all(dead(r) for r in rows)]
    if floors:
        stock = dict(floors).get(0.05)
        best = min(floors, key=lambda pair: pair[1])
        print(f"\nWHAT THIS SAYS ABOUT g17-01  (uncapped, its own setting)")
        print(f"  floor at lr 0.05, the value every earlier cell used: "
              f"{stock if stock is None else round(stock, 3)}")
        print(f"  floor at its best rate ({best[0]}): {best[1]:.3f}")
        if stock is not None and stock - best[1] > 0.10:
            print(f"  -> {stock - best[1]:.3f} bits of the 'the model does not "
                  f"learn word-level text' finding was the LEARNING RATE. "
                  f"g17-01's conclusion needs restating and g18-01 must run at "
                  f"a rate chosen per arm.")
        else:
            print("  -> the rate does not account for it; g17-01's conclusion "
                  "stands as written.")


if __name__ == "__main__":
    main()
