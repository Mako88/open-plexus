# 034 — How many machines can you actually use?

## The question the whole project rests on

The premise is that a capable system could run across many ordinary computers
instead of one big expensive one. That premise has an unstated assumption:
**that when the problem gets harder, you can just add more machines.**

Machine *size* isn't something we control — it's whatever people already own.
Machine *count* is the part that's supposed to be elastic.

So: as problems get harder, does the number of machines you can usefully split
across hold up?

## The answer is no, and now we know by how much

Each machine has a minimum useful size, and that minimum rises as problems get
harder:

| problem size | smallest machine that works alone |
|---|---|
| 48 | 6–8 slots |
| 96 | 8–10 |
| 192 | 16–20 |
| 384 | 24–30 |

Fit a curve through that and machines must get bigger roughly **twice as fast**
as total capacity needs to.

Concretely: **to handle a problem ten times longer, each machine has to be about
4.7× bigger, while the total capacity you need only grows 2.3×. So the number of
machines you can split across halves.**

The elastic quantity is the one that stops helping. That's the failure this test
was written to detect, and it detected it.

## It's a slope, not a cliff

Doubling the problem costs about 19% of the usable machine count. A hundredfold
increase costs a factor of four. You can still build large networks — what you
can't do is chop a *given* problem into ever-finer pieces, and the finest useful
piece gets coarser as problems grow.

## What I predicted, and where I was wrong

I predicted the answer would come out fine — machines roughly holding their own
as problems grow. **I also wrote down, before running it, that this was the
answer I wanted and therefore the one to distrust.**

Good thing. It came back clearly on the wrong side.

The interesting part is *why* I was wrong. My guesses for the four numbers were
close on the two harder problems and too high on the two easier ones. Since the
slope depends on the ratio between the ends, being wrong at one end was enough to
flip the conclusion.

## The tool got it wrong a third time — and this time in reverse

Twice before, my analysis script has been over-confident: it produced tidy answers
from data that didn't support them, and I've written rules against both.

This time it did the opposite. It reported **"unresolved"** for data that
resolves perfectly well, because it calculated its own margin of error using a
number hard-coded from a previous experiment — assuming the measurements were
twice as coarse as they actually were.

**The under-confident version is the more dangerous one.** An over-confident
number invites someone to check it. "Unresolved" invites another experiment that
was never needed — and this one would have cost hours.

Both failures have the same root: the tool assuming what its data looks like
instead of reading it.

## One genuinely promising thing

There are two ways to get an answer out of the network: ask one machine, or add
up what all of them say. Everything above uses the strict version — **one machine,
alone, no help.**

By the lenient version the picture is far better. On a problem where a lone
machine needs 20 slots, the pooled answer still works with **6**. And we
established earlier that pooling is cheap — a small combining step only when a
question is actually asked, not constant chatter.

So if a real deployment can afford that step, the limit is much looser than the
number above. Three of four measurements ran off the bottom of our grid before
they could pin it down, so **we don't know how much looser.**

That's the most promising direction available, and it's currently measured
nowhere.
