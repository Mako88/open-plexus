"""The counting mechanism, checked against what the statistics MEAN.

CLAUDE.md rule 10's calibration is `surprise`: a quantity computed exactly as
described, described exactly as implemented, tested against that description, and
named something the implementation did not earn. Every test here that matters is
therefore written as a property in words — *a thing that is always there tells
you nothing*, *admiration has to be returned* — with no reference to the formula
that is supposed to produce it.

The two load-bearing ones:

- `raw_count` must PREFER a distractor that is present every time. If it did not,
  the falsifier this whole line of work exists to run would be answering a
  question nobody asked.
- `ppmi` must score such a distractor at exactly zero, for every partner. That is
  the property that makes it a candidate at all, and it must hold without being
  told which surface is the distractor.
"""

from __future__ import annotations

import unittest

from openplexus.grounding import (STATISTICS, CoOccurrence, cliff, conditional,
                                  equivalence_classes, frequency_weighted,
                                  local_conditional, neighbours, ppmi,
                                  raw_count, reached_together, score_classes)


def _index(occasions: list[tuple[int, ...]]) -> CoOccurrence:
    index = CoOccurrence()
    for occasion in occasions:
        index.observe(occasion)
    return index


def _world(pairs: int = 4, repeats: int = 100, hub: bool = True,
           lonely: int = 40) -> tuple[CoOccurrence, int]:
    """`pairs` true couples, optionally with one surface present every time.

    Each couple `(2i, 2i+1)` appears together `repeats` times, and **each member
    appears `lonely` further times without the other.**

    Both members have to be intermittent, which the first version of this fixture
    got wrong. A surface that never turns up without its partner meets the
    partner and the hub exactly as often, so the two TIE and the ranking falls to
    the tie-break — an arbitrary outcome standing where a measurement should be.
    `presence` in `occasions.py` applies to every surface of a concept, so this
    is the shape the generator actually produces.
    """
    hub_id = 2 * pairs
    tail = (hub_id,) if hub else ()
    occasions = []
    for i in range(pairs):
        occasions.extend([(2 * i, 2 * i + 1) + tail] * repeats)
        occasions.extend([(2 * i,) + tail] * lonely)
        occasions.extend([(2 * i + 1,) + tail] * lonely)
    return _index(occasions), hub_id


class Counting(unittest.TestCase):
    """The accumulator, before any statistic is taken over it."""

    def test_a_pair_is_counted_once_per_occasion_in_both_directions(self):
        index = _index([(1, 2), (1, 2), (1, 3)])
        self.assertEqual(index.together(1, 2), 2)
        self.assertEqual(index.together(2, 1), 2)
        self.assertEqual(index.together(1, 3), 1)

    def test_a_surface_repeated_within_one_moment_counts_once(self):
        """An occasion is a SET. Otherwise a repeat is a stronger partner to all."""
        index = _index([(1, 1, 1, 2)])
        self.assertEqual(index.seen(1), 1)
        self.assertEqual(index.together(1, 2), 1)

    def test_a_surface_never_partners_itself(self):
        index = _index([(1, 2)])
        self.assertEqual(index.together(1, 1), 0)

    def test_occasions_counts_moments_not_surfaces(self):
        index = _index([(1, 2, 3), (4, 5)])
        self.assertEqual(index.occasions, 2)

    def test_an_unseen_surface_reads_zero_rather_than_raising(self):
        """A statistic is asked about surfaces it may never have met."""
        index = _index([(1, 2)])
        self.assertEqual(index.seen(99), 0)
        self.assertEqual(index.together(1, 99), 0)
        self.assertEqual(index.partners(99), [])


