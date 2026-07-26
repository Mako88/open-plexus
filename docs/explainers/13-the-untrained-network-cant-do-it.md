# 13. The untrained network can't do it at all

We built the random untrained network — the thing that has to score *badly* for
our benchmark to be any use — and measured it.

**It scores at chance. It isn't doing the task poorly; it isn't doing the task.**

That's the outcome we wanted. But the interesting part of this explainer is what
we did *before* believing it.

---

## The setup

[Explainer 5](05-what-makes-a-fair-test.md) explained why a random untrained
network matters: it's surprisingly capable, and if it already scores well on our
test then there's no room left to show that learning helped. The previous project
died of exactly this.

So we built one. A tangle of random connections that never changes, with a
simple trained reader attached to its output. **Nothing about the tangle
learns** — only the reader does.

Then we asked it to play the memory game.

---

## The number

At our standard setting:

| what | score |
|---|---|
| pure guessing | 0.125 |
| **the untrained network** | **0.180** |
| the one-line cheat trick from [explainer 12](12-what-does-knowing-nothing-score.md) | 0.344 |
| perfect | 1.000 |

**It's barely above pure guessing, and it loses to the one-line trick.**

We'd predicted somewhere between 0.40 and 0.70. Wrong — and wrong in a direction
we'd flagged in advance as possible, which is at least the right kind of wrong.

Making it bigger barely helps. Doubling the network from 64 units to 128 buys
**0.008**. Whatever is missing, it isn't size.

---

## The part that matters: not believing it yet

Here's where this gets interesting, and it's the habit rather than the result.

We had *predicted* the untrained network would fail. Then we ran it and it
failed.

**That's the most dangerous possible moment.** Because there are two completely
different reasons a number comes back near zero:

1. The network genuinely can't do the task. *(What we predicted.)*
2. **Our measuring apparatus is broken and would report near-zero for
   anything.**

From the outside, those are *identical*. A wire left unplugged between the
network and the reader produces exactly the number we expected to see.

And when a result confirms what you predicted, **nobody looks harder.** That's
the whole problem. A wrong result that surprises you gets investigated. A wrong
result that agrees with you gets written up.

## So we tested the apparatus

We asked the exact same pipeline — same network, same reader, same training,
same held-out data — to answer a *different* question:

> **"What symbol are you looking at right now?"**

That's information the network definitely has. It's the thing being fed into it
this instant. If the reader can't recover *that*, nothing is connected properly
and every other number is meaningless.

**It scored 1.000. Perfect. Every time.**

So the pipeline isn't merely working, it's flawless. The reader can extract what
the network holds. It just turns out the network doesn't hold the answer to the
memory question.

**0.180 is a fact about the network, not about our plumbing.** Now we can
believe it.

*(One exception, worth noting: the smallest network we tried, at 16 units,
scored only 0.777 on the check. 16 numbers can't cleanly distinguish 41
different symbols. That configuration is limited by the apparatus as well as the
task, so its result doesn't belong beside the others — and we say so rather than
quietly including it.)*

---

## What we've actually gained

**Room.** The gap between the untrained network and perfect is **0.82**.

For comparison, the previous project's benchmark had about **0.19** of total
room, and non-learning tricks had already eaten 40% of it. Ours has more than
four times the space, and none of it is spent.

That was the entire purpose of gate zero, and on this measure it has passed
handsomely.

---

## And a correction to our own plan

Here's a nice consequence nobody anticipated.

We'd been assuming the untrained network would be *the* thing to beat — the
floor any real method must clear.

**It isn't. It loses to the one-line trick.** 0.180 versus 0.344.

So the bar isn't the untrained network. It's whichever is higher — and that's
the trick. If we later build something scoring 0.30 and announce "we beat the
untrained network's 0.180," we'd be celebrating something a five-line heuristic
already does better.

Our written plan had this ordering wrong. It's now corrected in the plan itself,
not just noticed here.

---

## What we still can't claim

**We have not shown this task is learnable.**

We've shown two things: that it's *answerable* (the cheat that's told the answers
gets 100%), and that an untrained network can't do it.

Neither of those means a trainable system *can*.

And this matters more now, not less. A test where the untrained control sits at
chance is only useful if something trainable reaches high. **If nothing can do
it, then 0.82 of "room" is unreachable rather than available**, and every future
failure becomes uninterpretable — we'd never know whether our idea failed or the
task was simply impossible for everything.

Published work says a standard heavyweight model solves this perfectly. But that
was their version of the test, not ours.

**Until something is trained on our own generator and gets close to perfect,
gate zero isn't finished** — and we're recording it as unfinished rather than
declaring victory on the strength of a very encouraging 0.82.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
