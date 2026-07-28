"""Sparse non-negative keys -- biology's answer to interference, measured here.

The mechanism is real and on MQAR the result is negative: sparse keys are worse
than dense signed ones at every sparsity tested. The tests below fix the
mechanism in place so that negative result is about the idea rather than about a
buggy implementation of it.

**On the CORPUS the result reverses, and the knob had been left off because of
the number above.** Three seeds at width 64, 60,000 characters: dense 5.524,
`key_active` 8 at 5.346, `key_active` 4 at 5.342 -- about 0.18 bits, roughly
three times the seed spread, for no extra state at all (decision 67). A
mechanism measured only on the task it was designed for is not measured, and
this one had sat default-off through every language result the project produced.

Sparse keys are also CHEAPER ON THE WIRE than dense ones, which makes them the
rare mechanism that helps the loss and the C1 budget at the same time -- but
only once a node can rebuild one without being sent it. See
`DerivedSparseKeys` below.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)


def build(active: int, d: int = 64, seed: int = 1):
    return LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=20, d_model=d, key_active=active, key_scale=0.5, seed=seed))


class SparseKeysAreSparseAndNonNegative(unittest.TestCase):

    def test_every_key_has_exactly_the_requested_number_of_active_dimensions(self):
        for active in (1, 4, 16, 64):
            with self.subTest(active=active):
                model = build(active)
                counts = (model.wk != 0).sum(axis=1)
                np.testing.assert_array_equal(counts, np.full(20, active))

    def test_sparse_keys_are_non_negative(self):
        """The biologically faithful part, and the part that costs the most.

        A firing rate cannot go below zero. It is also what makes sparsity worth
        anything: a DENSE non-negative code has every pair of keys strongly
        overlapping, so non-negativity without sparsity is far worse than the
        signed code it replaces.
        """
        self.assertTrue((build(8).wk >= 0).all())

    def test_sparse_keys_have_the_same_length_as_dense_ones(self):
        """Otherwise this sweeps key SCALE while claiming to sweep sparsity.

        g3-02 measured a width-32 model at 0.263 with unit-norm keys and 0.960
        with the same keys scaled by 0.71. Letting the norm drift with `active`
        would confound the two, and that confound has already cost this project
        one wrong headline.
        """
        scale = 0.5
        for active in (4, 16, 64):
            with self.subTest(active=active):
                sparse = np.linalg.norm(build(active).wk, axis=1)
                np.testing.assert_allclose(sparse, scale, rtol=1e-12)
        # Dense rows are random, so their norms only CONCENTRATE on the scale --
        # comparing a deterministic value to a sample mean at rtol 1e-12 was the
        # first version of this test and it failed for that reason rather than
        # for a defect in the code.
        dense = np.linalg.norm(build(0).wk, axis=1)
        np.testing.assert_allclose(dense.mean(), scale, rtol=0.05)

    def test_different_tokens_get_different_active_sets(self):
        model = build(4)
        patterns = {tuple(np.flatnonzero(row)) for row in model.wk}
        self.assertEqual(len(patterns), 20,
                         "two tokens share an active set, so they are the same "
                         "address and can never be told apart")


class ZeroKeepsTheOldModel(unittest.TestCase):
    """Every result before this knob existed was measured at key_active=0."""

    def test_zero_gives_the_dense_signed_projection(self):
        model = build(0)
        self.assertTrue((model.wk < 0).any(),
                        "dense keys should be signed, not non-negative")
        self.assertGreater((model.wk != 0).mean(), 0.99)

    def test_zero_is_bit_identical_to_a_model_built_without_the_field(self):
        plain = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=20, d_model=64, key_scale=0.5, seed=1))
        np.testing.assert_array_equal(build(0).wk, plain.wk)
        np.testing.assert_array_equal(build(0).wv, plain.wv)


class DerivedSparseKeys(unittest.TestCase):
    """A sparse key a node can rebuild from `(seed, token)` and nothing else.

    **This is what makes the 0.18 bits usable rather than merely true.** Under
    C1 a node holds only its own slice and cannot be sent a key table; dense
    derived keys have been rebuildable from `(seed, token)` since note 012, and
    sparse ones were not, so the cheaper-on-the-wire scheme was the one a
    distributed node could not use.

    `derived_keys` and `key_active` were REFUSED as conflicting. They do not
    conflict -- nobody had written the per-token draw.
    """

    def test_a_row_rebuilds_from_seed_and_token_alone(self):
        config = LocalMemoryConfig(vocab_size=20, d_model=32, seed=7,
                                   key_active=4, derived_keys=True)
        model = LocalAssociativeMemory(config)
        for token in (0, 5, 19):
            active = np.random.default_rng((7, token)).choice(
                32, 4, replace=False)
            rebuilt = np.zeros(32)
            rebuilt[active] = config.key_scale / np.sqrt(4)
            np.testing.assert_allclose(rebuilt, model.wk[token])

    def test_a_row_does_not_depend_on_the_rows_drawn_before_it(self):
        """The property that makes it reconstructible OUT OF ORDER, which is
        what a node arriving late actually needs. A sequential draw gives every
        row a dependence on its predecessors and looks identical from outside."""
        wide = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=40, d_model=32, seed=7, key_active=4,
            derived_keys=True)).wk
        narrow = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=8, d_model=32, seed=7, key_active=4,
            derived_keys=True)).wk
        np.testing.assert_array_equal(wide[:8], narrow)

    def test_it_is_still_sparse(self):
        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=20, d_model=32, seed=7, key_active=4,
            derived_keys=True))
        for token in range(20):
            self.assertEqual(int((model.wk[token] != 0).sum()), 4)

    def test_undrawn_and_derived_differ(self):
        """If they matched, the per-token draw would not be doing anything and
        every test above would pass against the sequential implementation."""
        common = dict(vocab_size=20, d_model=32, seed=7, key_active=4)
        self.assertFalse(np.allclose(
            LocalAssociativeMemory(LocalMemoryConfig(**common,
                                                     derived_keys=True)).wk,
            LocalAssociativeMemory(LocalMemoryConfig(**common,
                                                     derived_keys=False)).wk))


class ImpossibleSparsitiesAreRefused(unittest.TestCase):

    def test_more_active_dimensions_than_exist(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=20, d_model=64, key_active=65)

    def test_negative(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=20, d_model=64, key_active=-1)


if __name__ == "__main__":
    unittest.main()
