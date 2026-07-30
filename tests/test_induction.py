"""Tests for the content-addressed lookup.

The lookup is the only thing standing between a substrate at chance and a
substrate that solves the task, so a lookup that quietly read the future would
manufacture the entire result.
"""

from __future__ import annotations

import unittest

from openplexus.models.induction import concatenate, induction_features

VOCAB = 6


def states_for(tokens):
    """Distinguishable dummy states, so a wrong position is visible."""
    return [[float(i), float(t)] for i, t in enumerate(tokens)]


class TestLookupIsCausal(unittest.TestCase):
    def test_never_reads_beyond_the_current_position(self):
        """A lookup at position i must be identical whatever follows it.

        Without this, the feature could return the answer from the future and
        every downstream score would be an artefact of the probe.
        """
        prefix = (1, 2, 3, 1, 4)
        a = (*prefix, 5, 2, 3)
        b = (*prefix, 3, 5, 5)
        fa = induction_features(a, states_for(a), VOCAB, mode="token")
        fb = induction_features(b, states_for(b), VOCAB, mode="token")
        self.assertEqual(fa[: len(prefix)], fb[: len(prefix)])

    def test_first_occurrence_yields_no_evidence(self):
        tokens = (1, 2, 3)
        features = induction_features(tokens, states_for(tokens), VOCAB, mode="token")
        for vector in features:
            self.assertEqual(sum(vector), 0.0)

    def test_reports_what_followed_the_previous_occurrence(self):
        #        pos: 0  1  2  3
        tokens = (1, 5, 2, 1)
        features = induction_features(tokens, states_for(tokens), VOCAB, mode="token")
        # Position 3 repeats token 1, first seen at 0; what followed was 5.
        self.assertEqual(features[3].index(1.0), 5)

    def test_uses_the_most_recent_occurrence_not_the_first(self):
        tokens = (1, 4, 1, 5, 1)
        features = induction_features(tokens, states_for(tokens), VOCAB, mode="token")
        self.assertEqual(features[4].index(1.0), 5)

    def test_state_mode_returns_the_state_at_the_following_position(self):
        tokens = (1, 5, 2, 1)
        states = states_for(tokens)
        features = induction_features(tokens, states, VOCAB, mode="state")
        self.assertEqual(features[3], states[1])


class TestLookupIsConnected(unittest.TestCase):
    def test_the_lookup_depends_on_the_current_token(self):
        """Input-dependence is the whole point. A feature that ignored the
        current token would be a fixed filter, which docs/archive/notes/006 §7 says is
        exactly what does not work."""
        a = (1, 4, 2, 1)
        b = (1, 4, 2, 4)
        fa = induction_features(a, states_for(a), VOCAB, mode="token")
        fb = induction_features(b, states_for(b), VOCAB, mode="token")
        self.assertNotEqual(fa[3], fb[3])

    def test_modes_produce_different_features(self):
        tokens = (1, 5, 2, 1)
        states = states_for(tokens)
        self.assertNotEqual(
            induction_features(tokens, states, VOCAB, mode="token"),
            induction_features(tokens, states, VOCAB, mode="state"),
        )

    def test_rejects_unknown_mode(self):
        with self.assertRaises(ValueError):
            induction_features((1,), [[0.0]], VOCAB, mode="attention")

    def test_rejects_mismatched_lengths(self):
        with self.assertRaises(ValueError):
            induction_features((1, 2), [[0.0]], VOCAB)

    def test_rejects_a_token_outside_the_vocabulary(self):
        """Raised for the same reason Reservoir.run raises: without it an
        out-of-range token surfaces as an IndexError from inside the loop,
        naming neither the token nor the vocabulary. Found by a test that
        supplied token 7 against a vocab of 6."""
        with self.assertRaises(ValueError):
            induction_features((1, 99), states_for((1, 99)), VOCAB)


class TestConcatenate(unittest.TestCase):
    def test_widths_add(self):
        joined = concatenate([[1.0, 2.0]], [[3.0]])
        self.assertEqual(joined, [[1.0, 2.0, 3.0]])

    def test_rejects_mismatched_lengths(self):
        with self.assertRaises(ValueError):
            concatenate([[1.0]], [[1.0], [2.0]])


if __name__ == "__main__":
    unittest.main()
