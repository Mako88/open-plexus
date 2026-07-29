"""The set-answer ruler, including the falsifier that must never score well.

ARCHITECTURE row F3: nothing in this project has ever scored a multi-token answer.
`openplexus/answers.py` is the ruler for that, and it is built and tested before
any mechanism produces such an answer, because a measurement convention is the
thing this project has repeatedly got wrong first -- decision 138 is a wrong
target surviving four sweeps and 142 cells.

Two of these tests are load-bearing in a way the rest are not:

`EmittingEverythingMustNotScoreWell` is the standing falsifier. The laziest
possible mechanism -- answer with the whole value alphabet -- achieves PERFECT
RECALL. If a change ever makes that arm look good, the change is wrong however
good the headline reads.

`TheConventionReproducesTheOldNumber` is decision 138's gate. Every accuracy in
this repository is `predicted == truth` over query positions; if the new
convention does not return that exact number on single-token answers, every
result measured before it stops being comparable to every result after it.
"""

from __future__ import annotations

import unittest

from openplexus.answers import (
    score_one, single_token_accuracy, summarise)


class ScoringOneAnswer(unittest.TestCase):

    def test_an_exact_match_scores_one_everywhere(self):
        score = score_one([4, 7], [7, 4])
        self.assertTrue(score.exact)
        self.assertEqual(score.precision, 1.0)
        self.assertEqual(score.recall, 1.0)
        self.assertEqual(score.f1, 1.0)

    def test_order_does_not_matter(self):
        # An answer is a SET. A traversal visits concepts in whatever order the
        # walk reaches them, and penalising that order would measure the walk's
        # sequencing rather than what it found.
        self.assertEqual(score_one([1, 2, 3], [3, 2, 1]),
                         score_one([3, 1, 2], [1, 2, 3]))

    def test_a_repeated_token_is_not_two_answers(self):
        # A traversal that revisits a concept would otherwise inflate its own
        # recall by emitting the same hit twice.
        self.assertEqual(score_one([5, 5, 5], [5]), score_one([5], [5]))

    def test_half_right_scores_half(self):
        score = score_one([1, 2], [1, 3])
        self.assertFalse(score.exact)
        self.assertEqual(score.precision, 0.5)
        self.assertEqual(score.recall, 0.5)
        self.assertEqual(score.f1, 0.5)

    def test_missing_one_costs_recall_and_not_precision(self):
        # Under-emitting and over-emitting are DIFFERENT defects, and F1 alone
        # cannot tell them apart. This is why the pair is carried.
        score = score_one([1], [1, 2])
        self.assertEqual(score.precision, 1.0)
        self.assertEqual(score.recall, 0.5)

    def test_one_too_many_costs_precision_and_not_recall(self):
        score = score_one([1, 2], [1])
        self.assertEqual(score.precision, 0.5)
        self.assertEqual(score.recall, 1.0)

    def test_a_disjoint_answer_scores_zero(self):
        score = score_one([8, 9], [1, 2])
        self.assertFalse(score.exact)
        self.assertEqual(score.f1, 0.0)

    def test_an_empty_prediction_scores_zero_rather_than_raising(self):
        # Declining to answer is a real behaviour (ARCHITECTURE row C4) and it
        # has to be scoreable, unlike an empty TRUE set which is undefined.
        score = score_one([], [1, 2])
        self.assertEqual(score.precision, 0.0)
        self.assertEqual(score.recall, 0.0)
        self.assertEqual(score.f1, 0.0)

    def test_an_empty_TRUE_set_is_refused(self):
        # Scoring it 1.0 for an empty prediction would let questions with no
        # answer raise the mean, which is a metric improving because the task got
        # easier in a way nobody declared.
        with self.assertRaises(ValueError):
            score_one([], [])


class EmittingEverythingMustNotScoreWell(unittest.TestCase):
    """THE STANDING FALSIFIER. Perfect recall, and it must not look good."""

    #: An eight-value alphabet, which is `families.py`'s default `n_values`.
    ALPHABET = tuple(range(8))

    def test_emitting_everything_gets_perfect_recall(self):
        # Stated positively FIRST, because the point is that the trap is real
        # rather than hypothetical. This is what a recall-only headline would
        # have reported for a mechanism that does nothing at all.
        score = score_one(self.ALPHABET, [3, 5])
        self.assertEqual(score.recall, 1.0)

    def test_and_is_not_exact(self):
        self.assertFalse(score_one(self.ALPHABET, [3, 5]).exact)

    def test_and_scores_badly_on_f1(self):
        score = score_one(self.ALPHABET, [3, 5])
        self.assertLess(score.f1, 0.45)

    def test_and_the_summary_shows_the_over_emission(self):
        # `mean_size` against `mean_truth_size` is where guessing-more shows up.
        # A mechanism that bought F1 this way is invisible in the headline and
        # obvious here, which is the reason both are carried.
        summary = summarise([score_one(self.ALPHABET, [3, 5]),
                             score_one(self.ALPHABET, [1, 2])])
        self.assertEqual(summary.mean_size, 8.0)
        self.assertEqual(summary.mean_truth_size, 2.0)
        self.assertEqual(summary.exact, 0.0)


