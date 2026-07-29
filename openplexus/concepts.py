"""A concept is not a word. This is the indirection that lets it not be.

## The defect this fixes, which John found by asking for something else

Everywhere in this project a concept id and a token id are **the same integer**.
The store is addressed by token. So the architecture cannot represent one concept
with several surfaces, however good the content vectors get: John's picture of a
dog and the word `dog` would be two different concepts that happen to land near
each other, not one concept seen two ways.

    surface            content vector       concept id        store
    word / image /  ->  what it means  ->   what it IS   ->   facts about it
    sound

**Today the middle arrows do not exist — the surface IS the id.** No amount of
work on similarity reaches that, because similarity relates *different* concepts
and this is about one concept having *different appearances*.

## Why it is built now rather than with the second modality

John's standing rule: get the core pieces right before refining on them, because
a change to what the store is keyed by invalidates everything measured through
it. Adding this after a sweep would throw the sweep away.

**So the seam lands now and the second modality does not.** `OneConceptPerToken`
reproduces today's behaviour exactly — one surface, one concept, the identity
mapping — so nothing measured changes and every existing number stays valid. A
second modality becomes a second mapping into the same concept space rather than
a redesign.

That deliberately leaves the hard part unbuilt, and it should be named rather
than glossed: **deciding that two surfaces are the same concept is the whole
problem**, and doing it across nodes without a coordinator is harder still, since
two nodes disagreeing about an assignment send a write and a read to different
machines. `Shared` below expresses the *result* of such a decision; nothing here
makes one.

## Locality

`of` must be pure and agreed. It is consulted at every write and every read, and
the two happen at different times on different machines — the same contract
`KeySource` carries, for the same reason. A mapping that drifted between a write
and a read would lose the binding with no error anywhere.
"""

from __future__ import annotations

from typing import Protocol, runtime_checkable


@runtime_checkable
class Surfaces(Protocol):
    """Which concept a surface token belongs to."""

    def of(self, token: int) -> int:
        """The concept `token` is a surface of.

        Pure and total: every token in the vocabulary maps somewhere, and the
        same token maps to the same concept on every node forever.
        """
        ...

    @property
    def concepts(self) -> int:
        """How many distinct concepts exist. **Not the vocabulary size** once
        surfaces are shared, and the store is sized by this rather than by the
        vocabulary -- which is the point of the whole indirection."""
        ...


class OneConceptPerToken:
    """The identity mapping. **Exactly today's behaviour, on purpose.**

    Every number in this project was measured with concept id equal to token id.
    This is that, stated rather than assumed, so the assumption becomes something
    a different implementation can replace instead of a fact welded into every
    call site.
    """

    def __init__(self, vocab: int) -> None:
        if vocab < 1:
            raise ValueError("a vocabulary of nothing has nothing to map")
        self.vocab = vocab

    def of(self, token: int) -> int:
        return int(token)

    @property
    def concepts(self) -> int:
        return self.vocab


class Shared:
    """Several surfaces of one concept, given as groups.

    **This expresses a decision; it does not make one.** Working out that a
    picture and a word denote the same thing is the open problem (note 045: the
    method is post-hoc alignment of separately-learned spaces, and its failure
    mode is a confident mapping of nothing). This is the shape the answer would
    take, so the rest of the model can be built and tested against it now.

    Concept ids are assigned by **lowest member token**, not by group order, so
    the mapping depends only on the grouping itself. Two nodes handed the same
    groups in different orders produce the same ids -- which is the agreement
    property the module docstring says everything rests on, and it would not hold
    if ids were handed out as groups arrived.
    """

    def __init__(self, vocab: int, groups: list[list[int]] | None = None) -> None:
        if vocab < 1:
            raise ValueError("a vocabulary of nothing has nothing to map")
        self.vocab = vocab
        merged: dict[int, int] = {}
        for group in groups or []:
            if not group:
                continue
            for token in group:
                if not 0 <= token < vocab:
                    raise ValueError(
                        f"token {token} outside a vocabulary of {vocab}")
                if token in merged and merged[token] != min(group):
                    raise ValueError(
                        f"token {token} is claimed by two different concepts; "
                        f"a surface belongs to one concept or the mapping is "
                        f"not a function and a read has no single destination")
                merged[token] = min(group)
        # Compacted so ids are contiguous. Without this the store would be
        # sized by the largest surviving token id rather than by the number of
        # concepts, which is the saving the indirection is supposed to produce.
        representatives = sorted({merged.get(t, t) for t in range(vocab)})
        order = {r: i for i, r in enumerate(representatives)}
        self._of = [order[merged.get(t, t)] for t in range(vocab)]
        self._concepts = len(representatives)

    def of(self, token: int) -> int:
        return self._of[int(token)]

    @property
    def concepts(self) -> int:
        return self._concepts

    def surfaces(self, concept: int) -> list[int]:
        """Every token that is a surface of `concept`.

        The direction a generative step needs: a concept has to be spoken, drawn
        or otherwise emitted, and which surface to use is a choice the concept
        itself does not contain.
        """
        return [t for t in range(self.vocab) if self._of[t] == concept]
