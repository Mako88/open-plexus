# Option record — `hop_relation`, bind a relation token into the hop's key

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.hop_relation`, an int, default `-1` (off).
- `tests/test_typed_hop.py`, `tests/test_hops.py`.

---

## What was tried, and what came back

### A hop can follow a NAMED edge, and the guard that blocked it was wrong — `158`

    CONFIG  when    2026-07-29
            source  decision 158
            script  unrecorded
            task    kinship
            model   hop key built from the accumulated retrieval plus a relation token
            knobs   hop_relation
            scale   unrecorded

Before this, a hop followed whatever the store returned. Binding a relation token into the
hop's key makes it follow a named edge instead, which is what a traversal over a typed
graph requires.

### Its limit, named in the same entry — `158`, `052 §2`

    CONFIG  when    2026-07-29
            source  decision 158, note 052
            script  none -- a scope statement
            task    kinship, where the query states the relation
            model   as above
            knobs   hop_relation
            scale   n/a

**The relation is fixed, not chosen.** In kinship the question states it, so it is free. In
an open query it is not, and that is what makes the relation-choosing question live at all
— the options for it are [try-all-and-gate.md](try-all-and-gate.md) and
[learned-relation-chooser.md](learned-relation-chooser.md).

### And the prior question was whether a hop could carry one at all — `162`

    CONFIG  when    2026-07-29
            source  decision 162
            script  unrecorded
            task    kinship with links
            model   as above
            knobs   hop_relation
            scale   unrecorded

Decision 162 split the question in two: *which* relation, and *whether a hop can carry its
own*. **The second blocks before the first matters**, and 158 is where it stopped blocking.
One relation for the whole walk is still a schedule the task does not supply, which 162
calls a fitted constant — the answer to that is
[hop-relations.md](hop-relations.md).
