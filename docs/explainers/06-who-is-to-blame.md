# 6. When it gets something wrong, who's to blame?

This is *the* problem in machine learning. It has a name — **credit
assignment** — and everything else is downstream of how you answer it.

[Explainer 2](02-why-ai-needs-data-centres.md) covered the standard answer and
why it forces a data centre. This one is about the answer we're choosing
instead, and the one property that makes it work.

---

## The problem, restated

Your system has millions of internal parts. It produces an answer. The answer is
wrong.

**Which part was at fault?**

You can't just blame all of them equally — that's not learning, that's noise.
You need to know that *this* part contributed a lot to the error and *that* one
barely at all.

The standard method figures this out by working backwards from the answer,
which requires every part to wait for every other part, thousands of times a
second. Hence: data centre.

---

## The distinction nobody drew

Here's the thing we found that we think the previous project missed, and it
reframes the whole question.

When people talk about "how the learning works," they're actually mixing up
**two different questions**:

1. **Where does the blame signal come from?** (the *source*)
2. **How does it get to the part being blamed?** (the *delivery*)

These sound similar. They're not, and only the first one decides whether you
need a data centre.

**An analogy.** Suppose a company gets a customer complaint and needs to work
out which department caused it.

- The **source** question is: where does the assessment of what went wrong come
  from? Head office reading the complaint? Or each department noticing its own
  output was off?
- The **delivery** question is: once someone knows, how does the message get to
  the right desk? Email? A memo? A meeting?

Now: **if head office is the only one who can assess the problem, then no
improvement to the memo system will help.** Every department still has to wait
for head office. You can make the memos faster, prettier, better-routed — every
department is still waiting on one central authority.

**That's what happened to the previous project.** It spent a year building
increasingly sophisticated memo systems, and never questioned that head office
was the only one doing the assessing. Its central measured failure — that the
blame signal arrives too late to be useful — is a *delivery* measurement of a
*source* problem. It was never going to be fixed by better delivery, because the
signal always had somewhere to travel from.

The good news: the memo systems it built are probably fine. They were just
attached to the wrong thing.

---

## Our answer: everyone assesses their own work

The scheme we're choosing:

> **Each part predicts what it's about to receive next. Then it sees what
> actually arrives, and the difference is its error.**

No head office. Nobody sends anybody a blame signal. Each part generates its own
correction out of two things it already has: what it guessed, and what showed
up.

---

## The property that makes this work

Here's the argument, and it's the whole reason for the choice.

**Under the old scheme, delay causes a race.** A part does something at 12:00.
The blame signal about it arrives at 12:05. For the correction to be meaningful,
the part must still remember what it did at 12:00 — and memory fades. So the
signal is racing against forgetting.

You can make the memory last longer, but then it gets *vaguer* — it now
remembers "roughly what I was doing this afternoon" rather than "what I did at
12:00." So the further the signal has to travel, the blurrier the blame.
**Distance and precision trade against each other, and you can't escape it.**

**Under the new scheme, delay costs storage.** A part predicts at 12:00 what
it'll see next. If that input is coming from a machine in Australia, it arrives
at 12:00 and a fifth of a second. Fine — the part just holds onto its prediction
until then, and compares.

The cost is **remembering your own guess for a while**. That's it. And here's
the key bit:

> **A prediction held for a fifth of a second is exactly as precise as one held
> for a millisecond.** Nothing gets blurry. You just need a slightly bigger
> notepad.

So the internet's slowness stops being a race you can lose and becomes a
storage cost you can budget for — known in advance, and with no accuracy
penalty.

**That's the entire case.** One property. If it holds up, the hardest measured
obstacle in the previous project doesn't exist in this one.

---

## A trap we nearly walked into

There are two different things called "predictive coding," and one of them would
have broken everything.

**The version we don't want:** each layer predicts the layer below it, and the
whole network then bounces messages up and down repeatedly until it settles into
a stable state — *before* any learning happens. That settling is dozens of round
trips through the entire network, per input. Over slow internet links that's
catastrophic, and it's precisely the everyone-waits-for-everyone pattern we're
trying to escape.

**The version we want:** predict what comes **next in time**. One pass. No
settling. The answer is supplied by the future rather than by everyone agreeing.

This matters practically, because the published results showing predictive
coding works well are mostly about the *first* version — and those results
depend on the settling. Taking the conclusion without the settling isn't
supported by the evidence. **Both go by the same name**, so anything we read has
to be checked for which one it means.

---

## A conflict we found by writing this down

Worth including, because it's the clearest example of why we write the argument
before the code.

[Explainer 5](05-what-makes-a-fair-test.md) proposed making the test harder by
**adding irrelevant noise** — because a random untrained network can't tell what
to keep, so junk fills up its memory. Good argument.

But this explainer's scheme learns by **predicting what comes next**. And random
noise is, by definition, *unpredictable*. So if most of the sequence is junk,
then most of what our system is trying to predict is impossible to predict — and
the useful learning signal drowns in irreducible error.

**Both arguments are correct. They pull in opposite directions.**

The most promising fix is neat: make the distracting material **structured but
irrelevant** rather than random. Then it's still hard to know what to keep
(which is what makes the test hard), but it's not impossible to predict (which
is what was breaking the learning). Those two properties were tangled together
and didn't need to be.

We haven't settled it. But finding it *now* — from two documents written days
apart, before either was built — is the entire point of working this way. The
alternative is discovering it six months in as an unexplained failure that looks
like the idea being wrong.

---

## The honest status

Everything here is an **argument**, not a result. Specifically unproven:

- **The big one:** we don't know whether a network's own state actually predicts
  what it's about to receive. If it doesn't, there's nothing to learn from and
  this whole scheme is dead. **This should be the very first thing tested** —
  it's one small experiment, and it gates everything else.
- We haven't confirmed that delayed comparison really is exact.
- **We still haven't properly read the prior work.** Everything here about what
  other researchers found is from second-hand summaries, and the source/delivery
  distinction — which is currently carrying the whole argument — needs checking
  against the actual papers.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
