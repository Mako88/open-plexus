"""Score g18-05: does the store carry the relational tasks?

Five text sweeps say the store contributes ~nothing on text, and each one then
protects itself with the same sentence: *"this does not touch the relational
line."* That was an inference. This is the measurement.

**Accuracy, not bits.** MQAR asks for a recalled value and either gets it or does
not, and the trivial floor is what guessing scores rather than what uniform
scores — a model with nothing to recall from does not guess, it emits a constant.
"""

from __future__ import annotations

import statistics
from collections import defaultdict

from tools.recovery import load, require

#: P1's gate: accuracy by which `floor` must beat its ablation.
GATE = 0.30
#: P3's tolerance: how far the bias may move `floor` before the task has a base
#: rate nobody intended.
NO_HELP = 0.05


def main() -> None:
    records = require(load(), "arm", "bias", "seed", "accuracy", "trivial")
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    reference = records[0]
    cells: dict[tuple, list[dict]] = defaultdict(list)
    for record in records:
        cells[(record["bias"], record["arm"])].append(record)

    print(f"records {len(records)}, seeds {sorted({r['seed'] for r in records})}")
    print(f"MQAR: {reference['n_pairs']} pairs, length {reference['seq_len']}, "
          f"vocab {reference['vocab']}, width {reference['width']}")
    print(f"trivial floor {reference['trivial']} -- what GUESSING scores")

    def value(bias, arm):
        rows = cells.get((bias, arm), [])
        return statistics.mean([r["accuracy"] for r in rows]) if rows else None

    print("\naccuracy on held-out sequences, mean over seeds")
    for bias in (False, True):
        for arm in ("floor", "nostore"):
            got = value(bias, arm)
            print(f"  bias{int(bias)} {arm:<9} "
                  + (f"{got:.4f}" if got is not None else "--"))

    print("\nPREDICTIONS")
    verdicts = []
    for bias in (False, True):
        floor, ablated = value(bias, "floor"), value(bias, "nostore")
        if floor is None or ablated is None:
            continue
        gain = floor - ablated
        verdicts.append(gain > GATE)
        print(f"  P1  THE GATE, bias {int(bias)}. floor {floor:.4f} against "
              f"nostore {ablated:.4f}   {gain:+.4f}"
              f"{'   THE STORE CARRIES IT' if gain > GATE else ''}")
    if verdicts:
        print(f"      -> {'CONFIRMED' if all(verdicts) else 'REFUTED'}")
        if all(verdicts):
            print("      The protective sentence in decisions 139, 140 and 141 "
                  "is measured rather than assumed: the store does on this task "
                  "what it does not do on text, so the text results are about "
                  "TEXT being the wrong instrument.")
        else:
            print("      The store carries neither text nor the task it was "
                  "built for, and the architecture question stops being about "
                  "addressing entirely.")

    ablated = value(False, "nostore")
    if ablated is not None:
        ok = ablated <= reference["trivial"]
        print(f"  P2  THE RAIL. nostore at bias 0 does not exceed the trivial "
              f"floor {reference['trivial']}: {ablated:.4f} -> "
              f"{'CONFIRMED' if ok else 'REFUTED'}")
        if ablated < 0.01:
            print("      It sits at ZERO rather than at guessing, and the "
                  "distinction is the point: a model with nothing to retrieve "
                  "does not guess uniformly, it emits a constant. The trivial "
                  "floor is what a SMART guesser scores.")

    plain, biased = value(False, "floor"), value(True, "floor")
    if plain is not None and biased is not None:
        moved = abs(biased - plain)
        print(f"  P3  THE FALSIFIER. the bias does not move `floor`: "
              f"{plain:.4f} against {biased:.4f}, moved {moved:.4f} -> "
              f"{'CONFIRMED' if moved <= NO_HELP else 'REFUTED'}")
        # THE DIRECTION DECIDES THE MEANING, and the prediction was written
        # expecting only one of them. A bias that PAYS would mean the generator
        # has a base rate. A bias that COSTS means something else entirely, and
        # it is what actually happened.
        #
        # **This is ACCURACY, so higher is better** -- the first version of this
        # branch carried the bits convention over from the text summarisers and
        # printed the two explanations the wrong way round. Every other number
        # in the g18 line is bits; this file is the exception, and the exception
        # is where a habit goes wrong.
        if biased > plain + NO_HELP:
            print("      The bias PAYS, which it should not be able to: MQAR's "
                  "values are uniform by construction, so a base rate here "
                  "means the generator has one and every MQAR number in this "
                  "project is partly a base-rate score.")
        elif biased < plain - NO_HELP:
            print("      The bias COSTS, which the prediction did not "
                  "anticipate and which is the more interesting failure. A "
                  "prior with nothing to predict does not sit idle -- it "
                  "competes with the retrieval for the same readout, and on a "
                  "task with no exploitable marginals it is pure interference. "
                  "The mirror of the text result, where the prior wins and the "
                  "store adds nothing.")


if __name__ == "__main__":
    main()
