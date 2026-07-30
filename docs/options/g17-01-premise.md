# Option record — g17-01's premise

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `experiments/sweeps/g17-01-*.txt` and the corrected harness that re-measured it.

---

## What was tried, and what came back

### The pivot was not an artefact — `140`

    CONFIG  when    2026-07-29
            source  decision 140
            script  unrecorded
            task    corpus
            model   the g18 harness AFTER the target fix
            knobs   as g17-01's
            scale   unrecorded

Decision 138 retracted four sweeps and 142 cells taken through a harness that trained on the
wrong target. **g17-01's premise survives its own correction** — the pivot it identified is
real and was not produced by the defect.

**This is the one thing in that whole line that held**, and it is recorded separately for a
reason: rule 12 says a fix invalidates evidence in both directions, and **discarding a good
idea on an invalid measurement is the most expensive error available.** A retraction that
quietly takes a correct finding with it is worse than the original defect, because nothing
afterwards will go looking for it.

So the sort was done deliberately — which past results the broken path could actually have
touched, and which it could not — rather than retracting everything in the neighbourhood.
Record for the retraction itself: [g18-harness.md](g18-harness.md).
