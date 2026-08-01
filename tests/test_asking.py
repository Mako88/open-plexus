"""A world that can be asked, and the three properties that make an ask honest.

`openplexus/tasks/asking.py` exists because `g39-06` measured a boundary counting
cannot cross, and its whole value rests on the refusal rate meaning something.
Three claims in its docstring decide whether it does, and none of them had a test:

- **the refusal is ONE DRAW**, so `patience` bounds the search for `present` and
  can never move the refusal rate. If it could, the quantity the idea rests on
  would be a dial on the result.
- **an ask costs every occasion it draws**, so a system that asks cannot quietly
  see more of the world than one that watches. Otherwise the comparison measures
  sample size, which is the confound `g41-01` found dominating another question.
- **a refusal and a miss are different answers.** A refusal is evidence; a miss
  is a budget running out.

And one more that is load-bearing across the whole project: **this world draws
from the same distribution as `occasions.generate`**, so nothing already measured
moves. That is asserted here rather than trusted to a docstring.
"""

from __future__ import annotations

import unittest

from openplexus.tasks.asking import Answer, World
from openplexus.tasks.occasions import OccasionConfig, generate

#: A small, dense world. `presence` 1.0 makes every surface of the subject
#: appear, so `ask` finds a qualifying occasion quickly and the tests stay
#: deterministic rather than depending on how long a search took.
DENSE = OccasionConfig(concepts=4, surfaces=3, presence=1.0, noise=1,
                       distractors=1, occasions=200, seed=7)

#: The same world with surfaces appearing only sometimes, which is where a
#: refusal and a miss can both happen.
SPARSE = OccasionConfig(concepts=8, surfaces=3, presence=0.5, noise=2,
                        distractors=1, occasions=200, seed=3)


class WatchingIsTheSAMESTREAMAsBefore(unittest.TestCase):
    """The claim that lets every earlier result stand: no new physics."""

    def test_watching_reproduces_generate_occasion_for_occasion(self):
        world = World(DENSE)
        watched = [world.watch() for _ in range(50)]
        expected = generate(DENSE, count=50)
        self.assertEqual([o.surfaces for o in watched],
                         [o.surfaces for o in expected])

    def test_the_clock_advances_once_per_occasion_however_it_was_drawn(self):
        world = World(SPARSE)
        world.watch()
        world.ask(present=0, absent=1)
        world.watch()
        seen = [o.when for o in generate(SPARSE, count=world.drawn)]
        self.assertEqual(seen, sorted(set(seen)))
        # The last occasion watched carries the clock the world has reached.
        self.assertEqual(world.watch().when, world.drawn - 1)


