"""Walking a graph while carrying what the edges MEANT, and counting the result.

The mechanism that produced this project's first positive result on external
data, moved out of the sweep that discovered it. On FB15k-237 it clears a
structureless floor by +0.0136 ± 0.0005 over 40,932 queries, where published
ComplEx clears the same floor by +0.0136 and DistMult by +0.0076 — with no
training, no embedding and no gradient.

## What it is, in one sentence

Walk two steps from the thing being asked about, remember which KINDS of edge you
walked, and score wherever you land by how often that kind of route has meant the
kind of thing being asked.

## Why it is not `grounding.reach`, and why that is the open problem

`reach` spreads outward, multiplies strength along the path so distance costs
without a penalty being written anywhere, and bounds the search with a beam. It
is the propagation this project's architecture describes. **It is also blind to
what its edges mean**, and it was measured at 0.0082 against a floor of 0.2334.

This is the opposite trade. It knows exactly what each edge means and has no
propagation at all: routes are enumerated flat, to a fixed two steps, with no
decay and no beam. It works.

**So the half with the architecture's behaviour lacks the knowledge, and the half
with the knowledge lacks the behaviour, and joining them is untried.** The join
is not a refactor: two steps is not a parameter here, it is the shape of the
table. `PathTypes` maps a PAIR of edge kinds to one, so a walk of three steps has
to reduce the first two to a derived kind and compose again — which is exactly
what `tasks/clutrr.reachable` already does over an arbitrary chain, and the two
have never been pointed at each other.

## What this does NOT duplicate, and what was searched

Searched by capability — path, route, walk, compose, relation type, two-hop —
across `openplexus/`, `tools/`, `tests/` and `experiments/`.

- **`openplexus/composition.py` supplies the counting**, and this is a caller
  rather than a copy: a route says *these two edge kinds got where that one kind
  gets*, which is a composition fact, so `Composition` counts it unchanged.
- **`openplexus/grounding.py`** supplies every statistic. Nothing here computes
  one.
- **`openplexus/tasks/clutrr.py`** holds `reachable`, the bracketing search that
  composes a chain of any length. It is the missing half named above and it is
  deliberately not imported yet — wiring it in is a mechanism change and wants
  its own measurement, not a quiet import.
"""

from __future__ import annotations

from openplexus.composition import Composition
from openplexus.grounding import Statistic

#: How a candidate accumulates evidence from the routes that reach it.
#: **`sum` is the one that pays**, 0.1234 against `max`'s 0.0834 on FB15k-237,
#: and the difference is the whole claim of a ranked walk over a thresholded
#: lookup: many weak agreeing routes outrank one strong route. A rule miner
#: keeps its best rule and cannot express that.
ACCUMULATORS = ("max", "sum")


