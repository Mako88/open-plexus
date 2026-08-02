"""What happened JUST BEFORE, carried into what is happening now.

John's original design, 2026-08-02, noticed missing after the restructure:
*"things that happen at the same time are co-occurring"* — and the half that
went with it, that things happening one step apart are related too, and
differently.

## What the graph could not say before this

`CoOccurrence.observe` takes a SET. Everything in it met everything else and
nothing came first. Occasion 5 and occasion 500 are indistinguishable, and **"A
then B" cannot be told from "A with B"** anywhere in the system. A table of
co-occurrence is a static object; a world is a process, and every bit of
sequence was discarded at the door.

That is why `prediction.Predictor` had to be handed `(state, action, next)`
explicitly: the ordering could not come from the graph, because the graph does
not hold any.

## The asymmetry was already there and nothing used it

`observed_with(surface, other)` writes ONE direction — `_pairs[a][b]` and
`_pairs[b][a]` are separate rows, and `pair()` simply writes both. So a
one-directional write already records *"a was followed by b"* distinctly from
the reverse, and `conditional(now, before)` already reads back *"how often does
now follow before"*. **Nothing had ever written a single direction on purpose.**
Time costs no new storage, only a decision about which direction to write.

## Why the window is flat rather than decaying

A weight that falls off with distance in time is the obvious refinement and it
needs fractional counts, which the accumulator does not hold. A flat window over
`span` moments is what can be built without changing the thing every result is
measured on, and it is a dial: `span=0` is exactly the old behaviour, so every
earlier number stays reachable.

## What this is NOT

It is not a clock. Nothing is looked up by time, nothing is ordered globally,
and two nodes never compare when anything happened — `buckets.Join` is the
mechanism for that and this is not it. This is local, per stream, and needs no
agreement: C1 is untouched because nobody waits for anybody.
"""

from __future__ import annotations

from collections import deque

from openplexus.grounding import CoOccurrence


class Window:
    """A stream of moments, where recent ones still count as context.

    Attributes:
        span: How many previous moments reach into the present. `0` is the old
            behaviour exactly — every occasion isolated, order discarded.
        index: The accumulator being written to.
    """

    def __init__(self, index: CoOccurrence, span: int = 0) -> None:
        if span < 0:
            raise ValueError("a window cannot reach backwards a negative "
                             "number of moments")
        self.index = index
        self.span = span
        self._recent: deque = deque(maxlen=span) if span else deque(maxlen=1)

    def observe(self, surfaces) -> None:
        """One moment. Everything in it meets everything else, symmetrically —
        and everything in the last `span` moments meets it ONE WAY, past to
        present.

        The direction is the whole point. A symmetric write would say these
        things go together and lose which came first, which is what the system
        already did. Writing `before -> now` makes `conditional(now, before)`
        mean *how often now follows before*, and leaves `conditional(before,
        now)` free to mean the other thing.
        """
        present = sorted(set(surfaces))
        self.index.observe(present)
        if self.span:
            for earlier in self._recent:
                for before in earlier:
                    for now in present:
                        if before != now:
                            self.index.observed_with(now, before)
            self._recent.append(present)

    def follows(self, now: int, before: int, statistic) -> float:
        """How strongly `now` follows `before`, read off the one-way edges.

        Reads the same rows `observe` wrote and does no arithmetic of its own,
        so a caller can check the two agree rather than trusting this.
        """
        return statistic(self.index, now, before)
