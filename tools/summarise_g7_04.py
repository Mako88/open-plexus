"""Where does forgetting start to pay, and does consolidation ever help?

Decay is expressed as a HALF-LIFE in units of the sequence, so a row means the
same thing at every length. `none` is a memory that never fades.

Two questions, and the sweep was reframed by its own controls to put them in this
order: (1) at what length does forgetting beat not-forgetting, and (2) does
consolidation add anything on top -- expected not to, and recorded either way.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

sys.path.insert(0, ".")
from tools.grid import pinned  # noqa: E402


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    lengths = sorted({r["seq_len"] for r in rows})
    halves = sorted({r["half_life"] for r in rows if r["half_life"] is not None},
                    reverse=True)
    consols = sorted({r["consolidation"] for r in rows})
    rates = sorted({r["lr"] for r in rows})

    def best(seq_len, half_life, consolidation):
        """This arm at its own best learning rate, averaged over seeds."""
        top = None
        for lr in rates:
            got = [r["overall"] for r in rows
                   if r["seq_len"] == seq_len and r["half_life"] == half_life
                   and r["consolidation"] == consolidation and r["lr"] == lr]
            if got and (top is None or sum(got) / len(got) > top[0]):
                top = (sum(got) / len(got), lr,
                       sorted(round(g, 3) for g in got))
        return top

    chosen = []
    print()
    print("OVERALL ACCURACY, each arm at its own best learning rate")
    header = "".join(f"h={h}".rjust(10) for h in halves)
    print(f"{'seq_len':>8}{'no decay':>10}{header}{'  best forgetting arm':>24}")
    forgetting_wins = {}
    for seq_len in lengths:
        never = best(seq_len, None, 0.0)
        cells, fading = [], {}
        for half_life in halves:
            arm = best(seq_len, half_life, 0.0)
            if arm:
                fading[half_life] = arm[0]
                chosen.append(arm[1])
            cells.append(f"{arm[0]:>10.3f}" if arm else f"{'-':>10}")
        if never:
            chosen.append(never[1])
        if not never or not fading:
            continue
        winner = max(fading, key=fading.get)
        margin = fading[winner] - never[0]
        forgetting_wins[seq_len] = margin
        print(f"{seq_len:>8}{never[0]:>10.3f}" + "".join(cells)
              + f"   h={winner} by {margin:+.3f}".rjust(24))

    print()
    print("DOES FORGETTING PAY?")
    paying = [s for s, m in forgetting_wins.items() if m > 0.02]
    if paying:
        print(f"  Yes, from seq_len {min(paying)} upward. Margins: "
              + ", ".join(f"{s}:{forgetting_wins[s]:+.3f}"
                          for s in sorted(forgetting_wins)))
        print("  g1-06 measured decay as unhelpful at seq_len 96 and wrote that "
              "it might matter once unbounded accumulation became the problem. "
              "This is where that begins.")
    else:
        print("  No, at any length tested. Not forgetting wins throughout, and "
              "g1-06's conjecture does not hold in this range.")

    print()
    print("DOES CONSOLIDATION ADD ANYTHING ON TOP?")
    helped = []
    for seq_len in lengths:
        for half_life in halves:
            plain = best(seq_len, half_life, 0.0)
            if not plain:
                continue
            for consolidation in consols:
                if consolidation == 0.0:
                    continue
                arm = best(seq_len, half_life, consolidation)
                if arm and arm[0] > plain[0] + 0.02:
                    helped.append((seq_len, half_life, consolidation,
                                   arm[0] - plain[0]))
    if helped:
        print("  Yes, somewhere:")
        for seq_len, half_life, consolidation, gain in helped:
            print(f"    seq={seq_len} half-life={half_life} "
                  f"rate={consolidation}: {gain:+.3f}")
    else:
        print("  No -- nowhere in the grid does it beat plain forgetting by more "
              "than 0.02. The lasting store never decays, so every confirmed "
              "retrieval adds to it permanently and it accumulates the same "
              "saturation the fast store was fading to avoid.")

    print()
    message = pinned(chosen, rates)
    print("  LEARNING-RATE GRID: " + (message or "contained its answer."))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
