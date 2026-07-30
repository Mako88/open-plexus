"""Tests for the MQAR generator.

Several of these are *connection tests* in the sense of CLAUDE.md rule 6: they
perturb an input and assert the output moves. A test that only checks the
generator runs without raising would pass against a generator that ignores half
its configuration.

Every test here is verified to fail when the thing it names is broken —
`tools/mutate.py` does that check and runs in the pre-commit sequence. A test
that has never been seen to go red is not evidence (rule 10).
"""

from __future__ import annotations

import unittest
from dataclasses import replace

from openplexus.tasks.mqar import IGNORE, MqarConfig, dataset, generate

BASE = MqarConfig(n_pairs=4, seq_len=40, n_keys=16, n_values=8, seed=7)


class TestTaskIsWellPosed(unittest.TestCase):
    """The properties without which a score would be meaningless."""

    def test_every_query_target_is_the_value_that_key_was_paired_with(self):
        """The task's whole definition. If this drifts, every downstream number
        is measuring something other than recall."""
        for filler in ("none", "random", "structured"):
            seq = generate(replace(BASE, filler=filler))
            for i in seq.query_positions:
                self.assertEqual(seq.targets[i], seq.pairs[seq.tokens[i]])

    def test_a_used_key_never_appears_as_filler(self):
        """A filler token identical to a query token, requiring a different
        output, makes the task ill-posed rather than hard — no model could tell
        them apart.

        This failed on the first sequence ever generated: filler was drawn from
        the whole key range, so key 2 appeared at position 16 as filler while
        also being queried at position 13. Fixed by drawing filler only from
        keys the sequence does not use.
        """
        for filler in ("none", "random", "structured"):
            seq = generate(replace(BASE, filler=filler))
            for key in seq.pairs:
                occurrences = [i for i, t in enumerate(seq.tokens) if t == key]
                self.assertEqual(
                    len(occurrences), 2,
                    f"key {key} appears {len(occurrences)} times under "
                    f"filler={filler!r}; expected exactly its pair and its query",
                )

    def test_all_pairs_are_queried(self):
        """The single-query variant is solved by architectures far weaker than
        attention, so a generator that quietly queried one pair would produce a
        benchmark everything passes (docs/archive/notes/006)."""
        seq = generate(BASE)
        self.assertEqual(len(seq.query_positions), BASE.n_pairs)
        self.assertEqual(set(seq.tokens[i] for i in seq.query_positions),
                         set(seq.pairs))

    def test_keys_and_values_occupy_disjoint_ranges(self):
        """A model that confused a key for a value must not be able to score."""
        seq = generate(BASE)
        for value in seq.pairs.values():
            self.assertGreaterEqual(value, BASE.n_keys)
        for key in seq.pairs:
            self.assertLess(key, BASE.n_keys)

    def test_scored_targets_excludes_ignored_positions(self):
        """Scoring over the whole sequence would dilute any measurement with a
        large number of free correct answers — most positions are IGNORE."""
        seq = generate(BASE)
        self.assertEqual(len(seq.scored_targets()), BASE.n_pairs)
        self.assertNotIn(IGNORE, seq.scored_targets())


class TestAutoregressiveMode(unittest.TestCase):
    """The layout that makes docs/archive/notes/001 P2 actually true.

    P2 claimed the self-supervised objective and the task metric are one
    quantity. g1-01 found that false in the classification layout: the answer
    is a label beside the stream and never appears in it, so "predict your next
    input" and "answer the query" are different questions. These tests pin the
    property P2 needs.
    """

    AR = replace(BASE, autoregressive=True, seq_len=48)

    def test_the_next_token_after_a_query_is_that_query_s_answer(self):
        """P2, as an executable assertion rather than a claim in a note."""
        seq = generate(self.AR)
        for position in seq.query_positions:
            self.assertEqual(seq.tokens[position + 1], seq.targets[position])

    def test_classification_mode_never_emits_the_answer_positionally(self):
        """The contrast that makes the flag mean something. Without this, an
        autoregressive-only generator would pass every test above while the
        flag did nothing."""
        seq = generate(replace(self.AR, autoregressive=False))
        self.assertEqual(seq.answer_positions, ())

    def test_answer_positions_are_classified_as_answers_not_filler(self):
        """The emitted answer is the single most task-relevant position in the
        sequence. Defaulting it to "filler" would exclude it from exactly the
        measurement it exists for, and the predictability probe splits on
        precisely this classification."""
        seq = generate(self.AR)
        kinds = seq.position_kinds()
        for position in seq.answer_positions:
            self.assertEqual(kinds[position], "answer")
        self.assertEqual(len(seq.answer_positions), self.AR.n_pairs)

    def test_answer_positions_follow_query_positions(self):
        seq = generate(self.AR)
        self.assertEqual(seq.answer_positions,
                         tuple(p + 1 for p in seq.query_positions))

    def test_autoregressive_needs_more_room_per_pair(self):
        self.assertEqual(replace(BASE, n_pairs=4).min_seq_len, 12)
        self.assertEqual(replace(BASE, n_pairs=4, autoregressive=True).min_seq_len, 16)
        with self.assertRaises(ValueError):
            replace(BASE, n_pairs=4, seq_len=14, autoregressive=True)

    def test_the_flag_changes_the_sequence(self):
        a = generate(replace(self.AR, autoregressive=False))
        b = generate(self.AR)
        self.assertNotEqual(a.tokens, b.tokens)

    def test_autoregressive_sequences_are_exactly_seq_len(self):
        """The mutation `autoregressive-flag-inert` survived the whole suite
        until this existed.

        Setting the query width back to 1 while still emitting an answer per
        query makes every sequence *longer* than requested — the layout silently
        overruns. Nothing asserted the length in autoregressive mode: the
        existing length test only ran the classification layout, so a region of
        the generator was covered by tests that could never see it. Rule 10's
        failure mode, third instance.
        """
        for n_pairs, seq_len in ((2, 24), (4, 48), (6, 80)):
            for autoregressive in (False, True):
                with self.subTest(n_pairs=n_pairs, autoregressive=autoregressive):
                    seq = generate(replace(BASE, n_pairs=n_pairs, seq_len=seq_len,
                                           autoregressive=autoregressive))
                    self.assertEqual(len(seq.tokens), seq_len)
                    self.assertEqual(len(seq.targets), seq_len)


