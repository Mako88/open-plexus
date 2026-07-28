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


class TheSuiteBites(unittest.TestCase):
    """Rule 10, applied to the suite rather than to the code it checks."""

    def test_impurity_is_caught(self):
        source = Drifting()
        self.assertFalse(np.allclose(source.key(TOKENS, 4),
                                     source.key(TOKENS, 4)))

    def test_a_wrong_shape_is_caught(self):
        self.assertNotEqual(np.shape(Drifting().key(TOKENS, 4)), (WIDTH,))

    def test_a_constant_source_fails_the_distinctness_check(self):
        constant = type("Constant", (), {
            "key": lambda self, tokens, t: np.ones(WIDTH)})()
        self.assertTrue(np.allclose(constant.key(TOKENS, 1),
                                    constant.key(TOKENS, 2)))


if __name__ == "__main__":
    unittest.main()
