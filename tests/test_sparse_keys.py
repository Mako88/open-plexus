"""Sparse non-negative keys -- biology's answer to interference, measured here.

The mechanism is real and the result is negative: on this task sparse keys are
worse than dense signed ones at every sparsity tested. The tests below fix the
mechanism in place so the negative result is about the idea rather than about a
buggy implementation of it.
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


class ImpossibleSparsitiesAreRefused(unittest.TestCase):

    def test_more_active_dimensions_than_exist(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=20, d_model=64, key_active=65)

    def test_negative(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=20, d_model=64, key_active=-1)


if __name__ == "__main__":
    unittest.main()
