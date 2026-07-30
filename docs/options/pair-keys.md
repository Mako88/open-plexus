# Option record — `PairKeys`, hashed `(previous, token)`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/keys.py` — `PairKeys` beside `TableKeys`, and `pair(a, b)`.
- `LocalMemoryConfig.derived_keys` and `context_keys`; `PairKeys(route=...)` with
  `first-concept` as note 073's routing.
- `tests/test_keys.py`, `tests/test_keys_conformance.py`, `tests/test_pair_keys_distributed.py`,
  `tests/test_context_keys.py`, `tests/test_derived_keys.py`, `tests/test_pair_routing.py`.

---

## What was tried, and what came back

### The problem it was built for: one key per entity collapses — `103`

    CONFIG  when    2026-07-28
            source  decision 103
            script  unrecorded
            task    kinship, 14 people, 10 facts
            model   single-token keys, `hop_accumulate` replace and concat
            knobs   none -- an oracle probe
            scale   395 sequences split by appearance count

An oracle handed hop 2 the correct second relation and nothing changed: `replace` went
0.027 → 0.560 and `concat` 0.347 → 0.560, **identical**, where holding both relations
should reach about 1.000. The readout was getting nothing from hop 1.

The reason is not about hops at all. Hop 1 splits by how many facts the queried person
appears in anywhere:

    appearances   sequences   hop 1 correct
              1         146          0.959
              2         145          0.366
              3          81          0.321
              4          23          0.348

`key(person)` accumulates one binding per appearance and a retrieval returns their **sum**.
The entry's own framing: this is what relational data *is*, an entity in exactly one
relation is the degenerate case, and **a graph cannot be laid out to avoid it** the way
decision 84 laid chains out contiguously.

### Pair keys largely fix it — `104`

    CONFIG  when    2026-07-28
            source  decision 104
            script  unrecorded
            task    kinship, 14 people, 10 facts
            model   `context_keys` binding (previous, token)
            knobs   context_keys on against off
            scale   as decision 103's split

    one appearance      0.884 -> 0.918
    two or more         0.303 -> 0.628

The residual at two-or-more appearances is **the same entity in the same ROLE**, which pair
keys cannot separate by construction.

### Typing an address costs nothing and pays at low load — `156`

    CONFIG  when    2026-07-29
            source  decision 156
            script  unrecorded
            task    families
            model   pair keys, typed addresses
            knobs   address typing on against off
            scale   unrecorded

### Typed writes stop link and fact colliding — `157`

    CONFIG  when    2026-07-29
            source  decision 157, and note 072 for the 4.7x collision figure
            script  unrecorded
            task    families with `family_links`
            model   typed writes
            knobs   typed against untyped
            scale   unrecorded

Every column lands within 0.05 of its link-free value — 0.8333, 0.4383, 0.8150 — where
untyped collapsed to 0.13, 0.03 and 0.12.

This is also the decision that picked the kinship layout for a 4.7× collision reduction,
with concept ownership not in view. Note 072 later measured what that pairing cost; the
account is in [concept-partitioning.md](concept-partitioning.md).

### Routing by the first concept of the pair — `note 073`

    CONFIG  when    2026-07-30
            source  note 073
            script  unrecorded
            task    CLUTRR, kinship layout
            model   PairKeys with the new route built and not defaulted
            knobs   route first-concept against the previous route
            scale   7,132 traversal bindings

Traversal bindings move from relation-owned to entity-owned, markers stop owning content
(31.6% → 0.0%), and the busiest peer drops 26.6% → 11.8%. The note's original
"0.0% relation-owned" is corrected: `pair(relation, entity)` remains relation-owned at
22.3% of all keys, though its value is a separator the traversal never reads.
