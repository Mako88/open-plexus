# Option record — everything measured by the g18 harness before the fix

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The corrected harness. Every number taken through the old one is retracted.

---

## What was tried, and what came back

### RETRACTION: it trained on the wrong target — `138`

    CONFIG  when    2026-07-29
            source  decision 138
            script  unrecorded
            task    corpus, word level
            model   the g18 harness before the fix
            knobs   three axes across four sweeps
            scale   142 cells

**Survived four sweeps and 142 cells**, because **every arm was wrong identically**. The
results were internally consistent, both rails passed, and the ordering was monotone with a
tidy explanation attached.

**What caught it was a figure the project had already measured** — not a rail, not a test,
not an inconsistency in the sweep. An external anchor.

### The lesson, stated as the rule it became

    CONFIG  when    2026-07-29
            source  decision 138
            script  none
            task    n/a
            model   n/a
            knobs   none
            scale   four sweeps, 142 cells

**Internal consistency is not evidence.** A harness with a systematic error produces a
coherent world, and every check that compares arms to each other passes inside it. The only
checks that catch this compare an arm to something outside the harness.

That is the same structure as `note 105`'s citation loop and decision 118's inherited
headline: a closed system agreeing with itself.

**Revival:** none for the numbers. The harness after the fix is a different instrument and
its results are not comparable to the retracted ones.

### And the premise underneath it survived — `140`

    CONFIG  when    2026-07-29
            source  decision 140
            script  unrecorded
            task    corpus
            model   corrected harness
            knobs   as g17-01's
            scale   unrecorded

g17-01's pivot was **not** an artefact of the defect. Record:
[g17-01-premise.md](g17-01-premise.md) — worth separating, because a retraction that takes
a correct finding with it is the more expensive error.
