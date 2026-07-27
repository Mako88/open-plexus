"""A gate on the FAST store, driven by a token the node actually receives.

[g8-03](../experiments/sweeps/g8-03-a-pool-you-have-to-win.txt) established what
the oracle does and what six failed mechanisms did not. `run(store=mask)` gates
**admission to the fast store**, so `memory` holds a constant number of bindings
whatever the sequence length — and that is its entire advantage. Every mechanism
tried before this one acted on the lasting store, or on what was promoted out of
the fast one.

So this gates the fast store. [Note 016](../docs/notes/016-who-supplies-relevance.md)
argues the signal cannot be derived from the statistics of the stream — five
mechanisms failed trying — and has to arrive from outside them. A reward token is
in the input, on the same broadcast every node already receives, which
`position_kinds()` is not.

**The difficulty is that the signal arrives late.** The reward comes after the
binding it refers to, so at write time the node cannot know. It writes
everything, and a reward retroactively decides what to keep. These tests pin that
retroaction; whether it recovers anything is g9-02's business.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 20, 24
REWARD = VOCAB - 1


def build(reward: int = -1, window: int = 0, decay: float = 0.99):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=decay,
        reward_token=reward, reward_window=window, seed=4))
    model.wo[:] = model.wv           # a decoder, so predictions track the memory
    return model


def stream(gap: int, rewards: int = 4, seed: int = 3) -> np.ndarray:
    """Bindings separated by filler, each followed `gap` steps later by a reward."""
    rng = np.random.default_rng(seed)
    tokens: list[int] = []
    for _ in range(rewards):
        tokens.extend(int(t) for t in rng.integers(0, REWARD, gap + 1))
        tokens.append(REWARD)
        tokens.extend(int(t) for t in rng.integers(0, REWARD, 12))
    return np.array(tokens)


TOKENS = stream(gap=4)


class OffByDefault(unittest.TestCase):

    def test_the_default_is_disabled(self):
        config = LocalMemoryConfig(vocab_size=VOCAB)
        self.assertEqual(config.reward_token, -1)
        self.assertEqual(config.reward_window, 0)

    def test_a_negative_window_is_refused(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_window=-1)

    def test_a_window_without_a_reward_token_is_refused(self):
        """It is the reach of a gate that would not exist."""
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=VOCAB, reward_window=3)

    def test_disabled_matches_no_gate_at_all(self):
        np.testing.assert_array_equal(build(reward=-1).run(TOKENS),
                                      build(reward=-1, window=0).run(TOKENS))


class ARewardDecidesWhatSurvives(unittest.TestCase):

    def test_the_gate_changes_the_answer(self):
        self.assertFalse(
            np.array_equal(build(reward=REWARD).run(TOKENS),
                           build(reward=-1).run(TOKENS)),
            "gating on the reward token changed nothing, so nothing is being "
            "taken back out of the store")

    def test_a_wider_window_keeps_more(self):
        answers = {build(reward=REWARD, window=w).run(TOKENS).tobytes()
                   for w in (0, 2, 8)}
        self.assertGreater(
            len(answers), 1,
            "the window is not being consulted, so the gate keeps the same "
            "thing however far back it is allowed to reach")

    def test_a_window_wide_enough_to_cover_everything_keeps_everything(self):
        """The equivalence that says what the mechanism IS.

        If the window reaches back further than anything written since the last
        reward, nothing is ever discarded, and the gate must reduce **exactly**
        to not gating. A difference means it is doing something besides deciding
        what to keep.
        """
        np.testing.assert_array_equal(
            build(reward=REWARD, window=10_000).run(TOKENS),
            build(reward=-1).run(TOKENS))

    def test_a_stream_with_no_rewards_is_never_pruned(self):
        """Nothing vouches for anything, so nothing is taken out. The store is
        exactly what it would have been ungated."""
        rng = np.random.default_rng(11)
        no_rewards = rng.integers(0, REWARD, 120)
        np.testing.assert_array_equal(
            build(reward=REWARD, window=1).run(no_rewards),
            build(reward=-1).run(no_rewards))


class TheSubtractionIsExact(unittest.TestCase):
    """A contribution has to be removed as it NOW stands, not as it went in.

    Everything pending was scaled by every decay step since it was written, and
    by any capping. Removing the original outer product would take out more than
    is actually there and drive the store negative.
    """

    def test_it_holds_under_decay(self):
        """With a window past everything, the gate is a no-op — but only if the
        bookkeeping tracks the fading. If it does not, this diverges from the
        ungated run."""
        for decay in (1.0, 0.99, 0.9, 0.5):
            with self.subTest(decay=decay):
                np.testing.assert_array_equal(
                    build(reward=REWARD, window=10_000, decay=decay).run(TOKENS),
                    build(reward=-1, decay=decay).run(TOKENS))

    def test_it_holds_under_a_binding_cap(self):
        """The cap rescales the whole store, so everything pending moves with it."""
        capped = LocalMemoryConfig(
            vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=0.99,
            memory_cap=2.0, reward_token=REWARD, reward_window=10_000, seed=4)
        plain = LocalMemoryConfig(
            vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5, decay=0.99,
            memory_cap=2.0, seed=4)
        one, two = LocalAssociativeMemory(capped), LocalAssociativeMemory(plain)
        one.wo[:] = one.wv
        two.wo[:] = two.wv
        np.testing.assert_array_equal(one.run(TOKENS), two.run(TOKENS))


class TheDelayIsTheDifficulty(unittest.TestCase):
    """At a short gap the rule is 'keep the thing before the marker', which
    learns nothing about value. The point of the mechanism is the long gap."""

    def test_the_gap_between_binding_and_reward_changes_the_outcome(self):
        near = build(reward=REWARD, window=1).run(stream(gap=1))
        far = build(reward=REWARD, window=1).run(stream(gap=12))
        self.assertNotEqual(near.tobytes(), far.tobytes())


if __name__ == "__main__":
    unittest.main()
