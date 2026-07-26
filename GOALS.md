# Open Plexus — Goals

What this project is for, what would refute it, and what is deliberately not
being attempted. Nothing below is a measurement. This document states intent and
the conditions under which the intent is wrong; every number in it is either
arithmetic or an inherited result from a prior project, and both are labelled as
such.

This is the first document in the project and the architecture is downstream of
it. That ordering is deliberate and is the correction of a specific, named
mistake — see *§6, Why this exists*.

---

## IN PLAIN TERMS

Today's AI runs in data centres, and it has to. The way neural networks are
trained requires every part of the network to exchange information with every
other part, in lockstep, many times a second. That is only affordable when the
machines sit in one building on a dedicated network. The consequence is that the
scale of an AI system is set by how much capital one organisation can raise.

Meanwhile there are billions of computers, phones and consoles sitting idle in
people's homes. They are already bought and already paid for. They are connected
by the ordinary internet — slow by data-centre standards, unreliable, and
constantly being switched off.

**This project asks whether a neural network can be built that runs on those
machines instead.** Not a faster network: a differently-shaped one, where no part
ever has to wait for a global picture, and where a machine leaving in the middle
of a thought is a normal event rather than a failure.

If it works, the payoff is a path to large-scale AI whose limit is how many
people want to join rather than how much money one company has.

If it does not work, the useful outcome is a clear, measured statement of
*which specific constraint kills it*. That is worth having on its own, because
nobody appears to have written it down.

---

## 1. The goals

**Primary — AGI.** A neural network distributed across the internet at scale,
with the potential to lead toward artificial general intelligence.

**Secondary — an LLM replacement that does not need data centres.** Replace
large language models, or pieces of them, to reduce the need for concentrated
compute.

**The ordering is load-bearing.** Where the two conflict, the primary wins. A
design that would make a better distributed LLM but forecloses generality is the
wrong trade.

### 1.1 The clarification that changes what counts as success

**Raw efficiency against a GPU is not the deciding question.** The world already
has billions of idle devices. A model that is *less* efficient per FLOP but runs
on hardware that already exists and is already paid for can still meet both
goals. "Would a single GPU beat this" is a footnote, not a blocker.

What matters instead is whether the thing works on **consumer devices that are
unreliable, heterogeneous, and constantly leaving.**

### 1.2 The operative research question

The goals above are a direction, not something a run can settle. The question
that experiments actually answer is:

> **What is the largest class of problems learnable using only local information
> and bounded asynchrony?**

Every design decision should be traceable to that sentence. If a mechanism does
not enlarge that class, or does not preserve locality while enlarging it, it
does not belong here however good the numbers look.

---

## 2. What is explicitly not the goal

Stating these because each one, unstated, quietly redirected the predecessor
project.

- **Not biological fidelity as a target.** Resembling a neuron is not a
  property this project optimises. Biology is nonetheless a first-class
  *reference* and should be used freely — see §2.1, which is the affirmative
  half of this and matters more than the restriction.

- **Not efficiency per FLOP.** See §1.1.

- **Not novelty for its own sake.** Prior art that already solves a
  sub-problem gets used and cited. The contribution, if there is one, is the
  system that works under these constraints — not the individual parts.

- **Not a working product.** This is a research project whose first job is to
  find out whether the central bet is wrong, as cheaply as possible.

### 2.1 Biology as a reference — used deliberately, and often

**Biology is the one existence proof of a system that learns under exactly the
constraints in §3.** It runs on local information, tolerates tens to hundreds of
milliseconds of conduction delay, and loses components continuously without the
whole failing. No engineered system does all three. That makes it the single
most valuable source of hypotheses available here, and it should be consulted
whenever a design question is open. Where evolution has already solved a problem
this project also has, understand that solution before inventing another one.

Two distinctions keep that productive rather than misleading.

**First: separate what neurons *compute* from what they merely had to *cope
with*.** Ion-channel kinetics, all-or-none spikes (axons attenuate; digital
links do not), and metabolic limits are constraints of wetware, and copying them
imports a cost without the reason for it. Dendritic computation, local plasticity
rules, homeostasis, and delay tolerance are candidate *computations*, and those
are worth taking seriously. When borrowing, say which of the two it is.

**Second: biology is a reason to try something, never a reason to keep it.**
"The brain does it this way" is a hypothesis of exactly the same standing as any
other, and it is subject to the same gates in §4 and the same evidence rules in
`CLAUDE.md`. This is where the predecessor drifted: its four headline departures
from conventional design were each justified biologically, and an audit later
found every one of them either inert in every configuration that had produced a
result, or refuted outright. The biological motivation was never the problem —
treating it as evidence was, because it made those mechanisms feel already
justified and so nobody measured them for a year.

