# Option record — `index_at_hops` with the position-level index

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.index_at_hops`, `index_branches`, `index_weight`, `index_sharpness`.
- `tests/test_index_at_hops.py`.

---

## What was tried, and what came back

### The pieces were built — `159`, `160`, `161`

    CONFIG  when    2026-07-29
            source  decisions 159, 160 and 161
            script  unrecorded
            task    families, kinship
            model   index proposing at the hop's landing concept
            knobs   index_at_hops
            scale   unrecorded

`159` the index proposes at the hop's landing concept, and only at depth. `160` the
*"alternatives, not additive"* framing was too strong and was what blocked the combination.
`161` `inherit` was never read-gated and nobody had counted its reads.

### The guard's premise was false — `154`

    CONFIG  when    2026-07-29
            source  decision 154
            script  unrecorded
            task    kinship
            model   soft hop key against a single token's key row
            knobs   none
            scale   unrecorded

The guard refused the combination because a hop key *"names no concept"*. Measured, a hop
key sits at cosine **0.96** to a single token's row, so it **does** name a concept. The
premise was wrong rather than the caution being excessive.

### It is blocked on an instrument, not on a mechanism — `note 050`

    CONFIG  when    2026-07-29
            source  note 050
            script  none -- an instrument gap
            task    none exists
            model   n/a
            knobs   none
            scale   n/a

**No task has both** an address that is never written and a composition over it. The index
pays where an address is read before it is written; composition needs a walk. Nothing in
the repository supplies both at once, so the combination has nothing to be measured on.

Note 050's own first attempt at such a task was refuted by its own fairness rail on the
first run (`155`), which is recorded under
[linked-families-task.md](linked-families-task.md).
