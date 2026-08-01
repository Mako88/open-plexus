"""A world that can be ASKED, not only watched.

Everything in `openplexus/tasks/` generates or reads. This is the first thing that
takes a request, and it exists because `g39-06` measured a boundary no amount of
watching crosses: a surface merely COMMON around a concept is refused by 0.4490,
and one genuinely CORRELATED with it by **0.0096** — a 47-fold collapse — with a
stronger confound crossing entirely.

**That is not a defect of any particular statistic.** A surface more common around
one concept IS evidence about that concept, and an observational stream contains
nothing separating *spuriously correlated* from *actually part of it*. Every
mechanism in `co-occurrence-statistic.md` reads the same counts and inherits the
same blindness.

## The question this makes askable

    world.ask(present, absent)

One occasion containing `present` and not containing `absent` — or a **REFUSAL**.

**The refusal is the signal, not an error.** A constitutive part is one you cannot
get an occasion without. Ask for the dog without the lamp and you get it; ask for
the dog without the bark and the world cannot comply. That is a claim about
causation and it is not reachable by counting.

## Three choices that decide whether the measurement means anything

**Asking by SURFACE, never by concept.** `Occasion.subject` is marked *diagnostics
only* in `occasions.py` because handing it to a mechanism is handing over the
answer. A learner that could name the concept it wanted would have already solved
the problem. Both arguments here are surfaces it has seen.

**The refusal is ONE DRAW.** Once an occasion featuring `present` is in hand, the
world reports whether `absent` came with it and does not try again. A
retry-until-satisfied loop would make the refusal rate a function of its cap,
which is a dial on the result — and the refusal rate is precisely the quantity the
whole idea rests on. As built it is exactly the conditional presence of `absent`
given `present`, measured rather than tuned.

**An ask costs every occasion it draws.** Finding one that features `present` may
take many attempts, and all of them are charged. Otherwise a system that asks
would quietly see more of the world than one that watches, and the comparison
would be measuring sample size — which is the confound `g41-01` found dominating
a different question entirely.

## What this does NOT duplicate, and what was searched

Searched by capability — ask, request, query, intervene, act, world, sample —
across `openplexus/`, `openplexus/tasks/`, `tools/`, `tests/` and `experiments/`.

- **`openplexus/tasks/occasions.py` is IMPORTED and its generator is UNCHANGED.**
  `draw_occasion` is called here exactly as `generate` calls it, so a stream taken
  through this world is drawn from the same distribution as every earlier result
  and nothing already measured moves. This adds a request path and no new physics.
- **`occasions.shuffled`** destroys co-occurrence to give a floor. That is a
  control over a fixed stream; this changes which occasions are drawn at all.
- **`openplexus/grounding.py`** consumes occasions and counts. It is the observer
  this is measured against, and it is not modified — an `ask` returns an
  `Occasion`, so the same `CoOccurrence.observe` consumes both arms.
- **`openplexus/tasks/mqar.py`** takes a query, but the query is the task's
  output and the answer is already in the stream. Here the request changes what
  the world produces.

**Nothing here is a claim that intervention works.** `g44-01` is the falsifier and
its predictions were committed before this file existed.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

from openplexus.tasks.occasions import Occasion, OccasionConfig, draw_occasion


@dataclass(frozen=True)
class Answer:
    """What came back from an ask.

    Attributes:
        occasion: The occasion drawn, or None if the world refused.
        refused: True when an occasion featuring `present` was found and
            `absent` was in it. **Distinct from `occasion is None` on a miss** —
            a refusal is evidence and a miss is a budget being exhausted, and
            conflating them would let an expensive question read as a causal one.
        drawn: Occasions the world consumed answering. Charged to the budget.
    """

    occasion: Occasion | None
    refused: bool
    drawn: int


class World:
    """`occasions.generate`'s stream, available one occasion at a time and askable.

    The clock advances on every occasion drawn, watched or asked, so `when` is
    still strictly increasing and a bucket join can round it exactly as before.
    """

    def __init__(self, config: OccasionConfig) -> None:
        self.config = config
        self._rng = random.Random(config.seed)
        self._when = 0
        #: Occasions drawn, however they were requested. **The budget both arms
        #: spend**, so an asking system cannot see more of the world than a
        #: watching one.
        self.drawn = 0

    def watch(self) -> Occasion:
        """The next occasion, unrequested. What every earlier result consumes."""
        occasion = draw_occasion(self.config, self._rng, self._when)
        self._when += 1
        self.drawn += 1
        return occasion

    def ask(self, present: int, absent: int, patience: int = 64) -> Answer:
        """One occasion featuring `present`, and whether `absent` came with it.

        Args:
            present: A surface the occasion must contain.
            absent: The surface being asked about. Its presence is a REFUSAL.
            patience: How many occasions may be drawn looking for one that
                features `present` before giving up. **This bounds the search
                for `present` and never the test for `absent`** — the refusal is
                decided by the first qualifying occasion and nothing is redrawn
                after it, so no setting of this can move the refusal rate.

        Returns:
            An `Answer`. `refused` is only meaningful when an occasion was found.
        """
        if present == absent:
            raise ValueError(
                "asking for a surface without itself is unanswerable by "
                "construction, and would report a refusal every time")
        drawn = 0
        for _ in range(patience):
            occasion = draw_occasion(self.config, self._rng, self._when)
            self._when += 1
            self.drawn += 1
            drawn += 1
            if present not in occasion.surfaces:
                continue
            if absent in occasion.surfaces:
                return Answer(occasion=occasion, refused=True, drawn=drawn)
            return Answer(occasion=occasion, refused=False, drawn=drawn)
        return Answer(occasion=None, refused=False, drawn=drawn)
