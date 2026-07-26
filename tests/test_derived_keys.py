"""Keys a node can rebuild instead of hold.

`Wk` is a frozen random projection that never learns, so it need not be stored:
with a per-token seed, a node regenerates any row from the token id alone. That is
what lets a node be sent a **token** rather than a key vector — 32 bytes per step
at any width, against `8·d·4` for the key.

[Note 012](../docs/notes/012-broadcast-the-token.md) has the arithmetic. The test
that matters here is the one the note said was owed: **statistically equivalent is
not verified**, so this checks reconstructibility exactly and task accuracy
empirically rather than trusting the row statistics.
"""

from __future__ import annotations

import unittest
from dataclasses import replace

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset

VOCAB, WIDTH, SEED = 20, 32, 11


def build(derived: bool, seed: int = SEED, width: int = WIDTH):
    return LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=width, key_scale=0.5,
        derived_keys=derived, seed=seed))


class OffByDefault(unittest.TestCase):

    def test_the_default_is_stored_keys(self):
        self.assertFalse(LocalMemoryConfig(vocab_size=VOCAB).derived_keys)

    def test_off_reproduces_the_stored_projection_exactly(self):
        plain = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=VOCAB, d_model=WIDTH, key_scale=0.5, seed=SEED))
        np.testing.assert_array_equal(build(False).wk, plain.wk)
        np.testing.assert_array_equal(build(False).wv, plain.wv)


class ARowIsRebuildableFromTheTokenAlone(unittest.TestCase):
    """The whole point: a node holds a seed, not a table."""

    def test_a_node_can_regenerate_any_row_without_the_model(self):
        """Reconstruct row by row, out of order, from `(seed, token)` only.

        Out of order deliberately. If a row depended on draws made before it —
        which is exactly what a single `rng` over the whole table gives — then a
        node could only rebuild the table by rebuilding all of it, and would have
        to hold the result. Rebuilding token 17 without touching tokens 0..16 is
        the property that makes the broadcast cheap.
        """
        model = build(True)
        spread = 0.5 / np.sqrt(WIDTH)
        for token in (17, 3, 11, 0, VOCAB - 1):
            with self.subTest(token=token):
                rebuilt = np.random.default_rng((SEED, token)).normal(
                    0.0, spread, WIDTH)
                np.testing.assert_array_equal(model.wk[token], rebuilt)

    def test_different_seeds_give_different_projections(self):
        self.assertFalse(np.array_equal(build(True, seed=1).wk,
                                        build(True, seed=2).wk))

    def test_every_token_gets_a_different_row(self):
        model = build(True)
        for token in range(1, VOCAB):
            self.assertFalse(np.array_equal(model.wk[0], model.wk[token]),
                             f"token {token} has the same key as token 0")


class TheProjectionHasTheSameShapeOfRandomness(unittest.TestCase):

    def test_row_norms_match_the_stored_projection(self):
        stored = np.linalg.norm(build(False, width=256).wk, axis=1)
        derived = np.linalg.norm(build(True, width=256).wk, axis=1)
        np.testing.assert_allclose(derived.mean(), stored.mean(), rtol=0.05)

    def test_cross_token_overlap_matches(self):
        """The quantity that governs interference, so the one that must match."""
        def overlap(model):
            rows = model.wk / np.linalg.norm(model.wk, axis=1, keepdims=True)
            products = rows @ rows.T
            return np.abs(products[np.triu_indices(VOCAB, 1)]).mean()
        self.assertAlmostEqual(overlap(build(True, width=256)),
                               overlap(build(False, width=256)), delta=0.02)


class ItScoresTheSameOnTheTask(unittest.TestCase):
    """Statistically equivalent is not verified. This is the verification."""

    def test_accuracy_is_within_seed_noise_of_stored_keys(self):
        task = MqarConfig(n_pairs=3, seq_len=48, n_keys=16, n_values=6,
                          autoregressive=True, filler="random", seed=404)
        train = dataset(task, 60)
        test = dataset(replace(task, seed=task.seed + 7), 30)

        def score(derived, seed):
            rng = np.random.default_rng(seed)
            model = LocalAssociativeMemory(LocalMemoryConfig(
                vocab_size=task.vocab_size, d_model=32, lr=0.05, key_scale=0.5,
                derived_keys=derived, seed=seed))
            for _ in range(3):
                for index in rng.permutation(len(train)):
                    tokens = np.asarray(train[index].tokens)
                    targets = np.roll(tokens, -1)
                    scored = np.ones(len(tokens), dtype=bool)
                    scored[-1] = False
                    model.run(tokens, targets, scored, learn=True)
            correct = total = 0
            for sequence in test:
                tokens = np.asarray(sequence.tokens)
                predicted = model.run(tokens)
                for q in sequence.query_positions:
                    correct += predicted[q] == tokens[q + 1]
                    total += 1
            return correct / total

        stored = [score(False, s) for s in (1, 2, 3)]
        derived = [score(True, s) for s in (1, 2, 3)]
        spread = max(stored) - min(stored)
        gap = abs(np.mean(derived) - np.mean(stored))
        self.assertLessEqual(
            gap, max(spread, 0.05),
            f"derived keys score {np.mean(derived):.3f} against "
            f"{np.mean(stored):.3f}, a gap of {gap:.3f} against a seed spread "
            f"of {spread:.3f} -- larger than noise")


class ConflictingKeySchemesAreRefused(unittest.TestCase):

    def test_derived_and_sparse_cannot_both_be_asked_for(self):
        """Both build `Wk`, and sparse has no per-token derivation yet.

        Silently letting one win would give a model that is not what either
        setting describes.
        """
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH,
                              derived_keys=True, key_active=4)


if __name__ == "__main__":
    unittest.main()
