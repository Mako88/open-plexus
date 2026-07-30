# Option record — `persistent_lasting`, a consolidated slow store

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.persistent_lasting`, `lasting_cap`, `lasting_decay`, `consolidation`.
- `tests/test_persistent_lasting.py`, `tests/test_consolidation.py`.

---

## What was tried, and what came back

### The first two passes measured the instrument, not the mechanism — `131`, `132`

    CONFIG  when    2026-07-28
            source  decisions 131 and 132
            script  unrecorded
            task    corpus, character level
            model   slow store with consolidation
            knobs   lasting_cap, write rate
            scale   unrecorded

The store was **saturated at `lasting_cap` before the run started**, and then the write rate
was 100× too large. Two passes producing numbers about the harness.

### It is a real gain on text — `133`

    CONFIG  when    2026-07-28
            source  decision 133
            script  unrecorded
            task    Tiny Shakespeare, character level, 4k to 125k characters
            model   fast store plus a persistent consolidated slow store
            knobs   persistent_lasting on against off; consolidation without persistence
            scale   3 seeds

**0.074–0.083 bits better than baseline at EVERY data point**, and its own control —
consolidation *without* persistence — is worse than baseline everywhere, so the attribution
is clean.

### And it does not move the data wall — `133`

    CONFIG  when    2026-07-28
            source  decision 133
            script  unrecorded
            task    as above
            model   as above
            knobs   corpus size
            scale   3 seeds, seed spread 0.04

**+0.0124 past 16k**, under the 0.04 seed spread and not monotone. Store norm is **0.4 at
every corpus size**, which is a fixed-size cache holding a moving window rather than a map
that grows.

### Note 082 explains the window mechanically, and rehabilitates the mechanism — `note 082`

    CONFIG  when    2026-07-30
            source  note 082
            script  unrecorded
            task    a fact stream at 10x the store's capacity
            model   fast store plus slow store
            knobs   correctness signal quality varied
            scale   overload ratios 1.1x, 4.2x and 10x

Recall of the asked-about facts goes from **0.020 to 1.000**, and **recall tracks the
correctness signal one-to-one** — 0.9 → 0.915, 0.7 → 0.705, 0.5 → 0.540 — so the whole
mechanism reduces to that signal, which `note 080` measures at six sd, label-free.

**Bounded, not unbounded.** The slow store saturates in turn: 1.1× → 0.965, 4.2× → 0.419.
So it buys `total ÷ useful`, not infinity. And a fact never asked about inside its window is
unrecoverable — the cost the fixture is built not to pay.

### Why it is not simply switched on

    CONFIG  when    2026-07-28
            source  decision 133, decision 115
            script  none -- a scope decision
            task    n/a
            model   n/a
            knobs   persistent_lasting
            scale   n/a

Turning it on invalidates the text comparison set. Since decision 115 says character-level
bits is the wrong target, that set may not be worth protecting — which makes this a cheap
decision currently made by inertia rather than by argument.
