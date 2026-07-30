100 — Messages per read was the wrong axis; reads per walk is the one that binds
==============================================================================

**Status:** measured, and it corrects the framing of notes 093 and 094 — both mine, from
earlier today. **The driver-free read path as built cannot meet `d_max`**, and the reason is
not the thing either note was counting.

---

## IN PLAIN TERMS

Removing the driver was sold on messages: a read costs 2 instead of 2 per peer, which is
256 times better at 256 peers. **That is true and it is not what decides whether a walk
finishes in time.**

A walk of ten hops at beam width 4 makes **77 reads**. It only has **ten** genuine
dependencies — everything inside one hop can be asked at once — but the client asks them one
at a time and waits for each. At a realistic 50 ms round trip that is **3.9 seconds against
a 640 ms budget.**

**Pipelined, the same walk is 500 ms and fits.** The gap between 3.9 s and 500 ms is entirely
in how the client asks.

---

## The measurement

    depth  width   reads   sequential deps   at 50 ms RTT   d_max 640 ms
        3      4      21                 3          150 ms   fits
        5      4      37                 5          250 ms   fits
       10      1      20                10          500 ms   fits, 78% of budget
       10      4      77                10          500 ms   fits IF pipelined

**Reads grow as `width × depth`; dependencies grow as `depth` alone.** Hop `k+1` needs hop
`k`'s answer, but the `width × (1 + branches)` reads *within* a hop are independent of each
other.

**`RemoteConcepts.read` is synchronous** — send, block on receive, return. So the built path
pays `reads × RTT`, not `depth × RTT`:

    depth 10, width 4, synchronous     77 x 50 ms = 3,850 ms      6x over d_max
    depth 10, width 4, pipelined       10 x 50 ms =   500 ms      78% of budget

## What this says about notes 093 and 094

Both are correct about what they measured and both measured the wrong quantity for this
question.

    note 093   "2 messages per read against 2N" -- true, and it is a THROUGHPUT and
               fan-out claim. It says nothing about a walk's wall time
    note 094   "11 reads over sockets, 7.1 ms for the walk" -- on loopback, where an
               RTT is 0.05 ms, so the serial cost was invisible. **A loopback
               measurement cannot see the constraint that binds**, which note 086
               said in as many words about the driver and I did not apply here

> **The lesson is the one that keeps recurring in different clothes: measure the quantity the
> constraint is about.** `d_max` is a deadline on a walk, so the axis is sequential round
> trips, and a message-count table invites exactly the wrong conclusion.

## What has to change

**Batch the reads within a hop into one request.** The client already knows all of them before
it issues any: a hop's candidate expansion is `width × branches` pairs, computed from the
previous hop's results. One request carrying `k` pairs and one reply carrying `k` vectors
turns `reads × RTT` into `depth × RTT`.

That is a protocol change, so `PROTOCOL` goes to 3 — and `note 099`'s pin means forgetting to
bump it fails a test rather than shipping.

## What is NOT claimed

**Not that 500 ms is comfortable.** It is 78% of `d_max` at ten hops with no jitter, no
retry, and no queueing at a peer. `SCALE.md` already called the headroom about 20%, and this
does not improve it — it only stops the client wasting the budget it has.

**Not measured over a real link.** The 50 ms figure is `SCALE.md`'s assumption, and the
container harness with `tc netem` exists (`note 086`) and has never been pointed at the peer
path.

**And batching is not free.** A batched request carries `k` pairs and a batched reply `k`
width-vectors, so a hop at width 4 moves ~8 KB in one message instead of 2 KB in each of
several. It trades round trips for bytes, which is the right trade only while latency
dominates bandwidth — true at 50 ms and untrue on loopback, which is why nothing here noticed
until the arithmetic was done.
