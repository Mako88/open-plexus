"""The external trials must be read as the experiments posed them.

`xsl.py` is the ruler for `g34-01`, and a ruler that mis-reads someone else's
stimuli produces a number about a task nobody ran. The load-bearing property is
that **a trial presents words and objects UNPAIRED**: if word `n` and object `n`
were the only two things that co-occurred on a trial, the answer would be handed
over and the whole cross-situational difficulty would be gone.
"""

from __future__ import annotations

import pathlib
import tempfile
import unittest

from openplexus.tasks import xsl

#: Two trials, three pairs each, in the published format.
SAMPLE = "5\t12\t1\n15\t8\t14\n"


def _condition(text: str = SAMPLE, name: str = "sample") -> xsl.Condition:
    with tempfile.TemporaryDirectory() as folder:
        path = pathlib.Path(folder) / f"{name}.txt"
        path.write_text(text, encoding="utf-8")
        return xsl.read(path)


class WhatATrialPresents(unittest.TestCase):

    def test_a_trial_shows_every_word_AND_every_object(self):
        """Six surfaces from three pairs. The pairing is what must be learned."""
        condition = _condition("1\t2\t3\n")
        self.assertEqual(len(condition.trials[0]), 6)

    def test_a_word_is_not_its_own_object(self):
        condition = _condition("1\t2\t3\n")
        for pair in (1, 2, 3):
            self.assertNotEqual(condition.word(pair), condition.object(pair))

    def test_every_word_co_occurs_with_every_object_on_the_trial(self):
        """THE property. Word 1 is equally consistent with objects 1, 2 and 3 on
        this trial, and only later trials separate them. A reader that emitted
        only the correct pairing would hand over the answer."""
        condition = _condition("1\t2\t3\n")
        present = set(condition.trials[0])
        for pair in (1, 2, 3):
            self.assertIn(condition.word(pair), present)
            self.assertIn(condition.object(pair), present)

    def test_the_true_mapping_is_one_word_to_one_object(self):
        condition = _condition("1\t2\t3\n")
        truth = condition.classes()
        self.assertEqual(truth[condition.word(1)],
                         frozenset({condition.word(1), condition.object(1)}))
        self.assertEqual(len(truth[condition.word(2)]), 2)

    def test_ids_start_at_zero_however_the_file_numbers_its_pairs(self):
        """The published files are 1-based; the recovery code indexes from 0."""
        condition = _condition("1\t2\n")
        self.assertEqual(sorted(condition.trials[0]), [0, 1, 2, 3])

    def test_surfaces_is_words_plus_objects(self):
        self.assertEqual(_condition("1\t2\t3\n").surfaces(), 6)

    def test_appearances_counts_trials_not_surfaces(self):
        condition = _condition("1\t2\n1\t3\n")
        counts = condition.appearances()
        self.assertEqual(counts[condition.word(1)], 2)
        self.assertEqual(counts[condition.word(2)], 1)


class Refusals(unittest.TestCase):

    def test_a_file_with_no_trials_is_refused(self):
        with self.assertRaises(ValueError):
            _condition("\n\n")

    def test_pair_ids_with_a_GAP_are_refused(self):
        """A gap means the file is not this format, and reading it anyway would
        invent surfaces for pairs that were never shown."""
        with self.assertRaises(ValueError):
            _condition("1\t2\n1\t9\n")


class TheFetchedData(unittest.TestCase):
    """Skipped when the data is absent, because CI has no fetch step for it."""

    DATA = pathlib.Path(__file__).resolve().parents[1] / "data" / "kachergis"

    def setUp(self) -> None:
        if not xsl.available(self.DATA):
            self.skipTest("run tools/fetch_kachergis.py first")

    def test_every_fetched_condition_reads(self):
        for path in xsl.available(self.DATA):
            condition = xsl.read(path)
            self.assertGreater(condition.pairs, 1, path.name)
            self.assertGreater(len(condition.trials), 1, path.name)

    def test_available_is_ordered_the_same_way_twice(self):
        self.assertEqual(xsl.available(self.DATA), xsl.available(self.DATA))


if __name__ == "__main__":
    unittest.main()
