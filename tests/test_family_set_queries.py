"""The first task in this project whose answer is a SET.

ARCHITECTURE row F3. The question is "what values were stated about this entity's
family", and the answer is every distinct one -- the family's value **and** its
exceptions. A single token cannot express that: it has to pick the rule or the
exception and lose the other.

That is why this is not only a scoring change. `families.py`'s own docstring says
EXCEPTION exists because

    a system that cannot hold "birds fly, but not this one" does not
    understand birds

and a one-token answer can report *birds fly* or *not this one* but never that both
are true. The set query is the first place the task can ask for the conjunction it
was built around.

**The measurement is off by default and byte-identical when off**, which
`TheOffPathIsUnchanged` asserts rather than intends -- decisions 143-151 reproduce
token for token.
"""

from __future__ import annotations

import unittest

from openplexus.answers import score_one, summarise
from openplexus.tasks import families
from openplexus.tasks.families import FamilyConfig, generate


def config(**over) -> FamilyConfig:
    settings = dict(n_families=4, family_size=4, stated_per_family=3,
                    exceptions_per_family=1, n_values=8, queries_per_kind=2,
                    set_queries=True, seed=0)
    settings.update(over)
    return FamilyConfig(**settings)


class TheAnswerIsASetAndHasMoreThanOneThingInIt(unittest.TestCase):

    def test_every_answer_set_has_at_least_two_values(self):
        # THE WHOLE POINT, and the thing a singleton "set" would silently destroy.
        # With one exception per family the answer is the family's value plus the
        # dissenting one, so two is the floor rather than a coincidence.
        sequence = generate(config())
        self.assertTrue(sequence.answer_sets)
        for answers in sequence.answer_sets:
            self.assertGreaterEqual(len(answers), 2)

    def test_the_answer_is_exactly_what_was_stated_about_the_family(self):
        # Reconstructed from the token stream rather than from the generator's
        # internals, so this checks the LAYOUT and not just the bookkeeping.
        cfg = config()
        sequence = generate(cfg)
        stream = sequence.tokens
        stated: dict[int, int] = {}
        for i in range(len(stream) - 2):
            if stream[i] == families.FACT:
                stated[stream[i + 1]] = stream[i + 2]
        for position, answers in zip(sequence.set_query_positions,
                                     sequence.answer_sets):
            family = cfg.family_of(stream[position])
            members = [cfg.entity_base + family * cfg.family_size + i
                       for i in range(cfg.family_size)]
            expected = frozenset(stated[m] for m in members if m in stated)
            self.assertEqual(answers, expected)

    def test_the_family_value_and_the_exception_are_both_in_it(self):
        # Named separately from the set-equality test above, because "the set is
        # what was stated" would still pass if the task had quietly stopped
        # generating exceptions -- and then every set would be a singleton, which
        # the first test catches, but not for a reason that names the cause.
        cfg = config()
        sequence = generate(cfg)
        for answers in sequence.answer_sets:
            self.assertGreater(len(answers), 1,
                               "an exception should make the set plural")

    def test_distinct_families_are_asked(self):
        # Two questions about one family have the SAME answer set, so a mechanism
        # that memorised one would score twice for it and the mean would be a
        # statistic over fewer independent items than `n` claims -- rule 8.
        cfg = config()
        sequence = generate(cfg)
        asked = [cfg.family_of(sequence.tokens[p])
                 for p in sequence.set_query_positions]
        self.assertEqual(len(asked), len(set(asked)))


