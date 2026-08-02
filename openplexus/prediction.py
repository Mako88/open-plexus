"""Predicting what comes next, and being wrong about it.

**The first mechanism in this project that can be wrong.** Counts only go up, so
nothing anywhere else has an error signal — decision 7 has said so since the
README was written. Predicting the next observation supplies one, and John's
connection is that the same signal decides when to ASK rather than watch, which
is currently a fixed budget fraction nobody chose on purpose.

## Prequential, which is the only honest way to score this

`learn` returns the surprise measured BEFORE the count is taken. Scoring after
updating would let the model see the answer first, and the number would improve
forever without anything being learned. Decision 10 already commits to this and
this is where it becomes a line of code.

## Two ways to condition on the action, and they are an arm each

Predicting the next surface from the current surface AND the action is a triple.
There are two ways to hold one and they differ in what they can express:

- **`bound`** makes `(state, action)` a SURFACE of its own, so the prediction is
  an ordinary pairwise co-occurrence between that surface and the next one.
  Exact, and C1-clean because a composite surface has an owner like any other.
  It costs `states x actions` surfaces, which is multiplicative and is the whole
  price.
- **`factored`** is what `composition.Composition` already does: count the three
  pairwise edges and score a candidate from both halves, combined with `min`.
  Far cheaper and it generalises across states — and **it cannot express an
  interaction**. Going right beside a wall and going right in open space are the
  same action and the same combination rule; only the state half distinguishes
  them, and under `min` a candidate still has to be typical of the action alone.

Snake is the case that separates them, because its dynamics are pure
interaction: the same action does entirely different things depending on what is
adjacent. So this is a measurement rather than a preference, and both are built.

**`bound` is a binding of two inputs into one unit**, which is the same shape as
the cortical column story John raised — many small units each bound to a slice
of the input, and the answer being what they agree on. That is an analogy and
not evidence, and it is recorded here as the reason the arm exists rather than
as a result.

## What was searched

By capability — predict, expect, surprise, error, next, forecast — across
`openplexus/`, `experiments/`, `tools/` and `tests/`.

- **`openplexus/composition.py` supplies the factored arm** and is called, not
  copied.
- **`openplexus/grounding.py` supplies the counting and every statistic.**
  Nothing here computes one.
- **Nothing else predicts anything.** `grep -rn "def predict"` over the package
  returns nothing, which is decision 7's ⬜ stated as a fact about the tree.
"""

from __future__ import annotations

import math

from openplexus.composition import Composition
from openplexus.grounding import STATISTICS, CoOccurrence, Statistic

#: How the action is conditioned on. `bound` is exact and multiplicative,
#: `factored` is cheap and cannot express an interaction.
#:
#: **`adaptive` is neither, and it is what the counterfactual measurement
#: earned.** The two are not better and worse — on a pair it has SEEN bound
#: wins 0.742 to 0.455, and on one never counted bound scores 0.000 where
#: factored scores 0.162 against a chance of 0.0019. They are MEMORY and
#: GENERALISATION. So use the bound surface where it has any evidence at all
#: and fall back to factoring where it has none.
#:
#: **The rule needs no threshold.** "Has this pair ever been counted" is a
#: question with an answer; "has it been counted enough" would be a dial. A
#: threshold above one is the obvious refinement and is not built.
#:
#: It costs both writes on every observation, which is the price of having
#: both tables to read from.
BINDINGS = ("bound", "factored", "adaptive")


