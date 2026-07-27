# 29. The signal was there, pointing backwards

*Six mechanisms have failed to make this thing selective. This is the first
measurement that says why two of them failed — and it says the answer was sitting
in a number we'd been reading upside down.*

---

## The setup

Last time ([explainer 28](28-a-window-has-to-be-the-right-size.md)) we established
that "keep the last N things" can't work, because N has to match a delay nobody
knows in advance. The fix is a **tag** — mark one specific thing, rather than a
stretch of time.

But a tag has to be set on *something*. Some signal, available to the node in the
moment, has to say "this one's worth marking."

So before building it: **is there such a signal at all?**

## Reading the recipe first

The task generates its data by picking, at random, which facts get rewarded. The
rewarded ones and the ignored ones are drawn from the same pool and look
identical.

Which means **nothing can predict which facts will matter**. Not because we're
bad at it — because the task was built that way on purpose. That's the honest
version of the problem: in the real world you don't get a hint either.

So a tag can't be smart about *value*. But it can be smart about something else:
telling a **real fact** apart from **filler**. And that turns out to matter more
than it sounds, because of a number nobody had bothered to print:

**31 steps of filler per actual fact.**

A window of 64 steps holds *two facts and sixty-two pieces of junk*. That's the
whole of last time's result, restated. A tag with four slots holding four *facts*
covers 124 steps. A window with four slots holds four *steps*.

## What we measured

For every signal a node can actually compute about itself, we asked: does it tell
a real fact from filler? The score is 0 to 1, where **0.5 means no information at
all**. Below 0.5 isn't failure — it means the signal works *backwards*, which is
just as usable if you know.

| signal | narrow node | wide node | |
|---|---:|---:|---|
| surprise | 0.50 | 0.49 | nothing |
| **retrieval strength** | **0.29** | **0.22** | **backwards** |
| how unusual the surprise was | 0.38 | 0.33 | backwards |
| was the prediction right | 0.49 | 0.49 | nothing |
| how recent it was | 0.48 | 0.48 | nothing |

## Four things in there

**1. There is a signal — and it's the one nothing was built on.**

When the memory is asked about something, the answer comes back with a size.
Filler repeats constantly (forty junk words shuffled through seven hundred slots),
so a junk word has been stored many times over and comes back **loud**. A real
fact's cue appears once, so it comes back **quiet**.

So the rule is: **keep the quiet ones.**

And here's the part that stings. One of the six failed mechanisms — competitive
capture — ranks candidates on exactly this number and keeps **the loudest**. It
was pointed backwards the whole time. We'd filed that failure under a
statistical explanation about how rare the important things are. The real reason
was simpler and more embarrassing.

It also gets *better* on a bigger node (0.29 → 0.22), which is the opposite of
what I predicted. And the six random restarts agree to within ±0.02, which is the
tightest agreement anything in this project has produced.

**2. "Predict the future and compare" carries nothing.**

John asked whether this idea deserved another look, given how many bugs have been
fixed since it last failed. It absolutely did — and this is the cheapest possible
version of that second look: instead of building the mechanism again and seeing
whether it works, just score the raw signal against the truth directly.

It's 0.49. No information. The mechanisms built on it weren't failing because of
the bugs. They were failing because the signal isn't there.

That's a much more useful negative than the previous ones, because it can't be
blamed on the machinery around it.

**3. Surprise is nothing — but *unusual* surprise is something, backwards.**

Raw surprise: 0.50, useless. But *how far surprise sat from this node's typical
surprise*: 0.38 and 0.33 — backwards again. Real facts sit close to the node's
normal level. **Filler is what lives in the extremes.**

Which explains a second failed mechanism. The salience gate fires on both
extremes — very surprising and very unsurprising — on the theory that the boring
middle isn't worth keeping. It had it exactly inverted: **the extremes are the
filler.** It was carefully selecting junk.

**4. Recency carries nothing at all: 0.48.**

A window ranks things by how recent they are, and nothing else. That number says
recency contains no information about whether something is a real fact.

So a window's only ever virtue was *reaching* the thing. It never *selected*
anything. Which is last time's result from the other side: once your window is
long enough to cover the delay, every extra step you add is admitted by a
coin flip.

## What I'm not claiming

- **A separable signal is not a working mechanism.** 0.22 is a good score and
  it's not zero. A tag built on it still has to beat the window's existing 0.25,
  and nothing here promises that.
- **One task, one density.** The signal exists *because* filler repeats and facts
  don't. On real text, the frequent words are often the useful ones, and this
  would flip. We already have a separate finding warning about exactly that.

## One more thing, about method

The trial run used 8 sequences and put one of the control numbers at **0.617** —
which looks like a discovery. At 32 sequences it was 0.541. Across six restarts,
0.510. Nothing changed but the amount of data.

That 0.617 is written into the permanent record on purpose. It's what an
under-powered measurement looks like sitting next to the same measurement done
properly, and **the gap between them is bigger than several results this project
has previously taken seriously.**

## Next

Build the tag. Keep the quiet ones.

---

*Previous: [28. A window has to be the right size](28-a-window-has-to-be-the-right-size.md)*
