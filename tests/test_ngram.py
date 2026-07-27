"""The baselines have to be right, because everything else is measured against
them.

A corpus result is a comparison: the memory's bits per character against a
bigram's. If the bigram is wrong the comparison is wrong in a way no amount of
care about the memory can fix, and it will be wrong in the flattering direction
— an under-powered baseline makes the model look good.

So these test the counting itself against hand-computed values, and they test
the two ways cross-entropy can be quietly meaningless: an unnormalised
distribution, which yields a smaller number, and a zero probability, which
yields infinity rather than an error.
"""

from __future__ import annotations

import math
import unittest

from openplexus.ngram import (
    DEFAULT_K, NGram, absurd, bits_from_distributions, perplexity,
    uniform_bits)


class CountingIsWhatItClaims(unittest.TestCase):

    def test_unigram_probability_is_the_smoothed_base_rate(self):
        """Hand-computed: 'a' three times of four, vocab 2, k = 0.5.

        (3 + 0.5) / (4 + 0.5 * 2) = 3.5 / 5 = 0.7
        """
        model = NGram(vocab_size=2, order=0, k=0.5).fit([[0, 0, 0, 1]])
        self.assertAlmostEqual(model.probability((), 0), 0.7)
        self.assertAlmostEqual(model.probability((), 1), 0.3)

    def test_a_distribution_sums_to_one(self):
        model = NGram(vocab_size=5, order=1).fit([[0, 1, 2, 3, 4, 0, 1]])
        for context in ((), (0,), (4,)):
            with self.subTest(context=context):
                self.assertAlmostEqual(
                    math.fsum(model.distribution(context)), 1.0)

    def test_bigram_conditions_on_the_PREVIOUS_token(self):
        """The whole difference between order 0 and order 1.

        In `0 1 0 1`, token 1 always follows 0 and 0 always follows 1, so a
        bigram must be near certain either way while a unigram is at chance.
        """
        stream = [0, 1] * 20
        bigram = NGram(vocab_size=2, order=1).fit([stream])
        unigram = NGram(vocab_size=2, order=0).fit([stream])
        self.assertGreater(bigram.probability((0,), 1), 0.95)
        self.assertGreater(unigram.probability((), 1), 0.45)
        self.assertLess(unigram.probability((), 1), 0.55)

    def test_a_context_shorter_than_the_order_is_its_own_context(self):
        """The first token has no predecessor, and padding it would merge every
        stream start with whatever token the padding used."""
        model = NGram(vocab_size=3, order=2).fit([[0, 1, 2]])
        self.assertAlmostEqual(math.fsum(model.distribution(())), 1.0)
        self.assertGreater(model.probability((), 0), model.probability((), 1))

    def test_an_unseen_context_falls_back_to_uniform(self):
        """Not to zero. This is what the smoothing is for."""
        model = NGram(vocab_size=4, order=1).fit([[0, 1]])
        self.assertAlmostEqual(model.probability((3,), 2), 0.25)


class WhatTheBaselinesAreWorth(unittest.TestCase):

    def test_uniform_bits_is_log2_of_the_vocabulary(self):
        self.assertAlmostEqual(uniform_bits(256), 8.0)
        self.assertAlmostEqual(uniform_bits(1), 0.0)

    def test_a_predictable_stream_costs_a_bigram_almost_nothing(self):
        stream = [0, 1] * 200
        bits = NGram(vocab_size=2, order=1).fit([stream]).bits_per_token([stream])
        self.assertLess(bits, 0.1)

    def test_the_SAME_stream_costs_a_unigram_a_whole_bit(self):
        """The guard on the test above, and the point of reporting both.

        A model that beats uniform has not necessarily learned any structure.
        Here the unigram is at 1.0 bits — chance — on a stream a bigram gets for
        free, so 'beat uniform' and 'beat bigram' are different claims.
        """
        stream = [0, 1] * 200
        bits = NGram(vocab_size=2, order=0).fit([stream]).bits_per_token([stream])
        self.assertGreater(bits, 0.9)
        self.assertLess(bits, 1.1)

    def test_perplexity_is_two_to_the_bits(self):
        self.assertAlmostEqual(perplexity(3.0), 8.0)


