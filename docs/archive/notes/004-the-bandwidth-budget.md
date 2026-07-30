# Note 004 — What crosses the network, and does it fit?

Answers open question 4 in [GOALS.md §8](../../GOALS.md). G4 turns on this
number.

**This note is arithmetic, not argument.** Every input is named in §2 and the
calculation is reproducible from them. The inputs are assumptions; the
arithmetic on top of them is not.

---

## IN PLAIN TERMS

Our machines have to send messages to each other constantly. Home internet
connections are slow, especially for *sending*. Does the traffic fit?

The finding is that **the question was pointed at the wrong number.** Everyone
including us has been asking "what fraction of the connections go over the
network?" — and at full scale that fraction is essentially 100%, no matter what
we do, so it is not something we get to choose.

The number we *do* control is different: **how many separate machines each part
has to send to.** That turns out to need to be around ten or fewer, which is a
demanding but not impossible target — and it tells us something concrete about
how the network has to be shaped.

Two nice results fall out. The slow-message tolerance we already needed turns
out to also make the bandwidth affordable, by letting us bundle messages. And
the heartbeat cost we were worried about in [note 003](003-the-churn-model.md)
is negligible.

---

## 1. The question

What fraction of connections crosses the network, and is that a free parameter
or forced by the design?

## 2. The model and its inputs

| symbol | meaning | value used | status |
|---|---|---|---|
| `N` | units in the network | 33 × 10⁶ | inherited scale target |
| `M` | machines | 31,000 | from a 16 bytes/connection memory bound |
| `r` | events per unit per second | 20 | 2% activity at 1 kHz |
| `f` | fan-out — targets per unit | 1,000 | assumption |
| `b` | bytes per event on the wire | 16 | assumption |
| `HDR` | per-packet framing overhead | 42 bytes | UDP/IPv4 + Ethernet |

**All six are assumptions, and `N`, `r` and `f` are inherited from an
architecture this project is not building.** They are used because an order of
magnitude is what is wanted here, and because the *shape* of the result — which
term dominates — is robust to them being wrong by a factor of a few. Any
conclusion that is not robust in that way is flagged where it appears.

That gives **1,065 units per machine**, emitting **21,290 events/second per
machine**.

## 3. The fraction crossing the network is not a free parameter

Under uniform placement, the probability that a given target of a given unit
happens to live on the same machine is `(N/M)/N = 1/M`:

```
P(target is local) = 3.23 × 10⁻⁵      →      p_remote = 0.999968
```

**Essentially every connection crosses the network.** This is not a tunable; it
is a direct consequence of each machine holding 1/31,000th of the network. The
predecessor's framing — "at 1% of synapses crossing the network it fits, at 10%
it does not" — describes a regime that **does not exist at scale under uniform
placement.** The parameter it named was not free.

**So the question as posed has a discouraging answer, and it is the wrong
question.** The next section has the right one.

## 4. The quantity that actually sets the bill

**You do not send one packet per connection. You send one packet per
destination *machine*.** If a unit's event is needed by sixty targets that all
live on the same remote machine, that is one packet, which the receiving machine
fans out locally.

So the binding quantity is:

> **`D` — the number of *distinct machines* holding at least one target of an
> emitting unit.**

Outbound traffic per machine is `(N/M) × r × D × b`.

**Under uniform placement, `D` does not save you anything.** With `f` = 1,000
targets scattered over `M` = 31,000 machines, the expected number of distinct
destinations is **984** — almost all of them land on different machines. The
aggregation is available in principle and does nothing in practice.

**`D` is free. `p` is not.** `D` is set entirely by *placement*: whether a
unit's targets are concentrated on a few machines or scattered across many.
That is a design decision, and it is the design decision this whole note is
about.

## 5. What the budget allows

Home connections are **asymmetric** — download is typically 5–20× faster than
upload — so **upload is the binding constraint** and every figure below is
outbound.

| `D` | MB/s out | Mbps out | verdict |
|---:|---:|---:|---|
| 1 | 0.34 | 2.7 | fits a poor upload |
| 3 | 1.02 | 8.2 | fits a poor upload |
| 10 | 3.41 | 27.3 | fits a good upload |
| 30 | 10.22 | 81.8 | needs fibre or business service |
| 100 | 34.06 | 272.5 | needs fibre or business service |
| 1000 | 340.65 | 2725.2 | impossible |

Solving directly:

- **10 Mbps upload** (a poor but common connection) → **`D` ≤ 3.7**
- **40 Mbps upload** (a good consumer connection) → **`D` ≤ 14.7**

