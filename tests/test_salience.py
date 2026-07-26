"""Storage triggered by salience, and the process that keeps it from exploding.

A brain floods with neuromodulator on external events, more strongly for outcomes
that are very good and very bad. Our first gate fired on every *correct*
prediction instead — most steps, once the model works — and
[g7-04](../experiments/sweeps/g7-04-when-does-forgetting-pay.txt) measured that
as monotonically harmful.

`salience` fires on the tails instead: only when a step's surprise departs from
this node's own running experience by more than a set number of deviations, **in
either direction**.

[Note 013](../docs/notes/013-salience-and-the-missing-body.md) records what
happened when it was measured: it works, it needs a compensatory process to avoid
diverging, and on this benchmark it promotes filler exclusively — 44 promotions,
44 from filler, none from a pair. These tests fix the mechanism so that finding is
about the benchmark rather than about a broken implementation.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 16, 24

# A REPEATING fixture cannot exercise this mechanism, which the first version of
# these tests learned the hard way. The gate fires when a step's surprise departs
# from the running average by more than a set number of deviations -- and a
# periodic sequence produces near-identical surprise at every step, so the
# deviation collapses toward zero and nothing ever clears the bar. The cap then
# appeared not to bind, because there was nothing to bind on.
#
# Varied input is therefore part of the fixture, not an incidental choice.
TOKENS = np.random.default_rng(3).integers(0, VOCAB, 120)


def _motif_in_noise(seed=1, motif_len=2, repeats=40, gap=1):
    """Noise with a short motif repeated through it.

    The motif is learned within a few repeats and then becomes far MORE
    predictable than its surroundings, while the noise between repeats stays
    unpredictable. So this fixture's surprise outliers sit on the LOW side,
    which is the half of the two-tailed rule that random tokens never exercise:
    random input is occasionally very surprising and never unusually
    predictable.
    """
    rng = np.random.default_rng(seed)
    motif = rng.integers(0, VOCAB, motif_len)
    out = []
    for _ in range(repeats):
        out.extend(motif.tolist())
        out.extend(rng.integers(0, VOCAB, gap).tolist())
    return np.array(out)


PREDICTABLE = _motif_in_noise()


def build(salience: float = 0.0, cap: float = 0.0, consolidation: float = 1.0):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=0.9,
        consolidation=consolidation, salience=salience, lasting_cap=cap,
        seed=4))
    model.wo[:] = model.wv           # a decoder, so predictions track the memory
    return model


class ZeroIsTheOriginalGate(unittest.TestCase):
    """Every earlier consolidation result was measured without salience."""

    def test_the_default_is_off(self):
        config = LocalMemoryConfig(vocab_size=VOCAB)
        self.assertEqual(config.salience, 0.0)
        self.assertEqual(config.lasting_cap, 0.0)

    def test_salience_zero_reproduces_consolidate_on_correct(self):
        plain = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=0.9,
            consolidation=1.0, seed=4))
        plain.wo[:] = plain.wv
        np.testing.assert_array_equal(build(0.0).run(TOKENS), plain.run(TOKENS))


class ItRefusesToRunWithoutACompensatoryProcess(unittest.TestCase):
    """Measured divergence, not a precaution.

    Consolidating on correctness is self-limiting -- being correct means the
    retrieval was already good. Consolidating on surprise is positive feedback: a
    large surprise promotes a large retrieval, which enlarges the store, which
    enlarges later surprises. Unbounded, it reached NaN.
    """

    def test_salience_without_a_cap_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, decay=0.9,
                              consolidation=1.0, salience=2.0)

    def test_salience_without_consolidation_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, decay=0.9,
                              salience=2.0, lasting_cap=1.0)

    def test_a_negative_cap_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, decay=0.9,
                              lasting_cap=-1.0)


class TheGateIsTwoTailed(unittest.TestCase):
    """Very good and very bad both count. One tail would be a different idea."""

    def test_it_fires_on_steps_that_are_unusually_PREDICTABLE(self):
        """The half of the rule a random fixture cannot reach.

        This replaces a test that recomputed `abs(s - mean) > k * sd` on a made-up
        list of scores. That version passed whatever the model did, because it
        never consulted the model -- it tested arithmetic, and the mutation
        harness caught it by flipping the real rule to one tail and watching
        every salience test still pass.

        On `PREDICTABLE` the outliers are all on the low-surprise side, so a
        one-tailed gate never fires and the run is identical to not
        consolidating at all. A two-tailed gate fires and the answers diverge.
        """
        gated = build(2.0, cap=50.0).run(PREDICTABLE)
        never = build(0.0, cap=0.0, consolidation=0.0).run(PREDICTABLE)
        self.assertFalse(
            np.array_equal(gated, never),
            "the gate did not fire on a sequence whose only unusual steps are "
            "unusually well predicted, so it is watching one tail, not two")

    def test_a_high_bar_fires_less_than_a_low_one(self):
        model_low = build(1.0, cap=50.0)
        model_high = build(4.0, cap=50.0)
        self.assertFalse(
            np.array_equal(model_low.run(TOKENS), model_high.run(TOKENS)),
            "raising the bar changed nothing, so the threshold is not being "
            "consulted")


class TheCapBoundsTheStore(unittest.TestCase):

    def test_two_caps_that_both_bind_give_two_different_answers(self):
        """The cap's VALUE has to matter, not just its presence.

        The earlier version compared a cap of 50 -- which never binds, the store
        never gets that large -- against one of 0.02. That passes for any
        mechanism that does *something* when the store is large, including
        clipping every entry to a fixed range, which is not synaptic scaling and
        is not local to the store as a whole.

        Both caps here bind. Scaling divides the whole store by its own norm and
        multiplies by the budget, so a smaller budget leaves a proportionally
        smaller store and a different answer. Clipping to a fixed range ignores
        the budget once it is below the clip, and collapses these two runs onto
        the same predictions.
        """
        looser = build(1.5, cap=0.02).run(TOKENS)
        tighter = build(1.5, cap=0.005).run(TOKENS)
        self.assertFalse(
            np.array_equal(looser, tighter),
            "two different binding caps gave identical answers, so the store is "
            "not being scaled TO the cap -- the cap's value is being ignored")

    def test_the_gate_fires_at_all_on_this_fixture(self):
        """Without this, every test above could pass on a gate that never fires.

        That is exactly what the first fixture did: a repeating token sequence
        gave near-constant surprise, so nothing cleared the threshold and the cap
        had nothing to act on.
        """
        gated = build(1.5, cap=50.0).run(TOKENS)
        ungated = build(0.0, cap=0.0, consolidation=0.0).run(TOKENS)
        self.assertFalse(np.array_equal(gated, ungated),
                         "salience-gated consolidation changed nothing at all, "
                         "so the gate never fired and this fixture cannot test "
                         "it")

    def test_the_run_stays_finite_where_it_previously_diverged(self):
        """The specific failure: unbounded salience consolidation reached NaN."""
        model = build(1.0, cap=1.0, consolidation=5.0)
        predictions = model.run(np.tile(TOKENS, 4))
        self.assertTrue(np.isfinite(predictions).all())
        self.assertTrue((predictions >= 0).all())
        self.assertTrue((predictions < VOCAB).all())


if __name__ == "__main__":
    unittest.main()
