"""The first task here where something is at stake, and the traps it has to avoid.

[Note 017](../docs/notes/017-a-task-with-something-at-stake.md) is the design.
The point of the task is that the signal saying *this mattered* arrives **in the
stream and late**, so a mechanism using it is reading its own input rather than
the generator's metadata.

Two traps are pinned here because both have already been sprung once in this
project:

**Filler that collides with an in-use cue** makes the task ill-posed rather than
hard — a filler token and a query token become byte-identical while requiring
different outputs. Found in MQAR by reading the first generated sequence.

**Queries that repeat one token.** The first version of this task had a single
rewarded item and asked about it three times. Every query was the same token, and
this memory is keyed on the current token, so `QUERY` retrieved whatever was last
bound to `QUERY` — the second and third queries were answerable by repetition.
That would have inflated every number the task ever produced.
"""

from __future__ import annotations

import hashlib
import unittest
from collections import Counter
from dataclasses import replace

from openplexus.tasks.reward_recall import (
    IGNORE, RewardConfig, dataset, generate)

BASE = RewardConfig(n_pairs=8, n_rewarded=2, n_cues=32, n_values=8,
                    seq_len=192, delay=4, seed=20260726)


class TheTaskIsWellPosed(unittest.TestCase):

    def test_filler_never_collides_with_a_cue_in_use(self):
        for sequence in dataset(BASE, 40):
            in_use = set(sequence.bindings)
            for token, kind in zip(sequence.tokens, sequence.position_kinds()):
                if kind == "filler":
                    self.assertNotIn(
                        token, in_use,
                        "a filler token equals a cue this sequence binds, so "
                        "two positions are byte-identical and need different "
                        "answers")

    def test_every_query_is_answerable_from_the_bindings(self):
        for sequence in dataset(BASE, 40):
            for position in sequence.query_positions:
                cue = sequence.tokens[position]
                self.assertEqual(sequence.targets[position],
                                 sequence.bindings[cue])

    def test_only_rewarded_cues_are_ever_asked_about(self):
        """The whole mechanism question: which bindings were worth keeping."""
        for sequence in dataset(BASE, 40):
            asked = {sequence.tokens[p] for p in sequence.query_positions}
            self.assertEqual(asked, set(sequence.rewarded))

    def test_the_trivial_floor_is_one_over_the_values(self):
        self.assertAlmostEqual(BASE.trivial_floor, 1 / BASE.n_values)


class TheQueriesDoNotAnswerThemselves(unittest.TestCase):
    """The trap that killed the first design of this task."""

    def test_more_than_one_distinct_cue_is_queried(self):
        for sequence in dataset(BASE, 20):
            asked = {sequence.tokens[p] for p in sequence.query_positions}
            self.assertGreater(
                len(asked), 1,
                "every query used the same token, so after the first one the "
                "answer can be had by repetition rather than by recall")

    def test_consecutive_queries_are_usually_different_cues(self):
        """Shuffled rather than grouped, or the repeats are free."""
        same = total = 0
        for sequence in dataset(BASE, 40):
            asked = [sequence.tokens[p] for p in sequence.query_positions]
            for a, b in zip(asked, asked[1:]):
                same += a == b
                total += 1
        self.assertLess(same / total, 0.5,
                        f"{same}/{total} consecutive queries repeat the "
                        f"previous cue, so recall is not being tested")


class TheRewardArrivesLateAndInTheStream(unittest.TestCase):
    """What makes this different from an oracle mask."""

    def test_a_reward_token_follows_each_rewarded_cue(self):
        for sequence in dataset(BASE, 20):
            kinds = sequence.position_kinds()
            self.assertEqual(kinds.count("reward"), BASE.n_rewarded)
            self.assertEqual(kinds.count("rewarded"), BASE.n_rewarded)

    def test_the_reward_comes_after_the_cue_it_marks(self):
        """Late is the point. A signal arriving first is not a later signal."""
        for sequence in dataset(BASE, 20):
            kinds = sequence.position_kinds()
            first_rewarded = kinds.index("rewarded")
            first_reward = kinds.index("reward")
            self.assertGreater(first_reward, first_rewarded)

    def test_the_delay_is_a_dial(self):
        near = generate(replace(BASE, delay=1))
        far = generate(replace(BASE, delay=30))
        self.assertNotEqual(near.tokens, far.tokens)

    def test_the_reward_token_is_in_the_vocabulary(self):
        """It is input, not metadata. A model can see it."""
        self.assertLess(BASE.reward_token, BASE.vocab_size)
        sequence = generate(BASE)
        self.assertIn(BASE.reward_token, sequence.tokens)


class TheRewardRateIsADial(unittest.TestCase):
    """The base rate note 013 blamed and g8-02 could not move."""

    def test_more_rewarded_pairs_means_more_queries(self):
        few = generate(replace(BASE, n_rewarded=1))
        many = generate(replace(BASE, n_rewarded=4))
        self.assertLess(len(few.query_positions), len(many.query_positions))

    def test_rewarding_everything_is_allowed(self):
        sequence = generate(replace(BASE, n_rewarded=BASE.n_pairs))
        self.assertEqual(len(sequence.rewarded), BASE.n_pairs)

    def test_rewarding_more_than_there_are_pairs_is_refused(self):
        with self.assertRaises(ValueError):
            replace(BASE, n_rewarded=BASE.n_pairs + 1)


