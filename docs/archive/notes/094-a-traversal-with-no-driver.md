094 — A traversal with no driver
================================

**Status:** built, measured, with a misroute control. **It closes the gap note 093 named
in its own "what is NOT claimed"** — the transport existed and no traversal used it.

---

## IN PLAIN TERMS

Note 093 built reads that go straight to the node holding the fact. It said plainly that
nothing actually walked that way.

**Now a full beam traversal does.** Every read goes to the one peer that owns it, nothing
is broadcast, nothing is summed, and the walk it finds is the same one a single process
finds.

---

## The measurement

    in-process walk      (20, 21, 22)
    DRIVER-FREE walk     (20, 21, 22)
    misrouted control    (21, 23, 22)

    11 reads over sockets, 7.1 ms for the walk
    a driver would have sent 8 messages per step instead of 2

**The control is what makes the equality mean something.** Misrouting every read by one
concept changes the walk, so the routing is producing the answer rather than the fixture
being insensitive. The fixture also carries a branch off the start, because out-degree 1
hides a routing fault — which is exactly how decision 108's missing-search capability
stayed hidden.

## The design choice: injection rather than detection

`search` now takes `reader=` on all four public entry points. A caller holding sockets
passes one in.

**Detection was the alternative and it was rejected:** teaching `_reader` about remote
stores would put the network in `search`'s imports, and `search` has no business knowing
what a peer is. With injection, `search` never learns.

**And the resolution lives in one function.** The first version wrote
`reader or _reader(...)` at three call sites, which is three chances for one of them to
stop honouring the injection — and a traversal that quietly reads the local store while
claiming to be distributed produces numbers nobody can catch. It is now `_resolve`, once,
with a mutation on it.

> That consolidation was forced by the mutation harness rather than chosen: the anchor
> appeared three times, so the mutation could not be applied and reported itself stale.
> **A tool refusing to guard a duplicated line is the duplication check working from an
> unexpected direction.**

## What is NOT claimed

**Not the write path.** Reads are point-to-point; `ConceptStore.write` still fans out to
`replicas` nodes, which is a small collective of its own.

**Not churn.** A peer that vanishes mid-walk gives the asker a broken socket rather than a
degraded answer. `distributed.py`'s deadline settles a step on what arrived and is not
wired to this.

**Not `ownership.Ring`.** `RemoteConcepts.owner` is `concept % peers`, which reshuffles
every concept when the peer count changes. The ring is the consistent-hashing mapping that
moves only 1/n, and swapping it in is the obvious next correctness step.

**And not at scale or over a real link.** Four peers on loopback, 7.1 ms for a 3-deep walk.
`tools/cluster_compose.py` can put peers in containers with `tc netem`, and has not been
pointed at this.
