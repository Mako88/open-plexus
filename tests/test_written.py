"""The written channel — a sensor reading, and the ways it is allowed to fail.

The load-bearing tests are the ones asserting the channel is NOT a label: that
it is sometimes silent, sometimes wrong, and that one digit produces more than
one surface. Everything else here would keep passing if `speak` returned the
digit's index.
"""

from __future__ import annotations

import random
import unittest

from openplexus.tasks.mnist import WORDS
from openplexus.tasks.written import (FORMS, Channel, features, forms, render,
                                      speak)


class OneConceptHasSeveralAppearances(unittest.TestCase):

    def test_a_digit_is_written_more_than_one_way(self):
        self.assertEqual(len(set(forms(3))), FORMS)

    def test_the_forms_do_not_collide_under_the_features(self):
        """Surfaces the hash cannot tell apart would make the discovery
        this module poses trivial rather than hard."""
        seen = {tuple(features(text.encode())) for text in forms(3)}
        self.assertEqual(len(seen), FORMS)

    def test_no_two_digits_share_a_form(self):
        every = [text for digit in range(len(WORDS)) for text in forms(digit)]
        self.assertEqual(len(every), len(set(every)))

    def test_a_digit_outside_the_vocabulary_is_refused(self):
        with self.assertRaises(ValueError):
            forms(len(WORDS))


class ItIsNotALabel(unittest.TestCase):
    """The three properties that separate an observation from a label."""

    def test_the_channel_is_sometimes_silent(self):
        rng = random.Random(0)
        channel = Channel(silence=0.3, mistake=0.0, corrupt=0.0)
        said = [speak(channel, 3, rng) for _ in range(2000)]
        self.assertGreater(said.count(None), 0)
        self.assertLess(said.count(None), len(said))

    def test_the_channel_is_sometimes_wrong(self):
        rng = random.Random(0)
        channel = Channel(silence=0.0, mistake=0.4, corrupt=0.0)
        said = [speak(channel, 3, rng)[1] for _ in range(2000)]
        mine = {text.encode() for text in forms(3)}
        self.assertTrue(any(word not in mine for word in said),
                        "a channel that is never wrong is a label")

    def test_a_mistake_names_another_digit_and_never_the_right_one(self):
        """A wrong word must be a plausible word, not gibberish, and must not
        secretly still be correct."""
        rng = random.Random(1)
        channel = Channel(silence=0.0, mistake=1.0, corrupt=0.0)
        every = {text.encode() for digit in range(len(WORDS))
                 for text in forms(digit)}
        mine = {text.encode() for text in forms(3)}
        for _ in range(500):
            named, word = speak(channel, 3, rng)
            self.assertIn(word, every)
            self.assertNotIn(word, mine)
            self.assertNotEqual(named, 3)
            self.assertIn(word, {text.encode() for text in forms(named)})

    def test_one_digit_produces_many_distinct_renderings(self):
        """Multiplicity is the whole repair. One surface per class was the bug."""
        rng = random.Random(0)
        channel = Channel(silence=0.0, mistake=0.0, corrupt=0.5)
        said = {speak(channel, 3, rng)[1] for _ in range(2000)}
        self.assertGreater(len(said), FORMS)


class PerturbingTheChannelMovesTheOutput(unittest.TestCase):
    """Connection tests. Each dial must be wired to the thing it names."""

    def test_more_corruption_makes_more_distinct_renderings(self):
        def distinct(corrupt):
            rng = random.Random(0)
            channel = Channel(silence=0.0, mistake=0.0, corrupt=corrupt)
            return len({speak(channel, 3, rng)[1] for _ in range(2000)})

        self.assertGreater(distinct(0.6), distinct(0.0))

    def test_more_silence_means_fewer_words(self):
        def spoken(silence):
            rng = random.Random(0)
            channel = Channel(silence=silence, mistake=0.0, corrupt=0.0)
            return sum(speak(channel, 3, rng) is not None for _ in range(2000))

        self.assertLess(spoken(0.8), spoken(0.1))

    def test_corruption_off_gives_exactly_the_forms(self):
        """The companion: without it, the test above would pass for a channel
        that was noisy at every setting."""
        rng = random.Random(0)
        said = {render(3, rng, corrupt=0.0) for _ in range(500)}
        self.assertEqual(said, {text.encode() for text in forms(3)})


class TheFeaturesAreABareHistogram(unittest.TestCase):

    def test_every_byte_is_counted_once(self):
        got = features(b"aab")
        self.assertEqual(sum(got), 3.0)
        self.assertEqual(got[ord("a")], 2.0)
        self.assertEqual(got[ord("b")], 1.0)

    def test_order_is_discarded_and_this_is_the_known_cost(self):
        self.assertEqual(features(b"three"), features(b"there"))


class DialsAreRefusedRatherThanClamped(unittest.TestCase):

    def test_a_probability_outside_the_range_is_refused(self):
        with self.assertRaises(ValueError):
            Channel(silence=1.5)

    def test_a_channel_that_could_never_be_right_is_refused(self):
        with self.assertRaises(ValueError):
            Channel(silence=0.7, mistake=0.5)


if __name__ == "__main__":
    unittest.main()
