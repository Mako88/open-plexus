"""Does letting a weak machine into the pool help or hurt?

The number that matters is `mixed - alone`: what a strong machine gains, or
loses, by admitting a weaker one. If it is ever negative by more than noise, open
participation needs admission control -- which is coordination, which is what this
project exists to avoid.

`doubled` is the fair control. A weak machine helping less than an equally-sized
strong one is expected and uninteresting; a weak machine helping less than NOTHING
is the finding.
"""

from __future__ import annotations

import glob
import json
import sys
from collections import defaultdict

sys.path.insert(0, ".")
from tools.grid import pinned  # noqa: E402

NOISE = 0.02


def main() -> int:
    rows = [r for f in glob.glob(sys.argv[1] if len(sys.argv) > 1 else "out/*.json")
            for r in json.load(open(f))]
    if not rows:
        print("no records matched")
        return 1

    strongs = sorted({r["strong"] for r in rows})
    weaks = sorted({r["weak"] for r in rows})
    rates = sorted({r["lr"] for r in rows})
    modes = sorted({r["mode"] for r in rows})

    chosen, harmed = defaultdict(list), []
    for mode in modes:
        # One learning rate per mode, ranked on the largest strong machine alone,
        # since membership is a read-time choice on one trained model.
        best_lr, best_score = None, None
        for lr in rates:
            got = [r["alone"] for r in rows if r["mode"] == mode
                   and r["lr"] == lr and r["strong"] == max(strongs)]
            if got and (best_score is None or sum(got) / len(got) > best_score):
                best_lr, best_score = lr, sum(got) / len(got)
        if best_lr is None:
            continue
        chosen[mode].append(best_lr)

        print()
        print(f"=== {mode} : what a strong machine gains by admitting a weak one "
              f"(lr {best_lr}) ===")
        print(f"{'strong':>7}{'alone':>8}{'+strong':>9}"
              + "".join(f"+w={w}".rjust(9) for w in weaks))
        for strong in strongs:
            cells = [r for r in rows if r["mode"] == mode
                     and r["lr"] == best_lr and r["strong"] == strong]
            if not cells:
                continue
            alone = sum(c["alone"] for c in cells) / len(cells)
            doubled = sum(c["doubled"] for c in cells) / len(cells)
            gains = []
            for weak in weaks:
                got = [c["mixed"] for c in cells if c["weak"] == weak]
                if not got:
                    gains.append(f"{'-':>9}")
                    continue
                delta = sum(got) / len(got) - alone
                gains.append(f"{delta:>+9.3f}")
                if delta < -NOISE:
                    harmed.append((mode, strong, weak, delta))
            print(f"{strong:>7}{alone:>8.3f}{doubled - alone:>+9.3f}"
                  + "".join(gains))

    print()
    print("DOES ADMITTING A WEAK MACHINE EVER HURT?")
    if harmed:
        print(f"  YES, in {len(harmed)} of the cells tested:")
        for mode, strong, weak, delta in sorted(harmed, key=lambda h: h[3])[:8]:
            print(f"    {mode}: a {strong}-node machine admitting {weak} nodes "
                  f"loses {delta:+.3f}")
        print("  Open participation therefore needs weighting or admission "
              "control, which is coordination -- the thing this project exists "
              "to avoid. This is the most consequential negative available here.")
    else:
        print("  No, nowhere by more than the noise threshold. Anyone can join "
              "and the network only improves, so participation needs no "
              "gatekeeping and no coordination.")

    print()
    print("CONTROL: does adding a SECOND STRONG machine still help?")
    for mode in modes:
        lr = chosen[mode][0]
        flat = []
        for strong in strongs:
            cells = [r for r in rows if r["mode"] == mode and r["lr"] == lr
                     and r["strong"] == strong]
            if not cells:
                continue
            alone = sum(c["alone"] for c in cells) / len(cells)
            doubled = sum(c["doubled"] for c in cells) / len(cells)
            if doubled - alone <= NOISE:
                flat.append(strong)
        if flat:
            print(f"  {mode}: doubling gains nothing at strong={flat} -- pooling "
                  f"has saturated there and no membership question can be read "
                  f"from those rows.")
        else:
            print(f"  {mode}: doubling still pays at every size, so the "
                  f"comparison is live throughout.")

    print()
    for mode in modes:
        message = pinned(chosen[mode], rates)
        print(f"  LEARNING-RATE GRID, {mode}: " + (message or "contained its answer."))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
