"""The cost model has to be right, because note 015 got one wrong.

Its first version made competitive capture look cheap; the corrected version
showed the obvious implementation is MORE expensive than superposition for
exactly the tiny nodes this project exists for. A cost model that flatters a
mechanism is how a mechanism gets built and then withdrawn.

So these pin the two properties the conclusion rests on: that storing vectors
scales with the node and therefore never gets cheaper by shrinking it, and that
storing token ids does not scale with the node at all.
"""

from __future__ import annotations

import unittest

from tools.slot_cost import (
    VOCAB, report, slots_as_tokens, slots_as_vectors, superposed)


class WhatScalesWithWhat(unittest.TestCase):

    def test_the_dense_store_scales_with_the_node(self):
        self.assertEqual(superposed(4, 256), 4 * 256)
        self.assertEqual(superposed(8, 256), 2 * superposed(4, 256))

    def test_vector_slots_scale_with_the_node_TOO(self):
        """The whole reason they never get cheaper by shrinking it."""
        self.assertEqual(slots_as_vectors(8, VOCAB, 8),
                         2 * slots_as_vectors(4, VOCAB, 8))

    def test_the_vector_ratio_is_the_SAME_at_every_width(self):
        """Both sides scale with `w`, so the ratio is a constant.

        This is the finding, and a cost model that reported it falling with
        width would be describing a mechanism that gets cheap on tiny nodes —
        which is precisely the error note 015 corrected.
        """
        ratios = {row["vectors_ratio"] for row in report()}
        self.assertEqual(len(ratios), 1)

    def test_token_slots_do_NOT_scale_with_the_node(self):
        self.assertEqual(slots_as_tokens(VOCAB, 8), slots_as_tokens(VOCAB, 8))
        self.assertEqual(
            {row["slots_tokens"] for row in report()},
            {VOCAB * 8})

    def test_token_slots_DO_scale_with_the_vocabulary(self):
        """The axis that matters for anything past character level. A word-level
        vocabulary is a thousand times this and the table changes completely."""
        self.assertEqual(slots_as_tokens(1000, 8), 1000 * 8)
        self.assertGreater(slots_as_tokens(1000, 8), slots_as_tokens(VOCAB, 8))

    def test_more_slots_cost_proportionally_more(self):
        self.assertEqual(slots_as_tokens(VOCAB, 16),
                         2 * slots_as_tokens(VOCAB, 8))


class TheCrossover(unittest.TestCase):
    """Where token slots become cheaper than the node's own store."""

    def test_a_narrow_node_pays_MORE_for_slots_than_for_its_store(self):
        rows = {row["node_width"]: row for row in report(slots=8, d=256)}
        self.assertGreater(rows[1]["tokens_ratio"], 1.0)

    def test_a_wider_node_pays_less(self):
        rows = {row["node_width"]: row for row in report(slots=8, d=256)}
        self.assertLess(rows[16]["tokens_ratio"], 1.0)

    def test_the_ratio_falls_monotonically_with_width(self):
        ratios = [row["tokens_ratio"] for row in report()]
        self.assertEqual(ratios, sorted(ratios, reverse=True))

    def test_the_vector_option_never_crosses(self):
        """The guard on the tests above: they must be about the TOKEN option
        specifically, not about slots in general."""
        for row in report():
            self.assertGreater(row["vectors_ratio"], 1.0)


if __name__ == "__main__":
    unittest.main()
