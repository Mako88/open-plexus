# Where this is going

**This is the only doc, and it is capped.** `DocsTests` fails the build if it
grows past the cap. **To add something, retire something.** The previous
`architecture.md` and `design.md` reached 1,646 and 756 lines and stopped being
loaded, which is the failure this replaces — a doc nobody reads is worse than no
doc, because it still gets cited.

**What every piece does lives in the XML comments next to the code**, and the
compiler enforces that they refer to things that exist
(`GenerateDocumentationFile`, so CS1572/1573/1574 are build errors). That check
cannot go stale. This file holds only what code comments cannot: **where we are
going, what to try next, and what not to try again.**

Everything deleted is in git — `git log --diff-filter=D -- docs/`.

---

## The goal, and what would count as reaching it

A system that **understands** rather than performs: it can be asked *what would
the world look like if I did X*, which a sequence model cannot be asked.
Learning is a co-occurrence count; everything else is careful plumbing around it.

**Four constraints shape every decision.** C1: no node ever reads another node's
data. C2: messages are late, jittered, out of order. C4: there is no episode
boundary — no run that ends, so nothing may depend on training then testing.

---

## Where it stands

**Three worlds that share no code.** Snake; a senses world where sight and touch
never co-occur; a binding world built so the architecture provably cannot answer
it.

**The result:** on the senses world the graph answers a question it was never
told — **0.7906 ± 0.0234 against a chance of 0.0833**, while the scrambled
control collapses to **0.0534 ± 0.0116**, below chance. 28.2 sigma apart, and a
memoriser scores exactly zero there by construction.

**The limit, measured 2026-08-03:** on the binding world it scores **0.5240 ±
0.0268 against a chance of 0.5000**, while a control differing only in a fact the
counts can hold scores **0.9167 ± 0.0095 on bit-identical input** — 13.8 sigma
apart, and both arms build the same graph down to the last edge.

> **Every number above was re-baselined once `Seeds.Apart` reached `Sweep`, and
> the correction is instructive.** The senses headline was published as 0.8898 ±
> 0.0068 on consecutive integer seeds; under decorrelated ones the **spread
> across seeds triples** and the mean falls about one true standard deviation.
> The claims are untouched and every error bar is wider. **Numbers elsewhere in
> this file that are not marked re-baselined are pre-correction** — in
> particular the 0.9974 three-vote figure, which has not been re-run.

**An occasion is a SET of co-occurring codes.** *Red ball beside blue box* and
*blue ball beside red box* are the same input. **That is the binding problem, and
everything below descends from it.** The composition result is real and is also a
ceiling: it is transitive association done very well, which is what
spreading-activation systems have done since the 1970s.

---

## NEXT — structure, and it has a scoreboard already waiting

**The binding world exists, is measured, and sits at chance.** Any structural
change either moves that number or it does not, against a control at 0.9247 on
identical input. **Do not build a new world for this; run that one.**

### 1a. Bind by an ephemeral tag — object files

**Each object in a scene gets a contentless code of its own, and every attribute
of that object co-occurs with it.** Then `red → tag₁ → ball` is the two-hop
composition the senses world already does at 0.9974, and the emitted code set
finally differs between the two bindings.

**Prior art, and it is strong.** Pylyshyn's visual indexes (FINSTs) and Kahneman
& Treisman's *object files*: a temporary pointer assigned by attention on
spatiotemporal grounds, **before any feature is identified**. Teyler & DiScenna's
hippocampal indexing theory is the same idea one level up — store an arbitrary
index, reactivate it, and the pattern comes back. In this architecture it is just
another code in the occasion, so it is **C1-legal and needs no new mechanism**.

> **CHECK THE ECONOMICS FIRST — this is reasoning from the design, not a
> measurement, and it is an hour to settle.** A receiver divides by *its own*
> marginal and a hop costs `1/weight`. So `red → tag` is cheap (the tag's
> marginal is 1, weight 1.0) but `tag → ball` prices at `seen(ball)`, which is
> large. **The hop that carries the answer may be unaffordable exactly when the
> tag is doing its job.** `ArrivalValue.Lift` divides out endpoint prevalence in
> the *ranking* but not in the *cost*.

