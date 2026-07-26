# 26. Three wrong answers and a right one

A question stayed open for four experiments. We got it wrong three times. The
fourth answer was sitting in our own code the whole time.

**And the answer turns out to be the best scaling result the project has
produced.**

---

## The question

Our rule needs a certain amount of room to work — below it, it fails; above it,
it's fine. **What decides how much?**

That matters practically. If the requirement balloons as you give the system more
to handle, it can't scale. If it grows gently, it can.

## Attempt one: how much you ask it to remember

Obvious guess. It stores things by layering them into one grid, so storing more
should crowd it more.

**Flat.** Four things or sixteen — the requirement didn't move. And at small
sizes, storing *more* was actually *better*.

## Attempt two: how many different symbols exist

If it's not the quantity stored, maybe it's the variety. More distinct symbols
means more chances for two of them to be confusable.

**Flat.** We swept the alphabet by a factor of sixteen. The requirement didn't
move at all.

At that point we had **no explanation whatsoever** for a central property of our
own system — and we'd written down, before running, that this outcome would be
the most informative and least comfortable available. It was both.

## Then we read the code

Three lines, which had been there since the day it was written:

> **Store an association between this thing and the previous thing.**
>
> Every step. Not just the meaningful ones — *every* step, including all the
> padding.

So in a 96-step sequence, the memory is holding **95 associations**. Four of them
are ones anyone will ever ask about.

Which explains both failures instantly.

- **Asking about four things instead of eight** changes 4-out-of-95 to
  8-out-of-95. A query is competing against ninety-odd others either way.
- **The alphabet** doesn't change the count at all.

**The crowding explanation was right the whole time.** It had just named the
wrong knob — and every experiment "refuting" it had been turning knobs that
barely touch the thing that matters.

The real load is **how long the sequence is** — which had been fixed at 96 in
*every single experiment this project has ever run*, including the ones that
supposedly disproved crowding.

---

## Attempt three: sequence length

| sequence length | associations held | room needed |
|---|---|---|
| 48 | 47 | **22** |
| 96 | 95 | **26** |
| 192 | 191 | **34** |
| 384 | 383 | **48** |

**It moves.** The first task setting in this entire project that does.

## And it moves gently

We predicted the requirement would grow *in proportion* to the load — eight times
the sequence, eight times the room. There's a standard argument for that, and
we'd written it out.

**Wrong, and wrong in the good direction.**

> **Eight times the sequence costs 2.2 times the room.**

That's roughly a **cube root**. Not the straight line we predicted.

Which means: going from 384 steps to 384 *thousand* — a thousandfold increase —
would need something like ten times the room, if the pattern holds that far. *If*
being load-bearing: extrapolating three decades past what we measured is
arithmetic, not evidence, and it's labelled that way in the record. But the
*shape* is encouraging where a straight line would have been fatal.

## For comparison

A conventional attention model **keeps every position it has ever seen.** Its
memory grows *linearly* with the stream, and its time cost *quadratically*.

Ours grows as roughly a cube root.

That's worth saying carefully rather than loudly — it's one axis, on one task,
and our rule still needs four times the room at the length we measured the price
at. But on *this* axis it isn't merely competitive. It's structurally better.

**Which immediately undermines our own headline.** The "four times" figure was
measured at *one* sequence length. If the two architectures scale differently in
stream length — and they must, given one is linear and the other is a cube
root — then four-times is **a point on a curve, not a constant.** Re-measuring it
across lengths is the obvious next experiment, and it could move the number in
either direction.

---

## What this episode actually taught

Not "read your code," though that would have helped.

The useful lesson is about **how long a correct idea can look refuted.**

The crowding explanation was right from the start. It survived being proposed,
then was "disproven" twice by careful experiments that were properly designed,
correctly executed, and honestly reported. Both refutations were *valid* — the
things they measured really are flat. They just weren't measuring the thing that
mattered.

There was no way to catch that by being more rigorous. Every individual step was
sound. What was needed was to notice that a variable *nobody had ever varied* was
doing the work — and it stayed invisible precisely because it was constant
everywhere, in every experiment, by an accident of how the first one was set up.

That's now happened twice in this project. The initialisation scale was the same
shape: a constant, never questioned, quietly determining the answer.

**A variable that never changes doesn't look like a variable. It looks like the
background.**

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
