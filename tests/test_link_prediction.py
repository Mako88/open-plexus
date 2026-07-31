"""The ranking convention `g30-01` and `g30-02` are both scored under.

**This does not duplicate** either experiment's own run, which needs `data/fb15k237/`
and answers *"what does it score"*. These assert the property the score depends on, on
hand-built matrices, so they run in CI where `data/` is gitignored and absent -- the same
split `tests/test_relation_contrastive.py` uses.

## Why the tie test is the important one

`frequency` -- the opponent both experiments lose to -- ignores the head entirely and
scores most candidates at exactly zero. So its ranking is one enormous tied block, and a
convention that counts ties as wins hands it a number it did not earn. The two arms are
not equally exposed: a learned scorer separates candidates continuously and almost never
ties. **A tie convention chosen inside the ranking loop would therefore have decided the
comparison silently, in the baseline's favour**, which is the reverse of the usual
worry and just as fatal.
"""

from __future__ import annotations

import unittest

import numpy as np

from tools.link_prediction import metrics, ranks


class TiesAreReportedRatherThanDecided(unittest.TestCase):

    def test_a_constant_scorer_is_first_and_last_at_once(self):
        """The failure the pair exists to catch, in its purest form.

        A scorer returning the same value everywhere knows nothing. Counting ties as
        wins ranks its answer 1st out of five; counting them as losses ranks it 5th.
        **Reporting only the first would make an empty scorer look perfect**, and the
        untrained gate arm in `g30-02` is precisely such a scorer.
        """
        scores = np.zeros((3, 5))
        best, tied = ranks(scores, [0, 2, 4])
        self.assertTrue((best == 1).all())
        self.assertTrue((tied == 5).all())

    def test_a_separating_scorer_gives_the_SAME_number_both_ways(self):
        """The companion. Without it the test above passes on a broken rank function.

        With no ties there is nothing to break, so the two conventions must agree
        exactly -- which is what makes a divergence in a real run mean something.
        """
        scores = np.array([[0.9, 0.5, 0.1], [0.1, 0.9, 0.5]])
        best, tied = ranks(scores, [0, 1])
        self.assertTrue((best == tied).all())
        self.assertTrue((best == 1).all())

    def test_the_rank_counts_only_what_OUTSCORES_the_target(self):
        """What breaks if this stops holding: every MRR in the g30 family.

        Two candidates above the target and two below is rank 3, not rank 2 and not
        rank 4. Off by one here moves `1/rank` by a third at the top of the table,
        where almost all of MRR lives.
        """
        scores = np.array([[5.0, 4.0, 3.0, 2.0, 1.0]])
        best, tied = ranks(scores, [2])
        self.assertEqual(int(best[0]), 3)
        self.assertEqual(int(tied[0]), 3)

    def test_a_filtered_out_candidate_cannot_beat_the_target(self):
        """`-inf` is how `evaluate` removes other known-true tails from the ranking.

        A filtered candidate must not count against the target under EITHER
        convention, including the pessimistic one where `>=` would catch an equal
        score.
        """
        scores = np.array([[9.0, -np.inf, 1.0, -np.inf]])
        best, tied = ranks(scores, [2])
        self.assertEqual(int(best[0]), 2)
        self.assertEqual(int(tied[0]), 2)

    def test_metrics_read_the_ranks_they_are_given(self):
        """MRR is the mean of `1/rank`, and Hits@k counts ranks at or below k.

        Pinned because a Hits@k written as `<` rather than `<=` is a silent one-place
        shift that still produces a plausible-looking table.
        """
        mrr, h1, h3, h10 = metrics(np.array([1.0, 3.0, 10.0, 20.0]))
        self.assertAlmostEqual(mrr, (1 + 1 / 3 + 1 / 10 + 1 / 20) / 4)
        self.assertAlmostEqual(h1, 0.25)
        self.assertAlmostEqual(h3, 0.5)
        self.assertAlmostEqual(h10, 0.75)


if __name__ == "__main__":
    unittest.main()
