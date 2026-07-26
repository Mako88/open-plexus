"""Tests for the trainable attention model.

The important one is `test_gradients_match_finite_differences`. Hand-derived
gradients are the purest instance of the failure this project is built against:
a wrong gradient still runs, still produces a falling loss, and still optimises
*something* — just not the objective anyone wrote down. Nothing else in this file
would catch it, and neither would any experiment downstream, because a model
trained on the wrong objective produces perfectly plausible numbers.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.attention import Adam, AttentionConfig, ShiftedAttention

CFG = AttentionConfig(vocab_size=11, d_model=8, seed=3)


def a_batch(seed: int = 0, length: int = 9):
    rng = np.random.default_rng(seed)
    tokens = rng.integers(0, CFG.vocab_size, length)
    targets = np.roll(tokens, -1)
    scored = np.zeros(length, dtype=bool)
    scored[1:-1] = True          # position 0 attends to nothing; last has no target
    return tokens, targets, scored


class TestGradients(unittest.TestCase):
    def _fd_check(self, indices_for, rtol, atol, label):
        model = ShiftedAttention(CFG)
        tokens, targets, scored = a_batch()
        logits, cache = model.forward(tokens)
        _, grads = model.loss_and_backward(logits, cache, targets, scored)

        def loss_now() -> float:
            lg, ca = model.forward(tokens)
            return model.loss_and_backward(lg, ca, targets, scored)[0]

        eps = 1e-5
        for name in model.PARAM_NAMES:
            flat = model.params[name].reshape(-1)
            for index in indices_for(grads[name].reshape(-1)):
                original = flat[index]
                flat[index] = original + eps
                up = loss_now()
                flat[index] = original - eps
                down = loss_now()
                flat[index] = original

                numeric = (up - down) / (2 * eps)
                analytic = grads[name].reshape(-1)[index]
                with self.subTest(check=label, param=name, index=int(index)):
                    self.assertLess(
                        abs(numeric - analytic),
                        rtol * max(abs(numeric), abs(analytic)) + atol,
                        f"{name}[{index}]: analytic {analytic:.3e} vs "
                        f"numeric {numeric:.3e}",
                    )

    def test_gradients_match_finite_differences_where_they_are_largest(self):
        """The strict check, on the entries finite differences can resolve.

        Central differences with eps=1e-5 on a loss of order 1 have an absolute
        noise floor around 1e-10 — machine epsilon divided by eps. A gradient
        entry of magnitude 1e-9 therefore carries about 10% numerical noise, and
        a pure relative-error assertion on it tests the noise rather than the
        gradient.

        So this checks the *largest* entries per parameter, where the numeric
        estimate is trustworthy, at a tight relative tolerance and effectively no
        absolute slack. The companion test below covers the rest.

        This split exists because the first version of this test asserted a flat
        2e-4 relative error over randomly chosen entries and failed on two of
        them — `wk[43]`, analytic 9.941e-10 against numeric 9.992e-10. The
        gradients agreed to four significant figures; the tolerance model was
        wrong. Rule 10: split rather than loosen.
        """
        self._fd_check(lambda g: np.argsort(np.abs(g))[-8:],
                       rtol=1e-4, atol=1e-12, label="largest")

    def test_gradients_match_finite_differences_everywhere_within_fd_noise(self):
        """The broad check, over randomly chosen entries including tiny ones.

        Same assertion with an absolute term sized to the finite-difference noise
        floor, so entries near it are not required to beat the method measuring
        them. Kept alongside the strict check rather than replacing it: a bug
        that scaled every gradient would still be caught here, and a bug in a
        single small entry would still be caught if it exceeded the floor.
        """
        rng = np.random.default_rng(0)
        self._fd_check(lambda g: rng.choice(g.size, size=min(12, g.size), replace=False),
                       rtol=1e-4, atol=2e-9, label="broad")

    def test_every_parameter_receives_gradient(self):
        """A parameter with an all-zero gradient is disconnected from the loss —
        it would sit at its initialisation forever while the model appeared to
        train normally."""
        model = ShiftedAttention(CFG)
        tokens, targets, scored = a_batch(seed=1)
        logits, cache = model.forward(tokens)
        _, grads = model.loss_and_backward(logits, cache, targets, scored)
        for name in model.PARAM_NAMES:
            with self.subTest(param=name):
                self.assertGreater(np.abs(grads[name]).max(), 0.0)


class TestForwardIsCausal(unittest.TestCase):
    def test_position_t_does_not_depend_on_tokens_after_t(self):
        """The model must not read its own target.

        Values are shifted by one, so a mask allowing s = t would hand position
        t the embedding of token t+1 — precisely the thing it is asked to
        predict. That would train to near-perfect accuracy and mean nothing.
        """
        model = ShiftedAttention(CFG)
        rng = np.random.default_rng(5)
        base = rng.integers(0, CFG.vocab_size, 10)
        changed = base.copy()
        changed[6:] = (changed[6:] + 3) % CFG.vocab_size
        a, _ = model.forward(base)
        b, _ = model.forward(changed)
        np.testing.assert_allclose(a[:6], b[:6], atol=1e-12)

    def test_first_position_is_finite(self):
        """Position 0 attends to nothing; a naive softmax over an all-masked row
        produces nan, which would surface downstream as divergence rather than
        as the empty row it is."""
        logits, _ = ShiftedAttention(CFG).forward(np.array([3, 4, 5]))
        self.assertTrue(np.isfinite(logits).all())

    def test_output_depends_on_the_input(self):
        model = ShiftedAttention(CFG)
        a, _ = model.forward(np.array([1, 2, 3, 4]))
        b, _ = model.forward(np.array([1, 2, 3, 9]))
        self.assertGreater(np.abs(a - b).max(), 0.0)

    def test_same_seed_gives_identical_parameters(self):
        x = ShiftedAttention(AttentionConfig(vocab_size=7, d_model=4, seed=2))
        y = ShiftedAttention(AttentionConfig(vocab_size=7, d_model=4, seed=2))
        for name in x.PARAM_NAMES:
            np.testing.assert_array_equal(x.params[name], y.params[name])

    def test_different_seed_gives_different_parameters(self):
        x = ShiftedAttention(AttentionConfig(vocab_size=7, d_model=4, seed=1))
        y = ShiftedAttention(AttentionConfig(vocab_size=7, d_model=4, seed=2))
        self.assertGreater(np.abs(x.params["wq"] - y.params["wq"]).max(), 0.0)


class TestTrainingActuallyReducesLoss(unittest.TestCase):
    def test_can_learn_a_trivial_deterministic_sequence(self):
        """The end-to-end connection test.

        A gradient that is correct per-parameter can still be assembled into an
        optimiser that does nothing. This trains on a fixed repeating sequence,
        which the model should fit easily, and requires the loss to fall a long
        way. It is deliberately not the real task: this asks whether the
        machinery learns *at all*.
        """
        cfg = AttentionConfig(vocab_size=6, d_model=16, seed=1)
        model = ShiftedAttention(cfg)
        optimiser = Adam(model.params, lr=0.05)
        tokens = np.array([1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3])
        targets = np.roll(tokens, -1)
        scored = np.zeros(len(tokens), dtype=bool)
        scored[1:-1] = True

        logits, cache = model.forward(tokens)
        first, _ = model.loss_and_backward(logits, cache, targets, scored)
        for _ in range(300):
            logits, cache = model.forward(tokens)
            _, grads = model.loss_and_backward(logits, cache, targets, scored)
            optimiser.step(grads)
        logits, cache = model.forward(tokens)
        last, _ = model.loss_and_backward(logits, cache, targets, scored)

        self.assertLess(last, first * 0.2,
                        f"loss went {first:.3f} -> {last:.3f}; training is not working")


class TestConfigValidation(unittest.TestCase):
    def test_rejects_impossible_configurations(self):
        for bad in (dict(vocab_size=1), dict(vocab_size=8, d_model=0),
                    dict(vocab_size=8, init_scale=0.0)):
            with self.subTest(**bad):
                with self.assertRaises(ValueError):
                    AttentionConfig(**bad)


if __name__ == "__main__":
    unittest.main()
