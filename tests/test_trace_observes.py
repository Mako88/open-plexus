"""Tracing must be a window, not a lever.

The probe that asks *is there a local signal separating a real binding from
filler* has to read the model's own quantities. The alternative is recomputing
them outside the model, which is exactly how the 150/300 cap values came from a
reimplementation whose store never bound — numbers that were correct about
something that was not this model.

So `trace` exists, and everything below is about it being observation. A trace
argument that changed a single prediction would make every number the probe
produces a number about the traced model rather than the real one.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 24, 32
TOKENS = np.random.default_rng(7).integers(0, VOCAB, 120)
MASK = np.zeros(len(TOKENS), dtype=bool)
MASK[::4] = True

KEYS = {"t", "token", "surprise", "mean", "deviation", "strength", "hit"}
#: A salience gate needs a cap or it diverges -- promoting on surprise enlarges
#: the store, which enlarges later surprises. The model refuses the pair without
#: one, which is why `lasting_cap` is here rather than a tidier two arguments.
SALIENT = dict(salience=1.5, capture_slots=4, consolidation=0.1, lasting_cap=8.0)


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=0.97, seed=2)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


class ItChangesNothing(unittest.TestCase):

    def test_predictions_are_identical_with_and_without_a_trace(self):
        np.testing.assert_array_equal(
            build().run(TOKENS, trace=[]), build().run(TOKENS))

    def test_identical_under_a_store_mask_too(self):
        """The masked path is a different branch and has its own fade."""
        np.testing.assert_array_equal(
            build().run(TOKENS, store=MASK, trace=[]),
            build().run(TOKENS, store=MASK))

    def test_identical_with_salience_on(self):
        """Salience reads `deviation`, which is the quantity the trace reports;
        if collecting it perturbed the running estimate this is where it shows."""
        np.testing.assert_array_equal(
            build(**SALIENT).run(TOKENS, trace=[]),
            build(**SALIENT).run(TOKENS))

    def test_the_same_model_traced_twice_gives_the_same_trace(self):
        first, second = [], []
        build().run(TOKENS, trace=first)
        build().run(TOKENS, trace=second)
        self.assertEqual(first, second)


class WhatItReports(unittest.TestCase):

    def test_every_entry_carries_every_signal(self):
        trace = []
        build().run(TOKENS, trace=trace)
        self.assertTrue(trace)
        for entry in trace:
            self.assertEqual(set(entry), KEYS)

    def test_steps_are_reported_in_order_and_only_once(self):
        trace = []
        build().run(TOKENS, trace=trace)
        steps = [entry["t"] for entry in trace]
        self.assertEqual(steps, sorted(steps))
        self.assertEqual(len(steps), len(set(steps)))

    def test_the_token_reported_is_the_token_that_arrived(self):
        """Off by one here would make every separability number about the
        neighbouring position, and nothing downstream could detect it."""
        trace = []
        build().run(TOKENS, trace=trace)
        for entry in trace:
            self.assertEqual(entry["token"], int(TOKENS[entry["t"]]))

    def test_the_first_step_cannot_be_traced(self):
        """Surprise needs a previous prediction, so step 0 has none. A trace
        that reported it would be reporting a made-up number."""
        trace = []
        build().run(TOKENS, trace=trace)
        self.assertNotIn(0, [entry["t"] for entry in trace])


class TheSignalsMeanWhatTheyAreCalled(unittest.TestCase):

    def test_surprise_falls_on_a_repeating_sequence(self):
        """The property that made `surprise` surprise rather than margin: a
        pattern seen again must be less surprising, not more. Margin surprise
        rose 266% on this exact shape."""
        cycle = np.tile(np.arange(6), 12)
        trace = []
        build(vocab_size=8, lr=0.2).run(cycle, trace=trace)
        early = [e["surprise"] for e in trace if e["t"] < 12]
        late = [e["surprise"] for e in trace if e["t"] >= len(cycle) - 12]
        self.assertLess(sum(late) / len(late), sum(early) / len(early))

    def test_the_running_mean_tracks_the_surprises_reported(self):
        """Otherwise `mean` describes some other sequence of numbers, and a
        deviation measured against it is not a deviation of this signal."""
        trace = []
        build().run(TOKENS, trace=trace)
        surprises = [e["surprise"] for e in trace]
        self.assertAlmostEqual(trace[-1]["mean"],
                               sum(surprises) / len(surprises), places=9)

    def test_hit_agrees_with_the_predictions_that_were_returned(self):
        """`hit` is *predict the future and compare* in its literal form, and it
        is scored against the array the run itself returned rather than against a
        recomputed one -- a probe that recomputed it would be measuring its own
        arithmetic."""
        trace = []
        predictions = build().run(TOKENS, trace=trace)
        for entry in trace:
            self.assertEqual(entry["hit"],
                             predictions[entry["t"] - 1] == TOKENS[entry["t"]])

    def test_hit_is_neither_always_true_nor_always_false(self):
        """A constant signal separates nothing, and would sit at AUC 0.5 while
        looking like a measurement."""
        trace = []
        build().run(TOKENS, trace=trace)
        hits = [entry["hit"] for entry in trace]
        self.assertTrue(any(hits))
        self.assertFalse(all(hits))

    def test_hit_and_surprise_are_not_the_same_signal(self):
        """They are treated as separate columns, so they have to be separable.
        If every hit had lower surprise than every miss, one would be a
        threshold on the other and scoring both would double-count."""
        trace = []
        build().run(TOKENS, trace=trace)
        hit = [e["surprise"] for e in trace if e["hit"]]
        miss = [e["surprise"] for e in trace if not e["hit"]]
        self.assertTrue(hit and miss)
        self.assertGreater(max(hit), min(miss))

    def test_deviation_is_zero_only_at_the_very_first_traced_step(self):
        trace = []
        build().run(TOKENS, trace=trace)
        self.assertEqual(trace[0]["deviation"], 0.0)
        self.assertTrue(any(e["deviation"] > 0.0 for e in trace[1:]))


if __name__ == "__main__":
    unittest.main()
