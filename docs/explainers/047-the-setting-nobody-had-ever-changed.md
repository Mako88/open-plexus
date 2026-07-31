# 47. The setting nobody had ever changed

We have a number for this system on a reasoning puzzle other people invented:
**0.8578**. Published systems on the same puzzle score between about 0.39 and
0.97, so it looked good, and it had been sitting in our records for a while.

This is what happened when we finally reported it properly.

## What the puzzle is

You are told a chain of family relationships — *Alice is Bob's mother, Bob is
Carol's father* — and asked how the two ends are related. The chains get long:
some are ten links.

Our system answers by walking the chain one link at a time and then working out
what the whole thing adds up to.

## Three things wrong with a single number

The 0.8578 was one average over 1,146 puzzles. That hides a lot.

**The puzzles are not equally hard.** A two-link puzzle and a ten-link puzzle are
not the same question, and averaging them together produces a figure that
describes neither.

**It was measured once.** One random starting position for the model. If that
start happened to be a good one, the number is luck.

**It was measured at settings copied from an older experiment.** The size of the
model's memory — how much room it has to work in — had been set to one value
years of experiments ago, and nobody had ever tried a different one.

## The third thing turned out to be the whole story

We tried four memory sizes. Here is the score on the hardest puzzles, the
ten-link ones:

```
memory size 32     0.19
memory size 64     0.72     <- the value everything had been measured at
memory size 128    0.87
memory size 256    0.91
```

At the smallest size the system barely works at all. At the size everyone had
been using, it gets about seven in ten. Give it four times as much room and it
gets nine in ten.

**Nobody had varied this.** It was not a considered choice that turned out badly
— it was a number copied from a different experiment, for a different task, and
then carried forward silently into every result since.

## And the single measurement was lucky

Running eight different random starts instead of one, at the old memory size, the
ten-link score ranges from **0.605 to 0.78**. The one that had been published was
the best of the eight.

Some places looked perfect on the original run — a hundred percent — and turned
out to be as low as 81% on other starts. Three runs would have missed that most
of the time, which is why we now use eight.

## The odd part: reporting it honestly made it better

You would expect a number to get worse when you stop averaging over easy cases
and start being careful about luck. This one got better, because the honest
version also let the memory size move — and at a sensible size, the hardest
puzzles score **0.91** where the flattering old average said 0.86.

The old number was not exaggerating. It was being dragged down by a setting that
the easy puzzles could absorb and the hard ones could not.

## What this does not mean

The system is still being helped in two ways that the published systems it is
being compared against are not.

**It is told how many links the chain has** before it starts walking. We measure
that next.

**It is told that these relationships add up.** *Father* is one generation up,
*son* is one down, and the arithmetic has to balance — that rule was supplied by
hand rather than discovered. Whether anything like it exists in a domain that
isn't family trees is the question after that.

So this is not "our system nearly matches the best published result." It is
"our system, given two hints, on the puzzles it is best at, lands in the upper
part of the published range." The next two experiments exist to remove the hints.

## The lesson worth keeping

The mistake here is not that someone picked a bad memory size. It is that a value
chosen once, for a good reason, in a different context, becomes invisible. It
stops looking like a choice and starts looking like the background.

Every individual experiment that used it was sound. None of them could have
caught this, because none of them varied it. The only thing that finds a constant
like that is deliberately going looking for constants — and this project has now
had to learn that four separate times.
