# Option record — the two-timescale memory loop

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Contradiction detection (`note 080`), blame localisation (`note 079`), promotion into the
  slow store, and eviction — assembled and run end to end in `note 092`.

---

## What was tried, and what came back

### The loop runs, and it repairs — `note 092`

    CONFIG  when    2026-07-30
            source  note 092
            script  unrecorded
            task    a fact stream with 30% of facts corrupted
            model   fast store plus slow store, contradiction and blame wired together
            knobs   promotion, eviction, repair passes
            scale   six passes

Recall returns to **1.000** after six passes, with blame falling **115 → 20**, so it
converges rather than oscillating. It damages nothing when nothing is wrong.

### And it cannot decide which side of a contradiction is wrong — `note 092`

    CONFIG  when    2026-07-30
            source  note 092
            script  unrecorded
            task    the same stream, corrupting each side in turn
            model   as above
            knobs   corrupt the direct fact against corrupt the derivation
            scale   as above

    corrupt the direct fact   repair takes 0.697 -> 1.000
    corrupt the DERIVATION    repair takes 1.000 -> 0.697

**Identical corruption, relocated.** Repair moves the damage to whichever side it does not
trust. `note 068` predicted exactly this — *"a wrong derived fact becomes a premise"* —
before anything was built.

### What is missing is redundancy, and it is untried — `note 092`

    CONFIG  when    2026-07-30
            source  note 092
            script  none -- nothing built
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

A derivation against a read is a two-way disagreement with no majority. Two *independent*
derivations against a read is a three-way vote. Nothing votes today. Trusting the direct
fact always is just "detect only", which trades one failure for the other.

The idea recorded beside it, untested: concept partitioning means the same binding is
reachable through differently-interfered stores, so a second opinion need not be a second
derivation from the same primitives — which is what defeated the earlier attempt.

### What supplies the two halves — `note 080`, `note 079`

    CONFIG  when    2026-07-30
            source  notes 079-080
            script  unrecorded
            task    fact streams with injected corruption
            model   contradiction signal and blame localisation
            knobs   none
            scale   contradiction measured at six standard deviations, label-free

Contradiction supplies *wrong* and blame supplies *where*. The contradiction signal is
label-free and separates at six sd, which is what makes the loop possible without an
oracle; blame follows the walk.
