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
    generate_object_question,
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
        config = KinshipConfig(n_people=12, hops=2, n_facts=10, seq_len=140)
        self.assertEqual(config.query_token,
                         config.n_people + len(RELATIONS))
        self.assertEqual(config.fact_token, config.query_token + 1)
        self.assertEqual(config.vocab_size, config.fact_token + 1)

    def test_a_fact_marker_precedes_every_subject(self):
        """**What makes a pair key usable** (decision 103). With `context_keys`
        the store binds `(previous, token)`, so a fact's subject is only
        addressable if what precedes it is predictable. The marker makes
        `key(FACT, S)` mean "S in subject role", distinct from `key(R, S)`
        which is "S in object role" — and those are exactly the two bindings
        that collide on a single-token key."""
        config = KinshipConfig(n_people=12, hops=2, n_facts=8, seq_len=140,
                               seed=31)
        for sequence in dataset(config, 20):
            for subject, relation, obj in sequence.facts:
                triple = (config.fact_token, subject,
                          config.relation_token(relation), obj)
                joined = sequence.tokens
                found = any(joined[i:i + 4] == triple
                            for i in range(len(joined) - 3))
                self.assertTrue(found, f"{triple} not laid down as a block")

    def test_the_question_ends_with_a_marked_subject(self):
        """The key at the scored position must be the pair a fact wrote. Ending
        the question any other way keys the retrieval on a pair that was never
        stored."""
        config = KinshipConfig(n_people=12, hops=2, n_facts=8, seq_len=140,
                               seed=32)
        for sequence in dataset(config, 20):
            at = sequence.answer_position
            self.assertEqual(sequence.tokens[at], sequence.asked[0])
            self.assertEqual(sequence.tokens[at - 1], config.fact_token)


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


class TheObjectQuestionAsksTheStepNothingElseMeasURES(unittest.TestCase):
    """`generate_object_question` is step 2 of decision 107's traversal.

    The whole case for building a traversal rests on step 2 being ~0.96, and
    that number came from an inline probe that left no script behind. These pin
    the generator so the measurement is reproducible.

    The property that matters is the KEY AT THE SCORED POSITION. A fact is laid
    out `FACT S R O` and the store binds the previous position's key to the
    current position's value, so the write at `O` binds `key(S, R)`. If the
    question does not end on a position whose pair key is `(S, R)`, it queries a
    binding that was never written -- which is the defect decision 100 measured
    at 0.020 against 0.713, and it would look like a weak store rather than a
    broken question.
    """

    def setUp(self):
        self.config = KinshipConfig(n_people=12, hops=2, n_facts=10, seed=7)

    def test_the_scored_position_carries_the_pair_key_the_fact_wrote(self):
        """The two tokens before the answer must be exactly `S` then `R`."""
        for seed in range(25):
            sequence = generate_object_question(
                KinshipConfig(n_people=12, hops=2, n_facts=10, seed=seed))
            subject, _ = sequence.asked
            relation = sequence.path[0]
            at = sequence.answer_position
            self.assertEqual(
                sequence.tokens[at],
                self.config.relation_token(relation),
                "the scored position must BE the relation token")
            self.assertEqual(
                sequence.tokens[at - 1], subject,
                "the token before the scored position must be the subject, or "
                "the pair key is not the one the fact wrote")

    def test_the_answer_is_the_object_of_a_stated_fact(self):
        """Independently checked against the fact list, not against the
        generator's own bookkeeping."""
        for seed in range(25):
            sequence = generate_object_question(
                KinshipConfig(n_people=12, hops=2, n_facts=10, seed=seed))
            subject, obj = sequence.asked
            relation = sequence.path[0]
            self.assertIn(
                (subject, relation, obj), sequence.facts,
                "the asked triple is not among the stated facts, so the "
                "question is unanswerable from the sequence")

    def test_the_answer_is_the_target_and_nothing_else_is(self):
        sequence = generate_object_question(self.config)
        scored = [(i, t) for i, t in enumerate(sequence.targets) if t != IGNORE]
        self.assertEqual(len(scored), 1)
        self.assertEqual(scored[0][0], sequence.answer_position)
        self.assertEqual(scored[0][1], sequence.asked[1])

    def test_it_asks_about_the_first_hop_of_the_path(self):
        """Step 2 follows the FIRST relation. Asking about a distractor would
        measure a different and easier thing -- a distractor's subject has no
        particular out-degree, where the queried subject's is the whole
        difficulty."""
        for seed in range(25):
            base_config = KinshipConfig(n_people=12, hops=2, n_facts=10,
                                        seed=seed)
            base = generate(base_config)
            sequence = generate_object_question(base_config)
            self.assertEqual(sequence.asked[0], base.asked[0],
                             "the object question should ask about the same "
                             "subject the composition question does")
            self.assertEqual(sequence.path[0], base.path[0],
                             "it should follow the first relation of the path")

    def test_the_query_block_does_not_write_the_binding_it_reads(self):
        """If the question's own tokens wrote `key(S, R)`, the retrieval would
        be reading what it just put there and the measurement would be
        circular. The write at the `R` position binds `key(FACT, S)`, and there
        is no input position after `R`."""
        sequence = generate_object_question(self.config)
        at = sequence.answer_position
        self.assertEqual(at, len(sequence.tokens) - 2,
                         "the scored position must be the last INPUT token, "
                         "with only the answer after it")

    def test_the_composition_question_is_unchanged(self):
        """A regression guard. `generate_object_question` calls `generate`, and
        an edit to either must not move the other -- nine sweeps' comparison
        set rests on `generate` producing what it produced."""
        first = generate(self.config)
        generate_object_question(self.config)
        second = generate(self.config)
        self.assertEqual(first.tokens, second.tokens)
        self.assertEqual(first.targets, second.targets)


if __name__ == "__main__":
    unittest.main()
