# Option record — partition the store by CONCEPT

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/partitioned.py` — `ConceptStore`, the per-concept store. `read` takes from
  **one** surviving holder: *"No pooling, no vote, no barrier."*
- `openplexus/ownership.py` — `Ring`, consistent hashing, `holders(concept, replicas)`.
- `LocalMemoryConfig.concept_nodes` (count) and `concept_replicas`.
- `openplexus/peer.py` — `ConceptPeer` and `RemoteConcepts`, the same partitioning over
  sockets with no driver.
- Six refusals in `LocalMemoryConfig.__post_init__` blocking `concept_nodes` from
  combining with `hops > 1`, `reward_token`, `memory_cap`, `tag_relative`, `carry_store`
  and `consolidation`.

---

## What was tried, and what came back

### The capacity argument, first stated and then corrected — `note 043`

    CONFIG  when    2026-07-28
            source  note 043
            script  none -- design pass
            task    design pass, nothing built
            model   n/a
            knobs   n/a
            scale   n/a

Claimed that concept partitioning multiplies total capacity where dimension splitting does
not. **The arithmetic does not support that** and it was written anyway: per unit of
memory the two are the same, and the note records the overstatement rather than quietly
fixing it.

What survived is narrower: holding **per-node memory fixed**, a concept node holds a
full-width store for its own concepts while a dimension node holds a `(d/N) × d` slice
that shrinks as nodes are added.

### The lone-node floor — `g4-01`

    CONFIG  when    2026-07-28
            source  g4-01
            script  experiments/g4_01_partitions.py
            task    kinship
            model   dimension splitting, the then-default
            knobs   slice width 4, 8 and 16 dimensions
            scale   unrecorded

A single node's answer holds at 16 dimensions and degrades fast below:

    16 dims   0.949
     8 dims   0.681
     4 dims   0.412

So under dimension splitting node count is bounded by `width ÷ 16`.

### Pooled capacity is identical; lone-node capacity is not — `134`

    CONFIG  when    2026-07-28
            source  decision 134
            script  unrecorded
            task    synthetic capacity probe
            model   per-node memory held equal at ~4,096 numbers
            knobs   1, 2, 4, 8 and 16 nodes, both arrangements
            scale   5 seeds, 50 cells

    pooled capacity      IDENTICAL to dimension splitting at every node count
    lone-node capacity   2048 against 128 at 16 nodes -- a factor of sixteen

### C4 cannot be met without it — `note 081`

    CONFIG  when    2026-07-30
            source  note 081
            script  unrecorded
            task    a stream arranged at 10.6x the store's capacity
            model   single store, capacity ~0.023*d^2
            knobs   decay on and off
            scale   unrecorded

Both alternatives to growing capacity fail. No decay saturates — recall **0.07 at 10.6×**,
and *symmetric*, oldest beating recent, so it is interference rather than forgetting and
**replay cannot fix it**. Decay windows hold 0.990 on the last 100 and **0.000** on
anything older.

Since each concept node holds a full-width store for its own concepts, total capacity is
`nodes × per-node`. Same note: the gate degrades under load — `148`'s structurally-zero
read is 1.26 at half capacity and **1.03 at 10.6×** — so gate health tracks live load
rather than total writes.

### Accuracy at four nodes, measured — `note 105`

    CONFIG  when    2026-07-30
            source  note 105, and note 075 for the monolithic baseline
            script  tools/clutrr_recovery.py --concept-nodes 4 --seeds 0 1 2
            task    CLUTRR gen_train23_test2to10, kinship layout, chain recovery
            model   width 64, decay 1.0, route current
            knobs   concept_nodes 4 against 0, beam width 4, branches 4
            scale   1146 puzzles, 3 seeds

     seed   search     beam      plain
        0   0.7880   0.9049    713/713
        1   0.8290   0.9389    713/713
        2   0.8098   0.9223    712/713

    mean   search 0.8089   beam 0.9220
    note 075, monolithic, same script and seeds:  search 0.7810   beam 0.8877

**+0.0343 on the beam and +0.028 on search.** The stated mechanism is that a node carries
interference only from what it owns.

This entry exists because `0.9220` was carried in the tree for a day citing `note 081`,
which contains no partitioning measurement at all — no `0.9220`, no `0.8877`, no mention
of a companion. Note 090 quoted it against a different baseline (`0.8805`, note 065's
unrecoverable-configuration mean) and note 103 quoted it as *"note 081's companion
measurement"*. **The run was real and had simply never been written down**, which the
re-run settles: it reproduces to four decimal places.

### A second node count, seed 0 only — `note 103`

    CONFIG  when    2026-07-30
            source  note 103
            script  tools/clutrr_recovery.py --concept-nodes 8
            task    CLUTRR, chain recovery
            model   width 64
            knobs   concept_nodes 0 and 8, route current and first-concept
            scale   seed 0 only, recorded in note 103 as a lead and not a result

    concept_nodes   route            search    beam
    0               either           0.7914    0.8770
    8               current          0.7845    0.9058
    8               first-concept    0.8141    0.9040

`route` is accuracy-neutral for the beam (0.9058 against 0.9040). At `concept_nodes=0` the
two routes are **bit-identical**, which is not a measurement: `owner()` is consulted only
when the store is partitioned, so an unpartitioned comparison of routes cannot say
anything.

### Ownership under the kinship layout capped it at twenty nodes — `note 072`

    CONFIG  when    2026-07-30
            source  note 072
            script  unrecorded
            task    CLUTRR, kinship layout against closure layout
            model   ownership as `previous_concept = tokens[t-1]`
            knobs   layout kinship against closure
            scale   7,132 traversal bindings

Kinship puts the RELATION in that position, so **100.0% of CLUTRR's 7,132 traversal
bindings were owned by a relation** (`sister` alone 20.2%) against 0.0% under the
`closure` layout. Two options each chosen alone, and the *pair* was the defect: `157`
picked kinship for a 4.7× collision reduction with ownership not in view.

### `PairKeys(route="first-concept")` moved ownership to entities — `note 073`

    CONFIG  when    2026-07-30
            source  note 073
            script  unrecorded
            task    CLUTRR, kinship layout
            model   the new route built and not defaulted
            knobs   PairKeys route first-concept against the previous route
            scale   7,132 traversal bindings

Traversal bindings move from relation-owned to **entity**-owned, markers stop owning
content (31.6% → **0.0%**), and the busiest peer drops 26.6% → **11.8%**.

073's original "0.0% relation-owned" is **corrected**: it scored 2 of 4 keys per block, and
`pair(relation, entity)` remains relation-owned at 22.3% of all keys — though its value is
a separator the traversal never reads.

### Over the wire, with no driver — `notes 093`–`101`

    CONFIG  when    2026-07-30
            source  notes 093-101
            script  openplexus/peer.py, tools/walk_rounds.py
            task    reads, writes and a beam walk across peers
            model   openplexus/peer.py, PROTOCOL 2 then 3, loopback only
            knobs   concept_replicas, ring seed, read_many batching
            scale   width 256; up to 64 peers; latency priced at an assumed 50 ms RTT

    messages per read      2 against 2N for broadcast, at width 256
    consistent hashing     a peer joining moves 1.4% of concepts at 64 peers,
                           where `concept % peers` moved 98.4%
    departure              costs a round trip, not the answer, when writes fan out
                           to every holder AND reads walk them
    walk latency           batched, a depth-10 beam is 20 rounds -- 1,000 ms at a
                           50 ms RTT, against `d_max` 640 ms

No measurement over a real link: `SCALE.md`'s 50 ms is an assumption, and the `tc netem`
container harness has never been pointed at the peer path.

### The `hops > 1` refusal was checked against the code

    CONFIG  when    2026-07-30
            source  read of openplexus/models/local_memory.py, no run
            script  none -- source reading
            task    none
            model   search_beam_width defaulted to 4 that day (note 103)
            knobs   concept_nodes > 0 with hops > 1
            scale   n/a

The refusal's stated reason is that the soft hop key is *"a softmax mixture of every
token's key row, so it names no concept"*. The hop loop is
`for depth in range(0 if searching else ...)`, so when search is on it runs zero times and
that key is never built; the walk commits to a hard token at every step. Read, not run —
no sweep has been taken with `concept_nodes` and `hops > 1` together.
