# 038 — How should a machine spend what it has?

## The question

Real computers are not all the same size. A phone holds a little, a desktop a lot.
And a machine that can hold a lot has a choice: run **one big node**, or **many
small ones**?

It matters because the nodes one machine runs can share their answers for free —
they're in the same process. Nodes on *different* machines have to talk over the
network. So "how many nodes does this machine run" and "where does a group end"
are the same question, and nobody had asked it.

A machine holding 64 units of capacity has seven options: one node of 64, two of
32, four of 16, eight of 8, sixteen of 4, thirty-two of 2, or sixty-four of 1.
Same memory, same arithmetic — they differ only in how the machine's answers get
combined.

## The answer is two answers

**Without selective storage, it matters enormously.**

| 64 units spent as… | score |
|---|---|
| one node of 64 | **1.000** |
| one node of 32 (+ 32 unused) | 0.981 |
| four nodes of 16 | 0.756 |
| sixty-four nodes of 1 | **0.583** |

The rule is blunt: **as few and as wide as you can manage.** Splitting costs you
almost half.

**With selective storage, it doesn't matter at all.**

The biggest gap between the best and worst way of spending *any* amount of
capacity is **0.031**. And sixteen units is enough however you divide it — one
node of sixteen and sixteen nodes of one both score a perfect 1.000.

## Why that's a bigger deal than it sounds

We already knew selective storage removes problem length as a difficulty — that
was the last finding. **It turns out it removes the allocation problem too.**

Which means: **a network of wildly different machines needs no policy.** Every
machine spends what it has however it likes, and it works. No coordinator
deciding who runs what, no negotiation about node sizes, no configuration.

For something meant to run on whatever hardware people already own, that's close
to the best possible answer. One mechanism, two problems gone.

## A prediction I got wrong, and a number that checked itself

I predicted the best allocation would be the same at every capacity. It isn't —
without the gate, the winner tracks capacity exactly: at 4 units the best is one
node of 4, at 8 it's one node of 8, and so on.

But that's not really a preference for big nodes. It's an older result reappearing:
we'd already measured that a node needs to be at least ~24 wide to work alone on
this problem length. So the winner is always "the widest node this machine can
afford", and once you can afford one over the threshold, the ranking stops moving.

Which gave an unplanned check. In this experiment, nodes of width 32 jump to
0.960 where nodes of 16 manage 0.704 — the threshold sits between them. A
completely separate experiment, asking a different question with a different grid,
had put that threshold at "between 20 and 24". **The two agree.**

## The honest caveat

With the gate on, everything above 16 units of capacity scores 1.000 — so seven
of the nine rows are ceiling, and the real comparison lives in the small
capacities below it, where the gaps are 0.003 to 0.031.

So "allocation doesn't matter" means: *below the point where everything works,
it's worth about 0.03; above it, nothing.* Still the answer — but measured over a
narrow band, not proven across every difficulty.

## What's still missing

**Every node in this experiment is the same size as every other.** That is exactly
what a real network isn't. Mixed machine sizes is the obvious next step, and two of
the brain papers John found are specifically about how the brain's own columns are
*not* uniform — so there may be something to learn there rather than just measure.
