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

**Repair.** Each concept is held on `replicas` nodes, so a departure is
survivable — 89.6% of concepts still reachable with HALF the network gone. But
**nothing restores a lost replica**, so the redundancy depletes under continued
churn and never recovers. See `lose()` for the measured decay. Under C3, which
makes churn the normal case rather than an event, that is the difference between
robust and merely slow to fail.
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

    def __init__(self, nodes: int, width: int, seed: int = 0,
                 replicas: int = 3) -> None:
        if nodes < 1:
            raise ValueError("a store needs at least one node")
        if replicas < 1:
            raise ValueError("a concept held nowhere is a concept lost")
        self.nodes = nodes
        self.width = width
        #: How many DISTINCT nodes hold each concept.
        #:
        #: **Not 1, and the default is not a detail.** John, 2026-07-29: *"when
        #: nodes drop you just lose concepts — that doesn't sound like a very
        #: robust system."* Correct, and it is the arrangement's sharpest cost:
        #: dimension splitting degrades every concept slightly on a departure,
        #: while this removes some entirely.
        #:
        #: **The arithmetic says the advantage can pay for the fix.** Decision
        #: 134 measured lone-node capacity 16x better at 16 nodes. Spending 3x
        #: on replicas leaves ~5x, and the loss probability falls from `f` to
        #: `f^3` — at 10% of nodes down that is 10% to 0.1%.
        #:
        #: Replicas are the next DISTINCT nodes clockwise, so a departure needs
        #: no data movement at all: the survivors already hold it.
        self.replicas = replicas
        self.ring = Ring(nodes, seed=seed)
        self._stores = [np.zeros((width, width)) for _ in range(nodes)]
        #: Nodes that have vanished. Their concepts are gone -- not degraded,
        #: GONE -- which is the cost of this arrangement and the reason
        #: replication is the next thing it needs.
        self._absent: set[int] = set()

    def owner(self, concept: int) -> int:
        return self.ring.owner(concept)

    def holders(self, concept: int) -> list[int]:
        """Every node holding `concept`, present or not."""
        return self.ring.holders(concept, self.replicas)

    def write(self, concept: int, key: np.ndarray, value: np.ndarray) -> None:
        """Bind `value` to `key`, on every node that holds `concept`.

        Writes to `replicas` nodes, not to all of them. Under dimension
        splitting every node writes a slice of every binding; here a binding
        lives in a handful of places, which is what makes a later read a
        selection rather than a collective — and what makes a departure
        survivable.
        """
        for node in self.holders(concept):
            if node not in self._absent:
                self._stores[node] += np.outer(value, key)

    def read(self, concept: int, key: np.ndarray) -> np.ndarray:
        """Read from ONE surviving holder.

        **No pooling, no vote, no barrier.** This is the property the whole
        arrangement exists for: note 009 §4's outstanding cross-group sum does
        not get smaller here, it stops existing. Replication does not change
        that — a read still touches one node, it just has a choice of which.

        Zeros only when EVERY holder has vanished, which is an honest absence
        rather than a degraded answer. `survival()` measures how often that
        happens.
        """
        for node in self.holders(concept):
            if node not in self._absent:
                return self._stores[node] @ key
        return np.zeros(self.width)

    def lose(self, node: int) -> None:
        """That node vanishes.

        ## NOTHING REDISTRIBUTES, and that is a gap rather than a choice

        John asked, 2026-07-29, whether concepts redistribute when a node drops.
        **They do not.** The ring is unchanged, so `holders` still names the
        departed node and a read simply falls through to the next survivor.
        Reads keep working — and the redundancy behind them **depletes**:

            nodes lost     survival     mean live holders per concept
                     0        1.000                              3.00
                     6        0.967                              2.03
                    10        0.873                              1.43
                    14        0.656                              0.84

        **Replication is a wasting asset without repair.** The 3 replicas are
        never restored, so under continuous churn — which is C3's premise — the
        count walks down to zero and survival follows it. The single-departure
        figures are the best case, not the steady state.

        The fix is standard and unbuilt: when a holder is lost, a survivor
        copies the concept to the next distinct node clockwise, restoring the
        count. Consistent hashing already determines WHO that is, so it is a
        local exchange between neighbours with no coordinator — which is why it
        fits C1. Its cost is `width^2` numbers per concept moved, and under
        constant churn that is constant background traffic, so it wants
        measuring against the `d_max` budget rather than assuming.

        That is anti-entropy and hinted handoff in the DHT literature, which
        GOALS §6.2 has listed unread since the project began.

        ## What a departure costs, either way

        C3's normal case. Under dimension splitting a departure degrades every
        concept slightly; here it removes some entirely and leaves the rest
        untouched. Which is preferable is a measurement, not a preference, and
        this is what makes it measurable.
        """
        self._absent.add(node)

    def live_holders(self, concept: int) -> int:
        """How many of this concept's holders are still present.

        **The depletion measure.** `survival` says whether a concept is
        reachable at all; this says how much redundancy is left behind that
        answer, which is what falls first and what repair would restore.
        """
        return sum(1 for node in self.holders(concept)
                   if node not in self._absent)

    def survival(self, concepts: int = 4096) -> float:
        """Share of concepts still reachable with the currently absent nodes.

        **The number John's objection is about**, measured rather than argued:
        *"when nodes drop you just lose concepts — that doesn't sound like a
        very robust system."*

        At `replicas = 1` this falls roughly as the fraction of nodes lost. At
        3 it falls as that fraction CUBED, because a concept is only gone when
        every holder is.
        """
        alive = sum(1 for concept in range(concepts)
                    if any(node not in self._absent
                           for node in self.holders(concept)))
        return alive / concepts

    @property
    def numbers_held(self) -> int:
        """Total numbers across all nodes, for equal-state comparisons.

        g10-09 was retracted for comparing a model with a cache against one
        without at equal WIDTH rather than equal STATE. Any comparison using
        this store should quote this beside it.
        """
        return self.nodes * self.width * self.width

    @property
    def numbers_per_concept(self) -> int:
        """What replication costs, in the unit a comparison should use.

        Replicas multiply storage. Quoting capacity without this would compare a
        3x-redundant store against a bare one at equal width, which is exactly
        the retraction g10-09 earned.
        """
        return self.replicas * self.width * self.width

    def load(self) -> list[int]:
        """Non-zero-ish store count per node, for checking the ring's balance
        in situ rather than trusting `Ring.balance`."""
        return [int(np.count_nonzero(store.any(axis=0)))
                for store in self._stores]