class WhatTheStatisticsMean(unittest.TestCase):
    """Properties, stated without reference to how they are computed."""

    def test_raw_counting_prefers_the_thing_that_is_always_there(self):
        """The failure the falsifier is aimed at, asserted as a fact about it."""
        index, hub = _world()
        for surface in index.surfaces():
            if surface == hub:
                continue
            best = neighbours(index, surface, raw_count, k=1)
            self.assertEqual(best, [hub],
                             f"raw counting did not rank the ever-present "
                             f"surface first for {surface}")

    def test_raw_counting_finds_the_true_partner_when_nothing_is_always_there(self):
        """The companion. Without it, a broken statistic passes the test above."""
        index, _ = _world(hub=False)
        for surface in index.surfaces():
            partner = surface + 1 if surface % 2 == 0 else surface - 1
            self.assertEqual(neighbours(index, surface, raw_count, k=1),
                             [partner])

    def test_a_thing_that_is_always_there_tells_you_nothing(self):
        """PPMI's defining property, and it must hold without being told which.

        A surface present on every occasion carries no information about any
        other: knowing it is here does not change the odds of anything, because
        it is here regardless. The statistic must return exactly nothing for it.
        """
        index, hub = _world()
        for surface in index.surfaces():
            if surface == hub:
                continue
            self.assertEqual(ppmi(index, surface, hub), 0.0)
            self.assertEqual(ppmi(index, hub, surface), 0.0)

    def test_and_it_still_finds_the_real_partners(self):
        """The companion. A statistic returning zero for everything passes above."""
        index, _ = _world()
        for surface in (0, 2, 4, 6):
            self.assertGreater(ppmi(index, surface, surface + 1), 0.0)

    def test_a_rarer_partner_that_always_travels_with_you_beats_a_commoner_one(self):
        """Why PPMI is expected to break on skew, asserted rather than assumed.

        Two candidates meet a surface equally often. One is rare and comes only
        with it; the other is common and comes with everything. Mutual
        information prefers the rare one — which is correct here and is exactly
        the behaviour that misfires when frequencies are very uneven.
        """
        index = _index([(0, 1, 2)] * 50 + [(2, 9)] * 500)
        self.assertEqual(index.together(0, 1), index.together(0, 2))
        self.assertGreater(ppmi(index, 0, 1), ppmi(index, 0, 2))
        self.assertEqual(raw_count(index, 0, 1), raw_count(index, 0, 2),
                         "raw counting cannot separate them, which is the point")

    def test_discounting_by_commonness_moves_a_common_partner_down(self):
        index = _index([(0, 1)] * 50 + [(0, 2)] * 50 + [(2, 9)] * 500)
        self.assertEqual(raw_count(index, 0, 1), raw_count(index, 0, 2))
        self.assertGreater(frequency_weighted(index, 0, 1),
                           frequency_weighted(index, 0, 2))

    def test_conditional_asks_how_reliably_the_neighbour_brings_you(self):
        """`P(x | y)`: of the times y showed up, how often x came too."""
        index = _index([(0, 1)] * 30 + [(1,)] * 70 + [(0, 2)] * 30)
        self.assertAlmostEqual(conditional(index, 0, 1), 0.30)
        self.assertAlmostEqual(conditional(index, 0, 2), 1.00)

    def test_the_only_statistic_needing_no_remote_read_prefers_the_distractor(self):
        """The C1 cost, stated as a property rather than as an argument.

        `owner(x)` holds `count(x,y)` and `count(x)`. The one normalisation it
        can compute alone is `P(other | x)`, and a thing present on every
        occasion has that at 1.0 for every surface while a true partner present
        only sometimes cannot. **So the free version fails for the same reason
        raw counting does**, and the working statistic is the one that has to ask
        another machine.
        """
        index, hub = _world()
        for surface in index.surfaces():
            if surface == hub:
                continue
            self.assertEqual(
                neighbours(index, surface, local_conditional, k=1), [hub],
                "the purely-local statistic did not prefer the distractor, so "
                "the remote read may not be necessary after all")

    def test_and_it_is_correct_when_nothing_is_always_there(self):
        """The companion. It is a working statistic, not a broken one — which is
        what makes its failure above a statement about the distractor rather
        than about the arithmetic."""
        index, _ = _world(hub=False)
        for surface in index.surfaces():
            partner = surface + 1 if surface % 2 == 0 else surface - 1
            self.assertEqual(
                neighbours(index, surface, local_conditional, k=1), [partner])

    def test_every_statistic_refuses_a_pair_that_never_met(self):
        index, _ = _world()
        for name, statistic in STATISTICS.items():
            with self.subTest(statistic=name):
                self.assertEqual(statistic(index, 0, 3), 0.0)


