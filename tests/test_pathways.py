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
from openplexus.pathways import PathTypes, concentration, flood

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


class TheFloodExpandsByWeightAndComposesAsItGoes(unittest.TestCase):
    """The join: propagation, edge meanings and composition in one walk.

    The world is a straight line, 0 -> 1 -> 2 -> 3, walked by edge kinds 0, 1
    and 4. Kind 0 then kind 1 amounts to kind 2; kind 2 then kind 4 amounts to
    kind 3. So the two-step answer is 2 and the three-step answer is 3, and the
    three-step one is only reachable by carrying the derived kind forward.
    """

    def world(self):
        types = PathTypes(kinds=6, spans=6)
        for _ in range(5):
            types.observe(0, 1, 2)       # kind 0 then 1 amounts to 2
            types.observe(2, 4, 3)       # kind 2 then 4 amounts to 3
        edges = {0: [(0, 1, 0.9)], 1: [(1, 2, 0.9)], 2: [(4, 3, 0.9)]}
        return types, (lambda node: edges.get(node, ()))

    def test_two_steps_arrive_with_their_route(self):
        types, adjacency = self.world()
        found = flood(adjacency, 0, 2, types, CONDITIONAL, floor=0.01, depth=3)[0]
        self.assertIn(2, found)
        self.assertEqual(found[2][1], (0, 1))

    def test_three_steps_need_the_composed_kind_carried_forward(self):
        """The thing a pair-shaped table cannot do without reducing first."""
        types, adjacency = self.world()
        found = flood(adjacency, 0, 3, types, CONDITIONAL, floor=0.01, depth=3)[0]
        self.assertIn(3, found)
        self.assertEqual(found[3][1], (0, 1, 4))

    def test_a_route_that_usually_means_something_else_still_counts(self):
        """The bug that made the flood arrive at NOTHING on real data.

        Scoring an arrival by what the route AMOUNTS to keeps only routes whose
        argmax happens to be the question, and throws away every route that
        carries real evidence while usually meaning something else. On
        FB15k-237 that was 0.0000 of answers reached, at every floor, while the
        tests here passed — because their worlds were built so the argmax and
        the question agreed.

        Here the pair means 2 twice as often as 5, so `best` says 2. A question
        about 5 must still find the endpoint.
        """
        types = PathTypes(kinds=6, spans=6)
        for _ in range(6):
            types.observe(0, 1, 2)
        for _ in range(3):
            types.observe(0, 1, 5)
        edges = {0: [(0, 1, 1.0)], 1: [(1, 9, 1.0)]}
        adjacency = lambda node: edges.get(node, ())     # noqa: E731 - local
        self.assertEqual(types.best(0, 1, CONDITIONAL)[0], 2)
        found, _, _ = flood(adjacency, 0, 5, types, CONDITIONAL, floor=0.01,
                            depth=2)
        self.assertIn(9, found)
        # And it scores below the same endpoint asked about the kind the route
        # DOES usually mean, or the evidence is not being read at all.
        stronger, _, _ = flood(adjacency, 0, 2, types, CONDITIONAL, floor=0.01,
                               depth=2)
        self.assertGreater(stronger[9][0], found[9][0])

    def test_depth_two_cannot_reach_the_three_step_answer(self):
        types, adjacency = self.world()
        found = flood(adjacency, 0, 3, types, CONDITIONAL, floor=0.01, depth=2)[0]
        self.assertNotIn(3, found)

    def test_the_floor_is_the_budget_and_it_prunes(self):
        # Strength multiplies, so three 0.9 edges land near 0.7 before the
        # composition confidences are applied. A floor above that stops it.
        types, adjacency = self.world()
        self.assertEqual(
            flood(adjacency, 0, 3, types, CONDITIONAL, floor=0.95, depth=3)[0], {})
        self.assertIn(
            3, flood(adjacency, 0, 3, types, CONDITIONAL, floor=0.01, depth=3)[0])

    def test_a_strong_walk_that_composes_weakly_is_pruned(self):
        """The floor that actually binds, and it binds AFTER composing.

        Both edges are certain, so the route is strong enough to walk. But the
        pair means its span only half the time, so what the route AMOUNTS to is
        weak — and a floor above that has to stop it. The check before
        composing cannot: strength only decreases, so it prunes nothing the
        later one would not.
        """
        types = PathTypes(kinds=6, spans=6)
        for _ in range(5):
            types.observe(0, 1, 2)      # the pair means 2 half the time
            types.observe(0, 1, 3)      # and 3 the other half
        edges = {0: [(0, 1, 1.0)], 1: [(1, 9, 1.0)]}
        adjacency = lambda node: edges.get(node, ())     # noqa: E731 - local
        self.assertIn(9, flood(adjacency, 0, 2, types, CONDITIONAL,
                               floor=0.3, depth=2)[0])
        self.assertEqual(flood(adjacency, 0, 2, types, CONDITIONAL,
                               floor=0.7, depth=2)[0], {})

    def test_a_route_that_composes_to_something_else_is_not_an_answer(self):
        types, adjacency = self.world()
        found = flood(adjacency, 0, 5, types, CONDITIONAL, floor=0.01, depth=3)[0]
        self.assertEqual(found, {})

    def test_the_ceiling_reports_that_it_gave_up(self):
        """A safety, and a run that gave up must not look like one that finished.

        The weight is meant to be the whole budget. This exists because a floor
        that fails to prune does not merely score badly — it does not return —
        and the caller has to be able to say how often that happened.
        """
        types, adjacency = self.world()
        found, expansions, gave_up = flood(adjacency, 0, 3, types, CONDITIONAL,
                                           floor=0.01, depth=3, ceiling=1)
        self.assertTrue(gave_up)
        self.assertEqual(found, {})
        whole, cost, finished = flood(adjacency, 0, 3, types, CONDITIONAL,
                                      floor=0.01, depth=3)
        self.assertFalse(finished)
        self.assertIn(3, whole)
        self.assertGreater(cost, expansions)

    def test_a_floor_of_zero_is_refused_rather_than_run(self):
        # It would expand every edge to the depth limit, which on a real graph
        # does not return -- a budget of nothing is not a budget.
        types, adjacency = self.world()
        with self.assertRaises(ValueError):
            flood(adjacency, 0, 2, types, CONDITIONAL, floor=0.0)[0]

    def test_one_step_is_refused_because_nothing_has_composed(self):
        types, adjacency = self.world()
        with self.assertRaises(ValueError):
            flood(adjacency, 0, 2, types, CONDITIONAL, floor=0.01, depth=1)[0]

    def test_agreeing_routes_add_up_and_the_kept_route_is_the_strongest(self):
        types = PathTypes(kinds=6, spans=6)
        for _ in range(5):
            types.observe(0, 1, 2)
            types.observe(3, 1, 2)
        edges = {0: [(0, 1, 0.9), (3, 5, 0.4)],
                 1: [(1, 9, 0.9)], 5: [(1, 9, 0.9)]}
        found, _, _ = flood(lambda n: edges.get(n, ()), 0, 2, types,
                            CONDITIONAL, floor=0.01, depth=2)
        strongest, _, _ = flood(lambda n: edges.get(n, ()), 0, 2, types,
                                CONDITIONAL, floor=0.01, depth=2,
                                accumulate="max")
        self.assertGreater(found[9][0], strongest[9][0])
        self.assertEqual(found[9][1], (0, 1))


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
