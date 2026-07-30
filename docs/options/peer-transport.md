# Option record — `openplexus/peer.py`, point-to-point reads and writes with no driver

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/peer.py` — `ConceptPeer`, `RemoteConcepts`, `PROTOCOL` 3, and `read_many`.
- `openplexus/ownership.py` — `Ring`, consistent hashing, `holders`, `owner`.
- `tests/test_peer_reads.py`, `test_batched_reads.py`, `test_departure.py`,
  `test_concept_routing.py`, `test_search_partitioned.py`.
- `tools/walk_rounds.py`.

---

## What was tried, and what came back

### Two messages per read, against 2N for broadcast — `note 093`

    CONFIG  when    2026-07-30
            source  note 093
            script  openplexus/peer.py
            task    a single read
            model   point-to-point to the peer owning the concept
            knobs   none
            scale   width 256, up to 256 peers

**2 messages against 2N** — 256× at 256 peers — and **the serialisation point goes with
it**. C1's collective is off the read path because the read is a selection rather than a
sum.

### A driver-free `beam` traversal is EXACT — `note 094`

    CONFIG  when    2026-07-30
            source  note 094
            script  openplexus/search.py, openplexus/peer.py
            task    CLUTRR beam walk
            model   `search` taking an injectable `reader=`
            knobs   correct routing against a deliberately misrouted control
            scale   unrecorded

Identical walk to one process, **and a misrouted control changes it** — so the routing is
what produces the answer rather than the walk being insensitive to it. That paired shape is
what makes the exactness claim mean something.

`search` takes `reader=` so a caller injects routing and `search` never imports a transport.

### Consistent hashing — `note 095`

    CONFIG  when    2026-07-30
            source  note 095
            script  openplexus/ownership.py
            task    a peer joining
            model   Ring against `concept % peers`
            knobs   peer count
            scale   64 peers

    Ring              1.4% of concepts move
    concept % peers   98.4% move

The ideal is `1/n`, and the Ring lands within a tenth of a point of it. The same note
records a control that *weakened* and says so.

### A departure costs a round trip, not the answer — `note 097`

    CONFIG  when    2026-07-30
            source  note 097
            script  openplexus/peer.py
            task    reads during a peer departure
            model   reads walk `Ring.holders`, writes fan out to every holder
            knobs   each half alone, and both together
            scale   unrecorded

**Both halves are needed; either alone looks fine.** Losing every holder returns zeros
**and counts them** — an uncounted zero decodes to whatever the readout prefers, which is a
wrong answer wearing the shape of a right one.

### Writes cross the wire — `note 098`

    CONFIG  when    2026-07-30
            source  note 098
            script  openplexus/peer.py
            task    writes to a partitioned store
            model   fan-out to every holder
            knobs   concept_replicas
            scale   unrecorded

### Fingerprinted, and it caught a change the day after it was written — `note 096`, `note 099`

    CONFIG  when    2026-07-30
            source  notes 096 and 099
            script  tests/test_peer_reads.py
            task    none -- a compatibility guard
            model   peer count, ring seed, key params and the wire-format version
            knobs   none
            scale   pinned to every struct by a test

**It caught `PROTOCOL` 3 the day after it was written.** A guard that fires within a day of
existing is the cheapest possible evidence that it is connected.

### One read per round trip was the wrong axis — `note 100`, `note 101`

    CONFIG  when    2026-07-30
            source  notes 100-101
            script  tools/walk_rounds.py
            task    a depth-10 beam walk
            model   PROTOCOL 3, loopback
            knobs   read_many batching on against off
            scale   priced at an assumed 50 ms RTT

    77 reads at depth 10, unbatched   3,850 ms
    20 rounds, batched                1,000 ms
    d_max                               640 ms

**Necessary and not sufficient.** A hop is two *dependent* rounds — follow, then look up
what the follow decoded to — so batching cannot make it one.

### A MIGRATING walk is where the remaining 2× is — `note 101`, `note 102`

    CONFIG  when    2026-07-30
            source  notes 101-102
            script  tools/prune_period.py, tests/test_prune_period.py
            task    CLUTRR chain recovery
            model   `owner` routing a hop's look-up and the next hop's follow to the
                    same concept
            knobs   search_prune_every 1 to 5 and never
            scale   1,146 puzzles, 3 seeds

**12 of 19 consecutive rounds ask a peer the round before had already used.** One peer visit
per hop is about `depth × RTT/2`.

**`note 102` CLEARS the pruning blocker**: the rendezvous is worth **0.089**, and its
PERIOD is nothing measurable against a seed spread of 0.0305. So a migrating walk must
meet — not meet *every hop*. `k=2` fits `d_max` for **2.29×** the reads.

**NOT BUILT.** The latency figures are estimates; `tests/test_prune_period.py` pins the
real path at `2 × depth` rounds.

### The costs, stated

    CONFIG  when    2026-07-30
            source  notes 093-101
            script  openplexus/peer.py
            task    n/a
            model   as above
            knobs   none
            scale   512 KB against 2 KB

Retrieval moves to the owning node, because a remote store cannot return a `d×d` matrix —
**512 KB against 2 KB**. A write waits for `R` holders, which is not `N` but is not free.
Batching trades round trips for bytes.

**Untried:** write ordering (they race, and the store is additive), re-replication,
negotiation rather than refusal, and any real link — every latency figure here is priced
from an assumed 50 ms RTT on loopback.
