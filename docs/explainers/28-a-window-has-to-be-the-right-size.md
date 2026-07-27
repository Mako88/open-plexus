# 28. A window has to be the right size

*The result that decides what gets built next. It came out the opposite of
convenient, which is usually a good sign.*

---

## The setup

The thing we're trying to build has a memory that fills up. It can't keep
everything, so something has to decide what's worth keeping.

The problem is that **you often don't know a thing was worth keeping until
later**. You meet someone, you hear their name, and only ten minutes into the
conversation does it become clear you'll want that name again. By then the name
has been buried under ten minutes of other stuff.

Real brains handle this. Ours doesn't yet. So we built a small version of the
problem — show a fact, wait a while, then send a signal meaning *that mattered* —
and tried the simplest possible fix.

## The simplest possible fix

**Keep a rolling window.** Hold on to the last N things provisionally. When the
"that mattered" signal arrives, commit whatever's in the window and drop the
rest.

It's the thing you'd try first. It half-worked: it recovered about a fifth of
what a cheating version gets — a version told in advance which facts would
matter. First time anything in this project recovered *any* of that.

But when the wait was long — twenty steps instead of eight — it went **worse than
useless**. Worse than just keeping everything indiscriminately. The window would
faithfully commit the last eight steps of irrelevant noise while the actual fact
had already fallen off the back.

## The question worth asking before building anything

The obvious reaction: *right, so a rolling window is too crude, let's build the
proper biological mechanism.* Brains do this with a chemical tag — a molecular
sticky note slapped on one specific connection, saying "hold this, we'll find out
shortly whether it mattered."

But there's a much cheaper question first. **What if the window was just too
small?**

Twenty steps is only a problem for a window of eight. Make the window thirty-two
and it reaches back twenty steps fine. If that works, then there's no deep
problem here — just a setting that was too low — and the fancy tag would only be
a way to save memory. Useful, but not important.

Those are genuinely different projects. And the difference is one number in a
config file.

So: don't build the mechanism. Run the sweep. Try every window size against every
delay and look at the shape.

## What came back

| how far it reaches ↓ | wait 1 | wait 4 | wait 8 | wait 20 |
|---:|---:|---:|---:|---:|
| **4** | **0.24** | **0.25** | −0.22 | −0.22 |
| **8** | 0.23 | 0.23 | **0.23** | −0.24 |
| **16** | 0.20 | 0.21 | 0.19 | −0.23 |
| **32** | 0.14 | 0.16 | 0.16 | **0.17** |
| **64** | 0.09 | 0.10 | 0.09 | 0.09 |

Higher is better. Negative means worse than not bothering.

**Read across:** the failures form a clean triangle in the bottom-left. Whenever
the window is shorter than the wait, it fails. Whenever it's long enough, it
works. So "delay 20 is hard" was wrong — delay 20 was never hard. A window of
eight aimed at something twenty steps back is hard. We'd been measuring the
window and calling it the delay.

**Now read down.** This is the part that mattered.

0.24, 0.23, 0.20, 0.14, 0.09.

Making the window bigger makes it **steadily worse**. Not catastrophically — it
never goes negative once it's long enough — but every doubling costs you about a
fifth of what you have left. A window of 64 gives you 0.09 no matter what the
delay is. It reaches everything and resolves nothing.

Which makes sense once you see it. A window of 64 keeps sixty-three pieces of
junk along with the one thing you wanted. At that point you're barely filtering.
"Keep the last 64 things" and "keep everything" are almost the same instruction.

## So the window is stuck between two failures

Too short: you miss the thing.
Too long: you keep the thing, buried in noise.

And the gap between those is narrow — a factor of two in either direction costs
most of the benefit.

**Here's why that settles it.** To pick the right window, you have to know how
long the wait will be. But *not knowing how long the wait will be* is the entire
problem we set out to solve. A window with a dial on it isn't a solution — it's
the original problem with a dial bolted to it, and someone has to turn the dial,
and nobody knows which way.

A tag is different in exactly the way that matters. A tag marks **one specific
thing**. It doesn't span a stretch of time, so it doesn't need to know how long
the stretch should be. The waiting takes care of itself.

That's the difference between an optimisation and a capability. This table is
what makes the tag the second one.

## The bonus argument

There's a size argument too, and it now comes for free.

The window has to physically hold everything it might commit. Each pending item
is two lists of numbers, so a window of 32 on a small node costs about 2000
numbers — **twice the size of the memory it's feeding**. On a project whose whole
point is running on tiny devices, that's not a footnote.

The tag can be a single integer. Because of how keys are generated here, one
token id is enough to reconstruct the whole pending item from scratch. Same reach
for 32 numbers instead of 2000.

## What I'm not claiming

Four predictions were written down before the run and all four came out right,
which normally means the predictions were too easy. Being honest about that:

- One was a wiring check — "the arms that don't read this setting shouldn't
  change when it changes." They didn't, to four decimal places. That's a
  plumbing test, not a discovery. It's written down because every number above
  is measured *against* those arms, and a baseline that drifted with the setting
  would have made the whole table meaningless. That exact confound wrecked a
  result three months ago.
- One followed automatically from another.
- **One was the real prediction**, and it was the one flagged in advance as most
  likely to be wrong. It wasn't.

And the honest limits:

- The smallest window tested was 4. At a wait of 1, the best window was 4 — the
  bottom of the range. The true best might be 1 or 2, and we didn't look.
- The best cell here is 0.25. That's a quarter of what the cheating version gets.
  **The tag has to beat 0.25, not 0.** If marking one thing turns out no better
  than guessing a span, that's a result about this whole line of work, not about
  the tag.

## Next

Build the tag.

---

*Previous: [27. Somebody already built the ruler](27-somebody-already-built-the-ruler.md)*
