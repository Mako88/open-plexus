# 48. It was being told where to stop

The previous explainer ended by naming two hints our system gets that the systems
we compare against do not. This is what happened when we measured the first one.

## The hint

To answer *"Alice is Carol's what?"* the system walks along the chain of stated
relationships — Alice to Bob, Bob to Carol — and then works out what the whole
walk adds up to.

To do that it has to stop walking at the right moment. At the moment we tell it
when: we read the puzzle in advance, count the links, and hand it the number.

Nobody had ever measured what that was worth. It might be a convenience. It might
be the answer.

## What it was worth

We gave the walk a number that was wrong by **one**, and nothing else changed.

```
told the right length      0.91
told one link too many     0.16
knowing nothing at all     0.06
```

One extra step takes it from nearly right to nearly nothing. The hint was not a
convenience.

## So we tried to remove it

The idea was simple and it did not need any new information. A question names
both people it is asking about, so the walk already knows where it is trying to
get to. Let it walk as far as it likes and stop when it arrives.

We gave it two budgets: walk at most ten steps, or at most fifteen. The deepest
puzzle in the data is ten links.

```
at most 10 steps    0.87
at most 15 steps    0.43
```

Two versions of the same idea, differing only in a limit that should not matter,
and they disagree by more than half.

**The ten-step version was not working. It was being told the answer again**, more
quietly. On the ten-link puzzles it cannot walk further than ten, so the limit and
the answer are the same number. Take that coincidence away and it falls apart.

We had written down before running it that this was the thing to watch for, which
is the only reason it was caught rather than published.

## The wrong explanation we nearly kept

There was an obvious culprit. When the walk compares two possible routes it scores
them by a calculation that is not adjusted for length, and longer routes happened
to produce bigger numbers. Long routes winning by size rather than by being right
would explain everything.

It looked convincing. Longer routes really did produce bigger numbers, and the
walk really did pick the longest available route most of the time.

So we fixed the scoring to remove the size effect and measured again. **It changed
nothing at all** — the same routes won.

The size difference was real and was not the cause. Had we not checked, the next
thing we built would have been a repair for a problem that was not there.

## Where this leaves us

The honest version of our result is now:

> Given the length of the chain, the system walks a ten-link relationship chain
> and names it correctly about 91% of the time.

That is a real capability. It is also a statement about a system with a crutch,
and the published systems we compare against do not have one.

We do not currently know how to take the crutch away. Scoring the destination —
the obvious approach, the one that needs no extra information — has been measured
and does not work.

## What is left to try

One idea, and it is not a patch.

Elsewhere in this project there is a small learned component whose whole job is to
decide when to stop — it works, and it works on depths it was never trained on. It
has never been pointed at this kind of walking.

That is worth trying. But it is a thing to build rather than a thing to adjust,
and there is a more dangerous question ahead of it in the queue: whether any of
this survives on data that is not family trees. That one can kill the idea
outright, so it goes first.

## The part worth keeping

Finding out that a result depends on a crutch is a better day's work than adding a
decimal place to it. The number we would have quoted was not wrong — the walk
really does score 0.91 — it just meant something narrower than it looked.

And the wrong explanation is the more useful half. It took two minutes to check
and it was convincing enough that skipping the check would have felt entirely
reasonable.
