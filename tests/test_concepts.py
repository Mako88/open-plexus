"""A concept must be able to have more than one surface, and cost nothing yet.

Two jobs, and the second is the one that protects every existing number:

- **`Shared` genuinely merges.** Two tokens of one concept must reach the same
  store address, or the indirection buys nothing.
- **`OneConceptPerToken` changes nothing.** Every measurement in this project
  assumed concept id equals token id. If the seam is not exactly that by
  default, it has silently invalidated the comparison set -- decision 74's
  failure, which is why the default not moving is tested rather than intended.
"""

from __future__ import annotations

import unittest

from openplexus.concepts import OneConceptPerToken, Shared, Surfaces


class TheDefaultIsExactlyTodaysBehaviour(unittest.TestCase):

    def test_every_token_is_its_own_concept(self):
        surfaces = OneConceptPerToken(50)
        for token in range(50):
            self.assertEqual(surfaces.of(token), token)

    def test_the_concept_count_is_the_vocabulary(self):
        self.assertEqual(OneConceptPerToken(50).concepts, 50)

    def test_it_satisfies_the_protocol(self):
        self.assertIsInstance(OneConceptPerToken(4), Surfaces)
        self.assertIsInstance(Shared(4), Surfaces)

    def test_SHARED_with_no_groups_is_the_identity_too(self):
        """The control that makes every `Shared` result attributable: with
        nothing merged it must be indistinguishable from the default, so any
        measured difference is the MERGING and not the machinery."""
        plain, shared = OneConceptPerToken(20), Shared(20)
        self.assertEqual([plain.of(t) for t in range(20)],
                         [shared.of(t) for t in range(20)])
        self.assertEqual(plain.concepts, shared.concepts)


class SurfacesOfOneConceptMEET(unittest.TestCase):
    """The property the whole module exists for."""

    def test_grouped_tokens_reach_one_address(self):
        surfaces = Shared(10, [[2, 7], [3, 4, 9]])
        self.assertEqual(surfaces.of(2), surfaces.of(7))
        self.assertEqual(surfaces.of(3), surfaces.of(4))
        self.assertEqual(surfaces.of(4), surfaces.of(9))

    def test_ungrouped_tokens_stay_apart(self):
        surfaces = Shared(10, [[2, 7]])
        self.assertNotEqual(surfaces.of(0), surfaces.of(1))
        self.assertNotEqual(surfaces.of(2), surfaces.of(1))

    def test_merging_SHRINKS_the_concept_space(self):
        """The saving the indirection is for. Ten tokens with two merges is
        eight concepts, so the store is sized by concepts and not by surfaces --
        which is what makes a second modality nearly free rather than a doubling.
        """
        self.assertEqual(Shared(10, [[2, 7], [3, 4]]).concepts, 8)

    def test_concept_ids_are_CONTIGUOUS(self):
        """Without compaction the ids would be the surviving token ids, so the
        store would still be sized by the largest one and the saving above would
        not exist."""
        surfaces = Shared(10, [[2, 7], [3, 4]])
        self.assertEqual(sorted({surfaces.of(t) for t in range(10)}),
                         list(range(surfaces.concepts)))

    def test_the_reverse_direction_is_available(self):
        """A concept has to be emitted somehow, and which surface to use is a
        choice the concept does not itself contain."""
        surfaces = Shared(10, [[2, 7]])
        self.assertEqual(surfaces.surfaces(surfaces.of(2)), [2, 7])


class TwoNodesMustAGREE(unittest.TestCase):
    """The contract the module docstring rests on. A mapping that differed
    between a write and a read would lose the binding with no error anywhere."""

    def test_group_ORDER_does_not_change_the_ids(self):
        """Ids come from the lowest member, not from the order groups arrive.
        Handing them out as groups arrive would make two nodes given the same
        grouping in different orders disagree -- and each would be internally
        consistent, so nothing would look wrong."""
        first = Shared(12, [[2, 7], [3, 4], [8, 11]])
        second = Shared(12, [[8, 11], [3, 4], [2, 7]])
        self.assertEqual([first.of(t) for t in range(12)],
                         [second.of(t) for t in range(12)])

    def test_member_order_within_a_group_does_not_matter_either(self):
        self.assertEqual([Shared(9, [[5, 1, 8]]).of(t) for t in range(9)],
                         [Shared(9, [[8, 5, 1]]).of(t) for t in range(9)])

    def test_the_mapping_is_stable_across_calls(self):
        surfaces = Shared(9, [[1, 5]])
        self.assertEqual([surfaces.of(t) for t in range(9)],
                         [surfaces.of(t) for t in range(9)])


class ImpossibleMappingsAreRefused(unittest.TestCase):

    def test_a_token_in_two_concepts(self):
        """A surface belongs to ONE concept or the mapping is not a function,
        and a read would have no single destination."""
        with self.assertRaises(ValueError):
            Shared(10, [[1, 2], [2, 3]])

    def test_a_token_outside_the_vocabulary(self):
        with self.assertRaises(ValueError):
            Shared(5, [[1, 99]])

    def test_an_empty_vocabulary(self):
        with self.assertRaises(ValueError):
            OneConceptPerToken(0)
        with self.assertRaises(ValueError):
            Shared(0)


if __name__ == "__main__":
    unittest.main()
