"""A capacity measured in bindings, and a mark that fades.

[g9-03](../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt) found the
reward gate's cliff is the diagonal: a window recovers about 0.2 wherever it
covers the delay and about -0.22 wherever it does not, and a node does not know
the delay. Widening it does not help, because reach is bought in steps — at 31
steps per binding a 64-step window holds two bindings and sixty-two steps of
filler, and recovers 0.09 at every delay.

[g9-04](../experiments/sweeps/g9-04-is-there-a-local-signal.txt) measured what a
mark could hang on. Retrieval strength separates a binding-write from a
filler-write at AUC 0.293 and 0.215 — **below 0.5, so inverted**: filler repeats,
so a filler key has been bound many times and retrieves strongly, while a
binding's cue is fresh and retrieves weakly. The rule is *admit the weak
retrievals*, which is the opposite of what competitive capture does.

**These assert what the gate KEEPS, not what the model then scores.** The claim
a gate makes is about which writes survive; accuracy is downstream of that and
cannot tell a tag holding bindings from a tag holding the first four writes after
every reward. `trace` reports the surviving set directly, so the tests read it.

Whether keeping those writes recovers anything is g9-05's business.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig, admit, fade, tag)

VOCAB, WIDTH = 24, 48
REWARD = VOCAB - 1
CUE, VALUE = 10, 11


def build(slots: int = 0, window: int = 0, decay_tag: float = 1.0,
          strongest: bool = False, reward: int = REWARD, decay: float = 0.99):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=decay,
        reward_token=reward, reward_window=window, tag_slots=slots,
        tag_decay=decay_tag, tag_strongest=strongest, seed=7))
    # A decoder, so a prediction reads the memory rather than an untrained
    # readout -- the same choice g9-04's probe made, for the same reason.
    model.wo[:] = model.wv
    return model


def _filler(count: int, rng) -> list[int]:
    """Drawn from a small spare alphabet, so filler keys recur and retrieve
    strongly. That recurrence is what makes the signal exist at all."""
    return [int(x) for x in rng.integers(1, 9, count)]


def stream(before: int = 25, after: int = 20, intervals: int = 1,
           seed: int = 0) -> np.ndarray:
    """`intervals` copies of: filler, one fresh binding, filler, reward."""
    rng = np.random.default_rng(seed)
    tokens: list[int] = []
    for _ in range(intervals):
        tokens += _filler(before, rng) + [CUE, VALUE] + _filler(after, rng)
        tokens.append(REWARD)
    return np.array(tokens)


def captures(model, tokens: np.ndarray) -> list[list[int]]:
    """The steps each capture kept, one list per reward.

    Read off `trace`, which reports the pending indices a capture protected and
    where each step's own write landed in that list. Reconstructing it here
    rather than reaching into the model keeps the model's surface closed --
    and the trace is pinned as prediction-neutral by test_trace_observes.
    """
    trace: list = []
    model.run(tokens, trace=trace)
    kept, where = [], {}
    for entry in trace:
        if entry["write_index"] >= 0:
            where[entry["write_index"]] = entry["t"]
        if entry["captured"]:
            kept.append(sorted(where[i] for i in entry["captured"] if i in where))
            where = {}
    return kept


class OffByDefault(unittest.TestCase):

    def test_the_default_is_disabled(self):
        config = LocalMemoryConfig(vocab_size=VOCAB)
        self.assertEqual(config.tag_slots, 0)
        self.assertEqual(config.tag_decay, 1.0)
        self.assertFalse(config.tag_strongest)

    def test_a_negative_capacity_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, tag_slots=-1)

    def test_a_tag_without_a_reward_token_is_refused(self):
        """Nothing would ever capture it, so the marks would never be read."""
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, tag_slots=4)

    def test_a_tag_and_a_window_together_are_refused(self):
        """Two answers to one question. An arm running both is neither arm."""
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              reward_window=4, tag_slots=4)

    def test_a_fade_outside_the_unit_interval_is_refused(self):
        for value in (0.0, -0.5, 1.5):
            with self.subTest(value=value), self.assertRaises(ValueError):
                LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                                  tag_slots=4, tag_decay=value)

    def test_a_fade_without_a_tag_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              tag_decay=0.9)

    def test_the_direction_flag_alone_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_token=REWARD,
                              tag_strongest=True)


class TheTagChoosesWhatSurvives(unittest.TestCase):

    def test_a_tag_keeps_a_different_set_than_a_window(self):
        """The connection test. If these agree the ranking is not consulted and
        the tag is a window with bookkeeping."""
        tokens = stream()
        self.assertNotEqual(captures(build(slots=4, decay_tag=0.9), tokens),
                            captures(build(window=3), tokens))

    def test_capacity_bounds_what_survives(self):
        tokens = stream()
        for slots in (1, 2, 4, 8):
            with self.subTest(slots=slots):
                for kept in captures(build(slots=slots), tokens):
                    self.assertLessEqual(
                        len(kept), slots,
                        "more writes survived than the pool has room for")

    def test_capacity_is_consulted(self):
        tokens = stream()
        sets = {str(captures(build(slots=k, decay_tag=0.9), tokens))
                for k in (1, 4, 16)}
        self.assertGreater(len(sets), 1, "the pool is not binding")

    def test_the_direction_is_consulted(self):
        """`tag_strongest` is g9-05's control arm. If it cannot change what
        survives, the arm measures nothing and the sweep cannot separate 'the
        signal works' from 'any capacity works'."""
        tokens = stream()
        self.assertNotEqual(
            captures(build(slots=4, decay_tag=0.9), tokens),
            captures(build(slots=4, decay_tag=0.9, strongest=True), tokens))

    def test_a_tag_bigger_than_the_demand_keeps_everything(self):
        """The equivalence that says what the mechanism IS.

        With more slots than writes to hold, nothing is refused and nothing is
        displaced, so the gate must reduce **exactly** to not gating. Any
        difference means it does something besides choosing what to keep.
        """
        tokens = stream()
        np.testing.assert_array_equal(build(slots=10_000).run(tokens),
                                      build(reward=-1).run(tokens))

    def test_a_stream_with_no_rewards_is_never_pruned(self):
        """Marks are made and nothing captures them, so nothing is removed."""
        rng = np.random.default_rng(3)
        tokens = np.array(_filler(60, rng) + [CUE, VALUE] + _filler(20, rng))
        np.testing.assert_array_equal(build(slots=2).run(tokens),
                                      build(reward=-1).run(tokens))


