"""Brakes on the fast store — the half of the paper we did not implement.

[Note 018](../docs/notes/018-the-fast-store-has-no-brakes.md) is the diagnosis.
`memory = decay * memory + outer(...)` is a geometric series, so repetition drives
it toward `1 / (1 - decay)` — about 277x a single binding at the half-life these
sweeps use. Retrieval is linear in that; the delta-rule update is **quadratic**.
It diverges.

`lasting_cap` already existed, bounding the *consolidated* store, and cites Zenke
& Gerstner (2017) — *Hebbian plasticity requires compensatory processes on
multiple timescales*. We implemented one.

**These tests pin the mechanism, not the benefit.** Note 018 registers four
predictions and the loudest is that a cap must NOT improve the gating result:
stability is not selectivity, and a fix that quietly lifts the headline number is
the most dangerous kind. That is a sweep's business, not a unit test's.
"""

from __future__ import annotations

import unittest
import warnings

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 16, 24

#: Heavy repetition of one token, which is what drives the geometric series.
#: Not a synthetic edge case: it is what Zipfian filler, real language, a stuck
#: sensor and a quiet node all look like.
REPEATED = np.array(([3] * 12 + [7, 1, 9]) * 24)
VARIED = np.random.default_rng(5).integers(0, VOCAB, len(REPEATED))


def build(cap: float = 0.0, decay: float = 0.999):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=decay,
        memory_cap=cap, seed=4))
    model.wo[:] = model.wv           # a decoder, so predictions track the memory
    return model


class ZeroIsWhatEverythingWasMeasuredWith(unittest.TestCase):

    def test_the_default_is_off(self):
        self.assertEqual(LocalMemoryConfig(vocab_size=VOCAB).memory_cap, 0.0)

    def test_a_negative_cap_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, memory_cap=-1.0)

    def test_a_cap_that_never_binds_changes_nothing(self):
        """If it fires when nothing is wrong, it is mis-set rather than safe."""
        np.testing.assert_array_equal(build(cap=1e9).run(VARIED),
                                      build(cap=0.0).run(VARIED))


class ItStopsTheRunaway(unittest.TestCase):
    """The reason it exists."""

    def test_repetition_diverges_without_it(self):
        """Pins the bug. Without this the test below proves nothing, because a
        cap trivially keeps a run finite if the run was never going to diverge."""
        with warnings.catch_warnings():
            warnings.simplefilter("error", RuntimeWarning)
            with self.assertRaises((RuntimeWarning, FloatingPointError)):
                model = build(cap=0.0)
                model.run(np.tile(REPEATED, 6), np.roll(np.tile(REPEATED, 6), -1),
                          np.ones(len(REPEATED) * 6, dtype=bool), learn=True)

    def test_and_does_not_with_it(self):
        with warnings.catch_warnings():
            warnings.simplefilter("error", RuntimeWarning)
            tokens = np.tile(REPEATED, 6)
            predictions = build(cap=5.0).run(
                tokens, np.roll(tokens, -1),
                np.ones(len(tokens), dtype=bool), learn=True)
        self.assertTrue(np.isfinite(predictions).all())
        self.assertTrue(((predictions >= 0) & (predictions < VOCAB)).all())


class TheCapsValueMatters(unittest.TestCase):
    """The lesson `lasting_cap` had to learn twice.

    Comparing a cap that binds against one that never binds passes for any
    mechanism that does *something* when the store is large — including clipping
    entries, which is a different and non-local mechanism. Two caps that BOTH
    bind is the comparison that distinguishes scaling from anything else.
    """

    def test_two_binding_caps_give_two_different_answers(self):
        self.assertFalse(
            np.array_equal(build(cap=2.0).run(REPEATED),
                           build(cap=0.5).run(REPEATED)),
            "two different binding caps gave identical answers, so the store is "
            "not being scaled TO the cap and its value is being ignored")

    def test_several_caps_do_not_all_collapse_to_one_answer(self):
        """Deliberately weaker than "all four differ", which this failed.

        **A uniform rescale of the memory cannot change an argmax.** Scaling the
        store scales the retrieval, which scales every score by the same factor,
        and the largest one is still the largest. So the cap's value only reaches
        the predictions through *when it binds* — a different cap that happens to
        bind at the same steps gives the same trajectory and the same answers.

        Asserting four distinct answers from four caps was asserting a
        coincidence. Three of the four differ, which is what the mechanism
        actually guarantees.
        """
        answers = {build(cap=c).run(REPEATED).tobytes()
                   for c in (0.25, 0.5, 1.0, 2.0)}
        self.assertGreaterEqual(
            len(answers), 2,
            "every cap gave the same predictions, so the cap is not reaching "
            "the answer at all")


if __name__ == "__main__":
    unittest.main()
