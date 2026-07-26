# 24. The number was a setting we picked

This one covers three experiments that turned into one story: a good result, a
puzzle it left behind, and what chasing the puzzle revealed about the number this
project has been reporting all along.

**The short version: machines leaving is survivable. And the headline figure was
measuring a knob we set once and never looked at again.**

---

## Part one: machines leaving

People close their laptops. The whole project assumes that's *normal* rather than
an accident — and that assumption had never once been tested, because nothing had
ever left.

This is different from the previous experiment, which lost individual *messages*.
A lost message is transient. A machine leaving takes its share of the system and
never comes back.

We removed a fraction of the machines halfway through training and kept going.

| machines lost | immediately after | after retraining |
|---|---|---|
| 12.5% | 0.970 | 0.991 |
| 25% | 0.943 | 0.986 |
| 50% | 0.749 | **0.924** |
| 75% | 0.365 | 0.578 |

Baseline with nothing lost: 0.992.

**Half the machines can vanish permanently, mid-training, and it comes back to
0.924 within a few passes.** Losing a quarter costs 0.006.

Why it heals: this design keeps almost nothing permanent. The working memory is
rebuilt from scratch for every sequence, so a departing machine carries away *no
stored memories* — there are none to carry. It takes room, and the room that
remains relearns.

That's the result the churn question was written to get, and it's a good one.

---

## Part two: the bit that didn't add up

But one number didn't fit.

A model reduced *down* to a small size scored **0.92**. A model *born* at that
same size scored **0.22**. Same design, same data, same amount of training.

We had two theories.

**Theory one: it's just slow.** Maybe the small model isn't too small — maybe it
needs longer, and we stopped early. If that were true, our headline number ("four
to six times more room") would be about patience, not room.

We trained the small model **eight times longer**. Result:

| epochs | 1 | 8 | 32 | 64 |
|---|---|---|---|---|
| accuracy | 0.185 | 0.225 | 0.192 | **0.180** |

Utterly flat. It converges within one epoch and sits there forever. **Not
slowness.**

**Theory two: it inherited something.** The shrunken model had trained at full
size before losing half of itself. Maybe its reader kept something useful.

We tested that by shrinking a model *before* any training at all.

---

## Part three: neither theory was right

Four versions, all ending up the same size, differing only in how they got there:

| version | accuracy |
|---|---|
| born small | **0.263** |
| shrunk before training | 0.963 |
| shrunk during training | 0.965 |
| **born small, with one number changed** | **0.960** |

Look at that last row.

It's a **normal, born-small model**. Nothing was removed from it. It had no
head start. The *only* difference from the first row is that the random numbers
it was initialised with were multiplied by **0.71**.

**0.263 → 0.960.**

Not capacity. Not inheritance. Not shrinking. **A scaling constant.**

---

## What that means for the headline

When I set this up, I chose to scale those initial random numbers by
`1 ÷ √(size)`. There's a standard reason to do that — it keeps them at a
consistent magnitude no matter the size — and it does exactly what it says.

What it *doesn't* do is keep the *learning* well-behaved. The learning step is
proportional to the **cube** of that scale. So the scale and the learning rate
are entangled, and I'd fixed one at a guess while measuring the other.

Which means **the width curve — the one that produced "locality costs four to six
times more room" — was substantially a curve about a number I picked out of the
air.**

It's flagged as **must not be quoted** in all three places it appears. Including
in what I told John a few hours ago, when he asked how this compares to
conventional neural nets.

---

## The trap in fixing it

Here's the part worth being careful about, because the temptation is real and the
cheat would be invisible.

The obvious fix: tune our setting, re-measure, report a better number.

**That would reproduce exactly the mistake being corrected.** The comparison model
has the *same* untuned knob — its own initialisation scale, fixed at one value
through every experiment and never swept. Tune ours and not theirs, and you get a
flattering figure by precisely the mechanism that produced the wrong one. And
nobody could tell from outside, because the result would look carefully measured.

So both get swept, both get taken at their own best, and the honest price is
where those two curves cross. That's running now.

The candidate rule, which goes into our standards if it turns out to be general
rather than a one-off:

> **A setting tuned on one side of a comparison must be tuned on all of them.**

And the prediction is written down in advance, including the unwelcome version:
if the comparison model improves *more* than ours does, the price of locality is
**larger** than we reported, and that gets said exactly as loudly.

---

## What survives

Being precise, because it would be easy to over-read this.

**The delay result is untouched.** Bit-identical learning below a stated network
delay comes from how messages are addressed, and involves no scale at all.

**The churn result is untouched in shape.** "Half the machines can leave" was
measured as a fraction of a working model and remains a fraction of a working
model. The specific sizes it was measured at will need restating once the curve
is corrected.

**One number is damaged**, and it's the one that got quoted most.

---

## The pattern, again

This is the fourth distinct species of error this project has caught, and it's a
new one.

Previously: believing an unread source; having a measurement and not using it;
reasoning correctly to a sign-flipped conclusion; and a plausible mechanistic
story that failed its own prediction.

This one is different again. **Nothing was wrong. A default was never
questioned.** No test could fail, because the code did exactly what it was told.
The setting was reasonable, standard, and had a textbook justification. It was
simply never *swept*, and it turned out to matter more than the thing being
measured.

It only surfaced because a churn result produced a number that didn't fit, and
because the inconsistency got chased instead of shrugged at.

**The lesson isn't "tune your hyperparameters."** It's that a number nobody
varied is not a measurement — it's an assumption wearing a measurement's clothes,
and it sits there looking like data.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
