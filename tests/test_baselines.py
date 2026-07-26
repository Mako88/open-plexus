"""Tests for the non-learning baselines.

The most important one here is `test_oracle_is_perfect_across_the_grid`. It is a
connection test on the *task* rather than on any model: if perfect information
cannot answer the question, the question is unanswerable, and every number
measured on it would be void while looking like an honest null.

That check is written over a grid rather than one configuration on purpose —
rule 12, fix the class rather than the instance. The defect it guards against
appeared in one filler mode at one size; a single-config assertion would have
caught that one instance and let the next through.
"""

from __future__ import annotations

import unittest
from dataclasses import replace
from itertools import product

from openplexus import baselines
from openplexus.tasks.mqar import MqarConfig, dataset, generate

BASE = MqarConfig(n_pairs=4, seq_len=40, n_keys=16, n_values=8, seed=11)


def grid():
    """Configurations spanning every dial, for class-level assertions."""
    for filler, n_pairs, n_values, seq_len in product(
        ("none", "random", "structured"), (1, 4, 8), (2, 8), (40, 90)
    ):
        yield replace(BASE, filler=filler, n_pairs=n_pairs,
                      n_values=n_values, seq_len=seq_len)


class TestTaskIsAnswerable(unittest.TestCase):
    def test_oracle_is_perfect_across_the_grid(self):
        """Perfect information must score exactly 1.0 in every configuration.

        Anything less means a query is unanswerable — the same input requiring
        two different outputs, or a query with no pair. The generator's first
        ever output failed this: filler could be byte-identical to a query token
        while requiring silence instead of an answer.
        """
        for config in grid():
            with self.subTest(config=config):
                self.assertEqual(
                    baselines.accuracy(baselines.oracle, dataset(config, 12)), 1.0
                )


