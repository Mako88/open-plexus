"""The first external instrument, and the properties that would silently corrupt it.

`openplexus/tasks/clutrr.py` reads CLUTRR's relational layer. Three of these tests
guard things the data does that an obvious implementation gets wrong:

- the graphs are **not paths** — 433 of 10,220 revisit a node
- the answer vocabulary is **larger** than the input vocabulary
- `max_appearances` is note 059's confound and has to be exposed, because train
  contains none of it and test is 37.8%

**Most of this file runs without the download.** CLUTRR is fetched rather than
committed, so a suite that needed `data/clutrr` would skip in CI — and a test that
skips is a test that does not run. The core logic is exercised against a CSV written
into a temp directory; only the reproduction of the real corpus counts is conditional.
"""

from __future__ import annotations

import csv
import pathlib
import tempfile
import unittest

from openplexus.tasks.clutrr import (
    FACT, QUERY, RELATIONS, ClutrrConfig, Puzzle, by_repetition, load)

REAL = pathlib.Path(__file__).resolve().parents[1] / "data" / "clutrr"
HAS_REAL = (REAL / "gen_train23_test2to10" / "test.csv").exists()

COLUMNS = ("id", "story", "query", "target", "target_text", "clean_story",
           "proof_state", "f_comb", "task_name", "story_edges", "edge_types",
           "query_edge", "genders", "task_split")


def write(rows, directory: pathlib.Path) -> ClutrrConfig:
    """A CLUTRR-shaped CSV, so the loader is testable without the download."""
    out = directory / "cfg"
    out.mkdir(parents=True, exist_ok=True)
    with (out / "test.csv").open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=COLUMNS)
        writer.writeheader()
        for i, (edges, types, query, target) in enumerate(rows):
            writer.writerow({c: "" for c in COLUMNS} | {
                "id": str(i), "story_edges": repr(edges),
                "edge_types": repr(list(types)), "query_edge": repr(query),
                "target_text": target})
    return ClutrrConfig(root=directory, config="cfg", split="test")


class TheLayoutIsTheOneThatAlreadyWorks(unittest.TestCase):
    """`FACT s o r` per edge, then `QUERY s o`. `closure.py`'s ordering."""

    def puzzle(self, directory) -> tuple[Puzzle, ClutrrConfig]:
        cfg = write([([(0, 1), (1, 2)], ["father", "sister"], (0, 2), "aunt")],
                    pathlib.Path(directory))
        return load(cfg)[0], cfg

    def test_each_fact_is_marker_subject_object_relation(self):
        with tempfile.TemporaryDirectory() as directory:
            p, cfg = self.puzzle(directory)
            e0, e1, e2 = (cfg.entity_base + i for i in range(3))
            father = cfg.relation_base + RELATIONS.index("father")
            sister = cfg.relation_base + RELATIONS.index("sister")
            self.assertEqual(p.tokens[:8],
                             (FACT, e0, e1, father, FACT, e1, e2, sister))

    def test_the_query_names_the_pair_and_not_the_answer(self):
        # A question whose answer follows it in the stream is not a question.
        with tempfile.TemporaryDirectory() as directory:
            p, cfg = self.puzzle(directory)
            e0, e2 = cfg.entity_base, cfg.entity_base + 2
            self.assertEqual(p.tokens[8:], (QUERY, e0, e2))
            self.assertNotIn(p.target, p.tokens)

    def test_query_position_points_at_the_object(self):
        with tempfile.TemporaryDirectory() as directory:
            p, cfg = self.puzzle(directory)
            self.assertEqual(p.tokens[p.query_position], cfg.entity_base + 2)

    def test_the_target_is_the_relation(self):
        with tempfile.TemporaryDirectory() as directory:
            p, cfg = self.puzzle(directory)
            self.assertEqual(p.target,
                             cfg.relation_base + RELATIONS.index("aunt"))


class TheGraphsAreNotPaths(unittest.TestCase):
    """433 of 10,220 revisit a node. An `(i, i+1)` assumption mis-reads them."""

    #: The real shape from the test split: 0->1->2->1->3, node 1 four times.
    WALK = ([(0, 1), (1, 2), (2, 1), (1, 3)],
            ["sister", "mother", "daughter", "uncle"], (0, 3), "uncle")

    def test_a_walk_that_revisits_a_node_loads(self):
        with tempfile.TemporaryDirectory() as directory:
            cfg = write([self.WALK], pathlib.Path(directory))
            p = load(cfg)[0]
            self.assertEqual(p.hops, 4)

    def test_a_revisited_node_gets_ONE_entity_slot(self):
        # The failure this guards: renumbering per edge rather than per node would
        # give node 1 several slots, and the store would never see the repeat --
        # which is exactly the difficulty note 059 says must stay visible.
        with tempfile.TemporaryDirectory() as directory:
            cfg = write([self.WALK], pathlib.Path(directory))
            p = load(cfg)[0]
            entities = {t for t in p.tokens
                        if cfg.entity_base <= t < cfg.relation_base}
            self.assertEqual(len(entities), 4)

    def test_max_appearances_counts_the_repeat(self):
        with tempfile.TemporaryDirectory() as directory:
            cfg = write([self.WALK], pathlib.Path(directory))
            self.assertEqual(load(cfg)[0].max_appearances, 4)


