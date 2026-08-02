"""A window over moments — the first thing here that can tell "then" from "with".

The load-bearing tests are that `span=0` is byte-identical to the old
behaviour, and that a one-way write makes an ORDER recoverable that a
symmetric one destroys. Everything else would pass for a window that carried
nothing.
"""

from __future__ import annotations

import unittest

from openplexus.grounding import STATISTICS, CoOccurrence
from openplexus.moments import Window

CONDITIONAL = STATISTICS["conditional"]


def _alternating(span, moments=200):
    """A stream where 1 always follows 0 and 0 never follows 1.

    Both orders co-occur equally often across the stream, so a symmetric table
    cannot tell them apart and a directional one must.
    """
    index = CoOccurrence()
    window = Window(index, span=span)
    for _ in range(moments):
        window.observe([0])
        window.observe([1])
    return index, window


class OffByDefault(unittest.TestCase):
    """Every earlier number has to stay reachable."""

    def test_span_zero_is_the_old_behaviour_exactly(self):
        plain = CoOccurrence()
        for _ in range(50):
            plain.observe([0, 1])
        through = CoOccurrence()
        window = Window(through, span=0)
        for _ in range(50):
            window.observe([0, 1])
        self.assertEqual(plain.together(0, 1), through.together(0, 1))
        self.assertEqual(plain.seen(0), through.seen(0))
        self.assertEqual(plain.occasions, through.occasions)

    def test_span_zero_writes_no_edge_between_separate_moments(self):
        index, _ = _alternating(span=0)
        self.assertEqual(index.together(0, 1), 0)
        self.assertEqual(index.together(1, 0), 0)

    def test_a_negative_span_is_refused(self):
        with self.assertRaises(ValueError):
            Window(CoOccurrence(), span=-1)


class OrderIsRecoverable(unittest.TestCase):
    """The thing the system could not do before."""

    def test_what_follows_is_told_apart_from_what_precedes(self):
        index, window = _alternating(span=1)
        self.assertGreater(window.follows(1, 0, CONDITIONAL),
                           window.follows(0, 1, CONDITIONAL))

    def test_the_counts_themselves_are_asymmetric(self):
        """The companion, one layer down: if these were equal the statistic
        above would be reading a difference that is not in the table."""
        index, _ = _alternating(span=1)
        self.assertGreater(index.together(1, 0), index.together(0, 1))

    def test_a_symmetric_write_destroys_it(self):
        """What the old behaviour would have said. Without this the test above
        would pass for a table that simply had more of everything."""
        index = CoOccurrence()
        for _ in range(200):
            index.observe([0, 1])
        self.assertEqual(index.together(1, 0), index.together(0, 1))


class TheSpanReachesAsFarAsItSays(unittest.TestCase):

    def test_a_span_of_one_does_not_reach_two_moments_back(self):
        index = CoOccurrence()
        window = Window(index, span=1)
        for _ in range(100):
            window.observe([0])
            window.observe([1])
            window.observe([2])
        self.assertGreater(index.together(1, 0), 0)   # adjacent
        self.assertEqual(index.together(2, 0), 0)     # two apart

    def test_a_span_of_two_does(self):
        index = CoOccurrence()
        window = Window(index, span=2)
        for _ in range(100):
            window.observe([0])
            window.observe([1])
            window.observe([2])
        self.assertGreater(index.together(2, 0), 0)

    def test_a_wider_span_writes_more_edges(self):
        """The connection test on the dial itself."""
        def edges(span):
            index = CoOccurrence()
            window = Window(index, span=span)
            for step in range(200):
                window.observe([step % 7])
            return sum(len(index.partners(s)) for s in index.surfaces())

        self.assertGreater(edges(3), edges(1))


class AMomentStillMeetsItself(unittest.TestCase):

    def test_things_in_one_moment_are_still_symmetric(self):
        index = CoOccurrence()
        window = Window(index, span=2)
        for _ in range(50):
            window.observe([10, 11])
        self.assertEqual(index.together(10, 11), index.together(11, 10))

    def test_a_surface_is_never_its_own_predecessor(self):
        index = CoOccurrence()
        window = Window(index, span=2)
        for _ in range(50):
            window.observe([5])
        self.assertEqual(index.together(5, 5), 0)


if __name__ == "__main__":
    unittest.main()
