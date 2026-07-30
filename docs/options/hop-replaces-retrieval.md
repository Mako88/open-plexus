# Option record — a hop REPLACES a retrieval, it does not combine with it

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.hop_accumulate`, default `"replace"`, with `"concat"` and `"bind"` as
  the alternatives.
- `tests/test_hops.py`.

---

## What was tried, and what came back

### The hop mechanism replaces rather than combines — `101`

    CONFIG  when    2026-07-28
            source  decision 101
            script  unrecorded
            task    kinship
            model   multi-hop retrieval
            knobs   hop_accumulate
            scale   unrecorded

Established that a hop's output stands in for the previous retrieval rather than being
merged with it. That is what makes a walk a walk: state at step `n` is where you are, not
a running sum of everywhere you have been.

### The accumulator was built, and the stated reason for choosing it was wrong — `102`

    CONFIG  when    2026-07-28
            source  decision 102
            script  unrecorded
            task    kinship
            model   accumulator over hops
            knobs   hop_accumulate replace against concat
            scale   unrecorded

The entry records the correction in its own text rather than quietly fixing it: the
mechanism was built and the argument given for the choice did not survive. What the choice
actually rests on is measured separately —
[hop-accumulate.md](hop-accumulate.md), where `concat` wins 1.000 to 0.812 for a reason
that is a property of having few rules rather than of concatenation being right.

### The oracle that showed the readout was getting nothing from hop 1 — `103`

    CONFIG  when    2026-07-28
            source  decision 103
            script  unrecorded
            task    kinship, 14 people, 10 facts
            model   hop 2 handed the correct second relation
            knobs   hop_accumulate replace and concat
            scale   395 sequences

    accumulate    real hop 2   ORACLE hop 2
    replace            0.027          0.560
    concat             0.347          0.560

**Identical.** If `concat` were using hop 1, holding both relations should reach about
1.000. 0.560 is exactly the `last`-relation information bound from decision 100 — so the
readout was getting nothing from hop 1, and the cause was addressing rather than the
accumulator. Record: [pair-keys.md](pair-keys.md).
