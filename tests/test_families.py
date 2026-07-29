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
    FACT, LINK, QUERY, FamilyConfig, background, dataset, generate)

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


class AnExceptionContradictsItsFamily(unittest.TestCase):
    """The arm decisions 144 and 145 rest on, added to the task after the first
    tests were written and therefore uncovered until now.

    Its whole meaning is that the entity's own stated fact **differs** from what
    its siblings state. If an exception ever coincided with its family's value
    the arm would score it as a success for the wrong reason, and the failure
    would look like the mechanism working.
    """

    ODD = FamilyConfig(n_families=4, family_size=4, n_attributes=2, n_values=6,
                       stated_per_family=3, exceptions_per_family=1,
                       queries_per_kind=1, seed=11)

    def test_an_exceptions_answer_differs_from_its_siblings(self):
        for seed in range(12):
            sequence = generate(self.ODD, seed=seed)
            tokens = np.asarray(sequence.tokens)
            facts = stated_facts(tokens)
            for position, exception in zip(sequence.query_positions,
                                           sequence.is_exception):
                if not exception:
                    continue
                entity = int(tokens[position])
                family = self.ODD.family_of(entity)
                siblings = [v for e, v in facts.items()
                            if self.ODD.family_of(e) == family and e != entity]
                self.assertTrue(siblings)
                self.assertNotIn(facts[entity], siblings)

    def test_an_exception_is_answered_by_its_OWN_fact(self):
        """Not its family's. That is the whole distinction."""
        for seed in range(12):
            sequence = generate(self.ODD, seed=seed)
            tokens = np.asarray(sequence.tokens)
            facts = stated_facts(tokens)
            for position, exception in zip(sequence.query_positions,
                                           sequence.is_exception):
                if exception:
                    entity = int(tokens[position])
                    self.assertEqual(int(tokens[position + 1]), facts[entity])

    def test_a_query_is_never_both_transfer_and_exception(self):
        """One says the fact was not stated, the other that it was and
        contradicts. Both at once is incoherent."""
        for seed in range(8):
            sequence = generate(self.ODD, seed=seed)
            for transfer, exception in zip(sequence.is_transfer,
                                           sequence.is_exception):
                self.assertFalse(transfer and exception)

    def test_the_majority_still_agrees(self):
        """Decision 145: the default survives because it outnumbers the
        dissent. If the generator ever let exceptions reach parity, that
        finding would silently become the 50/50 case 144 mistook for the
        mechanism."""
        for seed in range(8):
            sequence = generate(self.ODD, seed=seed)
            tokens = np.asarray(sequence.tokens)
            facts = stated_facts(tokens)
            for family in range(self.ODD.n_families):
                values = [v for e, v in facts.items()
                          if self.ODD.family_of(e) == family]
                if not values:
                    continue
                commonest = max(set(values), key=values.count)
                self.assertGreater(values.count(commonest), len(values) / 2)

    def test_no_exceptions_reproduces_the_task_143_measured(self):
        """`exceptions_per_family=0` is the default, and decision 143's numbers
        depend on this field's existence changing nothing."""
        plain = FamilyConfig(n_families=4, family_size=3, n_attributes=2,
                             n_values=6, stated_per_family=1,
                             queries_per_kind=1, seed=5)
        for seed in range(6):
            sequence = generate(plain, seed=seed)
            self.assertEqual(sum(sequence.is_exception), 0)


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


class LinksAreOffAndInvisible(unittest.TestCase):
    """Note 050's instrument, and the two things that make it safe.

    **The rail that matters most is the first.** `family_links` edits the file
    decisions 143-151 are measured on. If switching the field into existence
    changed one draw, every one of those numbers would stop reproducing while
    still looking plausible -- decision 74's failure, which is the reason this is
    asserted rather than intended.
    """

    def test_the_link_free_path_is_unchanged_by_the_field_existing(self):
        # Not a comparison against a stored blob: the point is that the DEFAULT
        # and the explicit `False` take the same draws, and that neither reserves
        # the LINK id. A regression here shifts `entity_base` or consumes the
        # generator differently, and both are silent.
        for seed in range(12):
            for exceptions in (0, 1):
                default = generate(FamilyConfig(
                    seed=seed, exceptions_per_family=exceptions))
                explicit = generate(FamilyConfig(
                    seed=seed, exceptions_per_family=exceptions,
                    family_links=False))
                self.assertEqual(default.tokens, explicit.tokens)
                self.assertEqual(default.query_positions,
                                 explicit.query_positions)
                self.assertEqual(default.is_transfer, explicit.is_transfer)
                self.assertEqual(default.is_exception, explicit.is_exception)
                self.assertEqual(default.is_linked, ())

    def test_links_do_not_move_the_vocabulary_when_off(self):
        off = FamilyConfig(seed=0)
        self.assertEqual(off.reserved, 2)
        self.assertEqual(off.entity_base, 2)
        # And they DO reserve one more when on, or `LINK` would collide with the
        # first entity and state a fact about a marker.
        self.assertEqual(FamilyConfig(seed=0, family_links=True).reserved, 3)

    def test_no_family_links_to_itself(self):
        # A self-link makes the question identical to TRANSFER, which would
        # dilute the arm rather than fail it -- the quietest kind of wrong.
        for seed in range(30):
            config = FamilyConfig(seed=seed, family_links=True)
            for family, other in enumerate(config.linked_family):
                self.assertNotEqual(family, other)

    def test_the_link_is_a_permutation(self):
        for seed in range(10):
            config = FamilyConfig(seed=seed, family_links=True)
            self.assertEqual(sorted(config.linked_family),
                             list(range(config.n_families)))

    def test_a_linked_query_is_never_also_asked_as_transfer(self):
        # Two different correct answers on one address would make the arm
        # unscoreable, and would look like the mechanism failing.
        for seed in range(20):
            sequence = generate(FamilyConfig(seed=seed, family_links=True))
            linked = {sequence.tokens[at] for at, flag
                      in zip(sequence.query_positions, sequence.is_linked)
                      if flag}
            transfer = {sequence.tokens[at] for at, flag
                        in zip(sequence.query_positions, sequence.is_transfer)
                        if flag}
            self.assertEqual(linked & transfer, set())

    def test_a_linked_entity_never_had_its_own_fact_stated(self):
        # The arm exists to need the gate. An entity whose fact was stated has
        # an occupied address and would be answerable without it.
        for seed in range(20):
            sequence = generate(FamilyConfig(seed=seed, family_links=True))
            tokens = sequence.tokens
            stated = {tokens[i + 1] for i in range(len(tokens) - 2)
                      if tokens[i] == FACT}
            for at, flag in zip(sequence.query_positions, sequence.is_linked):
                if flag:
                    self.assertNotIn(tokens[at], stated)

    def test_the_link_never_reaches_the_background_streams(self):
        # THE CALIBRATION, as a test rather than a one-off. `ContentIndex` is
        # fitted on these, so a LINK token appearing here would let the index
        # answer with no hop -- decision 143's circularity in a new costume.
        config = FamilyConfig(seed=0, family_links=True)
        for stream in background(config, 20):
            self.assertNotIn(LINK, stream.tolist())


if __name__ == "__main__":
    unittest.main()
