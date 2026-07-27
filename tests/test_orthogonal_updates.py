"""Our rank collapse is real, and it is NOT the disease Muon cures.

Boeshertz et al. (arXiv:2606.11123) found local feedback rules fail because
their updates collapse in rank — effective rank 12 where backprop reaches 100 —
and recovered CIFAR-100 ResNet-18 from 1.4% to 46.1% with Muon-style
orthogonalisation. Note 036 recorded it as the intervention with the largest
measured effect anywhere in the scan, and note 035 had already measured our own
store at effective rank ~3 at every width.

So it was tried. Measured on Tiny Shakespeare, width 64, window 32:

    accumulated update, effective rank raw              2.22
    accumulated update, effective rank orthogonalised  11.29
    window length                                      32

    orthogonal_every     bits per character
                   0     5.588
                   8     5.716
                  32     5.976
                 128     5.867

**The disease is present — rank 2.22 out of a possible 32 — the cure raises the
rank fivefold, and prediction gets WORSE.**

The reason matters more than the result. Boeshertz's rank collapse is a *learning
rule* failing to explore directions that carry signal. **Ours is the data
genuinely having few directions**: note 035 established the store is a bigram
count table over 66 characters, and such a table is low-rank because English is.
Forcing rank onto an update whose target is low-rank spreads its magnitude into
directions that carry nothing.

**Same symptom, different cause, and the cure for theirs actively hurts ours.**
That is worth a test rather than a paragraph, because "effective rank is low"
now has two readings in this repository and only one of them is a defect.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig, _orthogonalise)

VOCAB, WIDTH = 48, 64


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=5)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


def effective_rank(matrix: np.ndarray) -> float:
    values = np.linalg.svd(matrix, compute_uv=False)
    return float((values ** 2).sum() / (values[0] ** 2))


class OrthogonalisingDoesWhatItSays(unittest.TestCase):

    def setUp(self):
        rng = np.random.default_rng(4)
        # The sum of many outer products, which is exactly the shape an
        # accumulated delta-rule update has: full rank on paper, with the
        # magnitude concentrated in a few directions.
        self.low = sum(np.outer(rng.normal(size=VOCAB), rng.normal(size=WIDTH))
                       for _ in range(20))
        self.thin = np.outer(rng.normal(size=VOCAB), rng.normal(size=WIDTH))

    def test_it_RAISES_the_effective_rank(self):
        """5.90 to 12.74 on a 20-term sum."""
        self.assertGreater(effective_rank(_orthogonalise(self.low)),
                           effective_rank(self.low) * 2)

    def test_it_CANNOT_create_rank_from_nothing(self):
        """**The property that explains why the window is needed.**

        Orthogonalising equalises the singular values a matrix already has; it
        cannot invent directions. A single delta-rule step is `error ⊗
        retrieval` — rank one — and stays rank one however hard it is
        orthogonalised. That is why updates must be accumulated before there is
        anything to do, and it is not obvious from the name of the operation.
        """
        self.assertLess(effective_rank(_orthogonalise(self.thin)), 1.05)

    def test_it_preserves_the_MAGNITUDE(self):
        """Orthogonalising is meant to change an update's shape, not its size.
        If it changed both, any effect would be confounded with a learning-rate
        change and the comparison would mean nothing."""
        self.assertAlmostEqual(float(np.linalg.norm(_orthogonalise(self.low))),
                               float(np.linalg.norm(self.low)), places=6)

    def test_a_zero_matrix_survives(self):
        zero = np.zeros((VOCAB, WIDTH))
        np.testing.assert_allclose(_orthogonalise(zero), zero)

    def test_it_works_on_a_WIDE_matrix_too(self):
        """The iteration transposes to keep the products small, and a bug in
        that branch would be silent on our square-ish shapes."""
        rng = np.random.default_rng(9)
        wide = sum(np.outer(rng.normal(size=8), rng.normal(size=64))
                   for _ in range(6))
        self.assertGreater(effective_rank(_orthogonalise(wide)),
                           effective_rank(wide))


class TheUpdateIsLowRankBecauseTheTARGETIs(unittest.TestCase):
    """The finding, stated as the thing that distinguishes the two diseases."""

    def test_an_accumulated_update_is_far_below_its_window(self):
        model = build(orthogonal_every=32)
        tokens = np.tile(np.arange(6), 12).astype(np.int64)
        targets = np.concatenate([tokens[1:], tokens[-1:]])
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        model.run(tokens, targets, scored, learn=True)
        rank = effective_rank(model.pending_update[:, 0, :])
        self.assertLess(rank, 16.0,
                        f"accumulated rank {rank:.2f} of a 32-step window; if "
                        f"this were high there would be no collapse to explain")


class TheFlagIsOffByDefaultAndGuarded(unittest.TestCase):

    def test_the_default_is_off(self):
        self.assertEqual(
            LocalMemoryConfig(vocab_size=8, d_model=8).orthogonal_every, 0)

    def test_off_changes_nothing(self):
        tokens = np.tile(np.arange(6), 6).astype(np.int64)
        targets = np.concatenate([tokens[1:], tokens[-1:]])
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        a, b = build(), build(orthogonal_every=0)
        for model in (a, b):
            model.run(tokens, targets, scored, learn=True)
        np.testing.assert_array_equal(a.wo, b.wo)

    def test_a_negative_window_is_refused(self):
        with self.assertRaises(ValueError):
            build(orthogonal_every=-1)

    def test_it_CHANGES_the_readout(self):
        """The vacuity guard: a window that were counted but never applied
        would pass everything above."""
        tokens = np.tile(np.arange(6), 12).astype(np.int64)
        targets = np.concatenate([tokens[1:], tokens[-1:]])
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        a, b = build(), build(orthogonal_every=8)
        for model in (a, b):
            model.run(tokens, targets, scored, learn=True)
        self.assertFalse(np.allclose(a.wo, b.wo),
                         "orthogonalising changed nothing in the readout")


if __name__ == "__main__":
    unittest.main()
