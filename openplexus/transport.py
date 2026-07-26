"""Delivery over a network that is late, jittered and out of order — G2's instrument.

[GOALS.md](../GOALS.md) C2 asks for a **stated bound** with exact behaviour below
it, not graceful degradation: "a design that merely degrades gracefully is weaker
than one with a stated, tested bound, because only the latter can be engineered
against."

[docs/notes/002](../docs/notes/002-which-credit-assignment-scheme.md) §4 argues the
predictive objective converts latency from a *race* into a *buffer depth*. This
module is what turns that argument into a measurement.

## Emission-time indexing

Every event carries the step it was **emitted**, not the step it arrived. A
receiver holds a buffer `max_delay` deep and does not process emission index `t`
until `t + max_delay` has passed — by which point, if the bound holds, everything
with index `t` or lower has landed.

The consequence is the whole point:

> **Below the bound, arrival order is irrelevant.** Two runs whose packets
> arrived in completely different orders reassemble to the *same* sequence and
> therefore produce bit-identical weights. Above it, they do not.

This is the one idea [GOALS.md §6.1](../GOALS.md) rated as transferring at high
confidence from the predecessor project, and it was rated that way because it is
a property of the indexing scheme rather than of any model. It is re-derived here
rather than imported, as that section said to do.

## What this is not

A real transport. There are no sockets, no machines and no clock. What is modelled
is the only thing C2 cares about: that an event may arrive later than it was sent,
and out of order relative to its neighbours.
"""

from __future__ import annotations

import random
from dataclasses import dataclass


@dataclass(frozen=True)
class DeliveryConfig:
    """How badly the network misbehaves.

    Attributes:
        max_delay: The receiver's buffer depth, in steps. **This is the stated
            bound.** An event emitted at `t` is processed at `t + max_delay`.
        jitter: The largest delay actually applied to any event. When
            `jitter <= max_delay` every event lands in time and reassembly is
            exact; above it, some miss their slot. See `within_bound`.
        drop: Fraction of events lost entirely, for C3. Distinct from lateness:
            a dropped event never arrives at any delay.
        seed: Determines the delays and drops completely.
    """

    max_delay: int = 4
    jitter: int = 0
    drop: float = 0.0
    seed: int = 0

    def __post_init__(self) -> None:
        if self.max_delay < 1:
            raise ValueError("max_delay must be at least 1")
        if self.jitter < 0:
            raise ValueError("jitter cannot be negative")
        if not 0.0 <= self.drop < 1.0:
            raise ValueError("drop must be in [0, 1)")

    @property
    def within_bound(self) -> bool:
        """Whether every event is guaranteed to land before its slot is processed.

        The comparison is inclusive, and the reason is a deliberate ordering
        choice in `reassemble`: a receiver records everything that arrived on a
        step *before* releasing that step's slot. An event delayed by exactly
        `max_delay` therefore lands on the very step its slot is processed, and
        makes it.

        **Tolerance is `max_delay`, derived from this implementation.** The
        predecessor project measured `delay_min - 1` for its own scheme, and the
        first version of this file asserted that number instead of deriving one —
        which is exactly what `GOALS.md` §6.1 said not to do when it rated
        emission-time indexing as transferring "as a technique, to re-derive".
        The off-by-one is a property of where the release check sits, not a law.
        """
        return self.jitter <= self.max_delay


def arrivals(n_events: int, config: DeliveryConfig) -> list[tuple[int, int]]:
    """Return `(arrival_step, emission_index)` pairs in the order they land.

    Each event `t` is emitted at step `t` and arrives at `t + delay`, with delay
    drawn uniformly from `[0, jitter]`. Ties are broken by arrival order, which is
    how a real receiver would see them.
    """
    rng = random.Random(config.seed)
    landed = []
    for emission in range(n_events):
        if config.drop and rng.random() < config.drop:
            continue
        delay = rng.randint(0, config.jitter) if config.jitter else 0
        landed.append((emission + delay, emission))
    # Sort by arrival, then by a shuffled tiebreak so that simultaneous arrivals
    # are not silently ordered by emission index — that would hide reordering
    # exactly where it matters most.
    rng.shuffle(landed)
    landed.sort(key=lambda pair: pair[0])
    return landed


def reassemble(landed: list[tuple[int, int]], config: DeliveryConfig) -> list[int]:
    """Reconstruct emission order from arrival order, using a bounded buffer.

    Returns the emission indices in the order a receiver would process them.

    Emission index `t` is released at step `t + max_delay`, *after* that step's
    arrivals have been recorded. Anything that has not arrived by then is missing
    from the output — the honest representation of a late or dropped event, rather
    than stalling forever waiting for it, or appending it out of order later where
    it would corrupt the sequence in a way much harder to notice than a gap.
    """
    held: dict[int, bool] = {}
    by_arrival: dict[int, list[int]] = {}
    for arrival_step, emission in landed:
        by_arrival.setdefault(arrival_step, []).append(emission)

    processed: list[int] = []
    last_step = max((step for step, _ in landed), default=-1)
    horizon = last_step + config.max_delay + 1
    for step in range(horizon):
        for emission in by_arrival.get(step, ()):
            held[emission] = True
        release = step - config.max_delay
        if release >= 0 and held.pop(release, False):
            processed.append(release)
    return processed


def delivered_order(n_events: int, config: DeliveryConfig) -> list[int]:
    """Emission indices in the order a receiver processes them.

    The convenience wrapper the experiments use. Below the bound and with no
    drops this returns `range(n_events)` exactly, whatever the network did.
    """
    return reassemble(arrivals(n_events, config), config)
