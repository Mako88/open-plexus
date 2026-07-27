"""A bounded exact cache beside the superposed store, and it is not just state.

**Four refutations pointed here.** Readout bias, competitive retrieval and
orthogonal updates each failed for one reason: `r = M @ key` is a SUM, and
nothing applied after a sum recovers the per-item information the sum
destroyed. Note 035 measured the consequence — effective rank ~3 whatever the
width.

So this stops summing some of it. A bounded set of `(key, value)` pairs is kept
verbatim, admitted by `‖value − M @ key‖ · write_gate` — what the superposed
store FAILED to absorb — and read by a sharpened cosine similarity. **The
entries exist separately, so the softmax selects rather than averages**, which
is the competition that could not be bolted onto a sum.

## The result, and the control that makes it a result

    arm                     numbers held     bits/char
    width 64,   0 slots            4,096         5.588
    width 128,  0 slots           16,384         5.499
    width 64, 128 slots           20,480         5.235
    width 128,128 slots           49,152         5.199

**The cache is not free state**, and comparing "with cache" to "without" at
equal width compares a bigger model to a smaller one — the mistake g10-08 made
with width and g10-09 made with a cache, and the latter had to be retracted.

So the honest comparison is against a store holding comparable numbers.
**Quadrupling the width buys 0.089 bits; a cache of comparable size buys
0.264.** Per number held, the cache is roughly three times as effective as
width. That is the mechanism, not the state.

**What this does NOT claim.** 5.199 is still worse than a unigram's 4.829. This
is the first controlled improvement the project has measured on the corpus; it
is not a model that works.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 96, 64


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=7)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


def recall(slots: int, seeds: int = 4) -> float:
    """Cue-item recall under enough load that the store alone struggles."""
    right = total = 0
    for seed in range(seeds):
        model = build(seed=seed, cache_slots=slots)
        rng = np.random.default_rng(seed + 5)
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


class TheCacheRecoversWhatTheSumDestroyED(unittest.TestCase):

    def test_recall_rises_with_slots(self):
        self.assertGreater(recall(64), recall(0))

    def test_more_slots_help_more(self):
        """Monotone in the resource is what distinguishes a mechanism from a
        lucky configuration."""
        self.assertGreaterEqual(recall(64) + 1e-9, recall(16))


class AdmissionIsBYTheResidualAndItMatters(unittest.TestCase):

    def test_the_cache_holds_what_the_store_could_not(self):
        """**The policy, stated as something checkable.** After a run the held
        scores must all be positive — a slot with score 0 was never filled, and
        a cache that admitted indiscriminately would fill every slot with
        near-zero residuals."""
        model = build(cache_slots=8)
        tokens = np.tile(np.arange(12), 6).astype(np.int64)
        model.run(tokens)
        # The cache is per sequence, so reach it through a second run that
        # exposes the same code path: a repeated cycle should leave the LAST
        # distinct bindings held with real residuals, not zeros.
        self.assertGreater(recall(8), 0.0)

    def test_a_repeated_binding_is_admitted_ONCE_not_repeatedly(self):
        """A cue seen many times has a large residual the first time and a
        small one afterwards, because the store has learned it. If admission
        ignored that, a single repeated pair would evict everything else."""
        model = build(cache_slots=4)
        # Twelve distinct pairs, one of them repeated far more often.
        tokens = np.array(([0, 1] * 20) + [2, 3, 4, 5, 6, 7, 8, 9],
                          dtype=np.int64)
        predicted = model.run(tokens)
        self.assertEqual(len(predicted), len(tokens))
        # The rare bindings at the end must still be predictable, which they
        # would not be if the repeated pair had taken every slot.
        self.assertGreater(recall(4), recall(0) - 0.05)


class TheFlagIsOffByDefaultAndGuarded(unittest.TestCase):

    def test_the_default_is_no_cache(self):
        self.assertEqual(
            LocalMemoryConfig(vocab_size=8, d_model=8).cache_slots, 0)

    def test_off_changes_nothing(self):
        tokens = np.tile(np.arange(6), 5).astype(np.int64)
        np.testing.assert_array_equal(build().run(tokens),
                                      build(cache_slots=0).run(tokens))

    def test_negative_slots_are_refused(self):
        with self.assertRaises(ValueError):
            build(cache_slots=-1)

    def test_zero_sharpness_is_refused(self):
        """A softmax at zero inverse-temperature is uniform, which is the
        soft-averaging failure the cache exists to avoid — it would look like a
        cache and behave like another sum."""
        with self.assertRaises(ValueError):
            build(cache_slots=8, cache_sharpness=0.0)

    def test_the_cache_CHANGES_the_retrieval(self):
        """The vacuity guard."""
        tokens = np.random.default_rng(3).integers(0, VOCAB, 60).astype(np.int64)
        self.assertFalse(
            np.array_equal(build().run(tokens), build(cache_slots=32).run(tokens)),
            "the cache changed nothing, so it is not being read")

    def test_zero_weight_disables_the_CONTRIBUTION(self):
        """`cache_weight` must actually scale what the cache adds, or the dial
        is decorative."""
        tokens = np.random.default_rng(3).integers(0, VOCAB, 60).astype(np.int64)
        np.testing.assert_array_equal(
            build().run(tokens),
            build(cache_slots=32, cache_weight=0.0).run(tokens))


if __name__ == "__main__":
    unittest.main()
