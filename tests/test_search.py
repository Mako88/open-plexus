"""Search must find the branch that REACHES THE TARGET, not the loudest one.

Decision 108: multi-hop reasoning over a branching graph requires search, and an
associative store does retrieval. The store answers *"what relation does S
hold"* correctly; the question needs *"which of S's relations leads to T"*, and
the disambiguator -- the named object -- was never used by anything.

The test with teeth is `TheAmbiguousCaseIsWhereSearchEarnsItsPlace`. It builds a
store where the superposition's own argmax is the WRONG relation, so a greedy
traversal is not merely uncertain, it is confidently wrong. If search cannot
recover that case it has bought nothing, because that case is the whole reason
it exists.

## Why the store is built by hand here

Nothing is trained. The bindings are written directly with the model's own write
rule -- `memory += outer(value, key)` -- so the test states exactly what is in
memory rather than hoping training put it there.

That makes the fixture a reference implementation, which this repository has one
precedent for and one reason to allow: `test_decay_when_masked` needed the same
thing, because no black-box comparison could see the branch under test. Here the
point is control over the AMBIGUITY, which a trained store would supply only by
luck.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.keys import PairKeys, TableKeys
from openplexus.retrieval import SuperposedRead
from openplexus.search import beam, margin, search, walk_from

WIDTH = 256
VOCAB = 40
FACT = 0

#: Token ids, laid out so a reader can follow the graph in the assertions.
ALICE, BOB, CAROL, DAVE = 10, 11, 12, 13
PARENT, SIBLING, FRIEND = 20, 21, 22


def value_table(seed: int = 0) -> np.ndarray:
    """Frozen random value vectors, one per token, unit-ish scale."""
    rng = np.random.default_rng(seed)
    return rng.normal(0.0, 1.0 / np.sqrt(WIDTH), (VOCAB, WIDTH))


class Fixture:
    """A store with bindings written into it, and the pieces to read it."""

    def __init__(self, seed: int = 0) -> None:
        self.wv = value_table(seed)
        self.keys = PairKeys(seed=seed, spread=1.0 / np.sqrt(WIDTH),
                             width=WIDTH, start=VOCAB - 1)
        self.retrieval = SuperposedRead()
        self.store = np.zeros((WIDTH, WIDTH))

    def bind(self, previous: int, token: int, target: int,
             weight: float = 1.0) -> None:
        """Write `key(previous, token) -> value(target)`, the model's rule."""
        self.store += weight * np.outer(self.wv[target],
                                        self.keys.pair(previous, token))

    def state_fact(self, subject: int, relation: int, obj: int,
                   weight: float = 1.0) -> None:
        """A fact laid out `FACT S R O` writes the two bindings a walk uses.

        `key(FACT, S) -> R` is what names the subject's relation, and
        `key(S, R) -> O` is what follows it. Those are steps 1 and 2 of decision
        107's traversal, and they are the only two operations a walk performs.
        """
        self.bind(FACT, subject, relation, weight)
        self.bind(subject, relation, obj, weight)

    def find(self, start: int, target: int, depth: int, branches: int):
        return search(self.store, self.retrieval, self.keys, self.wv,
                      FACT, start, self.wv[target], depth, branches)


class AWalkFollowsTheBranchItWasGiven(unittest.TestCase):

    def test_it_commits_to_the_relation_handed_in(self):
        """A branch ASSERTS a candidate. If the walk re-decoded the first
        relation it would not be a branch, it would be another retrieval, and
        searching would be indistinguishable from greedy."""
        fixture = Fixture()
        fixture.state_fact(ALICE, PARENT, BOB)
        fixture.state_fact(ALICE, FRIEND, DAVE)

        walk = walk_from(fixture.store, fixture.retrieval, fixture.keys,
                         fixture.wv, FACT, ALICE, FRIEND, depth=1)
        self.assertEqual(walk.relations, (FRIEND,))

    def test_a_two_step_walk_passes_through_the_middle_entity(self):
        fixture = Fixture()
        fixture.state_fact(ALICE, PARENT, BOB)
        fixture.state_fact(BOB, SIBLING, CAROL)

        walk = walk_from(fixture.store, fixture.retrieval, fixture.keys,
                         fixture.wv, FACT, ALICE, PARENT, depth=2)
        self.assertEqual(walk.entities, (BOB,),
                         "the walk should have passed through Bob")
        self.assertEqual(walk.relations, (PARENT, SIBLING))

    def test_depth_zero_is_refused(self):
        fixture = Fixture()
        with self.assertRaises(ValueError):
            walk_from(fixture.store, fixture.retrieval, fixture.keys,
                      fixture.wv, FACT, ALICE, PARENT, depth=0)


