"""Settling the retrieval makes it WORSE, and the reason is structural.

The capacity literature is unanimous that competition is what separates linear
associative capacity O(d) from a competitive read's O(e^{d/2}), and note 035
recorded "try competition in the retrieval" as the cheapest untried lever. It
was tried. It fails, and these tests pin the failure so it is not re-proposed.

**Hopfield settling is for AUTO-associative memories**, where patterns map to
themselves. Ours is HETERO-associative: keys in, values out. Iterating
`M(Mᵀr)` is power iteration on `MMᵀ`, which converges to the store's dominant
singular direction **regardless of the query**. It does not sharpen; it forgets
what was asked.

Measured at width 96, 6 seeds:

    load 256      accuracy    margin
    1 step           0.924     1.656
    2 steps          0.600     0.347
    3 steps          0.343    -0.406
    6 steps          0.128    -1.499

**And the deeper reason competition cannot be bolted on here:** `answer.argmax()`
is already winner-take-all. The model's read ends in a competitive step. What is
linear is the *store* — a sum — and no nonlinearity applied after the sum can
recover the per-item similarities the sum destroyed. A clean-up against the
value codebook was tried too: top-1 leaves accuracy identical (it projects onto
the argmax that was already going to be chosen) and top-k for k>1 makes it worse.

So the O(d) → O(e^{d/2}) lever is **not available by changing the read**. It
requires not summing — a bounded exact cache, or sparse addressing — which is a
different mechanism and a different decision.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 128, 96


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=11)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


def recall(steps: int, seeds: int = 4) -> float:
    """Cue-item recall through the model, under enough load to matter."""
    right = total = 0
    for seed in range(seeds):
        model = build(seed=seed, retrieval_steps=steps)
        rng = np.random.default_rng(seed + 3)
        cues = rng.permutation(VOCAB)[:VOCAB // 2]
        items = rng.integers(0, VOCAB, len(cues))
        pairs = [(int(c), int(i)) for c, i in zip(cues, items)]
        laid = [t for pair in pairs for t in pair]
        tokens = np.array(laid * 3, dtype=np.int64)
        predicted = model.run(tokens)
        start = len(laid) * 2
        for index, (_, item) in enumerate(pairs):
            right += int(predicted[start + index * 2] == item)
            total += 1
    return right / total


class SettlingMakesRetrievalWORSE(unittest.TestCase):
    """The refutation, stated as the comparison that produced it."""

    def test_two_steps_is_worse_than_one(self):
        self.assertLess(recall(2), recall(1),
                        "settling helped; the hetero-associative argument in "
                        "this file's docstring would then be wrong")

    def test_it_keeps_getting_worse(self):
        """Monotone degradation is the signature of power iteration converging
        to a fixed point that has nothing to do with the query. A mechanism that
        merely needed tuning would not do this."""
        scores = [recall(n) for n in (1, 2, 3)]
        self.assertTrue(all(b <= a for a, b in zip(scores, scores[1:])),
                        f"degradation was not monotone: {scores}")


class TheDefaultIsASingleReadAndIsGuarded(unittest.TestCase):

    def test_one_step_is_the_default(self):
        self.assertEqual(
            LocalMemoryConfig(vocab_size=8, d_model=8).retrieval_steps, 1)

    def test_one_step_changes_nothing(self):
        """Every earlier result came through this path and must be unchanged."""
        tokens = np.tile(np.arange(6), 5).astype(np.int64)
        np.testing.assert_array_equal(build().run(tokens),
                                      build(retrieval_steps=1).run(tokens))

    def test_zero_steps_is_refused(self):
        """It would answer without reading the memory at all — and would look
        like a working model, since the readout alone can express a unigram."""
        with self.assertRaises(ValueError):
            build(retrieval_steps=0)

    def test_more_steps_CHANGES_the_answer(self):
        """The vacuity guard. Without it the two tests above would pass on a
        flag that was read and never applied."""
        tokens = np.random.default_rng(2).integers(0, VOCAB, 40).astype(np.int64)
        self.assertFalse(
            np.array_equal(build().run(tokens),
                           build(retrieval_steps=3).run(tokens)),
            "settling changed nothing, so it is not being applied")


if __name__ == "__main__":
    unittest.main()
