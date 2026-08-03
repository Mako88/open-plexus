# Where this is going

**The only doc, capped at 2,800 words by `DocsTests`. To add something, retire
something.** Its predecessors reached 15,260 and 6,602 words, stopped being
loaded, and kept getting cited — which is the failure this exists to prevent.

**What every piece does lives in the XML comments beside the code**, and the
compiler enforces that they refer to things that exist
(`GenerateDocumentationFile`; CS1572/1573/1574 are build errors). That cannot go
stale. This file holds only what code comments cannot: **where we are going, what
to try next, and what not to try again.** Deleted docs are in git.

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

**Three worlds sharing no code.** Snake; a senses world where sight and touch
never co-occur; a binding world built so this architecture provably could not
answer it.

- **Senses — 0.8077 ± 0.0215 against a chance of 0.0833**, scrambled control
  below chance. A memoriser scores exactly zero there by construction. **Still
  the most interesting result**, and it is transitive association done very well.
- **Binding — was 0.5240 ± 0.0268 against a chance of 0.5000. Now 0.8798 ±
  0.0148**, see 1a-next. **Every part of that lift is off by default**, so the
  older numbers stand unchanged.
- **The graphs are tiny** — 15 nodes on snake, 48 on binding. Nothing here has
  been run at a size where its claims could break. Hold them loosely.

**An occasion is a SET of co-occurring codes**, so *red ball beside blue box* and
*blue ball beside red box* were the same input. That was the ceiling, and it was
**representational, not a matter of scale**: the two arms received bit-identical
input and built identical graphs.

---

## NEXT

**The binding world is the scoreboard. Do not build a new world to test a change
to an old one — run that one.**

### 1a. Grouping — built, and alone it lifts nothing

`Occasion.Groups` + `IQuantizer.Bind`, null by default: the front end says which
codes belong to which object, and the rendezvous refuses to pair across objects.
The stable control goes 0.9167 → **1.0000** with edges collapsing 1,751 → 144,
because the cross-object edges were never real.

**But 0.5465 ± 0.0236 on the real task — pre-registered as a null, and it held.**
Grouping fixes *learning*; it cannot fix *reference*, because a question asked
with a colour cannot say **which** object it means.

### 1a-next. The index in the question, and sender pricing — the lift

`BindingSettings.Tagged` gives each object a contentless code of its own, fresh
each scene, in its own group, and the question carries the queried object's.
`Pricing.Sender` divides by the **sender's** marginal instead of the receiver's —
C1-legal, since a node sending its own count about itself reads nobody else's
data (`Message.Seen`).

**16 seeds, 400 scenes: 0.8798 ± 0.0148 against 0.5481 ± 0.0227 for the same
thing without the index.** 12.2 sigma apart, 25.7 clear of chance, **improving
with data** — 0.7095 at 150 scenes.

> **THE PREDICTION WAS BACKWARDS, AND THAT IS WHY THE FIX WORKS.** The worry was
> `tag → shape` being too *expensive*. The fault was `colour → tag` being too
> *cheap*: a fresh index has `seen = 1`, so its arrival weight is 1.0 — the
> cheapest hop there is — and every attribute accumulates one such partner per
> occurrence until the fan-out explodes. Under receiver pricing the 400-scene run
> does not finish at all.

> **THE CAVEAT THAT LIMITS THE CLAIM: this task is MEMORISABLE.** The index is
> grouped with its object's shape, so the occasion being asked about wrote the
> answer directly, and a lookup table scores 1.0. **It is not a composition
> result.** What it shows is what fork 25 denied — that the binding can be
> *represented* at all. That is the floor rising, not the ceiling.

**Three more caveats.** The front end supplies grouping and index, so this shows
the graph can **use** binding, not **discover** it. `Pricing.Sender` moves the
ranking as well as the price. It costs **5.9× the messages**.

**Costs nothing on the other worlds** (12 seeds, not in the suite): senses 0.8269
± 0.0090 against 0.8077 ± 0.0215; snake 186.6 ± 13.4 steps against 172.0 ± 18.9.
Both under 1 sigma. **Indistinguishable where unneeded, transformative where
needed** — the case for promoting it, once someone decides whether a ranking
change hiding inside a pricing change is acceptable.

### 1a-after. BUILD THIS NEXT — composition over bindings, designed not built

**The honest test of step 1**, because the world above is memorisable.

