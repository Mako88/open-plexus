"""`strength` and `reach` — the weighted walk that bounds SEARCH, not storage.

The meaning tests are the load-bearing ones. A quantity computed exactly as
described, described exactly as implemented, and named something the
implementation does not earn passes every other layer because they all agree
with each other. So these assert the properties that make this a weighted walk,
in a form that does not restate the formula.
"""

from __future__ import annotations

import unittest

from openplexus.grounding import (STATISTICS, CoOccurrence, equivalence_classes,
                                  reach, strength)

CONDITIONAL = STATISTICS["conditional"]


def _chain() -> CoOccurrence:
    """A -- B -- C, where A and C never share an occasion.

    The star `g33-02` and `g36-04` both measure: the only route from A to C is
    through B.
    """
    index = CoOccurrence()
    for _ in range(50):
        index.observe({0, 1})
    for _ in range(50):
        index.observe({1, 2})
    return index


def _with_distractor() -> CoOccurrence:
    """A real partner present half the time, and a distractor present always."""
    index = CoOccurrence()
    for step in range(100):
        present = {0, 99}
        if step % 2 == 0:
            present.add(1)
        index.observe(present)
    for _ in range(400):
        index.observe({99, 50})
    return index


class TheEdgeWeightIsSymmetricAndRefusesTheEverPresent(unittest.TestCase):

    def test_strength_does_not_depend_on_argument_order(self):
        index = _with_distractor()
        for other in (1, 99):
            self.assertAlmostEqual(strength(index, CONDITIONAL, 0, other),
                                   strength(index, CONDITIONAL, other, 0))

    def test_the_distractor_scores_BELOW_the_real_partner(self):
        """`g32-01`'s falsifier, asked of the symmetric weight.

        The distractor is present on 100 of the subject's 100 occasions and the
        real partner on 50, so RAW counting prefers the distractor. If this
        test fails, `strength` has lost the correction and every walk over it is
        measuring the wall rather than the thing.
        """
        index = _with_distractor()
        self.assertGreater(strength(index, CONDITIONAL, 0, 1),
                           strength(index, CONDITIONAL, 0, 99))

    def test_a_MEAN_also_ranks_them_correctly_here(self):
        """**This test exists to record a refuted justification.**

        A first version of `strength`'s docstring claimed `min` was necessary
        because a mean would carry `conditional(distractor, subject) = 1.0` and
        rank the distractor first. That was an unchecked assertion, and this
        assertion refuted it on the first run: mean gives the distractor
        **0.6** and the real partner **0.75**.

        So the case for `min` is structural — it is the soft analogue of
        mutuality — and NOT that the alternatives fail here. Keeping the test
        stops the wrong reason being re-derived.
        """
        index = _with_distractor()
        self.assertAlmostEqual(CONDITIONAL(index, 99, 0), 1.0)
        self.assertAlmostEqual(strength(index, CONDITIONAL, 0, 99, "mean"), 0.6)
        self.assertAlmostEqual(strength(index, CONDITIONAL, 0, 1, "mean"), 0.75)

    def test_the_combiners_are_ordered_and_min_is_the_conservative_one(self):
        """`min <= geometric <= mean <= max` for every edge, which is what makes
        `min` the conservative choice and `max` the permissive one. The sweep
        that picks between them is comparing points on this ordering."""
        index = _with_distractor()
        for other in (1, 99):
            values = [strength(index, CONDITIONAL, 0, other, name)
                      for name in ("min", "geometric", "mean", "max")]
            self.assertEqual(values, sorted(values), f"unordered at {other}")
            self.assertLess(values[0], values[-1],
                            "the combiners agree, so the axis is flat here")

    def test_a_hub_edge_is_LOPSIDED_which_is_why_the_choice_matters(self):
        """The specific doubt recorded at `strength`'s definition.

        A hub's edge to a spoke is near 1.0 in one direction and small in the
        other, so `min` weakens exactly the hub-to-spoke edges `g36-05` found
        being evicted. If this stops holding, that doubt is resolved and the
        docstring should say so.
        """
        index = CoOccurrence()
        for spoke in range(1, 8):
            for _ in range(20):
                index.observe({0, spoke})       # 0 is the hub
        forward = CONDITIONAL(index, 0, 1)      # hub -> spoke
        backward = CONDITIONAL(index, 1, 0)     # spoke -> hub
        self.assertAlmostEqual(forward, 1.0)
        self.assertLess(backward, 0.2)
        self.assertAlmostEqual(strength(index, CONDITIONAL, 0, 1, "min"),
                               backward)
        self.assertAlmostEqual(strength(index, CONDITIONAL, 0, 1, "max"),
                               forward)

    def test_an_unknown_combiner_is_refused_rather_than_defaulted(self):
        with self.assertRaises(ValueError):
            strength(_chain(), CONDITIONAL, 0, 1, "average")


