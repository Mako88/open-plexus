# Where this is going

**The only doc, capped at 2,800 words by `DocsTests`. To add something, retire
something.** Its predecessors grew past the point of being loaded and kept being
cited anyway, which is the failure this exists to prevent.

**What every piece does lives in the XML comments beside the code**, and the
compiler enforces that they refer to things that exist
(`GenerateDocumentationFile`; CS1572/1573/1574 are build errors). That cannot go
stale.

**This file is forward-facing and records no results.** A measurement is
something that happened; a plan is something to do. `DocsTests` fails the build
when a measured quantity appears here — a score, a spread, a separation, a tick
against a closed fork. Findings live in the commit that produced them, in the
comment beside the mechanism they are about, and in the test that asserts them.
Deleted docs are in git.

---

## The goal

A system that **understands** rather than performs — one that can be asked *what
would the world look like if I did X*, which a sequence model cannot be. Learning
is a co-occurrence count; everything else is plumbing around it.

**Three constraints shape every decision.** C1: no node reads another's data.
C2: messages are late, jittered, out of order. C4: no episode boundary, so
nothing may depend on training then testing.

---

## What is standing

**Four worlds sharing no world logic, over one `Fabric`.** Snake, where a chain
can cause a move; senses, where sight and touch never co-occur so the answer can
only be composed; binding, built so this architecture provably could not answer
it and since lifted; and composition, where the answer was never observed at all.

**An occasion is a SET of co-occurring codes**, so *red ball beside blue box* and
*blue ball beside red box* were the same input. That ceiling was
**representational, not a matter of scale** — and `Occasion.Groups` plus a
per-scene index is what lifts it.

**The graphs are tiny.** Nothing here has been run at a size where its claims
could break. Hold them loosely — and the size dial that exists has already found
one thing that was wrong.

### ONE WEIGHT DOING TWO JOBS IS THIS DESIGN'S RECURRING FAULT

**It has bitten twice.** An edge weight both RANKS a partner and PRICES the hop
to it, and each attempt to improve one silently wrecked the other.
`Pricing.Sender` moves the ranking while meaning to move the price. `Doubt` —
shrinkage, so a partner seen once cannot claim the strongest edge in the graph on
one accident — destroys the senses world applied to both, and repairs a real
defect for free applied to the score alone.

**So the general move is: find the number serving two masters and split it.**
`Accumulate` is the next candidate. **And the standing rule — John, 2026-08-03 — is
that a dial wanting different values in different worlds is the same fault**:
prefer splitting it, or fusing the arms, over sweeping it. `DialTests` records
which channel each dial may move and fails when one moves the other.

---

## NEXT

**The binding world is the scoreboard. Do not build a new world to test a change
to an old one — run that one.**

### 1. Composition over bindings — built and answered

**The honest test of the index**, because the world it lifted is *memorisable*.
`Composed` documents its own shape. A memoriser scores exactly zero here.

**Two things were needed, and both were architecture rather than tuning.**

`Accumulate.Agreement` — **rank a candidate by how many DISTINCT ORIGINS reached
it**, strength only to break a tie. That is what a conjunctive question asks and
`Sum` cannot say it: strength varies far more between routes than the count of
origins does, so one strong single-origin route outranks two weak agreeing ones.
Nothing new travels for it — a chain already begins at its origin. **Provably
inert on single-origin questions**, which is what makes the lift attributable.

`Refer.Narrowed` — **read back whichever index the graph itself ranked first, and
ask that one.** The evidence selecting an index lives in the origin's tally FOR
that index and never travels through it, because two routes arriving at a node
fire it twice and fan out independently. Two broadcasts, no index supplied.

**What is left is the world's own ambiguity, not the walk's.** Two scenes sharing
both referring values are genuinely indistinguishable by a conjunction, and how
often that happens goes as `scenes / values²`. Widen the alphabet and the score
follows it closely.

### 1a. RANKING BELONGS TO THE QUESTION, NOT TO THE MACHINE — decide this

**Agreement is not universally right.** Inert on senses, harmful on binding even
after being told which origins are one attribute said several ways — a deep walk
reaches the echo *through* the index, so both candidates end up agreed by both
groups and a weakly-reached-by-both outranks a strongly-reached-by-one.

**Those are different KINDS of question.** Composition asks a conjunction: the
thing meant is the one every origin reaches. Binding points with an index and
supplies a colour for context, where the origins are not equals. **The asker
knows which it is asking and today cannot say.** So move ranking onto the
question, beside the grouping that already travels with it — or better, fuse the
rankings by position rather than choosing between them, which is what rank
fusion does with scores that are not comparable.

