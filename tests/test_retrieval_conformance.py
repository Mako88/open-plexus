"""Properties EVERY retrieval strategy must satisfy, run over all of them.

`test_retrieval_seam.py` tests each strategy's own behaviour. This tests the
contract they share, parametrically, so a new strategy is admitted by passing a
suite rather than by whatever its author thought to check.

**Why this comes before a combinatorial sweep.** A grid over
keys x retrieval x readout is expensive, and a broken implementation inside it
does not announce itself — it produces a number, the number goes in a table, and
the table is read. Finding out that way costs the whole matrix; finding out here
costs a second.

## The properties, and why each one is here

- **`read` must not mutate what it is given.** The store is the model's live
  state and the key is reused by the caller. A strategy that wrote into either
  would corrupt the model from underneath, and every symptom would appear
  somewhere else.
- **`begin` must actually reset.** Per-run state surviving into the next run
  makes a sequence's result depend on what ran before it — invisible in any
  single run and inherited by every measurement.
- **`read` must be deterministic given the same state.** The store is written
  and read at different times and on different machines.
- **The shape must be the store's row count**, whatever the strategy does
  internally, or the readout silently contracts against the wrong axis.

## The suite has to bite

A conformance suite that passes everything proves nothing. `Sloppy` below
violates each property deliberately and is asserted to FAIL, which is rule 10
applied to the suite itself rather than to the code under test.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.retrieval import (
    ExactCache, Retrieval, SettlingRead, SuperposedRead)

WIDTH = 8


def implementations() -> list[tuple[str, object]]:
    """Every strategy the model can be built with, including compositions."""
    return [
        ("SuperposedRead", SuperposedRead()),
        ("ExactCache", ExactCache(SuperposedRead(), 4, 8.0, 1.0)),
        ("SettlingRead", SettlingRead(SuperposedRead(), 2)),
        ("SettlingRead(ExactCache)",
         SettlingRead(ExactCache(SuperposedRead(), 4, 8.0, 1.0), 2)),
    ]


class Sloppy:
    """Violates every property on purpose, so the suite can be shown to bite."""

    def begin(self, width):
        pass                                    # never resets

    def read(self, readable, key):
        readable *= 1.0000001                   # mutates the store
        key += 1e-9                             # mutates the key
        return (readable @ key)[:-1]            # wrong shape

    def observe(self, store, key, value, commitment):
        store += 1e-9                           # mutates the store


def exercise(strategy, store, key, rng):
    """Put a strategy through a short run: begin, some writes, a read."""
    strategy.begin(WIDTH)
    for _ in range(3):
        strategy.observe(store, rng.normal(size=WIDTH),
                         rng.normal(size=WIDTH), 1.0)
    return strategy.read(store, key)


class EveryStrategyHonoursTheContract(unittest.TestCase):

    def setUp(self):
        self.rng = np.random.default_rng(0)
        self.store = self.rng.normal(size=(WIDTH, WIDTH))
        self.key = self.rng.normal(size=WIDTH)

    def test_all_satisfy_the_protocol(self):
        for name, strategy in implementations():
            with self.subTest(name):
                self.assertIsInstance(strategy, Retrieval)

    def test_read_returns_one_value_per_store_row(self):
        for name, strategy in implementations():
            with self.subTest(name):
                strategy.begin(WIDTH)
                out = strategy.read(self.store, self.key)
                self.assertEqual(np.shape(out), (WIDTH,))

    def test_read_does_not_mutate_the_store_or_the_key(self):
        for name, strategy in implementations():
            with self.subTest(name):
                store, key = self.store.copy(), self.key.copy()
                strategy.begin(WIDTH)
                strategy.read(store, key)
                np.testing.assert_array_equal(store, self.store)
                np.testing.assert_array_equal(key, self.key)

    def test_observe_does_not_mutate_the_store(self):
        for name, strategy in implementations():
            with self.subTest(name):
                store = self.store.copy()
                strategy.begin(WIDTH)
                strategy.observe(store, self.key.copy(),
                                 self.rng.normal(size=WIDTH), 1.0)
                np.testing.assert_array_equal(store, self.store)

    def test_begin_resets_per_run_state(self):
        """Two runs that see the same thing must return the same thing. A
        strategy carrying state across `begin` makes a sequence's result depend
        on what ran before it, which no single run can reveal."""
        for name, strategy in implementations():
            with self.subTest(name):
                first = exercise(strategy, self.store.copy(), self.key,
                                 np.random.default_rng(1))
                second = exercise(strategy, self.store.copy(), self.key,
                                  np.random.default_rng(1))
                np.testing.assert_allclose(first, second)

    def test_read_is_deterministic(self):
        for name, strategy in implementations():
            with self.subTest(name):
                strategy.begin(WIDTH)
                np.testing.assert_allclose(
                    strategy.read(self.store, self.key),
                    strategy.read(self.store, self.key))


class TheSuiteBites(unittest.TestCase):
    """Rule 10, applied to the suite rather than to the code it checks."""

    def setUp(self):
        rng = np.random.default_rng(0)
        self.store = rng.normal(size=(WIDTH, WIDTH))
        self.key = rng.normal(size=WIDTH)

    def test_a_wrong_shape_is_caught(self):
        out = Sloppy().read(self.store.copy(), self.key.copy())
        self.assertNotEqual(np.shape(out), (WIDTH,))

    def test_a_store_mutated_by_read_is_caught(self):
        store = self.store.copy()
        Sloppy().read(store, self.key.copy())
        self.assertFalse(np.array_equal(store, self.store))

    def test_a_key_mutated_by_read_is_caught(self):
        key = self.key.copy()
        Sloppy().read(self.store.copy(), key)
        self.assertFalse(np.array_equal(key, self.key))

    def test_a_store_mutated_by_observe_is_caught(self):
        store = self.store.copy()
        Sloppy().observe(store, self.key.copy(), self.key.copy(), 1.0)
        self.assertFalse(np.array_equal(store, self.store))


if __name__ == "__main__":
    unittest.main()
