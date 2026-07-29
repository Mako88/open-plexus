"""Can a key scheme that the model has never heard of be dropped in?

**This is the anti-ossification test.** John's worry was that measuring
components would let the project decide "a model needs component X at
performance Y" and thereby rule out some component Z nobody has thought of yet.
The defence is not to stop measuring — it is to make replacing a component cheap
enough that a new idea gets tried instead of argued about.

So the test is not "does `PairKeys` work". It is: **can a key source defined
entirely outside the model, with no config flag, no branch in `run` and no edit
to any experiment script, be run end to end and change what the model does?**

The one used below is a SIMILARITY key — John's own idea, that keys for related
tokens should overlap so the model can generalise between them. It lives in this
test file and nowhere else, which is exactly the point.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.keys import KeySource, PairKeys, TableKeys
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 16, 64


def build(**overrides):
    config = dict(vocab_size=VOCAB, d_model=WIDTH, lr=0.05, key_scale=0.5,
                  decay=1.0, derived_keys=True, seed=11)
    config.update(overrides)
    model = LocalAssociativeMemory(LocalMemoryConfig(**config))
    model.wo[:] = model.wv
    return model


class FamilyKeys:
    """A key source the model has never heard of: tokens in the same family
    share part of their key.

    Each key is a family vector plus a token-specific one, so two tokens of the
    same family overlap by `share` and two of different families do not. This is
    the shape of the thing John asked for — derivable from a token id, and yet
    carrying similarity — and the reason it is HERE rather than in the model is
    that it has not been measured yet. The capacity cost of that overlap is the
    open question (note 032 measured key separation at 0.56 already), and a
    scheme with an unmeasured cost does not belong in the model.
    """

    def __init__(self, seed: int, width: int, families: int, share: float):
        self.width, self.families, self.share = width, families, share
        self.seed = seed

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        token = int(tokens[t])
        # Two disjoint seed spaces, tagged 0 and 1, so a family draw and a token
        # draw can never collide -- family 2 and token 2 would otherwise share a
        # vector and the "similarity" would be an artefact of the seeding.
        family = np.random.default_rng(
            (self.seed, 0, token % self.families)).normal(0.0, 1.0, self.width)
        own = np.random.default_rng((self.seed, 1, token)).normal(
            0.0, 1.0, self.width)
        return self.share * family + (1.0 - self.share) * own

    def concept(self, tokens: np.ndarray, t: int) -> int:
        """The token, not the family -- and the choice is the interesting one.

        Routing by FAMILY would put similar concepts on the same node, which is
        exactly what note 044 says content-derived keys will want: a query that
        should reach a related binding has to reach the node holding it. Routing
        by token spreads them, which is what the hash ring is for.

        **These two pull against each other**, and this outside-the-model source
        is where that becomes visible. Left as the token so this stays a test of
        the seam rather than an unmeasured design choice smuggled into a test.
        """
        return int(tokens[t])


class AKeySourceFromOutsideTheModelJustWorks(unittest.TestCase):

    def setUp(self):
        self.tokens = np.tile(np.arange(6), 8).astype(np.int64)

    def test_the_model_RUNS_with_a_key_source_it_has_never_seen(self):
        model = build()
        model.key_source = FamilyKeys(3, WIDTH, families=3, share=0.5)
        predicted = model.run(self.tokens)
        self.assertEqual(len(predicted), len(self.tokens))

    def test_it_actually_CHANGES_what_the_model_does(self):
        """The guard that matters. A seam that were silently ignored would pass
        the test above and teach us nothing."""
        stock = build().run(self.tokens)
        swapped = build()
        swapped.key_source = FamilyKeys(3, WIDTH, families=3, share=0.5)
        self.assertFalse(np.array_equal(stock, swapped.run(self.tokens)),
                         "swapping the key source changed nothing, so the model "
                         "is not really reading it")

    def test_it_needs_NO_config_flag(self):
        """The cost of a new idea is the thing being tested. `FamilyKeys`
        appears nowhere in `LocalMemoryConfig`, and could not — the config was
        written before it existed."""
        self.assertNotIn("family", {f for f in vars(LocalMemoryConfig(
            vocab_size=VOCAB, d_model=WIDTH))})

    def test_the_similarity_it_promises_is_REAL(self):
        """The vacuity guard on `FamilyKeys` itself: if the family term did
        nothing, the test above would still pass and would be measuring noise."""
        source = FamilyKeys(3, WIDTH, families=3, share=0.5)
        tokens = np.arange(VOCAB)

        def cosine(a, b):
            x, y = source.key(tokens, a), source.key(tokens, b)
            return float(x @ y / (np.linalg.norm(x) * np.linalg.norm(y)))

        # 0 and 3 share a family (3 families); 0 and 1 do not.
        self.assertGreater(cosine(0, 3), cosine(0, 1) + 0.2)


class TheStockSourcesHonourTheSeam(unittest.TestCase):

    def test_both_satisfy_the_protocol(self):
        self.assertIsInstance(build().key_source, KeySource)
        self.assertIsInstance(build(context_keys=True).key_source, KeySource)

    def test_the_default_is_the_table_and_the_flag_picks_the_pair(self):
        self.assertIsInstance(build().key_source, TableKeys)
        self.assertIsInstance(build(context_keys=True).key_source, PairKeys)

    def test_a_key_source_is_PURE(self):
        """The property the store depends on and no other test states: the same
        position must give the same vector every time. A key that drifted
        between the write and the read would break retrieval silently, and
        writes alone would still look correct."""
        tokens = np.tile(np.arange(6), 4).astype(np.int64)
        for model in (build(), build(context_keys=True)):
            source = model.key_source
            np.testing.assert_allclose(source.key(tokens, 5),
                                       source.key(tokens, 5))

    def test_the_table_source_reads_the_MODEL_s_table(self):
        """Churn, partitions and ablation all still work through `Wk`, so the
        table source must not have taken a copy."""
        model = build()
        model.wk[3] = 0.0
        np.testing.assert_allclose(model.key_source.key(np.array([3]), 0), 0.0)


if __name__ == "__main__":
    unittest.main()
