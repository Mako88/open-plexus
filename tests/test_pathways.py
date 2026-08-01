"""A route kind means something, and a candidate nothing reaches is absent.

`openplexus/pathways.py` is the mechanism behind this project's first positive
result on external data, and these fix the three claims that result rests on:

- **a route kind is a PAIR and its order matters** — walking `born in` then
  `located in` is not walking `located in` then `born in`, and conflating them
  would average two meanings into one row;
- **evidence ACCUMULATES over routes** — `sum` lets many weak agreeing routes
  outrank one strong route, which is the whole difference from a rule lookup,
  and it is worth 0.1234 against 0.0834 on FB15k-237;
- **an unreached candidate is absent, not zero** — the failure mode that costs
  the mechanism −0.0046 on two thirds of queries, and the one no weighting can
  repair.

The measured numbers live with the sweep. These fix the behaviour they rest on.
"""

from __future__ import annotations

import unittest

from openplexus.grounding import STATISTICS
from openplexus.pathways import PathTypes, concentration

CONDITIONAL = STATISTICS["conditional"]


def taught() -> PathTypes:
    """Route kind (0, 1) spans kind 0; route kind (2, 3) spans kind 1."""
    types = PathTypes(kinds=6, spans=3)
    for _ in range(5):
        types.observe(0, 1, 0)
        types.observe(2, 3, 1)
    return types


class ARouteKindIsAnOrderedPair(unittest.TestCase):

    def test_what_was_observed_is_what_comes_back(self):
        types = taught()
        self.assertGreater(types.weight(0, 1, 0, CONDITIONAL), 0.0)
        self.assertGreater(types.weight(2, 3, 1, CONDITIONAL), 0.0)

    def test_the_order_of_the_pair_matters(self):
        # Otherwise `born in` then `located in` and its reverse are one row, and
        # two different meanings are averaged into it.
        self.assertEqual(taught().weight(1, 0, 0, CONDITIONAL), 0.0)

    def test_a_route_kind_says_nothing_about_a_span_it_never_covered(self):
        self.assertEqual(taught().weight(0, 1, 1, CONDITIONAL), 0.0)

    def test_both_halves_must_support_the_answer(self):
        # A first edge that leads everywhere cannot carry a route alone, which
        # is grounding's refusal of an ever-present partner one level up.
        types = PathTypes(kinds=6, spans=3)
        for _ in range(5):
            types.observe(0, 1, 0)
            types.observe(0, 4, 1)      # kind 0 also starts routes spanning 1
        self.assertEqual(types.weight(0, 5, 0, CONDITIONAL), 0.0)

    def test_an_empty_space_is_refused(self):
        with self.assertRaises(ValueError):
            PathTypes(kinds=0, spans=3)
        with self.assertRaises(ValueError):
            PathTypes(kinds=3, spans=0)


class EvidenceAccumulatesOverRoutes(unittest.TestCase):
    """The claim that separates a ranked walk from a thresholded lookup."""

    def test_many_weak_agreeing_routes_outrank_one_strong_route(self):
        types = PathTypes(kinds=6, spans=2)
        for _ in range(9):
            types.observe(0, 1, 0)      # a route kind that always means 0
        for _ in range(9):
            types.observe(2, 3, 0)      # and another, weaker on its own
            types.observe(2, 3, 1)
        # Endpoint 100 is reached once by the strong kind; 200 three times by
        # the weaker one.
        routes = [(0, 1, 100), (2, 3, 200), (2, 3, 200), (2, 3, 200)]
        summed = types.score(routes, 0, CONDITIONAL, "sum")
        self.assertGreater(summed[200], summed[100])
        # And under `max`, which is what a lookup does, the single strong route
        # wins -- so the two accumulators genuinely differ.
        best = types.score(routes, 0, CONDITIONAL, "max")
        self.assertGreater(best[100], best[200])

    def test_an_unknown_accumulator_is_refused(self):
        with self.assertRaises(ValueError):
            taught().score([], 0, CONDITIONAL, "average")


class AnUnreachedCandidateIsAbsent(unittest.TestCase):
    """Absent and zero are different, and the difference is the failure mode."""

    def test_nothing_reached_scores_nothing_at_all(self):
        self.assertEqual(taught().score([], 0, CONDITIONAL), {})

    def test_a_route_that_says_nothing_leaves_its_endpoint_out(self):
        # Reached, but by a route kind carrying no evidence for the span asked
        # about. The endpoint must not appear with a zero -- a caller cannot
        # tell "arrived and said nothing" from "never arrived" if it does.
        found = taught().score([(0, 1, 77)], 1, CONDITIONAL)
        self.assertEqual(found, {})

    def test_only_the_endpoints_a_route_reached_are_present(self):
        found = taught().score([(0, 1, 77), (2, 3, 88)], 0, CONDITIONAL)
        self.assertEqual(set(found), {77})


class ConcentrationIsAConfidence(unittest.TestCase):

    def test_one_dominant_endpoint_is_concentrated(self):
        self.assertAlmostEqual(concentration({1: 9.0, 2: 1.0}), 0.9)

    def test_evidence_sprayed_evenly_is_not(self):
        self.assertAlmostEqual(concentration({i: 1.0 for i in range(10)}), 0.1)

    def test_nothing_reached_has_no_confidence(self):
        self.assertEqual(concentration({}), 0.0)
        self.assertEqual(concentration({1: 0.0}), 0.0)


if __name__ == "__main__":
    unittest.main()
