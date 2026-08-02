"""Broadcast a set of surfaces, let the graph carry it, and keep every chain.

John's design, 2026-08-01. Input is broadcast to every node. A node linked to
something in the broadcast re-broadcasts, appending itself, so a route arrives
carrying the whole chain of reasoning that produced it.

## What is different from `pathways.flood`

`pathways.flood` walks from ONE start and is gated by what a pair of edge KINDS
means. That gate needs two things a senses graph does not have: kinds on the
edges, and a kind on the question. Here there is neither, so the discrimination
has to come from somewhere else, and the design's answer is that MANY surfaces
broadcast at once and their routes accumulate on the same endpoints.

That claim is untested. It is the reason this module exists and the first thing
to measure.

## The gate is `forward`, not mutual, and the design said mutual

The design says *"refuel on MUTUAL strength, never raw"*, to keep the
ever-present background from funding routes straight through itself. The intent
is right and mutuality is the wrong instrument for it — but not for the reason a
first version of this docstring gave, which a test refuted.

Measured on `tests/test_broadcast._two_concepts`, which has the real
proportions: a word on 845 occasions, each of its codes on 60, a distractor on
all 3,845.

    seeded at a rare code   min  0.2298 vs 0.1231   correct
    seeded at the hub word  min  0.0766 vs 0.3592   INVERTED

**Mutuality is not wrong everywhere. It is wrong from the common end**, because
`conditional(code, word)` is 0.071 where `conditional(distractor, word)` is
1.00, so `min` takes the small number for the real partner and the large one for
the background.

**A flood stands on both ends during one walk.** A route seeded at an image code
arrives at the word and then expands from the word, and that hop is scored from
the hub's side. So a combiner correct only at the rare end funds the background
halfway through every route, which is exactly the failure the design was trying
to avoid. `forward` is the only combiner correct at both ends; `mean` and `max`
invert at both.

So the default is `forward` and `combine` is swept rather than pinned. The
principle the design was reaching for survives, stated the other way round:
**score a candidate by how well IT predicts YOU**, which refuses a thing that is
everywhere precisely because it predicts nothing in particular.

## Two counters, and they must not be merged

`stamina` is refuelled. The termination count is conserved. Merging them is the
obvious economy and it breaks: Huang's weight-throwing detects termination
precisely because total weight is conserved, and a refuelled budget creates
weight from nothing. The accounting below is Dijkstra-Scholten's shape instead —
a route splitting into k reports k, a route dying reports one death, and the
origin knows the thought is over when the live count reaches zero.

**In one process that count is exact. Over a network it is not**, because C2
loses messages and C3 removes nodes mid-route, and one lost death report leaves
the origin waiting forever. The deadline in decision 9 is what has to back it.
Nothing here sends a message, so nothing here has that problem yet.

## What was searched

By capability — broadcast, flood, propagate, spread, walk, route, stamina,
budget, terminate — across `openplexus/`, `experiments/`, `tests/`, `tools/`.

- **`grounding.strength` supplies the edge weight** and is called, not copied.
  It already holds every combiner and the evidence for `forward`.
- **`grounding.routed` is the closest existing walk** and is deliberately not
  reused: it is best-first with a beam, expands one start, and bounds the search
  by `beam` and `depth`. This has many starts, no beam, and is bounded by a
  budget that can be refilled. Sharing an implementation would mean one of them
  pretending to be the other.
- **`pathways.flood`** is the typed version, over kinds this graph has not got.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

from openplexus.grounding import CoOccurrence, Statistic, strength

#: How a route pays for a step.
#:
#: - `constant` charges the same everywhere. One tuned number, which is what
#:   the stamina design set out to remove.
#: - `local` charges the MEAN weight of the edges leaving the node. **Refuted.**
#:   About half a node's edges are above its own mean, so a route taking
#:   above-mean steps gains stamina forever. On the senses graph at 6 bits it
#:   was unbounded at every budget tried from 0.002 to 0.25 — all 44 audio codes
#:   reached, `gave_up` 1.00, agreement 0.1082 against a chance of about 0.108.
#:   A walk that reaches everything has answered nothing. It is kept as an arm
#:   because a refutation that cannot be re-run is a claim.
#: - `best` charges the STRONGEST edge available at the node — an opportunity
#:   cost. Stamina can then never rise, so only a route taking near-best edges
#:   keeps its budget. **The one measured to bound the walk**: 0.3 audio codes
#:   reached at a budget of 0.002, 2.1 at 0.01, 19.4 at 0.05, everything at
#:   0.25.
#:
#: **The design's claim that stamina removes a tuned constant is false**, and
#: the numbers above are why: the starting budget has a scale, and the scale has
#: to be swept exactly as a floor does. What stamina genuinely buys is history —
#: a route that walked strong edges can afford a weak one, where a floor cuts on
#: the single step in front of it. That is a real difference and it is a
#: different claim.
COSTS = ("constant", "local", "best")

#: What a route is paid for taking an edge — **the budget, not the score.** The
#: score is always the path strength; this decides only which routes survive.
#:
#: - `strength` pays the edge weight, so a route survives by walking edges the
#:   counts already favour. It can therefore only ever surface what is already
#:   expected, which is decision 4's standing objection to every walk here.
#: - `surprise` pays `-log2(weight)`, so a route survives by walking edges that
#:   were unlikely. This is "walk toward surprise rather than strength" in the
#:   only form a broadcast can express it: **preference as economics rather than
#:   as selection.** A sequential walk ranks its frontier and picks; a broadcast
#:   cannot rank anything globally, but it can make the unexpected cheaper to
#:   keep walking.
#:
#: Both are local: `-log2(conditional(other, here))` needs one node's own row
#: and one marginal, where `ppmi` needs the global occasion total that C1
#: forbids. That is why the surprise measure here is not `grounding.ppmi`.
#:
#: **Named risk**: the rarest edges are also the noisiest, so this may fund
#: routes through coincidence. That is the thing to measure, not to assume away.
REFUELS = ("strength", "surprise")

#: How a candidate accumulates evidence from the routes reaching it. `sum` is
#: what pays on the typed walk, 0.1234 against `max`'s 0.0834, and the reason is
#: the claim being made here: many weak agreeing routes should outrank one
#: strong route. Whether that survives without kinds is what this measures.
ACCUMULATORS = ("max", "sum")


@dataclass
class Arrival:
    """What reached an endpoint, and the strongest chain that got there.

    `score` is what the accumulator made of every route; `best` is the strength
    of the single strongest one. They are separate because under `sum` the score
    is not any route's strength, so it cannot be used to decide which chain to
    keep as the explanation.
    """

    score: float
    chain: tuple[int, ...]
    best: float = 0.0
    routes: int = 0


@dataclass
class Flood:
    """A finished broadcast, including what it cost.

    `messages` and `work` are the columns the design exists to produce.
    `expansions` in the typed flood counted only the walk; these count what a
    real network would have had to send and what one node would have had to do.

    Attributes:
        reached: Endpoint to `Arrival`. A surface no route reaches is absent
            rather than zero, the same distinction the typed walk turns on.
        messages: Every partner considered, which is one message on a network
            where the holder of a surface is another machine.
        work: Per node, how many partners it had to look at. The tail of this
            is the load a hub takes, and a mean hides it.
        live: Routes still alive when the walk stopped. Zero means the thought
            ended by its own accounting; anything else means `ceiling` fired.
        splits, deaths: The accounting itself, kept so the invariant
            `len(origins) + splits - deaths == live` can be asserted rather than
            trusted.
        gave_up: Whether `ceiling` stopped it. A run that gave up looks exactly
            like a run that finished unless this is reported.
    """

    reached: dict[int, Arrival] = field(default_factory=dict)
    messages: int = 0
    work: dict[int, int] = field(default_factory=dict)
    live: int = 0
    splits: int = 0
    deaths: int = 0
    gave_up: bool = False

    def balanced(self, origins: int) -> bool:
        """Whether the accounting adds up. One route in, k out, one death out."""
        return origins + self.splits - self.deaths == self.live

    def busiest(self) -> int:
        """Work done by the single hardest-hit node. The column a mean hides."""
        return max(self.work.values(), default=0)


def flood(index: CoOccurrence, statistic: Statistic, origins,
          *, stamina: float = 1.0, cost: str = "local",
          charge: float = 0.0, combine: str = "forward",
          refuel: str = "strength",
          accumulate: str = "sum", ceiling: int = 1_000_000) -> Flood:
    """Broadcast `origins` and return everything the graph carried them to.

    Every origin starts one route holding `stamina`. A route standing on a node
    considers each of that node's partners; the edge weight both funds the route
    and multiplies its strength. A route that cannot pay dies. When no route is
    alive the thought is over — there is no depth limit, because the budget is
    meant to be the whole of the schedule.

    A route may not revisit a node already in its own chain. That is a local
    check costing nothing, because the chain is already being carried, and it is
    what makes an unbounded walk terminate on a cyclic graph.

    Args:
        origins: The broadcast — the surfaces that fired. **Not a random
            seed**; the collision with `SEEDS` in the sweeps is why this is not
            called one. Many at once is the point — the discrimination
            that edge kinds supplied on a typed graph is supposed to come from
            routes converging, which cannot happen from a single seed.
        stamina: What each route starts with. **A swept budget, not a freed
            knob** — see `COSTS` for the measurement that killed that claim. It
            differs from a floor in carrying history rather than in having no
            scale: a route that walked strong edges can afford a weak one where
            a floor cuts on the single step in front of it.
        cost: A key of `COSTS`. `best` is the only one measured to bound the
            walk; `local` is refuted and kept so the refutation can be re-run.
        charge: The price per step when `cost` is `constant`. Ignored otherwise,
            and refused if set without it, because an argument that silently
            does nothing is a sweep arm that looks distinct and is not.
        ceiling: A safety, not part of the design. If it fires, `gave_up` is
            set, and a caller that does not report that is reporting a walk that
            stopped early as one that finished.

    Returns:
        A `Flood`. `reached` maps each endpoint to its best chain and its summed
        score; `messages`, `work` and `busiest()` are what it cost.
    """
    if cost not in COSTS:
        raise ValueError(f"cost must be one of {COSTS}")
    if refuel not in REFUELS:
        raise ValueError(f"refuel must be one of {REFUELS}")
    if accumulate not in ACCUMULATORS:
        raise ValueError(f"accumulate must be one of {ACCUMULATORS}")
    if cost == "local" and charge:
        raise ValueError(
            "charge is the price for cost='constant' and does nothing under "
            "'local'; passing both would give a sweep an arm that is not one")
    if cost == "constant" and charge <= 0.0:
        raise ValueError("cost='constant' needs a charge above zero")
    if stamina <= 0.0:
        raise ValueError("a route with no stamina cannot take its first step")

    origins = list(dict.fromkeys(origins))
    if not origins:
        raise ValueError("a broadcast of nothing reaches nothing")

    result = Flood(live=len(origins))
    frontier = [(origin, stamina, (origin,), 1.0) for origin in origins]

    while frontier:
        following = []
        for here, held, chain, carried in frontier:
            partners = index.partners(here)
            result.messages += len(partners)
            result.work[here] = result.work.get(here, 0) + len(partners)
            if result.messages > ceiling:
                result.gave_up = True
                return result

            weights = [(other, strength(index, statistic, here, other, combine))
                       for other in partners]
            # THE FUEL AND THE SCORE ARE TWO QUANTITIES. `weight` scores an
            # arrival; `paid` decides whether the route can keep going. Under
            # `strength` they are the same number, which is why they looked
            # like one thing until surprise needed them apart.
            paid = {other: (weight if refuel == "strength"
                            else -math.log2(weight) if 0.0 < weight < 1.0
                            else 0.0)
                    for other, weight in weights}
            live_weights = [fuel for fuel in paid.values() if fuel > 0.0]
            if cost == "local":
                price = (sum(live_weights) / len(live_weights)
                         if live_weights else 0.0)
            elif cost == "best":
                # Opportunity cost: the step is charged what the best step here
                # would have cost. Stamina is therefore non-increasing, and
                # strictly decreasing for anything but the strongest edge, so
                # the walk is bounded without a constant. See COSTS for the
                # measurement that ruled `local` out.
                price = max(live_weights, default=0.0)
            else:
                price = charge

            children = 0
            for other, weight in weights:
                if weight <= 0.0 or other in chain:
                    continue
                left = held - price + paid[other]
                if left <= 0.0:
                    continue
                onward = chain + (other,)
                travelled = carried * weight
                arrived = result.reached.get(other)
                if arrived is None:
                    result.reached[other] = Arrival(
                        travelled, onward, best=travelled, routes=1)
                else:
                    arrived.routes += 1
                    arrived.score = (max(arrived.score, travelled)
                                     if accumulate == "max"
                                     else arrived.score + travelled)
                    if travelled > arrived.best:
                        # The chain kept is the strongest single one. A sum has
                        # no chain of its own, and reporting the last arrival
                        # would make the explanation whichever branch happened
                        # to be walked last.
                        arrived.best = travelled
                        arrived.chain = onward
                children += 1
                following.append((other, left, onward, travelled))

            if children:
                # One route became `children` routes. The count moves by the
                # difference; a split is not a birth of `children` new ones.
                result.splits += children - 1
                result.live += children - 1
            else:
                result.deaths += 1
                result.live -= 1
        frontier = following

    return result
