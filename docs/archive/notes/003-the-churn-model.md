# Note 003 — What is the churn model?

Answers open question 3 in [GOALS.md §8](../../GOALS.md). C3 was a principle
without a definition; this gives it one.

**Unlike notes 001 and 002, part of this rests on primary sources that were
actually read** — see §7 for exactly which, because the distinction matters.

---

## IN PLAIN TERMS

"Machines keep leaving" was a slogan in this project, not a specification.
Nobody had said what leaving *means*, how you notice, or what the system owes
anyone when it happens.

This note goes and looks at what is already known. People have measured how real
home computers join and leave file-sharing networks, and the results are more
encouraging than expected — and one of them is genuinely counterintuitive.

**The counterintuitive one:** most visits are very short, but most machines
*present at any given moment* are long-stayers. Those two facts sound
contradictory and are both true. It changes who we are designing for.

**The useful one:** how long a machine has already been online is a good
predictor of how much longer it will stay. That means stability is something a
machine can work out about its neighbours by itself, without anyone keeping a
central list.

And one architectural result falls out: **the same mechanism that handles slow
messages also handles machines vanishing.** They turn out to be the same problem
with different timeouts.

---

## 1. The question

What does "a machine left" mean concretely — at what granularity, with what
warning, and what is the system's obligation when it happens?

## 2. What the measurements actually say

From Stutzbach & Rejaie, *Understanding Churn in Peer-to-Peer Networks* (IMC
2006), studying Gnutella (unstructured file sharing), Kad (a DHT) and BitTorrent
(content distribution). **Text extracted and read directly.**

**Session lengths are not exponential, and not heavy-tailed either.** The paper
explicitly contradicts prior studies on the second point: fitting the tail gave
tail indices α = 2.5, 2.7 and 2.1 across three BitTorrent datasets, all above the
α < 2 that heavy-tailed requires. Weibull and log-normal fit; exponential and
Pareto do not.

**Weibull shape parameters `k` are well below 1** — k = 0.34, 0.38 and 0.59 for
the three BitTorrent datasets; peer inter-arrival times fit Weibull with
0.53 ≤ k ≤ 0.79. The exponential distribution is the special case k = 1.

> **`k < 1` means a decreasing hazard rate: the longer a machine has been up,
> the *less* likely it is to leave in the next minute.** This is the opposite of
> the memoryless assumption that an exponential model would give, and it is the
> single most useful fact in this note.

**Uptime predicts remaining uptime.** In Gnutella this holds regardless of the
current uptime value — the median peer's remaining uptime is a substantial
fraction of its uptime so far.

**Past session length predicts next session length** in Gnutella and Kad, though
**not** in BitTorrent. The paper attributes the exception to BitTorrent's
different participation pattern (peers leave when a download completes). Our
case resembles the first two: participation is open-ended, not task-completing.

**Availability across consecutive days is strongly correlated** for individual
peers.

**Absolute numbers are short.** A cited passive-monitoring study of Kazaa found
a median session length of **2.4 minutes**, 90th percentile **28.25 minutes** —
and the paper notes passive monitoring *underestimates*. Reported medians across
the literature range from about a minute upward.

## 3. The finding that changes who we design for

The paper states it directly, and it is worth quoting the shape of the argument
because it is easy to get backwards:

> The session length of a *randomly selected session* is likely to be short,
> whereas the uptime of a *randomly selected active peer* is likely to be long.

Both are true simultaneously. A large number of short-lived peers join and leave
at such a high rate that they dominate the *count of sessions*, while
contributing very little presence at any given instant. If you take a snapshot
of who is currently online, you are overwhelmingly sampling the stable ones.

**This is the inspection paradox, and it inverts the design target.** "Median
session is 2.4 minutes" sounds fatal for a system that wants to keep state on
strangers' machines. It is not the relevant number. The relevant number is the
uptime distribution *of the machines present right now*, which is far more
favourable — and which the churn statistics understate precisely because they
count sessions rather than sampling presence.

**Consequence:** the system should be designed for the population it actually
has at any moment, not for the average visitor. It must survive the short
sessions without being *designed around* them.

## 4. The exploitable property, and why it does not violate C1

`k < 1` plus "uptime predicts remaining uptime" means **stability is
predictable**. And critically:

- A machine knows its own uptime. No coordination.
- A machine can observe how long each of its own neighbours has been reachable.
  Still no coordination.

So "prefer to place important state on neighbours that have already proven
stable" is a **purely local heuristic** computed from purely local observation.

**This needs stating explicitly because it looks like a C1 violation and is
not.** A global ranking of all peers by stability would be a violation — it
needs a population sort. Each peer independently preferring its own
longest-lived neighbours needs nothing global, produces no barrier, and requires
no agreement. The two are easy to confuse and only one is forbidden.

## 5. The definition — the churn model for this project

### Granularity

**A whole machine leaves at once**, taking every unit hosted on it. Units do not
fail independently; the machine is the failure domain. Anything finer is a
convenient fiction that will not match reality — when a laptop lid closes,
everything on it goes together.

