"""Binding on a token PAIR, and the ceiling it lifts.

[Note 033](../docs/notes/033-the-architecture-pass.md) proved that the model
binds only adjacent tokens, so a retrieval is the sum of the values of every
token that has followed this one — a bigram count table in superposition.
**Nothing in that architecture can represent a trigram, because no trigram is
ever written down.** "Beat a bigram" was therefore a ceiling, not a target.

`context_keys` derives the key from `(t-1, t)` instead of from `t`, which makes
`previous_key` the key of `(t-2, t-1)` and turns the same three lines into a
trigram table. One line in `run` changes; nothing else does.

**The test that matters is the discriminating one**: a sequence whose next token
is determined by the previous TWO tokens and genuinely ambiguous given one. A
bigram model cannot do better than chance on it however long it trains, so the
class below is the ceiling claim stated as something the model either can or
cannot do — not as a derivation, and not as a cosine.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

WIDTH = 96
# A B C and D B E, drawn at RANDOM rather than alternated. Every step is
# determined by its predecessor except the Bs, which are followed by C or E
# depending on what came before them.
#
# The randomness is load-bearing. A repeating `A B C D B E` cycle would make
# every position predictable from any other position at a fixed offset, so a
# model reading the WRONG pair -- `(t-2, t)` instead of `(t-1, t)` -- would score
# perfectly on an alignment it never had. A mutation doing exactly that survived
# the periodic version of this test.
BLOCKS = ([0, 1, 2], [3, 1, 4])
AMBIGUOUS = 1


def sequence(count: int = 60) -> np.ndarray:
    """Equal numbers of each block, shuffled.

    Drawn independently per block, an unlucky seed puts seven `D B E` blocks
    before the first `A B C`, and the store cannot resolve a context it has not
    met yet -- it is emptied between sequences, so within one pass every
    binding has to be rewritten. Balancing first removes an artefact that has
    nothing to do with what is being measured.
    """
    order = np.array([0, 1] * (count // 2))
    np.random.default_rng(4).shuffle(order)
    return np.array([t for choice in order for t in BLOCKS[choice]],
                    dtype=np.int64)


def build(**overrides):
    config = dict(vocab_size=8, d_model=WIDTH, lr=0.1, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=11)
    config.update(overrides)
    return LocalAssociativeMemory(LocalMemoryConfig(**config))


def share_right_after_the_ambiguous_token(context: bool) -> float:
    """Train on the pattern, then score ONLY the steps a bigram cannot resolve.

    Scoring every position would hide the finding: four of the six steps are
    determined by one token, so a bigram model reaches 2/3 and looks fine.
    """
    tokens = sequence()
    targets = np.concatenate([tokens[1:], tokens[-1:]])
    scored = np.ones(len(tokens), dtype=bool)
    scored[-1] = False
    model = build(context_keys=context)
    model.wo[:] = model.wv
    for _ in range(40):
        model.run(tokens, targets, scored, learn=True)
    predicted = model.run(tokens)
    # Score the second half only. The store is emptied between sequences, so
    # the opening steps are asking it to resolve a context it has not yet been
    # given -- a fact about working memory, not about what the key binds.
    at = [t for t in range(len(tokens) // 2, len(tokens) - 1)
          if tokens[t] == AMBIGUOUS]
    return sum(int(predicted[t] == targets[t]) for t in at) / len(at)


class ThePairKeyLiftsTheBigramCeiling(unittest.TestCase):
    """The whole point, measured through `model.run` rather than derived."""

    def test_a_bigram_cannot_resolve_the_ambiguous_step(self):
        """**The ceiling, stated as a failure.** With one-token keys the two Bs
        write to the same key, so the store holds C and E superposed and the
        readout has nothing to separate them. Half is what picking one gets."""
        share = share_right_after_the_ambiguous_token(context=False)
        self.assertLessEqual(
            share, 0.6,
            f"single-token binding resolved {share:.0%} of a step that requires "
            f"two tokens of context; note 033's ceiling derivation is wrong")

    def test_the_pair_key_resolves_it(self):
        """The same model, the same training, one flag."""
        share = share_right_after_the_ambiguous_token(context=True)
        self.assertGreater(
            share, 0.9,
            f"pair binding resolved only {share:.0%}; the ceiling did not move")

    def test_the_pair_key_is_BETTER_not_merely_different(self):
        """The guard. Two numbers from separate runs could both be flattering
        for unrelated reasons; this states the comparison itself."""
        self.assertGreater(share_right_after_the_ambiguous_token(context=True),
                           share_right_after_the_ambiguous_token(context=False))


class TheKeyIsDerivedRatherThanStored(unittest.TestCase):
    """The property that makes this affordable: `vocab^2` rows are never held.

    If pair keys had to be tabulated they would cost 16 million rows at vocab
    4096, and the argument for deriving keys at all would collapse.
    """

    def test_the_same_pair_gives_the_same_vector_in_a_FRESH_model(self):
        """What a second node would have to reproduce from the token ids."""
        first, second = build(context_keys=True), build(context_keys=True)
        np.testing.assert_allclose(first.context_key(3, 5),
                                   second.context_key(3, 5))

    def test_a_different_pair_gives_a_different_vector(self):
        """Including pairs that share a token, which is the case that matters:
        `(3, 5)` and `(4, 5)` are what the ambiguous step above turns on."""
        model = build(context_keys=True)
        a, b = model.context_key(3, 5), model.context_key(4, 5)
        overlap = float(a @ b) / float(np.linalg.norm(a) * np.linalg.norm(b))
        self.assertLess(abs(overlap), 0.4,
                        f"pairs sharing their second token overlap at {overlap:.2f}")

    def test_ORDER_matters(self):
        """`(3, 5)` and `(5, 3)` are different contexts and must not collide."""
        model = build(context_keys=True)
        self.assertFalse(np.allclose(model.context_key(3, 5),
                                     model.context_key(5, 3)))

    def test_the_cache_does_not_change_the_answer(self):
        """The second call is served from the cache; it must be the same vector
        the first call derived, not merely the same shape."""
        model = build(context_keys=True)
        first = np.array(model.context_key(2, 6))
        np.testing.assert_allclose(model.context_key(2, 6), first)


class TheFlagIsOffByDefaultAndGuarded(unittest.TestCase):

    def test_default_off_changes_nothing(self):
        tokens = sequence(count=8)
        np.testing.assert_array_equal(build().run(tokens),
                                      build(context_keys=False).run(tokens))

    def test_it_requires_derived_keys(self):
        with self.assertRaises(ValueError):
            build(context_keys=True, derived_keys=False)

    def test_asking_for_a_pair_key_without_the_flag_RAISES(self):
        """Silence would be worse: the vector is in no key space the store uses,
        so a caller would get plausible numbers from an unrelated projection."""
        with self.assertRaises(ValueError):
            build().context_key(1, 2)


if __name__ == "__main__":
    unittest.main()