So: read biology first and read it widely. Then measure it like anything else.

---

## 3. The three constraints that define the design space

These are the project. Everything else is negotiable.

### C1 — Locality

> **No operation may require globally synchronised state.**

A mechanism needing a population sort, a global mean, a pooled matrix, or a
barrier is a violation, and gets flagged as one **even when it improves the
numbers.** Backpropagation violates this by construction: the backward pass is a
global barrier moving data proportional to parameter count, which is precisely
why deep networks need tightly-coupled hardware.

If an exception is ever admitted, it is named as an exception, in one place, with
what depends on it — never absorbed silently.

### C2 — Bounded asynchrony

Information arrives late, out of order, and at varying delay. The design must
state a **bound** it tolerates, and be correct — ideally bit-identical — below
that bound. A design that merely degrades gracefully is weaker than one with a
stated, tested bound, because only the latter can be engineered against.

Intercontinental round trips are ~150 ms. A mechanism whose credit signal must
arrive within a few milliseconds cannot be distributed, no matter how well it
learns locally.

### C3 — Churn

**Machines leaving is the normal case, not an edge case.** A consumer device is
switched off, put to sleep, or has its network drop, constantly and without
warning. The system must be designed from the start on the assumption that any
node can vanish mid-computation and that the remainder continues.

This has never been tested in the predecessor project, because nothing ever left.

---

## 4. Falsification — the gate ladder

The goals in §1 are too large to test. This ladder is what actually gets tested.
Each gate names the outcome that **refutes** the project at that stage, so the
project can be killed cheaply and early rather than expensively and late.

The ordering is by *cost of finding out*, cheapest first. **No gate is skipped,
and no gate is passed on a single run** (rule 3).

| gate | the question, plainly | refuted if |
|---|---|---|
| **G0 — the instrument** ✅ **PASSED** | Is there a task that a random, untrained substrate *cannot already do*, and that is learnable from local information at all? | No such task can be constructed. Then nothing downstream can be measured, and the project has no instrument. |
| **G1 — does it learn** ✅ **PASSED** | Does a purely local objective beat the random substrate on that task? | The margin is null across seeds. The central bet is wrong. |
| **G2 — asynchrony** ✅ **PASSED** | Does the margin survive realistic delay, jitter and reordering, up to a stated bound? | The margin vanishes below the bound the internet actually imposes. |
| **G3 — churn** ✅ **PASSED** | Does the margin survive nodes leaving mid-run and rejoining? | Losing a node degrades the whole rather than a part, or recovery costs more than the node was worth. |
| **G4 — bandwidth** | Does the required cross-machine traffic fit consumer broadband? | The traffic needed for the margin exceeds what a home connection carries. |
| **G5 — scale** | Does the margin hold or grow as the network grows? | The margin shrinks with scale. Then it is a small-model curiosity, not a route to either goal. |

**G0 is first for a reason, and it is the correction of the predecessor's single
most expensive mistake.** Choosing a benchmark that defeats trivial baselines is
necessary but not sufficient — that benchmark must also leave a learning rule
something to do. In the predecessor, it did not: a frozen random substrate
already scored 0.802, total headroom to a strong non-local model was ~0.19, and
existing non-learning mechanisms took ~40% of it. Nearly a year of learning-rule
work was measured against a ceiling that was never there.

**The G0 acceptance test is therefore explicit:** before any learning mechanism
is written, the task must be shown to have substantial headroom between what a
random frozen substrate achieves and what a strong non-local reference achieves,
with both measured, multi-seed, and with the base rate of a constant predictor
reported alongside.

---

## 5. Sequencing — what is chosen before what

The predecessor's stated regret is the ordering: it picked biologically-motivated
mechanisms first and then looked for a learning rule that fit them. **Credit
assignment is the hard part and the binding constraint, so it is chosen first and
the substrate is chosen to serve it.**

1. **The task** (G0) — the instrument. Nothing is measurable before it exists.
2. **The credit-assignment scheme** — chosen against C1/C2/C3 *before* any
   substrate exists, on paper, with the locality and latency argument written
   out.
3. **The substrate** — the minimum representation that lets the chosen scheme
   work. Not a catalogue of interesting mechanisms.
