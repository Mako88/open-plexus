"""Where a key comes from, as a component you can replace.

**This exists because of a worry John raised**: that pinning down "a model needs
component X at performance Y" would rule out a component Z nobody has thought of
yet. The defence against that is not to avoid measuring components — it is to
make replacing one cheap, so a new idea costs a file rather than a refactor.

Keys were the obvious place to start, because they are the part of this model
that has already been varied twice. Each time it cost a config flag, a branch
inside a six-hundred-line `run`, and a threading of that flag through every
experiment script. A third variation would have cost the same again, and the
fourth is the one that never gets tried.

## The seam

    key(tokens, t) -> vector

That is the whole interface. It takes the token sequence and a position, and it
returns the vector the store binds against. Everything else in the model — the
write rule, the retrieval, the readout, the delta rule, the gates, the
partitions — is untouched by a change here.

**A new key scheme is a class in this file and one assignment.** No config flag,
no branch in `run`, no edit to any experiment. `tests/test_keys.py` proves that
by defining a key source that exists nowhere in the model and running it through
`model.run` end to end.

## What is deliberately NOT in the interface

There is no `fit`, no `update`, and no access to the store. A key source sees
token ids and a position, and nothing else. That is not tidiness — it is the C1
locality constraint written into a type: a node that could only be handed token
ids can compute any key defined here, and one that needed the store's contents
or a global statistic could not.

A scheme that genuinely needs to learn its keys will have to widen this
interface, and widening it should be a deliberate act with a note attached,
because it is where the broadcast-a-token argument (note 012) would be spent.
"""

from __future__ import annotations

from typing import Protocol, runtime_checkable

import numpy as np


@runtime_checkable
class KeySource(Protocol):
    """Produces the vector the store binds against at one position."""

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        """The key at position `t` of `tokens`.

        Must be pure: the same `(tokens, t)` gives the same vector, on any node,
        in any order. The store is written and read at different times and on
        different machines, and a key that drifted between the two would break
        the retrieval without breaking any test that only wrote.
        """
        ...


class TableKeys:
    """One key per token, read from a table. The original scheme.

    The table is `Wk`, built by the model — stored when `derived_keys` is off
    and regenerable from `(seed, token)` when it is on. Either way this class
    reads a row, so churn, partitions and ablation keep working through `Wk`
    exactly as they did.

    **Its limit is the ceiling of note 033**: a key that depends only on the
    current token makes the store a bigram count table, which cannot represent a
    trigram.
    """

    def __init__(self, table: np.ndarray) -> None:
        self.table = table

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        return self.table[tokens[t]]


class PairKeys:
    """A key derived from the token PAIR `(t-1, t)` by a fixed hash.

    Note 034: this makes `previous_key` the key of `(t-2, t-1)`, so the same
    write rule builds a trigram table. It lifts the bigram ceiling — 0.533 to
    1.000 on a step a bigram cannot resolve — and it is not free: the number of
    distinct keys goes from `vocab` to the number of pairs that actually occur,
    469 against 66 in 4000 characters of Shakespeare.

    Derived and cached, never tabulated. A `vocab^2` table is 16 million rows at
    `vocab 4096`, and not holding it is the same argument `derived_keys` rests
    on: a node is handed two token ids and rebuilds the vector itself.
    """

    def __init__(self, seed: int, spread: float, width: int,
                 start: int) -> None:
        self.seed = seed
        self.spread = spread
        self.width = width
        #: Stands in for "no previous token" at position 0, so the first step
        #: has a key in the same space as every other step rather than a special
        #: case the store would have to exclude.
        self.start = start
        self.cache: dict[tuple[int, int], np.ndarray] = {}

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        return self.pair(int(tokens[t - 1]) if t else self.start,
                         int(tokens[t]))

    def pair(self, previous: int, token: int) -> np.ndarray:
        """The vector for one pair, exposed so a test can check two nodes agree
        without having to build a sequence that reaches the pair."""
        cached = self.cache.get((previous, token))
        if cached is None:
            cached = np.random.default_rng(
                (self.seed, previous, token)).normal(
                    0.0, self.spread, self.width)
            self.cache[(previous, token)] = cached
        return cached