**The trap it is designed around:** to ask about a particular object you must
refer to it, and any reference that touches the answer makes the answer directly
observed. **So refer by CONJUNCTION** — *the red round one* — and never let the
referring attributes co-occur with the answer.

- Each object gets an index and **three** attributes, drawn fresh per scene.
- **Three moments per scene**, two objects each, grouped by index:
  `{tag₀+A₀, tag₁+A₁}`, then `{tag₀+B₀, tag₁+B₁}`, then `{tag₀+C₀, tag₁+C₁}`.
- **A, B and C never co-occur with each other.** Only an index links them, which
  is the senses world's trick applied to bound objects.
- **Ask with A₀ *and* B₀ — no index — for C₀.** Both reach `tag₀`; under `Sum` it
  gets double the support of any other tag, so the conjunction selects the object.

**Why each control bites.** A memoriser scores zero: `A₀→C₀` was never observed.
Ungrouped fails: `A₀` would pair with `tag₁` too. Untagged fails: nothing links
the moments. **Asking with `A₀` alone should sit at chance** — one attribute
cannot single out a scene — and that arm is the sharpest control of the set.

### 1b / 1c. Settled and parked

**Phase** is an oscillator relationship in milliseconds and **C2 destroys exactly
those**; carried as a field it is an index, not a phase. Built as one, above.
**Vector-symbolic binding** (Plate, Kanerva) stays parked: it would give the
similarity gradient named below, at the cost of opaque codes. Not needed yet.

### 2. Predictive coding — only surprise propagates

Rao & Ballard, Friston. An expected onset is silent. **Traffic collapses**; the
system gets an **internal error signal**, which it has never had — error is
measured by the harness from outside; and it **unblocks drives**, which need
uncertainty felt rather than scored. **Arguably the biggest single gap.**

### 3. Chunking — MDL

Fork 21 mints edges; it should mint **nodes**. When a set of codes recurs, create
a code standing for the set. Threshold is minimum description length, not a
constant. **This is what lets the alphabet GROW** — today the quantiser fixes it
forever. Fork 21's measured trade is the **utility problem** from
explanation-based learning (Minton, SOAR): utility belongs per chunk, not as one
global `Weight`.

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
- [ ] `Composed` — the world of 1a-after
- [ ] `Surprise` — the local prediction error of step 2
- [ ] `Chunk` — the minted node of step 3
- [ ] `Drives` — the bounded internal variables of step 4

---

## LATER — nothing is blocked on these

- **Fork 1 is smaller than it looks.** A count that only increments is a
  **G-Counter, a CRDT** — it converges under arbitrary reordering and loss with
  no coordination. The counts need no protocol; only the join does.
- **The one-way window on a senses graph.** Built, null on snake, **never run
  where `master` measured it working** (0.153 against 0.000).
- **Combinatorial codes** — several coarse hashes per item, so similarity becomes
  overlap. `master` measured conjunction purity at 0.9845.
- **The scaling curve**, which hands back fork 24's real target for free.
- **`Complaints` is duplicated** between `SensesResult` and `BindingResult` —
  about 25 identical lines, worth extracting.
- **The knob pass, deliberately last.** A dial swept before the structural work
  measures a system about to change underneath it.

### The wire, when the remote half lands — John, 2026-08-03

Only the local half of `HybridBus` exists, so none of this is built.

- **Coalesce a settling wave into one send.** `Cluster.DeliverAsync` already
  regroups by cluster. **Hold remote envelopes until local traffic drains, then
  one datagram per destination.** `WhenIdle()` is the trigger and is C1-legal.
  **Not a pure barrier**: flush on idle *or* size *or* time, or a busy machine
  never sends.
- **Bits, not JSON.** Addresses and modalities intern to small ints, a code is a
  varint. **`Chain` is what costs** — cycle check and explanation in one field,
  free locally and not on a wire. **Split them:** a fixed-size
  approximate-membership filter for the hop, full chain rebuilt at the origin. A
  false positive is a route wrongly refusing a partner — the loss C2 admits.
- **UDP is the matching transport, not a compromise.** C2 already assumes loss,
  and **TCP's head-of-line blocking would stall every thought behind one lost
  packet.** QUIC's unreliable datagram extension (RFC 9221) is the shape.

---

## DO NOT RE-TRY