class WaysCrossEntropyGoesQUIETLYWrong(unittest.TestCase):
    """Both of these produce a number rather than an error if unguarded, and
    both produce a FLATTERING one."""

    def test_an_unnormalised_distribution_is_refused(self):
        with self.assertRaises(ValueError) as caught:
            bits_from_distributions([[0.5, 0.5, 0.5]], [0])
        self.assertIn("not 1", str(caught.exception))

    def test_the_unnormalised_number_would_have_been_SMALLER(self):
        """Why the guard above is not pedantry.

        Doubling every probability halves the reported bits, so an
        unnormalised model reads as better rather than as broken.
        """
        honest = bits_from_distributions([[0.25, 0.75]], [0])
        self.assertAlmostEqual(honest, 2.0)
        self.assertLess(-math.log2(0.5), honest)

    def test_a_zero_probability_is_refused_rather_than_infinite(self):
        with self.assertRaises(ValueError) as caught:
            bits_from_distributions([[0.0, 1.0]], [0])
        self.assertIn("undefined", str(caught.exception))

    def test_zero_smoothing_is_refused_at_construction(self):
        """Refused where it is chosen, not where it explodes."""
        with self.assertRaises(ValueError):
            NGram(vocab_size=4, order=1, k=0.0)

    def test_scoring_nothing_is_refused(self):
        with self.assertRaises(ValueError):
            NGram(vocab_size=4, order=1).fit([[0, 1]]).bits_per_token([])


class ANumberWorseThanUniformIsBROKENNotBad(unittest.TestCase):
    """The guard that g10-01 needed and did not have.

    It reported **39.5 bits per character over an 86-symbol vocabulary, and
    NaN**, and both were read off a results table as though they measured a
    language model. They were a readout that had reached 1e72 with
    next-character accuracy of 0.005 — below the 1/86 chance rate.

    Uniform is what knowing nothing costs. Being far worse than that requires
    being confidently and specifically wrong, which a runaway readout or a
    mis-fitted temperature can manufacture and a model cannot reach by being
    poor at its job.
    """

    def test_the_number_that_actually_got_through(self):
        self.assertIsNotNone(absurd(39.544, 86))

    def test_nan_is_refused(self):
        self.assertIn("not finite", absurd(float("nan"), 86))

    def test_infinity_is_refused(self):
        self.assertIn("not finite", absurd(float("inf"), 86))

    def test_a_plausible_bad_model_is_ALLOWED(self):
        """The vacuity guard on the tests above.

        A rule that refused everything would pass them all while making the
        benchmark useless. 5.9 bits over 86 symbols is a genuinely poor model
        and a genuinely real measurement, and it must survive.
        """
        self.assertIsNone(absurd(5.891, 86))

    def test_uniform_itself_is_allowed(self):
        self.assertIsNone(absurd(uniform_bits(86), 86))

    def test_slightly_worse_than_uniform_is_allowed(self):
        """A miscalibrated model can sit a little above uniform and still be
        worth reporting. Only the wild values are refused."""
        self.assertIsNone(absurd(uniform_bits(86) + 0.5, 86))

    def test_the_threshold_scales_with_the_VOCABULARY(self):
        """13 bits is absurd for 86 symbols and unremarkable for 100,000.

        A constant cut-off would refuse real results on a large vocabulary,
        which is exactly the corpus this project would move to next.
        """
        self.assertIsNotNone(absurd(13.0, 86))
        self.assertIsNone(absurd(13.0, 100_000))

    def test_the_message_names_the_ceiling_it_used(self):
        """A refusal that does not say what it compared against cannot be
        argued with -- the same rule tools/recovery.py follows."""
        self.assertIn(f"{uniform_bits(86):.3f}", absurd(39.544, 86))


class TheTwoSCORERSAgree(unittest.TestCase):
    """`bits_from_distributions` exists so the memory model is scored by the
    same arithmetic as the baselines rather than a second copy of it.

    Three summarisers once disagreed about one number for exactly this reason,
    which is why `tools/recovery.py` exists. This checks the two paths here
    cannot drift apart.
    """

    def test_the_ngram_scored_through_its_own_distributions_matches(self):
        stream = [0, 1, 2, 1, 0, 2, 2, 1, 0]
        model = NGram(vocab_size=3, order=1).fit([stream])
        direct = model.bits_per_token([stream])
        through = bits_from_distributions(
            (model.distribution(tuple(stream[max(0, i - 1):i]))
             for i in range(len(stream))), stream)
        self.assertAlmostEqual(direct, through)


class BadArguments(unittest.TestCase):

    def test_a_token_outside_the_vocabulary_is_refused(self):
        with self.assertRaises(ValueError):
            NGram(vocab_size=3, order=1).fit([[0, 5]])

    def test_a_negative_order_is_refused(self):
        with self.assertRaises(ValueError):
            NGram(vocab_size=3, order=-1)

    def test_an_empty_vocabulary_is_refused(self):
        with self.assertRaises(ValueError):
            NGram(vocab_size=0, order=1)
        with self.assertRaises(ValueError):
            uniform_bits(0)

    def test_the_default_smoothing_is_positive(self):
        self.assertGreater(DEFAULT_K, 0.0)


if __name__ == "__main__":
    unittest.main()
