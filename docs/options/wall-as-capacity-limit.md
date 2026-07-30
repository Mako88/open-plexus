# Option record — "the wall is a capacity limit"

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing built on it. This is a relabel, and the record exists so the relabel cannot
  happen a third time without someone finding this file.

---

## What was tried, and what came back

### It is decision 133's relabel of its own null — `133`, `170`

    CONFIG  when    2026-07-28
            source  decision 133, decision 170
            script  unrecorded
            task    Tiny Shakespeare, character level, 4k to 125k characters
            model   fast store plus a persistent consolidated slow store
            knobs   persistent_lasting on against off
            scale   3 seeds, seed spread 0.04

Decision 133 ran a falsifier for the architecture case built on the 16k wall. **It was
refuted** — the mechanism moved the level by 0.074–0.083 bits everywhere and moved the wall
by **+0.0124**, under the seed spread and not monotone. The entry then described the wall it
had failed to move as a *capacity limit*.

That contradicts decision 110, which measured the readout above task demand, and decision
115, which eliminated store capacity, readout capacity and persistent representation **by
name**. Full account: [saturation-closed.md](saturation-closed.md).

### Why the log could not stop it — `170`

    CONFIG  when    2026-07-29
            source  decision 170, and docs/archive/decisions-log-083-171.md for the length
            script  none -- an audit
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Decision 115 closed saturation explicitly — *"saturation is not an open problem and should
stop being treated as one"*. The closure lived in one entry of a 6,040-line append-only log,
its Index stopped being maintained at entry 134, and nothing pointed back at it.

**A ratchet on proposals does not catch a re-label after the fact.** A reader arriving at
133 sees a reasonable entry; only reading 110 and 115 first makes it wrong, and nothing
made anyone do that.
