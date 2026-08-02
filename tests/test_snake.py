"""Snake with a local view — the world that answers back.

The load-bearing tests are the two that make it worth building rather than the
ones that make it work: that the view is CENTRED, which is what makes the same
situation recur, and that a local view genuinely HIDES things, which is what
gives "act to disambiguate" something to disambiguate.
"""

from __future__ import annotations

import random
import unittest

from openplexus.tasks.snake import (ACTIONS, BODY, EMPTY, FOOD, WALL,
                                    SELECTING_ENERGY, Snake,
                                    patches)


class TheViewIsCentredSoSituationsRecur(unittest.TestCase):
    """Counting needs a thing to happen twice before any statistic exists.

    A board-absolute view would make the same local situation in two places two
    different observations, and nothing would ever recur.
    """

    def test_the_same_surroundings_in_two_places_are_one_observation(self):
        left = Snake(width=20, height=20, sight=1, seed=0)
        right = Snake(width=20, height=20, sight=1, seed=0)
        left.body, left.food = [(5, 5)], (19, 19)
        right.body, right.food = [(12, 8)], (19, 19)
        self.assertEqual(left.view(), right.view())

    def test_but_different_surroundings_are_not(self):
        """The companion. Without it the test above would pass for a view that
        returned a constant."""
        open_field = Snake(width=20, height=20, sight=1, seed=0)
        open_field.body, open_field.food = [(5, 5)], (19, 19)
        cornered = Snake(width=20, height=20, sight=1, seed=0)
        cornered.body, cornered.food = [(0, 0)], (19, 19)
        self.assertNotEqual(open_field.view(), cornered.view())

    def test_a_short_stream_reuses_its_observations(self):
        world = Snake(width=8, height=8, sight=2, seed=0)
        rng = random.Random(0)
        views = [world.step(rng.randrange(len(ACTIONS))).view
                 for _ in range(2000)]
        self.assertLess(len(set(views)), len(views) / 4,
                        "without heavy reuse there is nothing for counting to "
                        "accumulate, which is the whole reason for this task")


class ALocalViewHidesThings(unittest.TestCase):
    """What makes acting informative, and the reason `sight` exists."""

    def _food_visible_share(self, sight):
        world = Snake(width=12, height=12, sight=sight, seed=0)
        rng = random.Random(1)
        seen = 0
        for _ in range(1000):
            seen += FOOD in world.step(rng.randrange(len(ACTIONS))).view
        return seen / 1000

    def test_the_food_is_usually_out_of_sight(self):
        self.assertLess(self._food_visible_share(2), 0.5)

    def test_and_the_full_board_always_sees_it(self):
        """The companion, and the other arm of the same experiment."""
        self.assertEqual(self._food_visible_share(None), 1.0)

    def test_the_edge_of_the_world_reads_as_wall(self):
        world = Snake(width=5, height=5, sight=1, seed=0)
        world.body, world.food = [(0, 0)], (4, 4)
        view = world.view()                 # 3x3, row-major, head at index 4
        self.assertEqual(view[0], WALL)     # (-1, -1), off the board
        self.assertEqual(view[4], BODY)     # the head itself, always centre
        self.assertEqual(view[8], EMPTY)    # (+1, +1), open board


class ActingChangesWhatIsObserved(unittest.TestCase):
    """The one property a recorded corpus cannot have."""

    def test_two_different_actions_lead_to_two_different_observations(self):
        """Near a feature. See the test below for why "near" is required."""
        def after(action):
            world = Snake(width=12, height=12, sight=2, seed=0)
            world.body, world.food = [(1, 1)], (11, 11)
            return world.step(action).view

        self.assertNotEqual(after(0), after(3))

    def test_IN_OPEN_SPACE_NO_ACTION_CHANGES_THE_VIEW(self):
        """A property of the task, recorded because an experiment must know it.

        A centred view of a featureless region is the same view whichever way
        you went. So a step taken away from walls, food and the snake's own body
        teaches NOTHING: prediction is trivially correct and acting is
        uninformative. The signal lives near features, and a board chosen much
        larger than `sight` spends most of its steps learning nothing.

        This is not a bug to fix. It is why board size has to be chosen
        relative to sight, and it bounds what any measurement here can show.
        """
        def after(action):
            world = Snake(width=12, height=12, sight=2, seed=0)
            world.body, world.food = [(6, 6)], (11, 11)
            return world.step(action).view

        self.assertEqual(after(0), after(3))

    def test_the_same_action_from_the_same_state_is_deterministic(self):
        """No noise at all, which is deliberate: uncertainty sampling's known
        failure is chasing irreducible noise and there is none here to chase."""
        def after():
            world = Snake(width=12, height=12, sight=2, seed=0)
            world.body, world.food = [(6, 6)], (1, 1)
            return world.step(1).view

        self.assertEqual(after(), after())