class Mutuality(unittest.TestCase):
    """Admiration has to be returned, and that is what stops a hub spreading."""

    #: 2 points at 0 because 0 is its commonest partner; 0 points at 1 because 1
    #: is its own. One-sided, so no edge — and 0-1 is two-sided, so an edge.
    ONE_SIDED = [(0, 1)] * 200 + [(0, 2)] * 150 + [(2, 3)] * 100

    def test_a_one_sided_link_is_not_an_edge(self):
        index = _index(self.ONE_SIDED)
        self.assertEqual(neighbours(index, 2, raw_count, k=1), [0])
        self.assertEqual(neighbours(index, 0, raw_count, k=1), [1])
        classes = equivalence_classes(index, raw_count, k=1)
        self.assertNotIn(2, classes[0])

    def test_a_two_sided_link_is(self):
        """The companion. A rule rejecting everything passes the test above."""
        index = _index(self.ONE_SIDED)
        classes = equivalence_classes(index, raw_count, k=1)
        self.assertEqual(classes[0], frozenset({0, 1}))

    def test_a_surface_with_no_returned_link_stands_alone(self):
        index = _index(self.ONE_SIDED)
        classes = equivalence_classes(index, raw_count, k=1)
        self.assertEqual(classes[2], frozenset({2}))

    def test_a_score_of_zero_is_never_an_edge_however_short_the_list(self):
        """`k` is a cap, not a quota. Padding would invent evidence."""
        index = _index([(0, 1)] * 10 + [(2, 3)] * 10)
        self.assertEqual(neighbours(index, 0, raw_count, k=5), [1])


#: A hub with three spokes, plus one very common surface that brushes against
#: all of them. Without that weak partner the hub's three scores are the WHOLE
#: ranking, there is no drop after them, and the rule has nothing to find — which
#: is how the first version of this fixture failed and is worth keeping visible:
#: a cliff rule needs something on the far side of the cliff.
_STAR = ([(0, 1)] * 300 + [(0, 2)] * 300 + [(0, 3)] * 300
         + [(0, 50)] * 20 + [(1, 50)] * 20 + [(2, 50)] * 20 + [(3, 50)] * 20
         + [(50, 60)] * 2000)


class TheCliff(unittest.TestCase):
    """Deriving the count from the ranking instead of being handed it."""

    def test_it_cuts_where_the_ranking_falls_off(self):
        self.assertEqual(cliff([0.9, 0.88, 0.87, 0.1, 0.09]), 3)

    def test_one_score_or_none_has_no_gap_to_argmax_over(self):
        self.assertEqual(cliff([]), 0)
        self.assertEqual(cliff([0.5]), 1)

    def test_on_an_EVEN_SLOPE_the_answer_is_decided_by_FLOATING_POINT(self):
        """A cliff rule needs a cliff, and on a slope it is worse than useless.

        These two lists are the same ranking with the same gaps, written
        differently. They give DIFFERENT answers — 2 and 1 — because
        `0.5 - 0.4` and `0.4 - 0.3` are not equal in binary, and an argmax over
        gaps has nothing else to go on.

        Note 058 measured real language co-occurrence decaying in steps of
        0.02–0.03 where the families task falls 0.45 at once, and found the
        profile bimodal at no setting. **So on that data this rule does not
        merely degrade — its output is determined by representation noise**, and
        any result taken from it there would be unreproducible for a reason no
        seed controls.

        Asserted rather than commented so nobody reads the docstring's caution as
        theoretical.
        """
        self.assertEqual(cliff([0.5, 0.4, 0.3, 0.2, 0.1]), 2)
        self.assertEqual(cliff([5.0, 4.0, 3.0, 2.0, 1.0]), 1)

    def test_and_on_a_REAL_cliff_it_is_stable_under_the_same_rescaling(self):
        """The companion. The instability is a property of the FLAT case, not of
        the rule everywhere — otherwise it could not be used at all."""
        self.assertEqual(cliff([0.9, 0.88, 0.87, 0.1, 0.09]), 3)
        self.assertEqual(cliff([90.0, 88.0, 87.0, 10.0, 9.0]), 3)

    def test_a_hub_and_a_spoke_get_DIFFERENT_counts_from_the_same_rule(self):
        """The property a fixed `k` cannot have, and the reason this exists.

        `g33-02` measured a single global `k` failing on a star: the hub needs
        `k` at least its own degree while a spoke needs 1, and no one value is
        both. Asserted here as a fact about the derived rule rather than about
        any particular world.
        """
        index = _index(_STAR)
        hub = neighbours(index, 0, conditional, k=None)
        spoke = neighbours(index, 1, conditional, k=None)
        self.assertEqual(sorted(hub), [1, 2, 3])
        self.assertEqual(spoke, [0])

    def test_a_fixed_k_cannot_do_that(self):
        """The companion. Whatever single `k` is chosen, one of the two is wrong."""
        index = _index(_STAR)
        for k in (1, 2, 3):
            hub = neighbours(index, 0, conditional, k=k)
            spoke = neighbours(index, 1, conditional, k=k)
            self.assertFalse(len(hub) == 3 and len(spoke) == 1,
                             f"k={k} gave the hub 3 and the spoke 1, which is "
                             f"what the derived rule is for")

    def test_look_is_a_ceiling_and_must_exceed_the_group(self):
        """Decision 167 measured 0.500 at a look of 4 for a group of 6, so being
        generous is free and being stingy is the one way to break it."""
        index = _index(_STAR)
        self.assertEqual(len(neighbours(index, 0, conditional, None, look=2)), 1)
        self.assertEqual(len(neighbours(index, 0, conditional, None, look=16)), 3)

    def test_a_look_of_zero_is_refused(self):
        index, _ = _world()
        with self.assertRaises(ValueError):
            neighbours(index, 0, conditional, None, look=0)