class TheVocabularyIsFixedAcrossSplits(unittest.TestCase):
    """The load-bearing one. Two splits must mean the same tokens."""

    def test_relation_ids_do_not_depend_on_what_the_file_contains(self):
        # Deriving ids from whichever split is open is an error that produces
        # numbers and no exception: a model trained on one split would be reading
        # different tokens on the other.
        with tempfile.TemporaryDirectory() as directory:
            one = write([([(0, 1)], ["father"], (0, 1), "father")],
                        pathlib.Path(directory) / "a")
            two = write([([(0, 1)], ["sister"], (0, 1), "sister")],
                        pathlib.Path(directory) / "b")
            self.assertEqual(one.vocab_size, two.vocab_size)
            self.assertEqual(one.relation_base, two.relation_base)
            father = load(one)[0].target - one.relation_base
            sister = load(two)[0].target - two.relation_base
            self.assertNotEqual(father, sister)

    def test_the_answer_vocabulary_is_larger_than_the_input_vocabulary(self):
        # Six relations appear only as targets. A model here must emit relations it
        # has never seen stated, which is a property worth asserting rather than
        # discovering.
        targets_only = {"nephew", "niece", "daughter-in-law", "father-in-law",
                        "mother-in-law", "son-in-law"}
        self.assertTrue(targets_only <= set(RELATIONS))
        self.assertEqual(len(RELATIONS), 20)


class ItRefusesWhatWouldProduceNumbersAnyway(unittest.TestCase):

    def test_a_relation_outside_the_fixed_vocabulary_is_refused(self):
        with tempfile.TemporaryDirectory() as directory:
            cfg = write([([(0, 1)], ["second-cousin"], (0, 1), "father")],
                        pathlib.Path(directory))
            with self.assertRaises(ValueError):
                load(cfg)

    def test_a_graph_and_its_labels_disagreeing_is_refused(self):
        # Pairing by position would mislabel every edge after the mismatch.
        with tempfile.TemporaryDirectory() as directory:
            cfg = write([([(0, 1), (1, 2)], ["father"], (0, 2), "aunt")],
                        pathlib.Path(directory))
            with self.assertRaises(ValueError):
                load(cfg)

    def test_too_few_entity_slots_is_refused(self):
        # Four entities into three slots. Without the check the fourth would take a
        # RELATION's id and every fact about it would name a relationship.
        with tempfile.TemporaryDirectory() as directory:
            base = write([([(0, 1), (1, 2), (2, 3)],
                           ["father", "sister", "mother"], (0, 3), "aunt")],
                         pathlib.Path(directory))
            tight = ClutrrConfig(root=base.root, config="cfg", split="test",
                                 max_entities=3)
            with self.assertRaises(ValueError):
                load(tight)

    def test_a_missing_file_says_how_to_get_it(self):
        with tempfile.TemporaryDirectory() as directory:
            cfg = ClutrrConfig(root=pathlib.Path(directory), split="test")
            with self.assertRaises(FileNotFoundError):
                load(cfg)

    def test_an_unknown_split_is_refused(self):
        with self.assertRaises(ValueError):
            ClutrrConfig(root=REAL, split="dev")


class TheRepetitionSplitIsCompleteAndDisjoint(unittest.TestCase):

    def test_the_two_arms_partition_the_set(self):
        with tempfile.TemporaryDirectory() as directory:
            cfg = write([
                ([(0, 1), (1, 2)], ["father", "sister"], (0, 2), "aunt"),
                ([(0, 1), (1, 2), (2, 1), (1, 3)],
                 ["sister", "mother", "daughter", "uncle"], (0, 3), "uncle"),
            ], pathlib.Path(directory))
            puzzles = load(cfg)
            plain, repeated = (by_repetition(puzzles, False),
                               by_repetition(puzzles, True))
            self.assertEqual(len(plain) + len(repeated), len(puzzles))
            self.assertEqual(len(plain), 1)
            self.assertEqual(len(repeated), 1)
            self.assertTrue(all(p.max_appearances <= 2 for p in plain))


@unittest.skipUnless(HAS_REAL, "CLUTRR not fetched; run tools/fetch_clutrr.py")
class ItReproducesTheCorpusCounts(unittest.TestCase):
    """The reproduction gate. Note 059's numbers, recomputed through the loader.

    Conditional because the data is fetched rather than committed. Everything above
    runs without it, so a clone with no data still exercises the loader.
    """

    def test_train_has_no_repeated_entities_at_all(self):
        puzzles = load(ClutrrConfig(root=REAL, split="train"))
        self.assertEqual(len(puzzles), 9074)
        self.assertEqual(len(by_repetition(puzzles, True)), 0)

    def test_test_is_38_percent_repeated(self):
        puzzles = load(ClutrrConfig(root=REAL, split="test"))
        self.assertEqual(len(puzzles), 1146)
        self.assertEqual(len(by_repetition(puzzles, True)), 433)
        self.assertEqual(len(by_repetition(puzzles, False)), 713)

    def test_train_is_two_and_three_hops_only(self):
        puzzles = load(ClutrrConfig(root=REAL, split="train"))
        self.assertEqual({p.hops for p in puzzles}, {2, 3})

    def test_test_reaches_ten_hops(self):
        puzzles = load(ClutrrConfig(root=REAL, split="test"))
        self.assertEqual(max(p.hops for p in puzzles), 10)


if __name__ == "__main__":
    unittest.main()
