"""Tests for emission-time delivery.

The claim under test is not "it copes with delay". It is the sharp one GOALS.md
C2 asks for: **below a stated bound, the network's misbehaviour makes no
difference at all** — not less difference, none.

So the central assertion is an equality, not a tolerance. A test that allowed
"close enough" here would pass against a scheme that merely degrades gracefully,
which is the weaker property C2 was written to rule out.
"""

from __future__ import annotations

import unittest

from openplexus.transport import (
    DeliveryConfig, arrivals, delivered_order, reassemble)

N = 40


class TestBelowTheBoundNothingChanges(unittest.TestCase):
    def test_reassembly_is_exact_for_every_jitter_below_max_delay(self):
        """The property the whole design rests on.

        Emission index t is released at t + max_delay, so any event delayed by
        less than that has landed by the time its slot comes up. The output is
        then the true order regardless of what the network did to it.
        """
        for max_delay in (1, 2, 4, 8):
            for jitter in range(max_delay + 1):
                for seed in range(4):
                    config = DeliveryConfig(max_delay=max_delay, jitter=jitter,
                                            seed=seed)
                    with self.subTest(max_delay=max_delay, jitter=jitter, seed=seed):
                        self.assertTrue(config.within_bound)
                        self.assertEqual(delivered_order(N, config), list(range(N)))

    def test_arrival_order_really_was_scrambled(self):
        """Without this the test above is vacuous.

        If the network never actually reordered anything, "reassembly is exact"
        would be true for the trivial reason that there was nothing to reassemble.
        This asserts the input to the reassembly was genuinely out of order.
        """
        config = DeliveryConfig(max_delay=8, jitter=7, seed=1)
        landed = arrivals(N, config)
        emission_order = [emission for _, emission in landed]
        self.assertNotEqual(emission_order, sorted(emission_order),
                            "the network delivered in order, so this proves nothing")

    def test_different_scramblings_reassemble_identically(self):
        """Two runs whose packets arrived in completely different orders must
        produce the same processed sequence. This is the property that makes a
        distributed run reproducible."""
        a = DeliveryConfig(max_delay=8, jitter=7, seed=1)
        b = DeliveryConfig(max_delay=8, jitter=7, seed=2)
        self.assertNotEqual([e for _, e in arrivals(N, a)],
                            [e for _, e in arrivals(N, b)])
        self.assertEqual(delivered_order(N, a), delivered_order(N, b))


class TestAtAndAboveTheBoundItBreaks(unittest.TestCase):
    def test_the_boundary_is_exactly_max_delay(self):
        """Where the bound sits, pinned in both directions.

        Tolerance is `max_delay` inclusive, because arrivals are recorded before
        the slot is released — an event delayed by exactly the bound lands on the
        step it is needed. One more than that misses.

        The first version of this test asserted `max_delay - 1`, taken from the
        predecessor project's measurement of its own scheme rather than derived
        from this one. It failed, and it was the assertion that was wrong. The
        off-by-one is a property of where the release check sits, not a law worth
        importing.
        """
        for max_delay in (2, 4, 8):
            with self.subTest(max_delay=max_delay):
                at = DeliveryConfig(max_delay=max_delay, jitter=max_delay, seed=3)
                self.assertTrue(at.within_bound)
                self.assertEqual(delivered_order(N, at), list(range(N)))

                over = DeliveryConfig(max_delay=max_delay, jitter=max_delay + 1,
                                      seed=3)
                self.assertFalse(over.within_bound)
                self.assertNotEqual(delivered_order(N, over), list(range(N)))

    def test_beyond_the_bound_events_are_lost_not_reordered(self):
        """A late event is dropped from the output rather than appearing out of
        place. Silently appending it later would corrupt the sequence in a way
        that is much harder to notice than a gap."""
        config = DeliveryConfig(max_delay=2, jitter=6, seed=5)
        order = delivered_order(N, config)
        self.assertEqual(order, sorted(order), "output must stay in emission order")
        self.assertLess(len(order), N, "some events should have missed their slot")


class TestDrops(unittest.TestCase):
    def test_dropped_events_never_arrive_at_any_delay(self):
        """C3's failure, distinct from C2's: a dropped event is gone, not late."""
        config = DeliveryConfig(max_delay=8, jitter=0, drop=0.25, seed=7)
        order = delivered_order(N, config)
        self.assertLess(len(order), N)
        self.assertEqual(order, sorted(order))

    def test_no_drops_by_default(self):
        self.assertEqual(delivered_order(N, DeliveryConfig(max_delay=4)),
                         list(range(N)))


class TestValidation(unittest.TestCase):
    def test_rejects_impossible_configurations(self):
        for bad in (dict(max_delay=0), dict(jitter=-1), dict(drop=1.0),
                    dict(drop=-0.1)):
            with self.subTest(**bad):
                with self.assertRaises(ValueError):
                    DeliveryConfig(**bad)

    def test_empty_stream(self):
        self.assertEqual(reassemble([], DeliveryConfig(max_delay=4)), [])


if __name__ == "__main__":
    unittest.main()
