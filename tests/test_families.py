"""The families task, and the properties without which a score would be a lie.

Three of these carry the task's whole meaning:

- **A TRANSFER entity's own fact is never stated.** If it were, transfer would be
  recall and the arm would measure nothing new. This is the property the task
  exists for.
- **Both query kinds answer with the FAMILY's value**, so DIRECT and TRANSFER
  are comparable and the only difference is whether it was said out loud.
- **The family→value mapping is redrawn every sequence.** Without that a global
  prior learns it and transfer becomes counting — note 047's failure, one level
  up, and the reason MQAR redraws its pairs.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.tasks.families import (
    FACT, QUERY, FamilyConfig, background, dataset, generate)

BASE = FamilyConfig(n_families=4, family_size=3, n_attributes=2, n_values=6,
                    stated_per_family=1, queries_per_kind=1, seed=5)


def stated_facts(tokens: np.ndarray) -> dict[int, int]:
    """Entity -> value, for every `FACT entity value` in the sequence."""
    found = {}
    for i in range(len(tokens) - 2):
        if tokens[i] == FACT:
            found[int(tokens[i + 1])] = int(tokens[i + 2])
    return found


class TheTaskIsWellPosed(unittest.TestCase):

    def test_a_transfer_entity_has_no_stated_fact(self):
        """**The property the whole task rests on.** If a transfer target's own
        fact appeared, the arm would be measuring recall and the word transfer
        would be a lie."""
        for seed in range(12):
            sequence = generate(BASE, seed=seed)
            tokens = np.asarray(sequence.tokens)
            facts = stated_facts(tokens)
            for position, transfer in zip(sequence.query_positions,
                                          sequence.is_transfer):
                if transfer:
                    self.assertNotIn(int(tokens[position]), facts)

    def test_a_direct_entity_does_have_one(self):
        """The other half, so the labels cannot be silently swapped."""
        for seed in range(12):
            sequence = generate(BASE, seed=seed)
            tokens = np.asarray(sequence.tokens)
            facts = stated_facts(tokens)
            for position, transfer in zip(sequence.query_positions,
                                          sequence.is_transfer):
                if not transfer:
                    self.assertIn(int(tokens[position]), facts)

    def test_every_query_is_answered_by_its_familys_value(self):
        """Both kinds answer the same way, which is what makes them
        comparable. A transfer answer must also be recoverable: some sibling
        stated it earlier in this sequence."""
        for seed in range(12):
            sequence = generate(BASE, seed=seed)
            tokens = np.asarray(sequence.tokens)
            facts = stated_facts(tokens)
            for position in sequence.query_positions:
                entity, answer = int(tokens[position]), int(tokens[position + 1])
                family = BASE.family_of(entity)
                siblings = [v for e, v in facts.items()
                            if BASE.family_of(e) == family]
                self.assertTrue(siblings, "no sibling stated a value")
                self.assertEqual(answer, siblings[0])

    def test_the_answer_follows_the_query_position(self):
        """`targets = roll(tokens, -1)` is how every task here is scored, so the
        answer must sit one after the position reported. Decision 138."""
        sequence = generate(BASE, seed=1)
        tokens = np.asarray(sequence.tokens)
        for position in sequence.query_positions:
            self.assertEqual(int(tokens[position - 1]), QUERY)
            self.assertLess(position + 1, len(tokens))

    def test_labels_line_up_with_positions(self):
        sequence = generate(BASE, seed=2)
        self.assertEqual(len(sequence.query_positions),
                         len(sequence.is_transfer))
        self.assertEqual(sum(sequence.is_transfer), BASE.queries_per_kind)


class TheMappingIsRedrawnEverySequence(unittest.TestCase):
    """Otherwise a prior learns it and transfer becomes counting."""

    def test_a_family_does_not_always_answer_the_same_way(self):
        answers: dict[int, set[int]] = {}
        for sequence in dataset(BASE, 40):
            tokens = np.asarray(sequence.tokens)
            for entity, value in stated_facts(tokens).items():
                answers.setdefault(BASE.family_of(entity), set()).add(value)
        self.assertTrue(any(len(v) > 1 for v in answers.values()),
                        "every family always answered the same value, so a "
                        "global prior would solve transfer without the store")

    def test_two_sequences_differ(self):
        first, second = dataset(BASE, 2)
        self.assertNotEqual(first.tokens, second.tokens)


class BackgroundCarriesTheStructureAndNothingElse(unittest.TestCase):

    def test_it_holds_no_facts_and_no_questions(self):
        """The family structure is learned across background streams; the facts
        live only in task sequences. A marker here would mean the index is
        being fitted on the thing being measured."""
        for stream in background(BASE, 4):
            self.assertNotIn(FACT, stream.tolist())
            self.assertNotIn(QUERY, stream.tolist())

    def test_an_entity_sits_between_two_of_its_own_attributes(self):
        """At `window=1` this is what makes a family discoverable at all --
        purity 0.875 against 0.375 when the entity led the mention instead."""
        config = FamilyConfig(n_families=3, family_size=2, n_attributes=2,
                              n_values=4, stated_per_family=1,
                              queries_per_kind=1, attribute_mentions=1, seed=9)
        entities = {config.entity_base + i
                    for i in range(config.n_entities)}
        stream = background(config, 1)[0]
        seen = 0
        for i, token in enumerate(stream):
            if int(token) in entities:
                seen += 1
                family = config.family_of(int(token))
                for side in (stream[i - 1], stream[i + 1]):
                    offset = int(side) - config.attribute_base
                    self.assertEqual(offset // config.n_attributes, family)
        self.assertEqual(seen, config.n_entities * config.attribute_mentions)


class TheDegenerateConfigurationsAreRefused(unittest.TestCase):

    def test_every_entity_stated_leaves_nothing_to_transfer(self):
        with self.assertRaises(ValueError):
            FamilyConfig(family_size=2, stated_per_family=2)

    def test_one_family_is_not_a_similarity_structure(self):
        with self.assertRaises(ValueError):
            FamilyConfig(n_families=1)

    def test_cannot_ask_more_direct_questions_than_facts_stated(self):
        with self.assertRaises(ValueError):
            FamilyConfig(stated_per_family=1, queries_per_kind=2)


class TheAnswerKeyIsSeparateFromTheTask(unittest.TestCase):
    """`families()` is for scoring a grouping. Nothing in the model may read
    it, and the test says so by checking it against the tokens rather than
    trusting it."""

    def test_families_matches_family_of(self):
        for index, group in enumerate(BASE.families()):
            for token in group:
                self.assertEqual(BASE.family_of(token), index)

    def test_every_entity_appears_in_exactly_one_family(self):
        members = [t for group in BASE.families() for t in group]
        self.assertEqual(len(members), BASE.n_entities)
        self.assertEqual(len(set(members)), BASE.n_entities)


if __name__ == "__main__":
    unittest.main()