class PathTypes:
    """Counts of *what a KIND of two-step route means*, and the query over them.

    A route is a pair of edge kinds. Walking `born in` and then `located in` is
    one route kind; walking `acted in` then `directed by` is another. This counts
    which single edge kind each route kind tends to span, and reads it back.

    Attributes:
        kinds: How many edge kinds exist. **Both directions count separately**:
            traversing an edge backwards is its own kind, because *directed by*
            and *director of* relate different things and conflating them would
            average two different meanings into one row.
        counts: The `Composition` underneath, public because every measurement in
            this project is taken from counts rather than from a summary.
    """

    def __init__(self, kinds: int, spans: int) -> None:
        if kinds < 1 or spans < 1:
            raise ValueError("a route over no edge kinds spans nothing")
        self.kinds = kinds
        self.spans = spans
        self.counts = Composition(kinds, right=kinds, target=spans)

    def observe(self, first: int, second: int, spanned: int) -> None:
        """One route: `first` then `second` got where `spanned` gets directly."""
        self.counts.observe(first, second, spanned)

    def weight(self, first: int, second: int, asked: int,
               statistic: Statistic) -> float:
        """How much this route kind says about `asked`. Zero says nothing.

        Both halves of the route must support the answer — the demanding
        combination — so a first edge that leads everywhere cannot carry a route
        on its own. That is `grounding`'s own argument for refusing an
        ever-present partner, one level up.
        """
        answer = self.counts.surface("target", asked)
        return min(statistic(self.counts.index, answer,
                             self.counts.surface("left", first)),
                   statistic(self.counts.index, answer,
                             self.counts.surface("right", second)))

    def score(self, routes, asked: int, statistic: Statistic,
              accumulate: str = "sum") -> dict[int, float]:
        """Score every endpoint the routes reach. `routes` yields `(first, second, end)`.

        **A candidate no route reaches is absent rather than zero**, and the
        distinction is the mechanism's main failure mode: where nothing reaches
        the true answer this can only push other candidates above it, and no
        weighting repairs that — a convex blend of this with a baseline ranks
        identically to adding it on top, so there is no arrangement in which an
        unreached answer is left alone. Measured on FB15k-237: worth +0.0474
        where a route arrives and −0.0046 where none does.
        """
        if accumulate not in ACCUMULATORS:
            raise ValueError(f"accumulate must be one of {ACCUMULATORS}")
        found: dict[int, float] = {}
        for first, second, end in routes:
            weight = self.weight(first, second, asked, statistic)
            if weight <= 0.0:
                continue
            if accumulate == "max":
                found[end] = max(found.get(end, 0.0), weight)
            else:
                found[end] = found.get(end, 0.0) + weight
        return found


    def best(self, first: int, second: int,
             statistic: Statistic) -> tuple[int, float]:
        """The single kind this pair most amounts to, and how strongly.

        `weight` asks *does this pair mean the kind I was asked about*. This asks
        *what does this pair mean at all*, and it is what makes a walk longer
        than two steps possible: the pair reduces to ONE kind, which can then be
        composed with the next edge. The table stays pair-sized however deep the
        walk goes, where a table over triples would need 106 million rows against
        272,115 facts and could never recur.

        Returns `(-1, 0.0)` when the pair means nothing. A caller must stop
        there rather than carry a kind it invented.
        """
        found = (-1, 0.0)
        for span in range(self.spans):
            score = self.weight(first, second, span, statistic)
            if score > found[1]:
                found = (span, score)
        return found


