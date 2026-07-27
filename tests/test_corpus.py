"""The split has to be honest, because a leak here flatters everything.

Three ways a character-level benchmark quietly cheats, all of them producing a
better number rather than an error:

- the same document on both sides of the split
- a vocabulary built from the test text, so symbols the model never had reason
  to learn still occupy indices
- a positional split, which on numbered notes measures drift as much as
  generalisation

Each has a test. The `chunks` boundary conditions are here too because a ragged
final chunk mixes a systematically different sample into every average.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.tasks.corpus import UNKNOWN, _is_test, build, chunks

TEXTS = {f"note-{i}.md": ("the quick brown fox " * 40) + chr(97 + i % 20) * 30
         for i in range(12)}


class TheSplitIsByDocument(unittest.TestCase):

    def setUp(self):
        self.corpus = build(TEXTS, test_share=0.3, min_count=1)

    def test_both_sides_are_non_empty(self):
        self.assertTrue(self.corpus.train)
        self.assertTrue(self.corpus.test)

    def test_a_split_that_empties_one_side_is_refused(self):
        with self.assertRaises(ValueError):
            build({"only.md": "aaaa"}, test_share=0.5, min_count=1)

    def test_the_split_is_deterministic(self):
        again = build(TEXTS, test_share=0.3, min_count=1)
        self.assertEqual([len(d) for d in self.corpus.test],
                         [len(d) for d in again.test])

    def test_the_split_does_not_track_document_ORDER(self):
        """A positional split would put every late note in test.

        The notes are numbered in time and differ in subject and style across
        that range, so a positional split measures drift. This checks the test
        set is not simply a suffix.
        """
        flags = [_is_test(name, 0.3) for name in sorted(TEXTS)]
        self.assertIn(True, flags)
        self.assertIn(False, flags)
        # `sorted` on booleans puts every False first, so a run equal to its own
        # sort is exactly a contiguous tail of test documents.
        self.assertNotEqual(flags, sorted(flags),
                            "test documents form a suffix, so the split is "
                            "positional and measures drift, not generalisation")

    def test_no_document_appears_on_both_sides(self):
        train = {d.tobytes() for d in self.corpus.train}
        for document in self.corpus.test:
            self.assertNotIn(document.tobytes(), train)


class TheVocabularyComesFromTRAININGTextOnly(unittest.TestCase):
    """A symbol seen only in test must not get its own index.

    It would occupy a slot the model never had reason to predict, which is a
    leak in the flattering direction.
    """

    def test_a_test_only_symbol_becomes_unknown(self):
        texts = dict(TEXTS)
        # Force one document into test and give it a symbol nothing else has.
        victim = next(n for n in sorted(texts) if _is_test(n, 0.3))
        texts[victim] = texts[victim] + "ǿ" * 50
        corpus = build(texts, test_share=0.3, min_count=1)
        self.assertNotIn("ǿ", corpus.symbols)

    def test_unknown_occupies_index_zero(self):
        self.assertEqual(build(TEXTS, test_share=0.3, min_count=1).symbols[0],
                         UNKNOWN)

    def test_rare_symbols_are_folded_into_unknown(self):
        """A long tail of symbols seen twice each is vocabulary that cannot be
        learned and inflates `uniform_bits`, flattering every model."""
        texts = {n: t + "Ѐ" for n, t in TEXTS.items()}
        corpus = build(texts, test_share=0.3, min_count=100)
        self.assertNotIn("Ѐ", corpus.symbols)

    def test_a_lower_threshold_keeps_it(self):
        """The guard on the test above: it must be the THRESHOLD doing the
        work, not the symbol being absent."""
        texts = {n: t + "Ѐ" * 5 for n, t in TEXTS.items()}
        corpus = build(texts, test_share=0.3, min_count=2)
        self.assertIn("Ѐ", corpus.symbols)

    def test_every_token_is_inside_the_vocabulary(self):
        corpus = build(TEXTS, test_share=0.3, min_count=1)
        for document in corpus.train + corpus.test:
            self.assertTrue((document >= 0).all())
            self.assertTrue((document < corpus.vocab_size).all())


class Chunking(unittest.TestCase):

    def test_every_chunk_is_exactly_the_requested_length(self):
        pieces = chunks((np.arange(25), np.arange(7)), size=10)
        self.assertTrue(pieces)
        for piece in pieces:
            self.assertEqual(len(piece), 10)

    def test_the_short_remainder_is_dropped(self):
        self.assertEqual(len(chunks((np.arange(25),), size=10)), 2)

    def test_a_document_shorter_than_a_chunk_yields_nothing(self):
        self.assertEqual(chunks((np.arange(5),), size=10), [])

    def test_chunks_do_not_overlap(self):
        pieces = chunks((np.arange(30),), size=10)
        self.assertEqual([p[0] for p in pieces], [0, 10, 20])

    def test_a_chunk_of_one_is_refused(self):
        """One token has no previous token, so nothing can be predicted."""
        with self.assertRaises(ValueError):
            chunks((np.arange(30),), size=1)


class Counts(unittest.TestCase):

    def test_the_token_counts_add_up(self):
        corpus = build(TEXTS, test_share=0.3, min_count=1)
        self.assertEqual(corpus.train_tokens + corpus.test_tokens,
                         sum(len(t) for t in TEXTS.values()))


if __name__ == "__main__":
    unittest.main()
