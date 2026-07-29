"""Addressing the store by concept instead of by surface.

`concepts.Shared` said which surfaces belong together and nothing used it for the
address. `keys.ByConcept` is the piece that does, and it has exactly two things
to prove:

- **It merges.** Two surfaces of one concept must produce the SAME key, or the
  address space has not collapsed and the whole proposal buys nothing.
- **It changes nothing by default.** Wrapped around the identity mapping it must
  reproduce its inner source vector for vector. Every number in this project was
  measured without it, and decision 74 is the entry about a default that moved
  and invalidated a comparison set quietly.

The third property -- that the model still learns through it -- is a measurement,
not a test, and it belongs in a sweep. What is asserted here is that the model
RUNS through it and that the readout still speaks surfaces: store by concept,
emit by word.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.concepts import OneConceptPerToken, Shared
from openplexus.keys import ByConcept, PairKeys, TableKeys
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

WIDTH = 8
VOCAB = 8
#: Tokens 2 and 3 are one concept; 5 and 6 are another. Both groups are away
#: from 0, which stands in for UNKNOWN in the corpus tasks and would confuse a
#: reader of the expectations below.
GROUPS = [[2, 3], [5, 6]]


def pairs() -> PairKeys:
    return PairKeys(seed=1, spread=0.3, width=WIDTH, start=VOCAB)


def wrapped(groups=GROUPS) -> ByConcept:
    return ByConcept(pairs(), Shared(VOCAB, groups), VOCAB)


class SurfacesOfOneConceptShareAnAddress(unittest.TestCase):

    def test_two_surfaces_of_a_concept_give_the_same_key(self):
        source = wrapped()
        np.testing.assert_allclose(
            source.key(np.array([1, 2]), 1),
            source.key(np.array([1, 3]), 1))

    def test_the_CONTEXT_merges_too(self):
        """A pair key is `(t-1, t)`, so collapsing only the current token would
        leave the address space as sparse as it was in the other coordinate --
        which is the whole defect g17-01 measured."""
        source = wrapped()
        np.testing.assert_allclose(
            source.key(np.array([2, 1]), 1),
            source.key(np.array([3, 1]), 1))

    def test_surfaces_of_DIFFERENT_concepts_still_differ(self):
        source = wrapped()
        self.assertFalse(np.allclose(source.key(np.array([1, 2]), 1),
                                     source.key(np.array([1, 5]), 1)))

    def test_a_substituted_candidate_is_mapped_like_any_other_surface(self):
        """The content index proposes WORDS. Asking the store about one has to
        ask about its concept, or a candidate read would reach an address the
        write never used."""
        source = wrapped()
        tokens = np.array([1, 2, 4])
        np.testing.assert_allclose(source.key_as(tokens, 2, 2),
                                   source.key_as(tokens, 2, 3))


class TheDefaultIsUntouched(unittest.TestCase):
    """Decision 74's failure mode, guarded rather than intended."""

    def test_wrapping_the_identity_reproduces_the_inner_keys(self):
        inner, tokens = pairs(), np.array([1, 4, 2, 7, 3])
        outer = ByConcept(inner, OneConceptPerToken(VOCAB), VOCAB)
        for t in range(len(tokens)):
            np.testing.assert_allclose(outer.key(tokens, t),
                                       inner.key(tokens, t))

    def test_it_wraps_a_table_source_as_readily_as_a_pair_source(self):
        """Concept addressing is a wrapper so that it composes with whatever
        replaces the key scheme. A second inner source is how that claim gets
        checked rather than asserted."""
        table = np.random.default_rng(0).normal(0.0, 0.3, (VOCAB, WIDTH))
        source = ByConcept(TableKeys(table), Shared(VOCAB, GROUPS), VOCAB)
        np.testing.assert_allclose(source.key(np.array([2]), 0),
                                   source.key(np.array([3]), 0))

    def test_routing_still_names_a_SURFACE(self):
        """The model composes `key_source.concept` with `model.surfaces` to get
        the routing address. Returning a concept id here would apply the mapping
        twice, which lands on a different node for tokens whose concept id
        happens to be another token's."""
        source = wrapped()
        self.assertEqual(source.concept(np.array([1, 3]), 1), 3)


class TheSequenceIsNotMutated(unittest.TestCase):
    """This is the only key source that rewrites its input before delegating,
    so the shared conformance check earns its place twice over here."""

    def test_the_caller_keeps_its_tokens(self):
        source, tokens = wrapped(), np.array([1, 2, 3, 5, 6])
        original = tokens.copy()
        source.key(tokens, 3)
        source.key_as(tokens, 3, 2)
        source.concept(tokens, 3)
        np.testing.assert_array_equal(tokens, original)


class TheModelRunsThroughIt(unittest.TestCase):
    """End to end, because a key source that satisfies every property above and
    cannot be assigned to a model is of no use to anything."""

    def build(self, groups):
        model = LocalAssociativeMemory(LocalMemoryConfig(
            d_model=WIDTH, vocab_size=VOCAB, seed=0,
            derived_keys=True, context_keys=True))
        surfaces = Shared(VOCAB, groups)
        model.key_source = ByConcept(model.key_source, surfaces, VOCAB)
        model.surfaces = surfaces
        return model

    def test_it_trains_and_predicts_over_the_SURFACE_vocabulary(self):
        model = self.build(GROUPS)
        tokens = np.tile(np.arange(VOCAB), 4).astype(np.int64)
        trace: list[dict] = []
        model.run(tokens, tokens, np.ones(len(tokens), bool), learn=True)
        model.run(tokens, trace=trace)
        # THE ASYMMETRY THE PROPOSAL RESTS ON: fewer addresses, same outputs.
        self.assertEqual(np.shape(trace[1]["scores"]), (VOCAB,))
        self.assertEqual(model.wo.shape[0], VOCAB)

    def test_grouped_context_predicts_the_same_thing_for_both_surfaces(self):
        """The cost of the trade, stated as a test rather than left implicit:
        two words in one concept are indistinguishable AS CONTEXT, so a model
        told `2 -> 7` cannot then tell `2 -> 7` from `3 -> 7`. That is the
        resolution being spent to buy recurrence, and a sweep is what says
        whether the trade pays.
        """
        model = self.build(GROUPS)
        lesson = np.array([2, 7] * 8, dtype=np.int64)
        model.run(lesson, lesson, np.ones(len(lesson), bool), learn=True)
        asked, other = [], []
        model.run(np.array([2, 7]), trace=asked)
        model.run(np.array([3, 7]), trace=other)
        # The trace starts at position 1: position 0 has no previous token, so
        # there is no retrieval there to record.
        np.testing.assert_allclose(asked[0]["scores"], other[0]["scores"])


if __name__ == "__main__":
    unittest.main()
