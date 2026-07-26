# 041 — Can anyone join?

## The worry

Our machines answer by adding their votes together. A powerful machine's vote is
mostly signal; a tiny one's is mostly noise.

So there's an obvious risk: **adding noise to a good answer could make it worse.**

If that's true, a real network can't just let anyone in. It would need to weigh
votes by how good each machine is, or refuse the weakest ones altogether — and
either way, **something has to decide who counts.** That something is a
coordinator, and avoiding coordinators is the entire point of this project.

So this is a small experiment with a large consequence.

## The answer: no, and it isn't close

We tested forty combinations — strong machines of every size admitting weak
machines of every size.

**One came out negative, by 0.006.** That's a third of what we treat as noise.
Everywhere else, letting a weaker machine join made the answer better or left it
unchanged.

| a strong machine of… | admitting 1 tiny machine | admitting 16 |
|---|---|---|
| 1 node | +0.089 | +0.406 |
| 4 nodes | +0.047 | +0.278 |
| 8 nodes | +0.007 | +0.158 |
| 16 nodes | **−0.006** | +0.062 |

The pattern is exactly what you'd hope: newcomers help a lot when the existing
machine is small, help less when it's already good, and never actively hurt.

**So the network can accept whoever turns up.** No vetting, no reputation scores,
no negotiation about who's worth listening to.

## A check nobody asked for, which passed

There's an accidental duplicate in this experiment. When the "weak" machine
happens to be the same size as the strong one, it's the same test as the control
we ran alongside — a second machine of equal size.

Those two numbers are produced by different code, from different lists of
participants. **They agree exactly, at every size.** Which is decent evidence
neither is mislabelled — the kind of check that's worth more for not having been
designed.

## What I got right, and why it counts this time

All four predictions written beforehand held.

Last time that happened I noted it was partly hollow: the checks we run before an
experiment had already taught us the answer, so the "predictions" were summaries.
**This time the predictions came first** and the checks came after. So the clean
result means what it looks like.

## What this doesn't cover

Machines here differ only in **how many** identical pieces they hold. Real ones
differ in reliability, in speed, and in whether they're still there in five
minutes.

A machine that does no harm while present might still do harm by **vanishing
mid-answer** — and nothing here, or in the earlier dropout work, tests that.
That's the gap worth naming rather than glossing.
