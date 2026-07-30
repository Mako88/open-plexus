# Option record — partition the store by DIMENSION

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `LocalMemoryConfig.partitions`. Every node computes `M_slice @ key_slice` and inherits
  the sum.
- `openplexus/distributed.py`, `tests/test_distributed.py`,
  `tests/test_partitioned_readout.py`.

---

## What was tried, and what came back

### A lone node's answer holds at 16 dimensions and degrades fast below — `g4-01`

    CONFIG  when    2026-07-28
            source  g4-01
            script  experiments/g4_01_partitions.py
            task    kinship
            model   dimension splitting
            knobs   slice width 4, 8 and 16 dimensions
            scale   unrecorded

    16 dims   0.949
     8 dims   0.681
     4 dims   0.412

**So node count ≈ width ÷ 16**, and that is a hard bound rather than a soft preference —
which is the ceiling concept partitioning does not have.

### Pooled capacity is identical to concept partitioning — `134`

    CONFIG  when    2026-07-28
            source  decision 134
            script  unrecorded
            task    synthetic capacity probe
            model   per-node memory held equal at ~4,096 numbers
            knobs   1, 2, 4, 8 and 16 nodes, both arrangements
            scale   5 seeds, 50 cells

Identical at every node count. What differs is **lone-node** capacity — 2048 against 128 at
16 nodes — because a concept node holds a full-width store for its own concepts while a
dimension node holds a `(d/N) × d` slice that shrinks as nodes are added.

### The readout it implies is the thing C1 forbids — `note 009 §4`

    CONFIG  when    2026-07-28
            source  note 009
            script  none -- reading the read path against the constraint
            task    n/a
            model   every node contributes a partial sum to one readout
            knobs   combine
            scale   n/a

Summing slices across nodes **is** a globally synchronised step. Surfaced in a footnote
after four gates were passed and five sweeps run on top of it. Record:
[global-summing-readout.md](global-summing-readout.md).