4. **Distribution** — the transport and the churn model, against a substrate
   that has already passed G1.

The current best candidate for step 2, carried forward as a *hypothesis* and not
a decision, is a **predictive / self-supervised local objective**: each unit's
error comes from comparing its own prediction against its own next input. Three
properties recommend it, and all three are arguments rather than measurements:

- **It dissolves C2 rather than working around it.** There is no broadcast
  signal, so there is nothing that can arrive late.
- **It is the same objective family as an LLM.** Next-token prediction is
  next-input prediction, which matters directly for the secondary goal.
- **It needs no labels.** A network running on strangers' devices cannot assume
  a labelled target at every node. This is a *primary-goal requirement*, not a
  preference.

The decision is deferred to the plan. This section records the candidate so the
plan can argue against it.

---

## 6. Why this exists — the predecessor, and what transfers

This project replaces `plexus` (`Mako88/submenu`, branch
`claude/bio-inspired-neural-model-ohhrp6`; handoff snapshot at
`PLEXUSBRIEF.md`). It is a restart, not a fork, for two stated reasons:

1. **The architecture was built without a plan first.** Mechanisms accumulated
   and the design document was written to describe them, so there was never a
   document that could reject a mechanism.
2. **The framing was "biology, but better."** The right framing is that the
   machines and the network already exist — so build for *those*. The two
   framings do not select the same design, and the second is the one that serves
   the goals.

**The name keeps the lineage on purpose; the architecture does not.** "Open"
carries the reframing — the machines are already out there, already owned by
people, and the network between them is the public internet rather than a
private fabric. Nothing in the predecessor's code is inherited. What is
inherited is its record of what did not work, which is the most useful thing it
produced.

### 6.1 What transfers, and at what confidence

**Nothing here is a measurement of this project.** These are prior results about
a different architecture, recorded so they can be tested rather than repeated,
and so no time is spent rediscovering them. Rule 1 applies: none of these may be
quoted as a property of this system until this system is measured.

| inherited finding | transfers? | why |
|---|---|---|
| **Emission-time indexing makes jitter free below a stated bound** — a run stays bit-identical under arbitrary reordering and lateness below `delay_min`; tolerance is exactly `delay_min − 1` | **High — as a technique, to re-derive** | This is a property of the indexing scheme, not of that model. It is the most defensible idea the predecessor produced and it directly serves C2. Re-derive it here rather than importing it. |
| **A short-term-plasticity-like mechanism carried memory across delays** (0.527 → 0.864 there) | **Medium — as a hypothesis** | Large effect, but measured on that substrate and that task. Treat as a candidate, not a result. |
| **The sparse-event bandwidth arithmetic** — a large sparse network at 1 kHz emits ~10⁸–10⁹ events/s; at 1% of connections crossing the network that is ~50 MB/s, at 10% it is ~500 MB/s | **High — it is arithmetic** | The order of magnitude follows from sparsity and rate, not from architecture. G4 is where this becomes real, and the fraction crossing the network was **never swept** there. |
| **Memory, not compute, is the binding constraint at scale** (~16 bytes per connection) | **Medium-high** | Follows from any design storing per-connection state. The constant is design-specific. |
| **A broadcast supervised error signal is the least local, least scalable part of such a design — and measured inert** | **High — as a warning** | Directly informs §5: do not choose it. |
| **Three-factor learning with eligibility traces did not learn** (−0.003, p = 0.79) | **Low as a result, high as a caution** | Published work reports this family working. The discrepancy is more likely an implementation or task problem than a refutation of the family. Do not treat as settled. |
| Every measured constant — credit windows, decodability scores, neurons per core | **None** | Properties of that architecture and that benchmark. Both are being replaced. |

### 6.2 Prior work to read before building

Recorded from the predecessor's notes and **not yet re-read by anyone on this
project.** Every row needs checking before anything is built on it, and no claim
from them may be quoted until it has been.

