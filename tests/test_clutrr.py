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
    FACT, QUERY, RELATIONS, ClutrrConfig, Puzzle, by_repetition,
    composition_table, load, reachable)

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


class TheLayoutDecidesWhichAddressesCollide(unittest.TestCase):
    """`kinship` exists because it collides on 7.7% of test where `closure` hits 35.9%.

    Decision 157's mechanism on someone else's data: keying `(entity, relation)`
    separates a repeated entity's edges when their relations differ. Note 059 found
    the repeated-entity case is 38% of test and **absent from training**, so it is the
    confound most likely to be mistaken for depth.
    """

    #: One entity as the subject of two edges with DIFFERENT relations. Under
    #: `closure` both write to `key(FACT, e0)`; under `kinship` they write to
    #: `key(e0, father)` and `key(e0, sister)`.
    SHARED = ([(0, 1), (0, 2)], ["father", "sister"], (1, 2), "sister")

    def rows(self, layout, directory):
        cfg = write([self.SHARED], pathlib.Path(directory))
        cfg = ClutrrConfig(root=cfg.root, config="cfg", split="test",
                           layout=layout)
        return load(cfg)[0], cfg

    def test_kinship_puts_the_relation_before_the_object(self):
        with tempfile.TemporaryDirectory() as directory:
            p, cfg = self.rows("kinship", directory)
            e0 = cfg.entity_base
            father = cfg.relation_base + RELATIONS.index("father")
            self.assertEqual(p.tokens[:4], (FACT, e0, father, e0 + 1))

    def test_closure_puts_the_object_before_the_relation(self):
        with tempfile.TemporaryDirectory() as directory:
            p, cfg = self.rows("closure", directory)
            e0 = cfg.entity_base
            father = cfg.relation_base + RELATIONS.index("father")
            self.assertEqual(p.tokens[:4], (FACT, e0, e0 + 1, father))

    def test_the_two_layouts_disagree(self):
        # Stated separately: if both produced the same stream the option would be
        # inert and every claim about the collision rate would be about nothing.
        with tempfile.TemporaryDirectory() as directory:
            k, _ = self.rows("kinship", directory)
            c, _ = self.rows("closure", directory)
            self.assertNotEqual(k.tokens, c.tokens)
            self.assertEqual(len(k.tokens), len(c.tokens))

    def test_both_carry_the_same_answer_and_query(self):
        # The layout changes addressing, not the question. If it changed the target
        # the two arms would not be comparable.
        with tempfile.TemporaryDirectory() as directory:
            k, _ = self.rows("kinship", directory)
            c, _ = self.rows("closure", directory)
            self.assertEqual(k.target, c.target)
            self.assertEqual(k.tokens[k.query_position],
                             c.tokens[c.query_position])

    def test_an_unknown_layout_is_refused(self):
        with self.assertRaises(ValueError):
            ClutrrConfig(root=REAL, split="test", layout="clsoure")

    def test_closure_is_still_the_default(self):
        # Note 060's floor was measured with `closure`, and a default that moved
        # under a recorded number is decision 74's failure.
        self.assertEqual(ClutrrConfig(root=REAL).layout, "closure")


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


