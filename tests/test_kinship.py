"""Typed-relation composition, and the ways it could be silently broken.

Same discipline as `test_chains.py`, for the same reason: a task can be
**impossible** (every arm sits at chance, and chance looks like a hard problem)
or **already solved** by a shortcut (every arm scores well and nothing was
measured). The assertions are about generated DATA, not the generator's
intentions.

## The defects this file was written after

All three were found by generating sequences and reading them, before any test
existed — which is the habit that caught every chain-task defect too.

1. **A distractor could state the asked pair directly**, in 7.0% of sequences.
   One in three hundred stated the answer outright; the rest CONTRADICTED it.
2. **Three-hop paths could not be generated at all** — rejection sampling over a
   table where only 24 of 256 pairs compose raised "no 3-hop path composes" on
   an ordinary seed. Paths are constructed by walking the table now.
3. **The floor was wrong.** 1/16 was assumed; the real majority-class strategy
   scores 0.080, 0.108 and 0.150 at one, two and three hops.
"""

from __future__ import annotations

import unittest

from openplexus.tasks.kinship import (
    COMPOSE,
    IGNORE,
    RELATIONS,
    KinshipConfig,
    compose,
    dataset,
    generate,
    majority_floor,
    shortcut_floor,
)


def routes(sequence, depth):
    """Every relation path between the asked pair, up to `depth` steps."""
    edges: dict[int, list[tuple[str, int]]] = {}
    for subject, relation, obj in sequence.facts:
        edges.setdefault(subject, []).append((relation, obj))
    source, target = sequence.asked
    found, stack = [], [(source, ())]
    while stack:
        node, so_far = stack.pop()
        if node == target and so_far:
            found.append(so_far)
        if len(so_far) >= depth:
            continue
        for relation, nxt in edges.get(node, ()):
            stack.append((nxt, so_far + (relation,)))
    return found


class TheTaskIsNotSecretlySolvable(unittest.TestCase):

    def test_the_asked_pair_is_never_stated_directly(self):
        """**Defect 1.** A distractor stating the asked pair either hands over
        the answer or contradicts it. Both directions are excluded: "4 is uncle
        of 9" answers "9 is ? of 4" for anyone who knows the inverse, and the
        inverse is a rule this task is meant to test rather than supply."""
        for hops in (2, 3):
            config = KinshipConfig(n_people=12, hops=hops, n_facts=10,
                                   seq_len=120, seed=11)
            for sequence in dataset(config, 60):
                first, last = sequence.asked
                for subject, _, obj in sequence.facts:
                    with self.subTest(hops=hops):
                        self.assertNotIn((subject, obj),
                                         {(first, last), (last, first)})

    def test_one_hop_needs_no_composition_and_is_the_control(self):
        """1 hop is a stated fact, so it measures recall — the positive control
        that makes a low score at 2 hops readable as an architectural gap rather
        than a broken task."""
        config = KinshipConfig(n_people=12, hops=1, n_facts=8, seq_len=96,
                               seed=5)
        for sequence in dataset(config, 30):
            self.assertIn((sequence.asked[0], sequence.path[0],
                           sequence.asked[1]), sequence.facts)


class TheTaskIsPossible(unittest.TestCase):

    def test_no_route_disagrees_with_the_answer(self):
        """**The other failure direction.** Extra routes that AGREE are fine and
        realistic; a route composing to a DIFFERENT relation leaves the question
        with no determined answer, which looks like a bad model rather than a
        broken task. Measured at 1.0% of 3-hop sequences before the guard."""
        for hops in (2, 3):
            config = KinshipConfig(n_people=12, hops=hops, n_facts=10,
                                   seq_len=120, seed=11)
            for sequence in dataset(config, 60):
                reached = {compose(path) for path in routes(sequence, hops)}
                reached.discard(None)
                with self.subTest(hops=hops):
                    self.assertEqual(reached, {sequence.answer})

    def test_every_fact_needed_to_answer_is_stated(self):
        for hops in (1, 2, 3):
            config = KinshipConfig(n_people=12, hops=hops, n_facts=10,
                                   seq_len=120, seed=7)
            for sequence in dataset(config, 40):
                with self.subTest(hops=hops):
                    self.assertEqual(len(sequence.path), hops)
                    self.assertEqual(compose(sequence.path), sequence.answer)

    def test_three_hops_generates_at_all(self):
        """**Defect 2.** Rejection sampling failed here on an ordinary seed.
        Several seeds, because the failure was seed-dependent and one lucky
        seed would hide it."""
        for seed in (0, 3, 11, 29, 101):
            config = KinshipConfig(n_people=12, hops=3, n_facts=10,
                                   seq_len=120, seed=seed)
            with self.subTest(seed=seed):
                self.assertEqual(len(generate(config).path), 3)


