"""Tests of what a quantity MEANS, not of what the code does.

Every other test in this suite checks code against claim: that the code does
what its docstring says, and -- through tools/mutate.py -- that the tests would
notice if it stopped. The surprise bug passed all of it. The code computed a
margin, the docstring said margin, the tests checked margin. Every layer agreed
with every other, and **nothing checked whether "margin" deserved the name
"surprise".**

So these tests state properties that make surprise *that quantity*, in a form
that does not mention how it is computed. They would have caught the bug on the
day it was written, and tools/mutate.py restores the old measure to prove they
still would.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig, surprise)


class TheDefiningProperty(unittest.TestCase):
    """Surprise reads the SHAPE of a prediction, never its size."""

    SCORES = np.array([0.4, 2.1, -0.7, 1.3, 0.0])

    def test_it_depends_on_the_alternatives_and_not_just_on_the_best_one(self):
        """The property that separates surprise from the measure it replaced.

        Both predictions below give the arriving token the same score and have
        the same best score. They differ only in how strong the *rejected*
        alternatives are. A measure that reads two numbers calls these
        identical; surprise must not, because a field of near-ties is a weaker
        prediction than a clear winner.
        """
        contested = np.array([5.0, 1.0, 4.9, 4.9, 4.9])
        decided = np.array([5.0, 1.0, -4.0, -4.0, -4.0])
        self.assertGreater(
            surprise(contested, 1), surprise(decided, 1),
            "a token arriving against four near-ties was not more surprising "
            "than the same token arriving against four rejected alternatives, "
            "so this is reading the best score and ignoring the prediction")

    def test_suppressing_the_alternatives_lowers_surprise(self):
        """What learning does, stated as a property.

        The arriving token's own score never moves. Only its competitors fall --
        which is what a memory does as a pattern repeats. Surprise has to fall
        with them, and this is John's expectation written as arithmetic.
        """
        values = [surprise(np.array([1.0, rival, rival, rival]), 0)
                  for rival in (0.9, 0.5, 0.0, -2.0, -6.0)]
        self.assertEqual(values, sorted(values, reverse=True),
                         f"surprise did not fall as the alternatives were "
                         f"suppressed: {[round(v, 4) for v in values]}")

    def test_adding_a_constant_to_every_score_changes_nothing(self):
        for offset in (-5.0, 3.0, 20.0):
            for token in range(len(self.SCORES)):
                self.assertAlmostEqual(
                    surprise(self.SCORES + offset, token),
                    surprise(self.SCORES, token), places=9)

    def test_a_better_predicted_token_is_less_surprising(self):
        ranked = np.argsort(self.SCORES)[::-1]
        values = [surprise(self.SCORES, int(t)) for t in ranked]
        self.assertEqual(values, sorted(values),
                         "a token the model scored higher came out MORE "
                         "surprising, which inverts the meaning of the word")

    def test_it_is_never_negative(self):
        for token in range(len(self.SCORES)):
            self.assertGreaterEqual(surprise(self.SCORES, token), 0.0)

    def test_a_flat_prediction_is_equally_surprising_whatever_arrives(self):
        flat = np.zeros(6)
        values = {round(surprise(flat, t), 9) for t in range(6)}
        self.assertEqual(len(values), 1)


class TheFailureModeItself(unittest.TestCase):
    """The bug in miniature, with no model involved.

    John's question was why a repeating pattern was not becoming less
    surprising. The cause: as a memory fills it produces the same PREDICTION at
    a larger magnitude, and the old measure grew with the magnitude.

    Reproducing it needs nothing from the model -- only a prediction whose
    alternatives are being suppressed, which is what learning does. No accessor
    for the model's internal scores is added to make this testable; the
    interface stays as small as it was.

    Note what happened when this file was first written: it asserted that
    surprise must be unchanged when every score is multiplied by a constant.
    **That was false** -- scaling scores is a temperature change and genuinely
    alters confidence -- and the test failed on its first run and was corrected.
    A meaning test can assert the wrong meaning. It is a better class of check
    than the ones around it, not a guarantee.
    """

    RIVALS = (0.9, 0.5, 0.0, -2.0, -6.0)

    def _prediction(self, rival: float) -> np.ndarray:
        """The arriving token scores 1.0 throughout. Only its rivals move."""
        return np.array([1.0, rival, rival, rival])

    def test_the_margin_measure_cannot_see_learning_happen(self):
        """Pinning the bug, so the reason for the shape of `surprise` survives.

        Across the whole sweep the arriving token is the best-scored one, so the
        margin is zero at every step and reports no change at all -- while the
        prediction goes from four near-ties to a clear winner. That blindness is
        the bug: what the margin DID move with was the growing size of the
        scores, which is why eight repeats of one identical cycle came out 266%
        more surprising instead of less.
        """
        margins = [float(self._prediction(r).max() - self._prediction(r)[0])
                   for r in self.RIVALS]
        self.assertEqual(set(margins), {0.0},
                         "the margin moved here, so this no longer demonstrates "
                         "what it was blind to")

        real = [surprise(self._prediction(r), 0) for r in self.RIVALS]
        self.assertGreater(
            real[0] - real[-1], 1.0,
            "surprise barely moved either, so this fixture does not show the "
            "difference between the two measures")


if __name__ == "__main__":
    unittest.main()
