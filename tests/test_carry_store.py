"""`carry_store` keeps the store between runs, and it is NOT a win.

**Why it exists at all.** The per-sequence reset is deliberate and guarded by
`local-memory-persists-across-sequences`: on MQAR and `reward_recall` each
sequence is independent, and a store accumulating across them would answer from
the training set rather than from the sequence in front of it. That is correct
for those tasks. It was then inherited by the corpus experiments, where it gives
the model a memory 128 characters long — a limit chosen for synthetic recall and
never chosen for text.

**Measured, and it fails for a reason worth recording.** Carrying appeared to buy
0.25 bits. Separating where the gain came from:

    chars     reset    carry in TRAINING only    carry in training AND test
    16,000    5.519                     5.667                         5.191
    62,500    5.529                     5.730                         5.331
   250,000    5.505                     5.678                         5.256

**Carrying during training is WORSE.** The entire apparent gain came from
carrying at TEST time — giving the model context across evaluation chunks that
the backprop baseline is not given. That is a change of measurement protocol
dressed as a change of model, and it is the g10-09 failure that was retracted
once already.

Kept default-off with the numbers attached rather than deleted, so it is not
proposed again. These are connection tests; the table above is not something a
unit test should re-run.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)


def model_for(carry: bool, seed: int = 3) -> LocalAssociativeMemory:
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=17, d_model=16, seed=seed, derived_keys=True,
        carry_store=carry))
    model.wo[:] = model.wv
    return model


def sequences(count: int = 3):
    rng = np.random.default_rng(0)
    for _ in range(count):
        tokens = rng.integers(0, 17, 40)
        targets = np.concatenate([tokens[1:], tokens[-1:]])
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        yield tokens, targets, scored


class TheStoreIsCarriedOrNot(unittest.TestCase):

    def test_without_it_a_sequence_is_unaffected_by_what_ran_before(self):
        """The property `local-memory-persists-across-sequences` guards, and the
        one the recall tasks depend on: a store accumulating across independent
        sequences answers from the training set."""
        primed = model_for(False)
        cold = model_for(False)
        runs = list(sequences())
        primed.run(*runs[0], learn=False)
        self.assertTrue(np.array_equal(
            np.asarray(primed.run(*runs[1], learn=False)),
            np.asarray(cold.run(*runs[1], learn=False))))

    def test_with_it_a_sequence_IS_affected_by_what_ran_before(self):
        """The connection test. A flag read once and never applied would leave
        the reset in place, and the measurement refuting this mechanism would
        have been the reset model measured twice."""
        primed = model_for(True)
        cold = model_for(True)
        runs = list(sequences())
        primed.run(*runs[0], learn=False)
        self.assertFalse(np.array_equal(
            np.asarray(primed.run(*runs[1], learn=False)),
            np.asarray(cold.run(*runs[1], learn=False))))

    def test_it_is_off_by_default(self):
        """Every earlier number in the project was measured with the reset, and
        every recall task requires it."""
        self.assertFalse(
            LocalMemoryConfig(vocab_size=5, d_model=4).carry_store)

    def test_the_carried_store_actually_grows_across_runs(self):
        """Not merely 'different'. A store that were re-zeroed and then
        re-populated would also differ, while carrying nothing."""
        model = model_for(True)
        sizes = []
        for run in sequences(3):
            model.run(*run, learn=False)
            sizes.append(float(np.linalg.norm(model._carried)))
        self.assertTrue(all(b > a for a, b in zip(sizes, sizes[1:])), sizes)


if __name__ == "__main__":
    unittest.main()
