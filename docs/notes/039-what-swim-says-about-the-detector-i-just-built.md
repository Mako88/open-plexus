# 039 — What SWIM says about the failure detector I just built

**IN PLAIN TERMS.** Yesterday I made the system carry on when a machine
disappears, by giving up on it after a set time. There is a well-known design for
doing this, published in 2002, and it does three things differently. Two of those
differences fix problems my version has. The most important one: my version
throws a machine out permanently the first time it fails to answer, and a machine
that was merely slow for two seconds never comes back.

---

## Why this note exists

GOALS §6.2 has listed gossip protocols and SWIM-style failure detectors as
**unread** since the project began, and note 003 named them the highest-value
gap. Decision 124 then found the driver stalling on an undeclared departure, and
decision 126's deadline was built without reading any of it.

**Read now, after building.** That ordering is the wrong way round and is the
third time this project has done it — note 010 (tagging and capture), note 020
(the capacity equation) and this one. The rule that came out of the second is *"search
for prior art at the point the requirements list is written, not after the code
is"*, and it was not followed here.

## What SWIM does

**Source: the paper itself** — Das, Gupta & Motivala, *SWIM*, DSN 2002, from
[Cornell](https://www.cs.cornell.edu/projects/Quicksilver/public_pdfs/SWIM.pdf).

> *An earlier version of this note read the Wikipedia summary and recorded the
> paper as unread, because the fetch returned "unparseable binary". It was not
> unparseable — the console was cp1252 and a single `fi` ligature killed the
> extraction. `pypdf` reads it fine to a UTF-8 file.* **Ten pages were sitting on
> disk behind an encoding error**, and the note carried a rule-1 caveat that a
> two-line script removed. Worth remembering the next time a source is written
> off as unavailable.

1. **Detection runs on its own channel.** Every node pings a random other node
   every `T'`. Liveness is never inferred from the absence of application data.
2. **Indirect probing.** If a direct ping goes unanswered, the prober asks `k`
   other members to ping the same target. This separates *"the link between me
   and it is bad"* from *"it is gone"*, and it spreads the detection cost instead
   of concentrating it.
3. **Suspicion, not immediate ejection.** An unresponsive node is marked
   *suspect*. Others keep trying; if it answers before a timeout, an *alive*
   message clears the mark. This exists specifically to survive transient
   trouble.
4. **Detection is separate from dissemination.** One component decides who is
   dead; another spreads that news, piggybacked on the probes.
5. It claims **strong completeness** — every live node eventually learns of a
   crash.

## The paper describes our bug in its own words

§4.2, on why the Suspicion subprotocol exists:

> a perfectly healthy process suffers a very heavy penalty, by being forced to
> drop out of the group at the very first instance that it is mistakenly
> detected as failed

That is exactly what the first version of `distributed.py`'s `unreachable` set
did, arrived at independently and for the same reason — one missed send was
treated as proof. The paper's causes are ours too: packet loss, a process that
was asleep, or a *slow* process.

## The parameters, which are measured rather than chosen

This is the part a summary cannot give, and it is the part that bears on our
code:

- **Two parameters only**: protocol period `T'`, and `k`, the size of the
  failure-detection subgroup.
- **No synchronised clocks.** The properties hold if `T'` is the *average*
  protocol period across members — which matters here, because a network of
  strangers' machines has no common clock.
- **The timeout before indirect probing is an estimate of the round-trip
  distribution** — the paper names the average or the 99th percentile. Measured,
  not picked.
- **`T'` must be at least three times the round-trip estimate.** A concrete
  ratio, and we have nothing like it.
- **Packets are at most 135 bytes regardless of group size.** That is precisely
  the property amended C1 asks for — bounded bytes per hop, independent of how
  many participants there are — and SWIM achieves it by separating detection
  from dissemination rather than by sending less often.

**`RETRY_AFTER_STEPS = 8` is a guess in the wrong unit.** SWIM's periods are
*time* derived from *measured* round-trip times; ours is a count of steps, and a
step has no fixed duration. Note 003's `d_max` is where a measured bound would
live, and it is still not measured.

## What that says about `distributed.py`

| SWIM | what decision 126 built |
|---|---|
| a dedicated probe channel | **a timeout on the data path** — liveness inferred from a missing vote, which note 003 said cannot work because on a sparse substrate silence is normal |
| indirect probing via `k` peers | none — a slow node and a gone node are indistinguishable |
| suspect, then confirm, with an *alive* message to recover | **none, and this is the real defect.** A send failure puts a node in `unreachable` permanently, for the rest of the run. There is no path back |
| detection distributed across members | **the driver is the sole detector**, which is a coordinator by another name |

**The permanent ejection is the part to fix first.** It is not a missing feature,
it is a wrong behaviour: a node whose network blips for one send is removed for
the remainder of the sequence, and its share of the store goes dark, and nothing
ever re-admits it. SWIM's suspicion mechanism exists for exactly that case.

## What survives

The deadline itself is not refuted. **Settling a step on a deadline removes the
barrier**, which was the point, and SWIM does not address that question at all —
it decides *who is alive*, not *when an answer is due*. The two are complementary:
a detector tells you to stop expecting a vote; a deadline tells you to stop
waiting for one.

Note 003's `d_max` unification also survives and is now better supported: SWIM
separates the *probe* period from the *suspicion* timeout, which is the same
shape as separating the C2 asynchrony bound from the C3 churn timeout. Note 003
collapsed them into one parameter and named the false-positive zone as the cost.
**SWIM's answer to that cost is the suspicion state**, which is precisely what we
do not have.

## What to do, in order

1. ~~**Add a suspicion state**~~ **DONE** (decision 126) — a node that fails a
   send is suspect rather than gone, retried, and cleared by a successful send.
   It fixed a wrong behaviour rather than adding a capability.
2. **Put the retry interval in the right unit.** `RETRY_AFTER_STEPS` counts
   steps; SWIM's periods are time derived from a measured round-trip
   distribution, with `T' ≥ 3 × RTT`. Measuring RTT on the testbed is the
   prerequisite, and it is the same measurement `d_max` has always needed.
3. **Then decide whether the driver should detect at all.** A single detector is
   a coordinator, and C1 exists to forbid those. SWIM's detection is peer-to-peer
   by design, and our driver is an artefact of the benchmark rather than of the
   deployment — the same "scaffolding that became load-bearing" pattern
   `CLAUDE.md` warns about.

## What this does NOT say

That the current code is broken in a way that invalidates a measurement. Every
churn result was taken with **declared** departures, where none of this applies.
It says the mechanism built for undeclared departure is a first approximation
with one wrong behaviour in it, found by reading rather than by a test.
