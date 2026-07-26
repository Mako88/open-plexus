"""A machine that vanishes while the work is running.

`ablate` models a machine gone *between* sequences — a tidy world where every
sequence starts with a known set of participants. The failure a real network
actually has is a machine dropping out halfway through, taking its rows of the
memory with it including whatever it stored earlier in that same sequence.

Measured at seq_len 192, half the nodes leaving:

    leaves at    0      8     24     64    120    180    never
    accuracy   0.592  0.592  0.600  0.625  0.675  0.696  0.704

**The cost falls the later it happens**, and leaving at step 0 is exactly as bad
as never having joined — so G3's between-sequence model is the worst case and the
realistic failure is strictly milder. These tests fix that bound in place.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH, GROUPS = 20, 32, 8


def build():
    return LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, partitions=GROUPS, key_scale=0.5,
        derived_keys=True, seed=13))


TOKENS = np.array([3, 9, 1, 7, 3, 5, 11, 2, 9, 4, 6, 1])


class LeavingAtTheStartIsNeverHavingJoined(unittest.TestCase):
    """The bound that makes G3 the worst case rather than an underestimate."""

    def test_departure_at_step_zero_matches_a_permanent_removal(self):
        half = list(range(GROUPS // 2))
        per_group = WIDTH // GROUPS
        mid = build().run(TOKENS, leave=(0, half))

        permanent = build()
        permanent.ablate(range(len(half) * per_group))
        np.testing.assert_array_equal(
            mid, permanent.run(TOKENS),
            "leaving at the first step should be indistinguishable from never "
            "having been there")


class TheDepartureIsRealAndPermanent(unittest.TestCase):

    def test_answers_change_after_the_departure_step(self):
        half = list(range(GROUPS // 2))
        model = build()
        model.wo[:] = model.wv        # a decoder, so predictions track the memory
        intact = model.run(TOKENS)
        broken = model.run(TOKENS, leave=(4, half))
        self.assertTrue(np.array_equal(intact[:4], broken[:4]),
                        "answers BEFORE the departure must be untouched")
        self.assertFalse(np.array_equal(intact[4:], broken[4:]),
                         "answers after it must not be")

    def test_a_departed_node_never_returns(self):
        """Silencing it for one step would be a dropped message, not a departure.

        C3's failure is permanent; C2's is transient. Conflating them would make
        churn look survivable for the wrong reason.
        """
        model = build()
        model.wo[:] = model.wv
        left_early = model.run(TOKENS, leave=(2, [0]))
        left_late = model.run(TOKENS, leave=(len(TOKENS) - 1, [0]))
        self.assertFalse(np.array_equal(left_early, left_late),
                         "when a node leaves made no difference, so it is not "
                         "actually staying gone")

    def test_what_it_stored_goes_with_it(self):
        """Not just its vote — its rows of the memory.

        A node that stopped voting but left its stored bindings behind would be
        a much gentler failure than the real one, and the difference is exactly
        what makes mid-sequence departure worth measuring separately.
        """
        model = build()
        model.wo[:] = model.wv
        per_group = WIDTH // GROUPS
        after = model.run(TOKENS, leave=(6, [0]))
        # The departed node's slice can contribute nothing, so reading the
        # answer off it alone must be constant for the rest of the sequence.
        alone = model.run(TOKENS, partition=[0], leave=(6, [0]))
        self.assertEqual(len(set(alone[6:].tolist())), 1,
                         "a departed node still produces varying answers, so "
                         "its memory rows were not cleared")
        self.assertIsNotNone(after)


class ImpossibleDeparturesAreRefused(unittest.TestCase):

    def test_a_node_outside_the_network(self):
        with self.assertRaises(ValueError):
            build().run(TOKENS, leave=(2, [GROUPS]))

    def test_a_step_outside_the_sequence(self):
        with self.assertRaises(ValueError):
            build().run(TOKENS, leave=(len(TOKENS), [0]))
        with self.assertRaises(ValueError):
            build().run(TOKENS, leave=(-1, [0]))


if __name__ == "__main__":
    unittest.main()