def flood(adjacency, start: int, asked: int, types: PathTypes,
          statistic: Statistic, *, floor: float, depth: int = 3,
          accumulate: str = "sum", ceiling: int | None = None):
    """Expand EVERY edge strong enough, compose as you go, keep the route.

    John's design, 2026-08-02: *"in parallel at the same time, traverse all
    possibilities gated by the weight, and the thing that is traversing composes
    as it goes so that when we get to the end state, each thing that gets there
    has a whole string of reasoning."*

    It is the join of three halves that existed separately and were never
    connected:

    - `grounding.reach` spreads and multiplies strength along the path, so
      distance costs without a penalty being written anywhere — and is blind to
      what its edges mean.
    - `PathTypes` knows exactly what a pair of edges means — and is flat, with no
      propagation and no decay, fixed at two steps by the shape of its table.
    - `tasks.clutrr.reachable` composes a chain of any length by reducing pairs —
      on symbols, with no weights and no graph.

    **The budget is the weight and not a count.** Every edge above `floor` is
    expanded; nothing is sampled and nothing is capped at N. That is the whole
    point of the design and it is also the risk: an entity with 7,614 edges whose
    weights do not discriminate will not be pruned by any floor, and the flood
    does not die. Whether weights discriminate at hubs is unmeasured, so `floor`
    is swept rather than pinned.

    Args:
        adjacency: `entity -> iterable of (edge kind, neighbour, weight)`.
        start: Where the question begins.
        asked: The kind of connection the question is about.
        floor: Path strength below which a branch is abandoned. **The only
            budget.** At 0.0 this enumerates the reachable graph and will not
            return on a real one.
        depth: How many edges a route may use. A bound on the worst case, not a
            schedule — most branches die on `floor` long before it.

    Args (continued):
        ceiling: Expansions after which the walk gives up, and **a safety rather
            than part of the design**. The weight is meant to be the whole
            budget; this exists because a floor that fails to prune does not
            merely score badly, it does not return. A caller that sets it MUST
            report how often it fired, or a run that gave up looks exactly like
            a run that finished.

    Returns:
        `(endpoints, expansions, gave_up)`. `endpoints` maps each endpoint
        reached by a route whose composed kind is `asked` to `(score, route)`,
        where the route is the edge kinds walked in order -- the chain of
        reasoning for that answer. `expansions` is what the walk cost.
    """
    if accumulate not in ACCUMULATORS:
        raise ValueError(f"accumulate must be one of {ACCUMULATORS}")
    if depth < 2:
        raise ValueError("a composed route needs at least two edges")
    if floor <= 0.0:
        raise ValueError(
            "a floor of zero is not a budget: every edge would be expanded to "
            "the depth limit, which on a real graph does not return")

    found: dict[int, tuple[float, tuple[int, ...]]] = {}
    expansions = 0
    # Each frontier entry carries where it is, what the route so far AMOUNTS to
    # as a single kind, how strong it is, and the kinds it walked.
    frontier = [(start, -1, 1.0, ())]
    for _ in range(depth):
        following = []
        for here, carried, strength, route in frontier:
            for kind, neighbour, weight in adjacency(here):
                expansions += 1
                if ceiling is not None and expansions > ceiling:
                    return found, expansions, True
                travelled = strength * weight
                # A COST GUARD, not a correctness one: strength
                # only decreases, so every branch this drops the
                # post-composition floor would drop as well. It
                # saves the composition lookup, nothing more.
                if travelled <= floor or neighbour == start:
                    continue
                if carried < 0:
                    # First edge: the route so far is just that edge.
                    became, confidence = kind, 1.0
                else:
                    became, confidence = types.best(carried, kind, statistic)
                    if became < 0:
                        continue
                    travelled *= confidence
                    if travelled <= floor:
                        continue
                walked = route + (kind,)
                following.append((neighbour, became, travelled, walked))
                if became != asked or len(walked) < 2:
                    continue
                held = found.get(neighbour)
                if held is None:
                    found[neighbour] = (travelled, walked)
                elif accumulate == "max":
                    if travelled > held[0]:
                        found[neighbour] = (travelled, walked)
                else:
                    # Agreeing routes add up, and the route KEPT is the
                    # strongest single one -- a sum has no route of its own and
                    # reporting the last-seen would make the explanation
                    # whichever branch happened to be visited last.
                    best_route = held[1] if held[0] >= travelled else walked
                    found[neighbour] = (held[0] + travelled, best_route)
        if not following:
            break
        frontier = following
    return found, expansions, False


def concentration(scores: dict[int, float]) -> float:
    """The largest candidate's share of the total. **A confidence, not a fix.**

    High when the routes agree on one endpoint, low when they spray over
    hundreds, and it needs no fitted constant. It was built to test whether the
    queries this mechanism loses are the ones where its evidence is thin.

    **They are not.** Weighting by it scores +0.0131 against a flat weight's
    +0.0136, with slightly MORE losses, because with `sum` the concentration is
    lowest exactly where accumulation is doing its work. Kept because it earns
    more where a route does reach the answer (+0.0574 against +0.0474) and
    because it cannot blow up — at full weight it holds 0.2379 where a flat
    weight collapses to 0.1278 — which is the form to use where there is no
    validation set to choose a weight with.
    """
    if not scores:
        return 0.0
    total = sum(scores.values())
    return max(scores.values()) / total if total > 0 else 0.0
