# Option record — `carry_store`, carry the raw fast store between sequences

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.carry_store`.
- `tests/test_carry_store.py`, two unit tests, and one mutation.
- No experiment on any instrument.

---

## What was tried, and what came back

### It costs bits on text, in the one place it was measured — `69`

    CONFIG  when    2026-07-27
            source  decision 69
            script  unrecorded
            task    corpus, character level
            model   superposed store, linear readout
            knobs   carry_store on during training
            scale   four corpus sizes

**−0.15 on the level, and no effect on the slope.** It sits in decision 69's table with the
five other mechanisms that move where the model converges to and not where it converges.

### It is superadditive with `hidden`, and the pairing was being quoted wrongly — `116`

    CONFIG  when    2026-07-28
            source  decision 116
            script  unrecorded
            task    corpus, character level
            model   linear readout against `hidden`
            knobs   carry_store and hidden, alone and together
            scale   unrecorded

The two together are worth more than the sum of their separate effects, which is the entry's
finding. It is also the entry that caught a figure being quoted from the wrong arm.

### There is no task here on which it could pay — `170`, `62`, `47`

    CONFIG  when    2026-07-29
            source  decisions 170, 62 and 47
            script  none -- a property of the task generators
            task    kinship, families, chains, closure, CLUTRR
            model   n/a
            knobs   carry_store
            scale   n/a

Every relational task in this repository **redraws its facts per sequence on purpose**,
which is decision 47's condition, so nothing should survive a boundary. Decision 62's guard
says that carrying the store would let the model answer from the training set.

**So persistence is unfalsified on the goal rather than refuted.** The blocker is an
instrument needing something genuinely stable across sequences *and* something genuinely
not — and no such instrument exists.