- **Merging routes AT A NODE is the version `Narrowed` could not do.** Reading
  the index back costs a round trip and puts the referent in the machine's
  hands; a node combining concurrent routes of one broadcast would keep it in the
  graph. It needs a wait, which C2 makes expensive — but it is the honest form.
- **`Accumulate.Max` was re-tried here, where its revival condition pointed, and
  is worse.** Stamina is nearly an exponent on cost and buys nothing.

**Three things must be switched on and none is the default** —
`Accumulate.Agreement`, `Pricing.Sender`, and `Fleeting` on any world that mints
an index. Each type documents why. **Promoting the last two is a live decision
and both look overdue**; `Agreement` cannot be promoted until ranking moves onto
the question.

**Vector-symbolic binding** (Plate, Kanerva) stays parked: the similarity
gradient named below, at the cost of opaque codes. Not needed yet.

### 2. Predictive coding — only surprise propagates

Rao & Ballard, Friston. An expected onset is silent. **Traffic collapses**; the
system gets an **internal error signal**, which it has never had — error is
measured by the harness from outside; and it **unblocks drives**, which need
uncertainty felt rather than scored. **Arguably the biggest single gap.**

### 3. Chunking — MDL

Fork 21 mints edges; it should mint **nodes**. When a set of codes recurs, create
a code standing for the set. Threshold is minimum description length, not a
constant. **This is what lets the alphabet GROW** — today the quantiser fixes it
forever. Fork 21's trade is the **utility problem** from explanation-based
learning (Minton, SOAR): utility belongs per chunk, not as one global `Weight`.

### 4. Homeostatic drives — Ashby

Keep internal variables in bounds and behaviour becomes goal-directed **with no
reward function** — which matters, because survival proved gameable by circling.
**Homeostasis has no episode boundary**, which fits C4.

**All four could land and still not be enough.** The confident claim is narrower:
without structure, an internal error signal, a growing alphabet and a reason to
act, no amount of scaling gets there.

---

## TO BUILD — a ticked box means the type exists, and a test checks

- [x] `Binding` — the world that measured the ceiling, and then lifted it
- [x] `Seeds` — decorrelated seeding for every sweep
- [x] `Fabric` — the bus/ring/clusters the three worlds used to each own
- [x] `Measurement` — the range checks the three worlds used to each own
- [x] `Composed` — the world of step 1, built and characterised, not yet lifted
- [ ] `Surprise` — the local prediction error of step 2
- [ ] `Chunk` — the minted node of step 3
- [ ] `Drives` — the bounded internal variables of step 4

---

## LATER — nothing is blocked on these

- **Fork 1 is smaller than it looks.** A count that only increments is a
  **G-Counter, a CRDT** — it converges under reordering and loss with no
  coordination. The counts need no protocol; only the join does.
- **The one-way window.** Built, null on snake, **never run where it worked.**
- **The absolute message cost is what step 2 attacks.** Nothing else should be
  optimised until it has been tried.
- **Cold storage, once a row can be bounded at all.** A count that only
  increments is a CRDT, and **paging a node out to disk keeps that** — the count
  does not decrease, it stops being resident. **Decay does not**, so eviction
  must key on "not touched since", never on eroding the count itself.
- **The knob pass, last.** A dial swept before the structural work measures a
  system about to change underneath it.

### The wire, when the remote half lands — John, 2026-08-03

Only the local half of `HybridBus` exists, so none of this is built.

- **Coalesce a settling wave into one send.** `Cluster.DeliverAsync` already
  regroups by cluster. **Hold remote envelopes until local traffic drains, then
  one datagram per destination.** `WhenIdle()` is the trigger and is C1-legal.
  **Not a pure barrier**: flush on idle *or* size *or* time, or a busy machine
  never sends.
- **Bits, not JSON.** Addresses and modalities intern to small ints, a code is a
  varint, the `double`s are almost certainly `float`s, and **a sixth of a packed
  message is the `Guid` broadcast id** — shorten it per connection.
- **`Chain` is what costs** — cycle check and explanation in one field, free
  locally and not on a wire. **Split them:** a fixed-size approximate-membership
  filter for the hop, full chain rebuilt at the origin. A false positive is a
  route wrongly refusing a partner — the loss C2 admits.
- **Voting multiplies the wrong half — John, 2026-08-03.** `votes: n` is n
  independent broadcasts, so it pays n floods to insure against loss on the way
  back. **One thought, redundant reports**: the flood is determined by the graph,
  and what C2 loses is the return path.
- **UDP is the matching transport, not a compromise.** C2 already assumes loss,
  and **TCP's head-of-line blocking would stall every thought behind one lost
  packet.** QUIC's unreliable datagram extension (RFC 9221) is the shape.

---

## DO NOT RE-TRY

