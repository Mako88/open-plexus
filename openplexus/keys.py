"""Where a key comes from, as a component you can replace.

**This exists because of a worry John raised**: that pinning down "a model needs
component X at performance Y" would rule out a component Z nobody has thought of
yet. The defence against that is not to avoid measuring components — it is to
make replacing one cheap, so a new idea costs a file rather than a refactor.

Keys were the obvious place to start, because they are the part of this model
that has already been varied twice. Each time it cost a config flag, a branch
inside a six-hundred-line `run`, and a threading of that flag through every
experiment script. A third variation would have cost the same again, and the
fourth is the one that never gets tried.

## The seam

    key(tokens, t) -> vector

That is the whole interface. It takes the token sequence and a position, and it
returns the vector the store binds against. Everything else in the model — the
write rule, the retrieval, the readout, the delta rule, the gates, the
partitions — is untouched by a change here.

**A new key scheme is a class in this file and one assignment.** No config flag,
no branch in `run`, no edit to any experiment. `tests/test_keys.py` proves that
by defining a key source that exists nowhere in the model and running it through
`model.run` end to end.

## What is deliberately NOT in the interface

There is no `fit`, no `update`, and no access to the store. A key source sees
token ids and a position, and nothing else. That is not tidiness — it is the C1
locality constraint written into a type: a node that could only be handed token
ids can compute any key defined here, and one that needed the store's contents
or a global statistic could not.

A scheme that genuinely needs to learn its keys will have to widen this
interface, and widening it should be a deliberate act with a note attached,
because it is where the broadcast-a-token argument (note 012) would be spent.
"""

from __future__ import annotations

from typing import Protocol, runtime_checkable

import numpy as np


@runtime_checkable
class KeySource(Protocol):
    """Produces the vector the store binds against at one position."""

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        """The key at position `t` of `tokens`.

        Must be pure: the same `(tokens, t)` gives the same vector, on any node,
        in any order. The store is written and read at different times and on
        different machines, and a key that drifted between the two would break
        the retrieval without breaking any test that only wrote.
        """
        ...

    def key_as(self, tokens: np.ndarray, t: int, token: int) -> np.ndarray:
        """The key at position `t` AS IF the token there were `token`.

        **What a candidate read needs.** The content index proposes concepts,
        and asking the store about one means building the key it would have
        had -- which only the key source knows how to do, since a pair key is
        `(t-1, t)` and a table key is a row.

        `search.walk_from` already does exactly this by calling
        `PairKeys.pair(fact_token, current)` directly, which works and reaches
        past the seam. This is that operation, named, so a candidate read
        composes with ANY key source instead of only with pair keys.

        `key_as(tokens, t, tokens[t])` must equal `key(tokens, t)`, and the
        conformance suite checks it -- otherwise a candidate read and an
        ordinary read would disagree about the same token.
        """
        ...

    def concept(self, tokens: np.ndarray, t: int) -> int:
        """WHICH concept this position's key belongs to, for routing.

        **`key` says what to retrieve; this says whom to ask.** Under concept
        partitioning a node holds some concepts and not others, so a read has to
        name one -- and **a key vector cannot be inverted to a concept**. With
        random keys there is no inversion at all, and with content-derived keys
        it would be approximate. The identity has to travel alongside the
        vector.

        Note 044 walks every read site in the model and finds all of them can
        supply this except the soft hop, whose key is a softmax mixture of every
        token's row and therefore names no concept at all.

        Pure, for the same reason `key` is: two nodes disagreeing about who owns
        a concept is a write and a read going to different machines.
        """
        ...


class TableKeys:
    """One key per token, read from a table. The original scheme.

    The table is `Wk`, built by the model — stored when `derived_keys` is off
    and regenerable from `(seed, token)` when it is on. Either way this class
    reads a row, so churn, partitions and ablation keep working through `Wk`
    exactly as they did.

    **Its limit is the ceiling of note 033**: a key that depends only on the
    current token makes the store a bigram count table, which cannot represent a
    trigram.
    """

    def __init__(self, table: np.ndarray) -> None:
        self.table = table

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        return self.table[tokens[t]]

    def key_as(self, tokens: np.ndarray, t: int, token: int) -> np.ndarray:
        return self.table[token]

    def concept(self, tokens: np.ndarray, t: int) -> int:
        """The token itself. One key per token means one concept per token."""
        return int(tokens[t])