class TheWalkReachesThroughAHub(unittest.TestCase):

    def test_a_reaches_c_although_they_never_co_occur(self):
        index = _chain()
        self.assertEqual(index.together(0, 2), 0)
        found = reach(index, CONDITIONAL, 0, beam=8, depth=3)
        self.assertIn(2, found, "the walk did not bridge the hub")
        self.assertIn(1, found)

    def test_the_direct_neighbour_outranks_the_bridged_one(self):
        """Path strength multiplies, so distance costs without a depth penalty
        being written anywhere. If this inverts, a long weak route would beat a
        short strong one and the ranking would be meaningless."""
        found = reach(_chain(), CONDITIONAL, 0, beam=8, depth=3)
        self.assertGreater(found[1], found[2])

    def test_depth_one_cannot_reach_the_far_end(self):
        found = reach(_chain(), CONDITIONAL, 0, beam=8, depth=1)
        self.assertIn(1, found)
        self.assertNotIn(2, found)

    def test_the_start_is_never_its_own_result(self):
        self.assertNotIn(0, reach(_chain(), CONDITIONAL, 0, beam=8, depth=3))


class TheBUDGETIsOnSearchAndNotOnStorage(unittest.TestCase):
    """The claim that distinguishes this from `equivalence_classes`: changing
    the budget changes what a QUESTION reaches and changes no stored value."""

    def _star(self) -> CoOccurrence:
        """One hub with six spokes, each spoke seen only with the hub."""
        index = CoOccurrence()
        for spoke in range(1, 7):
            for _ in range(20 + spoke):
                index.observe({0, spoke})
        return index

    def test_a_wider_beam_reaches_more_and_a_narrow_one_reaches_less(self):
        index = self._star()
        narrow = reach(index, CONDITIONAL, 1, beam=1, depth=2)
        wide = reach(index, CONDITIONAL, 1, beam=6, depth=2)
        self.assertLess(len(narrow), len(wide))
        # And the wide walk reaches every other spoke, which is the hub case a
        # single global `k` cannot express.
        self.assertEqual(set(wide) - {0}, {2, 3, 4, 5, 6})

    def test_the_table_is_untouched_by_the_budget(self):
        index = self._star()
        before = {s: dict((o, index.together(s, o))
                          for o in index.partners(s)) for s in index.rows()}
        reach(index, CONDITIONAL, 1, beam=1, depth=1)
        reach(index, CONDITIONAL, 1, beam=64, depth=6)
        after = {s: dict((o, index.together(s, o))
                         for o in index.partners(s)) for s in index.rows()}
        self.assertEqual(before, after)

    def test_the_hard_cut_DROPS_spokes_the_walk_keeps(self):
        """The comparison that motivates the whole mechanism.

        `equivalence_classes` bounds the representation, so on a star it cannot
        hold every spoke. If this ever stops being true the walk has no reason
        to exist, and the sweep comparing them would be comparing one thing
        against itself.
        """
        index = self._star()
        classes = equivalence_classes(index, CONDITIONAL, None)
        walked = set(reach(index, CONDITIONAL, 1, beam=6, depth=2)) - {0}
        self.assertLess(len(classes[1] - {0, 1}), len(walked))


class TheParametersAreGuarded(unittest.TestCase):

    def test_a_zero_beam_is_refused(self):
        with self.assertRaises(ValueError):
            reach(_chain(), CONDITIONAL, 0, beam=0)

    def test_a_zero_depth_is_refused(self):
        with self.assertRaises(ValueError):
            reach(_chain(), CONDITIONAL, 0, depth=0)

    def test_a_floor_above_every_edge_returns_nothing(self):
        self.assertEqual(reach(_chain(), CONDITIONAL, 0, floor=1.0), {})

    def test_an_unknown_surface_reaches_nothing_rather_than_raising(self):
        self.assertEqual(reach(_chain(), CONDITIONAL, 999), {})


if __name__ == "__main__":
    unittest.main()
