"""The slow store has to actually survive the sequence, and actually matter.

Decision 62 found that `memory = np.zeros((d, d))` sits inside `run`, so the
associative store is rebuilt every sequence -- and `lasting`, the consolidated
store, was built there too. Consolidation's two timescales both sat INSIDE one
sequence, and the only thing carrying across a corpus was `Wo`, one `vocab x d`
linear map.

Note 042: GOALS section 1.2 asks for a map of how concepts relate, and **a map
needs somewhere to live.**

Two failure modes are being guarded against, and they look identical from the
outside:

- **The store does not carry.** Decision 62 was found by noticing that
  `learn=False` predictions were byte-identical whether or not another sequence
  had run first. That is what "nothing persists" looks like.
- **The store carries and changes nothing.** Decision 79 caught a write gate
  producing numbers identical to the baseline to the last decimal, because the
  flag it depended on was not set. An inert mechanism reads as a null result.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (LocalAssociativeMemory,
                                            LocalMemoryConfig)

TOKENS = np.array([3, 9, 1, 7, 3, 5, 11, 2, 9, 4, 6, 1], dtype=np.int64)


def build(persistent: bool, **overrides) -> LocalAssociativeMemory:
    settings = dict(d_model=32, vocab_size=20, seed=0, consolidation=0.5,
                    lasting_cap=5.0, decay=0.9)
    settings.update(overrides)
    return LocalAssociativeMemory(LocalMemoryConfig(
        persistent_lasting=persistent, **settings))


#: Next-token targets, so `Wo` is trained and predictions mean something.
#:
#: **Without this every test below passes vacuously.** An untrained `Wo` is
#: zeros, `argmax` of a zero readout is token 0 at every position, and two
#: models agree perfectly whatever their stores contain. The first version of
#: these tests compared runs on an untrained model and reported "the persistent
#: store changes nothing" -- which was true, and was about the readout.
TARGETS = np.concatenate([TOKENS[1:], TOKENS[-1:]])
SCORED = np.ones(len(TOKENS), dtype=bool)


def learn(model: LocalAssociativeMemory, rounds: int) -> None:
    for _ in range(rounds):
        model.run(TOKENS, TARGETS, SCORED, learn=True)


class ItActuallyCarries(unittest.TestCase):

    def test_the_slow_store_grows_across_sequences(self):
        """Trained, because consolidation fires on `predictions[t-1] == token`.

        It promotes what the model CORRECTLY PREDICTED, so an untrained readout
        -- which answers token 0 everywhere -- consolidates only where the
        sequence happens to contain a zero. The first version of this test used
        an unlearned model and a sequence with no zeros, and reported that
        nothing was ever consolidated. That was true and it was about the
        readout, not about persistence.
        """
        model = build(True)
        learn(model, 4)
        first = float(np.linalg.norm(model._lasting))
        learn(model, 4)
        second = float(np.linalg.norm(model._lasting))
        self.assertGreater(first, 0.0, "nothing was consolidated at all")
        self.assertGreater(
            second, first,
            "the slow store did not grow over four more sequences, so it is "
            "being rebuilt rather than carried")

    def test_WITHOUT_the_flag_nothing_carries(self):
        """The control, and the exact observation decision 62 was found by.

        Scored with `learn=False` after training, so `Wo` is fixed and the only
        thing that could differ between the two runs is carried state.
        """
        model = build(False)
        learn(model, 8)
        first = model.run(TOKENS)
        second = model.run(TOKENS)
        np.testing.assert_array_equal(
            first, second,
            "answers changed between identical sequences with persistence OFF, "
            "so something is carrying that should not be")
        self.assertIsNone(model._lasting)

    def test_forgetting_is_explicit_and_works(self):
        model = build(True)
        model.run(TOKENS)
        self.assertIsNotNone(model._lasting)
        model.forget_lasting()
        self.assertIsNone(model._lasting)


class ItActuallyMATTERS(unittest.TestCase):
    """Carrying state that changes no answer is an inert flag.

    Decision 79's calibration: a write gate produced numbers identical to the
    baseline to the last decimal because the field it needed was unset, and that
    reads as a null result rather than as a bug.
    """

    def test_a_carried_store_changes_the_answers_eventually(self):
        """Compared against a FRESH model on the same sequence.

        Not against the same model's first run -- the fast store makes early
        sequences differ for reasons that have nothing to do with persistence.
        A fresh model is the honest control: same weights, same input, and the
        only difference is what the slow store has accumulated.
        """
        warmed = build(True)
        learn(warmed, 8)
        after = warmed.run(TOKENS)

        # Same amount of LEARNING, so `Wo` matches -- then the slow store is
        # dropped and only it differs. Training a fresh model would confound
        # the readout with the store.
        fresh = build(True)
        learn(fresh, 8)
        fresh.forget_lasting()
        before = fresh.run(TOKENS)

        self.assertFalse(
            np.array_equal(before, after),
            "eight sequences of accumulation changed no answer, so the "
            "persistent store is inert -- which reads as a null result and is "
            "a bug (decision 79)")

    def test_the_two_flags_are_not_the_same_model(self):
        on, off = build(True), build(False)
        learn(on, 8)
        learn(off, 8)
        self.assertFalse(np.array_equal(on.run(TOKENS), off.run(TOKENS)),
                         "persistence on and off agree after eight sequences")


class ItRefusesToBeSilentlyInert(unittest.TestCase):

    def test_persistence_without_consolidation_is_refused(self):
        """There is nothing to persist without a mechanism that promotes into
        it, and a flag that does nothing is worse than one that errors."""
        with self.assertRaises(ValueError):
            LocalMemoryConfig(d_model=32, vocab_size=20,
                              persistent_lasting=True, consolidation=0.0)


if __name__ == "__main__":
    unittest.main()
