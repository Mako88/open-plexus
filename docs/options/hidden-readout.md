# Option record — the `hidden` readout

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.hidden`, an integer, default 0 (a linear readout).
- `tests/test_composed_readout.py`.

---

## What was tried, and what came back

### The readout was the ceiling all along — `70`

    CONFIG  when    2026-07-27
            source  decision 70
            script  unrecorded
            task    corpus, character level
            model   linear readout against a composed two-layer readout
            knobs   hidden
            scale   unrecorded

The largest single factor found on text. Decision 71 then recorded that 70 **overstated
it**, which is worth carrying with the claim rather than only the headline.

### It is what the readout/store crossover points at — `110`

    CONFIG  when    2026-07-28
            source  decision 110
            script  unrecorded
            task    unrecorded
            model   linear readout against the superposed store
            knobs   width
            scale   unrecorded

The linear readout holds **2.00 items per dimension** at every width, where the store scales
as `d²`. They cross near **width ~100**, above which the readout binds rather than the store
— so above that width the depth is the thing to add. Record:
[superposed-read.md](superposed-read.md).

### And two "refuted" mechanisms partially recover under it — `74`, `76`, `77`

    CONFIG  when    2026-07-28
            source  decisions 74, 76 and 77
            script  unrecorded
            task    corpus, character level
            model   linear readout against composed
            knobs   sparse keys; cache_slots
            scale   3 seeds

**Sparse keys REVERSE** across the readout change — a clean crossover:

    linear readout      5.222 dense   4.794 sparse
    two-layer readout   4.487 dense   4.586 sparse

So sparsity was never a representational improvement; it was **compensation for a readout
that could not disentangle overlap**. The exact cache turned out to be mostly the same
story (`76`), and `g11-07` measured both partially recovering under the composed readout.

**This is the calibration behind the rule that a measurement is conditional on its
configuration**: every number taken beside the linear readout became a claim about a
configuration that no longer exists, and the enumeration of which ones was what found the
cache re-check.

### It is superadditive with `carry_store` — `116`

    CONFIG  when    2026-07-28
            source  decision 116
            script  unrecorded
            task    corpus, character level
            model   as above
            knobs   hidden and carry_store, alone and together
            scale   unrecorded

Worth more together than the sum of the two apart. The same entry caught a figure being
quoted from the wrong arm.