> **The design constraint is `D` in the single digits to low tens.** Everything
> below follows from that one line.

## 6. What that demands of the architecture

`D ≤ 15` with `f` = 1,000 means each unit's thousand targets must live on **at
most about fifteen machines** — roughly seventy targets per destination.

That cannot happen by accident. It requires the connectivity itself to have
**community structure** — units that talk mostly to a limited set of other units
— *and* a placement that respects that structure.

> **G4 therefore forces a property of the model that was not previously
> required: connectivity must be local-dominant, with long-range connections
> sparse.** This is now an architectural constraint derived from a bandwidth
> budget, not a preference and not a biological analogy.

**Under local-dominant connectivity the §3 result softens**, and this is worth
being explicit about because §3 read as bad news. If most of a unit's targets
are on its own machine, then `p` is small *and* `D` is small — but both are
outcomes of the same decision. **`p` was never an independent parameter. It is a
consequence of placement locality, which is the actual free variable.** That is
the answer to the question as asked.

**The cost, named rather than hidden:** concentrating connections weakens
long-range mixing. A network of tightly-clustered islands with thin links
between them may simply be less capable than a well-mixed one. **This is a real
trade and it is currently unmeasured.** It becomes measurable as soon as there
is something to measure, and it should be swept early, because the entire
distribution story rests on the answer being acceptable.

## 7. Batching is mandatory, and C2 pays for it

At 16 bytes per event and 42 bytes of framing, **an unbatched packet is 72%
overhead.** Batching is not an optimisation here; without it the header traffic
alone would dominate.

Events accumulate over a window and go out as one packet per destination per
window:

| window | packets/s | payload MB/s | header MB/s | overhead |
|---:|---:|---:|---:|---:|
| 1 ms | 10,000 | 3.41 | 0.420 | 11.0% |
| 5 ms | 2,000 | 3.41 | 0.084 | 2.4% |
| 20 ms | 500 | 3.41 | 0.021 | 0.6% |
| 50 ms | 200 | 3.41 | 0.008 | 0.2% |
| 150 ms | 67 | 3.41 | 0.003 | 0.1% |

*(at `D` = 10; payload is worst-case, assuming every event goes to every
destination)*

**The window is affordable only because C2 already tolerates delay.** A design
that needed millisecond delivery would pay 11% in headers and could not batch
its way out; the 150 ms budget makes framing overhead vanish entirely.

> **C2's delay tolerance pays for itself twice.** It is what lets the system
> span the internet at all, and it is what makes the bandwidth affordable. Two
> independent benefits from one property, which is a strong sign the constraint
> is cutting along a real joint rather than being a concession.

## 8. Heartbeats are affordable — note 003's worry resolved

[Note 003](003-the-churn-model.md) established that a sparse event substrate
cannot use absence-of-data as a liveness signal, and left the cost of an
explicit heartbeat channel unquantified as a worry against the G4 budget.

Against a 1.25 MB/s (10 Mbps) budget:

| neighbours | rate | cost | share of budget |
|---:|---:|---:|---:|
| 10 | 1 Hz | 0.58 kB/s | 0.05% |
| 10 | 10 Hz | 5.80 kB/s | 0.46% |
| 100 | 1 Hz | 5.80 kB/s | 0.46% |
| 100 | 10 Hz | 58.0 kB/s | 4.64% |

**Negligible in every configuration**, and heartbeats are per *machine pair*
rather than per unit, so the cost does not scale with network size. The worry
was reasonable and the arithmetic dismisses it.

## 9. Bandwidth and churn agree, independently

Note 003 concluded that units depending closely on each other should not be
spread across many machines, because that multiplies the failure domains any one
computation depends on.

This note concludes that a unit's targets must be concentrated on few machines,
because that is what the upload budget allows.

**Two constraints derived from unrelated considerations — resilience and
bandwidth — point at the same design property.** Neither was chosen to serve the
other. That is weak evidence the property is real rather than convenient, and it
is recorded as weak evidence rather than as confirmation.

## 10. What this does not settle

- **The inputs are assumptions**, three of them inherited from an architecture
  not being built. If `f` is 100 rather than 1,000, or the event encoding is 4
  bytes rather than 16, the absolute figures move. **The conclusion that `D` is
  the binding quantity and `p` is not does not move**, because it follows from
  `N/M` being small rather than from any of the disputed values.
- **The cost of clustering is unmeasured** and is the most important open item
  here (§6).
- **Inbound traffic is not modelled.** It is assumed to be easier because home
  connections favour download, but this has not been checked and asymmetric
  routing could make it interesting.
- **Nothing is measured on this project.** No code exists.