class TheAmbiguousCaseIsWhereSearchEarnsItsPlace(unittest.TestCase):
    """The case decision 108 is about, constructed so greedy gets it WRONG.

    Alice holds two relations. `key(FACT, Alice)` returns their sum, and the
    weights are set so the sum's argmax is FRIEND -- the branch that does NOT
    reach Carol. A greedy traversal therefore commits to FRIEND with confidence.

    Only the endpoint tells them apart, and the endpoint is what search scores.
    """

    def setUp(self):
        self.fixture = Fixture()
        # FRIEND is written more strongly, so it dominates the superposition.
        self.fixture.state_fact(ALICE, FRIEND, DAVE, weight=2.0)
        self.fixture.state_fact(ALICE, PARENT, BOB, weight=1.0)
        # Only the PARENT branch continues to Carol.
        self.fixture.state_fact(BOB, SIBLING, CAROL)
        self.fixture.state_fact(DAVE, SIBLING, DAVE)

    def test_greedy_commits_to_the_loudest_relation_and_is_wrong(self):
        """The control. If this ever passes, the fixture stopped being
        ambiguous and every other assertion here is vacuous."""
        greedy = self.fixture.find(ALICE, CAROL, depth=2, branches=1)
        self.assertEqual(len(greedy), 1)
        self.assertEqual(greedy[0].relations[0], FRIEND,
                         "the fixture is meant to make FRIEND the loudest; if "
                         "greedy already picks PARENT there is no ambiguity "
                         "left to test")

    def test_search_recovers_the_branch_that_reaches_the_target(self):
        walks = self.fixture.find(ALICE, CAROL, depth=2, branches=3)
        self.assertEqual(walks[0].relations[0], PARENT,
                         "search should prefer the branch whose endpoint is "
                         "Carol, not the branch with the strongest binding")
        self.assertEqual(walks[0].entities, (BOB,))

    def test_the_winning_branch_beats_the_runner_up_by_a_margin(self):
        walks = self.fixture.find(ALICE, CAROL, depth=2, branches=3)
        self.assertGreater(
            margin(walks), 0.0,
            "a search that cannot separate its top two has not decided "
            "anything, and reporting the top one would hide that")

    def test_asking_for_the_other_target_flips_the_answer(self):
        """The strongest form of the claim: the SAME store and the SAME start,
        with only the target changed, must produce the other branch. If it does
        not, search is reading something about the store rather than about the
        question."""
        walks = self.fixture.find(ALICE, DAVE, depth=2, branches=3)
        self.assertEqual(walks[0].relations[0], FRIEND)


