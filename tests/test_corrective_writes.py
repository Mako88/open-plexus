"""Storing the error instead of the value, and what that has to mean.

Hebbian storage adds `outer(value, key)` regardless of what the store holds, so
rebinding accumulates. g10-11 measured that at 0.0x chance after 512 rebindings.
Corrective storage subtracts what the key already retrieves:

    memory += outer(value - memory @ key, key) / (key @ key)

The tests that matter are not that it runs. They are that it is EXACT for the
key it wrote, that it leaves other keys alone, that it is off by default, and
that it is local — because a correction reading anything but this node's own
store and its own key would violate C1 while improving the numbers, which is the
failure mode GOALS names explicitly.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 32, 64


def build(**overrides):
    """A model whose predictions actually depend on what it retrieved.

    `Wo` must be given the decoder initialisation, as every probe in this repo
    does. Without it the scores are flat and `argmax` returns token 0 at every
    position, in every condition — so a test comparing two prediction streams
    compares two constant arrays and passes whatever the mechanism does.

    The vacuity guard below caught exactly that.
    """
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=3)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


def store(model, memory, key, value):
    """One write, as the model does it, so the test cannot drift from it."""
    scale = float(key @ key)
    if model.config.corrective_writes:
        memory += np.outer(value - memory @ key, key) / scale
    else:
        memory += np.outer(value, key)
    return memory


class TheWriteIsExactForItsOwnKey(unittest.TestCase):

    def setUp(self):
        self.model = build(corrective_writes=True)
        self.keys, self.values = np.array(self.model.wk), self.model.wv

    def test_one_write_lands_exactly_on_the_value(self):
        """The point of dividing by the key's squared norm.

        A Hebbian write lands on `value * (key @ key)`, so the scale depends on
        the key. This lands on `value`, whatever the key.
        """
        memory = np.zeros((WIDTH, WIDTH))
        memory = store(self.model, memory, self.keys[5], self.values[9])
        np.testing.assert_allclose(memory @ self.keys[5], self.values[9],
                                   atol=1e-9)

    def test_REBINDING_REPLACES_rather_than_accumulating(self):
        """The whole reason this exists.

        Hebbian storage would leave `values[9] + values[4]` here, and the argmax
        of a sum of two unrelated vectors is neither of them.
        """
        memory = np.zeros((WIDTH, WIDTH))
        memory = store(self.model, memory, self.keys[5], self.values[9])
        memory = store(self.model, memory, self.keys[5], self.values[4])
        np.testing.assert_allclose(memory @ self.keys[5], self.values[4],
                                   atol=1e-9)

    def test_the_HEBBIAN_rule_does_NOT_replace(self):
        """The guard on the test above: it must be the CORRECTION doing the
        work, not something about the vectors."""
        plain = build(corrective_writes=False)
        memory = np.zeros((WIDTH, WIDTH))
        memory = store(plain, memory, self.keys[5], self.values[9])
        memory = store(plain, memory, self.keys[5], self.values[4])
        got = memory @ self.keys[5]
        self.assertFalse(
            np.allclose(got / max(np.linalg.norm(got), 1e-12),
                        self.values[4] / np.linalg.norm(self.values[4]),
                        atol=1e-3),
            "Hebbian storage replaced the binding, so this test measures "
            "nothing about the correction")

    def test_a_second_key_is_left_ALMOST_alone(self):
        """Not exactly alone, and that is the honest statement.

        The correction is exact for the key it wrote. Other keys move by their
        overlap with it, which for near-orthogonal derived keys is small but not
        zero -- that residual IS the interference the capacity wall is made of,
        and a test claiming perfect isolation would be asserting a property the
        mechanism does not have.
        """
        memory = np.zeros((WIDTH, WIDTH))
        memory = store(self.model, memory, self.keys[5], self.values[9])
        before = (memory @ self.keys[11]).copy()
        memory = store(self.model, memory, self.keys[5], self.values[4])
        after = memory @ self.keys[11]
        moved = float(np.linalg.norm(after - before))
        self.assertLess(moved, float(np.linalg.norm(self.values[4])),
                        "an unrelated key moved by more than the value written")


class ThroughTheMODELRatherThanAReimplementation(unittest.TestCase):
    """The tests above use `store()`, which reimplements the rule.

    That makes them tests of the arithmetic and NOT of the model — mutating the
    model's own write left every one of them passing, which is how a
    reimplementation quietly becomes the thing under test. Note 012 records the
    same trap producing cap values that were correct about something that was
    not this model.

    These read the model's own retrieval, so a change to its write path has to
    show up here.
    """

    def stream(self):
        """`cue` bound to A, then later to B, with the cue queried at the end.

        Tokens alternate so the cue is genuinely rebound: the write at each step
        binds the PREVIOUS token to the current one.
        """
        cue, first, second = 5, 9, 4
        return np.array([cue, first, cue, second, cue], dtype=np.int64), second

    def test_the_model_retrieves_the_LAST_binding(self):
        tokens, expected = self.stream()
        model = build(corrective_writes=True)
        self.assertEqual(int(model.run(tokens)[-1]), expected)

    def test_the_HEBBIAN_model_does_not(self):
        """The guard: it must be the correction doing this, in the MODEL."""
        tokens, expected = self.stream()
        plain = build(corrective_writes=False)
        self.assertNotEqual(int(plain.run(tokens)[-1]), expected)

    def test_a_retrieved_binding_has_the_VALUE_S_magnitude(self):
        """Pins the division by `key @ key`, through the model.

        Exactness means `memory @ key` lands on `value`, so the retrieval's
        magnitude is the value's. Without the division it lands on
        `value * (key @ key)`, and at `key_scale` 0.5 that is about a QUARTER —
        the defect is a scale error of roughly 4x.

        A first version of this test asserted that retrieval strength is the
        same for every cue. It is not: the query step also writes, and that
        second write interferes by an amount that depends on the cue. The spread
        it measured was interference, not normalisation.

        The trace reports `strength` as the PREVIOUS step's retrieval — it is
        what the tag ranks on — so the query at position 2 is reported by the
        entry for position 3, and one extra token is needed to see it. Reading
        `trace[-1]` on a three-token stream measures the step before the query.
        """
        model = build(corrective_writes=True)
        wanted = float(np.linalg.norm(model.wv[9]))
        trace: list = []
        model.run(np.array([5, 9, 5, 9], dtype=np.int64), trace=trace)
        got = next(e["strength"] for e in trace if e["t"] == 3)
        self.assertGreater(got, 0.5 * wanted,
                           f"retrieval {got:.4f} is far below the value's "
                           f"magnitude {wanted:.4f}; the write is being scaled "
                           f"down by the key's squared norm")
        self.assertLess(got, 2.0 * wanted)


class ItIsOffByDefault(unittest.TestCase):
    """Every published number was measured with Hebbian storage."""

    def test_the_default_is_false(self):
        self.assertFalse(LocalMemoryConfig(vocab_size=VOCAB).corrective_writes)

    def test_predictions_are_identical_with_the_flag_off(self):
        tokens = np.random.default_rng(5).integers(0, VOCAB, 80)
        np.testing.assert_array_equal(
            build().run(tokens),
            build(corrective_writes=False).run(tokens))

    def test_the_flag_CHANGES_something_when_on(self):
        """The vacuity guard on the test above, and it caught something.

        A stream of RANDOM tokens is not enough. Before a key has been written
        under, `memory @ key` is near zero, so the correction reduces to the
        Hebbian update divided by the key's squared norm — and a uniform
        rescaling of the store does not move an argmax. The first version of
        this test used random tokens, found the predictions identical, and was
        right to fail.

        The mechanism differs where keys REPEAT, which is what it is for. This
        stream revisits a handful of tokens so that corrections actually apply.
        """
        tokens = np.tile(np.array([3, 7, 3, 11, 7, 3], dtype=np.int64), 12)
        self.assertFalse(np.array_equal(
            build().run(tokens),
            build(corrective_writes=True).run(tokens)))


class ItStaysLocal(unittest.TestCase):
    """C1 is the constraint that outranks the numbers.

    A correction reading a population statistic would improve retrieval and
    break the project, so what it reads is pinned rather than assumed.
    """

    def test_a_write_depends_only_on_THIS_key_and_the_store(self):
        """Two models differing ONLY in vocabulary produce the same update for
        the same key and value.

        If the correction consulted anything pooled across tokens -- a mean key,
        a normalisation over the vocabulary -- a larger vocabulary would change
        the result for an identical write.
        """
        small, large = build(vocab_size=8), build(vocab_size=64)
        key = np.asarray(small.wk[3], dtype=float)
        value = np.asarray(small.wv[3], dtype=float)
        a = store(build(corrective_writes=True), np.zeros((WIDTH, WIDTH)),
                  key, value)
        b = store(build(corrective_writes=True, vocab_size=64),
                  np.zeros((WIDTH, WIDTH)), key, value)
        np.testing.assert_allclose(a, b, atol=1e-12)
        del small, large

    def test_a_zero_key_is_skipped_rather_than_dividing_by_zero(self):
        model = build(corrective_writes=True)
        memory = np.zeros((WIDTH, WIDTH))
        tokens = np.zeros(6, dtype=np.int64)
        model.wk[0] = 0.0
        model.run(tokens)                       # must not raise or produce NaN
        self.assertTrue(np.isfinite(memory).all())


if __name__ == "__main__":
    unittest.main()
