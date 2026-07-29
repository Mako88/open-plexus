"""A hop must be able to follow a NAMED edge, not just the edge that is there.

ARCHITECTURE row D3. Decision 157 measured the gap: with typed WRITES a link no
longer overwrites a fact, and LINKED queries still score 0.1275 against a chance
of 0.125 while the gate correctly defers on 0.9933 of them. **The model knows it
does not know and cannot act on it**, because the hop reads `key(concept)` and
never `key(relation, concept)`.

These tests are deliberately about the MECHANISM rather than a task. They ask one
question -- does a typed hop read the address a typed write wrote -- on a
sequence built so that the two possible answers are different tokens. A task
measurement on top of a mechanism that was never shown to do its one job is how
decision 155 spent a night.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

#: `IS_A subject object` and `HAS_A subject object`, so the SAME subject carries
#: two different edges and the two answers are distinguishable tokens. Untyped,
#: these two writes land at one address and superpose -- decision 155.
IS_A, HAS_A = 0, 1
SUBJECT, THROUGH_IS_A, THROUGH_HAS_A = 2, 3, 4
#: A cue whose ordinary read LANDS ON the subject, so the hop is what has to do
#: the work. The first version of this test put the query directly on
#: `(IS_A, SUBJECT)`, where the ordinary read already answers and `hop_relation`
#: is inert -- both settings returned the same token and the test caught it.
START, POINTER = 5, 6
VOCAB = 8


def model(hop_relation: int) -> LocalAssociativeMemory:
    built = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=64, lr=0.05, key_scale=0.5, decay=1.0,
        context_keys=True, derived_keys=True, hops=2,
        hop_relation=hop_relation, seed=0))
    # The readout is the value matrix, so a retrieval decodes to the token it
    # stored rather than to whatever an untrained readout prefers. Every hop
    # test in this project does this -- see `tests/test_hops.py`.
    built.wo[:] = built.wv
    return built


class ATypedKeySeparatesTwoEdgesAboutOneSubject(unittest.TestCase):
    """The addressing claim, without the model's read path in the way."""

    def test_the_two_edges_get_different_addresses(self):
        # Typed, because an UNTYPED hop with pair keys is refused -- see
        # `ItRefusesWhatCannotWork`. Only the key source is used here.
        source = model(IS_A).key_source
        is_a = source.pair(IS_A, SUBJECT)
        has_a = source.pair(HAS_A, SUBJECT)
        cosine = float(is_a @ has_a
                       / (np.linalg.norm(is_a) * np.linalg.norm(has_a)))
        # Near-orthogonal rather than merely different: these have to not
        # interfere, and note 035 measured interference as O(N * rho) in exactly
        # this quantity.
        self.assertLess(abs(cosine), 0.2)

    def test_the_same_edge_gets_the_same_address(self):
        # Typed, because an UNTYPED hop with pair keys is refused -- see
        # `ItRefusesWhatCannotWork`. Only the key source is used here.
        source = model(IS_A).key_source
        first = source.pair(IS_A, SUBJECT)
        second = source.pair(IS_A, SUBJECT)
        np.testing.assert_allclose(first, second)


class ATypedHopReadsTheAddressATypedWriteWrote(unittest.TestCase):
    """The one job. A hop with `hop_relation` set must follow THAT edge."""

    #: `START POINTER SUBJECT` binds `(START, POINTER) -> SUBJECT`, so the
    #: ordinary read at the repeated cue lands ON the subject and the HOP is
    #: what must follow an edge. Both edges are about SUBJECT, so which one the
    #: hop follows is the whole measurement.
    TOKENS = np.array([START, POINTER, SUBJECT,
                       IS_A, SUBJECT, THROUGH_IS_A,
                       HAS_A, SUBJECT, THROUGH_HAS_A,
                       START, POINTER])

    def retrieved(self, hop_relation: int) -> np.ndarray:
        built = model(hop_relation)
        predictions = built.run(self.TOKENS)
        return predictions

    def test_a_hop_typed_IS_A_reaches_the_IS_A_object(self):
        predictions = self.retrieved(IS_A)
        # The final position keys on (START, POINTER) and retrieves SUBJECT.
        # The hop from there is typed IS_A, so it reads (IS_A, SUBJECT) and the
        # IS_A edge is the one that must come back.
        self.assertEqual(int(predictions[-1]), THROUGH_IS_A)

    def test_a_hop_typed_HAS_A_reaches_the_HAS_A_object(self):
        # THE SAME SEQUENCE, THE SAME POSITION, A DIFFERENT ANSWER. This is the
        # test that would be impossible before typing: untyped, both edges live
        # at `key(SUBJECT)` and the retrieval is their sum, so no setting of
        # anything could return one rather than the other.
        predictions = self.retrieved(HAS_A)
        self.assertEqual(int(predictions[-1]), THROUGH_HAS_A)

    def test_the_two_settings_disagree(self):
        # Stated separately, because both tests above passing for the WRONG
        # reason -- a readout that returns the same token regardless -- would
        # look identical to both passing for the right one.
        self.assertNotEqual(int(self.retrieved(IS_A)[-1]),
                            int(self.retrieved(HAS_A)[-1]))


class ItRefusesWhatCannotWork(unittest.TestCase):

    def test_a_typed_hop_without_pair_keys_is_refused(self):
        # A single key has nowhere to put the relation, so this would silently
        # do nothing -- the class of failure decision 74 is about.
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, hop_relation=IS_A)

    def test_off_by_default(self):
        self.assertEqual(LocalMemoryConfig(vocab_size=VOCAB).hop_relation, -1)


if __name__ == "__main__":
    unittest.main()
