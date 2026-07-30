081 — C4's failure mode is saturation, not forgetting, so it requires DISTRIBUTION
=================================================================================

**Status:** measured, prequential, with saturation guaranteed rather than hoped for.
**It is the most consequential measurement of the session** and it threatens the three
results above it, so it is stated before them in any summary.

C4 has been *"untested after two attempts, both times because the task was too easy to
need it"* (`091`, `092`). This arranges 10.6x capacity so it cannot be too easy.

---

## IN PLAIN TERMS

The system is meant to learn for as long as it runs. Writing 4,000 facts into a store that
holds about 377 gives two possible behaviours and **both of them fail**:

**Keep everything, and everything becomes unreadable.** Recall falls to 7%, and — the part
nobody predicted — **old and new degrade equally.** Nothing is being forgotten in favour of
anything else; it is all equally buried.

**Fade the old, and it works perfectly but only briefly.** The most recent hundred facts come
back at 99%, and anything older than about a hundred is simply gone.

**So the store is not a memory. It is a window.** And the thing that makes learning-forever
work cannot be a better fade rate, because there is no fade rate that holds more than the
store's capacity.

---

## The measurements

`d=128`, so capacity is `~0.023·d²` ≈ 377 bindings (`109`).

    written   load    recent  oldest   norm@written  norm@never   gate sep
        200   0.5x     1.000   1.000          1.563       1.245       1.26
       2000   5.3x     0.170   0.260          4.087       3.926       1.04
       4000  10.6x     0.080   0.060          5.649       5.500       1.03

    decay    last100     mid   first100   gate sep   effective window
    1.0        0.120   0.120      0.070       1.02              3,000
    0.999      0.920   0.020      0.000       1.10               ~999
    0.99       0.990   0.000      0.000       1.42                ~99

## Three findings, and the second is the dangerous one

**1. There is no recency gradient without decay.** At `decay=1.0`, oldest sometimes beats
recent (0.260 against 0.170 at 5.3x). That is not a fluke — a Hebbian sum is
order-independent, so **interference is symmetric.** *"Catastrophic forgetting"* is the wrong
name for this substrate's failure: nothing is preferentially lost.

> **That rules out replay as the fix.** GOALS §4's correction kept replay in scope precisely
> because it is *"one of the few known answers to the catastrophic forgetting C4 makes
> first-class."* Replay re-presents what is being lost — and here everything is lost equally,
> so there is nothing to preferentially re-present.

**2. THE GATE DOES NOT SURVIVE SATURATION, and three results depend on it.** Decision 148's
structurally-zero read needs an unwritten address to read ~0 against a written one. At 10.6x
load that ratio is **1.03** — 5.500 against 5.649, indistinguishable.

    what depends on it                             status at saturation
    `148`'s exact gate (1.0000/0.0000)             gone
    `note 080`'s contradiction detection           its norm signal is the gate
    `note 071`'s structured-address viability      measured at 3 facts per entity

**Note 080's absent-vs-written separation was 0.375 against 1.047 at low load.** That is the
same quantity as `gate sep`, and it collapses. So the credit loop closed last hour is closed
**only inside the window.**

**3. Decay buys the gate back, and only within its window.** `gate sep` goes 1.02 → 1.42 as
decay tightens, because a faded store has fewer live bindings interfering. So the gate's
health is a function of *live* load, not total writes — which is the useful form of the
statement.

## What follows, and it makes distribution mandatory rather than optional

Capacity is fixed at `~0.023·d²` for one store. The two knobs both fail: no decay saturates,
decay windows. **So satisfying C4 needs capacity that GROWS**, and there is exactly one
mechanism in this project that provides it — **concept partitioning**, where each node holds a
FULL-WIDTH store for its own concepts, so total capacity is `nodes × per-node` (`134`
measured lone-node capacity 2048 against dimension splitting's 128 at sixteen nodes).

> **Distribution has been framed throughout as how the system USES spare machines. This says
> it is the only known way the system satisfies its own fourth constraint.** That is a
> different and much stronger reason to build it, and it was not the reason anyone was
> building it.

## What is NOT claimed

**Not that partitioning is sufficient.** It multiplies capacity by node count, which buys
orders of magnitude and not infinity. C4 says *forever*, and forever exceeds any fixed
multiple — so something must still shed, and *what to shed* is untouched.

**Not that the three results above are wrong.** They are correct at the load they were
measured at, and note 071 explicitly recorded 3-facts-per-entity as its load. **What is new
is that the load matters more than any of them said.**

**Not measured with `memory_cap` or consolidation.** Both exist, both are refused in
combination with `concept_nodes` today, and neither was in this fixture. A cap bounds the
norm rather than the count, so it is not obviously a capacity mechanism, but it is untested
here.

**And the window is not useless.** 0.990 on the last hundred is excellent, and a system whose
working memory is a sharp recent window plus a partitioned long-term store is a coherent
design — it is roughly the one `consolidation` and `lasting` were built for. That combination
has never been measured, and after this it is the obvious thing to measure.
