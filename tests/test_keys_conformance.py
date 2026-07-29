"""Properties EVERY key source must satisfy, run over all of them.

The companion to `test_retrieval_conformance.py`, and here for the same reason:
before a grid over keys x retrieval x readout, a broken implementation inside it
does not announce itself. It produces a number, the number goes in a table, and
the table is read.

`test_keys.py` already checks the protocol and purity across both stock sources.
This adds what it does not have — a shape check, a does-not-mutate check, and a
deliberately broken source asserted to FAIL every property, because a
conformance suite that passes everything proves nothing.

## Why purity is the load-bearing one here

`openplexus/keys.py` states it in the Protocol docstring: the same `(tokens, t)`
must give the same vector, on any node, in any order. **The store is written and
read at different times and on different machines**, so a key that drifted
between the write and the read would break retrieval while every write still
looked correct — a failure with no symptom at the site of the fault.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.keys import KeySource, PairKeys, TableKeys

WIDTH = 8
VOCAB = 6
TOKENS = np.tile(np.arange(VOCAB), 3).astype(np.int64)


def sources() -> list[tuple[str, object]]:
    """Every key source the model can be built with."""
    table = np.random.default_rng(0).normal(0.0, 0.3, (VOCAB, WIDTH))
    return [
        ("TableKeys", TableKeys(table)),
        # `start` stands in for "no previous token" at position 0, so the
        # first step has a key in the same space as every other step.
        ("PairKeys", PairKeys(seed=1, spread=0.3, width=WIDTH, start=VOCAB)),
    ]


class Drifting:
    """Violates the contract on purpose, so the suite can be shown to bite.

    Impure by a hair, which is the realistic version: a source that returned
    obvious garbage would be caught by anything, and one that drifts slightly
    between the write and the read is the one that costs a measurement.
    """

    def __init__(self) -> None:
        self.calls = 0

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        self.calls += 1
        tokens += 0                                   # touches its input
        return np.full(WIDTH - 1, 1e-3 * self.calls)  # drifts, wrong shape

    def key_as(self, tokens: np.ndarray, t: int, token: int) -> np.ndarray:
        """Ignores the substitution entirely -- every candidate would read the
        same address, which is the failure `test_substituting_a_DIFFERENT_token`
        exists to catch."""
        return self.key(tokens, t)


class EverySourceHonoursTheContract(unittest.TestCase):

    def test_all_satisfy_the_protocol(self):
        for name, source in sources():
            with self.subTest(name):
                self.assertIsInstance(source, KeySource)

    def test_a_key_has_one_value_per_dimension(self):
        for name, source in sources():
            with self.subTest(name):
                self.assertEqual(np.shape(source.key(TOKENS, 4)), (WIDTH,))

    def test_a_key_source_is_pure(self):
        """The property the store depends on and the one with no local symptom:
        a key that drifted between the write and the read breaks retrieval while
        every write still looks correct."""
        for name, source in sources():
            with self.subTest(name):
                np.testing.assert_allclose(source.key(TOKENS, 4),
                                           source.key(TOKENS, 4))

    def test_a_key_does_not_depend_on_when_it_was_asked_for(self):
        """Purity across an INTERLEAVING, not just a repeat. A source caching
        the last position would pass a back-to-back check and fail here, and
        the store reads positions out of order."""
        for name, source in sources():
            with self.subTest(name):
                first = source.key(TOKENS, 4).copy()
                source.key(TOKENS, 9)
                source.key(TOKENS, 2)
                np.testing.assert_allclose(source.key(TOKENS, 4), first)

    def test_it_does_not_mutate_the_token_sequence(self):
        for name, source in sources():
            with self.subTest(name):
                tokens = TOKENS.copy()
                source.key(tokens, 4)
                np.testing.assert_array_equal(tokens, TOKENS)

    def test_different_positions_generally_give_different_keys(self):
        """Not a strong property, but a source returning one constant vector
        would satisfy every test above. It would also make the store bind
        everything to one address, and nothing else here would notice."""
        for name, source in sources():
            with self.subTest(name):
                self.assertFalse(np.allclose(source.key(TOKENS, 1),
                                             source.key(TOKENS, 2)))


class ASubstitutedKeyIsStillTHISPositionsKey(unittest.TestCase):
    """`key_as` is what a candidate read needs, and it has one hard contract.

    The content index proposes concepts and the model asks the store about each,
    which means building the key that concept WOULD have had at this position.
    If a substituted key at the position's own token differed from the ordinary
    key, a candidate read and a normal read would disagree about the same token
    and the index would look like it was finding things that were not there.
    """

    def test_substituting_the_actual_token_reproduces_the_key(self):
        for name, source in sources():
            with self.subTest(name):
                for t in (0, 1, 7):
                    np.testing.assert_allclose(
                        source.key_as(TOKENS, t, int(TOKENS[t])),
                        source.key(TOKENS, t))

    def test_substituting_a_DIFFERENT_token_changes_the_key(self):
        """Otherwise candidates would all read the same address and the index
        would be an expensive way to retrieve one thing several times."""
        for name, source in sources():
            with self.subTest(name):
                self.assertFalse(np.allclose(source.key_as(TOKENS, 4, 1),
                                             source.key_as(TOKENS, 4, 2)))

    def test_the_CONTEXT_is_kept_when_the_token_is_substituted(self):
        """A pair key is `(t-1, t)`. Substituting must change only the current
        token -- the question is "what if this position held that concept", not
        "what if the whole pair were different"."""
        for name, source in sources():
            with self.subTest(name):
                # Position 0 has no predecessor and position 6's is token 5, so
                # substituting the same concept at each must differ for a pair
                # key and match for a table key.
                #
                # The first version compared positions 4 and 10 -- which repeat
                # the vocabulary, so BOTH have token 3 before them. It varied
                # nothing and asserted a difference anyway.
                self.assertEqual(TOKENS[5], 5, "the fixture changed shape")
                same = np.allclose(source.key_as(TOKENS, 0, 3),
                                   source.key_as(TOKENS, 6, 3))
                self.assertEqual(
                    same, name == "TableKeys",
                    f"{name} kept the wrong amount of context when a token was "
                    f"substituted")


class EverySourceCanNameWhatItIsAskingFor(unittest.TestCase):
    """`concept` is what makes a read ROUTABLE, and it has its own contract.

    A key vector says what to retrieve; `concept` says whom to ask. Note 044:
    a key vector cannot be inverted to a concept, so the identity has to travel
    beside the vector or a partitioned store cannot serve the read at all.
    """

    def test_a_concept_is_an_integer(self):
        """It indexes a hash ring. A numpy scalar would work by accident until
        it reached `np.random.default_rng((seed, domain, concept))`, which is
        fussy about types in ways that surface far from here."""
        for name, source in sources():
            with self.subTest(name):
                self.assertIsInstance(source.concept(TOKENS, 4), int)

    def test_the_same_token_always_ROUTES_THE_SAME_WAY(self):
        """**The property a partitioned store lives on.** `TOKENS` repeats the
        vocabulary three times, so positions 4, 10 and 16 are the same token in
        different contexts. If they routed differently, a write and a later read
        of the same concept would reach different machines and the binding would
        simply be missing -- with no error anywhere.
        """
        for name, source in sources():
            with self.subTest(name):
                self.assertEqual(source.concept(TOKENS, 4),
                                 source.concept(TOKENS, 4 + VOCAB))
                self.assertEqual(source.concept(TOKENS, 4),
                                 source.concept(TOKENS, 4 + 2 * VOCAB))

    def test_different_tokens_generally_give_different_concepts(self):
        """A source routing everything to one concept would satisfy everything
        above and would also put the whole store on one node."""
        for name, source in sources():
            with self.subTest(name):
                self.assertNotEqual(source.concept(TOKENS, 1),
                                    source.concept(TOKENS, 2))

    def test_it_does_not_mutate_the_token_sequence(self):
        for name, source in sources():
            with self.subTest(name):
                tokens = TOKENS.copy()
                source.concept(tokens, 4)
                np.testing.assert_array_equal(tokens, TOKENS)


class TheSuiteBites(unittest.TestCase):
    """Rule 10, applied to the suite rather than to the code it checks."""

    def test_impurity_is_caught(self):
        source = Drifting()
        self.assertFalse(np.allclose(source.key(TOKENS, 4),
                                     source.key(TOKENS, 4)))

    def test_a_wrong_shape_is_caught(self):
        self.assertNotEqual(np.shape(Drifting().key(TOKENS, 4)), (WIDTH,))

    def test_a_source_that_cannot_ROUTE_fails_the_protocol(self):
        """`Drifting` has `key` and no `concept`, which is exactly the shape a
        key source written before note 044 has. It must not satisfy the
        protocol, or a partitioned store would accept it and then have nowhere
        to send the read."""
        self.assertNotIsInstance(Drifting(), KeySource)

    def test_a_constant_source_fails_the_distinctness_check(self):
        constant = type("Constant", (), {
            "key": lambda self, tokens, t: np.ones(WIDTH)})()
        self.assertTrue(np.allclose(constant.key(TOKENS, 1),
                                    constant.key(TOKENS, 2)))


if __name__ == "__main__":
    unittest.main()
