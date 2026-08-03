# Handoff — 2026-08-02

**Everything is committed and pushed.** `architecture.md` holds the findings and
the open forks; `design.md` holds what every piece does. **This file holds only
what those two cannot: what is in flight, what order was agreed, and the traps
that are cheap to fall into twice.**

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

## The agreed order

1. **Fork 19's one-way temporal window.** Every edge in the graph is currently
   simultaneous — the graph knows *with* and cannot represent *then*. Fork 18's
   metric says the system does not understand its own effects, and nothing
   action-conditional can work on top of pure simultaneity. **The mechanism is
   already built (`Window`) and simply never enabled on the world that would show
   it.** Cheapest high-value step available.
2. **Combinatorial codes** — the olfactory front end. Several coarse hashes per
   item rather than one fine one, so similarity becomes overlap and comes free.
   `master` measured conjunction purity at 0.9845 and never built it. Fixes the
   deepest structural gap: today two codes are identical or unrelated, with no
   gradient between.
3. **The scaling curve** — and it hands back fork 24's real target for free.
4. **The knob pass** — sweep every dial, automate what can be automated.
   **Deliberately last**, because a dial swept before steps 1–2 is measuring a
   system about to change underneath it.

**Fork 18 is answered as a metric and blocked as a result.** `Consequence` is
built, its control works (cutting the action wire drops the gap to 0.0007 ±
0.0034 against 0.0165 ± 0.0086 intact), and it reports that the system does not
yet model its own effect on the world. It is waiting on step 1.

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
