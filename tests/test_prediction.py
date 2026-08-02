"""Prediction — the first mechanism here that can be wrong.

The load-bearing tests are that the score is taken BEFORE the count (otherwise
the model sees the answer first and improves forever without learning), and that
`bound` can express an interaction `factored` cannot, which is the whole reason
both exist.
"""

from __future__ import annotations

import random
import unittest

from openplexus.prediction import BINDINGS, Predictor
from openplexus.tasks.snake import ACTIONS, Snake


def _play(binding, steps=3000, seed=0):
    """Random play, scored prequentially. Returns (first, last) hit rates."""
    world = Snake(width=8, height=8, sight=2, seed=seed)
    predictor = Predictor(actions=len(ACTIONS), binding=binding)
    rng = random.Random(seed)
    state = hash(world.view())
    hits = []
    for _ in range(steps):
        action = rng.randrange(len(ACTIONS))
        actual = hash(world.step(action).view)
        hits.append(predictor.hit(state, action, actual))
        predictor.learn(state, action, actual)
        state = actual
    window = len(hits) // 6
    return (sum(hits[:window]) / window, sum(hits[-window:]) / window)


class TheScoreIsTakenBeforeTheCount(unittest.TestCase):
    """Prequential, which is decision 10 and the only honest order.

    Counting first would let the model see the answer before being asked about
    it, and the error would fall forever without anything being learned.
    """

    def test_the_first_sighting_is_scored_as_a_miss(self):
        predictor = Predictor(actions=4)
        self.assertFalse(predictor.hit(state := 7, 0, 99))
        predictor.learn(state, 0, 99)
        self.assertTrue(predictor.hit(state, 0, 99))

    def test_learning_the_same_thing_twice_lowers_the_surprise(self):
        predictor = Predictor(actions=4)
        first = predictor.learn(5, 1, 42)
        second = predictor.learn(5, 1, 42)
        self.assertLess(second, first)

    def test_learn_returns_what_surprise_would_have_returned(self):
        """The companion: if `learn` scored AFTER counting these would differ."""
        predictor = Predictor(actions=4)
        predictor.learn(5, 1, 42)
        expected = predictor.surprise(5, 1, 42)
        self.assertAlmostEqual(predictor.learn(5, 1, 42), expected)


class ErrorFallsOnAWorldThatDoesNotChange(unittest.TestCase):
    """The connection test. Snake's dynamics never change, so a mechanism that
    is connected to them must get better at them."""

    def test_the_hit_rate_rises_with_evidence(self):
        first, last = _play("bound")
        self.assertGreater(last, first)
        self.assertGreater(last, 0.55)

    def test_a_shuffled_stream_does_NOT_improve(self):
        """The control, and it tests the DATA rather than the code: if the
        transitions carry no structure there is nothing to learn, and a
        mechanism reporting improvement anyway is reporting its own smoothing.
        """
        rng = random.Random(0)
        predictor = Predictor(actions=len(ACTIONS))
        hits = []
        for _ in range(3000):
            state, action = rng.randrange(300), rng.randrange(len(ACTIONS))
            actual = rng.randrange(300)
            hits.append(predictor.hit(state, action, actual))
            predictor.learn(state, action, actual)
        self.assertLess(sum(hits[-500:]) / 500, 0.1)


class BindingExpressesAnInteractionThatFactoringCannot(unittest.TestCase):
    """Why both arms exist. The same action does different things in different
    states, and a factored score has to like the action on its own."""

    def _interaction(self, binding):
        """Two states, two actions, outcomes that depend on the PAIR."""
        predictor = Predictor(actions=2, binding=binding)
        for _ in range(50):
            predictor.learn(0, 0, 100)
            predictor.learn(0, 1, 200)
            predictor.learn(1, 0, 200)
            predictor.learn(1, 1, 100)
        return [predictor.hit(state, action, want) for state, action, want in
                ((0, 0, 100), (0, 1, 200), (1, 0, 200), (1, 1, 100))]

    def test_bound_gets_every_cell_right(self):
        self.assertEqual(self._interaction("bound"), [True] * 4)

    def test_factored_does_not(self):
        """The companion. Without it, `bound` succeeding would be untested
        against the alternative actually failing."""
        self.assertNotEqual(self._interaction("factored"), [True] * 4)