**Three columns, one line each, enforced by a test.** The third matters most: a
refutation is conditional on its configuration, so a row without a revival
condition is a superstition.

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial where inverse is polynomial: 5,000,003 messages against 1,111 on a 12-clique | A bound not relying on positive cost at weight 1.0 |
| `Refuel` | Nothing is paid back, so it did nothing | Anything that returns budget to a route |
| Sender-*weighing*, `IMarginals` | A C1 violation; behaviour identical at 26.7 messages a step against 17.0 | Never. **But `Message.Seen` — the sender's OWN marginal — is legal and is built** |
| Absolute actions, unrotated view | 6.5 mean steps against 51.3; one move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins: 133.71 steps against 92.85, 2 fruit against 40 | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound — the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: 0.3802 at stamina 4, 0.4887 at 8 | A signal that discriminates; `Thwarted` is 1.19× |
| A deeper walk for prediction | Monotonic, 5.5×: novelty gap 0.0817 at budget 2, 0.0147 at 8 | **Edge kinds** — `master`'s refutation of untyped walking, reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, and both explanations refuted too | Lift in the **cost**. `Pricing.Sender` is the nearest thing built |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | A similarity gradient under the ranking |
| `Window` span on snake | Null at 150 seeds | **Revived — never run on a senses graph, where `master` got 0.153 against 0.000** |
| `includeEmpty: true` | 46,536 halts against 6, under `Best` | **Revived — inverse cost removed the reason; no clear winner at 60 seeds** |

---

## TRAPS

**Closed in code — named so nobody reintroduces them.** Consecutive integer seeds
are not independent (`Seeds.Apart`, and `Sweep` mixes the counter).
`Measured.Separation` returned 0 with no spread (infinity now).
`WhenQuiet()` was not a finish signal (renamed `WhenIdle()`).

**And one that was never a trap at all.** "The walk disagrees with itself —
0.8833 alone, 1.0000 under load, so numbers under different loads are not
comparable" **was fork 22**: questions were read before their walk finished, and
under load walks had longer to settle. Now **1.0000 ± 0.0000**, asserted.
**Voting existed because of that number and buys nothing in one process** — kept,
because a real network loses reports, but it is no longer evidence of anything.
The suite stays serial: cheap, and it removes the question.

**Live:**

- **A dial swept at one data volume may be measuring the volume.** The stamina
  plateau reversed between 300 and 1,200 moments. **Sweep at two run lengths.**
- **Short runs on the binding world score above chance for recency alone** — 0.63
  at 60 scenes, decaying to 0.5. **Nothing under a few hundred scenes measures
  anything there.**
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** `ThinkAsync`'s stamina was, and survived 155 tests and three
  measurements. **Every run reports `Complaints`; read them.**
- **A small sample can look like a mechanism.** One seed at 40 scenes gave 1.0000
  with the echo collapsing — three questions. At 32 seeds it was 1.4 sigma.

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

**Never renumbered** — `DocsTests` asserts each still resolves.

| | |
|---|---|
| **1** | The distributed rendezvous. Not needed until a second machine; see the CRDT note |
| **3** | Cluster placement: uniform hash against prefix locality. Open |
| **5** | ✅ A death writes off exactly the routes heading into the dead cluster |
| **6** | ✅ Broadcast the origin, route the hops |
| **11** | The output machine is not addressed. Open, above |
| **12** | ✅ **CLOSED by 22's fix, confirmed against its own control.** A fixed seed now reproduces a run exactly, `Halted` included |
| **18** | ✅ Score prediction **conditional on the next action**. `Consequence` says the system does not model its own effect — 1.9 sigma, on a prediction that loses to a blind guess. **Blocked on temporal edges** |
| **20** | ✅ Split budgets — deep to act, shallow to predict |
| **21** | ✅ Compression. **A trade**: 0.1827 → 0.7147 where the budget cannot compose, 0.8462 → 0.7596 where it can. Off by default |
| **22** | ✅ **CLOSED** — a transiently-zero live count untracked thoughts mid-flight and every later report was dropped. `InputMachine.Retire` asks twice. 0 of 39 unsettled, from 5–8 |
| **23** | Compression self-regulating? Not on this signal. `Thwarted` is right at 5.1 sigma but swings 1.19× against an effect running 0.18 to 0.83 |
| **24** | ✅ Budget controller converges from both directions and **aims at a moving target**: stamina 8 ties 24 at 300 moments, loses by 7 sigma at 1,200. Off by default |
| **25** | ✅ The binding world — built to fail, failed as predicted, **and since lifted** |
