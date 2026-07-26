# 10. The test we nearly built

[Explainer 9](09-checking-our-homework.md) checked our claims about how learning
works. This one checks the *other* set of borrowed claims — the ones about what
an untrained network can't do, which were deciding our entire choice of test.

**Result: the reasoning was right, and the specific test we picked was wrong in
a way that would have wasted the first experiment.**

---

## What we'd claimed

From [explainer 5](05-what-makes-a-fair-test.md): a random untrained network is
secretly good at blurry short-term memory, and bad at choosing what to remember
and looking things up by content. So we proposed a memory-game test — *pairs go
by, then you're asked about one of them* — because it targets those weaknesses.

All reasoned from first principles. None of it checked.

---

## The reasoning holds, and there are numbers now

Two established results, both stronger than what we'd argued.

**There's a hard ceiling on memory.** A network can't remember more than it has
storage for — and that's a proven bound, not a rule of thumb. We'd said its
memory is "finite and spent carelessly." The actual result is more precise:
capacity is limited by how many independent quantities the network tracks, full
stop.

**Being clever costs you memory.** This one we'd missed entirely, and it
*strengthens* our case. The very thing that makes these networks useful — the
complicated nonlinear way they mix inputs together — actively *destroys* stored
memory. You can't buy long memory by making the network more sophisticated,
because sophistication and memory spend from the same account.

---

## The mistake

Here's the one that matters.

We proposed: *show the network some pairs, then ask about one of them.*

**That version has already been solved — by models much weaker than the
heavyweight ones.** The researchers say so plainly: prior work shows relatively
simple architectures can solve it *perfectly*.

So if we'd built it, everything would have passed. Untrained network: fine.
Simple model: fine. Big fancy model: fine. **No gap. Nothing to measure.**

Which is precisely the trap that cost the previous project a year — a test where
everything scores well so you can't tell anything apart. **We'd have walked into
the identical trap by a completely different route**, having written an entire
document about avoiding it.

**The fix is small and decisive: ask about *all* the pairs, not one.**

That's it. Show ten pairs, then query all ten. That version *does* separate the
architectures, dramatically — and the researchers built it specifically because
the single-question version had stopped being informative.

We'd listed "number of pairs" as one adjustable setting among four. It isn't a
setting. **It's the thing that makes the test work at all.**

---

## How big is the gap?

Large, and measured on real language rather than a toy:

> A **70 million** parameter attention model beats a **1.4 billion** parameter
> model of the weaker kind at this task.

A twenty-fold size difference, and the smaller one wins. That's exactly the room
we needed and couldn't previously prove existed.

---

## Better news than we expected

Our claim was roughly "weak networks *can't* do this." The actual finding is
more subtle and considerably more useful:

**They can — it just costs them, at a rate that grows with how long the sequence
is.** Attention handles longer sequences without needing to get bigger. Weaker
architectures need to grow.

Why that's better: [explainer 5](05-what-makes-a-fair-test.md) warned about the
opposite trap — a test so hard that *only* the data-centre method can do it,
where our failure would tell us nothing. A test that's merely *expensive* for
weak methods is exactly right. There's a real gap, and it's a gap that could in
principle be closed, at a price we can measure.

---

## A worry we hadn't said out loud, now resolved

Looking something up by content sounds like it might *require* comparing
everything to everything else. And comparing everything to everything is exactly
the everyone-talks-to-everyone pattern we've banned.

If that were true, this test would be rigged against us permanently — we'd fail
it forever for reasons that have nothing to do with whether our idea works.

**Someone proved it isn't true.** There's a construction that solves the task
without all-the-comparisons, and it's *faster* than the attention approach, not
slower.

So the test is fair. We could have spent months worrying about that, and we
never quite formulated it clearly enough to worry properly. Writing it down was
what made it checkable.

---

## And one new requirement we didn't have

The researchers found what actually separates the winners from the losers, and
it isn't attention specifically:

> **The model has to adapt how it combines information based on what it's
> currently looking at.**

A network whose wiring behaviour is fixed in advance has to be enormous. A
network that adjusts its behaviour per example doesn't.

That's a genuine design requirement, and it arrived from outside rather than
from our own reasoning — which is exactly what reading was supposed to produce.

**Does it break our rules?** Probably not, and the distinction is delicate.

A part deciding *how strongly* to weigh what it's hearing, based on what it's
hearing — that's a purely local decision. Fine.

A part deciding *which machine to talk to* based on what it's hearing — that
would wreck the bandwidth budget from
[explainer 8](08-does-it-fit-down-the-pipe.md), because the whole plan there
depends on each part talking to a small, *fixed* set of machines.

**So: fixed wiring, adjustable volume.** That's our current best guess. We've
written it down as a guess, because whether adjustable volume on fixed wiring is
*enough* to satisfy the requirement is an open question we can't answer yet.

---

## Honest status

**Still inferred, not measured:** the researchers tested several architectures,
but **not the specific random-untrained-network setup we plan to use as our
floor.** It's a close cousin of what they tested, and we're reasoning by
analogy. Reasonable. Still an inference — and the last two explainers should
have taught us what those are worth.

Also still on credit: the memory-ceiling results here come from summaries rather
than the original papers, and several other things we've cited remain unread.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
