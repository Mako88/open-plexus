# Option record — self-modifying structure

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing. The store is `d × d`, fixed at construction.

---

## What was tried, and what came back

### There is nothing to modify, and that is the finding — `note 042`

    CONFIG  when    2026-07-28
            source  note 042
            script  none -- an architecture pass
            task    n/a
            model   store fixed at construction; Wk and Wv frozen; Wo the only durable map
            knobs   none
            scale   n/a

The store is allocated once and never grows. `Wk` and `Wv` are frozen random. So the only
structure a self-modifying rule could act on is a single linear map, and modifying that is
just training it — which is already what happens.

**Note 042 is right that 3b and 10 are prerequisites, not alternatives.** Something must
persist across sequences (component 3b) and there must be an objective that would notice
the change (component 10) before a structural change can be measured at all.

**Reserve the seam, build when a task can tell whether it helped.** That is the condition,
and it is a statement about instruments rather than about the mechanism — which is the same
blocker `carry_store` and `index_at_hops` are sitting behind. Records:
[carry-store.md](carry-store.md) and [index-at-hops.md](index-at-hops.md).