class SurpriseAndHitDisagreeAndBothAreReported(unittest.TestCase):

    def test_surprise_can_rise_while_the_hit_rate_rises_too(self):
        """A growing alphabet raises surprise on its own, which is why `hit` is
        the companion. A first version normalised by a sum of `conditional`
        scores rather than by counts, and reported a world with unchanging
        dynamics getting steadily more surprising over 4,000 steps.
        """
        predictor = Predictor(actions=2)
        predictor.learn(0, 0, 100)
        alone = predictor.surprise(0, 0, 100)
        for new in range(200, 260):
            predictor.learn(1, 0, new)
        self.assertGreater(predictor.surprise(0, 0, 100), alone)
        self.assertTrue(predictor.hit(0, 0, 100))


class InstancesDoNotShareState(unittest.TestCase):
    """`_history` was a class attribute once. Two arms in one sweep replayed
    each other's observations into their own table and both still ran."""

    def test_two_predictors_keep_their_own_history(self):
        one = Predictor(actions=2, binding="factored")
        two = Predictor(actions=2, binding="factored")
        for _ in range(20):
            one.learn(0, 0, 100)
        self.assertFalse(two.hit(0, 0, 100))


class ArgumentsAreRefused(unittest.TestCase):

    def test_an_unknown_binding_is_refused(self):
        with self.assertRaises(ValueError):
            Predictor(actions=2, binding="woven")

    def test_a_world_with_no_actions_is_refused(self):
        with self.assertRaises(ValueError):
            Predictor(actions=0)

    def test_an_action_outside_the_range_is_refused(self):
        with self.assertRaises(ValueError):
            Predictor(actions=2).learn(0, 5, 1)

    def test_every_binding_runs(self):
        for binding in BINDINGS:
            predictor = Predictor(actions=2, binding=binding)
            predictor.learn(0, 0, 9)
            self.assertIsInstance(predictor.surprise(0, 0, 9), float)


class AdaptiveIsMemoryWhereItHasSomeAndGeneralisationWhereItDoesNot(
        unittest.TestCase):
    """What the counterfactual measurement earned: the two arms are not
    better and worse, so take the bound surface where it has evidence and
    fall back to factoring where it has none. No threshold — "has this pair
    ever been counted" is a question with an answer."""

    def test_it_matches_bound_on_a_pair_it_has_seen(self):
        one, other = Predictor(actions=2), Predictor(actions=2,
                                                     binding="adaptive")
        for _ in range(30):
            one.learn(0, 0, 100)
            other.learn(0, 0, 100)
        self.assertTrue(other.hit(0, 0, 100))
        self.assertAlmostEqual(other.probability(0, 0, 100),
                               one.probability(0, 0, 100))

    def test_it_falls_back_where_bound_would_have_nothing(self):
        """The companion, and the whole point: bound alone answers nothing
        here, so a matching score would mean the fallback never fired.

        **The halves have to OVERLAP for factoring to offer anything.** A first
        version of this fixture gave `left` and `right` disjoint targets, and
        `min` scored every candidate at zero — factoring generalises by
        composing two views of the SAME outcome, so where they share no outcome
        it has nothing to compose. That is a real limitation of the fallback and
        not a fault in it.
        """
        bound = Predictor(actions=2, binding="bound")
        adaptive = Predictor(actions=2, binding="adaptive")
        for predictor in (bound, adaptive):
            for _ in range(40):
                predictor.learn(0, 0, 100)
                predictor.learn(1, 0, 100)
                predictor.learn(1, 1, 100)
                predictor.learn(2, 1, 100)
                predictor.learn(2, 0, 200)
        # (0, 1) was never counted by either, and 100 follows both halves.
        self.assertEqual(bound.scores(0, 1), {})
        self.assertTrue(adaptive.scores(0, 1),
                        "adaptive must offer candidates where bound cannot")

    def test_it_writes_both_tables(self):
        """Without both writes there is nothing to fall back TO."""
        adaptive = Predictor(actions=2, binding="adaptive")
        for _ in range(20):
            adaptive.learn(0, 0, 100)
        self.assertGreater(adaptive.bound_evidence(0, 0), 0)
        self.assertIsNotNone(adaptive._factored)


if __name__ == "__main__":
    unittest.main()
