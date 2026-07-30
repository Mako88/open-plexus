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
from tools.relation_contrastive import INDEX, learn, triangles

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


if __name__ == "__main__":
    unittest.main()