class TheMarkFades(unittest.TestCase):
    """Without this the tag ranks the whole interval at once, and the weakest
    retrievals it will ever see are the ones made when the store was smallest.

    That is not a hypothetical. Measured on `reward_recall`, an un-faded tag of
    four keeps the same 8 bindings out of 32 captures at every capacity and every
    delay -- because slots two through four go to the first writes after each
    capture, when the store is nearly empty and everything retrieves weakly.
    """

    def test_the_fade_changes_what_survives(self):
        """The connection test for the fade, and the one that matters most.

        The first version of `fade` multiplied every rank by the factor. For a
        tag admitting weak retrievals the ranks are negative, so that moved them
        toward zero -- toward the winning end -- and ENTRENCHED the marks it was
        meant to release. It produced numbers identical to no fade at all, at
        every setting from 0.99 down to 0.7, which is exactly the failure mode
        CLAUDE.md is written against: a dial that is read, applied, and doing
        nothing.
        """
        tokens = stream()
        sets = {str(captures(build(slots=4, decay_tag=f), tokens))
                for f in (1.0, 0.95, 0.9, 0.8)}
        self.assertGreater(
            len(sets), 1,
            "the fade changed nothing at any setting, so marks are not ageing")

    def test_fading_releases_the_oldest_marks(self):
        """The property, stated without reference to how it is computed: a fade
        moves what survives LATER in the interval, because old marks lose."""
        tokens = stream()
        without = captures(build(slots=4, decay_tag=1.0), tokens)[0]
        with_fade = captures(build(slots=4, decay_tag=0.9), tokens)[0]
        self.assertGreater(
            sum(with_fade) / len(with_fade), sum(without) / len(without),
            "fading marks did not shift the surviving set later, so age is not "
            "costing an incumbent anything")

    def test_a_rank_falls_whichever_end_is_winning(self):
        """`admit` keeps the largest rank, so fading means the rank FALLS.

        Which arithmetic does that depends on the sign, and getting it wrong is
        silent: one end fades and the other becomes immortal.
        """
        self.assertLess(fade(-2.0, 0.9), -2.0)      # weak-preferring: negative
        self.assertLess(fade(2.0, 0.9), 2.0)        # strong-preferring: positive

    def test_a_fade_of_one_is_the_identity(self):
        for rank in (-3.0, -0.5, 0.0, 0.5, 3.0):
            with self.subTest(rank=rank):
                self.assertEqual(fade(rank, 1.0), rank)


