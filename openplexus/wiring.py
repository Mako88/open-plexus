"""Did this run go through the architecture we think it has?

**John's ask, 2026-08-01, and it is a different question from a precondition.**
A precondition asks whether a run could have SEEN what it was looking for. This
asks whether the run went through the structure it claims. Nothing here would
have caught an under-resourced arm, and no precondition would have caught THREE
GRAPHS where the design says one.

That is the case this is built for. `CoOccurrence` is the whole representation,
and three separate instances existed — images+audio+words, the intervention
moments, the knowledge-graph triples — for as long as anyone can tell. Every
part was individually wired and tested. Nothing asked how many there were.

**So this counts instances, not calls.** "The module ran" was already true in
the failure it exists to prevent.

    with wiring.expect(graph=1):
        ...                          # raises if a second graph is built

A LOG WOULD NOT HAVE CAUGHT IT EITHER, which is the argument for failing loudly:
the trace was always available to anyone who went looking, and the reason nobody
found three graphs is that nobody had a reason to count. A declaration that is
checked mechanically does not depend on somebody wondering.

Off by default and free when unused: `touch` is a dict increment, and nothing
raises unless a run declares an expectation.
"""

from __future__ import annotations

import threading

_lock = threading.Lock()
_seen: dict[str, int] = {}
#: kind -> the node numbers it occupies, for the disjointness check.
_ids: dict[str, set[int]] = {}


class WiringError(AssertionError):
    """A run went through a different architecture than it declared."""


def touch(part: str) -> None:
    """Record that one instance of `part` came into being."""
    with _lock:
        _seen[part] = _seen.get(part, 0) + 1


def kind(name: str, ids=()) -> None:
    """Record that a KIND of thing entered a graph.

    **The check that finds the fault counting instances cannot.** One arm
    building one graph is right, and a sweep building many is right, so
    `expect(graph=1)` passes everywhere and says nothing. What was never true is
    that any single graph held pictures AND sounds AND words AND facts — three
    populations, each in its own accumulator, none ever meeting.

    The caller declares the kind because a `CoOccurrence` cannot know one: it
    holds integers, and what those integers MEAN lives with whoever fed them in.
    That is also why this cannot be inferred later from the data.

    Pass `ids` — the node numbers this kind occupies — and `expect(disjoint=
    True)` will refuse a merge in which two kinds share one. **That check is
    needed because the kind check cannot see the fault it matters most for.**
    All three sources here number from zero, so a naive merge puts image code 0,
    concept surface 0 and entity 0 in ONE row: every declared kind arrives, the
    declaration passes, and the counts are silently wrong.
    """
    touch(f"kind:{name}")
    if ids:
        with _lock:
            _ids.setdefault(name, set()).update(int(i) for i in ids)


def kinds() -> set[str]:
    """Which kinds have entered a graph since the last reset."""
    return {part.split(":", 1)[1] for part in trace() if part.startswith("kind:")}


def trace() -> dict[str, int]:
    """What has been built since the last reset."""
    with _lock:
        return dict(_seen)


def reset() -> None:
    with _lock:
        _seen.clear()
        _ids.clear()


def overlaps() -> dict[tuple[str, str], int]:
    """Which pairs of kinds share node numbers, and how many.

    Empty is the only healthy answer once more than one kind is in a graph.
    """
    with _lock:
        named = sorted(_ids)
        return {(a, b): len(_ids[a] & _ids[b])
                for i, a in enumerate(named) for b in named[i + 1:]
                if _ids[a] & _ids[b]}


class expect:
    """Declare the architecture a block is supposed to use, and enforce it.

    Counts are EXACT. `graph=1` fails on two and equally on none — a run that
    declares a graph and builds none has not passed a weaker version of the
    test, it has failed to do the thing.

    Parts not named are ignored, so a declaration says what a run is ABOUT
    rather than having to enumerate everything the process touches.
    """

    def __init__(self, holding: set[str] | None = None,
                 disjoint: bool = False, **counts: int) -> None:
        #: The kinds this run says its graph holds. **Exact, like the counts**:
        #: a run declaring pictures and sounds and getting only pictures has
        #: not half-passed, and one that quietly gains a kind nobody declared
        #: is the merge doing something its author did not describe.
        self.holding = None if holding is None else set(holding)
        #: Refuse a graph in which two kinds share a node number. Off by
        #: default so a run that declares nothing is unaffected.
        self.disjoint = disjoint
        self.counts = counts

    def __enter__(self) -> "expect":
        reset()
        return self

    def __exit__(self, kind, value, traceback) -> bool:
        # AN EXCEPTION ON THE WAY OUT WINS. Reporting a wiring mismatch caused
        # by a crash halfway through would bury the crash.
        if kind is not None:
            return False
        got = trace()
        if self.holding is not None:
            had = kinds()
            if had != self.holding:
                missing = sorted(self.holding - had)
                extra = sorted(had - self.holding)
                raise WiringError(
                    "this run's graph did not hold what it declared -- "
                    f"never arrived: {missing or 'none'}; "
                    f"undeclared: {extra or 'none'}")
        if self.disjoint:
            shared = overlaps()
            if shared:
                detail = ", ".join(f"{a} and {b} share {n} node number(s)"
                                   for (a, b), n in sorted(shared.items()))
                raise WiringError(
                    "two kinds are occupying the same nodes, so their counts "
                    f"are being added together -- {detail}. Every source here "
                    "numbers from zero; a merge needs a namespace.")
        wrong = {part: (want, got.get(part, 0))
                 for part, want in self.counts.items()
                 if got.get(part, 0) != want}
        if wrong:
            detail = ", ".join(
                f"{part}: declared {want}, built {had}"
                for part, (want, had) in sorted(wrong.items()))
            raise WiringError(
                f"this run did not use the architecture it declared -- {detail}"
                f". Full trace: {got}")
        return False
