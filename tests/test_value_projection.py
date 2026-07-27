"""`value_from_readout` writes the learned readout row instead of a frozen draw.

**Why it exists.** The store is rebuilt every chunk and `Wk`/`Wv` are never
updated, so `Wo` is the only thing this model learns across a corpus — one
`vocab x d` linear map (decision 62) — and the model converges at about 16,000
characters (decision 63). This is the cheapest available test of whether a
LEARNED value projection moves that: it adds no parameters, since `Wo` and the
value projection become one matrix.

**It did not.** Measured on Tiny Shakespeare at width 64, two seeds, against the
frozen draw: +0.000 bits at 4,000 characters, +0.014 at 16,000, +0.068 at 62,500
and +0.023 at 250,000 — neutral at the smallest size and WORSE everywhere else.
Kept behind a default-off flag with this number attached, because a refuted
mechanism nobody wrote down gets proposed again.

**What it does NOT refute:** a genuinely separate `Wv` with its own update and
its own parameters. That adds persistent capacity, which this does not, and it
is a different mechanism with a different prediction.

The tests here are connection tests. The measurement above is not something a
unit test should re-run.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)


def model_for(value_from_readout: bool, seed: int = 3) -> LocalAssociativeMemory:
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=17, d_model=16, seed=seed, derived_keys=True,
        value_from_readout=value_from_readout))
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


class TheFlagReachesTheOutput(unittest.TestCase):

    def test_turning_it_on_changes_the_predictions(self):
        """The connection test. A value projection that is read once and never
        applied would run, produce plausible numbers, and be doing nothing --
        and the measurement that refuted this mechanism would have been a
        measurement of the frozen model twice."""
        self.assertFalse(np.array_equal(train(model_for(False)),
                                        train(model_for(True))))

    def test_it_is_off_by_default(self):
        """Every earlier number on this corpus was measured without it."""
        self.assertFalse(
            LocalMemoryConfig(vocab_size=5, d_model=4).value_from_readout)

    def test_the_frozen_draw_is_untouched_either_way(self):
        """`value_from_readout` changes WHICH matrix supplies the value, not whether
        `Wv` is trained. If it silently started updating `Wv`, this would be a
        different mechanism wearing this one's name and its refutation would
        not apply to it."""
        for flag in (False, True):
            model = model_for(flag)
            before = model.wv.copy()
            train(model)
            self.assertTrue(np.array_equal(before, model.wv))

    def test_before_any_learning_the_two_agree(self):
        """`wo` is initialised to `wv` by these experiments, so the mechanism
        can only bite once the readout has moved. That is why the measured
        difference at 4,000 characters was +0.000 and it is not evidence the
        flag is dead -- which is what the first test is for."""
        off, on = model_for(False), model_for(True)
        self.assertTrue(np.array_equal(off.wo, off.wv))
        self.assertTrue(np.array_equal(on.wo, on.wv))


if __name__ == "__main__":
    unittest.main()
