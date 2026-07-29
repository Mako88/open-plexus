"""A store split by CONCEPT: superposition within a node, selection across them.

## What this is and why

Decision 134 measured the case. At equal per-node memory, pooled capacity is
**identical** to dimension splitting — 128, 256, 512, 1024, 2048 at 1 to 16
nodes in both. What differs is what a node can do ALONE:

    nodes    concept alone    dimension alone
    1                  128                128
    16                2048                128

Under dimension splitting, growing the network makes every node's view thinner
while the total stays the same, so **a node can never answer alone however large
the system gets.** Under concept splitting a node owns whole concepts, so its
standalone capability grows with the network.

**That is what amended C1 is about.** A read that requires every node is the
barrier the constraint forbids.

## The design decision, which is not the obvious one

The tempting version is a distributed hash table: one slot per concept, reads are
exact lookups. **Decision 119 rules that out** — the superposed store beat a
bounded cache **by a factor of eight** when bindings exceed slots, because it
holds far more than its size, degraded, where a cache holds its slot count and
then fails.

So each node keeps a **small superposed store** over the concepts it owns.
Superposition within a node, selection across them. That is the synthesis
decisions 119 and 134 jointly point at and neither alone would have found.

## The interface change this forces, and it is worth naming

`Retrieval.read(readable, key)` takes a key VECTOR. Routing needs the **token
id**, because ownership is decided by concept and a key vector cannot be
inverted to one.

So a concept-partitioned store cannot sit behind the existing retrieval seam
unchanged: **the identity of what is being looked up has to travel with the
lookup.** With `derived_keys` the model already has the id at every call site,
so this costs an argument rather than a redesign — but it is a real coupling and
it is why this is a module rather than another `Retrieval` implementation.

## What it does not do

**Replication.** Losing a node loses its concepts entirely, which is a sharper
failure than dimension splitting's uniform degradation. The fix is holding each
concept on `r` nodes, and that is the DHT literature GOALS §6.2 has listed as
unread since the project began. `lose()` exists so the cost is measurable
before anything is built to mitigate it.
"""

from __future__ import annotations

import numpy as np

from openplexus.ownership import Ring


class ConceptStore:
    """`nodes` independent superposed stores, one owner per concept.

    Attributes:
        nodes: How many stores there are.
        width: Each store's width. **Full width, not a slice** — that is the
            whole point, and it is why a lone node's capability grows with the
            network rather than shrinking.
        ring: Decides ownership. Consistent hashing, so a node joining or
            leaving moves about 1/n of concepts rather than nearly all.
    """

    def __init__(self, nodes: int, width: int, seed: int = 0) -> None:
        if nodes < 1:
            raise ValueError("a store needs at least one node")
        self.nodes = nodes
        self.width = width
        self.ring = Ring(nodes, seed=seed)
        self._stores = [np.zeros((width, width)) for _ in range(nodes)]
        #: Nodes that have vanished. Their concepts are gone -- not degraded,
        #: GONE -- which is the cost of this arrangement and the reason
        #: replication is the next thing it needs.
        self._absent: set[int] = set()

    def owner(self, concept: int) -> int:
        return self.ring.owner(concept)

    def write(self, concept: int, key: np.ndarray, value: np.ndarray) -> None:
        """Bind `value` to `key`, on whichever node owns `concept`.

        Writes to ONE node. Under dimension splitting every node writes a slice
        of every binding; here a binding lives in one place, which is what makes
        a later read a selection rather than a collective.
        """
        node = self.owner(concept)
        if node in self._absent:
            return
        self._stores[node] += np.outer(value, key)

    def read(self, concept: int, key: np.ndarray) -> np.ndarray:
        """Read from the owning node alone.

        **No pooling, no vote, no barrier.** This is the property the whole
        arrangement exists for: note 009 §4's outstanding cross-group sum does
        not get smaller here, it stops existing.

        A concept whose owner has vanished returns zeros — an honest absence
        rather than a degraded answer, and `lose()` is how that is measured.
        """
        node = self.owner(concept)
        if node in self._absent:
            return np.zeros(self.width)
        return self._stores[node] @ key

    def lose(self, node: int) -> None:
        """That node vanishes and takes its concepts with it.

        C3's normal case, and this arrangement's sharpest cost. Under dimension
        splitting a departure degrades every concept slightly; here it removes
        some entirely and leaves the rest untouched. Which is preferable is a
        measurement, not a preference, and this is what makes it measurable.
        """
        self._absent.add(node)

    @property
    def numbers_held(self) -> int:
        """Total numbers across all nodes, for equal-state comparisons.

        g10-09 was retracted for comparing a model with a cache against one
        without at equal WIDTH rather than equal STATE. Any comparison using
        this store should quote this beside it.
        """
        return self.nodes * self.width * self.width

    def load(self) -> list[int]:
        """Non-zero-ish store count per node, for checking the ring's balance
        in situ rather than trusting `Ring.balance`."""
        return [int(np.count_nonzero(store.any(axis=0)))
                for store in self._stores]