class TheFloorIsTheStrongestTrivialStrategy(unittest.TestCase):

    def test_the_majority_floor_beats_the_uniform_one(self):
        """**Defect 3, asserted so the weak floor cannot be quoted by mistake.**
        If these were ever equal, `uniform_floor` would be safe to use and this
        whole distinction would be dead weight — so the test also documents why
        it is not."""
        for hops in (1, 2, 3):
            config = KinshipConfig(n_people=12, hops=hops, n_facts=10,
                                   seq_len=120, seed=101)
            with self.subTest(hops=hops):
                self.assertGreater(majority_floor(config, 300),
                                   config.uniform_floor)

    def test_the_shortcut_floor_is_the_highest_of_the_three(self):
        """**The floor a composition claim must actually beat.**

        A model that cannot compose scored 0.407 against a 0.130 majority floor,
        because guessing from the first relation alone is worth 0.546 — every
        first relation admits exactly two answers in this table. Ordering the
        three floors here means a future reader cannot quote the weak one by
        accident, which is precisely how g8-01's seq-1536 row was withdrawn.
        """
        for hops in (2, 3):
            config = KinshipConfig(n_people=12, hops=hops, n_facts=10,
                                   seq_len=120, seed=101)
            with self.subTest(hops=hops):
                self.assertGreater(shortcut_floor(config, 400),
                                   majority_floor(config, 400))
                self.assertGreater(majority_floor(config, 400),
                                   config.uniform_floor)

    def test_the_shortcut_is_strong_enough_to_need_saying(self):
        """If the rule table were ever enriched enough to break the prefix
        shortcut, this test fails and `shortcut_floor`'s warning can be softened
        — so it records the current limitation rather than asserting it is
        desirable."""
        config = KinshipConfig(n_people=12, hops=2, n_facts=10, seq_len=120,
                               seed=101)
        self.assertGreater(shortcut_floor(config, 400), 0.4)

    def test_fewer_relations_are_reachable_as_paths_lengthen(self):
        """Why the floor rises with depth: composition contracts the answer
        space. If this ever stopped holding, the depth-dependent floor would be
        unnecessary and the simpler constant could come back."""
        counts = []
        for hops in (1, 3):
            config = KinshipConfig(n_people=12, hops=hops, n_facts=10,
                                   seq_len=120, seed=101)
            counts.append(len({s.answer for s in dataset(config, 300)}))
        self.assertLess(counts[1], counts[0])


class TheCompositionTableIsSound(unittest.TestCase):

    def test_every_rule_names_a_known_relation(self):
        for (left, right), result in COMPOSE.items():
            self.assertIn(left, RELATIONS)
            self.assertIn(right, RELATIONS)
            self.assertIn(result, RELATIONS)

    def test_composing_is_partial_on_purpose(self):
        """A total table would mean inventing answers for pairs that have none —
        a mother's husband is a father only under assumptions this task does not
        make. If the table ever became total, that decision was reversed by
        accident."""
        self.assertLess(len(COMPOSE), len(RELATIONS) ** 2)

    def test_an_uncomposable_path_returns_none_rather_than_guessing(self):
        self.assertIsNone(compose(("wife", "wife")))
        self.assertIsNone(compose(()))


class TheShapeIsWhatItClaims(unittest.TestCase):

    def test_exactly_one_position_is_scored(self):
        config = KinshipConfig(n_people=12, hops=2, n_facts=10, seq_len=120,
                               seed=4)
        sequence = generate(config)
        scored = [t for t in sequence.targets if t != IGNORE]
        self.assertEqual(len(scored), 1)
        self.assertEqual(scored[0],
                         config.relation_token(sequence.answer))

    def test_the_answer_is_always_a_relation_token(self):
        config = KinshipConfig(n_people=12, hops=2, n_facts=10, seq_len=120,
                               seed=9)
        for sequence in dataset(config, 40):
            token = sequence.targets[sequence.answer_position]
            self.assertGreaterEqual(token, config.n_people)
            self.assertLess(token, config.n_people + len(RELATIONS))

    def test_markers_sit_outside_both_ranges(self):
        config = KinshipConfig(n_people=12, hops=2, n_facts=10, seq_len=120)
        self.assertEqual(config.query_token,
                         config.n_people + len(RELATIONS))
        self.assertEqual(config.vocab_size, config.query_token + 1)


class ImpossibleShapesAreRefused(unittest.TestCase):

    def test_zero_hops(self):
        with self.assertRaises(ValueError):
            KinshipConfig(hops=0)

    def test_fewer_facts_than_hops(self):
        with self.assertRaises(ValueError):
            KinshipConfig(hops=4, n_facts=2)

    def test_too_few_people_for_the_path(self):
        with self.assertRaises(ValueError):
            KinshipConfig(n_people=2, hops=3, n_facts=6)

    def test_a_sequence_too_short_to_hold_the_facts(self):
        with self.assertRaises(ValueError):
            KinshipConfig(n_people=12, hops=2, n_facts=10, seq_len=12)


if __name__ == "__main__":
    unittest.main()
