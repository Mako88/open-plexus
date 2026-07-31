# Option record — the rounded TIMESTAMP as the cross-node co-occurrence key

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/buckets.py`: `BucketConfig`, `Observation`, `observations()` and `Join`.
  A node rounds **its own** clock, sends to `owner(bucket)` via `ownership.Ring`, and the
  owner writes pairs into a `grounding.CoOccurrence` and discards the bucket. Knobs for
  width, overlapping windows, clock skew, delivery lateness, flush grace and drop.
- `tests/test_buckets.py` and six mutations in `tools/mutate.py`.
- `experiments/g33_01_does_the_bucket_join_keep_the_signal.py`.

- `openplexus/federated.py` splits the accumulator by `owner(surface)` and counts every
  crossing; `openplexus/bucket_service.py` is ONE node's share and refuses any key it does
  not own; `openplexus/bucket_peer.py` puts a socket around that;
  `openplexus/node_main.py` gains `OPENPLEXUS_MODE=bucket`; `testbed/run.py --mode bucket`
  runs it in containers under `tc netem`, with `tools/bucket_drive.py` driving and
  `.github/workflows/testbed-bucket-identity.yml` re-running it.

**What does NOT exist:** the READ path across containers. The container run drives writes
and reads marginals back; it does not walk. So `g33-03`'s cost of one message per
candidate partner is still an in-process count. Churn is untested here too — no container
is killed mid-run.

---

## What was tried, and what came back

### WHAT A TIME BUCKET IS NOT, because the first write-up of this read as circular

    CONFIG  when    2026-07-31
            source  GOALS.md, the grounding section
            script  none -- a clarification, no measurement
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

**A time bucket is not a concept, and nothing durable is stored in one.** The first
version of this record was read as saying a concept IS a time window, which would make
the mechanism circular — define a concept by a window, then look concepts up by window.
It is not what the design says, and a record that reads that way is a defect in the
record.

Three objects, three lifetimes, and only one of them is permanent:

    PERCEPT       one image code, one word, one sound       forever, stable id
    TIME BUCKET   what showed up at 10:23:15                seconds, then discarded
    CONCEPT       never stored anywhere at all              a pattern, not an object

**The bucket is a rendezvous, not a container.** Two nodes observing different things at
one instant compute the same bucket address independently, send what they saw, and the
bucket's owner notices the coincidence. It immediately writes the pairwise links out to
`owner(percept)` for each percept involved — **and then the bucket is thrown away.**

**Nothing is ever looked up by time.** Lookups are by percept id, which is stable. Time is
used once, at the moment of observation, and never consulted again.

**And why a bucket is needed at all: the two observations are on DIFFERENT MACHINES.** One
machine that saw the image and heard the sound would simply notice, with no mechanism
required. The bucket exists so two machines can discover their observations coincided
without either asking the other, which is the collective C1 forbids. It is a
distributed-systems device, not a theory of meaning.

**One bucket is nearly worthless and that is expected.** Everything present in one second
gets linked — the dog, the sofa and the face alike. The signal is not in any bucket; it is
in the counts accumulated across thousands of them at the percept's owner, where the thing
that co-occurs every time separates from the thing that co-occurred once.

### The problem it answers, and why nothing else here answers it — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

Identity between modalities is learned from **temporal co-occurrence** — a picture, a bark
and the word *dog* arriving together, repeatedly. That requires a node to know that what it
saw and what another node heard **happened at the same time**, and to know it *without
asking*, because asking is the collective C1 forbids.

John's proposal: **round the arrival time to a coarse bucket and derive the address from
that.** Two nodes observing one event compute the same bucket independently.

**This is the same property that makes concept ownership work.** `Ring` gives
*computable locally, agreed globally, no message sent* for concepts; a rounded timestamp
gives it for episodes. Recorded because the parallel is the argument: this is not a new
kind of mechanism, it is the existing one applied to a different key.

### It is the same object as the consolidation tag — noted at the same time

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

The delayed-write design that replaces a write-time gate keeps a **tag** carrying a
timestamp, and consolidates on a later signal meaning *"something around now mattered."*

**A time bucket is that tag's address.** So the join key and the consolidation trigger are
one mechanism rather than two, and the join is what makes the tag reachable *across
machines* instead of only within one. Recorded so the two are not built twice.

### Four objections, raised at design time

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass, no measurement of any of these
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

**Boundaries hurt more than skew.** Two events a millisecond apart round to different
buckets when they straddle an edge. The standard answer is overlapping windows — hash to a
bucket and its neighbours — at a constant-factor cost in writes.

**The asynchrony bound fights the bucket size.** `d_max` is the C2 delay this project has
already accepted as normal. A bucket comparable to it puts a late-arriving input in the
wrong bucket routinely, so the bucket must be comfortably wider — which is coarser, and
binds more unrelated things together.

**A bucket is a hot spot.** Every input at one instant routes to one node. Moving
ownership to entities was worth a large fall in busiest-peer share
([concept-partitioning.md](concept-partitioning.md) holds the figures); time buckets are
the opposite move and much worse. Splitting by `(time, modality)` spreads the load and
destroys the join, which is the point of it.

**One episode is nearly worthless.** A dog, a sofa and a face all co-occur with the word.
Only what is constant across many episodes separates them — and if episodes are scattered
by time across nodes, gathering "every dog episode" is the global operation C1 forbids.

### The resolution to the fourth, which changed the design — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

> **Time is the TRANSIENT join. The percept's owner is the DURABLE accumulator.**

The bucket exists only long enough to observe one co-occurrence. The link is then written
to `owner(percept_id)`, which accumulates over that percept's whole lifetime — so the node
owning an image id ends up holding *everything that has ever co-occurred with it*.

**Cross-situational learning then falls out as local counting at a fixed address.** The
sofa fades because it appeared once; the word persists because it appears every time. No
gather, no global step, and the hot spot is transient rather than permanent because nothing
durable is stored at the time key.

It is the fast-store-and-durable-store shape the project already has, with **time
addressing the fast tier and percept id addressing the slow one**.

### What would refute it, registered before anything is built

    CONFIG  when    2026-07-30
            source  GOALS.md, the grounding section
            script  none -- not yet written
            task    proposed: a symbol stream with a persistent distractor
            model   n/a
            knobs   none
            scale   n/a

**Introduce a concept alongside a distractor that is present every single time, and see
whether the distractor is ever pruned.**

If it never fades — because co-occurrence alone cannot distinguish *"always there"* from
*"is the thing"* — then counting is insufficient and the missing ingredient is intervention.
That is the hypothesis [`GOALS.md`](../../GOALS.md) records as arriving independently from
the memory side of the project on the same day.

**This needs no perception layer.** It is a symbol stream with a designed co-occurrence
structure, which makes it the cheapest available test of the whole mechanism.

### The falsifier was run, and the distractor IS pruned — but not by counting — `g32-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt
            script  experiments/g32_01_can_counting_tell_the_distractor.py
            task    occasions, 64 concepts, 3 surfaces, presence 0.7, noise 3
            model   none -- counting only; NO bucket and NO join were built
            knobs   statistic, zipf, distractors, shuffled control; k 2; 3 seeds
            scale   8,000 occasions per stream

