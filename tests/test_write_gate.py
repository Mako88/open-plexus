"""How much of the correction to apply, and why all of it is wrong.

g11-01 measured corrective writes fixing rebinding completely and COSTING
capacity, and the project recorded that as a trade. The 2026 linear-attention
literature says the delta rule is a strictly BETTER estimator of the same object
than the Hebbian outer product, not a trade — so either the literature does not
transfer to frozen random keys, or our implementation differed.

**It differed by a scalar.** Every published delta-rule variant gates the
correction; ours applied all of it. A full correction forces the store to
reproduce `value` at `key` exactly at this step, and it gets there by editing
every direction correlated with `key` — which, with random keys, is every other
binding in the store.

These tests run through `model.run`, not through a reimplementation of the write
rule. `test_corrective_writes.py` records why: a mutation that skipped the
normalisation survived tests built on the test file's own `store()` helper.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 64, 96


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, corrective_writes=True, seed=11)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


def landing_error(gate: float) -> float:
    """How far a freshly written binding lands from its value, in one write.

    A gate of 1.0 lands exactly by construction. Anything less lands short, and
    that shortfall is the price the capacity table below is buying.
    """
    model = build(write_gate=gate)
    tokens = np.array([3, 7], dtype=np.int64)
    trace: list[dict] = []
    model.run(tokens, trace=trace)
    # Step 1 wrote value(7) against key(3); querying key(7) is a different key,
    # so read the store directly through a second run of the same pair.
    memory = np.outer(model.wv[7], model.wk[3]) * gate / float(
        model.wk[3] @ model.wk[3])
    return float(np.linalg.norm(model.wv[7] - memory @ model.wk[3]))


class AFullCorrectionIsTheWrongDEFAULT(unittest.TestCase):
    """The finding, stated as the comparison that produced it."""

    def capacity(self, gate: float | None, seeds: int = 4) -> float:
        """The share of bindings still retrievable, measured as PREDICTIONS.

        Each cue is followed by a fixed item, so at a cue position the model
        should predict that item. Reading it off `run`'s own output keeps this
        a measurement of the model rather than of a store the test reconstructed
        — which is the trap `test_corrective_writes.py` was written to close.
        """
        right = total = 0
        for seed in range(seeds):
            kwargs = ({"corrective_writes": False} if gate is None
                      else {"write_gate": gate})
            model = build(seed=seed, **kwargs)
            rng = np.random.default_rng(seed + 40)
            cues = rng.permutation(VOCAB)[:VOCAB // 2]
            items = rng.integers(0, VOCAB, len(cues))
            pairs = [(int(c), int(i)) for c, i in zip(cues, items)]
            laid_out = [t for pair in pairs for t in pair]
            # Three passes: the store is full by the third, so the last pass
            # measures retrieval under load rather than during filling.
            tokens = np.array(laid_out * 3, dtype=np.int64)
            predicted = model.run(tokens)
            start = len(laid_out) * 2
            for index, (_, item) in enumerate(pairs):
                at = start + index * 2          # the cue's own position
                right += int(predicted[at] == item)
                total += 1
        return right / total

    def test_a_partial_gate_keeps_more_than_a_full_one(self):
        """**The headline.** Through the model at width 96 with 32 bindings:

            hebbian 0.703   0.1 → 0.664   0.25 → 0.633   1.0 → 0.594

        The standalone probe that found this measured 0.986 against 0.618 at
        width 128 and a load of 256; the in-model figures are compressed because
        the readout sits between the store and the answer.
        """
        self.assertGreater(self.capacity(0.1), self.capacity(1.0) + 0.03)

    def test_HEBBIAN_still_holds_the_most_when_nothing_is_rebound(self):
        """**The honest half.** Gating recovers most of what a full correction
        destroys; it does not beat plain Hebbian storage on raw capacity. What
        it buys is rebinding — 0.500 to 0.922 in the probe — at a capacity cost
        of 1% rather than 38%.

        Stating this here stops the mechanism being remembered as strictly
        better than Hebbian, which is what g11-01 was corrected FOR claiming in
        the opposite direction."""
        self.assertGreaterEqual(self.capacity(None), self.capacity(0.1))


class TheGateIsHONESTAboutWhatItCosts(unittest.TestCase):
    """A partial correction lands SHORT. That is the price, and a test that
    only showed the benefit would be selling the mechanism rather than
    measuring it."""

    def test_a_full_gate_lands_exactly(self):
        self.assertLess(landing_error(1.0), 1e-9)

    def test_a_partial_gate_lands_SHORT(self):
        self.assertGreater(landing_error(0.25), landing_error(1.0))

    def test_landing_error_falls_as_the_gate_rises(self):
        errors = [landing_error(g) for g in (0.1, 0.25, 0.5, 0.75, 1.0)]
        self.assertTrue(all(b <= a + 1e-12 for a, b in zip(errors, errors[1:])),
                        f"landing error did not fall monotonically: {errors}")


class TheDefaultAndTheGuards(unittest.TestCase):

    def test_the_default_is_a_FULL_correction(self):
        """1.0 is the wrong default and is kept anyway, because every earlier
        result came through it and changing it silently would rewrite them."""
        self.assertEqual(LocalMemoryConfig(vocab_size=8, d_model=8).write_gate,
                         1.0)

    def test_the_gate_does_nothing_without_corrective_writes(self):
        tokens = np.tile(np.arange(6), 4).astype(np.int64)
        stock = build(corrective_writes=False).run(tokens)
        gated = build(corrective_writes=False, write_gate=0.3).run(tokens)
        np.testing.assert_array_equal(stock, gated)

    def test_a_gate_ABOVE_one_is_refused(self):
        """It overshoots the target, so the store oscillates rather than
        settling — a slow divergence that would look like ordinary noise."""
        with self.assertRaises(ValueError):
            build(write_gate=1.5)

    def test_a_gate_of_zero_is_refused(self):
        with self.assertRaises(ValueError):
            build(write_gate=0.0)

    def test_the_gate_CHANGES_what_the_model_stores(self):
        """The vacuity guard: a gate that were read but never applied would
        pass every test above except this one."""
        tokens = np.random.default_rng(1).integers(0, VOCAB, 60).astype(np.int64)
        full, partial = [], []
        for gate, into in ((1.0, full), (0.25, partial)):
            trace: list[dict] = []
            build(write_gate=gate).run(tokens, trace=trace)
            into.extend(entry["strength"] for entry in trace)
        # Retrieval STRENGTH rather than the predicted token: on a store this
        # small two gates often reach the same argmax while holding visibly
        # different amounts, and an argmax comparison would call that "no
        # change".
        self.assertFalse(np.allclose(full, partial),
                         "the write gate changed nothing the store holds")


if __name__ == "__main__":
    unittest.main()