class TheTagIsClearedByCapture(unittest.TestCase):
    """A mark is spent when the reward reads it.

    `tagged` holds indices into `pending`, and `pending` empties at every reward.
    A mark that outlived its capture would point into the NEXT interval's list
    and protect whichever writes landed at those positions -- a gate keeping the
    earliest writes after a reward, which nothing here argued for and which looks
    fine from outside.
    """

    def test_every_capture_keeps_only_its_own_interval(self):
        tokens = stream(intervals=3)
        rewards = [int(t) for t in np.flatnonzero(tokens == REWARD)]
        kept = captures(build(slots=4, decay_tag=0.9), tokens)
        self.assertEqual(len(kept), len(rewards))
        start = 0
        for steps, reward_at in zip(kept, rewards):
            self.assertTrue(steps, "a capture kept nothing at all")
            self.assertTrue(
                all(start < step <= reward_at for step in steps),
                f"a capture at {reward_at} kept steps {steps}, which are not "
                f"all inside ({start}, {reward_at}] -- marks are surviving "
                f"their own capture")
            start = reward_at


class TheRankingPrimitiveIsShared(unittest.TestCase):
    """`tag` is `admit` with the sign flipped, and that is the finding.

    Competitive capture admits the strongest trace; g9-04 measured the signal as
    inverted. Rather than a second pool with the comparison the other way round
    -- two implementations of one behaviour, which drift -- the rank handed to
    `admit` is negated.
    """

    def test_the_weakest_candidates_win_a_full_tag(self):
        tagged: list = []
        for index, strength in enumerate([5.0, 4.0, 3.0, 2.0, 1.0]):
            tag(tagged, strength, index, capacity=2, strongest=False)
        self.assertEqual(sorted(index for _, index in tagged), [3, 4])

    def test_the_strongest_win_when_asked(self):
        tagged: list = []
        for index, strength in enumerate([5.0, 4.0, 3.0, 2.0, 1.0]):
            tag(tagged, strength, index, capacity=2, strongest=True)
        self.assertEqual(sorted(index for _, index in tagged), [0, 1])

    def test_a_losing_candidate_is_refused_rather_than_queued(self):
        tagged: list = []
        for index, strength in enumerate([1.0, 2.0, 3.0, 4.0]):
            tag(tagged, strength, index, capacity=2, strongest=False)
        self.assertEqual(sorted(index for _, index in tagged), [0, 1],
                         "a stronger retrieval displaced a weaker incumbent, so "
                         "the pool holds the most recent k, not the best k")

    def test_ties_go_to_the_incumbent(self):
        """Same rule as `admit`: a strictly-better test is what makes a run of
        equal candidates settle instead of churning."""
        tagged: list = []
        for index in range(4):
            tag(tagged, 2.0, index, capacity=2, strongest=False)
        self.assertEqual(sorted(index for _, index in tagged), [0, 1])

    def test_it_is_admit_underneath(self):
        """If these disagree there are two pools, and one will be fixed while
        the other keeps producing plausible numbers."""
        self.assertIsNone(admit([-1.0, -2.0], -3.0, 2))
        tagged = [(-1.0, 0), (-2.0, 1)]
        tag(tagged, 3.0, 9, capacity=2, strongest=False)
        self.assertNotIn(9, [index for _, index in tagged])


if __name__ == "__main__":
    unittest.main()
