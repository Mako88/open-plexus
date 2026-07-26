# 033 — The wall that wasn't there

## I have to take something back

Last time I reported that this project had hit its first real failure: adding
machines stopped helping on long problems, and no amount of extra machines fixed
it.

**That was wrong, and here's the number that shows it.** On the exact problem
size where I reported a wall, **eight machines with 32 slots each score 0.999**,
comfortably over the bar.

The earlier experiment had fixed every machine at 16 slots and grown the network.
Sixteen slots is simply too small at that problem size — so what I measured was
the limit of a 16-slot machine, and what I *reported* was a limit of the whole
approach. Those are very different claims.

The verdict is withdrawn.

## What's actually true

Machines can't be arbitrarily small, and the floor rises as problems get harder:

| problem size | smallest machine that works alone |
|---|---|
| 96 | 16 slots |
| 192 | 32 slots |
| 384 | 32 slots |

So there *is* a real constraint. The question is how fast that floor rises — and
whether it outruns the growth in total capacity you need anyway.

If it rises slowly, you can keep adding machines forever and this works. If it
rises fast, then past some point you can only get further by making each machine
bigger, which defeats the purpose of running on ordinary computers.

## And that question came back unanswered

The measurement is `0.50`, give or take `0.50`.

The number we're comparing against is `0.37`, and it sits right in the middle of
that range. **So the experiment cannot tell the good outcome from the bad one.**

The reason is a design mistake I made: I tested machine sizes in doublings — 8,
16, 32, 64 — to measure something whose entire interesting range is one doubling
wide. **You can't measure a difference of 2× with a ruler marked in 2× steps.**

Fixing it needs finer steps (12, 16, 20, 24…) or a much wider range of problem
sizes. The second matters more, and costs more.

## The uncomfortable part

My analysis script printed this, against two of its three rows:

> AT THE EDGE OF THE GRID, breaking point not located

and then calculated an answer using those two rows anyway. It reported a
confident-looking figure for something it had just said it couldn't determine.

**This is the second time in two experiments.** Last time the same script threw
away the one problem size that failed, and I fixed that. The fix didn't help
here, because I'd written it against the specific shape of the first mistake
rather than the general one.

The general one, now written down as a rule:

> **A caveat printed next to a number doesn't attach to the number.** If a value
> is really a *bound*, the code has to refuse to use it as a value.

Three fixes went in, and one of them immediately found something else: the
learning-rate check had been looking at the whole experiment at once, where a
single good result anywhere made everything look fine. Checked row by row, one
row turned out to be pinned. Harmless this time — but it had been reporting clean.

## Where this leaves things

Better than last time, and less certain. Four gates pass. The fifth is open rather
than failed, resting on a question a badly-scaled experiment couldn't answer.

I'd rather report that than the tidy failure I reported yesterday.
