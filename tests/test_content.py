"""Content vectors must carry MEANING, not frequency, and cost what they claim.

P4 of note 045 is the gate the whole addressing line stands on: if co-occurrence
vectors carry no recoverable structure, an index over them cannot help and
nothing downstream is worth building.

**The control is the test, not the measurement.** Any construction over a corpus
produces vectors with non-zero overlaps; the question is whether the overlaps
predict anything a SHUFFLED corpus would not. A version of this file that only
checked "similar words come out similar" would pass on a construction that had
learned word frequency and nothing else.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.content import ContentIndex

#: A corpus with a fact in it: tokens 0-3 appear only beside 10, tokens 4-7 only
#: beside 11. So {0,1,2,3} is one meaning-group and {4,5,6,7} another, and
#: nothing but co-occurrence says so -- the ids interleave with the groups.
GROUPS = ((0, 1, 2, 3), (4, 5, 6, 7))
MARKERS = (10, 11)
VOCAB = 12


def corpus(repeats: int = 40, seed: int = 0) -> np.ndarray:
    rng = np.random.default_rng(seed)
    stream: list[int] = []
    for _ in range(repeats):
        for group, marker in zip(GROUPS, MARKERS):
            for token in rng.permutation(group):
                stream.extend((marker, int(token)))
    return np.array(stream, dtype=np.int64)


def fitted(power: float = 0.0, seed: int = 0, **kw) -> ContentIndex:
    stream = corpus(seed=seed)
    # Count first, then build. `power > 0` refuses to work any other way, and
    # `test_order_of_sequences_does_not_matter_AT_ANY_POWER` is why.
    index = ContentIndex(VOCAB, width=64, seed=seed, power=power,
                         frequency=ContentIndex.count(VOCAB, stream), **kw)
    index.observe(stream)
    return index


class MeaningIsRECOVERED(unittest.TestCase):

    def test_tokens_that_share_company_are_nearest(self):
        """The property. Group membership is expressed ONLY through what each
        token appears beside -- the ids interleave with the groups -- so a
        construction reading ids rather than context would fail here."""
        index = fitted()
        for group in GROUPS:
            for token in group:
                nearest = index.nearest(token, 3)
                self.assertTrue(
                    all(other in group for other, _ in nearest),
                    f"token {token}'s nearest are {nearest}, which crosses the "
                    f"group boundary that co-occurrence defines")

    def test_a_SHUFFLED_corpus_recovers_nothing(self):
        """**The control that makes the test above readable.**

        Shuffling destroys word order and keeps every frequency exactly. If the
        groups still came out, the construction would be reading frequency and
        the result above would mean nothing.
        """
        stream = corpus()
        np.random.default_rng(7).shuffle(stream)
        index = ContentIndex(VOCAB, width=64, seed=0)
        index.observe(stream)
        crossings = sum(
            1 for group in GROUPS for token in group
            for other, _ in index.nearest(token, 3) if other not in group)
        self.assertGreater(
            crossings, 4,
            "a shuffled corpus reproduced the group structure, so the groups "
            "are visible in something other than word order and this suite "
            "is not measuring what it claims")

    def test_an_unobserved_token_is_nobody_s_neighbour(self):
        """Token 9 never appears. A zero vector has cosine 0 with everything,
        which is honest; a normalised random one would be a confident wrong
        answer, and it would be nearest to whatever it happened to point at."""
        index = fitted()
        np.testing.assert_array_equal(index.vectors[9], np.zeros(64))
        for token in GROUPS[0]:
            self.assertNotIn(9, [other for other, _ in index.nearest(token, 3)])


class TheKNOWNWEAKNESSIsReported(unittest.TestCase):
    """`spread` is why `power` is a swept axis rather than a default."""

    def test_content_vectors_overlap_far_more_than_hash_keys(self):
        """Hash keys measure 0.0005 (g10-09). These are built to overlap -- that
        is the point -- and the size of it is what a placement scheme would have
        to cope with, so it is measured rather than assumed."""
        self.assertGreater(abs(fitted().spread()), 0.01)

    def test_weighting_changes_the_answer_it_gives(self):
        """If `power` did nothing, sweeping it would be wasted budget. It does,
        and note 045 measured it helping some queries and destroying others."""
        self.assertNotEqual(
            [t for t, _ in fitted(power=0.0).nearest(0, 6)],
            [t for t, _ in fitted(power=1.0).nearest(0, 6)])


class ItIsLOCALAndPure(unittest.TestCase):
    """C1, checked rather than claimed in a docstring."""

    def test_two_nodes_seeing_the_same_stream_AGREE(self):
        """The property everything distributed rests on. Two nodes must derive
        the same content space from the same observations, or a candidate list
        computed on one is meaningless on another."""
        np.testing.assert_allclose(fitted().vectors, fitted().vectors)

    def test_order_of_sequences_does_not_matter_AT_ANY_POWER(self):
        """Accumulation is a SUM, so nodes may observe in any order -- which is
        what makes this mergeable across nodes without a barrier.

        **AT ANY POWER is the part that bit.** The first version of this test ran
        only at the default `power=0.0`, where every weight is 1 and order
        cannot matter. The implementation weighted context by a RUNNING count,
        so at any other power two nodes seeing the same data in different orders
        produced different spaces -- and a candidate list computed on one would
        have been meaningless on the other.

        A test that exercises only the value where the mechanism is inactive is
        not a test of the mechanism.
        """
        first, second = corpus(seed=1), corpus(seed=2)
        counts = ContentIndex.count(VOCAB, first, second)
        for power in (0.0, 0.5, 1.0):
            with self.subTest(power=power):
                forward = ContentIndex(VOCAB, width=64, power=power,
                                       frequency=counts)
                backward = ContentIndex(VOCAB, width=64, power=power,
                                        frequency=counts)
                forward.observe(first)
                forward.observe(second)
                backward.observe(second)
                backward.observe(first)
                np.testing.assert_allclose(forward.vectors, backward.vectors,
                                           atol=1e-12)

    def test_weighting_by_a_RUNNING_count_is_refused(self):
        """The defect, pinned so it cannot come back as a convenience.

        Building with `power > 0` and no counts is exactly the API that was
        order-dependent, and it now raises rather than silently working.
        """
        with self.assertRaises(ValueError):
            ContentIndex(VOCAB, power=0.5)

    def test_counts_MERGE_across_nodes(self):
        """The property that makes the two-pass version local rather than a
        coordinator: counting is addition, so nodes count what they see and add
        up, with no barrier and no agreement protocol."""
        first, second = corpus(seed=1), corpus(seed=2)
        np.testing.assert_array_equal(
            ContentIndex.count(VOCAB, first) + ContentIndex.count(VOCAB, second),
            ContentIndex.count(VOCAB, first, second))

    def test_similarity_is_SYMMETRIC(self):
        """`a` near `b` must mean `b` near `a`. Crediting only forward
        neighbours would make similarity depend on English word order rather
        than on meaning."""
        vectors = fitted().vectors
        gram = vectors @ vectors.T
        np.testing.assert_allclose(gram, gram.T, atol=1e-12)


class CostIsReported(unittest.TestCase):

    def test_numbers_held_counts_both_tables(self):
        """g10-09 was retracted for comparing a model with extra state against
        one without at equal WIDTH. The context table is state too."""
        self.assertEqual(ContentIndex(50, width=16).numbers_held, 2 * 50 * 16)


class ImpossibleIndexesAreRefused(unittest.TestCase):

    def test_a_window_of_zero(self):
        with self.assertRaises(ValueError):
            ContentIndex(10, window=0)

    def test_a_negative_power(self):
        with self.assertRaises(ValueError):
            ContentIndex(10, power=-1.0)

    def test_asking_for_no_candidates(self):
        with self.assertRaises(ValueError):
            fitted().nearest(0, 0)

    def test_a_token_outside_the_vocabulary(self):
        with self.assertRaises(ValueError):
            ContentIndex(10).observe(np.array([0, 1, 99]))


if __name__ == "__main__":
    unittest.main()
