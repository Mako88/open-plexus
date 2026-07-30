"""Merging concepts without moving any address.

`concepts.Merged` is the acquisition step's other half: `Shared` expresses a decision
that two surfaces are one concept, and this expresses one made LATER, after bindings
already exist under both ids.

The obvious implementation — remap the loser's surfaces onto the winner — strands every
binding it was meant to preserve, because `keys.ByConcept` builds the key from the concept
id. That failure is silent: the facts are not corrupted, they are unreachable, and nothing
raises. **So the test that matters most here is that keys do not move.**

The rest is the distributed contract. `Surfaces.of` promises *"the same token maps to the
same concept on every node forever"*, and union-by-rank would break it — the
representative would depend on which order merges arrived in, so two nodes learning the
same merges out of order would disagree. Union by minimum id makes the answer a property
of the merge SET, which is what lets propagation be lazy instead of coordinated.
"""

from __future__ import annotations

import itertools
import unittest

import numpy as np

from openplexus.concepts import Merged, OneConceptPerToken, Shared
from openplexus.keys import ByConcept, PairKeys

VOCAB = 12
WIDTH = 16


def merged(*pairs) -> Merged:
    surfaces = Merged(OneConceptPerToken(VOCAB))
    for one, other in pairs:
        surfaces.merge(one, other)
    return surfaces


class AddressesDoNotMove(unittest.TestCase):
    """The property the design exists for, and the one whose failure is silent."""

    def test_of_is_unchanged_by_any_merge(self):
        surfaces = merged((5, 2), (7, 5), (9, 3))
        for token in range(VOCAB):
            self.assertEqual(surfaces.of(token), token,
                             "a merge that moves `of` moves every key built "
                             "from it, and strands the bindings already there")

    def test_the_KEY_for_a_surface_survives_a_merge(self):
        """Asserted through the real key source, not by reasoning about it.

        `ByConcept` hands concept ids to its inner source, so this is the
        composition that would break if `of` moved.
        """
        before = ByConcept(PairKeys(seed=1, spread=0.3, width=WIDTH,
                                    start=VOCAB),
                           OneConceptPerToken(VOCAB), VOCAB)
        surfaces = merged((5, 2), (7, 5))
        after = ByConcept(PairKeys(seed=1, spread=0.3, width=WIDTH,
                                  start=VOCAB), surfaces, VOCAB)
        tokens = np.array([2, 5, 7, 3])
        for t in range(len(tokens)):
            np.testing.assert_allclose(before.key(tokens, t),
                                       after.key(tokens, t))

    def test_the_concept_count_does_not_shrink(self):
        """Every merged id is still a live address being written to.

        Shrinking the count would under-size a store that still receives writes at
        every member of the class.
        """
        self.assertEqual(merged((5, 2), (7, 5)).concepts, VOCAB)

    def test_it_wraps_a_non_identity_mapping_too(self):
        """`Merged` composes with `Shared` rather than replacing it: one expresses
        a decision known up front, the other one made later."""
        surfaces = Merged(Shared(VOCAB, [[2, 3], [5, 6]]))
        merged_ids = surfaces.of(3)
        self.assertEqual(surfaces.of(2), merged_ids)
        surfaces.merge(surfaces.of(2), surfaces.of(5))
        self.assertEqual(surfaces.of(3), merged_ids,
                         "merging must not disturb the inner grouping")