class BranchingAtEVERYStepNotOnlyTheRoot(unittest.TestCase):
    """`beam`, and note 064 is why it exists.

    `search` commits to a first relation and then takes `argmax` at every later step,
    so it hedges only at the root. On CLUTRR that root decode is 0.974 while the later
    ones run 0.906–0.942 — the hedging was spent where it was not needed. Branching per
    step took chain recovery **0.659 → 0.873**, entirely at depth.

    The load-bearing test here is `test_the_degenerate_beam_IS_the_greedy_walk`. If a
    one-wide one-branch beam were not the same walk as greedy, every comparison between
    the two mechanisms would be measuring an unrelated difference.
    """

    #: Two facts about the same subject, the wrong one written louder, so a greedy
    #: decode takes the loud branch and only branching can recover. Placed at the
    #: SECOND step, which is exactly where `search` cannot hedge.
    START, MID, GOOD, BAD, END = 1, 2, 3, 4, 5
    LOUD, QUIET, FIRST = 11, 12, 13

    def setUp(self):
        self.f = Fixture()
        # step 1: START --FIRST--> MID, unambiguous.
        self.f.state_fact(self.START, self.FIRST, self.MID)
        # step 2: MID has two relations and the WRONG one is louder.
        self.f.bind(FACT, self.MID, self.LOUD, weight=1.6)
        self.f.bind(FACT, self.MID, self.QUIET, weight=1.0)
        self.f.bind(self.MID, self.LOUD, self.BAD)
        self.f.bind(self.MID, self.QUIET, self.GOOD)

    def run_beam(self, width, branches, target):
        return beam(self.f.store, self.f.retrieval, self.f.keys, self.f.wv,
                    FACT, self.START, self.f.wv[target], 2,
                    width=width, branches=branches)

    def test_the_degenerate_beam_IS_the_greedy_walk(self):
        # THE GATE. width 1, branches 1 must reproduce `search(branches=1)` exactly,
        # or the two mechanisms are not comparable and every number is suspect.
        greedy = self.f.find(self.START, self.GOOD, 2, 1)
        degenerate = self.run_beam(1, 1, self.GOOD)
        self.assertEqual(degenerate[0].relations, greedy[0].relations)
        self.assertEqual(degenerate[0].entities, greedy[0].entities)

    def test_root_only_branching_cannot_reach_the_quiet_second_step(self):
        # `search` with four branches still fails, because its branching is at the
        # root and the ambiguity is at step two. This is note 064's finding as a test.
        walks = self.f.find(self.START, self.GOOD, 2, 4)
        self.assertEqual(walks[0].relations[1], self.LOUD)

    def test_per_step_branching_reaches_it(self):
        walks = self.run_beam(4, 4, self.GOOD)
        self.assertEqual(walks[0].relations[1], self.QUIET)

    def test_and_still_prefers_the_loud_branch_when_THAT_is_the_target(self):
        # The companion. If the beam always returned the quiet branch it would be
        # broken in a way the test above cannot see.
        walks = self.run_beam(4, 4, self.BAD)
        self.assertEqual(walks[0].relations[1], self.LOUD)

    def test_pruning_alone_buys_nothing(self):
        # THE FALSIFIER, and it fired on CLUTRR too: width 4 with one branch has
        # nothing to choose between, so it must not beat greedy. Without this the
        # gain could be attributed to keeping partials rather than to branching.
        wide = self.run_beam(4, 1, self.GOOD)
        greedy = self.run_beam(1, 1, self.GOOD)
        self.assertEqual(wide[0].relations, greedy[0].relations)

    def test_the_walk_shape_matches_search(self):
        walks = self.run_beam(4, 4, self.GOOD)
        self.assertEqual(len(walks[0].relations), 2)
        self.assertEqual(len(walks[0].retrieved), 2)
        self.assertEqual(len(walks[0].entities), 1)

    def test_it_refuses_what_search_refuses(self):
        for kwargs in (dict(width=0, branches=1), dict(width=1, branches=0)):
            with self.subTest(**kwargs):
                with self.assertRaises(ValueError):
                    beam(self.f.store, self.f.retrieval, self.f.keys, self.f.wv,
                         FACT, self.START, self.f.wv[self.GOOD], 2, **kwargs)
        with self.assertRaises(ValueError):
            beam(self.f.store, self.f.retrieval, self.f.keys, self.f.wv, FACT,
                 self.START, self.f.wv[self.GOOD], 0)
        with self.assertRaises(ValueError):
            beam(self.f.store, self.f.retrieval,
                 TableKeys(np.zeros((VOCAB, WIDTH))), self.f.wv, FACT,
                 self.START, self.f.wv[self.GOOD], 2)