**Honest caveat to state in the write-up:** a tag supplied by the front end tests
whether the graph can *use* binding, not whether it can *discover* it. That is
the right split — vision solves binding at attention, not in association cortex —
but it has to be said out loud.

### 1b. Phase, and why it is probably 1a wearing a different name

Von der Malsburg and Singer bind by **firing in phase**. But phase is a
continuous oscillator relationship measured in milliseconds, and **C2 says
messages are late, jittered and out of order** — which is precisely what destroys
it. Carried as a *message field*, it is not phase at all; it is a tag. **So build
1a, and call it what it is.**

### 1c. If the tag feels like cheating — vector-symbolic binding

Plate's Holographic Reduced Representations, Kanerva's hyperdimensional
computing: bind role to filler with an invertible operation and superpose.
`red⊛colour + ball⊛shape` differs from `blue⊛colour + ball⊛shape`. **C1-legal
(local arithmetic on the message) and C2-immune (the structure rides inside the
code).** It also delivers the **similarity gradient** named below as one of the
three things that must become true. The cost is that codes stop being opaque
identities, which is a real architectural commitment.

### 2. Predictive coding — only surprise propagates

Rao & Ballard, Friston. An expected onset is silent; surprise travels. Three
things fall out at once: **traffic collapses** (today everything is broadcast,
including the entirely expected); **the system gets an INTERNAL error signal**,
which it has never had — error is currently measured by the harness from outside;
and **it unblocks drives**, which need uncertainty to be felt rather than scored.

### 3. New primitives by chunking — MDL

**Fork 21 mints edges; it should mint NODES.** When a set of codes recurs often
enough, create a code standing for the set, and let it join occasions like any
other so chunks of chunks form. Threshold is minimum description length, not a
constant somebody set. **This is what lets the alphabet GROW** — today it is
fixed by the quantiser forever.

