"""Grouping content vectors into concepts, and the properties a store needs.

Two of these matter more than the rest, because the rest of the architecture
rests on them rather than on the clustering being good:

- **Agreement.** `concepts.Surfaces.of` must be pure and agreed across nodes, so
  a grouping that varied between two nodes holding the same vectors would send a
  write and a read to different machines with no error anywhere. Determinism is
  therefore a correctness property here, not tidiness.
- **A partition, not a covering.** A token in two groups makes `of` a relation
  rather than a function, and `Shared` refuses it loudly. This checks the input
  never gets that far.

The clustering's *quality* is not tested here and cannot be: whether a grouping
means anything is what the shuffled control in a sweep is for. What is checked is
that structure the test itself planted comes back out, which is the weakest claim
that still bites.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.concepts import Shared
from openplexus.grouping import cluster

WIDTH = 16


def blobs(count: int, per: int, spread: float = 0.05,
          seed: int = 0) -> np.ndarray:
    """`count` well-separated directions, `per` unit vectors near each.

    Rows are ordered blob by blob, so the planted grouping is contiguous and a
    recovered group can be compared against it directly.
    """
    rng = np.random.default_rng(seed)
    centres = rng.normal(0.0, 1.0, (count, WIDTH))
    centres /= np.linalg.norm(centres, axis=1, keepdims=True)
    rows = []
    for centre in centres:
        for _ in range(per):
            point = centre + rng.normal(0.0, spread, WIDTH)
            rows.append(point / np.linalg.norm(point))
    return np.asarray(rows)


class PlantedStructureComesBack(unittest.TestCase):

    def test_separated_blobs_are_recovered_exactly(self):
        groups = cluster(blobs(4, 5), k=4, seed=0)
        self.assertEqual(groups, [[0, 1, 2, 3, 4], [5, 6, 7, 8, 9],
                                  [10, 11, 12, 13, 14], [15, 16, 17, 18, 19]])

    def test_asking_for_fewer_groups_than_blobs_merges_rather_than_drops(self):
        """The address space is what the caller is buying, so a smaller `k` must
        still cover every token. Losing tokens instead would silently shrink the
        vocabulary the store can address."""
        groups = cluster(blobs(4, 5), k=2, seed=0)
        self.assertLessEqual(len(groups), 2)
        self.assertEqual(sorted(t for group in groups for t in group),
                         list(range(20)))


class TheGroupingIsAPartition(unittest.TestCase):

    def test_no_token_appears_twice(self):
        groups = cluster(blobs(5, 4), k=5, seed=1)
        members = [t for group in groups for t in group]
        self.assertEqual(len(members), len(set(members)))

    def test_the_result_is_accepted_by_Shared(self):
        """The consumer is the point. `Shared` raises on a token claimed by two
        concepts, so this is the end-to-end statement of the property above."""
        vectors = blobs(4, 5)
        shared = Shared(len(vectors), cluster(vectors, k=4, seed=0))
        self.assertEqual(shared.concepts, 4)
        self.assertEqual(shared.of(0), shared.of(4))
        self.assertNotEqual(shared.of(0), shared.of(5))

    def test_groups_are_sorted_and_ordered_by_lowest_member(self):
        """`Shared` assigns ids by lowest member so that group ORDER cannot
        matter. This keeps the order canonical anyway, so two nodes comparing
        groupings compare equal lists rather than equal sets of sets."""
        groups = cluster(blobs(3, 6), k=3, seed=2)
        self.assertEqual(groups, sorted(groups, key=lambda g: g[0]))
        for group in groups:
            self.assertEqual(group, sorted(group))


class TokensNeverSeenAreNotAConcept(unittest.TestCase):
    """A zero row means the index never observed that token.

    Grouping them together would build a concept out of ignorance -- every rare
    word merged into one address, which is a real and confident-looking result
    that means nothing. They are left out, and `Shared` gives each its own id.
    """

    def test_zero_rows_join_no_group(self):
        vectors = blobs(3, 4)
        vectors = np.vstack([vectors, np.zeros((2, WIDTH))])
        groups = cluster(vectors, k=3, seed=0)
        self.assertNotIn(12, [t for group in groups for t in group])
        self.assertNotIn(13, [t for group in groups for t in group])

    def test_they_become_concepts_of_their_own(self):
        vectors = np.vstack([blobs(3, 4), np.zeros((2, WIDTH))])
        shared = Shared(len(vectors), cluster(vectors, k=3, seed=0))
        self.assertEqual(shared.concepts, 5)
        self.assertNotEqual(shared.of(12), shared.of(13))

    def test_nothing_observed_at_all_gives_no_groups(self):
        self.assertEqual(cluster(np.zeros((4, WIDTH)), k=2), [])


class TwoNodesAgreeWithoutTalking(unittest.TestCase):
    """The agreement property, which is why `seed` is the only other input."""

    def test_the_same_vectors_give_the_same_grouping(self):
        vectors = blobs(4, 5)
        self.assertEqual(cluster(vectors, k=4, seed=7),
                         cluster(vectors, k=4, seed=7))

    def test_a_copy_of_the_vectors_gives_the_same_grouping(self):
        """Identity must not be doing the work: a node rebuilds its content
        vectors from its own accumulation, so the arrays are never the same
        object and equality of VALUES is what has to be enough."""
        vectors = blobs(4, 5)
        self.assertEqual(cluster(vectors, k=4, seed=7),
                         cluster(vectors.copy(), k=4, seed=7))


class TheDegenerateCasesAreRefused(unittest.TestCase):

    def test_no_groups_is_refused(self):
        with self.assertRaises(ValueError):
            cluster(blobs(2, 2), k=0)

    def test_a_flat_array_is_refused(self):
        """One row per token is the contract, and a `(vocab,)` array would be
        read as one token of `vocab` dimensions -- which clusters happily and
        returns nonsense."""
        with self.assertRaises(ValueError):
            cluster(np.ones(WIDTH), k=2)

    def test_more_groups_than_points_returns_singletons_not_empties(self):
        groups = cluster(blobs(3, 1), k=10, seed=0)
        self.assertEqual(groups, [[0], [1], [2]])


class TheTestBites(unittest.TestCase):
    """Rule 10: a suite that cannot fail proves nothing."""

    def test_structureless_vectors_do_not_recover_the_planted_grouping(self):
        """Uniform random directions have no blobs, so the same call must NOT
        return contiguous groups of five. If it did, every positive result above
        would be the clusterer imposing structure rather than finding it.
        """
        rng = np.random.default_rng(3)
        noise = rng.normal(0.0, 1.0, (20, WIDTH))
        noise /= np.linalg.norm(noise, axis=1, keepdims=True)
        self.assertNotEqual(cluster(noise, k=4, seed=0),
                            [[0, 1, 2, 3, 4], [5, 6, 7, 8, 9],
                             [10, 11, 12, 13, 14], [15, 16, 17, 18, 19]])


if __name__ == "__main__":
    unittest.main()
