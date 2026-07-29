"""Which node owns a concept, decided without a directory or a coordinator.

## Why this exists

Decision 134 measured the case for concept partitioning: pooled capacity is
identical to dimension splitting, but **lone-node capacity is sixteen times
larger at sixteen nodes** and grows with the network, where dimension splitting
leaves a node stuck at one node's worth forever. A read that needs every node is
the barrier amended C1 forbids, so what one node can do alone is the question.

That arrangement needs one thing the project does not have: **given a concept,
which node holds it?**

## Why a RING and not `hash(token) % nodes`

Modulo is the obvious answer and it is wrong here for a reason C3 makes constant.
Changing the node count remaps **nearly every key**, so one machine joining or
leaving would move the whole store — and C3's premise is that machines leave
without warning, all the time.

Consistent hashing exists for exactly this. Nodes and keys are placed on a
circle; a key belongs to the first node clockwise of it. **Adding the nth node
moves only about 1/n of the keys**, and removing one moves only the keys it held
to its clockwise neighbour. Nothing else is disturbed.

Read from the [Wikipedia
description](https://en.wikipedia.org/wiki/Consistent_hashing) — **a summary,
not Karger et al. (1997)**, which remains unread. Rule 1: nothing here may be
quoted as a property of consistent hashing until the paper is read. The
mechanics below are implemented and tested against this module's own behaviour,
which is a different and weaker claim.

## Virtual nodes, and why the default is not 1

With one position per node the ring is lumpy: a node landing next to another
owns almost nothing while a node with a large gap before it owns far too much.
Several positions per node average that out, and they also **scatter a departed
node's load across many successors instead of dumping it on one** — which is the
property C3 wants, because the survivor of a departure should not inherit a
double share.

## Locality

**A node computes its own positions from its index and needs no directory.**
Positions come from `(seed, node, replica)`, exactly as `derived_keys` rebuilds a
key row from `(seed, token)` — the same argument, applied to ownership rather
than to keys. There is no membership service to be a coordinator, and a node
joining computes its share unilaterally.

**What this does NOT solve.** Every node must still agree on who is present, and
that is the membership problem SWIM addresses (note 039). This decides ownership
GIVEN a membership; it does not maintain one.
"""

from __future__ import annotations

import numpy as np

#: Positions per node on the ring. **Not 1**, which produces a visibly lumpy
#: assignment; the standard remedy is several labels per node.
#:
#: 64 is a guess in the same class as `RETRY_AFTER_STEPS` was before decision
#: 128 measured it — chosen because it is the usual order of magnitude, not
#: because anything here was measured. `balance()` exists so the cost of that
#: guess is visible rather than assumed.
REPLICAS = 64

#: The ring's circumference. A large integer rather than 360 degrees so that
#: positions collide only by accident rather than by resolution.
RING = 1 << 32

#: Separates the two things hashed onto the same ring. Without it, concept `c`
#: and node `c`'s first replica would draw the SAME position, so ownership would
#: correlate with the node index rather than being spread by the hash.
CONCEPT_DOMAIN = 1


class Ring:
    """Maps a concept to the node that owns it, by consistent hashing.

    Attributes:
        nodes: How many nodes are on the ring.
        replicas: Positions each node occupies.
    """

    def __init__(self, nodes: int, seed: int = 0,
                 replicas: int = REPLICAS) -> None:
        if nodes < 1:
            raise ValueError("a ring needs at least one node")
        if replicas < 1:
            raise ValueError(
                "a node with no positions on the ring owns nothing, so a ring "
                "of such nodes owns nothing and every lookup would fail")
        self.nodes = nodes
        self.replicas = replicas
        self.seed = seed
        # Positions and their owners, sorted once so a lookup is a binary
        # search. O(log N) rather than a hash table's O(1) -- the price of the
        # K/n guarantee, and it is paid per read.
        positions, owners = [], []
        for node in range(nodes):
            for replica in range(replicas):
                positions.append(self._position(node, replica))
                owners.append(node)
        order = np.argsort(positions)
        self._positions = np.asarray(positions, dtype=np.int64)[order]
        self._owners = np.asarray(owners, dtype=np.int64)[order]

    def _position(self, node: int, replica: int) -> int:
        """Where one of a node's labels sits, derived rather than stored.

        From `(seed, node, replica)` only, so a node computes its own positions
        knowing nothing but its index -- the same argument `derived_keys` rests
        on, applied to ownership.
        """
        return int(np.random.default_rng(
            (self.seed, node, replica)).integers(0, RING))

    def owner(self, concept: int) -> int:
        """The node holding `concept` -- first one clockwise of its position."""
        at = int(np.random.default_rng(
            (self.seed, CONCEPT_DOMAIN, concept)).integers(0, RING))
        index = int(np.searchsorted(self._positions, at, side="left"))
        # Past the last position wraps to the first, which is what makes it a
        # ring rather than a line.
        return int(self._owners[index % len(self._owners)])

    def owners(self, concepts: np.ndarray) -> np.ndarray:
        """`owner` for many concepts at once."""
        return np.asarray([self.owner(int(c)) for c in concepts])

    def balance(self, concepts: int = 4096) -> float:
        """Largest share of concepts any one node owns, over a fair share of 1.

        **The cost of the `replicas` guess, made visible.** A perfectly even
        ring returns 1.0; a lumpy one returns the factor by which the unluckiest
        node is overloaded. This is what would be measured before trusting any
        particular replica count, and it is reported rather than asserted
        because the right value depends on the node count.
        """
        counts = np.bincount(self.owners(np.arange(concepts)),
                             minlength=self.nodes)
        return float(counts.max() / (concepts / self.nodes))


def moved(before: Ring, after: Ring, concepts: int = 4096) -> float:
    """Share of concepts that change owner between two rings.

    **The K/n guarantee is the whole reason for the ring**, and this is how it
    is checked rather than assumed: adding the nth node should move about 1/n of
    the concepts, where modulo would move nearly all of them.

    C3 makes membership change the normal case, so this number is the cost of a
    machine arriving or leaving.
    """
    keys = np.arange(concepts)
    return float((before.owners(keys) != after.owners(keys)).mean())
