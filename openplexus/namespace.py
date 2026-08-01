"""Disjoint node numbers for kinds that all number from zero.

**The piece a shared graph cannot exist without.** `CoOccurrence` holds integers
and nothing else, so two sources that both start at 0 do not merge — they
collide, and image code 0, concept surface 0 and knowledge-graph entity 0 become
one row whose counts are the sum of three unrelated things. Nothing raises. The
numbers are simply wrong, which is the failure mode this project pays for most.

Every source here numbers from zero:

    surfaces_pipeline  image [0, codes), audio [codes, 2*codes), words above
    occasions          concept surfaces, then distractors, then shadows
    fb15k237           {entity: i for i, entity in enumerate(entities)}

So each kind reserves a block and translates its own local ids into it:

    space = Namespace()
    space.reserve("image", 1024)
    space.reserve("fact", 14541)
    node = space.node("image", 7)      # a global number no fact can take

**Reserving is NOT declaring.** `wiring.kind` is called when data actually
arrives, and deliberately not here, because a run that reserves room for sound
and then feeds none has a graph that does not hold sound — and that is exactly
what `expect(holding=...)` exists to catch. Wiring them together would make the
reservation satisfy the check, which is the check being fooled by the thing it
is watching.
"""

from __future__ import annotations


class Namespace:
    """Blocks of node numbers, one per kind, allocated in order and never reused.

    Attributes:
        size: How many node numbers have been handed out in total.
    """

    def __init__(self) -> None:
        self._blocks: dict[str, range] = {}
        self.size = 0

    def reserve(self, kind: str, count: int) -> range:
        """Claim `count` node numbers for `kind` and return them.

        Re-reserving a kind is refused rather than extended. A caller asking
        twice has either lost track of its own layout or is about to renumber
        nodes a graph already holds, and both produce counts attributed to the
        wrong thing.
        """
        if kind in self._blocks:
            raise ValueError(
                f"{kind!r} already holds {self._blocks[kind]}; reserving again "
                "would renumber nodes the graph may already hold")
        if count < 0:
            raise ValueError(f"cannot reserve {count} node numbers for {kind!r}")
        block = range(self.size, self.size + count)
        self._blocks[kind] = block
        self.size += count
        return block

    def node(self, kind: str, local: int) -> int:
        """This kind's `local` id as a global node number.

        Out-of-range is an error and not a wrap. A silently wrapped id lands in
        a NEIGHBOURING kind's block, which is the collision this module exists
        to prevent, arriving by a different road.
        """
        block = self._blocks.get(kind)
        if block is None:
            raise KeyError(f"{kind!r} has reserved nothing")
        if not 0 <= local < len(block):
            raise IndexError(
                f"{kind!r} local id {local} is outside its {len(block)} "
                "reserved node number(s)")
        return block.start + local

    def owner(self, node: int) -> str:
        """Which kind a global node number belongs to.

        The inverse of `node`, and what makes a merged graph readable: a route
        that crossed from a picture to a fact can say so.
        """
        for kind, block in self._blocks.items():
            if node in block:
                return kind
        raise KeyError(f"node {node} is in no reserved block")

    def ids(self, kind: str) -> range:
        """The node numbers `kind` holds, for `wiring.kind(name, ids)`."""
        block = self._blocks.get(kind)
        if block is None:
            raise KeyError(f"{kind!r} has reserved nothing")
        return block

    def kinds(self) -> tuple[str, ...]:
        return tuple(self._blocks)