| what | who | why it matters here |
|---|---|---|
| **Predictive coding** | Rao & Ballard (1999); Whittington & Bogacz (2017) | Local error from prediction mismatch. Directly the §5 candidate. |
| **e-prop** | Bellec et al. (2020) | Three-factor learning reported working. The most diagnostic discrepancy available. |
| **Forward-Forward** | Hinton (2022) | No backward pass at all. Maximally local. |
| **SORN** | Lazar, Pipa & Triesch (2009) | Closest existing system to the predecessor's substrate. Read for what makes their plastic part pay. |
| Dendritic / two-compartment error | Urbanczik & Senn (2014); Sacramento et al. (2018) | A local error computed within a unit. The natural way to *deliver* a predictive error. |
| Burst-dependent plasticity | Payeur et al. (2021) | Multiplexes credit and activity down one channel. |
| Feedback alignment / DFA | Lillicrap et al. (2016); Nøkland (2016) | Removes weight transport. |
| Reservoir computing | Maass (LSM); Jaeger (ESN) | **The G0 control.** Any result must be reported against a random frozen substrate, because that is what a reservoir already gives for free. |
| Federated / decentralised learning | (survey needed) | **Gap in the predecessor's reading.** This is the field that actually studies unreliable heterogeneous nodes, and it was never consulted. Read before designing distribution. |
| Gossip / epidemic protocols, CRDTs | (survey needed) | **Gap.** C1 and C3 are distributed-systems problems with a distributed-systems literature. |

The last two rows are additions. The predecessor read neuroscience and machine
learning and did not read distributed systems, which — given that the goal is a
system distributed over unreliable machines — is the more likely place for the
answer to C3 to already exist.

---

## 7. What the stack must satisfy

The implementation language is **not chosen here.** It is an architecture
decision and it follows the plan. What the goals fix is the set of constraints
any choice has to meet:

- **Two different jobs, possibly two different answers.** The *research kernel*
  optimises for speed of asking questions and access to the prior-work
  ecosystem. The *eventual runtime* optimises for shipping to a stranger's
  Windows laptop with no toolchain installed. Assuming one language serves both
  is an assumption, and it gets stated rather than made.
- **The research kernel must not become the project.** The predecessor was
  measured *overhead-bound*, meaning per-operation cost dominated real work.
  Whatever is chosen must make it cheap to find that out early.
- **A reference implementation must exist that is obviously correct and slow**,
  against which any fast path is asserted. A fast path that has never been
  checked against a simple one is an unmeasured claim.
- **GPU availability is not a constraint** on the research kernel (§1.1), and
  the development machine's GPU is too old for current frameworks regardless.
  Do not design around it.

---

## 8. Open questions before the plan can be written

In the order they need answering. Each is a question, not a task.

1. **What task passes G0?** The single highest-value unanswered question, and
   the one that sank the predecessor. It must be something a frozen random
   substrate cannot do, that a strong non-local reference *can*, with a large
   measured gap between them.

   **Analysed in [docs/notes/001](docs/notes/001-what-task-passes-g0.md); still
   open, because nothing has been run.** That note argues the corrective action
   is not "a harder task" but *a task whose difficulty lies where a random
   substrate has no answer*; recommends associative recall; argues for a task
   **family with a difficulty dial** rather than a single task, because a gap no
   local rule could ever close is as useless as no gap at all; and records its
   predictions in advance. Three sub-questions it raises are unresolved — the
   data budget, how a discrete-token task is encoded for an event-based
   substrate, and whether the reference model must be strong or merely non-local.
2. **Which credit-assignment scheme, and what is the argument that it satisfies
   C1 and C2?** Written on paper before anything is built.

   **Answered in [docs/notes/002](docs/notes/002-which-credit-assignment-scheme.md),
   subject to one untested gate.** Recommendation: **self-supervised temporal
   prediction** — each unit predicts its own next input and learns from the
   difference. The argument is that this converts latency from a *race* into a
   *buffer depth*: there is no signal in transit that can be late, so a delay
   costs memory rather than credit precision. The note also separates **error
   sources** from **error delivery** and argues the predecessor was stuck
   because it only ever varied the latter, and distinguishes temporal prediction
   from relaxation-based hierarchical predictive coding, which violates C1.

   **The gate:** does a unit's own state predict its next input above chance? If
   not, the scheme has nothing to learn from. One probe settles it, and it must
   run before anything is built on this choice.

   **It also surfaced a conflict with note 001** that has to be resolved at task
   design time — a noise dial widens the G0 gap but starves a predictive
   objective. See note 002 §7. **Resolved, and the note had the sign backwards:**
   [note 008 §4](docs/notes/008-the-task-objective-mismatch.md) shows irreducible
   loss contributes no gradient, so *random* filler is the correct choice and the
   proposed structured-filler fix was the harmful one. Measured at 0.824
   predictable versus 0.135 for task content, with 83% of positions being filler.

   **A larger mismatch is open**, argued in
   [note 008](docs/notes/008-the-task-objective-mismatch.md): MQAR's task content
   is uniformly random, so the only predictable task-relevant token is the answer,
   and that is predictable only after retrieval works. Three directions are laid
   out with a recommendation; **the choice is a project-direction call and has not
   been made.**
