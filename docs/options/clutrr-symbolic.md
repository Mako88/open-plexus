# Option record — CLUTRR-symbolic

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/clutrr.py`, the loader; `tools/clutrr_recovery.py`, the reproducible
  chain-recovery harness; `tests/test_clutrr.py`.
- Split `gen_train23_test2to10`, layout **`kinship`**.

---

## What was tried, and what came back

### The graph layer, never the prose

    CONFIG  when    2026-07-29
            source  openplexus/tasks/clutrr.py
            script  openplexus/tasks/clutrr.py
            task    CLUTRR gen_train23_test2to10
            model   n/a -- the instrument
            knobs   layout kinship
            scale   1,146 puzzles in the test split

Results are *"CLUTRR-symbolic"* and **published text numbers are not comparable**. The
layout was chosen on measurement — collisions 35.9% → 7.7%, which is `157`'s mechanism
applied to someone else's data.

### Report per hop bucket and split on ENTITY REPETITION — `note 059`

    CONFIG  when    2026-07-29
            source  note 059
            script  tools/clutrr_recovery.py
            task    CLUTRR, train and test splits compared
            model   n/a -- a property of the data
            knobs   none
            scale   train and test entity-repetition rates

**Test is 37.8% repeated where train is 0%.** So a falling accuracy curve reads as depth and
is really decision 103's addressing problem. Any per-depth number that does not split on
this is confounded.

### The `hops=1` floor is not chance — `note 060`

    CONFIG  when    2026-07-29
            source  note 060
            script  tools/clutrr_recovery.py
            task    CLUTRR, hops=1
            model   n/a
            knobs   none
            scale   unrecorded

**0.0856**, not chance, because sequence length leaks the hop count. A baseline that is not
chance has to be measured rather than assumed, and this is the entry that measured it.

### 065's numbers have no committed script — `note 074`, `note 075`

    CONFIG  when    2026-07-30
            source  notes 074-075
            script  tools/clutrr_recovery.py
            task    CLUTRR chain recovery
            model   width 64
            knobs   width, allowed mask, branches -- all tested
            scale   3 seeds

No committed script reproduces note 065's figures, so its configuration is unrecovered.
`tools/clutrr_recovery.py` prints 065's numbers beside its own and **gates on the mismatch**,
which is why it says *"this is a harness, not a finding"* in its own output.

**Differences are taken against that harness's own baseline** — three-seed means search
0.7810 and beam 0.8877.

### What it CANNOT test: concept acquisition — `note 076`

    CONFIG  when    2026-07-30
            source  note 076
            script  unrecorded
            task    CLUTRR
            model   n/a -- a property of the data
            knobs   none
            scale   entities carry 1-2 edges

Entities carry one or two edges, so **two surfaces of one concept share nothing by
arithmetic**. That gap is what OpenEA was fetched for. Record: [openea.md](openea.md).
