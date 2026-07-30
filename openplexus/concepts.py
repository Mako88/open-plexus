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


class Merged:
    """Concepts discovered to be one thing, expressed WITHOUT moving any address.

    ## The problem this is shaped by

    Learning that two concepts are the same is the acquisition step nothing in this
    module performs -- the docstring above says so, and calls it the whole problem.
    The obvious implementation is a forwarding pointer: remap the loser's surfaces
    onto the winner and follow the pointer on read.

    **That strands every binding it was meant to preserve.** `keys.ByConcept` hands
    its inner source CONCEPT ids where token ids would go, so the key is built from
    the concept id. Change which concept a surface maps to and the key changes with
    it: the facts stored under the old id are not corrupted, they are unreachable,
    and nothing anywhere raises.

    ## So `of` does not move, and the merge lives on the READ side

        of(token)          unchanged, forever. A write always lands on the
                           surface's OWN concept, so no address ever moves and
                           nothing is ever stranded
        aliases(concept)   the equivalence class. A reader gathers across it,
                           rebuilding the key per member because each member's
                           key is its own

    The cost is honest and it is on the read path: a class of `k` members costs `k`
    reads at `k` addresses, and no amount of pointer-chasing avoids it, because the
    bindings genuinely live at `k` different keys on `k` different nodes. Copying a
    class together is a later, lazy consolidation that shrinks the fan-out without
    ever breaking a read -- which is precisely what re-keying cannot promise.

    ## Union by MINIMUM ID, not by rank, and this is the distributed requirement

    Union-by-rank picks a representative that depends on the order merges arrived
    in. Two nodes learning the same merges in different orders would then disagree
    about the class representative, and `Surfaces.of` promises *"the same token maps
    to the same concept on every node forever"*.

    Taking the smallest id makes the representative a property of the SET of merges
    and not of their sequence, so nodes converge without agreeing on an order --
    no coordinator, which amended C1 requires. The price is deeper trees, and
    `aliases` returns the whole class anyway so depth costs nothing here.

    ## Why a late merge is a miss and never a corruption

    A node that has not yet learned `merge(a, b)` reads a smaller class, so it
    misses facts stored under the other member. It does not read the WRONG fact,
    and once the merge arrives the older bindings are reachable with no migration.
    **Merges are append-only, so propagation can be lazy** -- the property that made
    this approach worth choosing over re-keying, which would need a barrier.
    """

    def __init__(self, inner: Surfaces) -> None:
        self.inner = inner
        #: concept -> its class representative. Absent means "its own".
        self._parent: dict[int, int] = {}
        #: The merge SET, kept so two nodes can compare what they know rather
        #: than compare derived state that a different arrival order would shape
        #: differently.
        self._merges: set[tuple[int, int]] = set()

    def of(self, token: int) -> int:
        """**Unchanged, and that is the whole design.** Writes never move."""
        return self.inner.of(token)

    @property
    def concepts(self) -> int:
        """Still the inner count: every concept id remains a live address.

        A merged class occupies as many addresses as it has members, so shrinking
        this would under-size a store that is still being written to at every one
        of them.
        """
        return self.inner.concepts

    def representative(self, concept: int) -> int:
        """The class's smallest member, agreed by every node that knows the same
        merges regardless of the order they arrived in."""
        seen = concept
        while seen in self._parent:
            seen = self._parent[seen]
        return seen

    def merge(self, one: int, other: int) -> None:
        """Record that two concepts are the same thing. Idempotent."""
        if one == other:
            return
        self._merges.add((min(one, other), max(one, other)))
        left, right = self.representative(one), self.representative(other)
        if left == right:
            return
        # The LARGER representative points at the smaller, which is what makes
        # the outcome independent of arrival order.
        self._parent[max(left, right)] = min(left, right)

    def aliases(self, concept: int) -> tuple[int, ...]:
        """Every concept in `concept`'s class, smallest first.

        A reader gathers over these. Sorted so two nodes that know the same merges
        produce the same order -- a read that combined them in different orders
        would give different floating-point sums for the same question.
        """
        root = self.representative(concept)
        members = {c for c in self._parent if self.representative(c) == root}
        members.add(root)
        members.add(concept)
        return tuple(sorted(members))

    @property
    def merges(self) -> frozenset[tuple[int, int]]:
        """What this node has learned, as the SET that determines the mapping."""
        return frozenset(self._merges)
