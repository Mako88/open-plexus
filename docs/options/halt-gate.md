# Option record — `halt_gate`, a learned halting gate

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.halt_gate`, `gate_sharpness`, `gate_reads_key`, `gate_objective`.
- `tests/test_hops.py`, `tests/test_reward_gate.py`, and the gate mutations in
  `tools/mutate.py`.

---

## What was tried, and what came back

### A halting signal exists, and it is not confidence — `086`

    CONFIG  when    2026-07-28
            source  decision 86
            script  unrecorded
            task    chains and kinship, mixed depths
            model   a gate reading the retrieval
            knobs   halt_gate
            scale   unrecorded

What separates the stopping point from the continuing one is the **CONTENT** of the
retrieval, not how confidently it decodes. That distinction is what makes the gate a
different object from the decode margin, whose record is
[select-by-decode-margin.md](select-by-decode-margin.md).

### The gate learns which hop to read, and mixed depths reach 1.000 — `087`

    CONFIG  when    2026-07-28
            source  decision 87
            script  unrecorded
            task    mixed-depth chains
            model   learned halting gate
            knobs   halt_gate
            scale   unrecorded

**Two defects were found on the way, each of which looked like a working mechanism**, and
**the mutation harness caught what the tests did not**. That is the entry behind the
convention that a new mechanism arrives with a mutation.

### Three depths at once, and the gain has an upper edge — `088`

    CONFIG  when    2026-07-28
            source  decision 88
            script  unrecorded
            task    three depths simultaneously
            model   as above
            knobs   depth mix
            scale   unrecorded

### It generalises to a depth it never trained on, zero-shot — `092`

    CONFIG  when    2026-07-28
            source  decision 92
            script  unrecorded
            task    a depth held out of training entirely
            model   as above
            knobs   none
            scale   unrecorded

**0.992** on a depth it never saw. This is the strongest single result for the gate, and
the reason it is treated as a mechanism rather than a lookup.

### It is a token detector, measured, and the sign was the opposite of what was predicted — `089`

    CONFIG  when    2026-07-28
            source  decision 89
            script  unrecorded
            task    chains
            model   as above
            knobs   none
            scale   halt_w at +8.3 sd on one token's value vector

`halt_w` sits **+8.3 sd** on one token's value vector. So the gate learned to detect a
specific terminator token rather than a general property of being finished — **and the
prediction registered beforehand had the sign backwards**, which is recorded rather than
smoothed.

That measurement is what makes the two refusals beside it inevitable rather than
surprising: [gate-transfer.md](gate-transfer.md) and
[token-agnostic-terminal.md](token-agnostic-terminal.md).
