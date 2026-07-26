# 42. The cheat we could not replace

This is the worst result the project has produced, and it was the most important
one to run.

## The setup

Our devices remember things by piling every association into one small patch of
memory, on top of each other. That works until the pile gets too deep. How well
you can pull one thing back out goes as:

    quality  =  square root of (how much memory you have / how much you piled in)

So there are exactly two ways to keep quality up as sequences get longer: **get
bigger**, or **pile in less**.

Getting bigger is the thing we cannot do. The whole point is tiny devices — the
billions of phones, routers and gadgets already on the internet. So it has to be
piling in less. **Be selective. Only store what matters.**

## The cheat

Earlier experiments tested that idea by simply telling the model which parts of
the input mattered. The task knows the answer, so we let the model peek.

**It worked spectacularly.** Devices holding *one number each* scored the same at
every sequence length we tried — the rows of the results table were identical to
three decimal places. Length stopped being a difficulty at all. A follow-up found
that how you split a machine into devices stopped mattering too.

We were always honest that this was a cheat, and we called it an oracle. A real
device on a real network has nobody to tell it which of its inputs are worth
keeping. The plan was always to replace the cheat with something real.

## The question

**How much of that advantage can a device recover on its own?**

We reported it as a ratio. Zero means the honest mechanism bought nothing over
storing everything. One means it matched the cheat.

We tried the two best candidates we had:

- **Keep what proved useful.** Store everything weakly, and when something you
  remembered turns out to have been right, promote it. This is a real mechanism
  brains use — it was in the papers John collected, and we read the paper rather
  than guessing at it.
- **Keep what was surprising.** John's suggestion: brains flood with chemicals
  when something unexpected happens, and that decides what sticks. We built it,
  including the compensating process it needs to avoid running away.

We wrote down what we expected before running anything, including which
prediction we thought was most likely wrong.

## The answer

**Neither of them recovers anything.**

Across 36 combinations — four sequence lengths, three forgetting rates, three
learning rates, three seeds each — the best recovery anywhere is **0.05**. Seven
of twelve cells are **negative**: the mechanism is worse than not bothering.

And the prize being left on the table is enormous. The cheat scores **0.998 to
1.000 everywhere**. Storing everything falls from 0.385 to **0.000** as sequences
get long. The gap reaches **0.996**, and honest mechanisms close none of it.

**It also gets worse as sequences get longer** — 0.05 at the shortest length,
−0.00 at the longest. The failure is largest exactly where the gate is most
needed. We predicted that in advance and named it as the outcome that would hurt
most. It held.

## What this means

Three of our best results — length stops mattering, allocation stops mattering,
and the tiny-device claim itself — **describe what a device could do if something
told it which of its inputs mattered. Nothing we have tried can tell it.**

They are not wrong. The arithmetic is real and a ceiling is worth knowing. But
they are not claims about a system anyone can build today, and until now the
project's summary read as though they were. That has been rewritten.

## What it does not mean

**Not that it is impossible.** Two mechanisms failing does not empty the space of
mechanisms. There are two we have not tried, and one is now the most interesting
idea in the project.

**Replay.** Every mechanism we tested has to decide *at the moment the input
arrives* whether it is worth keeping — which is the moment you know least. Brains
appear not to do this. They revisit stored traces later, offline, when the
consequences are known. We have no offline phase of any kind. That came out of
John's own reading list, and it is the natural completion of the mechanism we did
build.

**And the benchmark may be the problem, not the mechanism.** Our test sequences
are 92% random padding. Random padding is, by construction, the most *surprising*
content in the sequence — so a mechanism that keeps surprising things keeps the
noise and discards the signal. Real language is the opposite: rare words are both
surprising *and* the ones that carry meaning. That diagnosis has never been
tested, and the next experiment tests exactly it, with the padding drawn from a
realistic distribution instead of a uniform one.

If that moves the result, this whole finding is about our test rather than about
the idea.

## The honest summary

We spent a lot of the project's best results on a mechanism we assumed we could
replace later. We tried to replace it. We could not.

Knowing that now — with the number, the direction, and the pre-written prediction
it confirmed — is worth considerably more than continuing to build on top of it.
