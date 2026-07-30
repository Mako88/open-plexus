# Option record — slice negotiation

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Static assignment: `LocalMemoryConfig.partitions` for dimension slices,
  `openplexus/ownership.py`'s `Ring` for concept ownership. Neither negotiates.
- `openplexus/deployment.py` sizes a node against cgroup limits, which is the nearest
  thing to a node choosing its own share and is a local decision rather than a protocol.

---

## What was tried, and what came back

### Static by John's explicit choice

    CONFIG  when    2026-07-28
            source  John's ruling in DECISIONS.md component 9, and note 095 for the
                    consistent-hashing figure
            script  none -- nothing built
            task    none
            model   static slices; consistent hashing for concepts
            knobs   partitions, concept_nodes, concept_replicas
            scale   n/a

A node that negotiates its own slice **is a coordination protocol**, and nothing needs one
yet. That is the whole argument: consistent hashing already gives a joining peer its share
without anyone agreeing to anything — a peer joining moves 1.4% of concepts at 64 peers
(`note 095`) — so the case negotiation would solve is not currently arising.

It is also the option most obviously in tension with C1: a negotiation is a collective
decision, which is the thing the read path was rebuilt to remove. Record:
[global-summing-readout.md](global-summing-readout.md).
