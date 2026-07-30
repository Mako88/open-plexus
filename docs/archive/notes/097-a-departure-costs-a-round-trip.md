097 — A departure costs a round trip, and a control weakened for the third time
==============================================================================

**Status:** built, with two mutations. It closes note 093's churn gap — the last thing C3
names on the peer read path.

---

## IN PLAIN TERMS

A peer vanishing used to break the read that was asking it. **Now the read asks the next
peer holding the same fact and gets the answer.** The data was already there: writes go to
several peers precisely so a departure moves nothing.

**And when every holder is gone, the read returns zeros AND counts it.** Zeros decode to
whichever token the readout happens to prefer, so an uncounted absence is indistinguishable
from a confident wrong answer. The count is what makes it honest.

---

## What changed

`RemoteConcepts.read` walks `Ring.holders(concept, replicas)` instead of asking only the
owner, dropping a dead peer's socket so a later read reconnects rather than reusing it.
`Ring.holders` already explains why this needs no data movement:

> *"a departing node's concepts pass to the successors that already hold them, so **nothing
> has to move on a failure** — the remaining replicas are already there and already warm."*

Two tests: kill the owner and the answer survives; kill **every** holder and the absence is
counted. Two mutations, both caught — one asking only the owner, one dropping the count.
**191 mutations, 1292 tests.**

## The control weakened again, and this is the third time

`test_MISROUTING_breaks_it` asks a read of somewhere that should not have the answer. Its
definition of "somewhere else" has now been wrong three times, each time for a different
reason, and each time because the *system* got better:

    1. `concept + 1`, while routing was `concept % peers`         correct then
    2. the ring made adjacent concepts share a peer, so `+1`      note 095
       often was not a misroute at all -- 3 of 24 matched
    3. a read now tries every HOLDER, so a different OWNER is     this note
       not enough: the holder sets overlap and the "misroute"
       reaches the answer anyway

It now requires a concept whose holder set is **disjoint** from the real one, and the fixture
carries `NODES = 5, REPLICAS = 2` so such a concept exists — at `replicas == peers` every
peer holds everything, no misroute is possible, and the control would be vacuous rather than
strict.

> **A control has to exclude every route to the answer, not just the first one.** Each time
> the read path gained a fallback, the control silently gained a way to succeed. That is the
> general shape: **making a system more robust makes its negative controls weaker**, and
> nothing warns you.

## What is NOT claimed

**Not that writes are distributed.** The fixture fans a write out to every holder because
`ConceptStore.write` does, but no peer-to-peer *write* path exists — `openplexus/peer.py`
serves reads only.

**Not that a departure is detected.** A read discovers a dead peer by failing to reach it,
every time, because nothing tracks liveness. So the first read after a departure pays the
timeout, and `distributed.py`'s deadline machinery — which settles a step on what arrived —
is still not wired to this.

**Not rebalanced.** When a peer leaves, its concepts are served by replicas that already hold
them, and nothing re-replicates to restore the count. After two departures a concept with
`replicas = 2` has one holder left and no mechanism notices.

**And `survival()` is unused.** `ConceptStore.survival` estimates how often every holder is
gone; the real quantity here is `absent / reads` and nothing sweeps it against a churn rate.

---

## CI FAILURE, and it was in this note's own mechanism

**`test_losing_EVERY_holder_is_a_counted_absence` failed in CI and passed here**, taking the
suite and all six mutation shards with it. The read returned a real value where zeros were
expected: **the peer kept serving after `close()`.**

`close()` closed the listener and returned. **Closing a socket that another thread is blocked
in `accept` on is not portable** — Windows failed the accept and the peer stopped; Linux kept
it alive. So a simulated departure was a race whose outcome depended on which machine ran it.

**The fix is in the production code, not the test.** The listener and each accepted connection
carry a 0.1 s timeout so both loops re-check `_stop`, and `close()` joins the thread. Measured:
**0.106 s with a live connection open**, thread confirmed dead, and the next read returns
counted zeros.

> **A departure has to be a fact by the time the next read happens.** An asynchronous close
> makes every churn test a race, and a race that this machine wins is a test that only reports
> on this machine.

### Two of my own mistakes on the way, both caught by the tools

**The fingerprint refused my new tests, twice** — I built peers with `peers=NODES` while giving
the client a one-peer dict, and the peer count is in the fingerprint. Note 096's handshake
working on its author.

**And the first version of the close test was vacuous.** `close` sets `_thread = None`, so
`assertFalse(peer._thread.is_alive())` passes trivially — and it passed with the join deleted
entirely, which the mutation caught. The test now holds the thread reference before closing.
**192 mutations.**