A distractor present on every occasion costs raw counting **0.3044** of f1 and costs a
chance-corrected statistic **0.0000**. So the distractor is pruned, the answer is not
*"counting is insufficient and the missing ingredient is intervention"*, and the
intervention hypothesis is neither supported nor refuted — it was not tested.

**What was tested is the accumulator, not the join.** No bucket exists; the whole stream
was observed in one process. That is deliberate: distribution cannot add information to a
count, so a failure here would have settled the design, while a pass settles nothing about
C1. Details and the asymmetry are in `openplexus/grounding.py`'s docstring.

**And the falsifier's own metric was the wrong one.** `captured` — the share of surfaces
whose class contains the distractor — moved by **0.0174** where f1 moved by **0.3044**,
because mutuality caps a distractor's degree and its harm is *displacement* rather than
joining. Recorded in [co-occurrence-statistic.md](co-occurrence-statistic.md), which holds
every figure from both runs.

### The join was BUILT, and it costs nothing across a wide envelope — `g33-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g33-01-does-the-bucket-join-keep-the-signal.txt
            script  experiments/g33_01_does_the_bucket_join_keep_the_signal.py
            task    occasions, 64 concepts, 8,000 occasions, tempo 100
            model   openplexus/buckets.py -- ONE PROCESS, no sockets, no containers
            knobs   width, moment duration, skew, spread, lateness, grace; k 2
            scale   3 observers, 8 nodes, 3 seeds

