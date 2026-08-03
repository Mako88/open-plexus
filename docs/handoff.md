# Handoff — where to pick up

**Everything is committed and pushed.** `architecture.md` holds the findings and
the open forks; `design.md` holds what every piece does. **This file holds only
what those two cannot: what is in flight, what order was agreed, and the traps
that are cheap to fall into twice.**

**START AT "THE PLAN" BELOW.** Step 0 is a world built to FAIL, with the failure
predicted in advance — do that before building anything meant to fix it.

---

## Where it stands

**214 tests, 0 warnings, branch `csharp`, head `9b8abbf`.**

The loop runs end to end on **two worlds that share no code**: snake, and a
senses world built so that sight and touch never co-occur, making a correct
answer a two-hop composition and nothing else.

**The headline result:** on the senses world the graph answers a question it was
never told — **0.9974 accuracy with three concurrent votes**, against a chance of
0.0833, while the scrambled control collapses below chance. A memoriser scores
exactly zero there by construction.

---

## What is switched OFF and why

**Nothing here is off because it failed to build. Each one is off because the
measurement said something specific.**

| | state | why |
|---|---|---|
| **Fork 21** compression | `Reflect = null` | Works, and it is a **trade**: at a budget too small to compose it nearly quadruples accuracy; where the budget already suffices it *costs*. Not a default until we know which regime matters. |
| **Fork 24** budget controller | `Budget = null` | Converges from both directions. But the plateau it targets is an artefact of run length — at 300 moments stamina 8 ties 24, at 1200 moments 24 wins by 7σ. **Right idea, wrong target.** |
| **Window** temporal span | `span = 0` | Measured null on snake at 150 seeds. **`master` measured the opposite on a senses graph — and we now have one. Never re-run here.** |

---

## THE CEILING, and it is not scale or tuning

**An occasion is a SET of co-occurring codes.** So *"red ball left of blue box"*
and *"blue ball left of red box"* produce the **identical code set**. No amount
of counting, walking, compressing or scaling separates them, because the
information was destroyed at the front door.

**That is the binding problem, and everything else descends from it.** No new
primitives, no roles, no variables, no relations — you cannot represent *X causes
Y* in a structure that cannot represent *which X*.

**This is why the composition result, which is real, is also a ceiling.** It is
transitive association — A–B, B–C ⟹ A–C — done very well. That is the easiest
form of "never told", and it is what spreading-activation systems have done since
the 1970s.

---

## THE PLAN — four borrowings, in build order

### 0. FIRST: build the world this architecture provably cannot do

**Before building any fix.** Two objects with swapped attributes, where the
question is *which attribute belongs to which object*.

**PRE-REGISTERED PREDICTION: the current system scores exactly at chance.** Not
poorly — **at chance**, because the two situations are literally the same input.

**If it does not fail, the model of the system in this document is wrong** and
everything below needs revisiting before it is built. A day's work, and it turns
the whole plan from speculation into a measured next step.

### 1. Binding by phase — von der Malsburg, Singer

Features of the same object **fire in phase**; a second object occupies a
different phase. Red and ball share a phase, blue and box share another. The set
is identical; the phase structure is not.

**Why it fits:** the system already has time. Phase is a **message field, exactly
like `Together`** — C1-legal by construction. The rendezvous already decides what
pairs with what; phase only adds *"…and only within a phase"*. **This is not a
rewrite.**

**It lifts the representational ceiling from sets to structured sets**, which is
the single change that makes everything below worth building.

### 2. Predictive coding — Rao & Ballard, Friston

**Only what was NOT predicted propagates.** An expected onset is silent; surprise
travels. Three things fall out at once:

- **Traffic collapses.** The scaling problem is partly self-inflicted — today
  everything is broadcast, including the entirely expected.
- **The system gets an INTERNAL error signal.** It currently has no way to be
  wrong *and know it*; error is measured externally by the harness. Surprise is
  that signal, and it is local.
- **It unblocks infotaxis and drives**, which need uncertainty to be *felt*
  rather than scored by a test.

**It is also the honest fix for fork 18:** surprise conditional on your own
action **is** prediction error about your own effects.

### 3. New primitives by chunking — MDL

**Fork 21 mints edges. It should mint NODES.** When a set of codes recurs often
enough, create a new code standing for the set, and let it join occasions like
any other — so chunks of chunks form naturally.

