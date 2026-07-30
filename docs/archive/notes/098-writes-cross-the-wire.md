098 — Writes cross the wire, and the read path finally has something to fall back to
===================================================================================

**Status:** built and measured. It closes note 097's first gap — *"no peer-to-peer write path
exists; `peer.py` serves reads only"* — and it is what makes this a network rather than a
distributed cache.

---

## IN PLAIN TERMS

Reads have been going to whichever peer holds a fact for several notes now, and every one of
those measurements loaded the facts into the peers **by hand**. Nothing had ever put a fact
onto a peer over a socket.

**Now a write goes to every peer that holds the concept, and each one acknowledges.** Twelve
facts written over sockets into empty stores, all twelve read back correctly, and when the
owner was killed the fact was still there — because the replica already had it.

---

## The measurement

    12 writes over the wire, replicas = 2, stores EMPTY to begin with

    holders acknowledging      min 2, max 2
    read back correctly        12/12
    writes served by peers     24          (12 facts x 2 replicas)
    after the owner left       survived
    lost writes / absent reads 0 / 0

**Empty stores are the point.** Every earlier peer measurement pre-loaded the data, so a write
path that did nothing at all would not have shown up.

## The one place a write is not free, stated rather than hidden

`write` waits for every holder it can reach, which is an **`R`-way wait**. `R` is 2 or 3 rather
than `N`, so it is not the collective C1 forbids — but it is not nothing.

**The alternative is to acknowledge the owner and let the replicas catch up**, which trades the
wait for a window in which a read can miss a recent write. Which is right depends on a
measurement nobody has taken, and the wait was chosen because it is the version whose failure
modes are obvious.

**Fewer acknowledgements than `replicas` is not an error.** A departed peer is what C3 says to
expect, and `ConceptStore.write` takes the same view — it skips absent nodes rather than
failing. A write reaching **nobody** increments `lost`, for the same reason an absent read
increments `absent`: a write that lands nowhere and says nothing is a fact the network believes
it holds.

## Two mechanisms that only work as a pair

Note 097 made reads walk every holder. This makes writes reach every holder. **Either alone is
useless and looks fine:** a fanned-out write with a single-holder read gains nothing, and a
falling-back read with an owner-only write falls back to an empty store and returns zeros.

So both halves carry a mutation — `a-read-gives-up-on-the-owner` and
`a-write-goes-only-to-the-owner` — and the departure test is what catches either. **194
mutations, 1298 tests.**

> The write's fan-out is assigned to a named `targets` because `for node in
> self.holders(concept)` now occurs on both paths, and a mutation cannot anchor on a line that
> appears twice. **The harness refused to guard duplicated code**, which is the duplication
> rule arriving from an unexpected direction for the second time (note 094 was the first).

## What is NOT claimed

**Not that the protocol is versioned.** Adding a write kind to the header changed the wire
format, and the raw-socket handshake test broke because it hardcodes that format — which is
exactly note 096's *"a protocol change is invisible to the fingerprint"*, arriving as a test
failure rather than as two peers silently misparsing each other. **A real network needs a
protocol version in the handshake and does not have one.**

**Not ordered.** Two writes to the same key from different callers land in whatever order the
sockets deliver, and the store is additive so the result depends on that order. Nothing here
sequences anything.

**Not re-replicated.** A write after a departure reaches the survivors only. The concept's
holder count silently drops and nothing restores it, which is note 097's gap unchanged.

**And not measured under load or latency.** Loopback, five peers, twelve facts.
