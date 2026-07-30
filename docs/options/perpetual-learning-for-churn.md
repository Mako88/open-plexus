# Option record — perpetual learning as a repair for churn

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The churn harness behind decisions 90–92, and `openplexus/tasks/` for the instruments
  they ran on.

---

## What was tried, and what came back

### It does not heal churn, because churn costs CAPACITY rather than knowledge — `091`

    CONFIG  when    2026-07-28
            source  decision 91
            script  unrecorded
            task    composition under node churn
            model   perpetual learning during and after churn
            knobs   continued learning on against off
            scale   unrecorded

**+0.008.** Treat it as a direction, not a number. What the entry establishes is the
mechanism: replay and continued learning restore *knowledge*, and what churn removes is
*room*.

### And 091/092 failed only because their tasks never saturated — `note 081`, `note 082`

    CONFIG  when    2026-07-30
            source  notes 081 and 082
            script  unrecorded
            task    a stream arranged at 10.6x the store's capacity
            model   single store, with and without decay
            knobs   decay
            scale   unrecorded

**C4 IS NOW TESTED.** At 10.6× capacity a single store gives recall **0.07**, and
**symmetrically — oldest beats recent** — so it is **interference, not forgetting, and
replay cannot fix it.** Decay converts that into a window: 0.990 on the last hundred,
**0.000** older.

**The answer is two multipliers**, and neither is perpetual learning: consolidation for
selectivity (`total ÷ useful`) and partitioning for capacity (node count). **Neither
suffices** — forever exceeds any fixed multiple — so *what to shed* is still open. Records:
[persistent-lasting.md](persistent-lasting.md) and
[concept-partitioning.md](concept-partitioning.md).

**Revival:** none as a repair for churn specifically. The mechanism is not refuted as
learning; it is refuted as an answer to this question, and the entries above name what the
question actually is.