class TestBaselinesAreHonest(unittest.TestCase):
    def test_baselines_do_not_read_the_future(self):
        """A prediction at position i must not depend on tokens after i.

        A baseline that peeks would score well for a reason that has nothing to
        do with the task, and would silently raise the floor every model is
        compared against.
        """
        config = replace(BASE, seq_len=60)
        seqs = dataset(config, 8)
        cases = {
            "constant": baselines.fit_constant(seqs, config),
            "most_recent_value": baselines.most_recent_value(config),
            "positional": baselines.positional(config),
        }
        seq = seqs[0]
        cut = seq.query_positions[0]
        scrambled = replace_tokens(
            seq, seq.tokens[: cut + 1] + tuple(reversed(seq.tokens[cut + 1:]))
        )
        for name, baseline in cases.items():
            with self.subTest(baseline=name):
                self.assertEqual(baseline(seq, cut), baseline(scrambled, cut))

    def test_constant_baseline_answers_the_same_thing_every_time(self):
        seqs = dataset(BASE, 20)
        constant = baselines.fit_constant(seqs, BASE)
        answers = {constant(s, p) for s in seqs for p in s.query_positions}
        self.assertEqual(len(answers), 1)

    def test_constant_baseline_picks_the_mode_not_an_arbitrary_value(self):
        """`fit_constant`'s contract is *most common*, and accuracy cannot check it.

        Values are drawn uniformly, so every constant scores about 1/n_values —
        the accuracy is pinned by the value distribution rather than by which
        value the baseline chose. Rule 9's failure mode exactly: an assertion on
        a quantity that something else holds fixed.

        The mutation `constant-baseline-not-fitted` (replacing the mode with the
        smallest value id) **survived the whole suite** until this test existed.
        It is asserted on deliberately skewed data, where the two differ.
        """
        skewed = [_sequence_with_target(20)] * 9 + [_sequence_with_target(17)] * 2
        constant = baselines.fit_constant(skewed, BASE)
        self.assertEqual(constant(skewed[0], 2), 20)

    def test_constant_baseline_tracks_the_value_alphabet(self):
        """The base rate must move when n_values moves. A baseline that ignored
        it would make every comparison wrong in the same direction."""
        narrow = replace(BASE, n_values=2)
        wide = replace(BASE, n_values=8)
        acc_narrow = baselines.accuracy(
            baselines.fit_constant(dataset(narrow, 200), narrow), dataset(narrow, 200))
        acc_wide = baselines.accuracy(
            baselines.fit_constant(dataset(wide, 200), wide), dataset(wide, 200))
        self.assertGreater(acc_narrow, acc_wide)

    def test_shortcut_baselines_score_the_predicted_trivial_floor(self):
        """The real floor a model must clear, and it is not the base rate.

        The first version of this test asserted only `< 0.5` at one
        configuration. That passed, and it was nearly vacuous: the shortcuts
        actually score 0.349 against a base rate of 0.134 at the reference, and
        a bound of 0.5 admits everything from "no better than guessing" to
        "solves a third of the task". It also would not have noticed the
        degenerate case below.

        Asserting the closed form instead makes this a real claim about the
        generator: shortcuts win when they name the queried pair (1/n_pairs) or
        when another pair carries the same value ((1-1/n_pairs)/n_values). Fits
        to within 0.016 across eight conditions.
        """
        for n_pairs, n_values in ((2, 8), (4, 8), (8, 8), (4, 2), (4, 16)):
            config = replace(BASE, n_pairs=n_pairs, n_values=n_values, seq_len=96)
            seqs = dataset(config, 400)
            for name, baseline in (
                ("most_recent_value", baselines.most_recent_value(config)),
                ("positional", baselines.positional(config)),
            ):
                with self.subTest(baseline=name, n_pairs=n_pairs, n_values=n_values):
                    self.assertAlmostEqual(
                        baselines.accuracy(baseline, seqs),
                        config.trivial_floor, delta=0.05,
                    )

    def test_a_single_pair_makes_the_task_trivially_solvable(self):
        """Documented so nobody reports a result at n_pairs=1 and believes it.

        With one pair there is one value token in the sequence, so emitting any
        value seen is always correct. Both shortcuts score exactly 1.0, and
        `trivial_floor` says so rather than leaving it to be discovered.
        """
        config = replace(BASE, n_pairs=1, seq_len=40)
        seqs = dataset(config, 50)
        self.assertEqual(config.trivial_floor, 1.0)
        self.assertEqual(baselines.accuracy(baselines.most_recent_value(config), seqs), 1.0)
        self.assertEqual(baselines.accuracy(baselines.positional(config), seqs), 1.0)

    def test_the_floor_falls_as_more_pairs_are_queried(self):
        """Why n_pairs is the load-bearing dial, now for a second reason.

        docs/notes/006 established that multi-query is what makes the task
        discriminating at all. This adds a measured reason: n_pairs also sets
        how far a model must beat a one-line heuristic. At 4 pairs the floor is
        0.344; at 16 it is 0.180.
        """
        wide = replace(BASE, n_keys=64, seq_len=96)
        floors = [replace(wide, n_pairs=k).trivial_floor for k in (2, 4, 8, 16)]
        self.assertEqual(floors, sorted(floors, reverse=True))
        self.assertAlmostEqual(replace(wide, n_pairs=16).trivial_floor, 0.180, delta=0.01)


class TestAccuracy(unittest.TestCase):
    def test_scores_only_query_positions(self):
        """Scoring the whole sequence would hand every baseline a large number of
        free correct answers and compress them all together."""
        config = replace(BASE, n_pairs=2, seq_len=60)
        seqs = dataset(config, 5)
        scored = sum(len(s.query_positions) for s in seqs)
        self.assertEqual(scored, 2 * 5)
        self.assertEqual(baselines.accuracy(baselines.oracle, seqs), 1.0)

    def test_rejects_empty_input(self):
        with self.assertRaises(ValueError):
            baselines.accuracy(baselines.oracle, [])


def replace_tokens(sequence, tokens):
    from openplexus.tasks.mqar import MqarSequence
    return MqarSequence(tokens=tokens, targets=sequence.targets,
                        pairs=sequence.pairs,
                        query_positions=sequence.query_positions)


def _sequence_with_target(value_token: int):
    """A minimal well-formed sequence whose single query expects `value_token`.

    Built directly rather than generated, so the target distribution can be
    skewed on purpose — the generator only produces uniform values, which is
    what hides an unfitted base rate.
    """
    from openplexus.tasks.mqar import IGNORE, MqarSequence
    return MqarSequence(tokens=(0, value_token, 0),
                        targets=(IGNORE, IGNORE, value_token),
                        pairs={0: value_token}, query_positions=(2,))


if __name__ == "__main__":
    unittest.main()
