# Option record — untrusted nodes

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing, in either direction. **No threat model at all.**
- The two capabilities a hostile node would abuse do exist: it can assert occupancy, and it
  can write to addresses it does not own.

---

## What was tried, and what came back

### Named as absent, with the two concrete attacks

    CONFIG  when    2026-07-30
            source  DECISIONS.md component 9, and decision 148 for the gate's structural
                    zero
            script  none -- nothing built
            task    none
            model   peer transport with no authentication and no attestation
            knobs   none
            scale   n/a

A node that **lies about occupancy** poisons the one selection rule this project has that
works — the occupancy gate reads exactly 0.0 for an unwritten address, and that structural
zero is trusted precisely because nothing can fake it locally. Remotely, something can.

A node that **writes to addresses it does not own** corrupts a store with no way for the
owner to notice, because the store is additive and a write is not attributable after the
sum.

### It forks on the project's endgame, which is undecided

    CONFIG  when    2026-07-30
            source  DECISIONS.md standing agreements
            script  none
            task    none
            model   n/a
            knobs   none
            scale   n/a

Open-source-and-runs-everywhere **implies** a threat model. A controlled network does not.
So this cannot be scoped until the endgame is, and the standing agreements record that the
endgame is deliberately open — recommendations must not quietly assume an answer.

That is why it is untried rather than deferred: the work is not blocked on effort, it is
blocked on a decision nobody has made.
