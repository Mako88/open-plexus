"""A hidden layer inside each group, and the locality claim that lets it exist.

**Why it exists.** `Wo` was the only thing this model learned across a corpus,
and it is a single LINEAR map onto a retrieval it does not influence. Note 037
measured what that costs: on the SAME frozen features a two-layer readout is
worth **0.63 bits prequentially** — one pass, no split, no temperature — and it
is the first mechanism in this project to move the data exponent rather than the
level (decisions 70, 71, 72).

**Why it is admissible under C1.** A group already holds its own
`d / partitions` slice of the retrieval and computes its own `parts[g]`. Making
that slice two layers means backpropagating through two matrices the same node
already owns, using its own activity and its own error. Nothing crosses a group.

That claim is the one that would be easy to get wrong and impossible to notice:
a readout that quietly needed another group's weights would still run, still
learn, and still improve the loss, while the locality argument silently became
false. `test_a_group_s_hidden_layer_ignores_another_group_s_readout` is the
assertion, and it is the reason this file exists rather than a smoke test.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

RNG = np.random.default_rng(0)
TOKENS = RNG.integers(0, 17, 60)
TARGETS = np.concatenate([TOKENS[1:], TOKENS[-1:]])
SCORED = np.ones(len(TOKENS), dtype=bool)
SCORED[-1] = False


def build(hidden: int, **overrides) -> LocalAssociativeMemory:
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=17, d_model=16, seed=3, derived_keys=True, hidden=hidden,
        **overrides))
    model.wo[:] = 0.0
    return model


def train(model, rounds: int = 6):
    for _ in range(rounds):
        model.run(TOKENS, TARGETS, SCORED, learn=True)
    return model


class TheHiddenLayerIsConnected(unittest.TestCase):

    def test_it_changes_the_predictions(self):
        """The connection test. A width read once and never applied would leave
        the readout linear, and the measurement that justified this whole change
        would have been the linear model twice."""
        self.assertFalse(np.array_equal(
            np.asarray(train(build(0)).run(TOKENS, learn=False)),
            np.asarray(train(build(8)).run(TOKENS, learn=False))))

    def test_the_hidden_weights_actually_train(self):
        """Not merely present. A hidden layer that is initialised and then never
        updated is a fixed random projection wearing a learned layer's name --
        and it would still change the predictions, so the test above cannot
        tell the difference."""
        before = build(8).hidden_w.copy()
        self.assertFalse(np.allclose(before, train(build(8)).hidden_w))

    def test_it_is_off_by_default(self):
        self.assertEqual(LocalMemoryConfig(vocab_size=5, d_model=4).hidden, 0)

    def test_zero_leaves_the_readout_linear(self):
        self.assertIsNone(build(0).hidden_w)

    def test_the_readout_is_reshaped_to_the_hidden_width(self):
        """With a hidden layer the output weights read the HIDDEN units, not the
        retrieval, so `wo` is `vocab x partitions*hidden`. If it kept its old
        shape the einsum would still contract and would be reading the wrong
        thing."""
        model = build(8, partitions=2)
        self.assertEqual(model.wo.shape, (17, 16))
        self.assertEqual(model.grouped_wo.shape, (17, 2, 8))
        self.assertIs(model.grouped_wo.base, model.wo)


class ItStaysInsideItsGroup(unittest.TestCase):

    def test_a_group_s_hidden_layer_ignores_another_group_s_readout(self):
        """**The C1 assertion, and the whole reason a composed readout is
        allowed at all.** Group 0's hidden update must depend only on group 0's
        output weights and group 0's error. If it needed the whole readout the
        model would still learn and the locality argument would be false."""
        # Train FIRST: `build` zeroes `wo`, so perturbing it before any
        # learning multiplies zero by three and tests nothing. That is the
        # shape of a locality test that passes because neither side moved.
        plain, nudged = train(build(8, partitions=2)), train(build(8, partitions=2))
        nudged.grouped_wo[:, 1, :] *= 3.0
        train(plain, rounds=1)
        train(nudged, rounds=1)
        self.assertTrue(
            np.allclose(plain.hidden_w[0], nudged.hidden_w[0]),
            "group 0's hidden layer moved when only group 1's readout changed")

    def test_the_perturbed_group_did_move(self):
        """Without this the test above passes if NOTHING moves -- which is
        exactly what a disconnected hidden layer looks like."""
        plain, nudged = train(build(8, partitions=2)), train(build(8, partitions=2))
        nudged.grouped_wo[:, 1, :] *= 3.0
        train(plain, rounds=1)
        train(nudged, rounds=1)
        self.assertFalse(np.allclose(plain.hidden_w[1], nudged.hidden_w[1]))


class ConfigurationsThatCannotMeanAnything(unittest.TestCase):

    def test_a_negative_width_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=5, d_model=4, hidden=-1)

    def test_a_width_that_does_not_divide_into_groups_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=5, d_model=6, partitions=4, hidden=8)

    def test_orthogonal_updates_are_refused_alongside_it(self):
        """`orthogonal_every` orthogonalises the readout update, whose shape is
        defined by the LINEAR readout. Across two layers it would orthogonalise
        a different matrix than the one it was measured on, silently."""
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=5, d_model=4, hidden=8,
                              orthogonal_every=4)


if __name__ == "__main__":
    unittest.main()
