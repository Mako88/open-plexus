"""Screen write-time gate signals against the 11.5x enrichment bar. Counting, not training.

**What this does not duplicate.** `experiments/g8_02_control.py` measures enrichment the
same way and its reasoning is reused wholesale -- measure the DATA, not a model, because
what a model can exploit is bounded by what is there to exploit. Two things differ:

    the CLASS      g8_02_control reports QUERY:filler, because it was asking about
                   surprise. A write gate must keep the BINDING, so this reports
                   PAIR:filler using `harness.oracle_mask`
    the CANDIDATES four structural signals nothing here has tried, beside surprise
                   as the control

## Why structural rather than better-tuned

`g8-01` measured `salience` at **7.6x** and it lost, because filler is 92% of the sequence.
The bar appended to that sweep on 2026-07-30: **11.5x** buys a stored set that is merely
half real, 34.5x buys three quarters, and 7.6x buys **39.8%**.

**A 100x gap is not a tuning gap.** A signal that could close it is unlikely to be a
threshold on a scalar, which is the argument for asking structural questions instead --
has this address been written, has this token appeared. The occupancy sketch already
answers the first exactly (`148`, `g26-01`).

## The rule that keeps this honest

A candidate may use only what a node has AT WRITE TIME: the tokens so far and its own
store. `position_kinds()` is used to SCORE and never as an input. A candidate that reads it
is the oracle wearing a different hat.

Predictions are in `experiments/sweeps/g28-01-screening-gates-against-the-bar.txt`,
committed at `d4fcc87` before this file existed.
"""

from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from experiments import harness  # noqa: E402
from openplexus.tasks.mqar import MqarConfig, dataset  # noqa: E402

#: `g8_02_control`'s parameters, so the control arm is comparable to the 7.6x it
#: is reproducing.
BASE = MqarConfig(n_pairs=4, seq_len=768, n_keys=32, n_values=8,
                  autoregressive=True, seed=20260726, queries_per_pair=3)
SALIENCE = 2.5
#: **THE BAR IS NOT A CONSTANT, and the first version of this file hardcoded it at
#: 11.5 and printed CLEARS against it.** That figure was derived at g8-01's 92%
#: filler share; this configuration measures **98.92%**, where the same target needs
#: **91.9x**. Carrying a constant from another configuration is the failure
#: `CLAUDE.md` rule 2 has three calibrations for, and it arrived inside the tool
#: built to stop a fifth gate being built on a bad screen.
#:
#: So it is computed FROM the measured base rate, every run.
TARGET_REAL = 0.5


def bar_for(store_share: float, filler_share: float,
            target: float = TARGET_REAL) -> float:
    """Enrichment needed for the stored set to be `target` real, at this base rate.

    `target * filler / ((1 - target) * real)`. At 92% filler and a half-real target
    that is 11.5x; at 98.92% filler the same target needs 91.9x. **The bar moves with
    the task's filler share, so it travels with the measurement rather than being
    quoted from another one.**
    """
    return target * filler_share / ((1.0 - target) * store_share)


def signals(tokens, counts, total):
    """Per position, a dict of candidate -> fired?, using ONLY the past.

    Every value at position `t` is computable from `tokens[:t+1]` and a store the node
    built itself. Nothing here consults `position_kinds`.
    """
    info = [-np.log(counts[token] / total) for token in tokens]
    mean, deviation = float(np.mean(info)), float(np.std(info))

    seen_pairs: set = set()
    seen_tokens: set = set()
    out: list[dict] = []
    for t, token in enumerate(tokens):
        pair = (int(tokens[t - 1]), int(token)) if t else (None, int(token))
        out.append({
            "surprise": abs(info[t] - mean) > SALIENCE * deviation,
            "addr-novel": pair not in seen_pairs,
            "addr-seen": pair in seen_pairs,
            "token-novel": int(token) not in seen_tokens,
            "token-seen": int(token) in seen_tokens,
        })
        seen_pairs.add(pair)
        seen_tokens.add(int(token))
    return out


def main() -> None:
    harness.parse_args(__doc__)
    sequences = dataset(BASE, 40)

    counts: Counter = Counter()
    for sequence in sequences:
        counts.update(sequence.tokens)
    total = sum(counts.values())

    names = ("surprise", "addr-novel", "addr-seen", "token-novel", "token-seen")
    fired = {n: [0, 0] for n in names}      # [on should-store, on filler]
    seen = [0, 0]

    for sequence in sequences:
        tokens = list(sequence.tokens)
        # THE SCORING KEY, and it is used only here. `oracle_mask` marks a position
        # whose PREDECESSOR was a pair -- the binding a write gate must keep.
        keep = harness.oracle_mask(sequence.position_kinds())
        kinds = sequence.position_kinds()
        for t, sig in enumerate(signals(tokens, counts, total)):
            if keep[t]:
                seen[0] += 1
                for n in names:
                    fired[n][0] += int(sig[n])
            elif kinds[t] == "filler":
                seen[1] += 1
                for n in names:
                    fired[n][1] += int(sig[n])

    print(f"\n{len(sequences)} sequences, seq_len {BASE.seq_len}, "
          f"n_keys {BASE.n_keys}")
    print(f"should-store positions {seen[0]:,}   filler positions {seen[1]:,}   "
          f"filler share {seen[1] / (seen[0] + seen[1]):.3f}")
    positions = seen[0] + seen[1]
    bar = bar_for(seen[0] / positions, seen[1] / positions)
    print(f"\n{'signal':<13}{'fires|store':>13}{'fires|filler':>14}"
          f"{'enrichment':>13}{'vs bar':>9}")
    for n in names:
        on, off = fired[n][0] / seen[0], fired[n][1] / seen[1]
        ratio = on / off if off else float("inf")
        print(f"{n:<13}{on:>13.4f}{off:>14.4f}{ratio:>13.2f}"
              f"{'CLEARS' if ratio >= bar else '--':>9}")
    print(f"\n  bar for a HALF-real stored set AT THIS BASE RATE: {bar:.1f}x")
    print(f"  g8-01's 11.5x was derived at 92% filler; here filler is "
          f"{seen[1] / positions * 100:.2f}%, so the bar is {bar / 11.5:.1f}x higher")
    print("  A signal below it stores more filler than content, whatever the "
          "threshold.")


if __name__ == "__main__":
    main()
