"""Decay, eviction to an archive, and reinstatement with a boost.

John's design, agreed 2026-08-01. The load-bearing tests are the two that could
have gone the other way: that decay OFF is exactly the old behaviour, and that
decay ON genuinely changes what the statistics say. A first version of the
design claimed the second could not happen.
"""

from __future__ import annotations

import unittest

from openplexus.grounding import STATISTICS, CoOccurrence

CONDITIONAL = STATISTICS["conditional"]


def _same_counts_different_times(half_life):
    """Two partners with IDENTICAL raw counts and opposite histories.

    Surface 1 shared its evidence with 0 early and then kept appearing alone.
    Surface 2 appeared alone early and shared with 0 late. Both end at
    `together(0, x) = 50` and `seen(x) = 100`, so raw counting cannot tell them
    apart and anything that does is reading the times.
    """
    index = CoOccurrence(half_life=half_life)
    for _ in range(50):
        index.observe({0, 1})
    for _ in range(50):
        index.observe({2, 9})
    for _ in range(200):
        index.observe({7, 8})
    for _ in range(50):
        index.observe({1, 9})
    for _ in range(50):
        index.observe({0, 2})
    return index


class OffByDefault(unittest.TestCase):
    """Every result measured before decay existed has to still be reachable."""

    def test_no_half_life_means_no_decay(self):
        index = CoOccurrence()
        self.assertIsNone(index.half_life)
        for _ in range(10):
            index.observe({0, 1})
        for _ in range(500):
            index.observe({7, 8})
        self.assertEqual(index.together(0, 1), 10)
        self.assertEqual(index.seen(0), 10)

    def test_a_half_life_of_zero_is_refused(self):
        with self.assertRaises(ValueError):
            CoOccurrence(half_life=0)

    def test_the_clock_is_this_node_s_own_occasions(self):
        """Not wall time and not a global total — a quiet node must not forget."""
        index = CoOccurrence(half_life=10)
        self.assertEqual(index.tick, 0)
        index.observe({0, 1})
        self.assertEqual(index.tick, 1)


class DecayChangesWhatTheStatisticsSay(unittest.TestCase):
    """**The design said it would not, and that was wrong.**

    The claim was that every statistic here is a ratio, so a factor applied to
    every count cancels and decay costs only memory. It cancels only when the
    numerator and denominator have the SAME history. Where they differ, decay
    reweights toward recency — which is the mechanism, not a side effect.
    """

    def test_identical_raw_counts_rank_apart_under_decay(self):
        decayed = _same_counts_different_times(100.0)
        recent = CONDITIONAL(decayed, 0, 2)
        stale = CONDITIONAL(decayed, 0, 1)
        self.assertGreater(recent, stale)
        self.assertGreater(recent / stale, 4.0,
                           "decay that barely moves the ranking is not doing "
                           "the job the design wants of it")

    def test_the_companion_without_decay_they_are_equal(self):
        """Without this, the test above would pass for a fixture that was
        simply lopsided to begin with."""
        plain = _same_counts_different_times(None)
        self.assertEqual(plain.together(0, 1), plain.together(0, 2))
        self.assertEqual(plain.seen(1), plain.seen(2))
        self.assertAlmostEqual(CONDITIONAL(plain, 0, 1),
                               CONDITIONAL(plain, 0, 2))

    def test_a_longer_half_life_reweights_less(self):
        """The connection test on the dial itself."""
        def gap(half_life):
            index = _same_counts_different_times(half_life)
            return CONDITIONAL(index, 0, 2) - CONDITIONAL(index, 0, 1)

        self.assertGreater(gap(100.0), gap(400.0))

    def test_one_half_life_untouched_halves_a_count(self):
        """What the parameter is NAMED after, asserted rather than assumed."""
        index = CoOccurrence(half_life=20)
        index.observe({0, 1})
        held = index.together(0, 1)
        for _ in range(20):
            index.observe({7, 8})
        self.assertAlmostEqual(index.together(0, 1), held / 2.0, places=6)


class EvictionIsAMoveAndNotADeletion(unittest.TestCase):

    def test_evicting_hands_back_everything_needed_to_restore(self):
        index = CoOccurrence()
        for _ in range(5):
            index.observe({0, 1, 2})
        held = index.evict(0)
        self.assertEqual(held["seen"], 5)
        self.assertEqual(held["row"], {1: 5, 2: 5})
        self.assertEqual(index.seen(0), 0)
        self.assertEqual(index.partners(0), [])

    def test_reinstating_restores_what_was_evicted(self):
        index = CoOccurrence()
        for _ in range(5):
            index.observe({0, 1, 2})
        index.reinstate(0, index.evict(0))
        self.assertEqual(index.seen(0), 5)
        self.assertEqual(index.together(0, 1), 5)
        self.assertEqual(index.partners(0), [1, 2])

    def test_a_boost_of_zero_is_refused(self):
        index = CoOccurrence()
        index.observe({0, 1})
        with self.assertRaises(ValueError):
            index.reinstate(0, index.evict(0), boost=0.0)


class TheBoostIsHysteresisAndNotDecoration(unittest.TestCase):
    """Reinstating at the old weight makes the surface the weakest again at
    once, which is a page fault every time it is touched."""

    def _faded(self):
        index = CoOccurrence(half_life=50)
        for _ in range(5):
            index.observe({0, 1})
        for _ in range(20):
            index.observe({2, 3})
            index.observe({3, 4})
        return index

    def test_without_a_boost_the_surface_is_weakest_again_immediately(self):
        index = self._faded()
        self.assertEqual(index.weakest(1), [0])
        index.reinstate(0, index.evict(0), boost=1.0)
        self.assertEqual(index.weakest(1), [0], "restoring at the old weight "
                                                "should thrash, and this "
                                                "asserts that it does")

    def test_with_a_boost_it_is_not(self):
        index = self._faded()
        index.reinstate(0, index.evict(0), boost=20.0)
        self.assertNotEqual(index.weakest(1), [0])


class WeakestRanksByWhatIsLEFT(unittest.TestCase):

    def test_the_faded_surface_is_shed_before_the_fresh_one(self):
        index = CoOccurrence(half_life=30)
        for _ in range(10):
            index.observe({0, 1})
        for _ in range(100):
            index.observe({2, 3})
        self.assertIn(0, index.weakest(2))
        self.assertNotIn(2, index.weakest(2))

    def test_ties_break_on_the_surface_id_so_two_runs_agree(self):
        index = CoOccurrence()
        for _ in range(3):
            index.observe({5, 9})
            index.observe({1, 7})
        self.assertEqual(index.weakest(4), [1, 5, 7, 9])

    def test_a_negative_count_is_refused(self):
        with self.assertRaises(ValueError):
            CoOccurrence().weakest(-1)


if __name__ == "__main__":
    unittest.main()