class PairKeys:
    """A key derived from the token PAIR `(t-1, t)` by a fixed hash.

    Note 034: this makes `previous_key` the key of `(t-2, t-1)`, so the same
    write rule builds a trigram table. It lifts the bigram ceiling — 0.533 to
    1.000 on a step a bigram cannot resolve — and it is not free: the number of
    distinct keys goes from `vocab` to the number of pairs that actually occur,
    469 against 66 in 4000 characters of Shakespeare.

    Derived and cached, never tabulated. A `vocab^2` table is 16 million rows at
    `vocab 4096`, and not holding it is the same argument `derived_keys` rests
    on: a node is handed two token ids and rebuilds the vector itself.
    """

    def __init__(self, seed: int, spread: float, width: int,
                 start: int) -> None:
        self.seed = seed
        self.spread = spread
        self.width = width
        #: Stands in for "no previous token" at position 0, so the first step
        #: has a key in the same space as every other step rather than a special
        #: case the store would have to exclude.
        self.start = start
        self.cache: dict[tuple[int, int], np.ndarray] = {}

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        # Routed THROUGH `key_as` rather than repeating its body. The two must
        # agree -- the conformance suite asserts `key_as(t, tokens[t]) == key(t)`
        # -- and two copies of one rule is how they stop agreeing.
        return self.key_as(tokens, t, int(tokens[t]))

    def key_as(self, tokens: np.ndarray, t: int, token: int) -> np.ndarray:
        """The PREVIOUS token is kept and only the current one substituted.

        A pair key is `(t-1, t)` and a candidate stands in for the current
        token, so the context it is being asked about must not change with it --
        the question is "what if THIS position held that concept instead", not
        "what if the whole pair were different".
        """
        return self.pair(int(tokens[t - 1]) if t else self.start, int(token))

    def concept(self, tokens: np.ndarray, t: int) -> int:
        """**The CURRENT token, not the pair**, and the choice is load-bearing.

        A pair key is `(t-1, t)`, so there are two defensible answers and they
        give different systems:

        - **By the pair.** Perfectly balanced, since pairs are numerous and the
          hash spreads them. But the facts about one entity scatter across every
          node, so no node owns an entity.
        - **By the current token.** Every `key(FACT, X)` lands on X's node, so
          **one node holds an entity and everything said about it.**

        The second is what decision 134's case is actually about. A node's lone
        capability grows with the network only if what it holds is *coherent* --
        holding a random sixteenth of every entity's facts is dimension
        splitting again, in a different coordinate.

        The cost is that balance now depends on how evenly tokens occur, where
        the pair hash would have spread them regardless. `Ring.balance` measures
        that and it is the number to check before trusting this.

        Routing by pair is not built. It is one line here, and it should be
        built the moment there is a measurement that wants it.
        """
        return int(tokens[t])

    def pair(self, previous: int, token: int) -> np.ndarray:
        """The vector for one pair, exposed so a test can check two nodes agree
        without having to build a sequence that reaches the pair."""
        cached = self.cache.get((previous, token))
        if cached is None:
            cached = np.random.default_rng(
                (self.seed, previous, token)).normal(
                    0.0, self.spread, self.width)
            self.cache[(previous, token)] = cached
        return cached


class ByConcept:
    """Any key source, addressed by CONCEPT instead of by surface.

    ## What it is for

    g17-01 found word-level text unlearnable and found the reason: with pair
    keys, 1,733 words make tens of thousands of distinct addresses over 90,000
    training words, so nearly every address the store is asked about at test time
    was written at most once. The store memorises hapaxes rather than superposing
    anything reusable.

    **Grouping surfaces into concepts collapses that address space.** Three
    hundred concepts make far fewer pairs than 1,733 words do, and each recurs
    many times. `concepts.Shared` already expresses the grouping and
    `grouping.cluster` builds one; this is the piece that makes the store
    actually *use* it, and it is deliberately a wrapper rather than a fourth key
    scheme — concept addressing composes with table keys, pair keys, and whatever
    replaces them.

    **The readout is untouched**, so the model still predicts a surface word from
    a concept-addressed retrieval: store by concept, emit by word. That asymmetry
    is the whole proposal, and it is also its risk — two words in one group
    become indistinguishable *as context*, which is a real loss of resolution
    traded for recurrence. Which way the trade goes is a measurement.

    ## `concept` returns the SURFACE, on purpose

    Every stock key source answers `concept` with a token id, and the model
    composes it with `model.surfaces` to get the routing address. Returning a
    concept id here would put that mapping in twice — the model would map an
    already-mapped id through the surface table again — so this stays a surface
    and the composition stays exactly where it was.
    """

    def __init__(self, inner: KeySource, surfaces, vocab: int) -> None:
        """
        Args:
            inner: The key source to wrap. It sees concept ids where it would
                have seen token ids, and needs no knowledge that it has been
                wrapped.
            surfaces: A `concepts.Surfaces`, consulted once per token here.
            vocab: How many surface tokens there are. Taken explicitly rather
                than read off `surfaces`, because `Surfaces` promises `of` and
                `concepts` and nothing about a vocabulary attribute — widening
                the protocol to save an argument would be paying for this in the
                wrong place.
        """
        if vocab < 1:
            raise ValueError("a vocabulary of nothing has nothing to address")
        self.inner = inner
        self.surfaces = surfaces
        #: The mapping, materialised once. `of` is pure by contract, so this is
        #: a cache and not a snapshot of something that could move.
        self._of = np.asarray([surfaces.of(t) for t in range(vocab)],
                              dtype=np.int64)

    def _as_concepts(self, tokens: np.ndarray) -> np.ndarray:
        """The sequence with every surface replaced by its concept.

        Rebuilt per call rather than cached against the array it came from.
        Caching on object identity would be correct only while nobody writes
        through a sequence in place, which is not a promise anything in this
        project makes, and the gather is a few microseconds against a `d x d`
        matrix product per step.
        """
        return self._of[np.asarray(tokens, dtype=np.int64)]

    def key(self, tokens: np.ndarray, t: int) -> np.ndarray:
        return self.inner.key(self._as_concepts(tokens), t)

    def key_as(self, tokens: np.ndarray, t: int, token: int) -> np.ndarray:
        """A candidate is a SURFACE and is mapped like any other.

        The content index proposes words, so what arrives here is a word. Asking
        the store about it means asking about the concept it belongs to — which
        also means two candidate surfaces from one concept produce the same read,
        and a caller that wanted them distinguished is asking the wrong layer.
        """
        return self.inner.key_as(self._as_concepts(tokens), t,
                                 int(self._of[int(token)]))

    def concept(self, tokens: np.ndarray, t: int) -> int:
        """The surface `inner` names, unmapped. See the class docstring."""
        return self.inner.concept(tokens, t)
