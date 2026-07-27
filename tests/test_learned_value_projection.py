"""`value_lr` trains `Wv`, which is the first thing besides `Wo` that learns.

**Why this matters more than an ordinary mechanism.** The store is rebuilt every
chunk and `Wk`/`Wv` are drawn once and never updated, so everything this model
learned across a corpus was a single `vocab x d` linear map — and it converges at
about 16,000 characters (decisions 62, 63). Training `Wv` **doubles the
persistent parameters**, where `value_from_readout` merely merged `Wv` into `Wo`
and was refuted for it (decision 64).

Whether the saturation point moves is what separates decision 59's explanation
(the sum destroys per-item information) from decision 62's (there is almost
nothing for data to fill).

## The rule, and why it is no less local than the readout's

A retrieval should land on the target's value, so

    dL/d(Wv[target]) = Wo^T (p - y)

and group `g`'s share of that uses only group `g`'s readout columns and group
`g`'s own prediction error. That is the same locality the delta rule beside it
already has — no node reads another node's activity — which is why this does not
widen the C1 argument. `test_a_group_s_update_ignores_another_group_s_readout`
is the assertion, because a rule that quietly needed the whole readout would
still run and would still learn.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)


def model_for(seed: int = 3, **overrides) -> LocalAssociativeMemory:
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=17, d_model=16, seed=seed, derived_keys=True, **overrides))
    model.wo[:] = model.wv
    return model


def train(model, rounds: int = 4):
    rng = np.random.default_rng(0)
    out = []
    for _ in range(rounds):
        tokens = rng.integers(0, 17, 40)
        targets = np.concatenate([tokens[1:], tokens[-1:]])
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        out.append(np.asarray(model.run(tokens, targets, scored,
                                        learn=True)).ravel())
    return np.concatenate(out)


class ItActuallyTrainsTheProjection(unittest.TestCase):

    def test_wv_is_frozen_when_the_rate_is_zero(self):
        model = model_for()
        before = model.wv.copy()
        train(model)
        self.assertTrue(np.array_equal(before, model.wv))

    def test_wv_moves_when_the_rate_is_not(self):
        """The connection test. A rate read once and never applied would leave
        the model exactly as it was and the whole capacity question would be
        answered by measuring the frozen model twice."""
        model = model_for(value_lr=0.02)
        before = model.wv.copy()
        train(model)
        self.assertFalse(np.array_equal(before, model.wv))

    def test_it_changes_the_predictions(self):
        self.assertFalse(np.array_equal(train(model_for()),
                                        train(model_for(value_lr=0.02))))

    def test_it_is_off_by_default(self):
        self.assertEqual(
            LocalMemoryConfig(vocab_size=5, d_model=4).value_lr, 0.0)

    def test_a_negative_rate_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=5, d_model=4, value_lr=-0.1)

    def test_training_a_matrix_nothing_reads_is_refused(self):
        """`value_from_readout` writes `Wo` as the value, so `Wv` would be
        trained and never read -- a mechanism that runs, consumes a learning
        rate, and does nothing."""
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=5, d_model=4, value_lr=0.02,
                              value_from_readout=True)


class ItStaysLocal(unittest.TestCase):

    def test_a_group_s_update_ignores_another_group_s_readout(self):
        """**The C1 assertion.** Group 0's share of the value update must depend
        only on group 0's readout columns and its own prediction error. If it
        quietly needed the whole readout, it would still run and still learn,
        and the locality argument would be false while every measurement
        continued to look fine.

        Perturbing only group 1's readout must leave group 0's half of `Wv`
        exactly where it was."""
        plain = model_for(partitions=2, value_lr=0.02)
        nudged = model_for(partitions=2, value_lr=0.02)
        nudged.grouped_wo[:, 1, :] *= 3.0
        before = plain.wv.copy()
        train(plain, rounds=1)
        train(nudged, rounds=1)
        half = plain.config.d_model // 2
        self.assertFalse(np.array_equal(before[:, :half], plain.wv[:, :half]),
                         "group 0 must have moved at all, or this proves nothing")
        self.assertTrue(
            np.allclose(plain.wv[:, :half], nudged.wv[:, :half]),
            "group 0's value update changed when only group 1's readout did")

    def test_dead_dimensions_stay_dead(self):
        """`ablate` zeroes a departed node's columns. An update that revived
        them would hand the model back capacity a machine took away, and every
        churn result would be measured against a model that never really lost
        anything."""
        model = model_for(value_lr=0.05)
        model.ablate([0, 1])
        train(model)
        self.assertTrue(np.array_equal(model.wv[:, [0, 1]],
                                       np.zeros((17, 2))))


if __name__ == "__main__":
    unittest.main()
