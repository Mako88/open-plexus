# Option record — the rounded TIMESTAMP as the cross-node co-occurrence key

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing. No bucket, no join, no accumulator. This record exists because the design was
  chosen before it was built, and the reasoning is worth more than a later reconstruction.
- The pieces it would be built from do exist: `openplexus/ownership.py` (`Ring`, consistent
  hashing) is what would own a bucket, and `openplexus/partitioned.py` is the store shape.

---

## What was tried, and what came back

### The problem it answers, and why nothing else here answers it — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Identity between modalities is learned from **temporal co-occurrence** — a picture, a bark
and the word *dog* arriving together, repeatedly. That requires a node to know that what it
saw and what another node heard **happened at the same time**, and to know it *without
asking*, because asking is the collective C1 forbids.

John's proposal: **round the arrival time to a coarse bucket and derive the address from
that.** Two nodes observing one event compute the same bucket independently.

**This is the same property that makes concept ownership work.** `Ring` gives
*computable locally, agreed globally, no message sent* for concepts; a rounded timestamp
gives it for episodes. Recorded because the parallel is the argument: this is not a new
kind of mechanism, it is the existing one applied to a different key.

### It is the same object as the consolidation tag — noted at the same time

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

The delayed-write design that replaces a write-time gate keeps a **tag** carrying a
timestamp, and consolidates on a later signal meaning *"something around now mattered."*

**A time bucket is that tag's address.** So the join key and the consolidation trigger are
one mechanism rather than two, and the join is what makes the tag reachable *across
machines* instead of only within one. Recorded so the two are not built twice.

### Four objections, raised at design time

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass, no measurement of any of these
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

**Boundaries hurt more than skew.** Two events a millisecond apart round to different
buckets when they straddle an edge. The standard answer is overlapping windows — hash to a
bucket and its neighbours — at a constant-factor cost in writes.

**The asynchrony bound fights the bucket size.** `d_max` is the C2 delay this project has
already accepted as normal. A bucket comparable to it puts a late-arriving input in the
wrong bucket routinely, so the bucket must be comfortably wider — which is coarser, and
binds more unrelated things together.

**A bucket is a hot spot.** Every input at one instant routes to one node. Moving
ownership to entities was worth a large fall in busiest-peer share
([concept-partitioning.md](concept-partitioning.md) holds the figures); time buckets are
the opposite move and much worse. Splitting by `(time, modality)` spreads the load and
destroys the join, which is the point of it.

**One episode is nearly worthless.** A dog, a sofa and a face all co-occur with the word.
Only what is constant across many episodes separates them — and if episodes are scattered
by time across nodes, gathering "every dog episode" is the global operation C1 forbids.

### The resolution to the fourth, which changed the design — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

> **Time is the TRANSIENT join. The percept's owner is the DURABLE accumulator.**

The bucket exists only long enough to observe one co-occurrence. The link is then written
to `owner(percept_id)`, which accumulates over that percept's whole lifetime — so the node
owning an image id ends up holding *everything that has ever co-occurred with it*.

**Cross-situational learning then falls out as local counting at a fixed address.** The
sofa fades because it appeared once; the word persists because it appears every time. No
gather, no global step, and the hot spot is transient rather than permanent because nothing
durable is stored at the time key.

It is the fast-store-and-durable-store shape the project already has, with **time
addressing the fast tier and percept id addressing the slow one**.

### What would refute it, registered before anything is built

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- not yet written
            task    proposed: a symbol stream with a persistent distractor
            model   n/a
            knobs   none
            scale   n/a

**Introduce a concept alongside a distractor that is present every single time, and see
whether the distractor is ever pruned.**

If it never fades — because co-occurrence alone cannot distinguish *"always there"* from
*"is the thing"* — then counting is insufficient and the missing ingredient is intervention.
That is the hypothesis [`GOALS.md`](../../GOALS.md) records as arriving independently from
the memory side of the project on the same day.

**This needs no perception layer.** It is a symbol stream with a designed co-occurrence
structure, which makes it the cheapest available test of the whole mechanism.
