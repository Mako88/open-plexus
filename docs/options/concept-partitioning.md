# Option record — partition the store by CONCEPT

> **RECORD ONLY. This file carries no status.** Whether this option is chosen, refused,
> untried or live-both lives in `DECISIONS.md` and nowhere else. If you are about to write
> "we use this" or "this is blocked", it belongs there.
>
> **Only events are recorded here, and events do not un-happen.** Every entry says what
> was tried, what the model looked like when it was tried, and what came back. That is why
> this file cannot go stale: a conclusion can be superseded, but "on 2026-07-14 this
> configuration produced 0.9220" stays true forever. The append-only log that preceded
> this project's tree went wrong by holding conclusions, not by holding history.
>
> **Absence means untried.** There is deliberately no "gaps" or "next steps" section —
> those are status, they rot, and `DECISIONS.md` owns them.
>
> **The model state matters and is recorded per entry.** A result taken at `hops=2` with
> single-token keys is not evidence about the same option at `hops=10` with pair keys, and
> this project has twice drawn a wrong conclusion by quoting a number across regimes.

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

**Model state:** design pass, nothing built.

Claimed that concept partitioning multiplies total capacity where dimension splitting does
not. **The arithmetic does not support that** and it was written anyway: per unit of
memory the two are the same, and the note records the overstatement rather than quietly
fixing it.

What survived is narrower: holding **per-node memory fixed**, a concept node holds a
full-width store for its own concepts while a dimension node holds a `(d/N) × d` slice
that shrinks as nodes are added.

### The lone-node floor — `g4-01`

**Model state:** dimension splitting, the then-default.

A single node's answer holds at 16 dimensions and degrades fast below:

    16 dims   0.949
     8 dims   0.681
     4 dims   0.412

So under dimension splitting node count is bounded by `width ÷ 16`.

### Pooled capacity is identical; lone-node capacity is not — `134`

**Model state:** per-node memory held equal at ~4,096 numbers, 5 seeds, 50 cells.

    pooled capacity      IDENTICAL to dimension splitting at every node count
    lone-node capacity   2048 against 128 at 16 nodes -- a factor of sixteen

### C4 cannot be met without it — `note 081`

**Model state:** single store, capacity `~0.023·d²`, arranged at 10.6× overload.

Both alternatives to growing capacity fail. No decay saturates — recall **0.07 at 10.6×**,
and *symmetric*, oldest beating recent, so it is interference rather than forgetting and
**replay cannot fix it**. Decay windows hold 0.990 on the last 100 and **0.000** on
anything older.

Since each concept node holds a full-width store for its own concepts, total capacity is
`nodes × per-node`. Same note: the gate degrades under load — `148`'s structurally-zero
read is 1.26 at half capacity and **1.03 at 10.6×** — so gate health tracks live load
rather than total writes.

### Accuracy, measured twice at different node counts

**Model state:** CLUTRR `gen_train23_test2to10`, kinship layout, beam search, width 64.

    nodes   beam    source
        0   0.8877  `note 081` companion, monolithic
        4   0.9220  `note 081` companion -- 713/713 on the plain subset
        8   0.9058  `note 103`, seed 0 only
        0   0.8770  `note 103`, seed 0 only, monolithic

The stated mechanism is that a node carries interference only from what it owns. The
two monolithic figures differ because they are different seed counts, which is the kind of
difference the model-state line exists to make visible.

### Ownership under the kinship layout capped it at twenty nodes — `note 072`

**Model state:** ownership was `previous_concept = tokens[t-1]`, CLUTRR kinship layout.

Kinship puts the RELATION in that position, so **100.0% of CLUTRR's 7,132 traversal
bindings were owned by a relation** (`sister` alone 20.2%) against 0.0% under the
`closure` layout. Two options each chosen alone, and the *pair* was the defect: `157`
picked kinship for a 4.7× collision reduction with ownership not in view.

### `PairKeys(route="first-concept")` moved ownership to entities — `note 073`

**Model state:** as above, with the new route built and not defaulted.

Traversal bindings move from relation-owned to **entity**-owned, markers stop owning
content (31.6% → **0.0%**), and the busiest peer drops 26.6% → **11.8%**.

073's original "0.0% relation-owned" is **corrected**: it scored 2 of 4 keys per block, and
`pair(relation, entity)` remains relation-owned at 22.3% of all keys — though its value is
a separator the traversal never reads.

### The two routes are bit-identical when nothing is partitioned — `note 103`

**Model state:** `concept_nodes=0`, CLUTRR, seed 0.

Both routes returned 0.7914 search / 0.8770 beam, to four decimals. `owner()` is consulted
only when the store is partitioned, so an unpartitioned comparison of routes is not a
measurement of anything.

### Over the wire, with no driver — `notes 093`–`101`

**Model state:** `openplexus/peer.py`, `PROTOCOL` 2 then 3, loopback only.

    messages per read      2 against 2N for broadcast, at width 256
    consistent hashing     a peer joining moves 1.4% of concepts at 64 peers,
                           where `concept % peers` moved 98.4%
    departure              costs a round trip, not the answer, when writes fan out
                           to every holder AND reads walk them
    walk latency           batched, a depth-10 beam is 20 rounds -- 1,000 ms at a
                           50 ms RTT, against `d_max` 640 ms

No measurement over a real link: `SCALE.md`'s 50 ms is an assumption, and the `tc netem`
container harness has never been pointed at the peer path.

### The `hops > 1` refusal was checked against the code — 2026-07-30

**Model state:** `search_beam_width=4` defaulted that day (`note 103`).

The refusal's stated reason is that the soft hop key is *"a softmax mixture of every
token's key row, so it names no concept"*. The hop loop is
`for depth in range(0 if searching else ...)`, so when search is on it runs zero times and
that key is never built; the walk commits to a hard token at every step. Read, not run —
no sweep has been taken with `concept_nodes` and `hops > 1` together.
