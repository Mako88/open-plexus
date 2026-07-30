# 101 — a hop is two round trips, and batching cannot make it one

2026-07-30. Builds note 100's fix, measures it, and finds it insufficient by a factor
of two. `tools/walk_rounds.py` is the measurement; `PROTOCOL` is now 3.

## What was built

A hop's reads now share one request. `RemoteConcepts.read_many` groups pairs by holder,
**sends every group before reading any reply**, and pairs answers to requests by
position. `read` is `read_many` of one, so there is a single read path rather than a
batch format bolted beside a scalar one.

`beam` issues three batches per hop-and-a-bit: the follow, the look-up, and at the end
the endpoints. `search.py` gained `_many`, which uses a reader's `many` attribute if it
has one and loops if it does not — so a local matrix, which has no round trips to save,
is unaffected and `beam` stays one function.

## What it bought, and what it did not

    depth   reads   rounds   one-by-one   batched   d_max 640 ms
        2      13        4        650 ms    200 ms   fits
        3      21        6      1,050 ms    300 ms   fits
        5      37       10      1,850 ms    500 ms   fits, 78%
        8      61       16      3,050 ms    800 ms   OVER
       10      77       20      3,850 ms  1,000 ms   OVER by 1.6x

Note 100 predicted batching would give `depth × RTT`. **It gives `2 × depth × RTT`, and
I should have seen that before writing the fix down as a solution.** A hop is two
DEPENDENT rounds:

    FOLLOW    read (entity, relation)      -> the next entity, after a decode
    LOOK UP   read (FACT, that entity)     -> its outgoing relations

The look-up's key *contains what the follow decoded to*, so no amount of batching puts
them in one request. Within each of the two, the `width` reads are independent, and that
is the part batching recovers — 3,850 ms to 1,000 ms at depth 10.

So the honest verdict is: **necessary, and not sufficient.** Depth 10 still misses
`d_max`, which is C2, which is a founding constraint rather than a performance target.

## Where the remaining factor of two is, measured rather than guessed

`PairKeys.owner` under `route="first-concept"` routes `(FACT, landed)` to concept
`landed`, and routes the NEXT hop's follow `(landed, relation)` to concept `landed` too.
**The look-up at one hop and the follow at the next are the same concept, so they are
the same peer.** `walk_rounds.py` counts it over the walk that actually ran: 12 of 19
consecutive rounds ask a peer the round before already spoke to.

That means the caller is bouncing to a peer, back to itself, and out to the same peer
again. A walk that MIGRATED — each peer decoding locally and handing the walk to the
next owner — would visit one peer per hop, and one-way hops cost half a round trip. The
arithmetic lands near `depth × RTT / 2`, comfortably inside `d_max` at depth 10.

**This is John's own suggestion arriving from the other direction.** He asked earlier
whether the search could work by broadcast — *"you broadcast and the nodes that care
about it receive it, and then they all broadcast"* — and I costed it as a traffic
problem. Costed as a LATENCY problem it is the thing that meets the constraint.

## The obstacle, named because it is real and not yet solved

A migrating beam has to prune, and pruning ranks all `width` partial walks against each
other. That is a rendezvous every hop. The caller is the rendezvous today, which is
exactly what costs the second round trip.

    width 1        no pruning, so the walk migrates freely, and rounds are depth
    width > 1      a `width`-way meeting per hop, or no global prune at all

A `width`-way meeting is bounded and is not the N-way collective C1 forbids — the
`R`-way write wait is already accepted on the same reasoning. But it is a barrier of a
kind, and whether a beam can prune LOCALLY at each peer without losing what pruning buys
is unmeasured. That is the next thing to measure, and it is a question about search
quality rather than about transport.

## What is not claimed

That 1,000 ms is the real number: 50 ms RTT is `docs/SCALE.md`'s assumption, and the
`tc netem` container harness has still never been pointed at the peer path. That
batching is free — it trades round trips for bytes, right while latency dominates
bandwidth and wrong on loopback, which is why note 094 could not see any of this. And
that the migration sketch works; its arithmetic is sound and its pruning is not.

## Two things this cost, worth recording

The wire pin from note 099 **earned its place immediately**: splitting the header into
`_REQUEST` and `_PAIR` failed `TheWireFormatIsPinnedToItsVersion` and the raw-socket
handshake test, which is a protocol change announcing itself instead of shipping. The
pin now covers both structs, since pinning the header alone would have missed a change
that made the header smaller.

And the first version of the batch failover test asserted bit-equality against the
pre-departure answer, and failed 6 of 24. Not a failover fault: a replica holds a
**different set of concepts**, superposes them into one matrix, so the same key carries
different interference there. Every other failover test in the suite asserts the decoded
token for this reason, which I would have known by reading one of them first.
