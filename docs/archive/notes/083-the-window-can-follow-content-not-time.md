083 — A bounded store's window can follow CONTENT rather than TIME, which is C4's answer
=======================================================================================

**Status:** measured, three seeds, deterministic on the arm that matters. **It closes the
question note 082 left open** — *what to shed* — and it is the last piece of C4.

---

## IN PLAIN TERMS

Note 082 showed that promoting useful facts into a non-fading store works completely, and
that the non-fading store saturates in turn. So something has to be discarded, and the
question was what.

**Discard whatever has gone longest unused, and a fact that keeps being asked about survives
forever.** 100% of persistently-used facts are still there after 4,000 facts have streamed
past a store with 150 slots — with zero variance across seeds.

**So learning forever is reachable in fixed storage, just not in the form the constraint
literally states.** The store cannot hold everything it has ever seen; it can hold whatever
is still being used, for as long as that stays true. **Bounded in content, unbounded in
time.**

---

## The measurement

4,000 facts stream past. 100 are queried throughout (PERSISTENT), 100 only during the first
quarter (ABANDONED). 150 slots, so eviction is forced. Fast store `decay=0.99`.

    policy                   persistent            abandoned
    random             0.717 (sd 0.045)     0.783 (sd 0.045)
    least-recent       1.000 (sd 0.000)     0.500 (sd 0.000)
    least-frequent     1.000 (sd 0.000)     0.500 (sd 0.000)

**1.000 with zero variance**, and both content-aware policies are indistinguishable from each
other. Abandoned settles at exactly 0.500 because 150 slots hold the 100 persistent facts plus
50 of the abandoned ones — arithmetic, not a coincidence.

> **Random is WORSE on persistent than on abandoned (0.717 against 0.783), consistently across
> three seeds, and I cannot explain it.** The gap is about 1.5 sd so it is not obviously noise.
> Recorded rather than smoothed over: an unexplained inversion in a control arm is exactly the
> kind of thing that turns out to matter.

## Why this is C4's answer and not a dodge

C4 says the system never freezes and learns for as long as it runs. **Fixed storage cannot
retain everything — that is arithmetic, not a design failure.** What it can do:

    the fast store      a sharp recent window. 0.990 on the last 100 (`note 081`)
    promotion           what proved right gets copied out of the window, gated by
                        a signal `note 080` measures at six sd, label-free
    eviction            what has gone longest unused leaves. Persistent facts:
                        1.000, forever, regardless of age -- this note
    partitioning        the slot budget grows with the network (`note 082`)

**Nothing here freezes and nothing has a training phase that ends.** A fact's survival depends
on use rather than on when it arrived, which is the property that makes "forever" meaningful
in a finite store.

## What is NOT claimed

**Not that "used" is available for free.** The fixture queries facts on a schedule it
controls. A real system's queries come from outside, and **a useful fact nobody happens to ask
about during its window is gone before it can be promoted** — note 082's cost, unchanged and
unpaid here.

**Not that recency and frequency are equivalent in general.** They are indistinguishable in
this fixture because persistent facts are both recent *and* frequent by construction. A
workload where something is used often but in bursts would separate them, and that is untried.

**Not the real `capture_slots`.** `LocalMemoryConfig.capture_slots` exists and its docstring
already argues this case — *"a fixed number of slots is the only tried mechanism"* that holds
`N` constant, citing g8-01's decay from 0.05 at seq 192 to −0.00 at 1536. This reimplements
the idea rather than exercising that code, and it does not test the eviction policy the real
one uses.

**And rebuilding the slow store from its slots on every promotion is not a mechanism**, it is
a fixture convenience. A real implementation must subtract the evicted binding, which needs
the slot to have kept enough to reconstruct it — `capture_slots`'s docstring says a slot costs
`w + 1` numbers for exactly that reason.