**Implication for placement:** units that depend closely on each other should
not be spread across many machines without reason, because that multiplies the
number of failure domains any one computation depends on.

### Warning

**Assume none.** Some departures are graceful — the user quits, the process gets
a chance to say goodbye. The model must not depend on it, because power cuts,
crashes, dropped Wi-Fi and closed lids give no notice at all. A graceful exit is
an optimisation, never a guarantee.

### Detection — and a problem specific to this architecture

The obvious detector is "I stopped receiving input from that machine."

**In a sparse event-driven system this is ambiguous, and it is a real trap.**
Silence is *normal*. A unit that fires rarely looks identical to a unit that has
vanished. The predecessor's substrate ran at roughly 2% activity, meaning any
given source is silent the overwhelming majority of the time.

So **absence of data cannot be the liveness signal.** Liveness needs a separate,
explicit channel — a heartbeat, or an explicit "nothing to report for interval
`t`" marker. This is a design requirement that falls directly out of choosing a
sparse event substrate, and it needs to be recorded now because it is exactly
the kind of thing that gets discovered late.

### The architectural result: one parameter serves C2 and C3

Note 002 established that under a predictive objective, a unit holds its
prediction in a buffer until the corresponding input arrives, sized by the delay
bound `d_max`.

**Churn detection falls out of the same buffer.** The unit is already waiting
for an input, with a bound on how long it should wait:

| what happened | how it appears |
|---|---|
| Input arrives within `d_max` | Late but fine. Compare, learn, bit-identical result. |
| Input does not arrive by `d_max` | The source is treated as gone. |

**`d_max` is simultaneously the asynchrony bound and the churn timeout.** C2 and
C3 turn out to be the same mechanism with one parameter, which is a genuinely
good sign for the design — two constraints that were listed separately are
served by one thing.

**The tension this creates, named now:** `d_max` must be large enough to
tolerate intercontinental lag (~150 ms round trip) but small enough that a
departed machine is noticed promptly. Those pull in opposite directions, and the
gap between them is where false positives live — a slow link declared dead. This
is a real cost of the unification, not a free win, and it wants measuring rather
than arguing.

### Obligation

**What the system owes when a machine leaves: nothing global.**

- **No recovery barrier.** Nothing stops and waits.
- **No reconstruction of the lost units.** They are gone; the network is smaller.
- **The obligation is purely local:** each unit that was receiving from the
  departed machine notices, stops expecting that input, and continues.

This is the strongest available reading of C3 and the one that matches the
biological existence proof — losing neurons degrades capability slightly and
continuously, rather than triggering a repair process.

**What is not yet decided** is whether *learned state* on a departed machine
should be replicated somewhere. That is a genuine trade — replication costs
bandwidth (which G4 says is the make-or-break budget) and buys resilience. It
should be answered by measurement after G1, not by preference now.

## 6. What the federated-learning literature does and does not give us

Federated learning studies exactly this population — unreliable heterogeneous
consumer devices — so it is the obvious place to look.

**Its architecture does not transfer.** Federated learning is round-based with a
central server aggregating model updates. That is a global barrier and a central
coordinator: a C1 violation twice over. Its *solutions* to churn are therefore
mostly inapplicable, because they are solutions to "how does the central server
cope", and we have no central server.

**Its taxonomy does transfer, and it is the right one:**

- A **straggler** is late. Its update still arrives and is still wanted.
- A **dropout** left before finishing. Its contribution never comes.

That maps exactly onto §5's table — within `d_max` is a straggler, beyond it is
a dropout. Adopting the distinction is worth it; adopting the machinery is not.

## 7. Provenance — what was actually read

Rule 1 applies to this note as much as to any measurement, and the notes so far
have been accumulating second-hand claims.

- **Read directly:** Stutzbach & Rejaie, *Understanding Churn in Peer-to-Peer
  Networks* (IMC 2006). Text extracted from the PDF; every number in §2 comes
  from that text.
- **Search summaries only, not read:** the federated-learning material in §6, and
  the gossip/epidemic-protocol literature. The §6 claims are about the shape of
  that field and are low-risk, but they are not verified.
- **Not consulted at all:** gossip protocols, SWIM-style failure detectors, and
  CRDTs. §5's detection design would benefit from SWIM in particular, which
  exists precisely to avoid the false positives that §5 names as the cost of
  unifying `d_max`. **This is the most valuable unread thing for this question.**

## 8. What this does not settle

- **Nothing here is measured on this project.** It is a specification informed by
  other people's measurements of other people's systems.
- **The `d_max` tension is unresolved** and is the first thing to measure once
  there is something to measure it on.
- **Whether learned state should be replicated** is deferred to after G1, on
  purpose.
- **The liveness-channel cost is unquantified.** Heartbeats on a sparse substrate
  could be a meaningful fraction of the bandwidth budget, which is G4's
  territory and interacts with open question 4.
