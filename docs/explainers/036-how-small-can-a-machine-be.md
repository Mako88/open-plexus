# 036 — How small can a machine be?

## The right question

The premise of this project is that a capable system could run on the computers
people already own. And the billions of devices on the internet are, overwhelmingly,
**tiny**.

So the number that matters is not how accurate the whole thing is. It is: **how
small can one machine be and still be worth including?**

## The good news is better than expected

We split a system into pieces and shrank the pieces until it broke.

**At a problem length of 128, a machine holding ONE number is enough.** Two hundred
and forty of them, each storing a single value, working together, score 0.978.

That is as small as a machine can possibly be. There is no smaller. And it works.

## The bad news is the growth rate

| problem length | smallest machine that works |
|---|---|
| 96 | **1** |
| 128 | **1** |
| 192 | 4–6 |
| 256 | 12–15 |
| 384 | 20–24 |

Machine size grows roughly with the **square** of problem length. Double the
problem and each machine needs about four times as much.

So it is not that our machines are too big today. It is that they grow too fast.

## Two ways to answer, and both grow

There are two ways to get an answer: ask one machine, or pool what all of them
say. Pooling is always better — but it degrades about **twice as fast**:

- Ask one machine: size grows as length to the power **0.82**
- Pool everyone: as length to the power **1.94**

Pooling starts from a much better place and loses ground quickly. Its advantage
runs 13×, then 13×, then 3.3×, then 2×, then 1.25×. **It postpones the wall
rather than removing it**, and I described it as "the most promising direction"
two days ago, which I now withdraw.

## Where the growth actually comes from

Every one of those numbers is a power of *problem length*, and there is exactly
one reason.

**Our system memorises every single consecutive pair it sees.** In a 384-step
problem it stores 383 facts.

The problem only ever asks about **four**.

So **more than 98% of the interference — the babble that forces machines to be
bigger — comes from facts nobody will ever ask about.** We are drowning ourselves
in our own note-taking.

That is not a fact about distributed computing, or about our learning rule, or
about pooling. It is a fact about writing everything down. And it is the one thing
we have never tried changing.

By the interference law we measured, cutting 383 stored facts down to 4 is worth
roughly a **tenfold** improvement — acting directly on the quantity that sets the
minimum machine size.

Brains do this. They gate what gets encoded — by novelty, by surprise, by
attention. And "was I surprised?" is something each machine can work out for
itself, with no coordination, which is the constraint this whole project runs
under.

**That is the next experiment, and it is now the most important one here.**
