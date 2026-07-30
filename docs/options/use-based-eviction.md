# Option record — use-based eviction

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The eviction policy measured in `note 083`: discard whatever has gone longest unused.
- `LocalMemoryConfig.memory_cap`, `lasting_cap` and `lasting_decay` are the bounds it acts
  within.

---

## What was tried, and what came back

### A persistently-queried fact survives, and random eviction does not keep it — `note 083`

    CONFIG  when    2026-07-30
            source  note 083
            script  unrecorded
            task    a stream of 4,000 facts through 150 slots
            model   bounded store with an eviction policy
            knobs   use-based eviction against random
            scale   3 seeds

    use-based    1.000 with zero variance
    random       0.717

**Bounded in content, unbounded in TIME.** Fixed storage cannot hold everything, which is
arithmetic, so this is the reachable form of C4's *forever* rather than a weakening of it.

**Recency and frequency are indistinguishable here**, because both are true of the same
facts by construction. The instrument does not separate them and the note says so.

### An inversion in the control arm, recorded rather than smoothed — `note 083`

    CONFIG  when    2026-07-30
            source  note 083
            script  unrecorded
            task    as above
            model   as above
            knobs   random eviction
            scale   3 seeds, about 1.5 sd

Random eviction is **worse on persistent facts than on abandoned ones** — 0.717 against
0.783. Unexplained. It is in the record because a control arm behaving backwards is
information about the instrument even when nobody can say what it means yet.

### The cost, and every fixture here is built not to pay it — `note 083`

    CONFIG  when    2026-07-30
            source  note 083
            script  none -- a property of the policy
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

A useful fact nobody asks about inside its window is gone before it can be promoted. Every
fixture measuring this policy queries the facts it cares about, so none of them pays that
cost — which is a statement about what the numbers above can and cannot support.
