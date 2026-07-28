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
from openplexus.search import margin, search, walk_from

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