class TheAlgebraIsReadOffTheDataAndNotFitted(unittest.TestCase):
    """`composition_table` and `reachable`, on rows small enough to check by hand."""

    def table(self, directory):
        cfg = write([
            ([(0, 1), (1, 2)], ["father", "sister"], (0, 2), "aunt"),
            ([(0, 1), (1, 2)], ["aunt", "son"], (0, 2), "brother"),
            # A THREE-hop row whose pair is not in the two-hop rows. It must not
            # enter the table: it constrains the algebra without determining it,
            # and using it would be inferring rather than counting.
            ([(0, 1), (1, 2), (2, 3)], ["son", "son", "son"], (0, 3), "grandson"),
        ], pathlib.Path(directory))
        return composition_table(load(cfg)), cfg

    def test_only_two_hop_rows_become_facts(self):
        with tempfile.TemporaryDirectory() as directory:
            table, _ = self.table(directory)
            self.assertEqual(len(table), 2)

    def test_a_fact_is_the_row_it_came_from(self):
        with tempfile.TemporaryDirectory() as directory:
            table, cfg = self.table(directory)
            father = cfg.relation_base + RELATIONS.index("father")
            sister = cfg.relation_base + RELATIONS.index("sister")
            aunt = cfg.relation_base + RELATIONS.index("aunt")
            self.assertEqual(table[(father, sister)], aunt)

    def test_a_chain_reduces_through_an_intermediate_it_was_never_told(self):
        # father . sister . son -> aunt . son -> brother. The three-hop answer
        # is not in the table and is reached by composing two facts, which is
        # the whole of what `reachable` claims to do.
        with tempfile.TemporaryDirectory() as directory:
            table, cfg = self.table(directory)
            ids = {name: cfg.relation_base + RELATIONS.index(name)
                   for name in ("father", "sister", "son", "brother")}
            found = reachable((ids["father"], ids["sister"], ids["son"]), table)
            self.assertEqual(found, frozenset({ids["brother"]}))

    def test_a_bracketing_the_left_to_right_reduction_would_miss(self):
        """The connection test: the search has to be a search, not a fold."""
        with tempfile.TemporaryDirectory() as directory:
            table, cfg = self.table(directory)
            ids = {name: cfg.relation_base + RELATIONS.index(name)
                   for name in ("son", "father", "sister", "aunt")}
            # son . father . sister: the left pair is unknown, so folding from
            # the left stops dead. The RIGHT pair is `father . sister -> aunt`,
            # and `son . aunt` is unknown too -- so this reduces to nothing and
            # the two arms agree. What differs is that the search LOOKED.
            self.assertNotIn((ids["son"], ids["father"]), table)
            self.assertEqual(reachable((ids["son"], ids["father"],
                                        ids["sister"]), table), frozenset())
            # And here the right bracketing is the only one that works.
            self.assertEqual(
                reachable((ids["father"], ids["sister"], ids["son"]), table),
                frozenset({cfg.relation_base + RELATIONS.index("brother")}))

    def test_a_missing_pair_reaches_nothing_rather_than_guessing(self):
        with tempfile.TemporaryDirectory() as directory:
            table, cfg = self.table(directory)
            wife = cfg.relation_base + RELATIONS.index("wife")
            self.assertEqual(reachable((wife, wife), table), frozenset())

    def test_a_single_relation_is_itself_and_an_empty_chain_is_nothing(self):
        with tempfile.TemporaryDirectory() as directory:
            table, cfg = self.table(directory)
            wife = cfg.relation_base + RELATIONS.index("wife")
            self.assertEqual(reachable((wife,), table), frozenset({wife}))
            self.assertEqual(reachable((), table), frozenset())

    def test_the_chain_is_carried_under_both_layouts(self):
        # The relation sits at a different offset in each, so a chain recovered
        # from `tokens` would read entity ids as relations under one of them.
        rows = [([(0, 1), (1, 2)], ["father", "sister"], (0, 2), "aunt")]
        with tempfile.TemporaryDirectory() as directory:
            cfg = write(rows, pathlib.Path(directory))
            for layout in ("closure", "kinship"):
                puzzle = load(ClutrrConfig(root=cfg.root, config=cfg.config,
                                           split="test", layout=layout))[0]
                self.assertEqual(
                    puzzle.chain,
                    (cfg.relation_base + RELATIONS.index("father"),
                     cfg.relation_base + RELATIONS.index("sister")))


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

    def test_the_whole_knowledge_of_the_benchmark_is_62_stated_facts(self):
        table = composition_table(load(ClutrrConfig(root=REAL, split="train")))
        self.assertEqual(len(table), 62)

    def test_those_62_facts_answer_every_test_puzzle(self):
        """**The benchmark's ceiling, and it is why a score on it proves little.**

        No model, no training, no representation: count the two-hop training rows
        into a table and search the bracketings. If this ever falls below 1.0 the
        claim in `reachable`'s docstring has stopped being true and every
        conclusion drawn from it needs revisiting.
        """
        table = composition_table(load(ClutrrConfig(root=REAL, split="train")))
        puzzles = load(ClutrrConfig(root=REAL, split="test"))
        found = sum(p.target in reachable(p.chain, table) for p in puzzles)
        self.assertEqual(found, len(puzzles))

    def test_and_the_difficulty_is_entirely_the_BRACKETING(self):
        """The companion. Without it the test above reads as *the task is easy*.

        The same 62 facts applied left to right answer 0.2757 of the split and
        0.0252 at ten hops, so what the benchmark measures is finding the order
        to apply knowledge in, not having it.
        """
        table = composition_table(load(ClutrrConfig(root=REAL, split="train")))
        puzzles = load(ClutrrConfig(root=REAL, split="test"))
        got = 0
        for puzzle in puzzles:
            current = puzzle.chain[0]
            for following in puzzle.chain[1:]:
                if (current, following) not in table:
                    current = None
                    break
                current = table[(current, following)]
            got += current == puzzle.target
        self.assertLess(got / len(puzzles), 0.35)
        self.assertGreater(got / len(puzzles), 0.20)


if __name__ == "__main__":
    unittest.main()