class ItIsReproducible(unittest.TestCase):

    def test_the_same_seed_gives_the_same_sequences(self):
        self.assertEqual([s.tokens for s in dataset(BASE, 5)],
                         [s.tokens for s in dataset(BASE, 5)])

    def test_different_seeds_do_not(self):
        self.assertNotEqual(generate(BASE, seed=1).tokens,
                            generate(BASE, seed=2).tokens)

    def test_golden_digest(self):
        """Pins the generator so a later change cannot silently move every
        result measured on it, the way the recurrent-MQAR digest does."""
        digest = hashlib.sha256(
            repr([s.tokens for s in dataset(BASE, 8)]).encode()).hexdigest()
        self.assertEqual(digest[:16], GOLDEN,
                         "the generator changed; if that was deliberate, every "
                         "number measured on the old one needs re-taking")


class TheShapeIsSane(unittest.TestCase):

    def test_a_sequence_is_the_requested_length(self):
        for n_rewarded in (1, 2, 4):
            sequence = generate(replace(BASE, n_rewarded=n_rewarded))
            self.assertEqual(len(sequence.tokens), len(sequence.targets))
            self.assertEqual(len(sequence.position_kinds()),
                             len(sequence.tokens))

    def test_targets_are_ignored_everywhere_except_queries(self):
        sequence = generate(BASE)
        scored = [i for i, t in enumerate(sequence.targets) if t != IGNORE]
        self.assertEqual(tuple(scored), sequence.query_positions)

    def test_a_sequence_too_short_to_hold_the_task_is_refused(self):
        with self.assertRaises(ValueError):
            replace(BASE, seq_len=8)

    def test_filler_dominates_so_selectivity_matters(self):
        kinds = Counter(generate(BASE).position_kinds())
        self.assertGreater(kinds["filler"] / sum(kinds.values()), 0.5)


#: Computed from the generator as first written, then pinned. A change here is
#: not a test failure to be fixed by updating the constant -- it means every
#: number ever measured on this task was measured on different data.
GOLDEN = "29beb9f989cce869"


class TheLayoutLeaksWhichBindingWasRewarded(unittest.TestCase):
    """A DEFECT, pinned so it is not rediscovered or silently fixed.

    Bindings sit on a lattice — `generate` uses a CONSTANT gap — and the reward
    is placed `delay` steps after its cue, where every sweep uses a delay far
    below the spacing. So the nearest binding before any reward is always the
    rewarded one, and "detect a binding, keep the most recent before a reward"
    solves the task exactly, using only local signals.

    That is not what note 017 built this task to pose. These tests assert the
    leak EXISTS rather than that it is absent, because the numbers in g9-02 to
    g9-10 were measured with it present and a test claiming otherwise would be
    false. See docs/notes/027-the-task-leaks-the-answer-through-its-layout.md.

    **When the generator is fixed, these tests should FAIL** — that is their
    purpose. Replace them then, and re-baseline what depends on them.
    """

    def _bindings_and_rewards(self, delay):
        config = RewardConfig(n_pairs=24, n_rewarded=4, n_cues=64, n_values=8,
                              seq_len=768, delay=delay, queries_per_reward=3,
                              seed=20260726)
        return [generate(config, seed=i) for i in range(12)]

    def test_the_nearest_binding_before_a_reward_is_always_the_rewarded_one(self):
        for delay in (1, 8, 20):
            with self.subTest(delay=delay):
                hits = total = 0
                for sequence in self._bindings_and_rewards(delay):
                    kinds = sequence.position_kinds()
                    values = [t for t, k in enumerate(kinds) if k == "value"]
                    for r, kind in enumerate(kinds):
                        if kind != "reward":
                            continue
                        before = [t for t in values if t < r]
                        if not before:
                            continue
                        total += 1
                        hits += kinds[before[-1] - 1] == "rewarded"
                self.assertEqual(
                    hits, total,
                    f"delay {delay}: the nearest binding before a reward was "
                    f"NOT always the rewarded one ({hits}/{total}). If the "
                    f"generator has been fixed, this test has done its job and "
                    f"should be replaced")

    def test_bindings_sit_on_a_regular_lattice(self):
        """The cause. A constant gap is what puts every unrewarded binding out
        of reach of a reward that is not its own."""
        sequence = self._bindings_and_rewards(8)[0]
        kinds = sequence.position_kinds()
        values = [t for t, k in enumerate(kinds) if k == "value"]
        gaps = {b - a for a, b in zip(values, values[1:])}
        self.assertEqual(
            len(gaps), 1,
            f"bindings are no longer evenly spaced (gaps {sorted(gaps)}), so "
            f"the lattice is broken and the leak above should be gone too")

    def test_the_spacing_exceeds_every_delay_the_sweeps_use(self):
        """Why the leak is exact rather than usual: a delay of at most 20
        cannot reach past a spacing of 31."""
        sequence = self._bindings_and_rewards(8)[0]
        kinds = sequence.position_kinds()
        values = [t for t, k in enumerate(kinds) if k == "value"]
        spacing = values[1] - values[0]
        self.assertGreater(spacing, 20,
                           "the spacing no longer exceeds the largest delay "
                           "any sweep uses, so the leak is not exact")


if __name__ == "__main__":
    unittest.main()