# 31. What the filter turned out to be

The last few explainers described a new way of deciding what a device should
remember. Several more experiments have run since, and the picture changed
enough that the earlier description is now misleading. This is what the thing
actually is.

## The problem, once more

A device sees a stream of information. Most of it is junk. Occasionally something
arrives that says *"the thing you saw a moment ago mattered"* — but it arrives
too late to help decide, and the device has already had to choose what to keep.

Keeping everything does not work: a memory holding a hundred things retrieves all
of them faintly, and the one you wanted is lost in the others. So the device must
throw almost everything away, without knowing what it will need.

## Two ways to choose, and what we thought the difference was

**By clock.** When the signal arrives, keep the last N things. Simple, and it
needs someone to pick N. Pick too small and you miss the item; too large and you
keep so much junk the memory is useless again.

**By content.** Mark the few items that look like real information rather than
noise, let the marks expire, and keep whatever is still marked when the signal
arrives. This was supposed to be the improvement — you never have to pick N, so
it should work whether the important thing was four steps back or forty.

It does work. Set up one way it recovers about a fifth of what a cheating
filter gets, and — unlike the clock — it does that equally well at every distance
we tested.

**But it turns out to be doing that for a reason nobody intended.**

## The awkward discovery

We counted what the filter actually keeps, rather than only scoring it.

It keeps **every single item that later gets asked about**. Not most: all of
them, at every distance. Its problem is not that it misses things. Its problem is
that it keeps about **twenty-nine useless items for each useful one**.

So the filter was never failing at "spot the important thing". It has been
failing at "stop keeping everything else" — which is a completely different
problem from the one we had been trying to solve, and the mechanism we were about
to build next was aimed at the wrong one.

## And the setting that made it look good is the setting that makes it bad

The filter has a budget: how many marks it may hold. At a *large* budget it is
indifferent to distance — the property it was built for. At a *small* budget it
gets much more precise but starts caring about distance again, sharply.

The reason is uncomfortable and simple. **At a large budget it is indifferent to
distance because it is keeping nearly everything.** If you keep almost all of it,
it does not matter where in the stream the important thing was. The headline
property was bought by giving up the very thing we wanted.

## What it actually is

Underneath, the marks fade with time, and the moment they are cashed in is the
moment the signal arrives. So what survives is *what was recent when the signal
came*. The filter is a **clock with soft edges and a budget** — the very
mechanism it was supposed to replace, wearing different clothes.

The content signal is real: it picks out genuine information about four and a
half times better than chance. But there is a hard limit on how much that can
buy, and it is a property of the test rather than of any device: only one item in
six is ever asked about, and nothing visible distinguishes that one from the
other five. **So a perfect content-detector would still keep six items per useful
one.** Our filter already gets most of the way to that limit, which is why making
the signal better kept producing such small gains.

## The genuinely good news

Running the two methods *together* — keep anything either one claims — is better
than either alone, and clearly so. At the smallest budget we tried, the content
filter on its own is worse than useless and the pair is the best result this
project has produced.

That makes sense in hindsight. The two are not competing versions of one idea.
One answers *is this real information*, the other answers *is this the piece the
signal was about*, and a device needs both answers. What is running now is a test
of how cheap the second half can be made.

## And one thing about the test itself

While counting, we found that our test lays its items out at evenly spaced
intervals, and the signal always arrives a short fixed distance after the item it
refers to — shorter than the spacing. So the item nearest each signal is always
the right one. **Always.**

That means a very simple rule would solve the test perfectly. We built that rule
to see, and our content-detection is not accurate enough to use it except in the
easiest case. So the leak is real and, for us, inert — the results are not
inflated by it.

It should still be fixed, because a test that can be beaten by a trick is not
measuring what it claims. But fixing it means re-running nine experiments to keep
the numbers comparable, so that is a decision to take deliberately rather than
overnight.

## Where this leaves things

- The filter works, and is a clock with soft edges rather than a new principle.
- Its remaining shortfall is precision, not blindness.
- There is a ceiling on how much better any content signal can do, and it belongs
  to the test.
- Combining both signals beats either, and is the live direction.
- The test has a flaw that does not currently affect the results, and fixing it
  costs a re-run.