class TheConventionReproducesTheOldNumber(unittest.TestCase):
    """Decision 138's gate: the single-token case must be the old accuracy."""

    #: Seven query positions, four of them answered correctly. Under the old
    #: convention this is `4 / 7`, and nothing about the new one may change it.
    PREDICTED = ([3], [5], [1], [7], [2], [4], [6])
    TRUTH = ([3], [5], [9], [7], [9], [4], [9])

    def test_singleton_exact_is_the_old_accuracy(self):
        scores = [score_one(p, t) for p, t in zip(self.PREDICTED, self.TRUTH)]
        old = sum(1 for p, t in zip(self.PREDICTED, self.TRUTH)
                  if p[0] == t[0]) / len(self.TRUTH)
        self.assertEqual(old, 4 / 7)
        self.assertEqual(summarise(scores).exact, old)
        self.assertEqual(single_token_accuracy(scores), old)

    def test_f1_and_exact_agree_on_singletons(self):
        # On one-token answers precision, recall, F1 and exact are the same
        # quantity, so a task that has not changed its questions cannot report a
        # different number depending on which column it is read from.
        scores = [score_one(p, t) for p, t in zip(self.PREDICTED, self.TRUTH)]
        summary = summarise(scores)
        self.assertEqual(summary.mean_f1, summary.exact)
        self.assertEqual(summary.mean_precision, summary.exact)
        self.assertEqual(summary.mean_recall, summary.exact)

    def test_every_answer_is_flagged_a_singleton(self):
        scores = [score_one(p, t) for p, t in zip(self.PREDICTED, self.TRUTH)]
        self.assertEqual(summarise(scores).singletons, len(scores))

    def test_recovering_an_accuracy_from_set_answers_is_refused(self):
        # Averaging a set score into a column labelled "accuracy" is how a number
        # stops meaning what its heading says -- rule 8, at the point where the
        # statistic crosses from where it was gathered to where it is used.
        with self.assertRaises(ValueError):
            single_token_accuracy([score_one([1, 2], [1, 2])])


class SummarisingRefusesToInventANumber(unittest.TestCase):

    def test_an_empty_summary_is_refused(self):
        # A zero here is indistinguishable from a mechanism that scored zero --
        # the accumulator reporting its own initial value.
        with self.assertRaises(ValueError):
            summarise([])

    def test_the_count_is_carried(self):
        # So an aggregate can say what it is a statistic OF.
        self.assertEqual(summarise([score_one([1], [1]),
                                    score_one([2], [3])]).n, 2)

    def test_mixed_sizes_are_summarised_without_hiding_the_mix(self):
        summary = summarise([score_one([1], [1]),
                             score_one([2, 3, 4], [2, 3, 4])])
        self.assertEqual(summary.exact, 1.0)
        self.assertEqual(summary.mean_truth_size, 2.0)
        # One of the two is the old single-token case, and the summary says so
        # rather than averaging it away.
        self.assertEqual(summary.singletons, 1)


class TheRulerTakesNoDependencies(unittest.TestCase):

    def test_answers_does_not_import_numpy(self):
        # CLAUDE.md's convention: the task and measurement layer is the ruler and
        # the ruler stays dependency-free (note 007). Asserted rather than
        # trusted, because an import added later would be invisible.
        #
        # PARSED RATHER THAN GREPPED, and the first version was grepped. It
        # searched the source for "import numpy" and failed on the module's own
        # docstring, which contains the sentence "the ruler does not import
        # numpy". A substring search over source answers a different question
        # from the one being asked -- rule 8, at the smallest possible scale.
        import ast
        import pathlib
        tree = ast.parse((pathlib.Path(__file__).resolve().parents[1]
                          / "openplexus" / "answers.py")
                         .read_text(encoding="utf-8"))
        imported = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                imported.update(alias.name.split(".")[0]
                                for alias in node.names)
            elif isinstance(node, ast.ImportFrom) and node.module:
                imported.add(node.module.split(".")[0])
        self.assertNotIn("numpy", imported)
        # A COMPANION THAT SOMETHING DID CHANGE. An assertion that a set does not
        # contain an item passes trivially when the set is empty, which is
        # exactly the case where the parse silently found nothing.
        self.assertIn("dataclasses", imported)


if __name__ == "__main__":
    unittest.main()
