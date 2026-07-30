# Option record — `hop_relations`, one relation per hop

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.hop_relations`, a tuple, default empty.
- `tests/test_hop_schedule.py`.

---

## What was tried, and what came back

### A walk can follow LINK then FACT — `164`

    CONFIG  when    2026-07-29
            source  decision 164
            script  unrecorded
            task    families with links
            model   one relation token per hop rather than one for the walk
            knobs   hop_relations schedules LINK-FACT, LINK-LINK, and hop_relation=LINK
            scale   3 seeds

    LINK then FACT                       reaches the linked family's value
    LINK then LINK                       stops at its representative
    hop_relation=LINK (the pre-164 best) stops there too

Stable across three seeds. The entry also records that one seed nearly hid the result,
which is why the seed count is on the row.

### It is an instrument, not the answer — `162`

    CONFIG  when    2026-07-29
            source  decision 162
            script  none -- a scope statement
            task    n/a
            model   a schedule supplied by the caller
            knobs   hop_relations
            scale   n/a

**A schedule the task does not supply is a fitted constant.** The mechanism demonstrates
that per-hop relations are representable and followable; it does not answer where the
schedule comes from in an open query. That question has its own records —
[try-all-and-gate.md](try-all-and-gate.md) and
[learned-relation-chooser.md](learned-relation-chooser.md) — and note 090 reached the same
end by a third route, supplying the DISPLACEMENT rather than choosing the relation
([generation-delta.md](generation-delta.md)).
