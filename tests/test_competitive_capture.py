"""A finite pool that tagged candidates have to win.

[Note 015](../docs/notes/015-we-implemented-the-tag-and-not-the-competition.md)
is the argument. In short: synaptic capture is competitive over a finite protein
pool, note 010 took the tag and left out the scarcity, and the scarcity is what
does the selecting.

It matters because of one asymmetry. A **threshold** fires at a rate, so
promotions grow with sequence length, so `N` grows, so retrieval — which goes as
`sqrt(d / N)` — decays with length. That is g8-01's measured result. The oracle
wins by holding `N` **constant**, and a fixed number of slots is the only tried
mechanism that also does.

These tests pin the mechanism, not the benefit. Whether it recovers any of the
oracle's advantage is a sweep's business and is not claimed here.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 16, 24
TOKENS = np.random.default_rng(7).integers(0, VOCAB, 200)


def build(slots: int = 0, consolidation: float = 1.0, salience: float = 0.0,
          cap: float = 0.0):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=0.9,
        consolidation=consolidation, capture_slots=slots, salience=salience,
        lasting_cap=cap, seed=4))
    model.wo[:] = model.wv           # a decoder, so predictions track the memory
    return model


class ZeroIsTheOldRule(unittest.TestCase):
    """Every earlier consolidation result was measured without a pool."""

    def test_the_default_is_off(self):
        self.assertEqual(LocalMemoryConfig(vocab_size=VOCAB).capture_slots, 0)

    def test_a_negative_pool_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, decay=0.9,
                              consolidation=1.0, capture_slots=-1)

    def test_a_pool_without_consolidation_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, decay=0.9,
                              capture_slots=4)


class APoolLargerThanTheDemandChangesNothing(unittest.TestCase):
    """The exact equivalence that pins what the mechanism is.

    If the pool is never full, nothing is ever displaced, and competitive
    capture must reduce **exactly** to the rule it replaces. Any difference means
    the slots are doing something besides bounding the count -- which is the most
    likely way to get this wrong, since it is easy to write an admission rule
    that also quietly reweights what it admits.
    """

    def test_a_huge_pool_reproduces_unbounded_consolidation(self):
        np.testing.assert_array_equal(build(slots=10_000).run(TOKENS),
                                      build(slots=0).run(TOKENS))

    def test_and_a_small_pool_does_not(self):
        """Otherwise the test above passes on a pool that is never consulted."""
        self.assertFalse(
            np.array_equal(build(slots=2).run(TOKENS),
                           build(slots=0).run(TOKENS)),
            "a pool of two behaved exactly like no pool at all, so nothing is "
            "ever displaced and the capacity is not binding")


class TheSizeOfThePoolIsADial(unittest.TestCase):

    def test_different_capacities_give_different_answers(self):
        answers = {build(slots=k).run(TOKENS).tobytes() for k in (1, 2, 4, 8)}
        self.assertEqual(len(answers), 4,
                         "two different pool sizes produced identical "
                         "predictions, so capacity is not being consulted")

    def test_it_composes_with_the_salience_gate(self):
        """Tagging and capture are separate: the gate picks candidates, the pool
        picks winners. Both have to still work when combined."""
        gated = build(slots=2, salience=1.5, cap=50.0).run(TOKENS)
        ungated = build(slots=2, salience=0.0).run(TOKENS)
        self.assertFalse(np.array_equal(gated, ungated))


class ItStaysFinite(unittest.TestCase):

    def test_a_long_run_produces_usable_predictions(self):
        predictions = build(slots=4).run(np.tile(TOKENS, 4))
        self.assertTrue(np.isfinite(predictions).all())
        self.assertTrue(((predictions >= 0) & (predictions < VOCAB)).all())


if __name__ == "__main__":
    unittest.main()
