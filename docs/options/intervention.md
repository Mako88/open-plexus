# Option record — INTERVENTION: acting to disambiguate, not only observing

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- **Nothing.** No mechanism, no instrument, no measurement. This record exists because
  the need for it has now been measured twice from different directions, and because
  John named it as the direction after kill-list #1.

---

## What was tried, and what came back

### The need, measured: a confound is refused by 0.0096 — `g39-06`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g39-06-what-about-a-thing-present-almost-always.txt
            script  experiments/g39_06_what_about_a_thing_present_almost_always.py
            task    MNIST + FSDD + 10 words, 12,000 occasions, one distractor
                    present on every occasion of digit 3 and 10% elsewhere
            model   conditional read FORWARD; no cut, no mutuality
            knobs   presence 0.5..1.0 independent; one correlated arm; 3 seeds
            scale   score gap between the weakest true partner and the confound

**A surface that is merely COMMON is refused with a wide margin — 0.4490 — and
partial presence cancels out of the statistic entirely.** A surface that is
CORRELATED with a concept is refused by **0.0096**, a 47-fold collapse, and only
because it happens to be 63% specific to that concept while a true partner is
essentially 100% specific.

**A stronger confound crosses, and nothing locates where.**

### And no statistic over co-occurrence can fix it — `g32-01`, `g39-06`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g39-06-what-about-a-thing-present-almost-always.txt
            script  experiments/g39_06_what_about_a_thing_present_almost_always.py
            task    as above
            model   the whole co-occurrence family
            knobs   none -- an argument about what the data contains
            scale   n/a

**This is not a defect of any particular statistic.** A surface genuinely more
common around one concept IS evidence about that concept, and an observational
stream contains nothing distinguishing *spuriously correlated* from *actually part
of it*. Every mechanism in `co-occurrence-statistic.md` reads the same counts and
therefore inherits the same blindness.

`g32-01` named intervention as the escape when the falsifier was first answered
and explicitly did not claim to have tested it. **It still has not been.**

### John's ruling on why it matters beyond this bug — 2026-07-31

    CONFIG  when    2026-07-31
            source  John, in conversation
            script  none -- a direction, nothing measured
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

> *"That's also, I think, heading towards AGI — being able to interact with the
> world is inherently necessary."*

**Recorded as a direction with a stated reason, not as a plan.** He placed it
after kill-list #1 in the queue.

**Why it is more than a fix for one failure.** Everything this project has built
is an OBSERVER: it watches a stream and accumulates. An intervening system
*changes* the stream to answer a question it has — show the picture without the
sound, or the sound without the lamp — which is the difference between correlation
and causation, and between a passive learner and an agent.

---

## What would have to be built, and what would refute it

**Nothing here is designed.** These are the shapes the measurements point at, and
`CLAUDE.md` is explicit that naming a mechanism is not building one and that a
diagnosis is a claim about behaviour needing the same evidence as any other.

- **The instrument does not exist.** `occasions.py` GENERATES a stream; it has no
  notion of a request from the learner. Every task in `openplexus/tasks/` is a
  reader or a generator, so intervention needs a task layer that can be ASKED
  for a specific occasion — and that is a bigger change than any mechanism.
- **The obvious falsifier**: does a system permitted `n` interventions separate
  a confound that an observer of the same stream cannot? **If it cannot, the
  whole direction is refuted cheaply**, and that experiment can be built before
  any learning rule changes.
- **The C1 question, which must be asked early.** An intervention is a request
  that something be shown. On a network of consumer devices there may be nothing
  to ask, and a mechanism requiring a cooperative world is not obviously
  compatible with §1's constraints. **Answer that before building.**
- **The cheapest first step is the confound boundary**, which is already
  registered: `g39-06` brackets the crossing between 63% specificity and
  wherever it fails, and locating it says how much intervention would have to
  buy.