class TheStreamNeverEnds(unittest.TestCase):
    """C4: no run that stops. A death is reported and play continues."""

    def test_a_death_is_reported_rather_than_raised(self):
        world = Snake(width=3, height=3, sight=1, seed=0)
        world.body, world.food = [(0, 1)], (2, 2)
        step = world.step(2)
        self.assertTrue(step.died)
        self.assertFalse(world.dead is None)

    def test_play_continues_after_a_death(self):
        world = Snake(width=3, height=3, sight=1, seed=0)
        rng = random.Random(0)
        for _ in range(500):
            world.step(rng.randrange(len(ACTIONS)))
        self.assertEqual(len(world.view()), 9)

    def test_eating_is_reported_but_nothing_optimises_it(self):
        world = Snake(width=6, height=6, sight=1, seed=0)
        world.body, world.food = [(3, 3)], (3, 2)
        self.assertTrue(world.step(0).ate)


class ArgumentsAreRefused(unittest.TestCase):

    def test_a_board_too_small_to_turn_in_is_refused(self):
        with self.assertRaises(ValueError):
            Snake(width=2, height=8)

    def test_a_sight_of_zero_is_refused(self):
        """It would see only the head, which never changes, so no action could
        ever be informative and the task would be vacuous."""
        with self.assertRaises(ValueError):
            Snake(sight=0)

    def test_an_action_that_does_not_exist_is_refused(self):
        with self.assertRaises(ValueError):
            Snake().step(len(ACTIONS))


if __name__ == "__main__":
    unittest.main()


class PatchesCutTheViewIntoOverlappingWindows(unittest.TestCase):
    """Many surfaces per input rather than one, which a grid supplies free."""

    def test_a_five_wide_view_gives_nine_three_wide_windows(self):
        world = Snake(width=12, height=12, sight=2, seed=0)
        self.assertEqual(len(patches(world.view(), 3)), 9)

    def test_each_window_holds_its_own_cells(self):
        view = tuple(range(25))
        got = patches(view, 3)
        self.assertEqual(got[0], (0, 1, 2, 5, 6, 7, 10, 11, 12))
        self.assertEqual(got[-1], (12, 13, 14, 17, 18, 19, 22, 23, 24))

    def test_the_windows_overlap(self):
        """Which is what stops a moving feature vanishing between them."""
        got = patches(tuple(range(25)), 3)
        self.assertTrue(set(got[0]) & set(got[1]))

    def test_a_window_the_size_of_the_view_is_the_view(self):
        view = tuple(range(9))
        self.assertEqual(patches(view, 3), [view])

    def test_a_view_that_is_not_square_is_refused(self):
        with self.assertRaises(ValueError):
            patches((1, 2, 3, 4, 5), 2)

    def test_a_window_larger_than_the_view_is_refused(self):
        with self.assertRaises(ValueError):
            patches(tuple(range(9)), 4)


class EnergyGivesItSomethingToLose(unittest.TestCase):
    """John's design. Not a reward: nothing declares food good. A policy that
    does not eat simply gets fewer steps of experience, which is selection."""

    def test_off_by_default(self):
        world = Snake(width=8, height=8, sight=2, seed=0)
        self.assertIsNone(world.energy)
        self.assertFalse(world.step(0).starved)

    def test_it_starves_when_the_energy_runs_out(self):
        world = Snake(width=8, height=8, sight=2, seed=0, energy=5)
        starved = [world.step(0).starved for _ in range(5)]
        self.assertEqual(starved, [False, False, False, False, True])

    def test_eating_extends_the_life(self):
        """The connection test, and the mechanism itself: without this the
        energy is a clock rather than a reason to eat."""
        fed = Snake(width=6, height=6, sight=2, seed=0, energy=4)
        fed.body, fed.food = [(3, 3)], (3, 2)
        self.assertTrue(fed.step(0).ate)
        self.assertEqual(fed.energy, 4)          # spent one, refilled to full

        hungry = Snake(width=6, height=6, sight=2, seed=0, energy=4)
        hungry.body, hungry.food = [(3, 3)], (0, 0)
        hungry.step(0)
        self.assertEqual(hungry.energy, 3)

    def test_dying_does_not_refund_the_step(self):
        """Otherwise a policy could stall the clock by running into walls."""
        world = Snake(width=3, height=3, sight=1, seed=0, energy=10)
        world.body, world.food = [(0, 1)], (2, 2)
        step = world.step(2)
        self.assertTrue(step.died)
        self.assertEqual(world.energy, 9)

    def test_a_snake_with_no_energy_is_refused(self):
        with self.assertRaises(ValueError):
            Snake(energy=0)

    def test_the_selecting_energy_is_not_a_uniform_timer(self):
        """SELECTING_ENERGY is measured, not chosen: at a value too small every
        policy starves at the same step and nothing is selected."""
        import random as _random
        lives = []
        for seed in range(4):
            world = Snake(width=8, height=8, sight=2, seed=seed,
                          energy=SELECTING_ENERGY)
            rng = _random.Random(seed)
            steps = 0
            while steps < 20000:
                steps += 1
                if world.step(rng.randrange(len(ACTIONS))).starved:
                    break
            lives.append(steps)
        self.assertGreater(max(lives), min(lives),
                           "if every seed starves at the same step this is a "
                           "clock and not a selection pressure")
        self.assertGreater(max(lives), SELECTING_ENERGY)
