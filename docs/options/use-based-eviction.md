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

### C4's answer has a SECOND limit, and it is in the code rather than in any record

    CONFIG  when    2026-07-30
            source  openplexus/models/local_memory.py, the capture_slots
                    promotion site and its docstring
            script  none -- read from the mechanism, nothing run
            task    none
            model   consolidation with capture_slots
            knobs   capture_slots, consolidation, salience
            scale   n/a

Note 083's account of C4 is an **eviction** story: fixed storage cannot hold everything, so
discard what has gone longest unused, and a persistently-queried fact survives forever.
*Bounded in content, unbounded in time.*

**The real mechanism has a prior gate that no record mentions.** `local_memory.py` promotes
into the lasting store only when the previous prediction was correct, and its own comment
says what that means:

> consolidation fires on `predictions[t-1] == token` — it promotes what the model
> ALREADY GOT RIGHT — so a persistent store cannot bootstrap a model that predicts badly

So the durable store does not receive *what was used*. It receives **what was already
predicted correctly**. Use-based eviction then decides what leaves among those.

**Stated without overclaiming, because this is not fatal and could be read as if it were.**
The route is fast store → becomes predictable → promoted, which is a validation gate rather
than a block, and promoting only what has proved right is a defensible design. What it
costs is real all the same:

- The durable store LAGS the fast one by however long a thing takes to become predictable.
- **A fact that never becomes predictable never becomes durable**, however often it is used.
- So *"bounded in content, unbounded in time"* is more exactly *"bounded to content the
  model already gets right, unbounded in time"*.

**Nothing here is measured.** It is read off the mechanism, and it is recorded because the
gap is between the CODE and the CLAIM rather than between two numbers: note 083 explicitly
did not exercise `capture_slots` — *"this reimplements the idea rather than exercising that
code"* — so the C4 answer on record describes a fixture without this gate in it.

The measurement that would settle its cost: a stream where some facts become predictable
and some never do, scored on what reaches the lasting store. `capture_slots` defaults to 0
and needs `consolidation`, so that arm has to be switched on deliberately.

### `model.consolidations` cannot attribute a firing to a fact — `g25-02`

    CONFIG  when    2026-07-30
            source  g25-02
            script  experiments/g25_02_consolidation_gate.py
            task    synthetic stream, 8 arbitrary facts recurring at a swept gap
                    in random filler, length held constant across gaps
            model   LocalAssociativeMemory width 64, consolidation 0.5,
                    lasting_cap 5.0, decay 0.9 and 0.95
            knobs   recurrence gap 2 to 64; decay
            scale   3 seeds

    background firings   about 150 per 6,144 positions
    fact contribution    order 10 to 25, and it swings NEGATIVE across gaps
    per_scored           about 3.3 -- 160 firings against 48 fact positions

The counter's comment says it exists *"so a null can be attributed"*, and that is true at
the granularity it was built for — **did the gate open at all**. It is false at the one
needed to price the gate — **did the gate open FOR THIS FACT**. Rule 8: a statistic
collected over one kind of object does not describe an object of another kind.

A difference control built specifically to rescue it — the same stream with fact pairs
replaced by filler, subtracted — returns noise that swings negative. Signal to background
is under 1:6.

**One-shot facts consolidate 23.33 times at decay 0.9 against 8 scored positions**, where
`g25-02` P4 predicted exactly zero and said in advance why it might not be. So on this
stream the gate is a scoring coincidence rather than a validation signal — *"promotes what
it already got right"* is really *"promotes what it scored right"*. The stream is
adversarial by design: arbitrary pairs in random filler give the readout nothing to be
right about.

**Stopped after three attempts, per rule 17**, with the bound published rather than a
fourth fixture built. What would measure it is **per-position attribution of consolidation
firings**, which the model does not expose — `run(trace=...)` may be the hook, and adding
it is a change to instrumentation rather than an experiment.
