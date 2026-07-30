# Option record — the store is per-sequence, rebuilt every sequence

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The store is allocated per sequence in `openplexus/models/local_memory.py` and discarded
  at its end. `carry_store` and `persistent_lasting` are the switches that change that,
  and both are off.
- `tests/test_carry_store.py` holds the boundary.

---

## What was tried, and what came back

### It was confirmed empirically rather than assumed — `62`

    CONFIG  when    2026-07-27
            source  decision 62
            script  unrecorded
            task    corpus
            model   store rebuilt per sequence
            knobs   learn=False, one sequence run before another against not
            scale   byte-identical comparison

With `learn=False`, predictions are **byte-identical** whether or not another sequence ran
first. That is the guard which makes a cross-sequence claim falsifiable: if the store were
leaking between sequences, this comparison would differ.

The same entry is why `carry_store` cannot be turned on casually — carrying the store would
let the model answer from the training set, and the byte-identity check is what would stop
being true.

### Every relational instrument redraws its facts on purpose — `47`, `170`

    CONFIG  when    2026-07-27
            source  decision 47, decision 170
            script  none -- a property of the task generators
            task    kinship, families, chains, closure, CLUTRR
            model   n/a
            knobs   none
            scale   n/a

Nothing in this repository is supposed to survive a sequence boundary, because every
relational task redraws its facts per sequence. **So there is no task here on which
persistence could pay** — which makes persistence unfalsified on the goal rather than
refuted, and the blocker an instrument rather than a mechanism.
