"""A world that answers back. Snake, with a local view so acting is informative.

John's suggestion, 2026-08-01, as the step before ARC-AGI-3. It supplies the two
things a recorded corpus cannot:

- **An action changes what is observed.** Every mechanism this project still
  lacks — prediction, the trigger for asking, an output channel — needs that,
  and no corpus can say whether acting to disambiguate beats watching.
- **Recurrence.** The same local configuration happens thousands of times, which
  is what counting needs before any statistic exists. ARC withholds recurrence by
  design; this gives it away.

## Why the view is LOCAL, and it is the whole point

Full-board snake is deterministic and fully observed, so everything worth
knowing is already on screen and a system that learns it has learned dynamics
rather than concepts. That is worth having and it does not test the claim.

With a window of `sight` cells, the food is usually off-view, so **moving is how
you find out where it is**. That gives "act to disambiguate" something to
disambiguate, and it gives the watching baseline a precise meaning: a passive
observer of somebody else's game, at the same number of observations.

`sight=None` is the full board, so the two are arms of one experiment rather
than two programs.

## What it is deterministic for

The dynamics have **no noise at all**. That is deliberate and it is the reason
to run this before anything stochastic: uncertainty sampling's known failure is
chasing irreducible noise, and here there is none, so a prediction-error-driven
policy cannot fail that way by construction. Adding stochasticity later is how
that failure mode gets tested, separately, rather than confounded into the first
measurement.

## What prediction needs, and it is already built

Predicting the next observation from the current one AND the action is a TRIPLE,
not a pair, so `CoOccurrence` cannot hold it — and `openplexus/composition.py`
already counts exactly this shape: `Composition(left, right, target)` with
`left` the current code, `right` the action and `target` the next code. No new
mechanism, and it is the same class the typed walk uses for route kinds.

## Conventions

Dependency-free, like every other task here: the model layer may use numpy and
the thing a result is measured against may not.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

#: The four things the machine can do. Absolute rather than relative, because
#: John's framing is buttons on a device and a button does not know which way
#: the snake is facing.
ACTIONS = ((0, -1), (0, 1), (-1, 0), (1, 0))

#: What a cell can contain. These are the alphabet an observation is written in,
#: and they are deliberately few: a front end that has to tell four things apart
#: is doing quantisation, not perception.
EMPTY, WALL, BODY, FOOD = 0, 1, 2, 3


@dataclass
class Step:
    """One transition, and everything needed to score a prediction of it.

    Attributes:
        view: What was observable AFTER the action, as flat cell values.
        action: Which action was taken to get here.
        ate: Whether the food was taken on this step. **Not a reward** — nothing
            here optimises it; it is reported so an experiment can say whether
            a policy that never sees a reward nonetheless eats more.
        died: Whether the snake ran into a wall or itself.
    """

    view: tuple[int, ...]
    action: int
    ate: bool
    died: bool


class Snake:
    """The board, and what one machine can see of it.

    Attributes:
        width, height: Board size.
        sight: Half-width of the visible window, or `None` for the whole board.
            A window of `sight=2` shows a 5x5 patch centred on the head.
    """

    def __init__(self, width: int = 12, height: int = 12,
                 sight: int | None = 2, seed: int = 0) -> None:
        if width < 3 or height < 3:
            raise ValueError("a board smaller than 3x3 has no room to turn")
        if sight is not None and sight < 1:
            raise ValueError("a sight of zero sees only the head, which never "
                             "changes, so no action could ever be informative")
        self.width, self.height, self.sight = width, height, sight
        self._rng = random.Random(seed)
        self.reset()

    def reset(self) -> None:
        self.body = [(self.width // 2, self.height // 2)]
        self.heading = 3
        self.dead = False
        self._place_food()

    def _place_food(self) -> None:
        free = [(x, y) for x in range(self.width) for y in range(self.height)
                if (x, y) not in self.body]
        self.food = self._rng.choice(free) if free else None

    def _cell(self, x: int, y: int) -> int:
        if not (0 <= x < self.width and 0 <= y < self.height):
            return WALL
        if (x, y) in self.body:
            return BODY
        return FOOD if (x, y) == self.food else EMPTY

    def view(self) -> tuple[int, ...]:
        """What is observable now, flattened.

        **Centred on the head**, so the same situation in two places on the
        board produces the same observation. That is what makes the recurrence
        this task exists to supply actually recur.
        """
        if self.sight is None:
            return tuple(self._cell(x, y)
                         for y in range(self.height)
                         for x in range(self.width))
        head_x, head_y = self.body[0]
        span = range(-self.sight, self.sight + 1)
        return tuple(self._cell(head_x + dx, head_y + dy)
                     for dy in span for dx in span)

    def step(self, action: int) -> Step:
        """Take one action and return what followed.

        A death resets the board and is reported rather than raised: the stream
        never ends, which is what C4 asks of everything here.
        """
        if not 0 <= action < len(ACTIONS):
            raise ValueError(f"no action {action}; there are {len(ACTIONS)}")
        dx, dy = ACTIONS[action]
        head_x, head_y = self.body[0]
        ahead = (head_x + dx, head_y + dy)
        died = (not (0 <= ahead[0] < self.width
                     and 0 <= ahead[1] < self.height)
                or ahead in self.body[:-1])
        ate = False
        if died:
            self.reset()
        else:
            self.body.insert(0, ahead)
            ate = ahead == self.food
            if ate:
                self._place_food()
            else:
                self.body.pop()
            self.heading = action
        return Step(view=self.view(), action=action, ate=ate, died=died)
