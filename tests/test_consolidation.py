"""Consolidate-on-use — the implementable replacement for the oracle gate.

g7-01 showed that keeping only the useful bindings makes the task trivial, and
that the blocker was deciding *at storage time* which those are. Note 010 took the
answer from synaptic tagging and capture: do not decide then. Write everything
weakly into a fast decaying store, and when a retrieval later turns out to have
been right, promote what was retrieved into a store that does not decay.

The confirming signal is the model's own prediction against the token that
arrives next — self-supervised, local, no lookahead. These tests fix that
timing in place, because a mechanism that peeked one step ahead would look
spectacular and mean nothing.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB = 14


def build(consolidation: float, decay: float = 0.9, seed: int = 3):
    return LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=32, lr=0.05, key_scale=0.5,
        decay=decay, consolidation=consolidation, seed=seed))


class OffByDefaultAndOffMeansUnchanged(unittest.TestCase):
    """Every result before this existed was measured with it absent."""

    def test_the_default_is_zero(self):
        self.assertEqual(LocalMemoryConfig(vocab_size=VOCAB).consolidation, 0.0)

    def test_zero_reproduces_the_plain_model(self):
        tokens = np.array([2, 5, 9, 2, 7, 5, 3, 9, 1, 2])
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        plain = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=VOCAB, d_model=32, lr=0.05, key_scale=0.5,
            decay=0.9, seed=3))
        gated = build(0.0)
        plain.run(tokens, targets, scored, learn=True)
        gated.run(tokens, targets, scored, learn=True)
        np.testing.assert_array_equal(plain.wo, gated.wo)


class ItNeedsSomethingToRescueFrom(unittest.TestCase):
    """Consolidation without decay is a second copy of a memory that never fades."""

    def test_consolidation_with_no_decay_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, consolidation=0.5, decay=1.0)

    def test_negative_consolidation_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, consolidation=-0.1, decay=0.9)


class ItActsOnlyOnConfirmedRetrievals(unittest.TestCase):
    """The property that makes it a gate rather than a second store."""

    def test_a_sequence_it_never_predicts_correctly_consolidates_nothing(self):
        """No confirmations means no consolidation, and the test must be able to
        tell.

        The first version used an untrained readout, whose predictions are token
        0 whatever the memory holds — so it could not have detected a change in
        the memory even if one happened. The mutation harness proved it: removing
        the confirmation check entirely left this assertion passing.

        With `wo = wv` the readout decodes retrieved values back to tokens, so
        the predictions vary with the memory and a spurious promotion would show.
        This particular sequence produces varied predictions and, checked
        explicitly below, zero confirmations.
        """
        tokens = np.array([2, 5, 9, 2, 7, 5, 3, 9, 1, 2, 5, 9])
        without, with_ = build(0.0), build(1.0)
        for model in (without, with_):
            model.wo[:] = model.wv

        baseline = without.run(tokens)
        confirmations = [t for t in range(1, len(tokens))
                         if baseline[t - 1] == tokens[t]]
        self.assertEqual(confirmations, [],
                         "the fixture confirms somewhere, so it cannot show "
                         "that no-confirmation means no-consolidation")
        self.assertGreater(len(set(baseline.tolist())), 2,
                           "predictions barely vary, so this test could not see "
                           "a change in the memory")

        np.testing.assert_array_equal(
            baseline, with_.run(tokens),
            "consolidation altered a run in which nothing was ever confirmed, "
            "so it promotes regardless of the signal")

    def test_a_sequence_it_does_predict_correctly_consolidates_something(self):
        """The control for the test above, or that one passes for free.

        It needs a sequence where the next token is actually predictable. The
        first version used a hand-made one and failed: in an arbitrary sequence
        the next token follows from nothing, so the confirming signal never fired
        and consolidation correctly did nothing. **The premise was wrong, not the
        code** — which is the failure mode of building a fixture instead of using
        the task.

        A real MQAR sequence has positions where the next token IS the bound
        value, so a model that has learned the readout confirms there.
        """
        from dataclasses import replace

        from openplexus.tasks.mqar import MqarConfig, dataset

        task = MqarConfig(n_pairs=3, seq_len=64, n_keys=16, n_values=6,
                          autoregressive=True, filler="random", seed=99,
                          queries_per_pair=3)
        train = dataset(task, 60)
        probe = np.asarray(dataset(replace(task, seed=task.seed + 7), 1)[0].tokens)

        trajectories = []
        for consolidation in (0.0, 1.0):
            model = LocalAssociativeMemory(LocalMemoryConfig(
                vocab_size=task.vocab_size, d_model=32, lr=0.05, key_scale=0.5,
                decay=0.9, consolidation=consolidation, seed=3))
            for sequence in train:
                tokens = np.asarray(sequence.tokens)
                targets = np.roll(tokens, -1)
                scored = np.ones(len(tokens), dtype=bool)
                scored[-1] = False
                model.run(tokens, targets, scored, learn=True)
            trajectories.append(model.run(probe))

        self.assertFalse(
            np.array_equal(*trajectories),
            "consolidation changed nothing on a task the model has learned, so "
            "the confirming signal is never firing")


class TheSignalComesFromTheNextTokenNotAheadOfIt(unittest.TestCase):
    """The timing, which is the whole claim to being implementable."""

    def test_the_first_position_can_never_consolidate(self):
        """There is no earlier prediction to confirm at t=0.

        A mechanism that consolidated at the first step would be using
        information it cannot have, and the difference would be invisible in
        aggregate accuracy.

        **This test asserted nothing until R4 in `tools/check_rails.py` found
        it.** It built a model, ran it, and reported success -- the exact shape
        that passes while measuring nothing, under a docstring naming a real
        property. The property IS observable: consolidation cannot move the
        prediction at step 0, because there is no previous retrieval to promote.
        """
        for tokens in (np.array([4, 4]),
                       np.array([4, 4, 7, 4, 7, 7]),
                       np.random.default_rng(1).integers(0, VOCAB, 30)):
            with self.subTest(length=len(tokens)):
                off, on = self._pair(tokens)
                self.assertEqual(
                    off[0], on[0],
                    "consolidation changed the prediction at step 0, where "
                    "there is no earlier prediction to confirm")

    def test_and_consolidation_can_move_predictions_at_all(self):
        """Guard. Without it the test above passes on a model where
        consolidation does nothing anywhere -- which is most short sequences,
        since it fires on a confirmed retrieval and short random streams rarely
        have one. Two fixtures above are of exactly that kind, deliberately."""
        tokens = np.random.default_rng(1).integers(0, VOCAB, 30)
        off, on = self._pair(tokens)
        self.assertFalse(
            np.array_equal(off, on),
            "consolidation changed no prediction anywhere on this stream, so "
            "step 0 agreeing says nothing about step 0")

    def _pair(self, tokens):
        """The same sequence with consolidation off and on, as a decoder."""
        runs = []
        for rate in (0.0, 1.0):
            model = build(rate)
            model.wo[:] = model.wv
            runs.append(model.run(np.asarray(tokens)))
        return runs

    def test_predictions_depend_only_on_the_past(self):
        """Truncating the future must not change any earlier prediction.

        This is the property that would break if consolidation read ahead: the
        answer at position t would depend on tokens after t.
        """
        tokens = np.array([2, 5, 9, 2, 7, 5, 3, 9, 1, 2, 5, 9])
        model = build(1.0)
        model.wo[:] = model.wv
        full = model.run(tokens)
        for cut in (4, 7, 10):
            with self.subTest(cut=cut):
                np.testing.assert_array_equal(
                    model.run(tokens[:cut]), full[:cut],
                    f"truncating after {cut} changed an earlier prediction, so "
                    f"something is reading ahead")


class ItHelpsWhereItCanAndNotWhereItCannot(unittest.TestCase):
    """The mechanism's fingerprint, which a wrongly-triggered gate cannot fake.

    Consolidation can only act on a REPEATED ask: the first ask is what confirms
    a retrieval was useful, and only later asks can spend that. So the lift must
    be concentrated on later asks and absent from the first.

    Synthetic fixtures could not distinguish this. On a hand-made sequence, twelve
    firings moved two predictions out of thirty-six -- too weak to separate a
    correct gate from one triggering on the wrong condition. **The signal only
    exists on the task**, which is where this test therefore lives, at the cost of
    half a second of training.
    """

    def _by_ask(self, consolidation):
        from collections import defaultdict
        from dataclasses import replace

        from openplexus.tasks.mqar import MqarConfig, dataset

        task = MqarConfig(n_pairs=2, seq_len=48, n_keys=16, n_values=6,
                          autoregressive=True, filler="random", seed=5,
                          queries_per_pair=3)
        train = dataset(task, 40)
        test = dataset(replace(task, seed=task.seed + 7), 20)

        rng = np.random.default_rng(1)
        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=task.vocab_size, d_model=16, lr=0.05, key_scale=0.5,
            decay=0.9, consolidation=consolidation, seed=1))
        for _ in range(3):
            for index in rng.permutation(len(train)):
                tokens = np.asarray(train[index].tokens)
                targets = np.roll(tokens, -1)
                scored = np.ones(len(tokens), dtype=bool)
                scored[-1] = False
                model.run(tokens, targets, scored, learn=True)

        buckets = defaultdict(lambda: [0, 0])
        for sequence in test:
            tokens = np.asarray(sequence.tokens)
            predicted = model.run(tokens)
            seen = defaultdict(int)
            for q in sequence.query_positions:
                key = tokens[q]
                slot = buckets[seen[key]]
                slot[0] += predicted[q] == tokens[q + 1]
                slot[1] += 1
                seen[key] += 1
        return {k: v[0] / v[1] for k, v in sorted(buckets.items())}

    def test_the_lift_lands_on_the_last_ask_and_not_the_first(self):
        off, on = self._by_ask(0.0), self._by_ask(1.0)
        last, first = max(off), min(off)
        lift_last = on[last] - off[last]
        lift_first = on[first] - off[first]
        self.assertGreater(
            lift_last, 0.1,
            f"consolidation gained only {lift_last:+.3f} on the final ask, "
            f"where it is the one thing that can help")
        self.assertGreater(
            lift_last, lift_first + 0.1,
            f"the gain is spread evenly ({lift_first:+.3f} on the first ask "
            f"against {lift_last:+.3f} on the last), so whatever helped is not "
            f"consolidation acting on repeated asks")


if __name__ == "__main__":
    unittest.main()
