# 5. What makes a fair test?

[Explainer 4](04-how-well-know-if-were-wrong.md) said the previous project's
test was too easy to show anything. This one is about how we pick a better one —
because the obvious fix turns out to be wrong.

---

## The obvious fix, and why it fails

The obvious response to "the test was too easy" is **make the test harder.**

That doesn't work, and understanding why is the whole point of this explainer.

Recall the setup: we compare a **random, untrained** network against a **strong
conventional** one, and we need a big gap between them. The untrained one scored
0.802 — far too high, leaving no room.

Now, you *can* make that test much harder. Make the sequences longer, add more
noise, whatever. And the untrained network's score will drop.

**But so will everything else's.** If the task is hard in a way that the
untrained network already handles gracefully, then making it harder just moves
everyone down together. The gap doesn't open. You've made the test more
difficult without making it more *informative*.

## The actual fix

> **Make the test hard in a direction the untrained network has no answer for.**

That's a different instruction, and it requires knowing what an untrained
network is secretly good at.

## What a random untrained network is secretly good at

This is the surprising bit. A big tangle of random connections is genuinely
good at:

- **Mixing recent inputs together** in complicated ways.
- **Spreading things out** so that a simple reader can tell apart things that
  looked identical going in.
- **Holding on to the recent past**, in a blurry fading way — like an echo.

That's not nothing. It's a real, respected technique.

**And that's exactly what the old test asked for.** The test measured
short-term mixing of recent inputs. The untrained network is a short-term mixer
of recent inputs. The test was, in effect, checking whether the system could do
the one thing it already did for free.

## What it's bad at

Here's where the room is:

- **Remembering things from a long time ago.** The echo fades.
- **Choosing what to remember.** ← this one matters most
- **Looking something up by content** — "what was that thing paired with?"
- **Recombining familiar pieces in a new arrangement.**

## Why "choosing what to remember" is the interesting one

A random network's memory is **indiscriminate**. It holds on to everything
recent, all fading at the same rate, whether it mattered or not. It has no way
to decide that one thing is worth keeping and another is worth dropping.

Its capacity is limited, so it spends that capacity on noise just as readily as
on signal.

**Deciding what's worth keeping is exactly the sort of thing learning is for —
and exactly what an untrained network structurally cannot do.**

So if we build a test where most of what arrives is irrelevant and a small part
matters much later, we've aimed the difficulty right at the gap we want to open.
The untrained version should do badly *for a reason we understand*, rather than
badly by accident.

## The test we're proposing

**Associative recall.** It works like a memory game:

> The sequence shows you pairs — `apple→3`, `river→7`, `candle→2` — a bunch of
> other stuff goes by, and then it asks: **`river→?`**

To answer, you must have kept `river→7` specifically, while discarding
everything else, across a long gap. That's selective retention and
look-up-by-content — two of the four things on the "bad at" list.

Three reasons we like it:

1. **It aims at the right weakness.** Not "harder," but hard in the direction
   that untrained networks can't handle.
2. **The task and the goal are the same thing.** Our leading idea for how
   learning works here is "each part predicts what it'll see next" — and this
   task *is* predicting what comes next. No translation needed, which means if
   it fails we know what failed. (If the task were scored some other way, a
   failure could mean *the learning doesn't work* or *the learning works but
   doesn't help on this metric*, and we couldn't tell which.)
3. **It has knobs.** How long the gap, how many pairs, how much noise. Which
   brings us to the last idea.

## Don't pick a test — pick a dial

There's a trap on the other side, and it's worth stating plainly because it
nearly cancels the first one.

If we make the test *too* hard, only the heavyweight conventional method can do
it at all. Then our approach fails — but so would anything without a data
centre, and we've learned nothing about our actual question. We'd have traded a
test that couldn't show success for one that couldn't show failure *usefully*.

There's no way to know in advance where that line is.

**So we don't pick a single test. We pick a test with a difficulty knob, and
turn it.** The result isn't a pass or a fail — it's a *curve*: how the untrained
version and the strong version each perform as the task gets harder. Then we
work in the region where the gap is widest, and we can revisit it later.

That's a better instrument than any single setting, and it costs almost nothing
extra to build.

## Everything above is a prediction

Worth being blunt: **nothing in this explainer has been measured.** That a
random network fails at associative recall is an expectation based on how these
things work and on published results — not something we've run.

We've written the predictions down in advance, specifically so we can't quietly
adjust them later if the results disagree. And the most useful outcome would be
the surprising one: **if a random untrained network turns out to do well at
this, our whole picture of what such networks can't do is wrong** — and that
picture is currently steering the entire choice of test. Finding that out early
would be worth more than being right.

---

*Next: nothing yet — this is the current edge of the project. See the
[index](README.md).*
