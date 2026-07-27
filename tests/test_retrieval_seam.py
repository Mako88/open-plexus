"""The retrieval seam: a strategy from outside the model has to work end to end.

`openplexus/keys.py` proved its seam by defining a key source that exists nowhere
in the model and running it through `model.run`. This does the same for
retrieval, and the bar is the same: **if a new strategy costs anything more than
a class and one assignment, the seam has not been built.**

Retrieval is the seam with the most at stake. `r = M @ key` is a SUM, and it is
the common cause of every mechanism this project has refuted — readout bias,
competitive retrieval, orthogonal updates. g11-05 then measured the consequence
on a second axis: sixteen times the training text moves our loss by 0.012 bits
against a backprop control that moves cleanly. A suspect that cannot be swapped
out cannot be tested.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.retrieval import (
    ExactCache, Retrieval, SettlingRead, SuperposedRead, build)


class ConstantRead:
    """A strategy defined nowhere in the model, returning a fixed vector.

    Deliberately degenerate. The question is whether the model USES it, not
    whether it is a good idea, and a strategy whose output cannot be confused
    with the real one makes the answer unambiguous.
    """

    def __init__(self, fill: float = 0.25) -> None:
        self.fill = fill
        self.begins = 0
        self.observations = []

    def begin(self, width: int) -> None:
        self.begins += 1
        self.width = width

    def read(self, readable: np.ndarray, key: np.ndarray) -> np.ndarray:
        return np.full(readable.shape[0], self.fill)

    def observe(self, store, key, value, commitment) -> None:
        self.observations.append((store.copy(), key.copy(), float(commitment)))


def model_for(**overrides) -> LocalAssociativeMemory:
    config = LocalMemoryConfig(vocab_size=11, d_model=8, seed=5,
                               derived_keys=True, **overrides)
    model = LocalAssociativeMemory(config)
    model.wo[:] = model.wv
    return model


def run_once(model, length: int = 24):
    rng = np.random.default_rng(1)
    tokens = rng.integers(0, 11, size=length)
    targets = np.concatenate([tokens[1:], tokens[-1:]])
    scored = np.ones(len(tokens), dtype=bool)
    scored[-1] = False
    return model.run(tokens, targets, scored, learn=True)


class AStrategyFromOutsideTheModel(unittest.TestCase):

    def test_swapping_it_in_changes_the_predictions(self):
        """The connection test. Perturb the component, assert the output moves.
        A seam whose replacement changes nothing is not wired in."""
        before = np.asarray(run_once(model_for())).ravel().tolist()
        model = model_for()
        model.retrieval = ConstantRead()
        after = np.asarray(run_once(model)).ravel().tolist()
        self.assertNotEqual(before, after)

    def test_it_costs_a_class_and_one_assignment(self):
        """ConstantRead subclasses nothing, imports nothing from the model, and
        is not mentioned by any config field. If this test needs a flag added
        to LocalMemoryConfig to pass, the seam has leaked."""
        self.assertIsInstance(ConstantRead(), Retrieval)
        model = model_for()
        model.retrieval = ConstantRead()
        run_once(model)

    def test_begin_is_called_once_per_run_not_once_per_position(self):
        """Per-run state that survived into the next run would make a
        sequence's result depend on what ran before it -- which no existing
        test would catch, and every measurement would inherit."""
        model = model_for()
        strategy = ConstantRead()
        model.retrieval = strategy
        run_once(model)
        self.assertEqual(strategy.begins, 1)
        run_once(model)
        self.assertEqual(strategy.begins, 2)

    def test_the_width_it_is_given_is_the_model_width(self):
        model = model_for()
        strategy = ConstantRead()
        model.retrieval = strategy
        run_once(model)
        self.assertEqual(strategy.width, 8)


class WhatObserveIsTold(unittest.TestCase):

    def test_the_store_it_sees_is_the_state_before_the_write(self):
        """The cache admits by what the superposed store FAILED to absorb, so
        it must be told what the store knew, not what it is about to be told.
        Being handed the post-write store would make every residual near zero
        and the cache would admit nothing -- silently, and it would still run."""
        model = model_for()
        strategy = ConstantRead()
        model.retrieval = strategy
        run_once(model)
        self.assertGreater(len(strategy.observations), 1)
        first, second = strategy.observations[0], strategy.observations[1]
        self.assertTrue(np.allclose(first[0], 0.0),
                        "the first write should see an empty store")
        self.assertFalse(np.allclose(second[0], 0.0),
                         "the second should see the first write's effect")

    def test_commitment_is_the_write_gate(self):
        model = model_for(write_gate=0.5)
        strategy = ConstantRead()
        model.retrieval = strategy
        run_once(model)
        self.assertTrue(all(c == 0.5 for _, _, c in strategy.observations))


class TheStrategiesCompose(unittest.TestCase):

    def test_the_default_is_the_bare_sum(self):
        built = build(LocalMemoryConfig(vocab_size=5, d_model=4))
        self.assertIsInstance(built, SuperposedRead)

    def test_a_cache_wraps_the_sum_and_settling_wraps_the_cache(self):
        """Nesting order is load-bearing: read the store, add the cache's
        selective contribution, then settle over the total. That is the order
        the operations had when they were inline, and swapping it would change
        results while every test still passed."""
        built = build(LocalMemoryConfig(vocab_size=5, d_model=4, cache_slots=4,
                                        retrieval_steps=2))
        self.assertIsInstance(built, SettlingRead)
        self.assertIsInstance(built.inner, ExactCache)
        self.assertIsInstance(built.inner.inner, SuperposedRead)

    def test_the_bare_sum_is_exactly_the_matrix_product(self):
        rng = np.random.default_rng(0)
        store, key = rng.normal(size=(6, 6)), rng.normal(size=6)
        self.assertTrue(np.allclose(SuperposedRead().read(store, key),
                                    store @ key))

    def test_one_settling_step_is_the_identity(self):
        """`retrieval_steps` of 1 is the default, so the refuted mechanism must
        cost exactly nothing when off."""
        rng = np.random.default_rng(0)
        store, key = rng.normal(size=(6, 6)), rng.normal(size=6)
        settling = SettlingRead(SuperposedRead(), 1)
        self.assertTrue(np.allclose(settling.read(store, key), store @ key))


class TheCacheOnlyContributesWhenItMatches(unittest.TestCase):

    def test_an_empty_cache_contributes_nothing(self):
        rng = np.random.default_rng(0)
        store, key = rng.normal(size=(6, 6)), rng.normal(size=6)
        cache = ExactCache(SuperposedRead(), slots=4, sharpness=8.0, weight=1.0)
        cache.begin(6)
        self.assertTrue(np.allclose(cache.read(store, key), store @ key))

    def test_a_query_matching_nothing_contributes_nothing(self):
        """A softmax returns a convex combination whatever it is given, so
        without the gate the cache adds a full-magnitude vector even when it
        holds nothing resembling the query -- noise by construction."""
        store = np.zeros((4, 4))
        cache = ExactCache(SuperposedRead(), slots=2, sharpness=8.0, weight=1.0)
        cache.begin(4)
        held = np.array([1.0, 0.0, 0.0, 0.0])
        cache.observe(store, held, np.array([0.0, 1.0, 0.0, 0.0]), 1.0)
        opposed = np.array([-1.0, 0.0, 0.0, 0.0])
        self.assertTrue(np.allclose(cache.read(store, opposed), store @ opposed))

    def test_a_matching_query_does_contribute(self):
        store = np.zeros((4, 4))
        cache = ExactCache(SuperposedRead(), slots=2, sharpness=8.0, weight=1.0)
        cache.begin(4)
        held = np.array([1.0, 0.0, 0.0, 0.0])
        cache.observe(store, held, np.array([0.0, 1.0, 0.0, 0.0]), 1.0)
        self.assertFalse(np.allclose(cache.read(store, held), store @ held))

    def test_a_weaker_binding_does_not_evict_a_stronger_one(self):
        """**Admission is by residual, not by recency**, and the difference is
        invisible unless a later write is deliberately made less novel than an
        earlier one. A cache admitting whatever arrived last keeps the last N
        bindings rather than the ones the store could not absorb -- HOLA
        measured that at 0.34 absolute worse -- and it still runs, still fills,
        and still contributes on every read.

        One slot, so the two writes genuinely contend."""
        store = np.zeros((4, 4))
        cache = ExactCache(SuperposedRead(), slots=1, sharpness=8.0, weight=1.0)
        cache.begin(4)
        novel_key = np.array([1.0, 0.0, 0.0, 0.0])
        cache.observe(store, novel_key, np.array([0.0, 1.0, 0.0, 0.0]), 1.0)
        self.assertAlmostEqual(float(cache.score[0]), 1.0)
        # Half the novelty of the first, so residual ordering decides it.
        cache.observe(store, np.array([0.0, 0.0, 1.0, 0.0]),
                      np.array([0.0, 0.0, 0.0, 0.5]), 1.0)
        self.assertTrue(np.allclose(cache.key[0], novel_key),
                        "the weaker binding evicted the stronger one, so "
                        "admission is by recency rather than by residual")
        self.assertAlmostEqual(float(cache.score[0]), 1.0)

    def test_a_zero_commitment_write_is_never_admitted(self):
        """Residual is novelty TIMES commitment. A write the gate suppressed
        entirely is not a binding the cache should be holding."""
        store = np.zeros((4, 4))
        cache = ExactCache(SuperposedRead(), slots=2, sharpness=8.0, weight=1.0)
        cache.begin(4)
        cache.observe(store, np.array([1.0, 0.0, 0.0, 0.0]),
                      np.array([0.0, 1.0, 0.0, 0.0]), 0.0)
        self.assertTrue(np.allclose(cache.score, 0.0))


if __name__ == "__main__":
    unittest.main()