class TheLayoutCarriesNoAnswerToken(unittest.TestCase):

    def test_the_marker_precedes_the_entity(self):
        cfg = config()
        sequence = generate(cfg)
        for position in sequence.set_query_positions:
            self.assertEqual(sequence.tokens[position - 1], cfg.ask_all)

    def test_the_position_holds_an_entity(self):
        cfg = config()
        sequence = generate(cfg)
        for position in sequence.set_query_positions:
            token = sequence.tokens[position]
            self.assertGreaterEqual(token, cfg.entity_base)
            self.assertLess(token, cfg.attribute_base)

    def test_set_positions_are_not_in_query_positions(self):
        # A script scoring these as `roll(tokens, -1)` would compare the model
        # against whichever token happens to come next, which for the last set
        # query is nothing at all. Keeping the two lists separate is what stops
        # that being possible rather than merely discouraged.
        sequence = generate(config())
        self.assertFalse(set(sequence.set_query_positions)
                         & set(sequence.query_positions))

    def test_the_marker_is_not_an_entity_id(self):
        cfg = config()
        self.assertLess(cfg.ask_all, cfg.entity_base)

    def test_the_marker_moves_out_of_the_way_of_LINK(self):
        # The id is CONDITIONAL, which is the trap the module's RESERVED comment
        # warns about: a fixed constant would be a marker in one configuration and
        # a real entity in another.
        without = config(family_links=False)
        with_links = config(family_links=True)
        self.assertNotEqual(without.ask_all, with_links.ask_all)
        self.assertLess(with_links.ask_all, with_links.entity_base)


class TheOffPathIsUnchanged(unittest.TestCase):

    def test_off_by_default(self):
        self.assertFalse(FamilyConfig().set_queries)

    def test_no_set_fields_when_off(self):
        sequence = generate(config(set_queries=False,
                                   exceptions_per_family=1))
        self.assertEqual(sequence.set_query_positions, ())
        self.assertEqual(sequence.answer_sets, ())

    def test_the_stream_is_byte_identical_when_off(self):
        # THE RAIL. Turning the flag off must reproduce the task decisions 143-151
        # measured, token for token -- so the new block may consume no randomness
        # and shift no id when it is not running.
        off = generate(config(set_queries=False, exceptions_per_family=1))
        reference = generate(FamilyConfig(
            n_families=4, family_size=4, stated_per_family=3,
            exceptions_per_family=1, n_values=8, queries_per_kind=2, seed=0))
        self.assertEqual(off.tokens, reference.tokens)
        self.assertEqual(off.query_positions, reference.query_positions)

    def test_the_stream_grows_only_by_the_set_queries(self):
        # Two tokens per set question and not one more, which is what "no answer
        # token" means made checkable.
        off = generate(config(set_queries=False, exceptions_per_family=1))
        on = generate(config(set_queries=True, exceptions_per_family=1))
        self.assertEqual(len(on.tokens) - len(off.tokens),
                         2 * len(on.set_query_positions))


class ItRefusesWhatWouldMeasureNothing(unittest.TestCase):

    def test_set_queries_without_exceptions_is_refused(self):
        # Every answer set would be a singleton, so the measurement would be the
        # single-token one under a new heading -- and it would SCORE WELL, because
        # a mechanism emitting one token is right. That is the shape of result this
        # project's standards exist to prevent.
        with self.assertRaises(ValueError):
            FamilyConfig(set_queries=True, exceptions_per_family=0)

    def test_reading_the_marker_when_off_is_refused(self):
        # That id belongs to an entity when the flag is off, so returning it
        # anyway would address a real entity while looking like a marker.
        with self.assertRaises(ValueError):
            FamilyConfig().ask_all


