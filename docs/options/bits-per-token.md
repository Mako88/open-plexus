# Option record — bits per token as evidence about the store

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/corpus.py` and the bits-per-character scoring around it, paused rather
  than removed.

---

## What was tried, and what came back

### The objective is n-gram bounded, so it cannot show what the store adds — `047`, `142`

    CONFIG  when    2026-07-27
            source  decisions 47 and 142
            script  unrecorded
            task    corpus, character level; and MQAR as the contrast
            model   superposed store
            knobs   store on against off
            scale   unrecorded

On a next-token objective the only relation the store can express is *"what followed this"*.
A counting table does that exactly, so the store's contribution is bounded above by
something simpler — and on MQAR, where a prior cannot substitute, the same store gives
**0.995 against 0.000**.

**So a bits-per-token number is a measurement of the objective, not of the mechanism.** Do
not re-propose.

### The bigram table is intrinsically low-rank, which is the mechanical form of the same thing — `115`

    CONFIG  when    2026-07-28
            source  decision 115
            script  unrecorded
            task    corpus, character level, 66 symbols
            model   widths 32 to 256
            knobs   width
            scale   effective rank at every width

**Effective rank ~3 at every width.** There is not enough structure in the target for a
wider store to express. Record: [saturation-closed.md](saturation-closed.md).

**Revival:** an objective over text that is not next-token — the constraint is on what the
target contains, not on text as a medium.
