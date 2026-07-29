"""What a token MEANS, as a vector, built by one accumulation per observed pair.

## Why this exists

Every address in this model is a random draw. `dog` and `wolf` are as unrelated
as `dog` and `7`, so the store has **no notion of similarity at all** — it is a
lookup table with a superposition trick, not a map. GOALS §1.2 asks for *"a good
map of most all concepts, and how a given concept relates to some other
concept"*, and note 042 found there is no such map anywhere in the architecture.

John put the same problem the other way round, 2026-07-29: *"there is a picture
of a dog, and there is a drawing of a dog, and there is the sound of a dog"* —
one concept, many surfaces. **A concept can only have one address today, because
the address IS the token id.** Both wants need the same thing: something that
says what a token means, separately from what it is called.

## What this is NOT, and the refusal is the design

**This does not go into the key vector**, and note 045 rejects that route on
evidence this project already recorded. Retrieval is `M @ key`, a sum weighted by
key overlap, so **deliberate overlap is deliberate interference** — note 035
gives it as `O(N·ρ)` in mean key cosine. Similarity in the key buys meaning by
spending the capacity that is already the measured wall.

So the two jobs are split:

    WHAT IT IS      concept id       hash-derived, near-orthogonal, UNCHANGED
    WHAT IT MEANS   content vector   this module, a separate space

A read stays exact. What this adds is a way to decide **which** exact reads to
make — candidates in meaning-space, then ordinary keyed retrieval on each, each
committing to a hard token id.

That commitment is the third independent argument for the same design. Decision
123 chose it for accuracy (a branch that commits can be scored, a blur cannot);
note 044 found it is the only form the model has that can be **routed** across
nodes at all; and here it is what lets similarity exist without costing capacity.
None of the three was chosen with the others in mind.

## Why co-occurrence, and why it respects C1

A token's content vector is the running sum of the random vectors of the tokens
near it. Two tokens appearing in similar company end up pointing the same way.
This is distributional semantics — the idea behind word2vec and GloVe — **without
their machinery**. There is no objective, no negative sampling and no gradient:

    for each observed pair (token, neighbour):
        content[token] += context[neighbour]

**Everything a node needs, it already has.** The tokens in a window arrive by the
same 5-byte broadcast the model has always used; `context` is regenerable from
`(seed, token)` exactly as `derived_keys` regenerates a key row; and the
frequency weighting below uses a count the node keeps itself. No barrier, no
global statistic, no second round trip. C1 holds without an argument.

**And nothing here is textual.** A second modality's content vector would be
built the same way, from what it co-occurs with — which is why note 045 records
multimodality as downstream of this rather than as a separate project.

## The measured caveat, which is why `power` is a parameter and not a default

P4 (note 045) built these vectors on 180,430 words of Shakespeare and found real
structure — `king → prince, crown`; `father → son, brother` — where the same
construction on a **shuffled** corpus returned only high-frequency words for
every query.

**But mean off-diagonal cosine came out at 0.50**, against 0.0005 for hash keys.
Everything resembles everything, because everything co-occurs with `the`.
Down-weighting frequent context by `1/frequency**power` drops it to 0.22 and
sharpens `king` to `richard, edward, henry` — the actual kings in the plays —
while collapsing `father` and `love` into function words. **It over-corrects.**

So `power` has a large effect in both directions and no defensible default. It is
a swept axis, and a result quoted at one value of it is a result about that value.
"""

from __future__ import annotations

import numpy as np


