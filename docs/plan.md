# Where this is going

**This is the only doc, and it is capped at 3,600 words.** `DocsTests` fails the
build if it grows past that. **To add something, retire something.** The previous
`architecture.md` and `design.md` reached 15,260 and 6,602 words and stopped
being loaded, which is the failure this replaces — a doc nobody reads is worse
than no doc, because it still gets cited.

**What every piece does lives in the XML comments next to the code**, and the
compiler enforces that they refer to things that exist
(`GenerateDocumentationFile`, so CS1572/1573/1574 are build errors). That check
cannot go stale. This file holds only what code comments cannot: **where we are
going, what to try next, and what not to try again.**

Everything deleted is in git — `git log --diff-filter=D -- docs/`.

---

## The goal

A system that **understands** rather than performs — one that can be asked *what
would the world look like if I did X*, which a sequence model cannot be. Learning
is a co-occurrence count; everything else is plumbing around it.

**Three constraints shape every decision.** C1: no node reads another's data.
C2: messages are late, jittered, out of order. C4: no episode boundary, so
nothing may depend on training then testing.

---

## Where it stands

**Three worlds that share no code.** Snake; a senses world where sight and touch
never co-occur; a binding world built so the architecture provably cannot answer
it.

**The result:** on the senses world the graph answers a question it was never
told — **0.8077 ± 0.0215 against a chance of 0.0833**, while the scrambled
control collapses below chance. A memoriser scores exactly zero there.

**The limit, and it has been lifted.** The binding world was built so this
architecture provably could not answer it, and it measured **0.5240 ± 0.0268
against a chance of 0.5000**. With grouping, an index in the question and sender
pricing it now scores **0.8798 ± 0.0148** — see 1a-next. **The three parts are
off by default and the old numbers all still stand.**

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
change either moves that number or it does not, against a control at 0.9167 on
identical input. **Do not build a new world for this; run that one.**

### 1a. Grouping — `Occasion.Groups`, `IQuantizer.Bind`, null by default

The front end says which codes belong to which object; the rendezvous refuses to
pair across objects.

**Alone it lifts nothing — 0.5465 ± 0.0236, pre-registered and held.** Grouping
fixes **learning**; it cannot fix **reference**, because a question asked with a
colour cannot say *which* object it means. **That is what sent the index into the
question**, below.

### 1a-next. ✅ THE CEILING LIFTS — index in the question, sender weighs the edge

**`BindingSettings.Tagged`** gives each object a contentless code of its own,
fresh every scene, in its own group, and the question carries the queried
object's. **`Pricing.Sender`** divides by the *sender's* marginal instead of the
receiver's — C1-legal, because a node sending its own count about itself reads
nobody else's data (`Message.Seen`).

**16 seeds, 400 scenes: 0.8798 ± 0.0148, against a control at 0.5481 ± 0.0227
differing only in whether the question carries the index.** 12.2 sigma apart,
25.7 clear of chance, on a world that measured **at chance** before this.
**And it improves with data** — 0.7095 at 150 scenes, 0.8798 at 400.

**All three parts are load-bearing**, each with its own control:

| | without | with |
|---|---|---|
| grouping | cross-object edges that never existed | control 0.9167 → 1.0000, edges 1751 → 144 |
| index in the question | 0.5481, chance | **0.8798** |
| sender pricing | 0.5726 at 150 scenes; does not finish at 400 | 0.7095 at 150, 0.8798 at 400 |

> **THE PREDICTION WAS HALF WRONG AND THAT IS WHY THE FIX WORKS.** The worry was
> `tag → shape` being too *expensive*. The real fault was `colour → tag` being too
> *cheap*: a fresh index has `seen = 1`, so its arrival weight is 1.0 — the
> cheapest hop there is — and every attribute accumulates one such partner per
> occurrence until the fan-out explodes. Sender pricing inverts that hop and
> leaves the useful one alone.

> **THE CAVEAT THAT MATTERS MOST, AND IT LIMITS THE CLAIM: THIS TASK IS
> MEMORISABLE.** The index is grouped with its object's colour *and* shape, so
> the occasion being asked about wrote `tag → shape` directly. **A memoriser
> scores 1.0 here**, where on the senses world a memoriser scores exactly zero by
> construction. So this is **not** a composition result.
>
> **What it does show is exactly what fork 25 denied.** The ceiling was
> *representational*: the code set was identical under both bindings, so nothing
> could store which-went-with-which at all. It can now. **Whether it can COMPOSE
> over bindings is a separate question and needs a world where the answer was
> never directly observed** — the senses world's trick, applied to bound objects.
> That is the next thing to build, and it is the honest test of step 1.