class TreatingDepthAsAMaximum(unittest.TestCase):
    """`beam(any_length=True)`, and the two claims it has to earn.

    The mechanism exists because every CLUTRR figure this project has published --
    note 091's 0.8578 and g41-01's per-bucket version -- hands the walk `len(chain)`,
    parsed out of the puzzle. Published systems on that benchmark are not given it.

    Two claims, and each has a test that fails when it stops holding:

      - **A shorter walk can WIN.** If the length-1 route is the one that reaches the
        target, `any_length=True` must return it, and `any_length=False` must not.
        Without the second half the first passes whenever the flag is disconnected.
      - **It costs no extra reads.** The docstring says the endpoint of a length-k
        walk is the value hop k+1 already fetches. If that were wrong, `any_length`
        would be a bandwidth change wearing the clothes of a free one, and kill-list
        #11 is measured in reads.

    The store: `START --FIRST--> MID --QUIET--> GOOD`. Asking for MID at depth 2 makes
    the correct answer a walk SHORTER than the depth, which is the case the exact-depth
    version cannot express at all.
    """

    START, MID, GOOD = 1, 2, 3
    FIRST, QUIET = 11, 12

    def setUp(self):
        self.f = Fixture()
        self.f.state_fact(self.START, self.FIRST, self.MID)
        self.f.state_fact(self.MID, self.QUIET, self.GOOD)

    def run_beam(self, target, depth=2, **kwargs):
        return beam(self.f.store, self.f.retrieval, self.f.keys, self.f.wv,
                    FACT, self.START, self.f.wv[target], depth,
                    width=4, branches=4, **kwargs)

    def test_the_short_walk_wins_when_it_is_the_one_that_arrives(self):
        walks = self.run_beam(self.MID, any_length=True)
        self.assertEqual(len(walks[0].relations), 1)
        self.assertEqual(walks[0].relations, (self.FIRST,))

    def test_and_the_exact_depth_version_cannot_return_it(self):
        # THE COMPANION. Perturb the input, assert the output moves: with the flag
        # off, every walk is exactly `depth` long, so the length-1 answer is not
        # merely unranked -- it does not exist. Without this, the test above passes
        # whenever `any_length` is disconnected and the short walk wins anyway.
        walks = self.run_beam(self.MID, any_length=False)
        self.assertTrue(walks, "the exact-depth beam still returns something")
        self.assertTrue(all(len(w.relations) == 2 for w in walks),
                        "no walk shorter than `depth` may appear with the flag off")

    def test_it_is_not_a_preference_for_SHORT_walks(self):
        # The other companion. A mechanism that always returned the shortest walk
        # would pass the first test and be worthless, so the case where the LONG
        # walk is right has to keep working with the flag on.
        walks = self.run_beam(self.GOOD, any_length=True)
        self.assertEqual(walks[0].relations, (self.FIRST, self.QUIET))

    def test_it_costs_no_extra_reads(self):
        """The docstring's bandwidth claim, asserted rather than asserted-in-prose.

        A length-k walk's endpoint is read at `(current, relations[-1])`, which is
        exactly the pair hop k+1 issues to follow. So turning the flag on must not
        change the number of reads -- if it ever does, the free-ness claim is wrong
        and every message-count taken with it is wrong too.
        """
        counts = []
        for flag in (False, True):
            tally = [0]
            plain = self.f.retrieval, self.f.keys

            def counting(previous, token, tally=tally, plain=plain):
                tally[0] += 1
                retrieval, keys = plain
                return retrieval.read(self.f.store, keys.pair(previous, token))

            beam(self.f.store, self.f.retrieval, self.f.keys, self.f.wv, FACT,
                 self.START, self.f.wv[self.MID], 4, width=4, branches=4,
                 reader=counting, any_length=flag)
            counts.append(tally[0])
        self.assertEqual(counts[0], counts[1],
                         f"any_length changed the read count {counts[0]} -> "
                         f"{counts[1]}; the endpoint is supposed to come out of "
                         f"the follow that was happening anyway")

    def test_the_default_is_OFF(self):
        # New mechanisms default to off, so every result taken before this existed
        # reproduces without being re-run.
        default = self.run_beam(self.MID)
        explicit = self.run_beam(self.MID, any_length=False)
        self.assertEqual([w.relations for w in default],
                         [w.relations for w in explicit])


class ItRefusesWhatItCannotDo(unittest.TestCase):

    def test_single_token_keys_are_refused_rather_than_searched_badly(self):
        """Decision 105: hops and pair keys are orthogonal key spaces, measured
        cosine -0.069, and with both on the model queried a space nothing was
        written to AND STILL RETURNED ANSWERS AND ACCURACIES. A wrong number is
        worse than an error, so this raises."""
        fixture = Fixture()
        table = TableKeys(np.zeros((VOCAB, WIDTH)))
        with self.assertRaises(ValueError) as caught:
            search(fixture.store, fixture.retrieval, table, fixture.wv,
                   FACT, ALICE, fixture.wv[CAROL], depth=2, branches=2)
        self.assertIn("PairKeys", str(caught.exception))

    def test_zero_branches_is_refused(self):
        fixture = Fixture()
        with self.assertRaises(ValueError):
            fixture.find(ALICE, CAROL, depth=2, branches=0)


