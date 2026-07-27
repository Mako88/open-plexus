# Note 016 — Who supplies relevance, and why it decides whether this is AGI work

Four mechanisms have now failed to derive "what is worth storing" from the token
stream: consolidate-on-use, salience, both again under realistic statistics, and
— on its pre-dispatch control — competitive capture. Every result that matters in
this project runs through an **oracle** that is told the answer.

The obvious move is to stop calling that cheating and accept a supplied signal.
John's question is whether doing so quietly converts this into an LLM-components
project and closes off the AGI goal.

**It depends entirely on who supplies it, and the two candidates look identical
in our code today.** That is why this is worth writing down before the
architecture hardens.

## The two things "supplied relevance" can mean

**(a) The application tells us.** A chat system knows when a user asked a
question. A retrieval system knows which tokens came from a document and which
from a prompt. `store=mask` from an API flag.

This works, it is honest, and it would make goal 2 reachable. **It is also an
AGI dead end**, and not for a subtle reason: a system that only remembers what it
was told to remember cannot be doing the thing intelligence does. It has no
opinion about its own experience.

**(b) An evaluative subsystem tells us.** Not the task — a *part of the same
agent*, outside the memory, that assigns value: reward, harm, novelty, goal
relevance, drive satisfaction.

This is what brains do, and it is worth being precise about it, because the whole
project has been assuming otherwise. **Neuromodulatory signals are not derived by
cortex from the input stream.** Dopamine, acetylcholine and noradrenaline arrive
from elsewhere, driven by outcomes the organism cares about — food, pain,
surprise relative to expectation, effort. The cortex does not work out from the
statistics of its inputs which of them mattered. **It is told, by a system whose
entire job is caring.**

## Which means our four failures may be answering the wrong question

We have been asking: *can a memory infer, from the stream alone, which of its
contents will matter later?*

That is a harder question than biology solves, and we have four independent
negative results on it. That pattern — a mechanism that works in brains, failing
repeatedly in a faithful implementation — is usually a sign that a component
present in the original is missing here rather than that the mechanism is wrong.

**John proposed exactly this component and proposed it early:** *"it may need a
'body' that triggers storage. a real brain gets chemicals flooding due to
external events."* [Note 013](013-salience-and-the-missing-body.md) implemented
the *signal* — surprise — but kept it **intrinsic**, computed from the model's own
predictions. That is still the memory grading its own homework. The body in the
proposal was a source of value **outside** the predictive machinery, and that
part was never built.

## (c) The agent's own output, marked by consequence

John asked whether an **indirect** output could serve as the reward — not the
model grading itself, which obviously breaks, but something downstream of its
behaviour, so it can shape itself.

The distinction that decides it is not direct-versus-indirect. It is **whether
the loop passes through something the model cannot control.**

**Intrinsic signals have failed six times.** Confirmation, surprise, both again
under skew, and competitive capture at two pool sizes are all quantities the
model computes from its own state. However indirectly they are derived, a model
that can influence the signal can satisfy the signal, and the six failures are
not obviously separable from that. Anything the model calculates about itself is
in this category no matter how many steps of arithmetic sit between.

**But a consequence is not intrinsic.** If the model acts, and the *environment*
responds, and the response is the signal, then the model has shaped the signal
without being able to choose it. That is the same structure as the reward token
in `reward_recall` — extrinsic, late, and in the input — except the model
influences *which* reward arrives rather than only receiving one.

So the answer is yes, with a specific condition: **the output can generate the
reward as long as the world is in the loop.** Self-shaping is fine; self-certifying
is not.

### What that would take, concretely

A task where the model's prediction changes what it later sees. Then "was that
binding worth keeping" is answered by what happens next, and the model cannot
answer it for itself.

`reward_recall` deliberately does not do this — the reward is scripted, and the
model is a passive receiver. That was the right first step, because it isolates
*can a gate use a real relevance signal* from *can an agent generate one*. The
first is now measurable and looks positive at 0.34 recovery on a control. The
second is a different task and a different mechanism.

### Correction: "intrinsic versus extrinsic" was the wrong axis

John's follow-up — *many problems have built-in incremental-success values that
can be fed back as the world* — breaks the framing above, and the break is
useful.

