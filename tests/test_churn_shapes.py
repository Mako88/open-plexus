"""The churn-shape generator, which was wrong and said nothing about it.

`doomed` decides which dimensions a departing machine takes. Its first version
rounded a fractional machine count to the nearest whole one, so at P=1 a request
to remove half the width removed all of it -- and reported a clean-looking
0.000 that would have been written up as a finding about churn.

It was caught by running the connection control before dispatching the matrix.
That worked, but it worked by luck of ordering: nothing forced the control to run
first. These tests are what forces it, and they run in a second rather than after
a sweep. Experiment code is not usually tested here, and the standing reason is
that experiments are read once and thrown away -- but a generator that silently
returns the wrong SET rather than crashing is exactly the shape of thing that
turns a sweep into a confident wrong answer.
"""

from __future__ import annotations

import unittest

import numpy as np

from experiments.g4_02_machine_churn import block_is_possible, doomed


class BothShapesRemoveTheSameAmount(unittest.TestCase):
    """The comparison is about shape. If the sizes differ it is about size."""

    def test_scattered_and_block_remove_an_identical_count(self):
        rng = np.random.default_rng(0)
        for width, groups, fraction in ((64, 4, 0.25), (64, 4, 0.5),
                                        (64, 8, 0.25), (128, 8, 0.5)):
            with self.subTest(width=width, groups=groups, fraction=fraction):
                self.assertTrue(block_is_possible(width, groups, fraction))
                scattered = doomed("scattered", width, groups, fraction, rng)
                block = doomed("block", width, groups, fraction, rng)
                self.assertEqual(len(set(block.tolist())), len(block),
                                 "block removed a dimension twice, so it takes "
                                 "fewer distinct dimensions than scattered")
                self.assertEqual(len(scattered), len(block))
                self.assertEqual(len(scattered), int(round(width * fraction)))


class BlockChurnIsMachineShaped(unittest.TestCase):

    def test_block_removes_whole_groups_and_nothing_partial(self):
        """Every removed dimension's group must be removed entirely.

        This is the property that makes it a model of a machine leaving rather
        than of a coincidence.
        """
        rng = np.random.default_rng(1)
        width, groups = 64, 8
        per_group = width // groups
        removed = set(doomed("block", width, groups, 0.5, rng).tolist())
        for group in range(groups):
            owned = set(range(group * per_group, (group + 1) * per_group))
            self.assertIn(len(owned & removed), (0, per_group),
                          f"group {group} was partly removed, which is not "
                          f"something a machine leaving can do")

    def test_scattered_is_not_accidentally_machine_shaped(self):
        """Otherwise the two arms are the same experiment run twice.

        With 32 of 64 dimensions drawn at random across 8 groups, landing on
        whole groups is overwhelmingly unlikely -- but "unlikely" is not a test,
        so this asserts it over several draws rather than assuming it.
        """
        width, groups = 64, 8
        per_group = width // groups
        partial = 0
        for seed in range(8):
            removed = set(doomed("scattered", width, groups, 0.5,
                                 np.random.default_rng(seed)).tolist())
            for group in range(groups):
                owned = set(range(group * per_group, (group + 1) * per_group))
                if 0 < len(owned & removed) < per_group:
                    partial += 1
        self.assertGreater(partial, 0,
                           "scattered churn never split a group across 8 draws, "
                           "so it is indistinguishable from block churn")


class ImpossibleFractionsAreRefused(unittest.TestCase):
    """The bug: rounding a fractional machine instead of refusing it."""

    def test_half_a_machine_cannot_leave(self):
        """At P=1 the only block levels are none and all.

        The original generator rounded 0.5 machines up to 1 here and removed the
        entire model. It must raise instead -- a sweep that silently measures
        100% churn where it asked for 50% produces a number, and a number is far
        harder to notice than a crash.
        """
        self.assertFalse(block_is_possible(64, 1, 0.5))
        with self.assertRaises(ValueError):
            doomed("block", 64, 1, 0.5, np.random.default_rng(0))

    def test_a_fraction_that_straddles_machines_is_refused(self):
        self.assertFalse(block_is_possible(64, 4, 0.3))
        with self.assertRaises(ValueError):
            doomed("block", 64, 4, 0.3, np.random.default_rng(0))

    def test_exact_fractions_are_allowed(self):
        for groups, fraction in ((4, 0.25), (4, 0.5), (8, 0.125), (8, 0.5)):
            with self.subTest(groups=groups, fraction=fraction):
                self.assertTrue(block_is_possible(64, groups, fraction))

    def test_zero_churn_is_not_a_block(self):
        """Nothing leaving is not "a whole number of machines leaving".

        Otherwise the grid runs a block arm at churn 0.0, which is the scattered
        arm under another name and pads the matrix with a duplicate.
        """
        self.assertFalse(block_is_possible(64, 4, 0.0))
        self.assertEqual(len(doomed("block", 64, 4, 0.0,
                                    np.random.default_rng(0))), 0)


if __name__ == "__main__":
    unittest.main()