class ContentIndex:
    """Content vectors per token, and nearest-neighbour lookup over them.

    Attributes:
        vocab: How many tokens there are.
        width: Content-space width. **Unrelated to the store's `d_model`** —
            this space is never bound against, so nothing forces them equal and
            conflating them would tie two independent costs together.
        power: How hard to down-weight frequent context tokens. 0.0 is
            unweighted. See the module docstring: this is a swept axis.
    """

    def __init__(self, vocab: int, width: int = 128, seed: int = 0,
                 power: float = 0.0, window: int = 4,
                 frequency: np.ndarray | None = None) -> None:
        if vocab < 2:
            raise ValueError("a content index needs at least two tokens")
        if width < 1:
            raise ValueError("width must be at least 1")
        if window < 1:
            raise ValueError(
                "a window of zero sees no neighbours, so every content vector "
                "would stay at the origin and every similarity would be 0/0")
        if power < 0.0:
            raise ValueError(
                "a negative power would UP-weight the commonest tokens, which "
                "is the failure this parameter exists to correct")
        if power and frequency is None:
            # ORDER-INDEPENDENCE, ENFORCED RATHER THAN HOPED FOR.
            #
            # The first version of this weighted context by a RUNNING count, so
            # a token seen early was weighted against a smaller denominator than
            # the same token seen later. That makes the result depend on the
            # order sequences arrive in -- which silently breaks the property
            # this module's own docstring claims: that accumulation is a sum, so
            # nodes may observe in any order and merge by addition.
            #
            # The suite did not catch it, because the order test ran at the
            # default `power=0.0` where weights are all 1.
            #
            # Counting is itself order-independent, so the fix is to take counts
            # as an argument: one counting pass, then accumulation against FIXED
            # weights. Counts merge across nodes by addition, which is the same
            # property one level down.
            raise ValueError(
                "power > 0 needs a `frequency` array: weighting by a running "
                "count makes accumulation depend on the order sequences "
                "arrive, so two nodes seeing the same data in different orders "
                "would disagree. Count first, then build")
        if frequency is not None and np.shape(frequency) != (vocab,):
            raise ValueError(
                f"frequency must have one entry per token, got "
                f"{np.shape(frequency)} for a vocab of {vocab}")
        self.vocab = vocab
        self.width = width
        self.window = window
        self.power = power
        #: Regenerable from `(seed, token)`, exactly as `derived_keys` rebuilds a
        #: key row. A node holds this or recomputes it; either way it needs no
        #: one else's copy.
        self._context = np.random.default_rng(seed).normal(
            0.0, 1.0 / np.sqrt(width), (vocab, width))
        self._sums = np.zeros((vocab, width))
        #: A running count the node keeps for itself, used ONLY to know which
        #: tokens have been seen at all. **Not used for weighting** -- see the
        #: `frequency` refusal above.
        self._seen = np.zeros(vocab)
        #: Fixed weights, from counts supplied up front. `None` when `power` is
        #: 0, where every weight is 1 and there is nothing to fix.
        self._weight = (None if frequency is None else
                        1.0 / np.maximum(np.asarray(frequency, dtype=float),
                                         1.0) ** power)
        self._cache: np.ndarray | None = None

    @staticmethod
    def count(vocab: int, *streams: np.ndarray) -> np.ndarray:
        """Token counts, for the `frequency` argument.

        A separate pass, and a cheap one. **Counts are a sum**, so a node counts
        what it sees and counts merge across nodes by addition -- the same
        property the content sums have, one level down, and the reason this can
        be done without a coordinator.
        """
        counts = np.zeros(vocab)
        for stream in streams:
            np.add.at(counts, np.asarray(stream, dtype=np.int64), 1.0)
        return counts

    def observe(self, tokens: np.ndarray) -> None:
        """Accumulate one sequence. **The whole learning rule.**

        Symmetric: each token is credited with its neighbours on both sides, so
        `content[a]` gains `context[b]` and `content[b]` gains `context[a]`.
        Asymmetric accumulation would make similarity depend on which of two
        words came first, which is a property of English word order rather than
        of meaning.
        """
        tokens = np.asarray(tokens, dtype=np.int64)
        if tokens.size and (tokens.min() < 0 or tokens.max() >= self.vocab):
            raise ValueError(
                f"token outside vocab of {self.vocab}: "
                f"[{tokens.min()}, {tokens.max()}]")
        np.add.at(self._seen, tokens, 1.0)
        weight = self._weights()
        for offset in range(1, self.window + 1):
            left, right = tokens[:-offset], tokens[offset:]
            if left.size == 0:
                break
            np.add.at(self._sums, left, self._context[right] * weight[right, None])
            np.add.at(self._sums, right, self._context[left] * weight[left, None])
        self._cache = None

    def _weights(self) -> np.ndarray:
        """How much each token counts AS CONTEXT.

        Applied at accumulation time rather than afterwards, because the effect
        is on WHAT GETS SUMMED. Applying it to the finished sums would rescale a
        vector rather than change which directions dominate it -- a different
        operation entirely, and not the one that moved `king` to `richard`.

        Fixed, from counts supplied up front, so this is a constant across the
        whole run and accumulation stays order-independent.
        """
        return (np.ones(self.vocab) if self._weight is None else self._weight)

    @property
    def vectors(self) -> np.ndarray:
        """Unit-length content vectors, one row per token.

        Centred before normalising. The common mode -- every token's overlap
        with `the` and `and` -- is a direction shared by everything, so it
        carries no information about any particular token and consumes most of
        the dynamic range. Tokens never observed stay at the origin and are
        returned as zeros rather than as an arbitrary unit vector, so they are
        never anyone's nearest neighbour by accident.
        """
        if self._cache is None:
            seen = self._seen > 0
            centred = self._sums.copy()
            if seen.any():
                centred[seen] -= centred[seen].mean(axis=0)
            centred[~seen] = 0.0
            norms = np.linalg.norm(centred, axis=1, keepdims=True)
            self._cache = centred / np.where(norms > 0.0, norms, 1.0)
        return self._cache

    def nearest(self, token: int, count: int) -> list[tuple[int, float]]:
        """The `count` most similar OTHER tokens, best first, with scores.

        **Scores are returned rather than bare ids**, for the same reason
        `search.candidates` returns them: the margin between the first two is
        what a gate would read, and a caller given only ids cannot recover it.

        Excludes `token` itself. A token that is its own nearest neighbour is
        true and useless -- the exact read at its own id is what the index is
        choosing *between*, not something it needs to be told about.
        """
        if count < 1:
            raise ValueError("asking for no candidates retrieves nothing")
        scores = self.vectors @ self.vectors[token]
        scores[token] = -np.inf
        order = np.argsort(-scores)[:min(count, self.vocab - 1)]
        return [(int(i), float(scores[i])) for i in order
                if np.isfinite(scores[i])]

    def spread(self) -> float:
        """Mean off-diagonal cosine. **The number that made `power` an axis.**

        Hash keys measure 0.0005 here (g10-09). These measure 0.50 unweighted
        and 0.22 at `power=0.5`. Reported rather than asserted because the right
        value is what the sweep is for.
        """
        gram = self.vectors @ self.vectors.T
        off = gram[~np.eye(self.vocab, dtype=bool)]
        return float(off.mean())

    @property
    def numbers_held(self) -> int:
        """What the index COSTS, in the unit a comparison must quote.

        `vocab x width` for the sums plus the same for the context table. g10-09
        was retracted for comparing a model with extra state against one without
        at equal width rather than equal state; anything citing an accuracy from
        this must cite this beside it.
        """
        return 2 * self.vocab * self.width