**Three more caveats.** The front end supplies grouping and index, so this shows
the graph can **use** binding, not **discover** it. `Pricing.Sender` moves the
ranking as well as the price. It costs **5.9× the messages**.

**And it costs nothing on the other two worlds — 12 seeds, not in the suite:**
senses 0.8269 ± 0.0090 against 0.8077 ± 0.0215 (0.8 sigma), snake 186.6 ± 13.4
steps against 172.0 ± 18.9 (0.6 sigma). **Indistinguishable where it was not
needed, transformative where it was**, which is the case for promoting it —
after someone decides whether a ranking change hiding inside a pricing change is
acceptable.

> **A CONFOUND IN THIS WORLD, FOUND HERE: short runs score above chance for
> recency alone** — 0.63 at 60 scenes, decaying to 0.5 as history accumulates,
> because the scene just observed dominates a sparse aggregate. **Nothing under a
> few hundred scenes measures anything here.**

### 1b. Phase — settled, and it was 1a under another name

Von der Malsburg and Singer bind by firing in phase, but a phase is an oscillator
relationship in milliseconds and **C2 destroys exactly those**. Carried as a
field it is not a phase, it is an index. Built as one, above.

### 1a-after. BUILD THIS NEXT — composition over bindings, designed not built

**The honest test of step 1**, because the world above is memorisable. Design
worked out 2026-08-03; nothing written.

**The trap it is designed around:** to ask about a particular object you must
refer to it, and any reference that touches the answer makes the answer directly
observed. **So refer by CONJUNCTION** — the way people do, *the red round one* —
and never let the referring attributes co-occur with the answer.

- Each object gets an index and **three** attributes, drawn fresh per scene.
- **Three moments per scene**, two objects each, grouped by index:
  `{tag₀+A₀, tag₁+A₁}`, then `{tag₀+B₀, tag₁+B₁}`, then `{tag₀+C₀, tag₁+C₁}`.
- **A, B and C never co-occur with each other. Ever.** Only an index ever links
  them, which is the senses world's trick applied to bound objects.
- **Ask with A₀ *and* B₀ — no index — for C₀.** Both reach `tag₀`; under `Sum`
  it gets double the support of any other tag, so the conjunction is what selects
  the object. Then `tag₀ → C₀`.

**Why each control bites.** A memoriser scores zero — `A₀→C₀` was never observed.
Ungrouped fails — `A₀` would pair with `tag₁` too. Untagged fails — nothing links
the moments at all. Asking with `A₀` alone should be **at chance**, because one
attribute cannot single out a scene, and that arm is the sharpest control here.

### 1c. Vector-symbolic binding, if the index ever proves too weak

Plate's HRR, Kanerva's hyperdimensional computing: bind role to filler with an
invertible operation and superpose, so `red⊛colour + ball⊛shape` differs from
`blue⊛colour + ball⊛shape`. C1-legal and C2-immune, and it delivers the
**similarity gradient** named below. Costs opaque codes, which is a real
commitment. **Not needed yet.**

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

## TO BUILD — a ticked box means the type exists, and a test checks

**This is the sync mechanism.** An entry naming a type must be unticked while
that type does not exist and ticked once it does, so **building something forces
it out of the plan** and planning something that already exists fails the build.

- [x] `Binding` — the world that measures the ceiling
- [x] `Seeds` — decorrelated seeding for every sweep
- [x] `Binding` — the world; `Tagged` and `Segmented` are step 1a, measured
- [ ] `Surprise` — the local prediction error of step 2
- [ ] `Chunk` — the minted node of step 3
- [ ] `Drives` — the bounded internal variables of step 4

---

## LATER — worth doing, nothing is blocked on them

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

## DO NOT RE-TRY

