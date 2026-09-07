"""The `Core` protocol and the cost row every call to one produces.

A core is the only part of this system that holds a language model. Everything
else -- the store, the replay, the fleet -- is built so that the core can be
swapped for a bigger one without anything above it changing. That is the whole
reason there is a protocol here rather than a class.

THE STATE IS A VALUE AND NOT AN ATTRIBUTE OF THE CORE. `feed` takes a state and
returns a new one; the core itself is stateless between calls. This is not
tidiness. A thread's state must be saveable, loadable, forkable and (open fork)
storable as a fragment, and a core that hides its state inside itself can do
none of those. It also means one loaded core can serve many threads, which is
what Phase 5 needs when several nodes share a box with one GPU.

`save_state` and `load_state` therefore take the state explicitly. The design
doc writes them as `save_state(path)`, which reads as though the core owned one
state; the signatures here are the same idea with the state passed in, because
`feed` already returns it.
"""

from __future__ import annotations

import time
from collections.abc import Sequence
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Any, Protocol, Self, runtime_checkable

import numpy as np


@dataclass(frozen=True)
class Cost:
    """What one call to a core cost, in the four units every reading records.

    `flops` is an ESTIMATE from the standard counts -- forward is
    `2 * params * tokens`, training is `6 * params * tokens` -- and not a
    measurement off the card. It is here so that an arm and the full-context
    baseline can be compared on compute rather than on wall clock, since wall
    clock on a shared desktop GPU moves with whatever else is on the screen.
    Do not read it as an efficiency number for the kernel; read it as the
    denominator the exam divides a score by.
    """

    tokens_in: int
    tokens_out: int
    seconds: float
    flops: float

    def __add__(self, other: Cost) -> Cost:
        return Cost(
            tokens_in=self.tokens_in + other.tokens_in,
            tokens_out=self.tokens_out + other.tokens_out,
            seconds=self.seconds + other.seconds,
            flops=self.flops + other.flops,
        )

    @property
    def tokens_per_second(self) -> float:
        n = self.tokens_in + self.tokens_out
        return n / self.seconds if self.seconds > 0 else 0.0

    def row(self) -> dict[str, Any]:
        d = asdict(self)
        d["tokens_per_second"] = self.tokens_per_second
        return d


ZERO_COST = Cost(0, 0, 0.0, 0.0)


def forward_flops(params: int, tokens: int) -> float:
    """The `2 * N * T` estimate for a forward pass. See `Cost`."""
    return 2.0 * params * tokens


def training_flops(params: int, tokens: int) -> float:
    """The `6 * N * T` estimate for forward plus backward. See `Cost`."""
    return 6.0 * params * tokens


class Meter:
    """Accumulates `Cost` across the calls that make up one turn or one exam."""

    def __init__(self) -> None:
        self.total = ZERO_COST
        self.calls = 0

    def add(self, cost: Cost) -> Cost:
        self.total = self.total + cost
        self.calls += 1
        return cost

    def timed(self, params: int, tokens_in: int, tokens_out: int, seconds: float) -> Cost:
        cost = Cost(
            tokens_in=tokens_in,
            tokens_out=tokens_out,
            seconds=seconds,
            flops=forward_flops(params, tokens_in + tokens_out),
        )
        return self.add(cost)


class clock:
    """`with clock() as c: ...` then `c.seconds`. A context manager, hence lowercase."""

    def __enter__(self) -> Self:
        self._start = time.perf_counter()
        self.seconds = 0.0
        return self

    def __exit__(self, *exc: object) -> None:
        self.seconds = time.perf_counter() - self._start


@runtime_checkable
class Core(Protocol):
    """A language model that carries its memory in a fixed-size state.

    The contract every implementation owes:

    - `feed` is pure with respect to the core. Given the same state and tokens
      it returns the same state, and the state passed in is not mutated. The
      exam depends on this: Tier A replays a saved state and must get the same
      thing back.
    - `generate` stops at `budget` tokens or at a stop condition, whichever is
      first, and returns the state as it stands after the generated tokens have
      been fed back in. A caller that discards the returned state has thrown
      away the turn.
    - The state is opaque. Nothing outside `core` may index into it.
    """

    @property
    def params(self) -> int:
        """Parameter count, for the flops estimate."""

    @property
    def name(self) -> str:
        """What went in the reading's `core` field. Includes the size."""

    def encode(self, text: str) -> list[int]: ...

    def decode(self, tokens: Sequence[int]) -> str: ...

    def feed(self, tokens: Sequence[int], state: Any | None) -> tuple[Any, Cost]: ...

    def generate(
        self, prompt_tokens: Sequence[int], state: Any | None, budget: int
    ) -> tuple[list[int], Any, Cost]: ...

    def embed(self, tokens: Sequence[int]) -> np.ndarray: ...

    def save_state(self, state: Any, path: Path) -> None: ...

    def load_state(self, path: Path) -> Any: ...


@dataclass
class Sampling:
    """The generation dials, in one place so a reading can record them.

    Defaults are RWKV's own published inference settings for the G1 line rather
    than anything measured here. When a reading moves because of one of these,
    that is the reading's problem to name.
    """

    temperature: float = 1.0
    top_p: float = 0.3
    presence_penalty: float = 0.5
    frequency_penalty: float = 0.5
    penalty_decay: float = 0.996
    stop_tokens: tuple[int, ...] = (0,)  # RWKV world: token 0 is the end-of-text marker.
    stop_strings: tuple[str, ...] = field(default=("\n\n",))
