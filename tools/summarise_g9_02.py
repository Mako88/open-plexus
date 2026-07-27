"""Does a reward token recover what six stream-derived gates could not?

Recovery is `(arm - none) / (oracle - none)`, the same quantity as g8-01 and
g8-03 so the numbers are comparable across tasks, with the two refusals from
`tools/recovery.py`: no ratio when the floor arm is at or below the trivial
floor, and none when the oracle's advantage does not beat the seed spread.

**Accuracy here is FIRST ASKS.** In autoregressive mode the first query of a cue
re-binds it, so later queries about that cue measure short-term echo rather than
retention. Both are reported, and the ratio uses the first.

`on-use` and `salience` are carried over unchanged from the sweeps they failed,
so this grid contains the mechanisms it is trying to beat rather than a
description of them. If they suddenly work here, suspect the task before
celebrating.

> **The learning rate is now chosen differently, and it can move the numbers.**
> This summariser used to pick, per delay, the rate with the LARGEST
> `oracle - none`. It skipped collapsed floors first, so it was not the worst
> version of that mistake — but among the cells that survive, maximising the gap
> still prefers whichever rate left the floor arm lowest, which is the third rule
> in `tools/recovery.py` and the one that bit hardest. It now picks by what the
> `reward` arm actually recovers, via `best_by`, which selects after the refusals
> rather than before them. **The table this prints may therefore differ from the
> one in the g9-02 sweep file**; that file records what was reported at the time
> and is not edited to match.
"""

from __future__ import annotations

from tools.recovery import (
    REWARD_RECALL_FLOOR, assess, best_by, by_cell, load)

ARMS = ("none", "oracle", "on-use", "salience", "reward")


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    first = by_cell(rows, "delay", "lr")
    delays = sorted({r["delay"] for r in rows})
    rates = sorted({r["lr"] for r in rows})

    print(f"\ntrivial floor {REWARD_RECALL_FLOOR:.3f}")
    print("\n=== FIRST-ASK accuracy per seed ===")
    for delay in delays:
        print(f"\ndelay {delay}")
        for lr in rates:
            line = [f"  lr={lr:<5}"]
            for arm in ARMS:
                by_seed = first.get((delay, lr, arm), {})
                if not by_seed:
                    line.append(f"{arm}=--")
                    continue
                line.append(f"{arm}=" + "/".join(
                    f"{by_seed[s]:.3f}" for s in sorted(by_seed)))
            print("  ".join(line))

    print("\n=== RECOVERY of the oracle's advantage, by delay ===")
    print("The delay is the point: at 1 the marker is adjacent and the rule is")
    print("'keep the step before the obvious token', which learns nothing about")
    print("value. At 20 the binding is long past when the reward arrives.")
    print(f"{'delay':>7}{'lr':>7}{'none':>8}{'oracle':>8}{'gap':>7}{'spread':>8}"
          f"{'on-use':>9}{'salience':>10}{'reward':>9}")
    for delay in delays:
        best = best_by(
            ((lr, assess(first, (delay, lr), ARMS, REWARD_RECALL_FLOOR))
             for lr in rates), "reward")
        if best is None:
            print(f"{delay:>7}   every rate refused or missing")
            for lr in rates:
                got = assess(first, (delay, lr), ARMS, REWARD_RECALL_FLOOR)
                why = "no records" if got is None else got.refused
                print(f"          lr={lr}: {why}")
            continue
        lr, got = best
        print(f"{delay:>7}{lr:>7}{got.means['none']:>8.3f}"
              f"{got.means['oracle']:>8.3f}{got.gap:>7.3f}{got.spread:>8.3f}"
              f"{got.ratios['on-use']:>9.2f}{got.ratios['salience']:>10.2f}"
              f"{got.ratios['reward']:>9.2f}")

    print("\n=== ALL ASKS, the same table ===")
    print("Repeats included, so this is short-term echo as well as retention.")
    everything = by_cell(rows, "delay", "lr", metric="accuracy_all")
    for delay in delays:
        best = best_by(
            ((lr, assess(everything, (delay, lr), ARMS, REWARD_RECALL_FLOOR))
             for lr in rates), "reward")
        if best is None:
            print(f"{delay:>7}   every rate refused or missing")
            continue
        lr, got = best
        print(f"{delay:>7}{lr:>7}{got.means['none']:>8.3f}"
              f"{got.means['oracle']:>8.3f}{got.gap:>7.3f}{got.spread:>8.3f}"
              f"{got.ratios['on-use']:>9.2f}{got.ratios['salience']:>10.2f}"
              f"{got.ratios['reward']:>9.2f}")

    print("\nreward high at delay 1 and falling  -> the signal works and the")
    print("  LATENESS is the unsolved part, which is what tagging and capture")
    print("  exists to close.")
    print("reward high at every delay          -> suspect the task. A marker 20")
    print("  steps later should not be free.")
    print("reward near zero                    -> the gate cannot use relevance")
    print("  even when handed it, which is worse news than six failed")
    print("  inference mechanisms.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
