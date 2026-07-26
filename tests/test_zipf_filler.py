"""Filler drawn from a power law, and what makes that claim true.

This mode exists to attack one diagnosis. [Note 013](../docs/notes/013-salience-and-the-missing-body.md)
found the salience gate genuinely selective -- query positions fire at 7.6x the
filler rate -- and losing anyway, because uniform filler is 92% of the sequence
*and* maximally surprising. A 7.6x enrichment cannot survive that base rate.

Uniform filler is the adversarial case for any surprise-driven mechanism, and it
was chosen for reasons that had nothing to do with surprise. Real language is
Zipfian: the common word is common everywhere, and the rare word is the
informative one. If the base-rate explanation is right, this mode should move the
gating result; if it does not, the explanation is wrong and that is worth more
than another confirmation.

**What is tested here is the distribution, not the benchmark.** Whether it
changes anything is g8-02's business.
"""

from __future__ import annotations

import math
import unittest
from collections import Counter
from dataclasses import replace

from openplexus.tasks.mqar import MqarConfig, dataset

BASE = MqarConfig(n_pairs=4, seq_len=256, n_keys=32, n_values=8,
                  autoregressive=True, filler="zipf", seed=20260726)


def filler_counts(config: MqarConfig, n: int = 40) -> Counter:
    counts: Counter = Counter()
    for sequence in dataset(config, n):
        kinds = sequence.position_kinds()
        for token, kind in zip(sequence.tokens, kinds):
            if kind == "filler":
                counts[token] += 1
    return counts


def entropy(counts: Counter) -> float:
    total = sum(counts.values())
    return -sum((c / total) * math.log(c / total) for c in counts.values())


class ItIsActuallySkewed(unittest.TestCase):
    """The defining property: mass concentrates, and concentrates with `s`."""

    def test_it_carries_less_entropy_than_uniform_filler(self):
        skewed = entropy(filler_counts(BASE))
        uniform = entropy(filler_counts(replace(BASE, filler="random")))
        self.assertLess(
            skewed, uniform,
            "power-law filler was no more concentrated than uniform filler, so "
            "nothing about the base rate has changed and this mode is decoration")

    def test_a_larger_exponent_concentrates_further(self):
        values = [entropy(filler_counts(replace(BASE, zipf_s=s)))
                  for s in (0.5, 1.0, 1.5, 2.0)]
        self.assertEqual(values, sorted(values, reverse=True),
                         f"entropy did not fall as the exponent rose: {values}")

    def test_the_common_tokens_are_the_low_ranks_not_the_high_ones(self):
        """Inverting the law would still be skewed, and still be wrong.

        A test that only checks concentration passes on `weights[rank]` and
        `1/weights[rank]` alike, so it has to check WHICH end is heavy.
        """
        counts = filler_counts(replace(BASE, zipf_s=1.5))
        ranked = [token for token, _ in counts.most_common()]
        top, bottom = ranked[:5], ranked[-5:]
        self.assertLess(
            sum(top) / 5, sum(bottom) / 5,
            "the most frequent filler tokens were the high ids, so the law is "
            "inverted -- rare tokens are being made common")

    def test_zero_is_uniform_so_the_mode_contains_its_own_control(self):
        flat = entropy(filler_counts(replace(BASE, zipf_s=0.0)))
        uniform = entropy(filler_counts(replace(BASE, filler="random")))
        self.assertAlmostEqual(flat, uniform, places=1)


class ItIsStillTheSameTask(unittest.TestCase):
    """Changing the filler distribution must not change what is being asked."""

    def test_filler_never_collides_with_a_key_this_sequence_uses(self):
        """The ill-posedness guard, which every filler mode has to satisfy.

        A filler token equal to an in-use key is byte-identical to a query while
        requiring a different output, and no model can tell them apart.
        """
        for sequence in dataset(replace(BASE, zipf_s=2.0), 60):
            in_use = set(sequence.pairs)
            for token, kind in zip(sequence.tokens, sequence.position_kinds()):
                if kind == "filler":
                    self.assertNotIn(token, in_use)

    def test_the_queries_and_answers_are_untouched(self):
        """Only filler positions may differ between the modes."""
        skewed = dataset(BASE, 20)
        uniform = dataset(replace(BASE, filler="random"), 20)
        for a, b in zip(skewed, uniform):
            self.assertEqual(a.query_positions, b.query_positions)
            self.assertEqual(a.pairs, b.pairs)
            self.assertEqual(a.targets, b.targets)


class TheOtherModesAreUnchanged(unittest.TestCase):
    """Every earlier result was measured with these; adding a mode must not
    disturb the random stream they draw from."""

    def test_random_filler_is_byte_identical_to_before(self):
        one = dataset(replace(BASE, filler="random"), 8)
        two = dataset(replace(BASE, filler="random"), 8)
        self.assertEqual([s.tokens for s in one], [s.tokens for s in two])

    def test_a_negative_exponent_is_refused(self):
        with self.assertRaises(ValueError):
            replace(BASE, zipf_s=-1.0)


if __name__ == "__main__":
    unittest.main()
