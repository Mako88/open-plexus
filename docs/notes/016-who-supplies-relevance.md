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