class TheWalk(unittest.TestCase):
    """`equivalence_classes` is what "reaching a concept" means here."""

    def test_the_walk_is_transitive(self):
        """A chain of mutual links is one class, which is what walking means."""
        index = _index([(0, 1)] * 100 + [(1, 2)] * 100 + [(2, 3)] * 100)
        classes = equivalence_classes(index, raw_count, k=2)
        self.assertEqual(classes[0], frozenset({0, 1, 2, 3}))

    def test_every_member_of_a_class_reaches_the_same_class(self):
        """Starting anywhere gives the same answer — the record's own claim."""
        index, _ = _world(hub=False)
        classes = equivalence_classes(index, raw_count, k=1)
        for surface, found in classes.items():
            for member in found:
                self.assertEqual(classes[member], found,
                                 f"{surface} and {member} disagree")

    def test_changing_k_changes_what_is_reached(self):
        """Connection test: the knob must reach the output."""
        index, _ = _world()
        self.assertNotEqual(equivalence_classes(index, raw_count, k=1),
                            equivalence_classes(index, raw_count, k=4))

    def test_changing_the_statistic_changes_what_is_reached(self):
        """Connection test: the arm must reach the output, or a sweep is inert."""
        index, _ = _world()
        self.assertNotEqual(equivalence_classes(index, raw_count, k=2),
                            equivalence_classes(index, ppmi, k=2))

    def test_a_hub_displaces_the_true_partner_under_counting_and_not_under_ppmi(self):
        """The falsifier in miniature, on a world small enough to read.

        Four unrelated couples and one thing present every time. Under counting
        the ever-present surface is everyone's best partner, so it takes a real
        partner's place: the class reached from 0 contains the hub and **not 1**,
        and every couple in the world is broken. Under PPMI the hub scores
        nothing, stands alone, and the couples come back intact.

        **What this test corrected, and it changes how a result is read.** It was
        first written asserting the hub welds ALL surfaces into one class. It
        does not, and mutuality is why: a hub can only keep `k` edges, because
        every other surface it points back at is one it did not choose. So the
        damage is not collapse — it is that a handful of concepts absorb the
        distractor while the rest are merely broken apart. **`captured` therefore
        understates the harm and `f1` is the quantity that sees it**, which is a
        caution for reading any sweep over these arms.
        """
        index, hub = _world()
        counted = equivalence_classes(index, raw_count, k=1)
        self.assertIn(hub, counted[0])
        self.assertNotIn(1, counted[0])
        for surface in (0, 2, 4, 6):
            self.assertNotIn(surface + 1, counted[surface],
                             "counting left a true couple intact")

        informed = equivalence_classes(index, ppmi, k=1)
        self.assertEqual(informed[hub], frozenset({hub}))
        for surface in (0, 2, 4, 6):
            self.assertEqual(informed[surface], frozenset({surface, surface + 1}))

    def test_mutuality_is_what_caps_the_hub(self):
        """The companion to the caution above, so it is a fact and not a story.

        Nine surfaces, and the ever-present one is the top-ranked partner of
        every one of the eight. A one-sided rule would give it eight edges. It
        keeps at most `k`.
        """
        index, hub = _world()
        for surface in index.surfaces():
            if surface != hub:
                self.assertEqual(neighbours(index, surface, raw_count, k=1),
                                 [hub])
        classes = equivalence_classes(index, raw_count, k=1)
        self.assertLessEqual(len(classes[hub]) - 1, 1)