class Predictor:
    """Expects the next observation, reports how wrong it was, then counts it.

    Attributes:
        actions: How many actions exist.
        binding: A key of `BINDINGS`.
        index: The counts underneath, public because every measurement in this
            project is taken from counts rather than from a summary.
    """

    def __init__(self, actions: int, binding: str = "bound",
                 statistic: str = "conditional",
                 half_life: float | None = None) -> None:
        if binding not in BINDINGS:
            raise ValueError(f"binding must be one of {BINDINGS}")
        if actions < 1:
            raise ValueError("a world with no actions cannot be acted on")
        self.actions = actions
        self.binding = binding
        self.statistic: Statistic = STATISTICS[statistic]
        self.index = CoOccurrence(half_life=half_life)
        # The factored arm needs its role blocks sized up front, and the state
        # alphabet is not known until the stream arrives. It is grown lazily by
        # `_composition`, which is why the factored arm holds a rebuildable
        # table rather than one made in `__init__`.
        self._factored: Composition | None = None
        self._states: dict[int, int] = {}
        self._targets: dict[int, int] = {}
        # PER INSTANCE. As a class attribute this is one list shared by every
        # predictor ever made, so two arms in one sweep would replay each
        # other's history into their own table and both would be wrong in a way
        # that still runs.
        self._history: list[tuple[int, int, int]] = []

    def _state_id(self, state: int) -> int:
        return self._states.setdefault(state, len(self._states))

    def _target_id(self, target: int) -> int:
        return self._targets.setdefault(target, len(self._targets))

    def _bound(self, state: int, action: int) -> int:
        """The surface standing for *this state with this action taken*.

        Negative, so it can never collide with an observation surface however
        the caller numbers those. A composite surface is still one surface: it
        has an owner, it is written by that owner alone, and nothing waits.
        """
        if not 0 <= action < self.actions:
            raise ValueError(f"no action {action}; there are {self.actions}")
        return -1 - (self._state_id(state) * self.actions + action)

    def seen(self, state: int, action: int) -> float:
        """How much evidence exists for this state and action."""
        if self.binding in ("bound", "adaptive"):
            return self.index.seen(self._bound(state, action))
        return float(len(self._targets))

    def bound_evidence(self, state: int, action: int) -> float:
        """How much has been counted for this exact pair. Zero means never."""
        return self.index.seen(self._bound(state, action))

    def scores(self, state: int, action: int) -> dict[int, float]:
        """Every candidate next observation, scored. Empty when nothing is known."""
        if self.binding == "adaptive":
            # MEMORY WHERE IT EXISTS, GENERALISATION WHERE IT DOES NOT.
            return (self._bound_scores(state, action)
                    if self.bound_evidence(state, action) > 0
                    else self._factored_scores(state, action))
        if self.binding == "bound":
            return self._bound_scores(state, action)
        return self._factored_scores(state, action)

    def _bound_scores(self, state: int, action: int) -> dict[int, float]:
        here = self._bound(state, action)
        return {other: self.statistic(self.index, other, here)
                for other in self.index.partners(here)}

    def _factored_scores(self, state: int, action: int) -> dict[int, float]:
        if self._factored is None:
            return {}
        left = self._state_id(state)
        # A state first seen on THIS call has an id the table was not built
        # with. Nothing is known about it, which is the honest answer; asking
        # anyway raised, because `Composition` refuses an out-of-range role
        # rather than folding it back in.
        if left >= self._factored.sizes["left"]:
            return {}
        back = {value: key for key, value in self._targets.items()}
        return {back[target]: score
                for score, target in self._factored.ranked(
                    left, action, self.statistic)
                if score > 0.0 and target in back}

    def probability(self, state: int, action: int, actual: int) -> float:
        """What the model gives `actual`, add-one smoothed over outcomes seen.

        **From COUNTS, not from the statistic.** A first version divided by the
        sum of `conditional` scores, which is not a normalising constant, and
        the resulting "surprise" tracked how fast the alphabet was growing
        rather than how well the model predicted — it went UP over 4,000 steps
        of a world whose dynamics never change.

        The factored arm has no joint count to divide by, so it uses the
        combined scores as unnormalised weights. That is an approximation and
        it is the reason the two arms' bits are comparable only in direction.
        """
        # **Outcomes seen PLUS ONE**, never below two. The `+1` is room for
        # something never seen, which is what smoothing is for; without it an
        # empty model divides 1 by 1 and calls its very first sighting certain,
        # at zero bits. With it, total ignorance costs exactly one bit — a coin
        # flip between "this" and "anything else", which is the honest statement
        # of knowing nothing.
        alphabet = max(len(self._targets) + 1, 2)
        bound = (self.binding == "bound"
                 or (self.binding == "adaptive"
                     and self.bound_evidence(state, action) > 0))
        if bound:
            here = self._bound(state, action)
            total = self.index.seen(here)
            count = self.index.together(here, actual)
        else:
            scores = self.scores(state, action)
            total = sum(scores.values())
            count = scores.get(actual, 0.0)
        return (count + 1.0) / (total + alphabet)

    def surprise(self, state: int, action: int, actual: int) -> float:
        """Bits of surprise at `actual`, measured BEFORE this is counted.

        **A growing alphabet raises this on its own**, because an unseen
        outcome competes with more of them. So a falling curve is evidence and
        a flat one is not necessarily a failure — `hit` is the companion that
        does not move with the alphabet.
        """
        return -math.log2(self.probability(state, action, actual))

    def hit(self, state: int, action: int, actual: int) -> bool:
        """Was the single best guess right? Immune to the alphabet growing."""
        best = self.expect(state, action, 1)
        return bool(best) and best[0] == actual

    def learn(self, state: int, action: int, actual: int) -> float:
        """Score the prediction, then count what happened. Returns the surprise.

        **In this order and it matters.** Counting first would let the model see
        the answer before being asked about it, and the error would fall forever
        without anything being learned.
        """
        cost = self.surprise(state, action, actual)
        if self.binding in ("bound", "adaptive"):
            self.index.observe((self._bound(state, action), actual))
        if self.binding in ("factored", "adaptive"):
            self._observe_factored(state, action, actual)
        self._target_id(actual)
        return cost

    def _observe_factored(self, state: int, action: int, actual: int) -> None:
        """Grow the role blocks as the alphabet does, then count the triple.

        A `Composition` is sized at construction, so a stream that keeps
        producing new observations outgrows it. Rebuilding replays what is
        already held, which is affordable only because the alphabet grows
        slowly; a stream with unbounded novelty would need a different table.
        """
        left, right = self._state_id(state), action
        target = self._target_id(actual)
        needed = (max(len(self._states), 1), self.actions,
                  max(len(self._targets), 1))
        if (self._factored is None
                or self._factored.sizes["left"] < needed[0]
                or self._factored.sizes["target"] < needed[2]):
            grown = Composition(max(needed[0], 1) * 2, right=self.actions,
                                target=max(needed[2], 1) * 2)
            for one, two, three in self._history:
                grown.observe(one, two, three)
            self._factored = grown
        self._factored.observe(left, right, target)
        self._history.append((left, right, target))

    def expect(self, state: int, action: int, count: int = 1) -> list[int]:
        """The `count` most likely next observations, best first."""
        scores = self.scores(state, action)
        return [surface for surface, _ in
                sorted(scores.items(), key=lambda pair: (-pair[1], pair[0]))
                ][:count]
