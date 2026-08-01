"""One graph holding more than one kind of thing.

**The thing this project has never had.** `CoOccurrence` is the whole
representation and three separate instances existed — images+audio+words,
intervention moments, knowledge-graph facts — each individually wired, each
individually tested, none of them ever meeting. Nothing was broken. Nobody had
asked how many there were, or what any one of them held.

A `SharedGraph` is a `CoOccurrence` and a `Namespace` and nothing else. Sources
speak in their own local ids, which all start at zero, and it translates:

    shared = SharedGraph()
    shared.reserve("image", 1024)
    shared.reserve("word", 10)
    shared.observe([("image", 7), ("word", 3)])

That occasion says *this picture and this word turned up together*, and the two
land on node numbers no other kind can take. A later source reserving `"fact"`
gets a block above both and adds facts to the SAME graph, so a route may cross
from a picture to a word to a fact without anything having been told that those
are different sorts of thing.

**Kinds are declared as data arrives, never as room is made.** `observe` reports
exactly the nodes it used, so `wiring.expect(holding=..., disjoint=True)` sees
what a graph actually holds rather than what someone intended it to hold — and a
run that reserves space for sound and feeds none fails, which is the point.
"""

from __future__ import annotations

from openplexus import wiring
from openplexus.grounding import CoOccurrence
from openplexus.namespace import Namespace


class SharedGraph:
    """A count graph several kinds of thing can be poured into.

    Attributes:
        index: The counts. One accumulator, and the reason this class exists.
        space: Which node numbers belong to which kind.
    """

    def __init__(self) -> None:
        self.index = CoOccurrence()
        self.space = Namespace()

    def reserve(self, kind: str, count: int) -> range:
        """Claim node numbers for a kind. **Does not declare that it arrived.**"""
        return self.space.reserve(kind, count)

    def observe(self, items) -> list[int]:
        """One occasion, given as `(kind, local id)` pairs. Returns its nodes.

        The pairs may name different kinds, and that is the entire purpose: an
        occasion holding a picture and a word is what lets a route cross between
        them later. A single-kind occasion is allowed and is what every source
        did before.
        """
        pairs = list(items)
        nodes = [self.space.node(kind, local) for kind, local in pairs]
        # DECLARED PER KIND, WITH THE NODES ACTUALLY USED. Handing `wiring` the
        # reserved block instead would report a kind as present on the strength
        # of room having been made for it.
        seen: dict[str, list[int]] = {}
        for (kind, _), node in zip(pairs, nodes):
            seen.setdefault(kind, []).append(node)
        for kind, used in seen.items():
            wiring.kind(kind, used)
        self.index.observe(nodes)
        return nodes

    def linked(self, one: str, other: str, depth: int = 4) -> bool:
        """Can any node of `one` reach any node of `other`?

        **The check the wiring instruments cannot make, and the one the real
        merge needs most.** `graph=1`, `holding={...}` and `disjoint=True` all
        pass a graph that is two disconnected islands sharing a dictionary:
        every kind arrived, nothing collided, and nothing connects. That is
        "one graph" in name only.

        It matters because kinds do not automatically meet. Pictures, sounds and
        words co-occur — they arrive in the same moment. Knowledge-graph facts
        share no occasion with a picture of a digit at all, so pouring both in
        produces two components and no route can ever cross between them.

        A merged graph is only one graph if something bridges its kinds, and
        this is what says whether anything does.
        """
        starts = {self.space.node(one, i) for i in range(len(self.space.ids(one)))}
        targets = {self.space.node(other, i)
                   for i in range(len(self.space.ids(other)))}
        seen, edge = set(starts), set(starts)
        for _ in range(depth):
            nxt = {p for node in edge for p in self.index.partners(node)}
            if nxt & targets:
                return True
            edge = nxt - seen
            if not edge:
                return False
            seen |= edge
        return False

    def holds(self) -> set[str]:
        """Which kinds have actually been observed, not merely reserved."""
        return wiring.kinds() & set(self.space.kinds())