class Scoring(unittest.TestCase):
    """The metrics must say what their names say."""

    TRUTH = {0: frozenset({0, 1}), 1: frozenset({0, 1}),
             2: frozenset({2, 3}), 3: frozenset({2, 3}),
             9: frozenset({9})}

    def test_a_perfect_recovery_scores_one_and_captures_nothing(self):
        recovered = dict(self.TRUTH)
        result = score_classes(recovered, self.TRUTH, distractors=[9])
        self.assertAlmostEqual(result["f1"], 1.0)
        self.assertAlmostEqual(result["captured"], 0.0)

    def test_a_collapsed_recovery_captures_everything(self):
        everything = frozenset({0, 1, 2, 3, 9})
        recovered = {s: everything for s in everything}
        result = score_classes(recovered, self.TRUTH, distractors=[9])
        self.assertAlmostEqual(result["captured"], 1.0)
        self.assertAlmostEqual(result["largest"], 1.0)
        self.assertLess(result["f1"], 0.6)

    def test_a_class_that_is_right_but_too_large_is_penalised(self):
        """F1 and not recall, so a mechanism cannot win by answering everything."""
        recovered = {s: frozenset({0, 1, 2, 3}) for s in (0, 1, 2, 3)}
        recovered[9] = frozenset({9})
        result = score_classes(recovered, self.TRUTH, distractors=[9])
        self.assertAlmostEqual(result["f1"], 2 * 0.5 * 1.0 / 1.5)
        self.assertAlmostEqual(result["captured"], 0.0,
                               msg="no distractor was in any class")

    def test_singletons_score_nothing_rather_than_raising(self):
        recovered = {s: frozenset({s}) for s in self.TRUTH}
        result = score_classes(recovered, self.TRUTH, distractors=[9])
        self.assertAlmostEqual(result["f1"], 2 * 1.0 * 0.5 / 1.5)

    def test_a_distractor_is_not_scored_as_a_concept(self):
        """It has no class to recover, so including it would inflate every f1."""
        recovered = dict(self.TRUTH)
        with_it = score_classes(recovered, self.TRUTH, distractors=[])
        without = score_classes(recovered, self.TRUTH, distractors=[9])
        self.assertAlmostEqual(with_it["f1"], without["f1"])
        self.assertEqual(len(self.TRUTH), 5)

    def test_reaching_together_scores_a_TOTAL_COLLAPSE_as_perfect(self):
        """Documented rather than fixed, because it cannot be fixed here.

        `reached_together` asks whether nominated pairs share a class, so a
        recovery that puts everything in one class satisfies every pair. It is
        recall and it has recall's failure. `g33-02` hit it for real: one class
        of 256 surfaces out of 257, reported as a perfect bridge, while
        `score_classes` read 0.0308 in the same cell.

        The assertion exists so that anyone changing this function sees the trap
        before removing the warning from its docstring.
        """
        everything = frozenset({0, 1, 2, 3, 9})
        collapsed = {s: everything for s in everything}
        self.assertEqual(reached_together(collapsed, [(0, 3), (1, 9)]), 1.0)

    def test_and_the_companion_metric_sees_the_collapse(self):
        """Which is why they are reported together and never apart."""
        everything = frozenset({0, 1, 2, 3, 9})
        collapsed = {s: everything for s in everything}
        result = score_classes(collapsed, self.TRUTH, distractors=[9])
        self.assertAlmostEqual(result["largest"], 1.0)
        self.assertLess(result["f1"], 0.6)

    def test_scoring_no_pairs_at_all_is_refused(self):
        """`complete` has nothing to bridge, and 1.0 for the absence of the
        question is the way this reads as a pass while testing nothing."""
        with self.assertRaises(ValueError):
            reached_together({0: frozenset({0})}, [])

    def test_a_world_of_nothing_but_distractors_is_refused(self):
        with self.assertRaises(ValueError):
            score_classes({}, {9: frozenset({9})}, distractors=[9])


if __name__ == "__main__":
    unittest.main()
