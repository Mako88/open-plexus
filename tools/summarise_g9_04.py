"""Does any local signal separate a binding-write from a filler-write?

This is not a recovery sweep, so `tools/recovery.py`'s two refusals do not apply
— there is no floor arm and no oracle gap. The quantity is an **AUC**, and it has
its own version of the same discipline: **per-seed values, never a mean alone**,
because an AUC averaged across seeds that disagree in DIRECTION would report 0.5
and call it "no signal" when the truth is "two opposite signals".

Direction is never folded away. A signal that separates the classes backwards is
still usable — a tag can admit the low end as easily as the high end — and
`max(a, 1 - a)` would hide exactly the finding this probe exists to find.
"""

from __future__ import annotations

from collections import defaultdict

from tools.recovery import load

#: An AUC has to clear this to count as separation rather than noise. It is
#: deliberately generous: the question here is "is there anything at all to hang
#: a mechanism on", and a near-miss is worth seeing rather than rounding away.
MARGIN = 0.05


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    by_signal: dict[tuple, list] = defaultdict(list)
    for r in rows:
        by_signal[(r["signal"], r["width"])].append(r)

    widths = sorted({r["width"] for r in rows})
    signals = sorted({r["signal"] for r in rows},
                     key=lambda s: [r["signal"] for r in rows].index(s))
    density = [r["steps_per_binding"] for r in rows]
    print(f"\nsteps per binding: {sum(density) / len(density):.1f}")
    print("A window spans STEPS; a tag's capacity spans ADMITTED ITEMS. That")
    print("ratio is how much a tag could buy, if anything separates.")

    for question in ("binding_vs_filler", "rewarded_vs_unrewarded"):
        print(f"\n=== {question.replace('_', ' ').upper()} ===")
        if question == "rewarded_vs_unrewarded":
            print("The generator picks rewarded cues uniformly, so every number")
            print("here should be noise around 0.5. Anything else is a LEAK and")
            print("invalidates the column above it.")
        print(f"{'signal':>20}{'width':>7}   {'per-seed AUC':<40}"
              f"{'mean':>6}{'verdict':>14}")
        for signal in signals:
            for width in widths:
                cells = sorted(by_signal[(signal, width)],
                               key=lambda r: r["seed"])
                values = [c[question] for c in cells]
                if not values:
                    continue
                mean = sum(values) / len(values)
                per_seed = " ".join(f"{v:.3f}" for v in values)
                if all(v > 0.5 + MARGIN for v in values):
                    verdict = "separates"
                elif all(v < 0.5 - MARGIN for v in values):
                    verdict = "INVERTED"
                elif (any(v > 0.5 + MARGIN for v in values)
                      and any(v < 0.5 - MARGIN for v in values)):
                    # The case a mean would hide entirely.
                    verdict = "DISAGREES"
                else:
                    verdict = "noise"
                print(f"{signal:>20}{width:>7}   {per_seed:<40}"
                      f"{mean:>6.3f}{verdict:>14}")

    print("\nanything separates (either direction) -> a fixed capacity over")
    print("  ADMITTED items reaches any delay. Build the tag, admitting on that")
    print("  signal and in that direction.")
    print("everything noise                      -> admission is a random subset,")
    print("  so a small tag is WORSE than a small window, and that is a result")
    print("  about the whole line of work rather than about the tag.")
    print("`hit` noise                           -> predict-the-future-and-")
    print("  compare carries nothing HERE, measured directly rather than")
    print("  inferred from a mechanism that could have failed six other ways.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
