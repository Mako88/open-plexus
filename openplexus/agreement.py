"""Does a graph split across owners answer like one held whole?

**The question `testbed/driver.py` left behind, asked of the count graph.** That
driver measured a distributed `Network` of `LocalAssociativeMemory` against a
single-process one, and its architecture was deleted in the restructure — but
its reason survives, in its own words: reporting agreement rather than accuracy
is *"the only measurement that distinguishes a network which is slow from one
which is wrong"*. A run that merely completed tells you nothing.

So this compares reads, surface by surface, and reports WHERE they differ rather
than whether they matched. **A count that disagrees by one is a routing bug; a
count that disagrees everywhere is a split that never happened**, and a single
boolean cannot tell those apart.

It is deliberately not a container harness. Two processes on a socket is a
different question — latency, departure, partition — and this is the one that
has to hold first, because a federation that disagrees in one process will
disagree in twelve.
"""

from __future__ import annotations


def disagreements(whole, federation, surfaces) -> list[tuple]:
    """Every read where a split graph differs from a whole one.

    Returns `(surface, what, whole_value, split_value)` tuples, empty when the
    two agree everywhere. `whole` is a `CoOccurrence`; `federation` is anything
    offering `seen`, `together` and `partners_of`.
    """
    found = []
    surfaces = list(surfaces)
    for surface in surfaces:
        here, there = whole.seen(surface), federation.seen(surface)
        if here != there:
            found.append((surface, "seen", here, there))
        mine = sorted(whole.partners(surface))
        theirs = sorted(federation.partners_of(surface))
        if mine != theirs:
            found.append((surface, "partners", mine, theirs))
        for other in surfaces:
            if other == surface:
                continue
            # READ AS A NODE READS. `at(owner)` is the path `rank` uses, so
            # this checks the routing rather than stepping past it.
            view = federation.at(federation.owner(surface))
            a, b = whole.together(surface, other), view.together(surface, other)
            if a != b:
                found.append(((surface, other), "together", a, b))
    return found


def summary(found: list[tuple], surfaces) -> str:
    """A line that says which of the two failures this is."""
    if not found:
        return f"agrees on all {len(list(surfaces))} surface(s)"
    kinds: dict[str, int] = {}
    for _, what, _, _ in found:
        kinds[what] = kinds.get(what, 0) + 1
    detail = ", ".join(f"{n} {what}" for what, n in sorted(kinds.items()))
    return f"DISAGREES: {detail}"
