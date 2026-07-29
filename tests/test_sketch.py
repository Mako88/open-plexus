"""An address sketch must answer membership, and must never carry a value.

Decision 147 refuted two ways of choosing between two retrievals and named the
question neither could answer: **was anything written at this address?** The two
sketches here answer it two ways, and the difference between them is the whole
argument, so it is measured rather than asserted:

- **`SumSketch` fails at the width the store runs at**, because a sum of `N`
  normalised near-orthogonal keys carries cross-talk of standard deviation
  `sqrt(N / d)`. That is the mechanism, and a test that only checked the easy
  regime would have hidden it.
- **`AddressSketch` does not**, because its collision rate is set by `bits`
  rather than by `width`.

And the invariant that keeps the whole thing honest: **an unwritten address must
read exactly zero.** The `inherit` rule's bar is structurally zero rather than
fitted, which is the only reason it needs no tuned constant, and that holds only
if a miss is exact.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.sketch import AddressSketch, SumSketch


def keys(count: int, width: int, seed: int = 0) -> np.ndarray:
    return np.random.default_rng(seed).normal(size=(count, width))


class AnUnwrittenAddressReadsExactlyZero(unittest.TestCase):
    """The `inherit` rule compares against 0.0 and nothing else."""

    def test_a_fresh_sketch_reads_zero(self):
        sketch = AddressSketch(64, seed=1)
        for key in keys(20, 64):
            self.assertEqual(sketch.count(key), 0.0)

    def test_writes_elsewhere_do_not_make_an_address_read_nonzero(self):
        sketch = AddressSketch(64, seed=1)
        written = keys(200, 64, seed=2)
        for key in written:
            sketch.add(key)
        # 16 bits puts a collision at 1.5e-5 per pair, so 200 unrelated writes
        # against 200 unrelated probes should not produce one. A failure here is
        # the sketch being too small, not the test being unlucky.
        misses = sum(sketch.count(key) == 0.0 for key in keys(200, 64, seed=3))
        self.assertGreater(misses, 195)

    def test_a_written_address_reads_positive(self):
        sketch = AddressSketch(64, seed=1)
        for key in keys(200, 64, seed=2):
            sketch.add(key)
        for key in keys(200, 64, seed=2):
            self.assertGreater(sketch.count(key), 0.0)


class TheHashIsFreeOfTheStoresWidth(unittest.TestCase):
    """The claim `SumSketch` cannot make, at the width the model runs at."""

    def test_it_separates_written_from_unwritten_where_the_sum_does_not(self):
        width, count = 64, 100
        written = keys(count, width, seed=2)
        unwritten = keys(count, width, seed=3)

        hashed, summed = AddressSketch(width, seed=1), SumSketch(width)
        for key in written:
            hashed.add(key)
            summed.add(key)

        # The hash separates them completely.
        self.assertGreater(min(hashed.count(k) for k in written), 0.0)
        self.assertEqual(max(hashed.count(k) for k in unwritten), 0.0)

        # The sum does not, and this is decision 147's floor: cross-talk of
        # sqrt(100 / 64) ~= 1.25 against a signal of exactly 1.0. Written and
        # unwritten addresses OVERLAP, so no comparison built on it can work.
        self.assertLess(min(summed.count(k) for k in written),
                        max(summed.count(k) for k in unwritten))


class TheSumSketchWorksWhereItsFloorIsLow(unittest.TestCase):
    """Kept so the failure above reads as a regime rather than a bug."""

    def test_few_writes_in_a_wide_space_separate_cleanly(self):
        width, count = 4096, 20
        summed = SumSketch(width)
        for key in keys(count, width, seed=2):
            summed.add(key)
        written = min(summed.count(k) for k in keys(count, width, seed=2))
        unwritten = max(summed.count(k) for k in keys(count, width, seed=3))
        self.assertGreater(written, unwritten)


class DecayFollowsTheStore(unittest.TestCase):
    """A binding that has faded from the store must fade from the sketch."""

    def test_counts_shrink_by_the_factor(self):
        sketch = AddressSketch(64, seed=1)
        key = keys(1, 64)[0]
        sketch.add(key)
        before = sketch.count(key)
        sketch.decay(0.5)
        self.assertAlmostEqual(sketch.count(key), before * 0.5)

    def test_a_write_after_a_decay_is_worth_its_full_amount(self):
        sketch = AddressSketch(64, seed=1)
        first, second = keys(2, 64)
        sketch.decay(0.5)
        sketch.add(second)
        self.assertAlmostEqual(sketch.count(second), 1.0)

    def test_decay_never_makes_a_written_address_read_zero(self):
        # `inherit` treats 0.0 as "nothing was ever written here". If decay
        # could reach it, a stated fact would silently become an unstated one
        # and the entity would inherit its family's answer instead of its own.
        sketch = AddressSketch(64, seed=1)
        key = keys(1, 64)[0]
        sketch.add(key)
        for _ in range(2000):
            sketch.decay(0.99)
            self.assertGreater(sketch.count(key), 0.0)

    def test_the_same_holds_for_the_sum(self):
        summed = SumSketch(64)
        key = keys(1, 64)[0]
        summed.add(key)
        summed.decay(0.5)
        self.assertAlmostEqual(summed.count(key), 0.5)


class ItRecordsThatAndNeverWhat(unittest.TestCase):
    """The invariant that keeps the sketch from becoming a second store."""

    def test_the_count_is_a_scalar_per_address(self):
        sketch = AddressSketch(64, seed=1)
        sketch.add(keys(1, 64)[0])
        self.assertIsInstance(sketch.count(keys(1, 64)[0]), float)

    def test_two_writes_at_one_address_count_twice(self):
        sketch = AddressSketch(64, seed=1)
        key = keys(1, 64)[0]
        sketch.add(key)
        sketch.add(key)
        self.assertAlmostEqual(sketch.count(key), 2.0)

    def test_the_scale_of_a_key_does_not_change_what_a_write_is_worth(self):
        # Otherwise `key_scale` would silently set how much each write counts
        # for, which is the class of hidden coupling decision 74 was about.
        sketch = AddressSketch(64, seed=1)
        key = keys(1, 64)[0]
        sketch.add(key * 17.0)
        self.assertAlmostEqual(sketch.count(key), 1.0)

        summed = SumSketch(64)
        summed.add(key * 17.0)
        self.assertAlmostEqual(summed.count(key), 1.0)


class ItRefusesConfigurationsThatCannotWork(unittest.TestCase):

    def test_zero_bits_is_rejected(self):
        with self.assertRaises(ValueError):
            AddressSketch(64, bits=0)

    def test_absurd_widths_are_rejected(self):
        with self.assertRaises(ValueError):
            AddressSketch(64, bits=64)


if __name__ == "__main__":
    unittest.main()