class SharedAttributesMakeFamiliesGENUINELYConfusable(unittest.TestCase):
    """The axis note 057 needed, and its first version was inert by construction.

    That version had each family borrow its NEIGHBOUR'S attributes, so family f used
    `{f0, g1, g2, g3}` -- a set no other family used, and therefore still uniquely
    identifying. Purity stayed at **1.000** sharing three of four attributes. These
    tests are written against the property that was missing, not against the fix:
    **two families must actually share tokens.**
    """

    def evidence(self, cfg, family):
        """Which attribute tokens co-occur with this family's entities."""
        stream = families.background(cfg, 1)[0]
        members = set(cfg.families()[family])
        seen = set()
        for i in range(1, len(stream) - 1):
            if int(stream[i]) in members:
                seen.add(int(stream[i - 1]))
                seen.add(int(stream[i + 1]))
        return seen

    def test_families_share_tokens_when_sharing_is_on(self):
        cfg = config(n_attributes=4, shared_attributes=3)
        first, second = self.evidence(cfg, 0), self.evidence(cfg, 1)
        self.assertTrue(first & second,
                        "no token is common to two families, so the axis is "
                        "inert -- which is exactly how the first version failed")

    def test_families_share_NOTHING_when_it_is_off(self):
        # The companion. Without it the test above passes for a task where every
        # family always shared everything, which is a different broken.
        cfg = config(n_attributes=4, shared_attributes=0)
        self.assertFalse(self.evidence(cfg, 0) & self.evidence(cfg, 1))

    def test_more_sharing_leaves_less_private_evidence(self):
        # The quantity the axis is supposed to move, moved.
        wide = config(n_attributes=4, shared_attributes=1)
        tight = config(n_attributes=4, shared_attributes=3)
        self.assertGreater(
            len(self.evidence(wide, 0) - self.evidence(wide, 1)),
            len(self.evidence(tight, 0) - self.evidence(tight, 1)))

    def test_off_by_default_and_byte_identical(self):
        base = FamilyConfig(n_families=4, family_size=4, stated_per_family=3,
                            exceptions_per_family=1, n_values=8, n_attributes=4,
                            queries_per_kind=2, seed=0)
        self.assertEqual(base.shared_attributes, 0)
        off = config(n_attributes=4, shared_attributes=0, set_queries=False)
        self.assertEqual(families.background(off, 2)[0].tolist(),
                         families.background(base, 2)[0].tolist())
        self.assertEqual(off.value_base, base.value_base)

    def test_a_family_with_no_private_attribute_is_refused(self):
        # Unrecoverable by construction rather than merely hard, which is a task
        # that measures nothing.
        with self.assertRaises(ValueError):
            config(n_attributes=4, shared_attributes=4)

    def test_reading_the_shared_pool_when_off_is_refused(self):
        with self.assertRaises(ValueError):
            config(n_attributes=4, shared_attributes=0).shared_base

    def test_the_pool_sits_before_the_values(self):
        cfg = config(n_attributes=4, shared_attributes=2)
        self.assertGreaterEqual(cfg.shared_base, cfg.attribute_base)
        self.assertLess(cfg.shared_base + cfg.shared_attributes - 1,
                        cfg.value_base)


class ScoringItThroughTheRuler(unittest.TestCase):
    """The task and `openplexus.answers` have to fit, including the falsifier."""

    def test_the_true_set_scores_exactly(self):
        sequence = generate(config())
        scores = [score_one(answers, answers)
                  for answers in sequence.answer_sets]
        self.assertEqual(summarise(scores).exact, 1.0)

    def test_answering_with_the_family_value_alone_is_only_half_right(self):
        # The single-token mechanism's best possible behaviour, scored under the
        # set convention: it names the rule and misses the exception. Perfect
        # precision, partial recall -- which is the diagnosis the pair exists for.
        cfg = config()
        sequence = generate(cfg)
        stream = sequence.tokens
        stated: dict[int, int] = {}
        for i in range(len(stream) - 2):
            if stream[i] == families.FACT:
                stated[stream[i + 1]] = stream[i + 2]
        for position, answers in zip(sequence.set_query_positions,
                                     sequence.answer_sets):
            family = cfg.family_of(stream[position])
            members = [cfg.entity_base + family * cfg.family_size + i
                       for i in range(cfg.family_size)]
            majority = max(answers,
                           key=lambda v: sum(1 for m in members
                                             if stated.get(m) == v))
            score = score_one([majority], answers)
            self.assertEqual(score.precision, 1.0)
            self.assertLess(score.recall, 1.0)
            self.assertFalse(score.exact)

    def test_emitting_every_value_must_not_look_good(self):
        # THE FALSIFIER, carried into the task rather than left in the ruler's
        # own tests. Perfect recall, and it must not read as success.
        cfg = config()
        sequence = generate(cfg)
        everything = [cfg.value_base + v for v in range(cfg.n_values)]
        scores = [score_one(everything, answers)
                  for answers in sequence.answer_sets]
        summary = summarise(scores)
        self.assertEqual(summary.mean_recall, 1.0)
        self.assertEqual(summary.exact, 0.0)
        self.assertLess(summary.mean_f1, 0.5)


if __name__ == "__main__":
    unittest.main()
