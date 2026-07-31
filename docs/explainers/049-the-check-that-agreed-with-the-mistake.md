# 49. The check that agreed with the mistake

Our best result on family-tree puzzles leans on an arithmetic trick, and we
finally found out how far it travels. Not far. But the way we nearly concluded the
opposite is worth more than the answer.

## The trick

Every family relationship moves you a number of generations. Father is one up, son
is one down, sister is level. Walk any loop that comes back to where it started
and those have to cancel to zero.

That constraint is strong enough to fill in a step the system was never taught. It
is worth a lot: on the hardest puzzles it is the difference between getting about
six in ten right and about nine in ten.

It only works if the subject matter **has** such a quantity. Family trees do.

## Does anything else?

We took a large real-world knowledge graph — 272,000 facts about films, athletes,
countries, awards, companies — and asked the same question. Is there some
quantity that every relation moves you by, which cancels around every loop?

No. And not nearly: the arithmetic that would show a "close enough" version shows
nothing at all.

But a graph like that is not one subject. It is dozens. There is no reason film
credits and geography should share one accounting system, and maybe some corner
of it balances even though the whole does not.

So we split the graph into its thirty subject areas and asked each one separately.
Then we tried the best-evidenced 128 relations, then 64, then 32, all the way down
to 2.

**Nothing closes. Anywhere.** Two relations with 2,574 loops between them still do
not add up.

So the trick is a fact about family trees. It is not a general mechanism, and we
should stop describing it as one.

## The part that nearly went wrong

The first time we ran this, it said something much more exciting. Four subject
areas balanced — films, geography, education, government — with plenty of
evidence behind each.

It was completely wrong, and the mistake was small and dull. Some relations appear
in no loop at all inside their own subject area. A relation with no loops has
nothing constraining it, so the arithmetic counts it as "balanced" for free. It
means nothing.

The tool we already had was written to exclude exactly those, and its own
documentation explains why, naming the earlier graph where this first happened.
Our new script called that tool, **threw away its answer, and worked the number
out again** — badly, in three lines.

The giveaway, once we looked: the number of fake balances in each subject area was
exactly the number of relations with no loops. Two and two. One and one. Four and
four.

## The safety check made it look right

Here is the uncomfortable bit.

Alongside the real measurement we ran a scrambled version — same data, but with
the relations shuffled so any real pattern is destroyed. If the real result
survives scrambling, it was never a real result. This is standard and it is
usually the thing that saves you.

The scrambled version showed nothing. So the output read:

```
real data       four areas balance
scrambled       nothing balances
```

That is exactly what a genuine discovery looks like. The check appeared to confirm
it.

**It could not have done anything else.** Scrambling spreads every relation across
the whole graph, which means no relation is left without loops — which means the
bug that created the fake result **cannot happen in the scrambled version**. The
check did not fail to catch the problem. It removed the conditions for the problem
and then reported that the problem was absent.

## What actually caught it

Not the check, and not the tests. One line of the output disagreed with a tool we
had run on the same file an hour earlier. Two pieces of code that had to agree,
giving different answers.

That is all it was.

## The lesson worth keeping

A scrambling check asks *"is this pattern really in the data?"* It does not ask
*"is our code computing what we think?"* Those are different questions and only one
of them was being asked.

The habit that would have caught it immediately: when a new piece of code works out
a number that an existing tool already works out, **make it print both and compare
them.** We do this for approximations already. We had not thought to do it for a
three-line reimplementation, because three lines does not feel like something that
can be wrong.

The correct result took under a minute to compute. Getting it right took the rest
of the hour, and the difference was one row that did not match.