With clocks agreed and no lateness, the join reproduces the single-process ceiling
**exactly** — `1.0000` at every swept width from 5 to 500, at every moment duration.

**Two of this record's four stated objections do not survive measurement.**

*"Boundaries hurt more than skew"* and the overlapping-window answer to them: overlapping
windows work, but **widening the bucket does the same job at one fifth of the messages**.
At skew 50, `spread` 2 lifts width 50 from `0.6420` to `1.0000` at **5.0** messages per
observation, while width 200 reaches `1.0000` at **1.0**. The proposed fix is dominated by
the simpler knob.

*"The asynchrony bound fights the bucket size"*: it does not, in the direction feared. Skew
is free once the bucket is roughly four times it — at skew 50, width 200 loses nothing.

**And moments do not have to be separated in time**, which this record assumed. At a moment
duration equal to the gap between moments, every width from 20 up still reaches `1.0000`.

*"One episode is nearly worthless"* stands and is the reason it works: `g32-02` measured
the accumulation needed at about 16 occasions per concept.

### What the wide-window tolerance actually is — `g33-01` probe A

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g33-01-does-the-bucket-join-keep-the-signal.txt
            script  experiments/g33_01_does_the_bucket_join_keep_the_signal.py
            task    occasions, as above, instantaneous moments
            model   openplexus/buckets.py
            knobs   width from 500 to 50,000 at tempo 100
            scale   1 seed

`1.0000` at five and ten moments merged per bucket, `0.9836` at twenty, `0.5124` at fifty
and `0.3389` at a hundred — below the 0.5 floor, which is the shuffled control's regime.

**The sweep's own grid stopped at five and did not contain this failure.** It was found by
probing past the grid afterwards.

### Lateness is a deadline, and the join survives losing most of its traffic — `g33-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g33-01-does-the-bucket-join-keep-the-signal.txt
            script  experiments/g33_01_does_the_bucket_join_keep_the_signal.py
            task    occasions, as above
            model   openplexus/buckets.py, width 50, spread 0, skew 0
            knobs   lateness, grace, drop
            scale   3 seeds

Grace at least as large as the worst delay loses nothing and scores `1.0000`. The same
delay with no grace loses a **0.7463** share of observations at lateness 200 and still
scores `0.8647`.

Dropping observations outright: `1.0000` at drop `0.50`, `0.9149` at `0.75`, `0.4496` at
`0.90`.

**Loss is NOT equivalent to a shorter stream**, which was tested because it looked like it
should be. It removes individual observations rather than whole occasions, so a pair needs
both members and the damage is quadratic — at 25 surviving occasions per concept this
scores `0.5692` where `g32-02`'s clean stream at the same count scores `0.9503`.

### It runs in real containers and agrees exactly, impaired and clean — `g35-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g35-01-the-grounding-store-in-real-containers.txt
            script  testbed/run.py --mode bucket, driver tools/bucket_drive.py
            task    occasions, 6 concepts, 60 occasions, 19 surfaces
            model   bucket_service + bucket_peer, one container per node, no model layer
            knobs   nodes 4; tc netem delay 40ms jitter 10ms, or clean
            scale   Docker 29.6.1, 305 observations, 60 flushes

