"""Score g18-04: does g17-01's premise survive the training-target correction?

g17-01 concluded *"the model does not learn word-level text at all"* — 90,000
words buying 0.038 bits over uniform — and that is the finding note 042's
architecture pass was built on. Decision 138 found its training call targets the
current token where the model predicts the next, so the premise was measured on a
mistrained readout.

This runs its exact configuration with the target fixed and nothing else changed.
**A reproduction, not an improvement**: any other difference would answer a
different question.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, require

#: What g17-01 recorded for its floor, at width 256, two epochs, lr 0.05, no cap,
#: bias off. The number this run exists to check.
G17_01_FLOOR = 10.721
#: P1's tolerance on that reproduction.
CLOSE = 0.10
#: P2's tolerance on "a model with no store and no prior sits at uniform".
AT_UNIFORM = 0.05


def dead(record: dict) -> bool:
    return bool(record["diverged"] or record.get("unstable"))


def main() -> None:
    records = require(load(), "kind", "bias", "error", "diverged")
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    cells: dict[tuple, list[dict]] = defaultdict(list)
    for record in records:
        cells[(record["bias"], record["kind"])].append(record)
    reference = records[0]

    print(f"records {len(records)}, "
          f"width {reference['width']}, epochs {reference.get('epochs')}, "
          f"lr {reference['lr']}, cap {reference['cap']}")
    print(f"the bars: unigram {reference['unigram']:.3f}, "
          f"bigram {reference['bigram']:.3f}, "
          f"uniform {reference['uniform']:.3f}")
    print(f"g17-01 recorded its floor at {G17_01_FLOOR}")

    gone = [r for r in records if dead(r)]
    print(f"\nRAILS\n  no measurement in {len(gone)} of {len(records)} cell(s)")

    def value(bias, kind):
        rows = [r for r in cells.get((bias, kind), []) if not dead(r)]
        return statistics.mean([r["error"] for r in rows]) if rows else None

    print("\nbits per word on TEST text")
    for bias in (False, True):
        for kind in ("floor", "nostore"):
            got = value(bias, kind)
            print(f"  bias{int(bias)} {kind:<9} "
                  + (f"{got:.3f}" if got is not None else "--"))

    floor, ablated = value(False, "floor"), value(False, "nostore")
    print("\nPREDICTIONS")
    if floor is not None:
        off = abs(floor - G17_01_FLOOR)
        learned = reference["uniform"] - floor
        print(f"  P1  THE GATE. corrected floor reproduces g17-01's "
              f"{G17_01_FLOOR} within {CLOSE}: {floor:.3f}, off by {off:.3f} -> "
              f"{'CONFIRMED' if off <= CLOSE else 'REFUTED'}")
        print(f"      It learns {learned:.3f} bits over uniform where g17-01 "
              f"reported 0.038.")
        if off > CLOSE:
            print("      THE PREMISE DOES NOT REPRODUCE. The architecture pivot "
                  "of 2026-07-28 was made on a number this harness cannot "
                  "recover, and note 042's starting point needs restating "
                  "before anything else rests on it.")

    if ablated is not None:
        off = abs(ablated - reference["uniform"])
        print(f"  P2  THE RAIL. no store and no prior sits at uniform "
              f"{reference['uniform']:.3f} within {AT_UNIFORM}: "
              f"{ablated:.3f}, off by {off:.3f} -> "
              f"{'CONFIRMED' if off <= AT_UNIFORM else 'REFUTED'}")

    if floor is not None:
        print(f"  P3  THE FALSIFIER. the corrected floor does NOT beat the word "
              f"unigram at {reference['unigram']:.3f}: {floor:.3f} -> "
              f"{'CONFIRMED' if floor > reference['unigram'] else 'REFUTED'}")
        if floor <= reference["unigram"]:
            print("      The correction rescues the premise entirely, and "
                  "everything built on 'word level is unlearnable' needs "
                  "revisiting rather than extending.")

    with_bias, ablated_bias = value(True, "floor"), value(True, "nostore")
    if with_bias is not None and ablated_bias is not None:
        print(f"\nAND THE SAME CONFIGURATION WITH A PRIOR AVAILABLE")
        print(f"  floor {with_bias:.3f} against nostore {ablated_bias:.3f}   "
              f"the store is worth {ablated_bias - with_bias:+.3f}")
        print("  Decision 139's claim, at g17-01's own configuration rather "
              "than at a tuned one.")


if __name__ == "__main__":
    main()
