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

### The walk over a REAL impaired link: it misses `d_max` at depth 2 — `g24-01`

    CONFIG  when    2026-07-30
            source  g24-01
            script  testbed/run.py --mode peer --nodes 4 --width 64
                    --delay 80ms [--jitter 20ms --loss 2%]
            task    search.beam through RemoteConcepts, beam width 2, branches 2
            model   node_main peer mode, width 64, vocab 40, 24 facts, 2 replicas
            knobs   tc netem delay/jitter/loss, applied to the ASKER as well as
                    the peers
            scale   4 peers + 1 asker in containers, Docker 29.6.1

    clean 80 ms delay          80 ms + 20 ms jitter + 2% loss
    depth  wall ms  ms/round   depth  wall ms  ms/round
        1    322.3    161.15       1    328.5    164.25
        2    645.3    161.33       2   1083.3    270.83
        3    967.8    161.30       3   1814.7    302.45
        5   1614.4    161.44

**Note 101's table is the prediction and it was committed long before this run**: it
priced a round at an assumed 50 ms and put the breaking point at depth 8. Measured, a
round costs **161 ms** and the walk goes over `d_max` at **depth 2**. Depth 5, which that
table records as fitting at 78% of budget, costs **1,614 ms**.

Rounds equal `2 * depth` in every row of every run, so the STRUCTURE note 101 established
is confirmed exactly; only its constant was wrong, by 3.2x.

**And a term no model here has: with loss, cost is SUPERLINEAR in depth.** Clean,
`ms/round` is flat to two decimals. With 2% loss it grows — 164, 271, 302 — because each
additional round is another chance at a retransmit and a retransmit costs a timeout, not a
round trip. `tools/walk_rounds.py` and note 101 both treat a walk as `rounds x RTT`, which
is linear. **Depth is dearest exactly when the network is worst.**

Unimpaired on the same bridge: 0.40–1.21 ms/round, which is the identity check.

**What it does not settle.** `d_max` 640 ms is itself a floor from six simulated links
(`128`), and a real WAN raises it — that would move the bar the walk's way and is
unmeasured. This is one machine and one bridge: no route changes, no competing traffic, no
asymmetry, no NAT. And the migrating walk (`note 102`) is the untested answer, whose
required gain this run raises from about 2x to about 8x.

### A diverged peer is NOT detected, and serves a third of its answers wrong — `g27-01`

    CONFIG  when    2026-07-30
            source  g27-01
            script  in-process probe over openplexus/peer.py and node_main
                    populate/derive; recorded in the sweep
            task    24 scaffold facts, read every one through RemoteConcepts
            model   3 peers + asker, width 64, 2 replicas, ring seed 0
            knobs   one peer's MODEL seed changed from 5 to 99
            scale   24 reads per arm

    arm                    held per peer   reads ok   raised   mean |v|
    MATCHED  seeds 5,5,5   [16, 14, 18]          24        0     1.1561
    DIVERGED seeds 5,5,99  [16, 14, 18]          24        0     1.1408

    identical answers between arms: 16 of 24

**Nothing raised.** The diverged peer held the same number of facts, answered every
request, and its mean vector norm differs by 0.015 — not a quantity anyone could
threshold on. **A third of the answers changed, silently.**

**`peer.fingerprint` is honest about its coverage and the gap is beside it.** Its docstring
lists the wire format, the routing and the KEY SOURCE. The VALUE table is not in it:
`node_main.derive` builds values from the MODEL seed, which appears nowhere in the
fingerprint, while `PairKeys` is constructed with a fixed `seed=1` inside `derive` so the
key source matches regardless.

**So two peers can agree completely about where to look and disagree about what is stored
there.** Reachable today by getting one environment variable wrong.

**It is a small fix and it is not made here.** Both sides derive from the model seed
already, so adding it costs a string — a change to `peer.py` that belongs in a commit
saying so. Replica count is also unfingerprinted and untested.

**Scope:** this is the transport layer, not `DECISIONS.md` component 1's quantiser
question, whose falsifier **cannot be built** because no quantiser exists in the tree. Same
failure shape one level down.
