"""The first mechanism in this project that emits a SET, and what bounds it.

ARCHITECTURE row F3. `LocalAssociativeMemory.answer_set` reads the entity's own
address and its content-index neighbours', skips every address the occupancy sketch
says was never written, and returns the decoded values as a set.

**It COLLECTS where decisions 146 and 147 tried to CHOOSE.** 146 found that reading
neighbours through the index can only average rather than select; 147 refuted both
obvious rules for picking a winner. Neither objection applies here, because a set
answer selects nothing — so the mechanism that was wrong for one token is the right
shape for this question.

## What these tests do and do not claim

They do **not** assert that the mechanism scores 1.000, even though it does at one
setting. The whole finding is that the setting is load-bearing: the enumeration
bound has to equal the group's size, and **the model does not know the group's
size.** `TheEnumerationBoundIsFitted` asserts the shape of that dependence, which
is the honest claim, and it fails if the peak ever stops tracking `family_size`.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.answers import score_one, summarise
from openplexus.content import ContentIndex
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks import families
from openplexus.tasks.families import FamilyConfig, background, generate

#: Three seeds and two questions each. Small on purpose -- this is a mechanism
#: test in the suite, not a sweep, and a sweep is what Actions is for.
SEEDS = (0, 1, 2)


def task(family_size: int, seed: int) -> FamilyConfig:
    return FamilyConfig(n_families=4, family_size=family_size,
                        stated_per_family=family_size - 1,
                        exceptions_per_family=1, n_values=8,
                        queries_per_kind=2, set_queries=True, seed=seed)


def fitted(cfg: FamilyConfig, seed: int) -> LocalAssociativeMemory:
    index = ContentIndex(vocab=cfg.vocab_size, width=128, seed=seed)
    for stream in background(cfg, 40):
        index.observe(stream)
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=cfg.vocab_size, d_model=256, lr=0.05, key_scale=0.5,
        decay=1.0, context_keys=True, derived_keys=True,
        track_occupancy=True, seed=seed))
    # The readout is the value matrix, so a retrieval decodes to the token it
    # stored -- the same fixture every hop test in this project uses.
    model.wo[:] = model.wv
    model.content = index
    return model


def measure(family_size: int, branches: int):
    scores = []
    for seed in SEEDS:
        cfg = task(family_size, seed)
        model = fitted(cfg, seed)
        sequence = generate(cfg)
        model.run(np.asarray(sequence.tokens, dtype=np.int64))
        for position, truth in zip(sequence.set_query_positions,
                                   sequence.answer_sets):
            entity = sequence.tokens[position]
            scores.append(score_one(
                model.answer_set(families.FACT, entity, branches), truth))
    return summarise(scores)


class ItRecoversTheStatedValueSet(unittest.TestCase):

    def test_at_the_matched_bound_it_is_exact(self):
        # family_size 4 and branches 3, which is the entity plus its three
        # siblings -- exactly the family and nothing else.
        self.assertGreaterEqual(measure(4, 3).exact, 0.9)

    def test_it_emits_more_than_one_value(self):
        # The point of row F3. A mechanism returning one token cannot be right
        # here, and this says the mechanism is not secretly doing that.
        self.assertGreater(measure(4, 3).mean_size, 1.5)

    def test_the_answer_holds_the_rule_AND_the_exception(self):
        # `families.py`'s sentence, satisfied: "birds fly, but not this one". The
        # set contains the family's value and the dissenting one, which is what no
        # single token could express.
        cfg = task(4, 0)
        model = fitted(cfg, 0)
        sequence = generate(cfg)
        model.run(np.asarray(sequence.tokens, dtype=np.int64))
        for position, truth in zip(sequence.set_query_positions,
                                   sequence.answer_sets):
            got = model.answer_set(families.FACT, sequence.tokens[position], 3)
            self.assertGreaterEqual(len(truth), 2)
            self.assertEqual(got, truth)


class TheGateIsWhatKeepsItPrecise(unittest.TestCase):

    def test_the_answer_is_far_smaller_than_the_candidate_list(self):
        # THE GATE'S ACTION, asserted through the public surface. At branches 8
        # the mechanism considers nine candidates; an ungated read decodes all
        # nine, because an unwritten address returns noise that still argmaxes to
        # a real token. Measured: 3.90 gated against 4.70 ungated.
        #
        # There is no ungated arm to compare against here BY DESIGN -- the model
        # refuses `answer_set` without the sketch (see `ItRefusesWhatCannotWork`),
        # so the gate is not an option that could be left off.
        summary = measure(4, 8)
        self.assertLess(summary.mean_size, 6.0)

    def test_precision_is_perfect_when_the_bound_is_right(self):
        # Every value emitted was genuinely stated about the family. That is the
        # gate's contribution and it costs nothing fitted: decision 148's
        # structurally-zero read means an unwritten sibling contributes nothing
        # rather than contributing noise.
        self.assertEqual(measure(4, 3).mean_precision, 1.0)


class TheEnumerationBoundIsFitted(unittest.TestCase):
    """THE FINDING, and it is a limitation rather than a result."""

    def test_over_enumerating_destroys_precision(self):
        # Neighbours beyond the family are OTHER FAMILIES' entities, and their
        # addresses ARE written -- they have stated facts. So the gate cannot
        # reject them: it filters emptiness, not irrelevance. That distinction is
        # the whole limit of this mechanism.
        matched = measure(4, 3)
        over = measure(4, 8)
        self.assertEqual(matched.mean_precision, 1.0)
        self.assertLess(over.mean_precision, 0.8)
        self.assertLess(over.exact, matched.exact)

    def test_under_enumerating_costs_recall(self):
        under = measure(4, 1)
        self.assertLess(under.mean_recall, 1.0)
        self.assertEqual(under.mean_precision, 1.0)

    def test_the_peak_tracks_the_group_size(self):
        # THE LOAD-BEARING ASSERTION. If the optimum sat at a fixed number the
        # finding would be about that number; it tracks `family_size - 1` in every
        # row, so the finding is that enumeration must be bounded by the group's
        # size -- which the model is not told and cannot currently discover.
        #
        # Asserted as a RELATION rather than a table of values, so it survives
        # every change that does not break the relation.
        for family_size in (3, 4, 5):
            with self.subTest(family_size=family_size):
                matched = measure(family_size, family_size - 1)
                too_many = measure(family_size, family_size + 2)
                self.assertGreater(matched.exact, too_many.exact)
                self.assertGreater(matched.mean_f1, too_many.mean_f1)


class ItRefusesWhatCannotWork(unittest.TestCase):

    def test_before_any_sequence_has_run_it_is_refused(self):
        # A zero matrix decodes to whatever the readout prefers and looks exactly
        # like a mechanism that found nothing.
        cfg = task(4, 0)
        with self.assertRaises(ValueError):
            fitted(cfg, 0).answer_set(families.FACT, cfg.entity_base, 3)

    def test_without_the_sketch_it_is_refused(self):
        # Without the gate every neighbour contributes a value and the answer is
        # as large as `branches` regardless of what was stored.
        cfg = task(4, 0)
        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=cfg.vocab_size, d_model=256, context_keys=True,
            derived_keys=True, track_occupancy=False, seed=0))
        model.content = ContentIndex(vocab=cfg.vocab_size, width=128, seed=0)
        model.run(np.asarray(generate(cfg).tokens, dtype=np.int64))
        with self.assertRaises(ValueError):
            model.answer_set(families.FACT, cfg.entity_base, 3)

    def test_without_a_content_index_it_is_refused(self):
        cfg = task(4, 0)
        model = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=cfg.vocab_size, d_model=256, context_keys=True,
            derived_keys=True, track_occupancy=True, seed=0))
        model.run(np.asarray(generate(cfg).tokens, dtype=np.int64))
        with self.assertRaises(ValueError):
            model.answer_set(families.FACT, cfg.entity_base, 3)

    def test_zero_branches_is_refused(self):
        cfg = task(4, 0)
        model = fitted(cfg, 0)
        model.run(np.asarray(generate(cfg).tokens, dtype=np.int64))
        with self.assertRaises(ValueError):
            model.answer_set(families.FACT, cfg.entity_base, 0)


if __name__ == "__main__":
    unittest.main()
