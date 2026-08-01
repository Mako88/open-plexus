"""Blocks that do not overlap, and refusals where a silent wrap would collide.

`openplexus/namespace.py` is what lets three sources that all number from zero
share one `CoOccurrence`. The failure it prevents is silent — colliding ids
raise nothing, they just add two unrelated things' counts together — so these
fix that it separates, that the separation FOLLOWS the sizes asked for, and that
the two ways back into a collision are refused rather than wrapped.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus import wiring  # noqa: E402
from openplexus.namespace import Namespace  # noqa: E402


class BlocksDoNotOverlap(unittest.TestCase):

    def test_two_kinds_get_disjoint_numbers(self):
        space = Namespace()
        first = space.reserve("image", 10)
        second = space.reserve("fact", 10)
        self.assertEqual(set(first) & set(second), set())

    def test_the_blocks_FOLLOW_the_sizes(self):
        """The connection test. Fixed blocks would pass the test above."""
        space = Namespace()
        space.reserve("image", 3)
        self.assertEqual(space.reserve("fact", 4), range(3, 7))
        self.assertEqual(space.size, 7)

    def test_local_zero_of_each_kind_is_a_different_node(self):
        """THE WHOLE POINT. Every source in this project numbers from zero."""
        space = Namespace()
        space.reserve("image", 5)
        space.reserve("fact", 5)
        self.assertNotEqual(space.node("image", 0), space.node("fact", 0))

    def test_a_node_says_which_kind_it_belongs_to(self):
        space = Namespace()
        space.reserve("image", 5)
        space.reserve("fact", 5)
        self.assertEqual(space.owner(space.node("fact", 2)), "fact")

    def test_it_satisfies_the_disjointness_check(self):
        """The two halves meeting: what `reserve` hands out is what
        `wiring.expect(disjoint=True)` is given."""
        space = Namespace()
        space.reserve("image", 20)
        space.reserve("fact", 20)
        with wiring.expect(holding={"image", "fact"}, disjoint=True):
            wiring.kind("image", space.ids("image"))
            wiring.kind("fact", space.ids("fact"))


class ARouteBackIntoACollisionIsRefused(unittest.TestCase):

    def test_an_out_of_range_local_id_raises_rather_than_wrapping(self):
        """A wrapped id lands in the NEIGHBOURING kind's block, which is the
        collision this module exists to prevent arriving by another road."""
        space = Namespace()
        space.reserve("image", 5)
        space.reserve("fact", 5)
        with self.assertRaises(IndexError):
            space.node("image", 5)
        with self.assertRaises(IndexError):
            space.node("image", -1)

    def test_reserving_a_kind_twice_is_refused(self):
        space = Namespace()
        space.reserve("image", 5)
        with self.assertRaises(ValueError):
            space.reserve("image", 5)

    def test_an_unreserved_kind_raises(self):
        space = Namespace()
        with self.assertRaises(KeyError):
            space.node("image", 0)
        with self.assertRaises(KeyError):
            space.ids("image")

    def test_a_node_outside_every_block_raises(self):
        space = Namespace()
        space.reserve("image", 5)
        with self.assertRaises(KeyError):
            space.owner(99)


if __name__ == "__main__":
    unittest.main()
