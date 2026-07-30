# Option record — "concept partitioning is where the capacity comes from"

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing built on it. It was superseded one entry after it was written.

---

## What was tried, and what came back

### Pooled capacity is identical between the two arrangements — `134`

    CONFIG  when    2026-07-28
            source  decision 134
            script  unrecorded
            task    synthetic capacity probe
            model   per-node memory held equal at ~4,096 numbers
            knobs   1, 2, 4, 8 and 16 nodes; concept partitioning against dimension splitting
            scale   5 seeds, 50 cells

    pooled capacity, both arrangements   128 / 256 / 512 / 1024 / 2048 at 1/2/4/8/16 nodes
    lone-node capacity                   2048 against 128 at 16 nodes

**Pooled capacity is identical.** What differs is what a *lone* node can hold, by a factor
of sixteen at sixteen nodes — which is a different claim and the one that survives.

This is the same overstatement note 043 made and then corrected in its own text: per unit of
memory the two arrangements are the same. The narrower true statement, and the C4 argument
that does rest on it, are in
[concept-partitioning.md](concept-partitioning.md).

### It was decision 133's follow-on, and 133 was already a relabel — `133`, `170`

    CONFIG  when    2026-07-28
            source  decision 133, decision 170
            script  none -- an audit
            task    n/a
            model   n/a
            knobs   none
            scale   superseded one entry later

Decision 133 relabelled a null as a capacity limit; this was the mechanism proposed to
supply the capacity. Decision 134 superseded it **one entry later**, and on 2026-07-29 the
pair still produced a wrong recommendation, because a log is read by looking things up
rather than forward from a point. Record: [wall-as-capacity-limit.md](wall-as-capacity-limit.md).
