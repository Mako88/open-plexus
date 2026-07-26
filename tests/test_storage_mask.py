"""Which positions write into the memory.

`run(store=...)` is the dial behind this project's largest finding: with only the
useful bindings kept, sequence length stops being a difficulty dial at all
([g7-02](../experiments/sweeps/g7-02-tiny-nodes-and-clusters.txt)). It went in
during a probe and reached that result with **no test and no mutation covering
it**, which is the gap these close.

The one that matters most is the off-by-one. A binding written at step `t` is
`(t-1 -> t)`, so `store[t]` gates a binding whose key came from the previous
position. Gating the wrong index would keep the wrong bindings while leaving
every count, shape and summary statistic identical — a silent corruption of the
exact mechanism the headline rests on.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB = 12


def model(seed: int = 5, partitions: int = 1):
    return LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=32, partitions=partitions, lr=0.05,
        key_scale=0.5, seed=seed))


class NoMaskIsTheOriginalBehaviour(unittest.TestCase):
    """Every result before the mask existed was measured with no mask."""

    def test_none_and_all_true_agree_exactly(self):
        tokens = np.array([3, 7, 1, 9, 4, 2, 8, 5])
        keep = np.ones(len(tokens), dtype=bool)
        np.testing.assert_array_equal(model().run(tokens),
                                      model().run(tokens, store=keep))

    def test_none_and_all_true_agree_while_learning_too(self):
        """The mask sits inside the loop that also updates the readout."""
        tokens = np.array([3, 7, 1, 9, 4, 2, 8, 5])
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        plain, masked = model(), model()
        plain.run(tokens, targets, scored, learn=True)
        masked.run(tokens, targets, scored, learn=True,
                   store=np.ones(len(tokens), dtype=bool))
        np.testing.assert_array_equal(plain.wo, masked.wo)


class TheMaskGatesTheRightBinding(unittest.TestCase):
    """The off-by-one, and the first version of this test could not see it.

    That version reconstructed the memory from `wk` and `wv` inside the test and
    compared *its own* arithmetic against itself, never looking at what `run`
    actually stored. The mutation harness flagged it immediately: with `store[t]`
    changed to `store[t - 1]`, every assertion still passed.

    **That is the third time in this project a test has reimplemented the thing it
    was meant to check.** The fix is the same each time — observe the model
    through the interface a caller would use.

    Here that means giving the readout a decoder and reading its predictions. With
    `wo = wv`, the readout scores each token by how much the retrieved vector
    looks like that token's value, so the prediction at a query position names
    whatever the memory has bound to that key.
    """

    def _decoding_model(self):
        m = model()
        m.wo[:] = m.wv        # a decoder: argmax(wv @ v_b) is b
        return m

    def test_the_kept_binding_is_the_one_from_the_previous_position(self):
        """A binding written at `t` is `(t-1 -> t)`.

        The sequence ends by repeating an earlier token, so the last position is
        a query. Keeping only position `t` should make that query return
        `tokens[t]` when asked with `tokens[t-1]` — and under an off-by-one the
        memory instead holds `(t -> t+1)`, so the query returns nothing useful
        and this fails.
        """
        for t in (1, 3, 5):
            with self.subTest(position=t):
                tokens = np.array([2, 4, 6, 8, 10, 3, 7, 0])
                tokens[-1] = tokens[t - 1]          # query the kept binding's key
                keep = np.zeros(len(tokens), dtype=bool)
                keep[t] = True
                predicted = self._decoding_model().run(tokens, store=keep)
                self.assertEqual(
                    int(predicted[-1]), int(tokens[t]),
                    f"querying with token {tokens[t - 1]} did not return "
                    f"{tokens[t]}, so the mask gated the wrong binding")

    def test_the_successor_binding_is_NOT_what_gets_kept(self):
        """States the failure directly rather than by implication.

        Under `store[t-1]` the memory would hold `(t -> t+1)`, so querying with
        `tokens[t]` would return `tokens[t+1]`. It must not.
        """
        tokens = np.array([2, 4, 6, 8, 10, 3, 7, 0])
        t = 3
        tokens[-1] = tokens[t]                      # query the SUCCESSOR's key
        keep = np.zeros(len(tokens), dtype=bool)
        keep[t] = True
        predicted = self._decoding_model().run(tokens, store=keep)
        self.assertNotEqual(
            int(predicted[-1]), int(tokens[t + 1]),
            "the memory returned the successor binding, so the mask is gating "
            "one position too early")

    def test_the_decoder_actually_decodes(self):
        """Without this the two tests above could pass on a broken readout."""
        tokens = np.array([2, 4, 6, 8, 10, 3, 7, 2])
        keep = np.zeros(len(tokens), dtype=bool)
        keep[1] = True
        m = self._decoding_model()
        self.assertEqual(int(m.run(tokens, store=keep)[-1]), 4,
                         "the decoder cannot recover a value it was just given")


class AnEmptyMaskEmptiesTheMemory(unittest.TestCase):

    def test_storing_nothing_makes_every_retrieval_identical(self):
        """With nothing stored, retrieval is zero and the readout sees no input.

        So predictions collapse to one token for the whole sequence. If they do
        not, something is writing to the memory that the mask does not control.
        """
        tokens = np.array([3, 7, 1, 9, 4, 2, 8, 5])
        keep = np.zeros(len(tokens), dtype=bool)
        predictions = model().run(tokens, store=keep)
        self.assertEqual(len(set(predictions.tolist())), 1)

    def test_a_full_mask_does_not_collapse(self):
        """The control: otherwise the test above passes for the wrong reason."""
        tokens = np.array([3, 7, 1, 9, 4, 2, 8, 5])
        m = model()
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        m.run(tokens, targets, scored, learn=True)
        self.assertGreater(len(set(m.run(tokens).tolist())), 1)


class TheMaskWorksAlongsideClusters(unittest.TestCase):
    """Both arguments are read in the same loop and could interfere."""

    def test_masking_changes_what_a_cluster_answers(self):
        tokens = np.array([3, 7, 1, 9, 4, 2, 8, 5])
        m = model(partitions=8)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        m.run(tokens, targets, scored, learn=True)
        keep = np.zeros(len(tokens), dtype=bool)
        keep[3] = True
        self.assertFalse(
            np.array_equal(m.run(tokens, partition=[0, 1]),
                           m.run(tokens, partition=[0, 1], store=keep)),
            "the mask made no difference to a cluster read, so one of the two "
            "arguments is being ignored")


if __name__ == "__main__":
    unittest.main()