class TheRepresentativeIsOrderIndependent(unittest.TestCase):
    """The distributed requirement. Union-by-rank would fail every case here."""

    #: **Mixed argument order on purpose.** The first version of this fixture wrote
    #: every pair larger-first, which makes union-by-min and union-by-arrival agree
    #: by accident — and a mutation swapping them survived. Ascending pairs are
    #: what separate the two rules.
    PAIRS = ((5, 2), (5, 7), (9, 7))

    def test_the_ARGUMENT_order_does_not_decide_the_representative(self):
        """The sharpest case, and the one the vacuous fixture missed.

        `merge(2, 5)` and `merge(5, 2)` state the same fact. A rule that points the
        first argument's representative at the second's would answer 5 for one and 2
        for the other, so two nodes told the same thing in different words would
        route the same concept to different machines.
        """
        self.assertEqual(merged((2, 5)).representative(2), 2)
        self.assertEqual(merged((5, 2)).representative(2), 2)
        self.assertEqual(merged((2, 5)).aliases(5), merged((5, 2)).aliases(5))

    def test_ascending_chains_agree_with_descending_ones(self):
        """Same class, built from the bottom up and the top down."""
        self.assertEqual(merged((2, 5), (5, 7), (7, 9)).aliases(9),
                         merged((9, 7), (7, 5), (5, 2)).aliases(9))

    def test_every_arrival_order_gives_the_same_classes(self):
        expected = None
        for order in itertools.permutations(self.PAIRS):
            surfaces = merged(*order)
            classes = tuple(surfaces.aliases(c) for c in range(VOCAB))
            if expected is None:
                expected = classes
            self.assertEqual(classes, expected, f"order {order} disagreed")

    def test_every_arrival_order_gives_the_same_representative(self):
        for order in itertools.permutations(self.PAIRS):
            self.assertEqual(merged(*order).representative(9), 2)

    def test_the_representative_is_the_smallest_member(self):
        surfaces = merged((5, 2), (7, 5), (9, 7))
        for member in (2, 5, 7, 9):
            self.assertEqual(surfaces.representative(member), 2)

    def test_the_merge_SET_is_what_nodes_would_compare(self):
        """Derived state depends on nothing but this, so two nodes can check
        agreement by comparing merges rather than internal parents."""
        one = merged((5, 2), (7, 5))
        other = merged((7, 5), (5, 2))
        self.assertEqual(one.merges, other.merges)


class Classes(unittest.TestCase):

    def test_merging_is_transitive(self):
        self.assertEqual(merged((5, 2), (7, 5)).aliases(7), (2, 5, 7))

    def test_an_unmerged_concept_is_its_own_class(self):
        self.assertEqual(merged((5, 2)).aliases(3), (3,))

    def test_two_disjoint_classes_stay_disjoint(self):
        surfaces = merged((5, 2), (9, 3))
        self.assertEqual(surfaces.aliases(5), (2, 5))
        self.assertEqual(surfaces.aliases(9), (3, 9))

    def test_merging_is_idempotent(self):
        surfaces = merged((5, 2), (5, 2), (2, 5))
        self.assertEqual(surfaces.aliases(5), (2, 5))
        self.assertEqual(len(surfaces.merges), 1)

    def test_merging_a_concept_with_itself_does_nothing(self):
        surfaces = merged((5, 5))
        self.assertEqual(surfaces.aliases(5), (5,))
        self.assertEqual(surfaces.merges, frozenset())

    def test_aliases_are_sorted(self):
        """Two nodes summing a class's reads in different orders would get
        different floating-point totals for the same question."""
        surfaces = merged((9, 2), (5, 9), (7, 5))
        self.assertEqual(surfaces.aliases(7), (2, 5, 7, 9))

    def test_a_class_is_the_same_seen_from_any_member(self):
        surfaces = merged((5, 2), (7, 5), (9, 7))
        expected = (2, 5, 7, 9)
        for member in expected:
            self.assertEqual(surfaces.aliases(member), expected)


class ALateMergeIsAMissAndNotACorruption(unittest.TestCase):
    """The property that lets propagation be lazy, which is why this beats
    re-keying: re-keying would need every node to agree before any write."""

    def test_a_node_missing_a_merge_reads_a_SUBSET(self):
        behind = merged((5, 2))
        ahead = merged((5, 2), (7, 5))
        self.assertEqual(behind.aliases(5), (2, 5))
        self.assertEqual(ahead.aliases(5), (2, 5, 7))
        self.assertTrue(set(behind.aliases(5)) <= set(ahead.aliases(5)),
                        "a behind node must miss facts, never read wrong ones")

    def test_catching_up_needs_no_migration(self):
        behind = merged((5, 2))
        behind.merge(7, 5)
        self.assertEqual(behind.aliases(5), merged((5, 2), (7, 5)).aliases(5))


if __name__ == "__main__":
    unittest.main()