**The threshold is minimum description length**, not a constant somebody set:
mint when the chunk pays for its own storage in reduced description length.

**This is what lets the alphabet GROW.** Today it is fixed by the quantiser
forever, so the system can never form a concept it was not handed.

### 4. Homeostatic drives — Ashby's ultrastability

Keep a handful of internal variables inside bounds, and behaviour becomes
goal-directed **without a reward function** — which matters, because reward is
what this design deliberately avoided, and survival already proved gameable by
circling.

**It fits C4 properly:** homeostasis has no episode boundary. There is no run
that ends.

---

## What this displaces, and why those are still worth doing

The earlier order still holds *after* the above, and none of it is wasted:

- **Fork 19's one-way temporal window.** Every edge is currently simultaneous.
  **The mechanism is already built (`Window`) and simply never enabled on the
  world that would show it** — `master` measured it working on a senses graph,
  this branch has one, and it has never been re-run. Cheapest thing on the list,
  and predictive coding needs it.
- **Combinatorial codes** — several coarse hashes per item, so similarity becomes
  overlap and comes free. `master` measured conjunction purity at 0.9845 and
  never built it.
- **The scaling curve**, which hands back fork 24's real target for free.
- **The knob pass, deliberately last** — a dial swept before the structural work
  is measuring a system about to change underneath it.

**Fork 18 is answered as a metric and blocked as a result.** `Consequence` is
built, its control works (cutting the action wire drops the gap to 0.0007 ±
0.0034 against 0.0165 ± 0.0086 intact), and it reports that the system does not
yet model its own effect on the world.

---

## The honest caveat on all of it

**All four could land and it still might not be enough.** Nobody knows what is
sufficient, and anyone claiming otherwise is guessing.

**The narrower claim is the confident one:** without structure, an internal error
signal, a growing alphabet, and a reason to act, **no amount of scaling this gets
there.** Those four look necessary. Sufficient is not a claim anyone can make
honestly today.

---

## Traps, all of which cost time today

**A dial swept at one data volume may be measuring the volume, not the dial.**
The stamina plateau reversed between 300 and 1200 moments. Anything measured at
one run length is conditional on that run length.

**Numbers taken under different machine loads are not strictly comparable.** The
walk's disagreement with itself is 0.8833 when run alone and 1.0000 inside the
full parallel test suite. Two tests deliberately assert nothing about it for
this reason.

**`Measured.Separation` returns 0 when neither arm has spread.** That is correct
in general — two single measurements must not read as significantly different —
and exactly wrong for 1.0000 against 0.0000, where they are perfectly separated.
Assert on the means there.

**`WhenQuiet()` is not a "the walk finished" signal.** In-flight reaches zero in
the gap between a cluster handling a message and dispatching what it produced.
Use `Thought.Settled`.

**Voting exists only on `SensesRun`.** `SnakeRun` still asks once, so every snake
number is a lower bound taken at the noisy end.

---

## Known open defects

**Fork 22 — a few thoughts never settle.** 5–7 of 39 questions; waiting twenty
times longer barely moves it. `Balanced()` still passes, so the books agree with
themselves while claiming routes the bus has already finished. **Every silent
count in this project is an upper bound until this is closed.** Allowed by name
in one test and bounded, rather than hidden.

**A mutation still survives.** Removing the action from `SnakeRun`'s prediction
broadcast turns no test red. **Three attempts to kill it failed**, and the
failures are recorded: a positive `Differed` count proves nothing because
concurrent delivery makes identical broadcasts differ; a zero count proves
nothing either because on a small graph the top-ranked codes are the same
whichever action is named. Killing it needs a third arm asking the *same* action,
to measure how far the walk lands from itself.

---

## What would change my mind about the whole approach

**The learner is a co-occurrence count.** Everything else — the bus, the ring,
the flood, the death accounting — is careful plumbing around a sparse
association table with a walk over it. The composition result is transitive
association done very well, and transitive association is old.

**Three things would need to become true**, and none is close: a similarity
gradient between codes (step 2 attacks this), hierarchy — a code that stands for
a *pattern* of codes — and acting to reduce one's own uncertainty.

**The measurement discipline is the asset.** In one session it caught a race that
silently ate reports, a disconnected dial, an inverted control signal, a
mutation-killer that killed nothing, and a plateau that was an artefact of run
length. **Most projects at this stage have ambiguous results. This one has sharp
refutations, which is evidence it is testing something real.**
