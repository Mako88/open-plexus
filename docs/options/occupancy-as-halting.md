# Option record — occupancy as a free halting signal

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Both halves exist separately: `AddressSketch` and the learned `halt_gate`. Nothing wires
  the first into the second.

---

## What was tried, and what came back

### Half the gate can go where the index cannot, and it has nothing to say there — `153`

    CONFIG  when    2026-07-29
            source  decision 153
            script  unrecorded
            task    chains
            model   occupancy read at chain start, middle and end
            knobs   none
            scale   unrecorded

    chain start    0.893
    chain middle   0.791
    chain end      0.898

**That is not a signal.** The end of a chain does not read differently from its start,
because every address on the chain was written before the query ran. Occupancy is
informative exactly where an address is READ BEFORE IT IS WRITTEN within the sequence, and
a traversal is the case where it never is.

**The attraction was that it would be free** — the sketch is already computed and needs no
training, where `halt_gate` is learned. Free and uninformative is still uninformative.

**Revival:** a task where a walk can run off the end of what was written, which is where
occupancy would separate. Nothing here does. Records:
[inherit-gate.md](inherit-gate.md) and [halt-gate.md](halt-gate.md).
