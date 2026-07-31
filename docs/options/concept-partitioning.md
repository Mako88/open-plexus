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

### G5's WALL WAS MEASURED ON DIMENSION SPLITTING, AND THIS HAS NEVER BEEN SCALED

    CONFIG  when    2026-07-31
            source  g5-01, and DECISIONS.md component 9
            script  none -- a scope finding from reading g5-01 and grepping
                    experiments/sweeps for concept_nodes
            task    none
            model   n/a
            knobs   none
            scale   n/a

`g5-01`'s own conclusion, in its own words:

    256 dimensions in one piece solve seq 384. The same 256 dimensions split
    sixteen ways reach 0.769. **The wall is caused by the partitioning, not by
    the underlying rule.**

**That wall is DIMENSION splitting**, where each node holds `d/n` of the width and every
read is a degraded partial read summed across nodes. **Concept partitioning does not do
that**: a read goes to the one node holding the fact, and that node has a FULL-WIDTH store
(`peer.py`: *"2 messages per read against 2N"*, no sum).

**Grepped `experiments/sweeps/` for `concept_nodes`: no file mentions it.** No sweep has
ever varied sequence length, node count or any scale axis under concept partitioning. The
measurement that produced `GOALS.md`'s G5 verdict does not cover the arrangement
`note 081` calls mandatory.

**Stated as an OPEN QUESTION, not a rescue.** Reasons for caution, both on record: `134`
measured pooled capacity as IDENTICAL between the two arrangements at equal per-node
memory, and it is lone-node capacity that differs by 16×. So a full-width read does not
obviously translate into a better scaling exponent, and the arithmetic has not been done.

**What would settle it:** `g5-01`'s grid re-run with `concept_nodes` set instead of
dimension slices, reporting the fitted exponent against g5-01's 0.69 and g1-10's
unpartitioned 0.37. Predictions registered first, and the exponent's confidence interval
reported rather than a point estimate — `g5-02` and `g5-03` are the calibration for fitting
through crossings that were bounds.

### It wins g5-01's cell outright — at 7.7× the state, so it settles nothing — `g29-01`

    CONFIG  when    2026-07-30
            source  experiments/sweeps/g29-01-does-concept-partitioning-escape-g5s-wall.txt
            script  experiments/g5_01_scaling.py --mode concept
            task    MQAR at g5-01's parameters
            model   concept_nodes=16 against partitions=16, both at d_model 256
            knobs   lr {0.01, 0.02, 0.05, 0.1, 0.2}, seeds 1-3, 16 epochs
            scale   seq_len 384, the cell g5-01 measured at 0.769

    concept     1.0000  in 30 cells of 30, zero spread across seeds and rates
    dimension   0.7549  mean, range 0.685-0.808 (g5-01 recorded 0.769)

**The state is not equal and the gap is 7.7×.** Each concept node keeps a full `d × d`
store, so sixteen nodes hold sixteen of them. Measured on a model that has actually run a
384-token sequence — `ConceptStore` allocates lazily, and a freshly constructed model
reports 81,984 numbers for every arrangement, so a count taken at construction would have
shown no difference:

    dimension, 16 groups at width 256      147,520
    concept,   16 nodes  at width 256    1,132,608

`local_memory.py` states this at the knob's own definition: the comparison at equal
`d_model` is *"biased TOWARD partitioning"*, a LOSS would be unambiguous, and a win
*"would need the g10-09 equal-state treatment before it meant anything."*

**Two further defects, recorded so the entry is not read as merely confounded.** Thirty
identical `1.0000`s is a grid at its ceiling, which cannot rank a learning rate and so
could not have detected an effect had one existed. And it is ONE sequence length: `#10` is
about how required width GROWS with length, and one cell cannot fit a slope.

**What it does establish**, and it is the same thing `tests/test_concept_routing.py`
already had at one seed: routing does not break learning.
[`g29-02`](../../experiments/sweeps/g29-02-concept-partitioning-at-EQUAL-state.txt) re-runs
the cell with the concept arm at width 64, where it holds **less** state than the dimension
arm and a win therefore cannot be bought. Its state figures live there, not here.

*Cost, which is a property of the arrangement and had not been written down:* concept cells
took 47–53 minutes against the dimension cells' 26–30 at equal width, so about **1.7×** per
cell.

### Learned identity and deterministic ownership are in tension — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- a conflict between two commitments, recorded unresolved
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

This arrangement rests on `Ring` — consistent hashing over a concept id, so **any node can
compute who owns a concept without asking anyone.** That is what makes a read one hop
instead of a broadcast, and it is why the cross-machine sum stops existing rather than
merely shrinking.

**It requires the id to be STABLE.** [`GOALS.md`](../../GOALS.md) now commits the project
to identity *learned from temporal co-occurrence* rather than computed at the edge — which is
negotiated, changes as evidence arrives, and differs between nodes with different
experience. **A concept whose identity is still being negotiated cannot be consistent
hashed.**

Two directions, **neither chosen and neither measured**:

- **Split routing from meaning.** A cheap deterministic id decides *where a thing lives*;
  learned structure decides *what it means*. Keeps every property this record depends on,
  at the cost of putting a quantiser back in the addressing path —
  [discrete-surface-ids.md](discrete-surface-ids.md) is where that half lives.
- **Converge by gossip.** Nodes negotiate concept identity between themselves over time.
  Honest to the goal and much harder: distributed agreement on a moving target, under the
  churn C3 requires.

**Recorded rather than resolved.** The failure this project has paid for repeatedly is a
conflict noticed piecemeal, one surprised result at a time, months after both commitments
were made. Both were made deliberately and both are current.

### The tension is dissolved: the concept was never the address — John, 2026-07-31

    CONFIG  when    2026-07-31
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Neither exit above was taken. **A concept gets no id at all** — it is the equivalence class
that falls out of co-occurrence links, reached by walking from any member. What needs a
stable hashable address is a *percept*, and percepts already have one.

So `Ring` keeps every property this record depends on, over `owner(surface id)` rather than
over a negotiated concept id, and identity stays learned on top. The full mechanism and its
costs are in [identity-without-a-global-id.md](identity-without-a-global-id.md); the
cross-node join that makes co-occurrence observable without a collective is in
[time-bucket-join.md](time-bucket-join.md).

**What this record must now carry as an open cost**: a percept that co-occurs with
everything accumulates an enormous neighbour list at one owner, which is new pressure on
the busiest-peer share measured above, and nothing has measured it.
