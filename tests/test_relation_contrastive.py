"""The local contrastive relation rule: does its signal reach the vectors, and is
the held-out rule genuinely held out?

**This does not duplicate** `tools/relation_contrastive.py`'s own run, which needs
the CLUTRR corpus and answers *"what does it score"*. These assert the properties
that score depends on, on synthetic triangles, so they run in CI where `data/` is
gitignored and absent.

## Why the guard test is the important one

Note 070 reported `0.223` for a counted relation representation on a random-quarter
holdout; note 088 then measured the same mechanism BELOW random filling once the
holdout was adversarial. The difference between a result and an artefact here is
whether a held-out rule reached the representation.

Measured, on the real corpus, the day this was written: excluding held-out rules
scores **0.2437**, including them scores **0.4188**. **The guard is worth 0.175** —
so if it silently stopped working, the number would nearly double and would look like
a breakthrough.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.tasks.clutrr import RELATIONS
from tools.relation_contrastive import INDEX, learn, learn_pairs, triangles

WIDTH = 16
#: Three relation names that certainly exist, so the test does not depend on which.
A, B, C = RELATIONS[0], RELATIONS[1], RELATIONS[2]


def puzzle(first: str, second: str, target: str):
    """One 2-hop row in `relation_profiles.rows` shape: edges, types, query, target."""
    return ([(0, 1), (1, 2)], [first, second], (0, 2), target)


class AHeldOutRuleNeverReachesTheRepresentation(unittest.TestCase):

    def test_a_permitted_rule_contributes(self):
        """The companion assertion. Without it the exclusion test passes vacuously.

        Rule 10: a test that something did NOT happen passes whenever the mechanism
        is disconnected, which is precisely the case it exists to catch.
        """
        found = triangles([puzzle(A, B, C)], permitted={(A, B)})
        self.assertEqual(found, [(INDEX[A], INDEX[B], INDEX[C])])

    def test_an_excluded_rule_contributes_NOTHING(self):
        found = triangles([puzzle(A, B, C)], permitted={(B, A)})
        self.assertEqual(found, [])

    def test_exclusion_is_by_the_ORDERED_pair(self):
        """`(A, B)` and `(B, A)` are different rules and must not be conflated.

        Composition is not commutative -- mother-then-father and father-then-mother
        name different people -- so a guard keyed on an unordered pair would leak
        every reversed rule while looking correct.
        """
        self.assertEqual(triangles([puzzle(B, A, C)], permitted={(A, B)}), [])
        self.assertEqual(len(triangles([puzzle(B, A, C)], permitted={(B, A)})), 1)


class TheObjectiveReachesTheVECTORS(unittest.TestCase):

    def test_training_moves_the_relations_it_saw(self):
        """The connection test. Perturb the input, assert the output moves."""
        before = learn([], WIDTH, seed=0, epochs=0, lr=0.0, temperature=0.1)
        after = learn([(INDEX[A], INDEX[B], INDEX[C])], WIDTH, seed=0,
                      epochs=20, lr=0.05, temperature=0.1)
        moved = np.linalg.norm(after[INDEX[A]] - before[INDEX[A]])
        self.assertGreater(moved, 1e-6,
                           "the contrastive update never reached the vector it "
                           "was computed for")

    def test_the_composition_lands_CLOSER_to_its_target(self):
        """The property that makes it that mechanism, not merely a change.

        A rule that moved the vectors without making `compose(a, b)` point at `c`
        would pass the test above and be doing nothing this is for.
        """
        tri = (INDEX[A], INDEX[B], INDEX[C])
        before = learn([], WIDTH, seed=0, epochs=0, lr=0.0, temperature=0.1)
        after = learn([tri], WIDTH, seed=0, epochs=40, lr=0.05, temperature=0.1)
        self.assertGreater(
            float(after[INDEX[C]] @ (after[INDEX[A]] * after[INDEX[B]])),
            float(before[INDEX[C]] @ (before[INDEX[A]] * before[INDEX[B]])),
            "training did not raise the score of the composition on its target")

    def test_more_data_gives_a_different_representation(self):
        """A second connection test, on the argument rather than the mechanism."""
        one = learn([(INDEX[A], INDEX[B], INDEX[C])], WIDTH, 0, 20, 0.05, 0.1)
        two = learn([(INDEX[A], INDEX[B], INDEX[C]),
                     (INDEX[B], INDEX[C], INDEX[A])], WIDTH, 0, 20, 0.05, 0.1)
        self.assertGreater(float(np.linalg.norm(one - two)), 1e-6)

    def test_no_triangles_is_not_a_crash(self):
        """A seed whose permitted set admits nothing must return untrained vectors.

        Reachable whenever a holdout removes every rule a split contains, and
        returning `None` or raising would take out the whole sweep rather than one
        cell.
        """
        vectors = learn([], WIDTH, seed=3, epochs=8, lr=0.05, temperature=0.1)
        self.assertEqual(vectors.shape, (len(RELATIONS), WIDTH))
        self.assertTrue(np.isfinite(vectors).all())

    def test_same_seed_is_identical(self):
        tri = [(INDEX[A], INDEX[B], INDEX[C])]
        self.assertTrue(np.array_equal(
            learn(tri, WIDTH, 1, 5, 0.05, 0.1),
            learn(tri, WIDTH, 1, 5, 0.05, 0.1)))

    def test_vectors_stay_finite_and_normalised(self):
        """Renormalising every step is what keeps the softmax from saturating.

        Without it the vectors grow, the logits blow up, and the run produces nan --
        which reads as divergence rather than as a missing constraint.
        """
        tri = [(INDEX[A], INDEX[B], INDEX[C]), (INDEX[B], INDEX[C], INDEX[A])]
        vectors = learn(tri, WIDTH, 0, 50, 0.2, 0.1)
        self.assertTrue(np.isfinite(vectors).all())
        norms = np.linalg.norm(vectors, axis=1)
        self.assertTrue(np.allclose(norms, 1.0, atol=1e-6))


class TwoTablesAndSampledNegatives(unittest.TestCase):
    """`learn_pairs`, the generalisation `g30-02` needed. The rule must not have moved.

    `learn` delegates here, so every recorded CLUTRR and graph number is now produced by
    this code. These tests exist because that delegation is exactly the kind of change
    that keeps working while quietly computing something else.
    """

    #: `learn` on this input, from the implementation at `0eb75a8` -- BEFORE the
    #: refactor. Generated by executing the old file out of git and comparing, which
    #: caught a real difference: `lr * outer / T` associates differently from
    #: `lr * (outer / T)`, and the first version of the refactor had the second form.
    #: Bit-identical is the bar because anything less cannot distinguish "the rule is
    #: unchanged" from "the rule changed a little".
    GOLDEN_TRIANGLES = [(0, 1, 2), (2, 0, 1), (1, 2, 0), (3, 4, 0)]
    GOLDEN_SUM = -5.369082339701994

    def test_the_delegation_did_not_move_the_rule(self):
        """What breaks if this stops holding: every number `learn` ever produced.

        Read `0.3680` in this module's own docstring -- the graph result -- as the
        thing being protected. A reordering that fed already-updated rows back into
        the two factor gradients would change it silently, which is what
        `the-gradients-are-taken-AFTER-the-bulk-update` mutates.
        """
        vectors = learn(self.GOLDEN_TRIANGLES, 8, 7, 3, 0.1, 0.1, n_relations=5)
        self.assertAlmostEqual(float(vectors.sum()), self.GOLDEN_SUM, places=12)

    def test_a_shared_table_is_literally_one_array(self):
        """`n_right=None` must not quietly make a second table.

        Two arrays would still train and still look sane -- relations composing with
        relations would simply stop sharing a representation, which is the mechanism.
        """
        left, right = learn_pairs(self.GOLDEN_TRIANGLES, 8, 0, 2, 0.1, 0.1,
                                  n_left=5, n_right=None)
        self.assertIs(right, left)

    def test_both_tables_move_and_an_untouched_row_does_not(self):
        """The connection test, with its companion.

        A test that the untouched relation stayed put passes whenever NOTHING moves,
        which is precisely the disconnected case it exists to catch -- so the
        assertions that `a` and `b` DID move are not optional decoration.

        **The property is DIRECTION, not position, and the first version of this test
        asserted the wrong one.** Initial rows are drawn at scale `1/sqrt(width)` and
        the full-contrast path renormalises every row of both tables each step, so an
        untouched row is rescaled to unit length whether or not any gradient reached
        it. Asserting it had not moved failed on the first run -- against correct code
        -- which is the `surprise` calibration's shape exactly: a meaning test that
        passes first time deserves suspicion, and this one did not pass.
        """
        def direction(v):
            return v / np.linalg.norm(v)

        start_left, start_right = learn_pairs([], 8, 0, 0, 0.1, 0.1,
                                              n_left=6, n_right=3)
        left, right = learn_pairs([(2, 1, 4)], 8, 0, 4, 0.1, 0.1,
                                  n_left=6, n_right=3)
        self.assertFalse(np.allclose(direction(left[2]), direction(start_left[2])))
        self.assertFalse(np.allclose(direction(right[1]), direction(start_right[1])))
        self.assertTrue(np.allclose(direction(right[0]), direction(start_right[0])))
        self.assertTrue(np.allclose(direction(right[2]), direction(start_right[2])))

    def test_sampling_touches_only_the_drawn_rows(self):
        """Bounded blast radius, and the companion that something was hit at all.

        With `negatives=K` at most `K + 2` rows can change in one update -- the drawn
        set, the target, and `a`. If sampling were ignored and the full contrast ran,
        every row would move, which is the failure this bounds.
        """
        start, _ = learn_pairs([], 8, 0, 0, 0.1, 0.1, n_left=200, n_right=3)
        left, _ = learn_pairs([(7, 1, 9)], 8, 0, 1, 0.1, 0.1,
                              n_left=200, n_right=3, negatives=5)
        moved = int((~np.isclose(left, start).all(axis=1)).sum())
        self.assertGreater(moved, 0)
        self.assertLessEqual(moved, 5 + 2)

    def test_every_duplicate_negative_contributes(self):
        """Sampling with replacement draws the same row repeatedly; all of it counts.

        What breaks if this stops holding, with the concrete number: under
        `left[rows] -= bulk` only the LAST write for a repeated index survives. At
        `n_left=5` with 40 draws each row is drawn ~8 times, so roughly seven eighths
        of the computed gradient is discarded -- and the rule still runs, still moves
        the vectors, and still looks like it is learning.

        The property is stated as learning rather than as arithmetic: with every
        contribution kept, four rules over a five-symbol alphabet are separable.
        """
        items = [(0, 0, 1), (1, 0, 2), (2, 0, 3), (3, 0, 4)]
        left, right = learn_pairs(items, 16, 0, 60, 0.2, 0.1,
                                  n_left=5, n_right=1, negatives=40)
        hits = sum(int(np.argmax(left @ (left[a] * right[b])) == target)
                   for a, b, target in items)
        self.assertEqual(hits, len(items))


if __name__ == "__main__":
    unittest.main()