**Three columns, one line each, and a test enforces the shape.** The third is the
one that matters: a refutation is conditional on its configuration, so a row
without a revival condition is a superstition rather than a finding.

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial where inverse is polynomial: 5,000,003 messages against 1,111 on a 12-clique | A bound not relying on positive cost at weight 1.0 |
| `Refuel` | Nothing is paid back, so it did nothing | Anything that returns budget to a route |
| Sender-weighing, `IMarginals` | A C1 violation; behaviour identical at 26.7 messages a step against 17.0 | Never. **But `Message.Seen` — the sender's OWN marginal — is legal and is now built** |
| Absolute actions, unrotated view | 6.5 mean steps against 51.3; one move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins: 133.71 steps against 92.85, 2 fruit against 40 | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound — the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: 0.3802 at stamina 4, 0.4887 at 8 | A signal that discriminates; `Thwarted` is 1.19× |
| A deeper walk for prediction | Monotonic, 5.5×: novelty gap 0.0817 at budget 2, 0.0147 at 8 | **Edge kinds** — `master`'s refutation of untyped walking, reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, and both explanations refuted too | Lift in the **cost**. Untried; `Pricing.Sender` is the nearest thing built |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | A similarity gradient under the ranking |
| `Window` span on snake | Null at 150 seeds | **Revived — never run on a senses graph, where `master` got 0.153 against 0.000** |
| `includeEmpty: true` | 46,536 halts against 6, under `Best` | **Revived — inverse cost removed the reason; no clear winner at 60 seeds** |

---

## TRAPS

**Four are closed in code and cannot be fallen into again. Three are live and
need discipline.**

### Closed — named so nobody reintroduces them; the code comments carry the detail

**Consecutive integer seeds are not independent** (`Seeds.Apart`, and `Sweep`
mixes the counter). **Machine load moved the numbers** (the suite is serial now).
**`Measured.Separation` returned 0 with no spread** (infinity for that case).
**`WhenQuiet()` was not a finish signal** (renamed `WhenIdle()`).

### Live

**A dial swept at one data volume may be measuring the volume.** The stamina
plateau reversed between 300 and 1,200 moments. **Convention: every sweep runs at
two run lengths, and a conclusion that does not hold at both is not one.**

**A dial can be declared, documented, passed at every call site and connected to
nothing.** `ThinkAsync`'s stamina was, and survived a build, 155 tests, a mutation
run and three measurements. **Every run type reports `Complaints`; read them.**

**~~Voting exists only on `SensesRun` and `BindingRun`, so every snake number is
a lower bound taken at the noisy end.~~ BUILT AND MEASURED, AND THE CAVEAT WAS
WRONG.** `SnakeRun.PlayAsync(votes:)` exists and is off by default, because the
snake walk disagrees with itself on **0.0018 ± 0.0018 of steps** — one standard
error from zero — against roughly one question in eight on senses. Three turns
over a few dozen nodes wins by a margin delivery order does not overturn.
**Asserted as traffic rather than as outcome**, since "voting changed nothing" is
also what a disconnected dial looks like.

---

## OPEN DEFECTS

**Fork 22 is CLOSED — see the fork index. Silent counts are no longer upper
bounds.** The lesson worth keeping: it was diagnosed by counting reports *sent*
against reports *folded*, and neither number existed before. **When an accounting
disagrees with reality, count the raw thing rather than reasoning about the
derived one.**

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

**Fork 12 is CLOSED too — see the index. The system is now deterministic at a
fixed seed**, which it never was. **The lesson: its old description understated
it because it was measured at a configuration where the symptom could not
appear** (horizon 50, where the backstop never fires and `Halted` is always
zero). A quantity that cannot move in the arm you measured is not evidence that
it does not move.

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
| **12** | ✅ **CLOSED by 22's fix, confirmed against its own control.** A fixed seed now reproduces a run exactly, `Halted` included — `DeterminismTests` |
| **18** | ✅ Score prediction **conditional on the next action**. `Consequence` says the system does not model its own effect — 1.9 sigma, on a prediction that loses to a blind guess. **Blocked on temporal edges** |
| **20** | ✅ Split budgets — deep to act, shallow to predict |
| **21** | ✅ Compression. **A trade**: 0.1827 → 0.7147 where the budget cannot compose, 0.8462 → 0.7596 where it can. Off by default |
| **22** | ✅ **CLOSED** — `InputMachine.Retire` asks twice. 0 of 39 unsettled, from 5–8 |
| **23** | Compression self-regulating? Not on this signal. `Thwarted` is right at 5.1 sigma but swings 1.19× against an effect running 0.18 to 0.83 |
| **24** | ✅ Budget controller converges from both directions and **aims at a moving target**: stamina 8 ties 24 at 300 moments, loses by 7 sigma at 1200. Off by default |
| **25** | ✅ The binding world — built to fail, failed as predicted, **and since lifted**. See above |
