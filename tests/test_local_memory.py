"""Tests for the locality-respecting associative memory.

There are no gradients to check here — the whole point is that no backward pass
exists — so the weight of the suite falls on two things: that the model cannot
see its own target, and that each part is actually connected. A memory that
never stored anything, or a readout that ignored what it retrieved, would sit at
chance and look exactly like an honest negative result for locality.

That is the failure this project keeps meeting, and it would be at its most
persuasive here: G1 is the stage where a null is the *expected* outcome.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import LocalAssociativeMemory, LocalMemoryConfig

CFG = LocalMemoryConfig(vocab_size=12, d_model=32, seed=1)


def a_sequence(length=12, seed=0):
    rng = np.random.default_rng(seed)
    tokens = rng.integers(0, CFG.vocab_size, length)
    targets = np.roll(tokens, -1)
    scored = np.ones(length, dtype=bool)
    scored[-1] = False
    return tokens, targets, scored


class TestCannotSeeItsOwnTarget(unittest.TestCase):
    def test_prediction_at_t_does_not_depend_on_later_tokens(self):
        """The one that would manufacture a perfect score out of nothing.

        The store binds (t-1 → t) *before* the retrieval at t, which is safe
        because that binding is entirely in the past. Binding (t → t+1) first
        would put the answer in the memory before it is queried, and the model
        would score 1.000 for a reason that has nothing to do with learning.
        """
        model = LocalAssociativeMemory(CFG)
        base = np.array([1, 2, 3, 4, 5, 6, 7, 8, 9])
        changed = base.copy()
        changed[5:] = 0
        np.testing.assert_array_equal(model.run(base)[:5], model.run(changed)[:5])

    def test_memory_does_not_persist_between_sequences(self):
        """`M` is per-sequence working memory. If it leaked across sequences the
        model would accumulate the training set and a held-out split would stop
        being held out.

        The readout must be trained first. The original version compared an
        untrained model's predictions, which are `argmax` of an all-zero readout
        — constant 0 whatever the memory holds. It compared two constants, and
        `local-memory-persists-across-sequences` survived because of it. Rule 9's
        failure mode: an assertion on a quantity something else was pinning.
        """
        model = LocalAssociativeMemory(CFG)
        tokens, targets, scored = a_sequence(seed=5)
        for _ in range(30):
            model.run(tokens, targets, scored, learn=True)
        self.assertGreater(np.abs(model.wo).max(), 0.0, "readout never trained")

        first = model.run(tokens)
        model.run(np.array([7, 3, 7, 3, 7, 3, 7, 3]))
        np.testing.assert_array_equal(first, model.run(tokens))


class TestEachPartIsConnected(unittest.TestCase):
    """Rule 6. Each of these would pass against a model missing that part, while
    the model still ran and still produced plausible output."""

    def test_the_same_token_retrieves_what_followed_IT_in_THIS_sequence(self):
        """The mechanism, exercised through the model rather than reimplemented.

        The first version of this test rebuilt the memory by hand inside the
        test and asserted on that. It passed, and it tested the reconstruction
        rather than the model — the mutation
        `local-store-binds-the-current-token-to-itself` survived the whole suite
        because nothing ever exercised the real store.

        The property that separates the two bindings: `a` is followed by X in one
        sequence and by Y in another, with everything else identical. Binding
        (t-1 -> t) retrieves what followed `a` *in this sequence* and can tell
        them apart. Binding (t -> t) retrieves something about `a` itself, which
        is the same in both, and cannot.
        """
        config = LocalMemoryConfig(vocab_size=8, d_model=128, seed=2, lr=0.2)
        model = LocalAssociativeMemory(config)
        a, x, y, pad = 1, 2, 3, 0
        with_x = np.array([a, x, pad, pad, a, x])
        with_y = np.array([a, y, pad, pad, a, y])
        scored = np.array([False, False, False, False, True, False])

        for _ in range(60):
            for tokens in (with_x, with_y):
                model.run(tokens, np.roll(tokens, -1), scored, learn=True)

        self.assertEqual(model.run(with_x)[4], x)
        self.assertEqual(model.run(with_y)[4], y)

    def test_the_store_actually_stores(self):
        """A model whose memory stayed empty would predict a constant."""
        model = LocalAssociativeMemory(CFG)
        tokens, targets, scored = a_sequence()
        model.run(tokens, targets, scored, learn=True)
        self.assertGreater(np.abs(model.wo).max(), 0.0,
                           "the delta rule never fired, so nothing was learned")

    def test_learning_changes_predictions(self):
        model = LocalAssociativeMemory(CFG)
        tokens, targets, scored = a_sequence(seed=3)
        before = model.run(tokens)
        for _ in range(20):
            model.run(tokens, targets, scored, learn=True)
        self.assertFalse(np.array_equal(before, model.run(tokens)))

    def test_learning_is_off_by_default(self):
        """New mechanisms default to off, and a run that silently trained would
        contaminate every evaluation."""
        model = LocalAssociativeMemory(CFG)
        tokens, targets, scored = a_sequence()
        model.run(tokens)
        np.testing.assert_array_equal(model.wo, np.zeros_like(model.wo))

    def test_d_model_changes_the_memory_width(self):
        for d in (8, 64):
            model = LocalAssociativeMemory(LocalMemoryConfig(
                vocab_size=9, d_model=d, seed=0))
            self.assertEqual(model.wk.shape[1], d)
            self.assertEqual(model.wo.shape[1], d)

    def test_seed_changes_the_projections(self):
        a = LocalAssociativeMemory(LocalMemoryConfig(vocab_size=9, d_model=16, seed=1))
        b = LocalAssociativeMemory(LocalMemoryConfig(vocab_size=9, d_model=16, seed=2))
        self.assertGreater(np.abs(a.wk - b.wk).max(), 0.0)

    def test_decay_changes_behaviour(self):
        """A decay that did nothing would make the parameter a lie, and it is
        the one knob available for bounding interference in long sequences."""
        tokens, targets, scored = a_sequence(length=30, seed=4)
        outs = []
        for decay in (1.0, 0.8):
            model = LocalAssociativeMemory(LocalMemoryConfig(
                vocab_size=CFG.vocab_size, d_model=32, seed=1, decay=decay))
            for _ in range(10):
                model.run(tokens, targets, scored, learn=True)
            outs.append(model.run(tokens))
        self.assertFalse(np.array_equal(outs[0], outs[1]))


class TestChurn(unittest.TestCase):
    """A machine leaving permanently — C3's failure, not C2's.

    A dropped message is transient; the next one arrives. A departed machine
    takes its share of the state and never comes back. The tests that matter are
    that it really is permanent, and that what it takes is capacity rather than
    stored memories.
    """

    def test_a_departed_machine_cannot_come_back(self):
        """The property that makes this churn rather than a dropout.

        The delta rule multiplies by the retrieved vector, which is zero in dead
        dimensions, so the readout's columns there stay zero without masking. If
        they ever refilled, the model would be quietly recovering capacity that
        no longer exists and every churn measurement would be too optimistic.
        """
        model = LocalAssociativeMemory(CFG)
        tokens, targets, scored = a_sequence(seed=9)
        for _ in range(20):
            model.run(tokens, targets, scored, learn=True)
        model.ablate(range(8))
        for _ in range(40):
            model.run(tokens, targets, scored, learn=True)
        np.testing.assert_array_equal(model.wo[:, :8], np.zeros_like(model.wo[:, :8]))
        np.testing.assert_array_equal(model.wk[:, :8], np.zeros_like(model.wk[:, :8]))

    def test_surviving_width_reports_the_honest_denominator(self):
        model = LocalAssociativeMemory(CFG)
        self.assertEqual(model.surviving_width(), CFG.d_model)
        model.ablate([0, 1, 2])
        self.assertEqual(model.surviving_width(), CFG.d_model - 3)

    def test_ablation_changes_predictions(self):
        """Rule 6. An ablation the forward pass ignored would make every churn
        result a measurement of nothing."""
        model = LocalAssociativeMemory(CFG)
        tokens, targets, scored = a_sequence(seed=11)
        for _ in range(30):
            model.run(tokens, targets, scored, learn=True)
        before = model.run(tokens)
        model.ablate(range(CFG.d_model // 2))
        self.assertFalse(np.array_equal(before, model.run(tokens)))

    def test_ablating_nothing_changes_nothing(self):
        model = LocalAssociativeMemory(CFG)
        tokens, targets, scored = a_sequence(seed=12)
        for _ in range(20):
            model.run(tokens, targets, scored, learn=True)
        before = model.wo.copy()
        model.ablate([])
        np.testing.assert_array_equal(model.wo, before)

    def test_rejects_a_dimension_outside_the_model(self):
        with self.assertRaises(ValueError):
            LocalAssociativeMemory(CFG).ablate([CFG.d_model])


class TestValidation(unittest.TestCase):
    def test_rejects_impossible_configurations(self):
        for bad in (dict(vocab_size=1), dict(vocab_size=8, d_model=0),
                    dict(vocab_size=8, lr=0.0), dict(vocab_size=8, decay=0.0),
                    dict(vocab_size=8, decay=1.5)):
            with self.subTest(**bad):
                with self.assertRaises(ValueError):
                    LocalMemoryConfig(**bad)

    def test_rejects_a_token_outside_the_vocabulary(self):
        with self.assertRaises(ValueError):
            LocalAssociativeMemory(CFG).run(np.array([0, CFG.vocab_size]))

    def test_learning_without_targets_raises(self):
        with self.assertRaises(ValueError):
            LocalAssociativeMemory(CFG).run(np.array([1, 2, 3]), learn=True)


if __name__ == "__main__":
    unittest.main()
