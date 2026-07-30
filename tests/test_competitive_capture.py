"""A finite pool that tagged candidates have to win.

[Note 015](../docs/archive/notes/015-we-implemented-the-tag-and-not-the-competition.md)
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
    LocalAssociativeMemory, LocalMemoryConfig, admit)

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


class TheAdmissionPolicy(unittest.TestCase):
    """Tested directly, because through the model it was not tested at all.

    Two mutations survived the first version of this file -- keep the most
    RECENT k, and evict the STRONGEST -- and both survived for one reason: every
    test here checked that the pool's CAPACITY binds, and none checked which
    items it keeps. A pool that admits everything and a pool that keeps the worst
    are both bounded, so all of those tests pass on either.

    Reaching the policy through predictions means hunting for fixtures where the
    difference happens to surface. The decision is three lines of pure
    arithmetic, so it is a function now and is tested as one.
    """

    def test_room_means_the_candidate_is_appended(self):
        self.assertEqual(admit([], 0.5, 3), 0)
        self.assertEqual(admit([9.0, 9.0], 0.001, 3), 2,
                         "a candidate weaker than everything must still be "
                         "taken while the pool has room -- the competition is "
                         "for SPACE, and there is space")

    def test_a_full_pool_refuses_a_weaker_candidate(self):
        """The case that has to exist. A pool where everything gets in is a pool
        in name only, and is exactly the `admits-everything` mutation."""
        self.assertIsNone(admit([1.0, 2.0, 3.0], 0.5, 3))
        self.assertIsNone(admit([1.0, 2.0, 3.0], 0.999, 3))

    def test_a_stronger_candidate_displaces_the_WEAKEST(self):
        """Not the strongest, and not the oldest."""
        self.assertEqual(admit([5.0, 1.0, 3.0], 4.0, 3), 1)
        self.assertEqual(admit([1.0, 5.0, 3.0], 4.0, 3), 0)
        self.assertEqual(admit([3.0, 5.0, 1.0], 4.0, 3), 2)

    def test_ties_go_to_the_incumbent(self):
        """Otherwise a run of equal-strength candidates churns forever, each
        evicting the last and the pool never settling."""
        self.assertIsNone(admit([2.0, 2.0, 2.0], 2.0, 3))

    def test_the_position_of_the_weakest_is_what_decides_it_not_the_order(self):
        """The same multiset in any arrangement must evict the same VALUE."""
        for arrangement in ([1.0, 7.0, 4.0], [7.0, 4.0, 1.0], [4.0, 1.0, 7.0]):
            index = admit(arrangement, 5.0, 3)
            self.assertEqual(arrangement[index], 1.0)

    def test_a_pool_of_one_keeps_the_best_thing_it_has_seen(self):
        self.assertEqual(admit([], 1.0, 1), 0)
        self.assertIsNone(admit([9.0], 1.0, 1))
        self.assertEqual(admit([1.0], 9.0, 1), 0)


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
