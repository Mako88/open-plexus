"""The stated/entailed split is the only thing that makes this task readable.

`closure.py` puts recall and composition in one stream with nothing marking
which is which. That is the point -- decision 95 measured the marked-question
format as most of the remaining gap -- and it means **every result rests on the
split being right.** If an entailed position were actually stated somewhere in
the same sequence, its relation would be recallable and the composition half
would be measuring recall.

These check the split, and that nothing about the LAYOUT gives the answer away.
Note 027 is the precedent: `reward_recall` leaked through its spacing, the leak
was found by reading the generator rather than by any test, and nine sweeps'
comparison set went with it.
"""

from __future__ import annotations

import unittest

from openplexus.tasks.closure import (
    OBJECT, RELATION, SUBJECT, WIDTH,
    ClosureConfig, dataset, generate, majority_floor, stated_positions)
from openplexus.tasks.kinship import COMPOSE, IGNORE, RELATIONS


class TheSplitIsSoundAndCovers(unittest.TestCase):

    def test_stated_and_entailed_are_disjoint_and_cover_every_target(self):
        for seed in range(30):
            sequence = generate(ClosureConfig(seed=seed))
            scored = {i for i, t in enumerate(sequence.targets) if t != IGNORE}
            entailed = set(sequence.entailed)
            stated = set(stated_positions(sequence))
            self.assertEqual(entailed & stated, set(),
                             "a position is counted as both stated and "
                             "entailed, so the two halves overlap")
            self.assertEqual(entailed | stated, scored,
                             "the halves do not cover every scored position")

    def test_an_entailed_relation_is_genuinely_implied_by_two_stated_facts(self):
        """The load-bearing claim, verified against COMPOSE independently.

        If an entailed edge were not actually implied, the model would be scored
        on something no amount of composing could reach -- and the result would
        read as a failure of the mechanism rather than of the task.
        """
        for seed in range(40):
            config = ClosureConfig(seed=seed)
            sequence = generate(config)
            edges = {(s, o): r for s, o, r in sequence.facts}
            for position in sequence.entailed:
                start = position - RELATION
                subject = sequence.tokens[start + SUBJECT]
                obj = sequence.tokens[start + OBJECT]
                relation = RELATIONS[sequence.targets[position]
                                     - config.n_people]
                # Some middle person must chain to it under the rule table.
                reachable = [
                    COMPOSE.get((edges[(subject, middle)], second))
                    for (a, middle), _ in list(edges.items())
                    if a == subject
                    for (b, target), second in edges.items()
                    if b == middle and target == obj]
                self.assertIn(
                    relation, reachable,
                    f"seed {seed}: an entailed edge is not implied by any pair "
                    f"of stated edges, so it cannot be composed")

    def test_an_entailed_pair_is_never_ALSO_stated(self):
        """Otherwise it is recallable and the composition half is recall."""
        for seed in range(40):
            sequence = generate(ClosureConfig(seed=seed))
            pairs = [(s, o) for s, o, _ in sequence.facts]
            self.assertEqual(len(pairs), len(set(pairs)),
                             "a (subject, object) pair appears twice, so one "
                             "of them can be answered by recall")


class TheLayOUTGivesNothingAway(unittest.TestCase):
    """Note 027's defect, checked for rather than discovered later."""

    def test_entailed_facts_are_not_at_predictable_positions(self):
        """If entailed edges clustered at the end, position would be the answer.

        `reward_recall` leaked exactly this way -- a constant gap meant the
        nearest binding before a reward was always the rewarded one, 160/160 --
        and no mechanism used it only because binding-detection was too weak.

        **Measured as the MEAN relative position, not as a share of the final
        block.** The first version of this test checked only the last block and
        a mutation removing the shuffle SURVIVED it: entailed edges are
        appended, so six of them spread across the last six blocks and only one
        lands in the final one. A leak that fills the back third is still a
        leak.
        """
        places = []
        for seed in range(80):
            sequence = generate(ClosureConfig(seed=seed))
            blocks = len(sequence.tokens) // WIDTH
            if blocks < 2:
                continue
            for position in sequence.entailed:
                places.append((position // WIDTH) / (blocks - 1))
        self.assertGreater(len(places), 100, "too few entailed edges to judge")
        mean = sum(places) / len(places)
        self.assertLess(
            abs(mean - 0.5), 0.12,
            f"entailed edges sit at a mean relative position of {mean:.2f} "
            f"rather than the middle; position predicts the split and the task "
            f"leaks the way reward_recall did")

    def test_every_fact_block_has_the_same_shape(self):
        """A marker, a subject, an object, a relation -- and nothing else that
        could distinguish an entailed fact from a stated one."""
        config = ClosureConfig(seed=1)
        sequence = generate(config)
        self.assertEqual(len(sequence.tokens) % WIDTH, 0)
        for start in range(0, len(sequence.tokens), WIDTH):
            self.assertEqual(sequence.tokens[start], config.fact_token)
            self.assertLess(sequence.tokens[start + SUBJECT], config.n_people)
            self.assertLess(sequence.tokens[start + OBJECT], config.n_people)
            self.assertGreaterEqual(sequence.tokens[start + RELATION],
                                    config.n_people)


class TheObjectPrecedesTheRelation(unittest.TestCase):
    """The design decision the whole task rests on.

    `FACT S O R` makes the store write `key(S, O) -> R`, which decision 107
    named as the binding the task needed and could not form. Reversed to
    `FACT S R O` it would write `key(S, R) -> O` and every fact would be
    recallable from its subject and relation, which is `kinship.py`.
    """

    def test_the_relation_is_the_last_token_of_its_block(self):
        sequence = generate(ClosureConfig(seed=2))
        for start in range(0, len(sequence.tokens), WIDTH):
            self.assertEqual(sequence.targets[start + RELATION],
                             sequence.tokens[start + RELATION])
            for offset in (0, SUBJECT, OBJECT):
                self.assertEqual(sequence.targets[start + offset], IGNORE)


class TheFloorIsMeasured(unittest.TestCase):

    def test_the_majority_floor_is_well_below_one(self):
        """A floor near 1.0 would mean one relation dominates and the task is
        answerable by always saying it."""
        floor = majority_floor(ClosureConfig(seed=0), n_sequences=200)
        self.assertGreater(floor, 0.0)
        self.assertLess(floor, 0.5,
                        f"the commonest entailed relation covers {floor:.0%} "
                        f"of targets, so guessing it is most of the task")


class ImpossibleShapesAreRefused(unittest.TestCase):

    def test_too_few_stated_edges_to_imply_anything(self):
        with self.assertRaises(ValueError):
            ClosureConfig(n_stated=1)

    def test_too_few_people_for_a_composed_path(self):
        with self.assertRaises(ValueError):
            ClosureConfig(n_people=2)


class TheGraphIsDenseEnoughToImplySOMETHING(unittest.TestCase):
    """The calibration in the config docstring, pinned.

    At people 12 / stated 8 more than half of all sequences imply nothing at
    all, which would make the entailed half almost entirely noise. If the
    defaults drift back toward that, this fails.
    """

    def test_most_sequences_carry_entailed_edges(self):
        data = dataset(ClosureConfig(seed=0), 200)
        empty = sum(1 for s in data if not s.entailed)
        self.assertLess(
            empty / len(data), 0.10,
            f"{empty / len(data):.0%} of sequences imply nothing, so the "
            f"composition half is mostly absent")
        mean = sum(len(s.entailed) for s in data) / len(data)
        self.assertGreater(mean, 3.0,
                           f"only {mean:.2f} entailed edges per sequence")


if __name__ == "__main__":
    unittest.main()
