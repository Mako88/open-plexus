# Option record — the global dimension-summing readout

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- It is what `partitions` implies today: every node contributes a partial sum and one
  readout adds them.
- `combine="vote"` exists as the bandwidth mitigation, and does not remove the violation.

---

## What was tried, and what came back

### It is the globally synchronised step C1 forbids — `note 009 §4`

    CONFIG  when    2026-07-26
            source  note 009
            script  none -- reading the read path against the project's own constraint
            task    n/a
            model   dimension splitting with a summed readout
            knobs   none
            scale   n/a

The project's **first constraint**, violated by its default read path. Surfaced in a
footnote to note 009 §4 **after four gates were passed and five sweeps run on top of it**.

That is CLAUDE.md rule 17's calibration in one line: rigour on the wrong question is still
the wrong question, and the model carried a single global readout because MQAR asks for one
answer per query.

### `combine="vote"` mitigates the BANDWIDTH and not the violation

    CONFIG  when    2026-07-28
            source  decision 124
            script  unrecorded
            task    kinship, distributed
            model   dimension splitting
            knobs   combine sum against vote
            scale   4 bytes per node, about 8 KB at 1024 nodes

A vote costs 4 bytes per node rather than a full vector — about 8 KB at 1024 nodes — which
is a real saving and leaves the synchronisation exactly where it was.

### What removes it is a concept-partitioned read — `note 093`

    CONFIG  when    2026-07-30
            source  note 093
            script  openplexus/peer.py
            task    a single read
            model   point-to-point to the owning peer
            knobs   none
            scale   2 messages against 2N

A concept-partitioned read is a **selection**, not a sum, so there is nothing to
synchronise. Records: [peer-transport.md](peer-transport.md) and
[concept-partitioning.md](concept-partitioning.md).

**Revival:** none while C1 stands. If C1 were relaxed the arithmetic would still favour the
selection — 2 messages against 2N — so this is refused on two independent grounds.