class TheModelRefusesWhatAWalkCannotDo(unittest.TestCase):
    """Every one of these would otherwise fail QUIETLY.

    Decision 105 is the calibration: with hops and pair keys both on, every hop
    queried a key space the store never wrote to, **and the model still returned
    answers and accuracies.** A wrong number is worse than an error, so each
    missing prerequisite raises instead.
    """

    def base(self, **overrides):
        from openplexus.models.local_memory import LocalMemoryConfig
        settings = dict(d_model=64, vocab_size=VOCAB, hops=2,
                        hop_accumulate="concat", derived_keys=True,
                        context_keys=True, search_branches=2,
                        search_fact_token=FACT, search_query_token=1)
        settings.update(overrides)
        return lambda: LocalMemoryConfig(**settings)

    def test_single_token_keys_are_refused(self):
        with self.assertRaises(ValueError):
            self.base(context_keys=False)()

    def test_depth_one_is_refused(self):
        with self.assertRaises(ValueError):
            self.base(hops=1)()

    def test_replace_is_refused_because_it_discards_every_step_but_the_last(self):
        with self.assertRaises(ValueError):
            self.base(hop_accumulate="replace")()

    def test_a_missing_fact_marker_is_refused(self):
        with self.assertRaises(ValueError):
            self.base(search_fact_token=None)()

    def test_a_missing_query_marker_is_refused(self):
        """Without a target a walk would have to score by confidence, which
        decision 93 measured at 0.628 against 0.500 for guessing."""
        with self.assertRaises(ValueError):
            self.base(search_query_token=None)()

    def test_hops_with_pair_keys_is_STILL_refused_when_search_is_off(self):
        """Decision 105's refusal stands wherever search is not the mechanism.

        Lifting it unconditionally would re-open exactly the failure it was
        written for -- a hop re-encoding through Wk into a pair-keyed store.
        """
        with self.assertRaises(ValueError):
            self.base(search_branches=0)()


class TheModelActuallyWalksWhenToldTo(unittest.TestCase):

    def test_more_branches_changes_the_answer(self):
        """If branches did nothing the wiring would be inert -- which is the
        state decision 79 caught a write gate in, producing numbers identical
        to the baseline to the last decimal.

        **The readout has to be trained for this to say anything.** Untrained,
        `Wo` is zero and every position returns token 0 whatever the retrieval
        was, so the two arms agree trivially -- the first version of this test
        compared thirty zeros against thirty zeros and would have passed under
        any wiring at all, including none.
        """
        import numpy as np

        from openplexus.models.local_memory import (LocalAssociativeMemory,
                                                    LocalMemoryConfig)
        from openplexus.tasks.kinship import IGNORE, KinshipConfig, dataset

        task = KinshipConfig(hops=2, seed=3)
        train = dataset(task, 60)
        test = dataset(KinshipConfig(hops=2, seed=90_000), 30)

        def answers(branches):
            model = LocalAssociativeMemory(LocalMemoryConfig(
                d_model=128, vocab_size=task.vocab_size, seed=0, hops=2,
                hop_accumulate="concat", derived_keys=True, context_keys=True,
                search_branches=branches, search_fact_token=task.fact_token,
                search_query_token=task.query_token))
            for sequence in train:
                tokens = np.array(sequence.tokens, dtype=np.int64)
                targets = np.array(sequence.targets, dtype=np.int64)
                model.run(tokens, targets, targets != IGNORE, learn=True)
            return [int(model.run(np.array(s.tokens, dtype=np.int64))[
                s.answer_position]) for s in test]

        greedy = answers(1)
        self.assertGreater(len(set(greedy)), 1,
                           "the readout is degenerate -- every answer is the "
                           "same token, so this test cannot distinguish "
                           "anything and would pass under any wiring")
        self.assertNotEqual(
            greedy, answers(4),
            "a greedy walk and a four-branch search returned identical answers "
            "on every one of thirty sequences, which means the branches are "
            "not reaching the readout")


class TheMarginIsHonestAboutOneBranch(unittest.TestCase):

    def test_a_single_branch_has_no_margin(self):
        """Reporting a large margin for a greedy walk would make a traversal
        that never chose anything look decisive."""
        fixture = Fixture()
        fixture.state_fact(ALICE, PARENT, BOB)
        self.assertEqual(margin(fixture.find(ALICE, BOB, 1, branches=1)), 0.0)


if __name__ == "__main__":
    unittest.main()
