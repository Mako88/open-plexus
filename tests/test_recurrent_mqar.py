"""Recurrent MQAR: querying each pair more than once.

The original task asks about each binding exactly once, at the end. Nothing a
system notices at storage time, and nothing it learns from a query, can ever pay
off — a mechanism that consolidates what proved useful has no second occasion to
be useful on. `queries_per_pair` supplies one.

**The first test here is the important one.** Every result in this project was
measured on sequences the previous generator produced, so the default must keep
producing them exactly. A benchmark that drifts silently makes every earlier
number describe a task that no longer exists.
"""

from __future__ import annotations

import hashlib
import unittest
from dataclasses import replace

from openplexus.tasks.mqar import MqarConfig, dataset

BASE = MqarConfig(n_pairs=4, seq_len=96, n_keys=32, n_values=8,
                  autoregressive=True, filler="random", seed=20260726)

# Pinned from the generator as it stood when `queries_per_pair` was added, and
# verified at that moment against the previous implementation across 144
# configurations of pairs, keys, values, layout, filler and length — all
# bit-identical.
GOLDEN_DIGEST = "59d565cf2b778bb0e64c3d107d6684f3e738104c555c963eab420aa9f40e3b67"


class TheDefaultIsTheOldBenchmark(unittest.TestCase):

    def test_default_output_matches_the_pinned_digest(self):
        """If this fails, every earlier result describes a different task.

        Not a style check. The generator is the one thing every number in this
        repository is measured against, so it is pinned rather than trusted. A
        failure here means either the benchmark changed — in which case the
        comparison to every prior sweep is void until re-measured — or the pin
        is stale and needs deliberately re-cutting with that fact recorded.
        """
        blob = "".join(
            f"{s.tokens}{s.targets}{s.query_positions}{s.answer_positions}"
            for s in dataset(BASE, 20))
        self.assertEqual(hashlib.sha256(blob.encode()).hexdigest(),
                         GOLDEN_DIGEST,
                         "the default benchmark changed")

    def test_one_query_per_pair_is_the_default(self):
        self.assertEqual(BASE.queries_per_pair, 1)
        sequence = dataset(BASE, 1)[0]
        self.assertEqual(len(sequence.query_positions), BASE.n_pairs)


class RepeatsMakeRelevanceRecur(unittest.TestCase):

    def test_each_key_is_queried_exactly_the_requested_number_of_times(self):
        for repeats in (2, 3, 4):
            with self.subTest(repeats=repeats):
                config = replace(BASE, seq_len=192, queries_per_pair=repeats)
                for sequence in dataset(config, 20):
                    counts = {}
                    for q in sequence.query_positions:
                        key = sequence.tokens[q]
                        counts[key] = counts.get(key, 0) + 1
                    self.assertEqual(len(counts), config.n_pairs)
                    self.assertEqual(set(counts.values()), {repeats})

    def test_every_query_of_a_key_has_the_same_answer(self):
        """Otherwise repeats make the task ill-posed instead of recurrent.

        This is the property consolidation depends on: the whole point is that
        remembering what a key resolved to the first time is worth something the
        next time. If the answer moved, remembering would be actively harmful.
        """
        config = replace(BASE, seq_len=192, queries_per_pair=3)
        for sequence in dataset(config, 40):
            answers = {}
            for q in sequence.query_positions:
                key, value = sequence.tokens[q], sequence.targets[q]
                if key in answers:
                    self.assertEqual(answers[key], value,
                                     f"key {key} has two different answers")
                answers[key] = value

    def test_the_query_count_scales_with_repeats(self):
        for repeats in (1, 2, 4):
            with self.subTest(repeats=repeats):
                config = replace(BASE, seq_len=192, queries_per_pair=repeats)
                sequence = dataset(config, 1)[0]
                self.assertEqual(len(sequence.query_positions),
                                 config.n_pairs * repeats)

    def test_repeats_do_not_change_how_many_pairs_are_presented(self):
        """A repeat is another question, not another fact.

        If repeats also multiplied the bindings, the task would get harder in
        the way this project already measures — more to remember — and the
        recurrence effect would be confounded with plain interference.
        """
        for repeats in (1, 3):
            with self.subTest(repeats=repeats):
                config = replace(BASE, seq_len=192, queries_per_pair=repeats)
                sequence = dataset(config, 1)[0]
                self.assertEqual(len(sequence.pairs), config.n_pairs)


class ImpossibleConfigurationsAreRefused(unittest.TestCase):

    def test_repeats_must_be_at_least_one(self):
        with self.assertRaises(ValueError):
            MqarConfig(n_pairs=4, seq_len=96, queries_per_pair=0)

    def test_the_minimum_length_accounts_for_repeats(self):
        base = MqarConfig(n_pairs=4, seq_len=96, autoregressive=True)
        more = replace(base, seq_len=96, queries_per_pair=3)
        self.assertGreater(more.min_seq_len, base.min_seq_len)
        with self.assertRaises(ValueError):
            replace(base, seq_len=more.min_seq_len - 1, queries_per_pair=3)


if __name__ == "__main__":
    unittest.main()