**Every `g32` and `g33` number was taken with crossings COUNTED rather than sent.** This
sends them. Four containers, a real Docker network, a driver that owns nothing and reads
each count back from the peer holding it.

`agrees_with_one_process: true`, **0 mismatches** across all 19 surfaces, on a clean link
and again at 40 ms delay with 10 ms jitter. So the join, the sharded accumulator and the
ownership rule survive a real process and container boundary unchanged.

**The incidental finding is a 96x bill**: **2.66s** clean against **255.68s** impaired.
That is one-connection-per-message, which `bucket_peer.py` names as a deliberate
simplification — each message pays a setup round trip and a request round trip. It **must
not be quoted as a latency for this architecture**; `g24-01`'s 161 ms a round remains the
figure for a walk over held connections.

### The reply had to precede the writes, and only containers showed it — `g35-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g35-01-the-grounding-store-in-real-containers.txt
            script  none -- found while wiring OPENPLEXUS_MODE=bucket
            task    n/a
            model   bucket_peer
            knobs   none
            scale   3 OS processes

`BucketPeer` replied to a message and forwarded its outbox afterwards, so a `FLUSH`
returned while its `NOTE` and `LINK` messages were still in flight. **In one process that
raced fast enough to pass every test**; across three OS processes a caller reading a count
straight after a flush read one that had not arrived.

Forwarding now precedes the reply, so a reply means the work has landed. The regression
test stalls a forward deliberately rather than relying on a race, because a loopback race
is not a test of an ordering.

### A departure is AMPLIFIED by the modality count — `g35-02`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g35-02-what-a-departure-costs-the-grounding-store.txt
            script  experiments/g35_02_what_a_departure_costs_the_grounding_store.py
            task    occasions, 32 concepts, 4,000 occasions, no distractor loss modelling
            model   Federation, 8 nodes, conditional, derived bound, NOTHING replicated
            knobs   surfaces per concept 2/3/5, nodes lost 0/1/2/4; 3 seeds
            scale   in-process; ownership real, transport not

**The grounding store has no replicas**, unlike `partitioned.ConceptStore`, so a
departed node's rows are gone permanently and nothing falls through to a survivor.

Losing one node of eight removes about **0.094** of surfaces and damages **0.260** of
concepts at 3 surfaces each — a concept is hit if ANY of its surfaces was there, and the
measured share matches `1 - (1 - gone)^surfaces` closely. At 5 surfaces it is **0.479**.
So the surfaces of one concept spread across the ring rather than landing together, which
is what makes the exponent apply.

**Nothing collapses.** `largest` never exceeds **0.0462**, and losing half the network
still returns f1 near **0.40** rather than failing.

**But almost none of them are LOST, and multimodality is why.** A floor-free metric —
the share of surviving surfaces whose class still holds at least one true partner — goes
**0.8901, 0.9923, 1.0000** at 2, 3 and 5 surfaces with one node of eight gone, and
**0.5522, 0.8160, 0.9596** with HALF the network gone. `largest` never exceeds 0.0462, so
none of those are collapses.

A surface needs only ONE partner to stay reachable, so five surfaces give it four chances
where two give it one. **With nothing replicated, the surfaces themselves are the copies** —
which answers, for this store, the question `partitioned.ConceptStore.lose` leaves open:
*"which is preferable is a measurement, not a preference."*

Replication is therefore an improvement rather than a prerequisite, and the two-surface row
is where it would pay.

**The f1 column cannot be compared across that axis**, because the metric's floor moves
with the class size — 0.6667, 0.5000, 0.3333 for a concept recovered alone. The first run
read it as flat and could not settle anything; the floor-free metric is what settled it.