**Three columns, one line each, enforced by a test.** The third matters most: a
refutation is conditional on its configuration, so a row without a revival
condition is a superstition. The commit named in git holds the numbers.

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial message growth where inverse cost is polynomial | A bound not relying on positive cost at weight 1.0 |
| `Refuel` | Nothing is paid back, so it did nothing | Anything that returns budget to a route |
| Sender-*weighing*, `IMarginals` | A C1 violation, and behaviour was identical without it | Never. **But `Message.Seen` — the sender's OWN marginal — is legal and is built** |
| Absolute actions, unrotated view | One move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins: it lives longest and eats least | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound — the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: it wrote most where it helped least | A signal that discriminates; `Thwarted` does |
| A deeper walk for prediction | Monotonically worse — without edge kinds, deeper reaches more and ranks worse | **Edge kinds**, and that refutation reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, both explanations refuted, `Max` re-tried where its revival condition pointed and worse there too. **Both now DELETED** | Lift in the **cost**, which `Doubt` is the nearest thing to |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | A similarity gradient under the ranking |
| `Window` span on snake | Null there, at every seed count tried | **Revived — never run on a senses graph, which is what it was built for** |
| `includeEmpty: true` | Ruinous under `Best` pricing | **Revived — inverse cost removed the reason; no clear winner since** |

---

## TRAPS

**Closed in code — named so nobody reintroduces them.** Consecutive integer seeds
are not independent (`Seeds.Apart`, and `Sweep` mixes the counter).
`Measured.Separation` returned zero where repeated measurement found no spread at
all (infinity now). `WhenQuiet()` was not a finish signal (renamed `WhenIdle()`).
Questions were read before their walk had finished, which made every number taken
under one load incomparable with any other (fork 22; the suite is serial and
`Unsettled` is asserted). **Voting survives that fix and buys nothing in one
process** — kept, because a real network loses reports.

**Live:**

- **A dial swept at one data volume may be measuring the volume.** The stamina
  plateau reversed between short runs and long ones. **Sweep at two run lengths.**
- **Short runs on the binding world score above chance for RECENCY ALONE**, and
  it decays with data. **Nothing under a few hundred scenes measures binding
  there.**
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** `ThinkAsync`'s stamina was, and survived the whole suite and three
  measurements. **Every run reports `Complaints`; read them.**
- **A small sample can look like a mechanism.** One seed with a collapsing echo
  read as a discovery and was three questions.
- **A MEAN OVER A POPULATION THE PROBLEM CREATED CANNOT SEE THE PROBLEM.** Mean
  fan-out stayed flat on the world whose rows grew without bound, because the
  growth mints tiny nodes that hold the average down. **Read `Widest`.**
- **Copies drift where nothing fails.** Three worlds each grew their own settle
  loop, complaint list and vote tally, and a difference between them would move a
  headline without failing a test. `DuplicationTests` is the budget now.

---

## OPEN DEFECTS

**A mutation survives.** Removing the action from `SnakeRun`'s prediction
broadcast turns no test red. Three attempts failed, instructively: a positive
`Differed` count proves nothing because concurrent delivery makes identical
broadcasts differ, and a zero count proves nothing because on a small graph the
top codes are the same whichever action is named. **Killing it needs a third arm
asking the same action**, to measure how far the walk lands from itself.

**Fork 11 — the output machine is not addressed.** `Message.ReturnTo` is the
input machine; the harness hands the finished thought over by direct call. Needed
before a second machine exists.

---

## FORK NUMBERS THE CODE CITES

**Never renumbered** — `DocsTests` asserts each still resolves. Closed forks stay
listed because the code still points at them.

| | |
|---|---|
| **1** | The distributed rendezvous. **Open** — not needed until a second machine; see the CRDT note |
| **3** | Cluster placement: uniform hash against prefix locality. **Open** |
| **5** | A death writes off exactly the routes heading into the dead cluster. Closed |
| **6** | Broadcast the origin, route the hops. Closed |
| **11** | The output machine is not addressed. **Open**, above |
| **12** | A fixed seed reproduces a run exactly, `Halted` included. Closed by 22's fix |
| **18** | Score prediction **conditional on the next action**. `Consequence` says the system does not model its own effect. **Blocked on temporal edges** |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression. A trade: it pays where the budget cannot compose and costs where it can. Off by default |
| **22** | A transiently-zero live count untracked thoughts mid-flight and dropped every later report. `InputMachine.Retire` asks twice. Closed |
| **23** | Compression self-regulating? Not on this signal. `Thwarted` is the right shape and swings too little against the effect |
| **24** | Budget controller converges from both directions and **aims at a moving target**. Off by default |
| **25** | The binding world — built to fail, failed as predicted, and since lifted |