class TestConfigurationIsConnected(unittest.TestCase):
    """Rule 6: perturb the input, assert the output moves.

    Each of these would pass against a generator that ignored the field it
    names.
    """

    def test_seed_changes_the_sequence(self):
        self.assertNotEqual(generate(replace(BASE, seed=1)).tokens,
                            generate(replace(BASE, seed=2)).tokens)

    def test_same_seed_reproduces_exactly(self):
        self.assertEqual(generate(replace(BASE, seed=3)).tokens,
                         generate(replace(BASE, seed=3)).tokens)

    def test_n_pairs_changes_how_many_queries_are_scored(self):
        self.assertEqual(len(generate(replace(BASE, n_pairs=2)).query_positions), 2)
        self.assertEqual(len(generate(replace(BASE, n_pairs=6)).query_positions), 6)

    def test_seq_len_changes_the_sequence_length(self):
        self.assertEqual(len(generate(replace(BASE, seq_len=40)).tokens), 40)
        self.assertEqual(len(generate(replace(BASE, seq_len=80)).tokens), 80)

    def test_filler_mode_changes_the_filler_but_not_the_answers(self):
        """The dial that carries docs/archive/notes/002 §7's unresolved tension. If the
        modes produced identical sequences the sweep would silently compare a
        condition against itself."""
        a = generate(replace(BASE, filler="random"))
        b = generate(replace(BASE, filler="structured"))
        c = generate(replace(BASE, filler="none"))
        self.assertNotEqual(a.tokens, b.tokens)
        self.assertNotEqual(b.tokens, c.tokens)
        # ...while the task itself is unchanged.
        self.assertEqual(a.pairs, b.pairs)
        self.assertEqual(a.query_positions, b.query_positions)

    def test_structured_filler_is_predictable_from_position_and_random_is_not(self):
        """The property the whole filler distinction exists for, and the reason
        docs/archive/notes/002 §7's tension may be resolvable: structured filler must be
        a function of *absolute* position, so a predictive objective has signal,
        while remaining irrelevant to the answer.

        Asserted over absolute body offset, not over the filler tokens compacted
        together. The first version of this test compacted them and expected
        periodicity in the compacted index; that fails (152 of an expected 180
        matches) because query positions remove entries from the compacted list
        while the generator's cycle advances with absolute offset regardless.
        The generator was right and the assertion was measuring the wrong index
        space.
        """
        cfg = replace(BASE, seq_len=200, n_pairs=2)
        body_start = cfg.n_pairs * 2

        def filler_by_residue(seq, period):
            groups: dict[int, set[int]] = {}
            for i, tok in enumerate(seq.tokens):
                if i >= body_start and seq.targets[i] == IGNORE:
                    groups.setdefault((i - body_start) % period, set()).add(tok)
            return groups

        struct = generate(replace(cfg, filler="structured"))
        rand = generate(replace(cfg, filler="random"))
        period = len([k for k in range(cfg.n_keys) if k not in struct.pairs])

        for residue, tokens in filler_by_residue(struct, period).items():
            self.assertEqual(len(tokens), 1,
                             f"structured filler at offsets ≡{residue} (mod {period}) "
                             f"should be one token, got {sorted(tokens)}")

        varying = sum(len(t) > 1 for t in filler_by_residue(rand, period).values())
        self.assertGreater(varying, 0,
                           "random filler should not be a function of position")

    def test_n_values_changes_the_answer_alphabet(self):
        """This sets the base rate. A generator ignoring it would make every
        base-rate measurement wrong in the same direction."""
        wide = dataset(replace(BASE, n_values=8), 60)
        narrow = dataset(replace(BASE, n_values=2), 60)
        self.assertGreater(
            len({v for s in wide for v in s.scored_targets()}),
            len({v for s in narrow for v in s.scored_targets()}),
        )


class TestRejectsImpossibleConfigurations(unittest.TestCase):
    """Configurations that cannot produce a well-posed task must raise rather
    than emit something plausible-looking."""

    def test_rejects_more_pairs_than_keys(self):
        with self.assertRaises(ValueError):
            MqarConfig(n_pairs=8, n_keys=4, seq_len=40)

    def test_rejects_no_spare_key_for_filler(self):
        with self.assertRaises(ValueError):
            MqarConfig(n_pairs=8, n_keys=8, seq_len=40, filler="structured")

    def test_rejects_sequence_too_short_to_hold_the_task(self):
        with self.assertRaises(ValueError):
            MqarConfig(n_pairs=8, n_keys=16, seq_len=10)

    def test_rejects_unknown_filler_mode(self):
        with self.assertRaises(ValueError):
            MqarConfig(filler="gaussian")


class TestDataset(unittest.TestCase):
    def test_sequences_differ_from_each_other(self):
        seqs = dataset(BASE, 20)
        self.assertGreater(len({s.tokens for s in seqs}), 1)

    def test_dataset_is_reproducible_from_one_seed(self):
        self.assertEqual([s.tokens for s in dataset(replace(BASE, seed=5), 10)],
                         [s.tokens for s in dataset(replace(BASE, seed=5), 10)])


if __name__ == "__main__":
    unittest.main()