> **Fork 21's measured trade is a named problem with thirty years of prior art:
> the UTILITY PROBLEM in explanation-based learning** (Minton; SOAR's chunking).
> Learned macro-operators pay only when search is the bottleneck; otherwise they
> are more to match against. **Utility should be estimated per chunk, not set as
> one global `Weight`.**

### 4. Homeostatic drives — Ashby's ultrastability

Keep a few internal variables inside bounds and behaviour becomes goal-directed
**with no reward function** — which matters, because reward is what this design
deliberately avoided and survival already proved gameable by circling.
**Homeostasis has no episode boundary**, which fits C4 properly.

**All four could land and it still might not be enough.** The confident claim is
the narrower one: without structure, an internal error signal, a growing
alphabet, and a reason to act, no amount of scaling this gets there.

---

## LATER — worth doing, nothing is blocked on them

- **Fix termination detection properly.** Fork 22 below is a *distributed*
  problem, and Dijkstra–Scholten (what is built) is correct only under delivery
  assumptions C2 says we do not have. **Mattern's four-counter method** and
  **Safra's algorithm** are the standard robust replacements; the four-counter
  one specifically tolerates in-transit and out-of-order messages.
- **Fork 1 is smaller than it looks.** A co-occurrence count that only increments
  is a **G-Counter, which is a CRDT** — it converges under arbitrary reordering,
  duplication and delay with no coordination. So the counts need no protocol;
  only the join does. **The thinking half is where C2 can actually hurt.**
- **The one-way temporal window on the senses world.** Built (`Window`), measured
  null on snake, **never run on a senses graph — where `master` measured it
  working, 0.153 against 0.000.** Cheapest thing on this list, and predictive
  coding needs it.
- **Combinatorial codes** — several coarse hashes per item, so similarity becomes
  overlap and comes free. `master` measured conjunction purity at 0.9845.
  Largely subsumed by 1c if that is taken.
- **The scaling curve**, which hands back fork 24's real target for free.
- **The knob pass, deliberately last.** A dial swept before the structural work
  is measuring a system about to change underneath it.

### THE WIRE, when the remote half lands — John, 2026-08-03

**Only the local half of `HybridBus` exists**, so none of this is built. Recorded
now because the shape of it changes what the local half should look like.

- **Coalesce a whole settling wave into one send.** `Cluster.DeliverAsync`
  already regroups by owning cluster, so wire cost is distinct clusters reached
  rather than nodes. **John's extension: hold outgoing remote envelopes until the
  machine's own local traffic has drained, then send one datagram per
  destination.** `WhenIdle()` is the natural trigger and is C1-legal — it
  observes one process's own dispatch queue. **Must not be a pure barrier**: flush
  on idle *or* a size *or* a time bound, or a machine that never goes idle never
  sends.
- **Bits, not JSON.** A machine address and a modality intern to small integers, a
  code is a varint, `Held`/`Carried`/`Together` are fine as floats. **`Chain` is
  what actually costs** — it is the cycle check and the explanation carried in one
  field, which is free locally and is not free on a wire. **So split them:** send
  a fixed-size approximate-membership filter for the hop's cycle check, and
  rebuild the full chain at the origin from the arrival reports. A filter's false
  positive is a route wrongly refusing a partner, which is the same magnitude of
  error C2 already admits.
- **UDP is not a compromise here, it is the matching transport.** C2 already says
  messages are late, jittered, out of order and lost, and the system is built to
  survive that; **TCP's head-of-line blocking would actively hurt**, stalling
  every other thought behind one lost packet. What John wants — datagrams with a
  connection's conveniences — is QUIC's unreliable datagram extension (RFC 9221).
  **The one thing that is not loss-tolerant is the accounting**, which is the same
  reason Mattern replaces Dijkstra–Scholten above. Transport choice and the fork
  22 fix are one decision.

---

## DO NOT RE-TRY — refuted, with the number that refuted it

| | |
|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial where inverse cost is polynomial — **5,000,003 messages against 1,111** on a 12-clique. Only a form strictly positive at weight 1.0 terminates; `1/weight` is it |
| `Refuel` | Nothing is paid back under inverse cost, so it did nothing |
| Sender-weighing, `IMarginals` | The C1 violation receiver-weighing removes. Behaviour indistinguishable (88.87 steps against 95.12, se ≈ 5.5) at **26.7 messages a step against 17.0** |
| Absolute actions, unrotated view | **6.5 mean steps against 51.3**, and one move in four was instantly fatal |
| Survival as the score | Repeating one turn is a circle held forever: **133.71 steps against the chain's 92.85, and 2 fruit against 40.** The arm that survives longest achieves least |
| A beam over partners | A constant nobody set, doing the cutting. Refused on `master` and still refused |
| Clusters grouped by modality | Puts a picture and a sound on different machines by construction — the one link this design exists to make |
| Clusters grouped by time of creation | Two machines seeing the same thing at different times compute different owners, and placement-without-a-coordinator is gone |
| `Adaptive` reflection scaled by `Hunger` | **Inverted** (0.3802 at stamina 4, 0.4887 at 8). Inverse cost exists to exhaust the budget, so starvation is how nearly every route ends at every scale |
| A deeper walk for prediction | Monotonic and about 5.5× end to end: novelty gap **0.0817 at budget 2 against 0.0147 at 8.** Direct association *is* the predictive signal |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, both inert, and both explanations for why were refuted too |
| Naming fewer codes in a prediction | Half true. The ranking carries **coarse** information and no fine ranking — an earlier "carries none at all" was retracted |

**Two that are conditional rather than dead.** `Window` at span 0 is a null *on
snake*, where almost everything persists frame to frame — `master` measured the
opposite on a senses graph. `includeEmpty: false` was worth four orders of
magnitude under `Best` pricing; under inverse cost that reason is gone and at 60
seeds there is **no clear winner** (included: acts far more, survives longer;
withheld: predicts better, a tenth of the messages).

---

## TRAPS

**Four are closed in code and cannot be fallen into again. Three are live and
need discipline.**

### Closed — kept only so nobody reintroduces them

| was | closed by |
|---|---|
| **Consecutive integer seeds are not independent.** `new Random(~s)` **is** `new Random(s + 1)`, and neighbours agree far more than chance allows — spread **1.3 where the binomial says 3.1**. `Measured.StdErr` is taken across exactly those seeds, so every arm read as more significant than it was | `Seeds.Apart` mixes rather than offsets, and `Sweep.ArmAsync` mixes the counter before it reaches the run. Everything re-baselined: error bars widened, no claim changed |
| **Numbers under different machine loads were not comparable** — walk self-agreement 0.8833 alone against 1.0000 under load | The suite runs one test at a time (`Parallelism.cs`). 2m57s against 1m32s |
| **`Measured.Separation` returned 0 with no spread**, reading "indistinguishable" for arms that never varied and landed apart | Returns infinity for that case; the test that asserted on bare means now asserts on sigma |
| **`WhenQuiet()` was not a "the walk finished" signal** and the name invited reading it as one | Renamed `WhenIdle()`. `Thought.Settled` is the finish signal |

### Live

**A dial swept at one data volume may be measuring the volume.** The stamina
plateau reversed between 300 and 1,200 moments. **Convention: every sweep runs at
two run lengths, and a conclusion that does not hold at both is not one.**

**A dial can be declared, documented, passed at every call site and connected to
nothing.** `ThinkAsync`'s stamina was, and survived a build, 155 tests, a mutation
run and three measurements. **Every run type reports `Complaints`; read them.**

**Voting exists only on `SensesRun` and `BindingRun`.** `SnakeRun` asks once, so
every snake number is a lower bound taken at the noisy end.

---

## OPEN DEFECTS

**Fork 22 — a few thoughts never settle.** Re-measured 2026-08-03 and **still
live**: 5–8 of 39 questions on senses, 2–7 of 39 on binding. `Balanced()` passes
throughout, so the books agree with themselves while claiming routes the bus has
finished. **Every silent count in this project is an upper bound until this is
closed.** See Mattern above.

**A mutation still survives.** Removing the action from `SnakeRun`'s prediction
broadcast turns no test red. Three attempts to kill it failed and the failures
are instructive: a positive `Differed` count proves nothing, because concurrent
delivery makes identical broadcasts differ; a zero count proves nothing either,
because on a small graph the top codes are the same whichever action is named.
**Killing it needs a third arm asking the same action**, to measure how far the
walk lands from itself.

**Fork 11 — the output machine is not addressed.** `Message.ReturnTo` is the
input machine; the harness hands the finished thought over by a direct call.
Needed before a second machine exists.

**Fork 12 — `Halted` is approximate**, and both orderings cost something. A
cluster sends onward before reporting, so a downstream death can be reported
before the upstream split that created it. Reporting first was measured and
destabilised whole runs.

---

## FORK NUMBERS THE CODE CITES

**Deliberately never renumbered** — the code cites these in a dozen places and
`DocsTests` asserts each one still resolves.

| | |
|---|---|
| **1** | The distributed rendezvous. Not needed until a second machine — and see the CRDT note |
| **3** | Cluster placement: uniform hash against prefix locality. Open |
| **5** | ✅ A death writes off exactly the routes heading into the dead cluster |
| **6** | ✅ Broadcast the origin, route the hops |
| **12** | `Halted` is approximate. Open, above |
| **18** | ✅ Score prediction of world state **conditional on the next action**. Built; `Consequence` reports the system does not yet model its own effect — gap 0.0165 ± 0.0086 intact against 0.0007 ± 0.0034 with the action wire cut, which is only 1.9 sigma and sits on a prediction that loses to a blind guess. **Blocked on temporal edges** |
| **20** | ✅ Split budgets — deep to act, shallow to predict. Wins survival, mirroring and prediction at once |
| **21** | ✅ Compression built. **A trade, not a win**: at a budget too small to compose it lifts accuracy 0.1827 → 0.7147; where the budget suffices it costs, 0.8462 → 0.7596. `Reflect = null` by default, and off is the control |
| **22** | Thoughts that never settle. Open, above |
| **23** | Can compression regulate itself? Not on this signal. `Thwarted` goes the right way at 5.1 sigma but swings only 1.19× against an effect running 0.18 to 0.83 |
| **24** | ✅ Budget controller built, converges from both directions — and **aims at a moving target**: at 300 moments stamina 8 ties 24, at 1200 moments 24 wins by 7 sigma. `Budget = null` by default |
| **25** | ✅ The binding world. Built to fail, and failed as predicted. See "Where it stands" |