class ThePatienceCannotMoveTheRefusalRate(unittest.TestCase):
    """The property the whole measurement rests on.

    `patience` bounds the search for `present`. Once a qualifying occasion is in
    hand the answer is read off it and nothing is redrawn — so two worlds with
    the same seed and different patience must return the identical answer.
    """

    def test_the_same_seed_answers_identically_at_every_patience(self):
        answers = []
        for patience in (2, 8, 64, 512):
            world = World(SPARSE)
            answers.append(world.ask(present=0, absent=1, patience=patience))
        found = [a for a in answers if a.occasion is not None]
        self.assertTrue(found, "no patience found a qualifying occasion")
        self.assertEqual({a.refused for a in found}, {found[0].refused})
        self.assertEqual({a.occasion.surfaces for a in found},
                         {found[0].occasion.surfaces})

    def test_refusals_happen_and_so_do_permissions(self):
        """Otherwise the test above passes on a world that always says one thing.

        A refusal rate pinned at 0 or 1 would satisfy every equality above while
        measuring nothing, which is the shape of a vacuous test this project
        keeps finding.
        """
        world = World(SPARSE)
        seen = set()
        for _ in range(80):
            answer = world.ask(present=0, absent=1)
            if answer.occasion is not None:
                seen.add(answer.refused)
        self.assertEqual(seen, {True, False})

    def test_the_first_qualifying_occasion_decides_it_across_many_worlds(self):
        """Per world, not per rate — and the difference is the subtlety.

        **A refusal RATE taken over answered queries is not invariant to
        patience**, and the first version of this test asserted that it was and
        failed at 0.3462 against 0.3667. Nothing was wrong with the world: a
        small patience MISSES the worlds where `present` is rare, so it scores a
        different sample rather than the same sample differently.

        The invariant is per world, and it is exact: wherever both patiences
        found an occasion, they found the SAME one and read the same answer off
        it. The mutation this guards is a retry-until-satisfied loop, which
        drives refusals toward zero while every other column looks healthy.
        """
        both = agreed = refused = 0
        for start in range(60):
            config = OccasionConfig(**{**SPARSE.__dict__, "seed": 100 + start})
            small = World(config).ask(present=0, absent=1, patience=4)
            large = World(config).ask(present=0, absent=1, patience=128)
            if small.occasion is None or large.occasion is None:
                continue
            both += 1
            agreed += small.refused == large.refused
            refused += large.refused
        self.assertGreater(both, 20, "too few worlds answered to conclude")
        self.assertEqual(agreed, both)
        # And the rate is neither pinned at 0 nor at 1, or the equality above
        # would hold on a world that only ever says one thing.
        self.assertGreater(refused, 0)
        self.assertLess(refused, both)


class AnAskCostsEveryOccasionItDraws(unittest.TestCase):
    """The budget, which is what keeps asking and watching comparable."""

    def test_the_world_charges_what_the_answer_reports(self):
        world = World(SPARSE)
        before = world.drawn
        answer = world.ask(present=0, absent=1)
        self.assertEqual(world.drawn - before, answer.drawn)
        self.assertGreater(answer.drawn, 0)

    def test_a_miss_charges_its_whole_patience(self):
        # A surface no occasion can contain, so the search runs to the end.
        world = World(DENSE)
        answer = world.ask(present=DENSE.vocabulary - 1, absent=0, patience=5)
        if answer.occasion is None:
            self.assertEqual(answer.drawn, 5)
            self.assertEqual(world.drawn, 5)

    def test_watching_and_asking_spend_the_same_budget(self):
        watcher, asker = World(SPARSE), World(SPARSE)
        for _ in range(10):
            watcher.watch()
        spent = 0
        while spent < 10:
            spent += asker.ask(present=0, absent=1).drawn
        self.assertGreaterEqual(asker.drawn, watcher.drawn)


class ARefusalIsNotAMiss(unittest.TestCase):
    """Conflating them would let an expensive question read as a causal one."""

    def test_a_miss_reports_no_occasion_and_no_refusal(self):
        world = World(DENSE)
        answer = world.ask(present=DENSE.vocabulary - 1, absent=0, patience=3)
        if answer.occasion is None:
            self.assertFalse(answer.refused)

    def test_a_refusal_carries_the_occasion_that_caused_it(self):
        world = World(SPARSE)
        for _ in range(80):
            answer = world.ask(present=0, absent=1)
            if answer.refused:
                self.assertIsNotNone(answer.occasion)
                self.assertIn(1, answer.occasion.surfaces)
                return
        self.fail("no refusal in eighty asks; the world is not exercising it")

    def test_asking_for_a_surface_without_itself_is_refused_as_a_question(self):
        with self.assertRaises(ValueError):
            World(DENSE).ask(present=2, absent=2)


class TheAnswerIsAValue(unittest.TestCase):

    def test_it_carries_exactly_what_was_drawn_and_decided(self):
        answer = Answer(occasion=None, refused=False, drawn=3)
        self.assertEqual((answer.occasion, answer.refused, answer.drawn),
                         (None, False, 3))


if __name__ == "__main__":
    unittest.main()