He is right that such signals are everywhere and are free: distance to a goal,
inversions remaining in a sort, pieces correctly placed, digits correct, bits
saved. None of them needs an environment to be built. **The task already computes
them, and the model cannot fake them.**

But that exposes a problem with the account above, because **consolidate-on-use
is already one of these.** It fires when a prediction was correct — and whether a
prediction was correct is decided by the token that *arrives*, which the model
does not choose. By the intrinsic/extrinsic test it is extrinsic, world-supplied,
ungameable. **And it is one of the six failures.**

So that axis does not separate what works from what does not. A second one does:

> **Does the signal carry information about FUTURE usefulness, or only about
> present correctness?**

- `on-use` says *this retrieval was right just now*. It is extrinsic and it is
  about the present. It failed.
- The reward token says *this binding will be asked about*. It is extrinsic and
  it is about the future. It recovers 0.19–0.23 where the others recover nothing.

The two-by-two makes the six failures look less like six attempts at one idea and
more like a systematic exploration of one quadrant. Everything tried before
`reward` was **present-tense**, whatever its source.

That reframes John's suggestion rather than diminishing it. Incremental-success
values are exactly the right family to draw from — **provided the increment is
about progress toward something not yet achieved.** Distance-to-goal qualifies:
it falls because of a step taken now and it is defined by an outcome not yet
reached. "Was that last move legal" does not.

**Status of this: argument only.** The two-by-two is a reading of results already
in hand, not a new measurement, and it was written after seeing them — which
makes it a hypothesis about why the six failed, not a finding. What would test it
is a present-tense signal that is unusually strong, or a future-tense one that is
weak; either would separate the axis from the source.

### The near-term version, which is cheap and probably wrong

Bootstrapping: train with the reward gate, then use the trained model's own
confident retrievals as pseudo-rewards for a second round. It is self-shaping,
the loop is short, and it is the kind of thing that either compounds or drifts.

**Recorded as a candidate rather than a plan**, because it is exactly the shape
of the six failures — a signal the model computes about itself — and the only
thing that distinguishes it is that the first round's gate was extrinsic. That
may be enough or it may be laundering.

## So the decision is not "accept the oracle or don't"

It is **which of (a) or (b) the accepted signal is**, and they demand different
next steps:

- **(a) is a deployment convenience.** It should be labelled as one, used where it
  helps goal 2, and never described as a finding about intelligence.
- **(b) is a missing architectural component.** Accepting it is not surrender; it
  is factoring the problem the way biology factors it — *memory* plus *value
  system*, rather than a memory expected to infer value. An AGI needs an
  evaluative subsystem regardless. Building one is on the path, not off it.

**The risk of not distinguishing them is that they are the same line of code.**
Both are `run(store=mask)`. A project that accepts (a) for the pragmatic reason
and then reports the tiny-node results as intact has quietly changed what it
claims while changing nothing that anyone can see in a diff.

## The limit that has to be stated plainly

**MQAR cannot tell (a) and (b) apart.** Any signal that identifies which
positions matter on this benchmark *is* `position_kinds()`, whatever we call its
source. There is no extrinsic value in a sequence of random symbols — nothing in
it is good or bad for anything.

So the distinction is untestable here, and testing it needs a task where
something is genuinely at stake: an outcome the system is trying to obtain or
avoid, arriving separately from the input. That is a **reinforcement-shaped
task**, not a sequence-modelling one, and this project has never had one.

That is a real fork in the benchmark ladder, and it deserves to be a deliberate
choice rather than a drift:

- **Sequence tasks** — Zipfian, then a corpus, then bAbI — serve goal 2 and test
  whether the memory works on real statistics.
- **A task with extrinsic reward** would be the first thing that could test (b),
  and the first thing pointed at the AGI goal rather than beside it.

## Status

**Argument only. Nothing here is measured**, and the claim about neuromodulation
is from the source list's summaries plus general background rather than from
papers read for this purpose — the same standard note 010 was held to, and it
should be met before anything is built on this.

The immediate consequence is small and worth doing anyway: **when the oracle is
used, say which kind of signal it is standing in for.** GOALS currently says only
that it is a ceiling.