3. **What is the churn model?** What does "a machine left" mean concretely —
   at what granularity, with what warning, and what is the system's obligation
   when it happens? C3 is currently a principle without a definition.

   **Defined in [docs/notes/003](docs/notes/003-the-churn-model.md), and this one
   rests partly on a primary source that was read.** Granularity: the machine is
   the failure domain, all its units go together. Warning: assume none. Detection
   is a *separate liveness channel*, because on a sparse event substrate silence
   is normal and absence of data cannot signal absence of a machine. Obligation:
   nothing global — no barrier, no reconstruction, each affected unit notices
   locally and continues.

   **Two findings changed the picture.** Measured session lengths are Weibull
   with shape `k` well below 1, so the hazard rate *decreases* — the longer a
   machine has been up, the less likely it is to leave, and uptime predicts
   remaining uptime. And the inspection paradox: a randomly chosen *session* is
   short, but a randomly chosen *currently-active peer* is long-lived. The
   frightening median-session numbers answer a question we are not asking.

   **Architectural result:** `d_max` is simultaneously the C2 asynchrony bound
   and the C3 churn timeout — within it a source is a straggler, beyond it a
   dropout. Two constraints, one parameter. The cost is a false-positive zone
   where a slow link is declared dead, which wants measuring.
4. **What fraction of connections crosses the network, and is that a free
   parameter or forced by the design?** G4 turns on this number.

   **Re-asked for the architecture the project actually has**, in
   [note 009](docs/notes/009-splitting-the-memory.md). The local rule has no
   fan-out — it has one `d × d` matrix — so the question becomes how to split
   that matrix. **Splitting by columns forces an all-reduce every step and
   violates C1; splitting by rows does not**, at the cost of broadcasting the
   full key vector to every machine. Broadcasting from an origin is impossible at
   any usable rate; broadcasting as a tree costs `F · d · 4` bytes per machine per
   step and `log_F(M)` hops of depth — and that depth is exactly what g2-01
   measured as free. Affordable region on a 10 Mbps upload at fan-out 8 is
   roughly `d · rate ≤ 40,000`.

   **Still analysis, not measurement**, and the largest untested assumption is
   that the global readout is a benchmark artefact rather than part of the
   design.

   **Answered in [docs/notes/004](docs/notes/004-the-bandwidth-budget.md), and
   the question named the wrong parameter.** The fraction crossing the network is
   **forced to ~1** under uniform placement — each machine holds 1/31,000th of
   the network, so a target is local with probability 3 × 10⁻⁵. The free
   quantity is **`D`, the number of distinct destination *machines* per emitting
   unit**, because delivery is one packet per destination machine, not per
   connection. Both `p` and `D` are consequences of one real variable:
   **placement locality**.

   **The budget:** upload binds, being 5–20× slower than download on consumer
   connections. `D` ≤ 3.7 at 10 Mbps up, `D` ≤ 14.7 at 40 Mbps. **`D` must be
   single digits to low tens.**

   **This forces an architectural property:** connectivity must be
   local-dominant with sparse long-range links, since ~1,000 targets have to
   land on ~15 machines. Derived from a bandwidth budget, not from biology. **The
   cost — whether clustered islands are less capable than a well-mixed network —
   is unmeasured and is the most important open item in the note.**

   **Two side results.** Batching is mandatory (72% framing overhead unbatched),
   and it is affordable only because C2 already tolerates delay — at 150 ms the
   overhead is 0.1%, so **C2 pays for itself twice**. And note 003's heartbeat
   worry is dismissed: under 5% of budget in the worst configuration, because
   heartbeats are per machine pair rather than per unit.
5. ~~**Does the distributed-systems literature already answer C3?**~~
   **Partly, and it was cheap — as predicted.** Answered as a side effect of
   [note 003](docs/notes/003-the-churn-model.md): the churn measurements
   transferred directly and changed the design target, while federated
   learning's architecture did not transfer at all (round-based, central
   aggregator — a C1 violation twice over) though its straggler/dropout taxonomy
   did. **Still unread and now the highest-value gap: gossip protocols,
   SWIM-style failure detectors, and CRDTs.** SWIM in particular exists to avoid
   exactly the false positives that note 003 §5 names as the cost of unifying
   `d_max`.

---

