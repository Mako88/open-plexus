# Two ways to pick what to keep

Our model has a small memory and a firehose of input. Most of what goes past is
noise. Some of it matters. It has to throw away almost everything, and it has to
decide without being told.

There is one break in its favour. Something in the stream marks the good bits —
but it arrives *after* them. Think of a smoke alarm: by the time it goes off, the
thing that caused it already happened. So the model keeps everything for a short
while, and when the alarm sounds it saves a handful of recent things and dumps
the rest.

**"A handful of recent things" is where it goes wrong.**

## The clock, and why it needs a lucky guess

The obvious rule is: when the alarm sounds, keep the last twenty steps.

Twenty is a guess. If the thing that mattered happened four steps ago, you keep
it — along with sixteen pieces of junk that came with it, and the junk crowds out
the real memories. If it happened forty steps ago, you keep twenty pieces of junk
and miss the thing entirely.

We measured this. Wherever the number was big enough, the rule worked, and
wherever it was too small, it did **worse than not filtering at all**. And the
number that works depends on something the model has no way of knowing.

## The mark, and why it should have fixed it

So we tried counting in *things* rather than in *time*. As information goes past,
put a mark on the few items that look like real content rather than noise — we
found a way it can tell, roughly, from a signal it already computes. When the
alarm sounds, keep whatever is still marked, however long ago it happened.

Four marks reach back as far as four real items go, which on our test data is
about 124 steps. Four *steps* reach back four. No guess needed.

## What we found instead

It half worked, and the half that failed is the interesting part.

Marking does find real content. It is about eight times better than picking at
random. But the task does not ask for *some* real content — it asks about **one
particular item**, the one the alarm was about. And nothing in the item itself
says so. The only thing that identifies it is *that it happened just before the
alarm*.

Which is exactly what the clock rule uses.

So the two rules are not a better and a worse version of the same idea. They
answer different questions:

- the mark asks **is this worth keeping at all**
- the clock asks **is this the thing the alarm was about**

A memory needs both answers, and each rule has only one of them. When we counted
directly, a well-set clock found the right item every single time, and marking
found it in under half of the attempts. Where the clock was set wrong, it found
it *never*, and marking still found it about a third of the time.

## The bug that taught us the most

Biology's version of a mark fades over time, and our first attempt did not. When
we added fading, it changed nothing. Every number came out identical — not close,
identical.

That is a bigger deal than a wrong answer. A setting that does nothing looks
exactly like a setting that works, and every measurement taken while it was
broken would have been quietly meaningless. We found it only because a dial that
should have changed something didn't.

It turned out we were fading marks in the wrong direction: instead of letting old
marks expire, we were making them permanent. Fixed, fading matters enormously —
it is the difference between finding the right item 9% of the time and 44%.

## What happens next

We have written down what we expect before running the real test, which is the
rule here: a prediction made after seeing the answer is not a prediction.

We expect the mark to lose to a well-set clock, and to beat a badly-set one. If
that is what comes back, the honest conclusion is that we spent this round
building the *second* half of a gate whose first half already worked — and the
next thing to build is the one that uses both.
