# Option record — content-derived keys for ENTITIES

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing that addresses by similarity. `openplexus/content.py` — `ContentIndex` — is the
  separate index that proposes neighbours without anything addressing by it.

---

## What was tried, and what came back

### Ranked third by blast radius, and the sweep that looked like evidence was retracted — `note 042 §2`

    CONFIG  when    2026-07-28
            source  note 042
            script  unrecorded
            task    design pass; g10-09 was the sweep
            model   store addressed by token identity
            knobs   none
            scale   n/a

Note 042 §2 ranked it third by blast radius — *"the store has no notion of similarity at
all"* — and `g10-09` was **RETRACTED**: its cache indexed by token id, so the question was
never asked.

### The refusal already exists under another name, and reading the tree as a tree found it

    CONFIG  when    2026-07-30
            source  note 052, note 035
            script  none -- reading the tree against itself
            task    none
            model   identity addressing, occupancy gate, hashed sketch
            knobs   none
            scale   n/a

"Address the store by continuous vector" is refused in this project's first component
*because* nearby addresses raise `ρ` and interference is `O(N·ρ)`, which also turns the
gate's structurally-zero bar into a tuned threshold. **Nearby addresses is what is refused,
however the nearness arises** — and with thousands of entities that is fatal.

Full argument: [continuous-vector-addressing.md](continuous-vector-addressing.md).

### Note 067 split the refusal, and only half of it holds — `note 067`

    CONFIG  when    2026-07-29
            source  note 067
            script  unrecorded
            task    CLUTRR composition, held-out relations
            model   `bind` over random relation vectors
            knobs   structured relation representation on against off
            scale   held-out quarter

The refusal is right for **entities** and does not transfer to **relations**. Twenty
relations must be *comparable* rather than exactly separated, and the store addresses by
`(entity, relation)` where the entity supplies the exactness, so `O(N·ρ)` does not bite.
`bind` over random relations scores **0.056** held out against chance 0.050 — generalising
composition is impossible without structure. That half has its own record:
[structured-relations.md](structured-relations.md).

### The resolution that is already the architecture — `note 045`

    CONFIG  when    2026-07-28
            source  note 045
            script  none -- design pass
            task    none
            model   ContentIndex proposes; nothing addresses by it
            knobs   none
            scale   n/a

Similarity lives in a **separate index**. `ContentIndex` proposes candidates and the store
is still addressed by identity, so exact separation and a notion of resemblance coexist
without either paying for the other.
