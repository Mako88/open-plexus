"""The two-task split, which was ill-posed on its first attempt.

`remap` sends generated sequences into one task's private half of the model's
vocabulary. Its first version folded a 32-key alphabet down to 16 with a modulo,
which made keys `k` and `k + 16` the same token: in 600 sampled sequences every
one contained two distinct keys that collided, and 82 of 2400 queries had two
different correct answers.

That is g1-01's bug returning -- a benchmark that cannot be answered, producing
numbers that look like results. These tests are what stops it returning again.
"""

from __future__ import annotations

import unittest
from dataclasses import replace

import numpy as np

from experiments.g6_01_forgetting import BASE, GEN, remap
from openplexus.tasks.mqar import dataset


class TheSplitTaskIsAnswerable(unittest.TestCase):

    def test_no_query_has_two_different_answers(self):
        """The property whose absence made the first version worthless."""
        for half in (0, 1):
            for sequence in dataset(replace(GEN, seed=GEN.seed + half), 100):
                mapped = remap(np.asarray(sequence.tokens), half)
                answers = {}
                for q in sequence.query_positions:
                    key, value = int(mapped[q]), int(mapped[q + 1])
                    if key in answers:
                        self.assertEqual(
                            answers[key], value,
                            f"key {key} has two answers, so the task cannot be "
                            f"answered and any score on it is meaningless")
                    answers[key] = value

    def test_distinct_keys_stay_distinct(self):
        """The same ambiguity one step earlier, and easier to localise."""
        for half in (0, 1):
            for sequence in dataset(replace(GEN, seed=GEN.seed + half), 100):
                tokens = np.asarray(sequence.tokens)
                mapped = remap(tokens, half)
                seen = {}
                for original, sent in zip(tokens, mapped):
                    if original >= GEN.n_keys:
                        continue
                    if int(sent) in seen:
                        self.assertEqual(seen[int(sent)], int(original),
                                         "two distinct keys folded together")
                    seen[int(sent)] = int(original)


class TheTwoTasksShareNothing(unittest.TestCase):
    """Otherwise "switching between problems" is switching between one problem."""

    def _alphabets(self, half):
        keys, values = set(), set()
        for sequence in dataset(replace(GEN, seed=GEN.seed + 11 * half), 100):
            for token in remap(np.asarray(sequence.tokens), half):
                if token < BASE.n_keys:
                    keys.add(int(token))
                elif token < BASE.n_keys + BASE.n_values:
                    values.add(int(token))
        return keys, values

    def test_keys_and_values_are_both_disjoint(self):
        keys_a, values_a = self._alphabets(0)
        keys_b, values_b = self._alphabets(1)
        self.assertFalse(keys_a & keys_b, "the tasks share keys, so they "
                                          "address the same memory columns")
        self.assertFalse(values_a & values_b, "the tasks share values, so "
                                              "there is nothing to forget")
        self.assertTrue(keys_a and keys_b and values_a and values_b)

    def test_everything_lands_inside_the_model_vocabulary(self):
        for half in (0, 1):
            for sequence in dataset(replace(GEN, seed=GEN.seed + half), 50):
                mapped = remap(np.asarray(sequence.tokens), half)
                self.assertLess(int(mapped.max()), BASE.vocab_size)
                self.assertGreaterEqual(int(mapped.min()), 0)

    def test_the_map_is_injective(self):
        """A bijection, not a fold -- stated directly rather than inferred."""
        for half in (0, 1):
            source = np.arange(GEN.n_keys + GEN.n_values)
            sent = remap(source, half)
            self.assertEqual(len(set(sent.tolist())), len(source))


if __name__ == "__main__":
    unittest.main()
