"""A random frozen substrate — G0's floor, and nothing more.

This is a standard echo-state reservoir: random input weights, a random
recurrent matrix scaled to a chosen spectral radius, a leaky tanh update, and
**no learning anywhere**. Its weights never change.

It exists for one reason. Every claim this project ever makes about a learning
rule has to be read against what an *untrained* substrate of the same size
already achieves, because a random recurrent network with a trained linear
readout is a genuinely capable thing and the previous project lost a year to not
taking that seriously enough (docs/explainers/05).

It is emphatically **not** the architecture being proposed. It is the control.

Standard library only, and slow on purpose (see openplexus/linalg).
"""

from __future__ import annotations

import math
import random
from dataclasses import dataclass

from openplexus import linalg


@dataclass(frozen=True)
class ReservoirConfig:
    """Shape and dynamics of the frozen substrate.

    Attributes:
        n_units: State dimension. This is the capacity bound — a reservoir
            cannot hold more independent information than it has units
            (docs/notes/006 §2), so it is the dial that decides how much of the
            benchmark is reachable without learning.
        spectral_radius: Target for the recurrent matrix's largest eigenvalue
            magnitude. Below 1.0 the state forgets; the closer to 1.0, the
            longer memory persists and the less stable the dynamics.
        leak: Fraction of the state replaced each step. Lower means slower
            dynamics and longer effective memory.
        input_scale: Magnitude of the random input projection. Large values
            drive tanh into saturation, which costs memory — the
            memory–nonlinearity trade-off (docs/notes/006 §2) is set here and by
            `spectral_radius` together.
        density: Fraction of recurrent connections that are non-zero.
        seed: Determines every weight. The substrate is a deterministic function
            of this and the shape.
    """

    n_units: int = 64
    spectral_radius: float = 0.9
    leak: float = 0.3
    input_scale: float = 0.5
    density: float = 0.2
    seed: int = 0

    def __post_init__(self) -> None:
        if self.n_units < 1:
            raise ValueError("n_units must be at least 1")
        if not 0.0 < self.leak <= 1.0:
            raise ValueError("leak must be in (0, 1]")
        if not 0.0 < self.density <= 1.0:
            raise ValueError("density must be in (0, 1]")
        if self.spectral_radius <= 0.0:
            raise ValueError("spectral_radius must be positive")


class Reservoir:
    """A frozen random recurrent substrate.

    The contract: `run` maps a token sequence to one state vector per position,
    deterministically, using weights that never change. Two `Reservoir`s built
    from equal configs produce identical states.
    """

    def __init__(self, config: ReservoirConfig, vocab_size: int) -> None:
        if vocab_size < 1:
            raise ValueError("vocab_size must be at least 1")
        self.config = config
        self.vocab_size = vocab_size
        rng = random.Random(config.seed)
        n = config.n_units

        # One random input vector per token. Equivalent to a one-hot input
        # through a dense projection, without materialising the one-hot.
        self._w_in = [
            [rng.uniform(-1.0, 1.0) * config.input_scale for _ in range(n)]
            for _ in range(vocab_size)
        ]

        recurrent = [
            [rng.gauss(0.0, 1.0) if rng.random() < config.density else 0.0
             for _ in range(n)]
            for _ in range(n)
        ]
        radius = linalg.spectral_radius(recurrent, seed=config.seed)
        if radius < 1e-12:
            raise ValueError("recurrent matrix is degenerate; raise density or n_units")
        scaled = linalg.scale(recurrent, config.spectral_radius / radius)
        # Stored as (source, weight) pairs per row. `density` is 0.2 by default,
        # so a dense loop would spend most of its time multiplying by zero. This
        # is not a fast path asserted against a reference — it is the obvious
        # representation for a sparse matrix, and the dense form is never used.
        self._w = [[(j, w) for j, w in enumerate(row) if w] for row in scaled]

    def run(self, tokens: tuple[int, ...]) -> list[list[float]]:
        """Map a token sequence to one state vector per position.

        Returns `len(tokens)` states, each of length `n_units`. The state at
        position i reflects tokens[0..i] and nothing later — the substrate is
        causal, and `tests/test_reservoir.py` asserts that by scrambling the
        future rather than trusting this docstring.
        """
        n = self.config.n_units
        leak = self.config.leak
        state = [0.0] * n
        out: list[list[float]] = []
        for token in tokens:
            if not 0 <= token < self.vocab_size:
                raise ValueError(f"token {token} outside vocab of {self.vocab_size}")
            w_in = self._w_in[token]
            state = [
                (1.0 - leak) * state[i]
                + leak * math.tanh(
                    sum(w * state[j] for j, w in self._w[i]) + w_in[i]
                )
                for i in range(n)
            ]
            out.append(state)
        return out