*Status: **G0, G1 and G2 passed.** A rule with no backward pass and no softmax over
positions — every update a product of two signals at the synapse — solves MQAR
at 8/8 seeds, against 0.180 for a frozen substrate and 0.344 for a one-line
heuristic. **The price of locality is roughly 4–6× in width** (crossing at
48–64, against attention's 8–16). Unexpectedly, the local rule is **graded**
where attention was all-or-nothing, because superposition interference eases
continuously while circuit discovery does not — which makes it better shaped for
a learning rule than the thing it replaces. **G2 is passed too**: below a stated delay bound the learned weights are
**bit-identical** to a run with no network at all — 6/6 seeds, including every
event delayed by up to 64 steps on 96-step sequences. That exactness is bought by
emission-time indexing rather than by the learning rule, so it holds for any rule
behind it. Two costs measured: loss *compounds* (a binding needs its pair and its
query to both survive, so accuracy falls as a product not a fraction), and a
buffer deep enough for intercontinental lag is deeper than these sequences — the
system batches rather than streams, so "latency is free" holds for throughput and
not for time-to-first-response. **G3 is passed**: half the substrate removed permanently, mid-training, recovers
to 0.924 against a 0.992 baseline within a few epochs; a quarter costs 0.006.
Nothing persists but the readout, so a departing machine takes capacity rather
than memories.

**One number is retracted.** g3-02 found the width curve — and therefore the
"locality costs 4–6× in width" figure — was substantially measuring the frozen
projections' initialisation scale, a constant chosen once and never swept: a
native width-32 model scores 0.263 at scale 1.0 and 0.960 at 0.71, nothing else
changed. **g1-08 has now re-measured it with both arms tuned, and the honest price is
4.0× in width** — the local rule crosses at 32 and attention at 8, each at its
own best scale. Tuning both made the price *worse*: like-for-like at the old
untuned settings it was 3.0×, so the retracted figure was not conservative but
unfounded, and landing near the right answer was luck.
G4 (bandwidth) and G5 (scale) remain — but G4's central assumption is no longer
an assumption. [g4-01](experiments/sweeps/g4-01-no-global-readout.txt) removed the
global readout, which [note 009](docs/notes/009-splitting-the-memory.md) §4 had
identified as the largest untested claim in the project and as a standing C1
violation hiding inside a benchmark convenience. **At adequate width it costs
nothing: 1.000 against 1.000 with the width split eight ways.** And a single
machine's answer stands up alone (0.949 at eight-way, 0.996 at four-way), so the
pooling step is optional rather than required — which is the claim C1 actually
needs.

The penalty numbers away from ceiling are provisional: the learning-rate grid
pinned at an edge in four of six rows, so those rows are under-tuned on every arm.

**The price, and a caveat that is now permanent.** Upward of **5.6×–8.2× the
width**, growing with sequence length; in *working memory*, worse than attention
below ~96 steps and about **2× better** at 384. Both are **bounds, not points** —
attention had still not converged at four times the budget already thought
sufficient, and **all four revisions of this comparison have moved against the
local rule** ([g1-13](experiments/sweeps/g1-13-both-arms-fed.txt)). The ratio is
not being chased further: it has a systematic bias toward whichever side was
measured less carefully, which has been ours every time.

*What is not in doubt are the properties rather than the ratios — no backward
pass, no softmax over positions, bit-identical under a scrambled network,
survives half its machines leaving, converges in one epoch where attention needs
thousands of steps, and working memory that does not grow with sequence length.
None of those has been revised once.*

**And one scaling law is now measured.** The width the local rule needs grows as
roughly the **cube root** of the stream length it must hold (exponent 0.37 across
an eightfold range) — where attention's state grows *linearly* in stream length
and its time quadratically. That took four attempts: `n_pairs` and `n_keys` are
both flat, and the load turned out to be `seq_len`, because the store binds every
consecutive pair rather than only the meaningful ones. It also means the 4.0×
price is a point on a curve rather than a constant, since the two architectures
must diverge in stream length — re-measuring it across `seq_len` is the natural
next step.*

*Previously: **G0 passed.** MQAR is answerable (oracle 1.000, checked mechanically
across a grid), reachable (one hand-written lookup, 1.000), and **learnable** (a
model trained from scratch, 1.000 on 5/5 seeds). Its trivial floor is measured
and has a closed form; a frozen substrate sits at 0.180, leaving **0.82 of
verified headroom**. G1 is next and is the actual bet: whether a **local** rule
can reach what an all-to-all one just did.*

*Nothing in this document
has been measured by this project.*
