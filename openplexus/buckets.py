"""The time bucket: how two machines discover a coincidence without asking.

[`time-bucket-join.md`](../docs/options/time-bucket-join.md) is the design. Two
nodes observing different things at one instant round the time to a coarse
bucket, both get the same number, and both send what they saw to whoever owns
that bucket. The owner notices the coincidence, writes the pairwise links out to
`owner(surface)`, **and throws the bucket away.**

Nobody asked anybody anything. That is the whole point: asking is the collective
amended C1 forbids.

## What this is FOR, which is not what `grounding.py` is for

`grounding.py` answers *can counting separate what belongs from what is merely
present*. `g32-01` and `g32-02` answered it: yes, with a chance-corrected
statistic, and a concept needs about sixteen occasions.

**This answers the other half, and it is the half the design exists for.** The
statistic was measured with every observation in one process and perfect
knowledge of which things co-occurred. Here the co-occurrence set is not given —
it has to be *reconstructed* from rounded clocks on machines that disagree about
the time and deliver late. Everything that reconstruction gets wrong is signal
lost, and the single-process score is the ceiling it is measured against.

Three ways it can go wrong, all of them named as objections in the option record
before anything was built:

    too NARROW   one moment straddles a boundary and its halves never meet
    too WIDE     unrelated moments land together and are bound as if they met
    SKEW         two clocks disagree, so one instant becomes two buckets

The first two pull in opposite directions, so **there has to be an interior
optimum or the mechanism has no operating point at all.** That is what makes this
a falsifier rather than a tuning exercise.

## What this does NOT duplicate, and what was searched

Searched by capability — bucket, window, join, episode, clock, skew, deadline —
across `openplexus/`, `tools/`, `tests/`, `testbed/` and `experiments/`.

- **`openplexus/transport.py` models a late, jittered, reordering network and is
  NOT reused, deliberately.** It indexes events by emission step, holds a buffer
  `max_delay` deep, and *reassembles order* — its whole subject is putting a
  sequence back into sequence. **A bucket has no order to restore and no
  buffer.** An observation is inside its bucket's window when the owner flushes
  or it is gone, and what is lost is a pairing rather than a position. The unit
  differs too: transport counts steps, this counts whatever the clock counts.
  Sharing the type would mean one config field meaning two things.
- **`openplexus/ownership.py` (`Ring`) IS used, not reimplemented.** A bucket id
  is an integer and the ring already maps integers to owners without a directory,
  which is exactly the property the join needs and the reason the option record
  says this is the existing mechanism applied to a different key.
- **`openplexus/grounding.py` (`CoOccurrence`) IS used, not reimplemented.** The
  bucket's whole output is pairs, and pairs are what that accumulator takes. The
  scoring path is therefore identical to the single-process one, which is what
  makes the comparison exact rather than approximate.
- **`openplexus/distributed.py` and `openplexus/peer.py`** are real sockets and
  real containers. This is the addressing and deadline logic, which is
  transport-agnostic and has to be right before any of it is worth wiring to a
  socket. `testbed/run.py` is where that happens.
- **`openplexus/tasks/occasions.py`** generates the stream and stays untouched:
  it has no notion of time beyond an index, and putting a clock model into the
  ruler would make the instrument depend on the mechanism it measures.

## What is NOT here, and the second item is the honest one

**No sockets, no processes, no containers.** Everything below runs in one
process, and a pass here says nothing about C1 — for the same reason
`grounding.py` gives: distribution can only lose information, so a failure here
is conclusive and a success is not. The container run is `testbed/run.py`.

**And the accumulator is NOT actually sharded.** `Join` holds one `CoOccurrence`
for the whole world. `Ring` decides which node owns each *bucket*, so the join
half is addressed for real — but *"the link is written to `owner(surface)`, where
it accumulates over that percept's lifetime"* is a claim this file asserts and
does not demonstrate. One object holding every surface's row cannot show that the
rows are separable, however true it is that they are.

That is a gap rather than a caveat, and it is the next thing to close: splitting
the accumulator by `owner(surface)` and counting the messages between the two
halves makes the locality structural instead of asserted, and produces a
bytes-per-observation figure G4 has never had for this path. Containers are the
step *after* that, because a socket around an unsharded object proves nothing
either.
"""

from __future__ import annotations

import random
from dataclasses import dataclass
from itertools import combinations

from openplexus.grounding import CoOccurrence
from openplexus.ownership import Ring


@dataclass(frozen=True)
class BucketConfig:
    """How the join is configured, and how badly the world misbehaves.

    Attributes:
        width: Bucket width, in the same units as an observation's timestamp.
            The quantity the whole mechanism turns on.
        spread: How many neighbouring buckets an observation is ALSO sent to.
            0 is plain rounding. 1 sends to the bucket either side, which is the
            option record's answer to boundaries, at a constant-factor cost in
            messages that `messages_per_observation` reports.
        skew: Largest clock offset any node carries, drawn uniformly from
            `[-skew, +skew]` per node and fixed for the run. A node's clock is
            wrong by a constant, which is what an unsynchronised machine looks
            like over any short window.
        lateness: Largest delivery delay from an observing node to a bucket's
            owner, drawn uniformly per observation.
        grace: How long after a bucket's window ends its owner waits before
            flushing and discarding it. An observation arriving after that is
            LOST — there is nothing durable at a time key to add it to later.
        drop: Fraction of observations lost entirely, for C3.
        nodes: How many machines own buckets and surfaces.
        observers: How many machines do the perceiving. Observation of a surface
            falls to `surface % observers`, so at 3 this is one machine per
            modality — the case the join exists for, where the picture and the
            sound genuinely arrive in different places.
        seed: Determines skew, delays and drops completely.
    """

    width: int = 1
    spread: int = 0
    skew: int = 0
    lateness: int = 0
    grace: int = 0
    drop: float = 0.0
    nodes: int = 8
    observers: int = 3
    seed: int = 0

    def __post_init__(self) -> None:
        if self.width < 1:
            raise ValueError("a bucket narrower than one tick holds nothing")
        if self.spread < 0:
            raise ValueError("spread cannot be negative")
        if self.skew < 0:
            raise ValueError("skew cannot be negative")
        if self.lateness < 0:
            raise ValueError("lateness cannot be negative")
        if self.grace < 0:
            raise ValueError("grace cannot be negative")
        if not 0.0 <= self.drop < 1.0:
            raise ValueError("drop must be in [0, 1)")
        if self.nodes < 1:
            raise ValueError("a ring needs at least one node")
        if self.observers < 1:
            raise ValueError("something has to do the observing")


@dataclass(frozen=True)
class Observation:
    """One machine noticing one thing at one moment.

    Attributes:
        surface: What was seen.
        when: When it happened, in true time. **No node knows this** — each
            reads its own skewed clock instead.
        observer: Which machine saw it.
    """

    surface: int
    when: int
    observer: int


def observations(stream, config: BucketConfig, tempo: int = 100,
                 spread_within: int = 0) -> list[Observation]:
    """Put a stream of occasions into real time, one observation per surface.

    Args:
        stream: `occasions.Occasion` values, whose `when` is an index.
        config: Supplies `observers` and the seed.
        tempo: True time between one occasion and the next.
        spread_within: How far apart the surfaces of ONE occasion may arrive.
            0 means a moment is instantaneous, which is the easy case and the
            positive control. Above 0 a single moment has duration, and a bucket
            narrower than it cannot hold the moment together.

    Returns:
        Observations in true-time order.
    """
    if tempo < 1:
        raise ValueError("occasions must be at least one tick apart")
    if spread_within < 0:
        raise ValueError("spread_within cannot be negative")
    rng = random.Random(config.seed ^ 0x0B0CC)
    out: list[Observation] = []
    for occasion in stream:
        base = occasion.when * tempo
        for surface in occasion.surfaces:
            offset = rng.randint(0, spread_within) if spread_within else 0
            out.append(Observation(surface=surface, when=base + offset,
                                   observer=surface % config.observers))
    out.sort(key=lambda o: (o.when, o.surface))
    return out


class Join:
    """Bucket owners collecting coincidences and writing links out to surfaces.

    The bucket is a **rendezvous, not a container**: it exists long enough to
    notice that two things showed up together, emits the pairing to each
    surface's own owner, and is discarded. Nothing is ever looked up by time.

    Attributes:
        index: The durable accumulator the links land in — the same
            `CoOccurrence` the single-process path uses, so a score taken from it
            is comparable to `g32-01`'s without qualification.
        delivered: Observations that reached their bucket before it flushed.
        lost_late: Observations that arrived after their bucket was gone.
        lost_dropped: Observations that never arrived at all.
        messages: Observation-to-bucket messages sent, `spread` included.
    """

    def __init__(self, config: BucketConfig) -> None:
        self.config = config
        self.index = CoOccurrence()
        self.delivered = 0
        self.lost_late = 0
        self.lost_dropped = 0
        self.messages = 0
        self._ring = Ring(nodes=config.nodes, seed=config.seed)
        rng = random.Random(config.seed)
        #: Fixed per node, because an unsynchronised clock is wrong by a roughly
        #: constant amount over any window short enough to matter here.
        self._offset = [rng.randint(-config.skew, config.skew) if config.skew
                        else 0 for _ in range(config.observers)]
        self._rng = rng
        #: bucket id -> {surface: the CLOCK READING that sent it here}. The
        #: reading travels with the observation because the owner needs it to
        #: decide whether this bucket is the one that counts -- see `_flush`.
        #: Discarded on flush; nothing durable lives at a time key.
        #: Two observations of one surface landing in a single bucket collapse
        #: to the later reading, which is the `too WIDE` failure behaving as it
        #: should: a bucket that cannot separate two moments must not pretend to.
        self._open: dict[int, dict[int, int]] = {}
        #: bucket id -> the true time after which it no longer exists.
        self._closes: dict[int, int] = {}
        #: Every bucket id ever written to, kept for `busiest_share` alone. It is
        #: the one thing here that outlives its bucket, and it is a measurement
        #: rather than part of the mechanism -- no node would keep it.
        self._touched: set[int] = set()

    def bucket_owner(self, bucket: int) -> int:
        """Which node owns a bucket. Computed locally, agreed globally."""
        return self._ring.owner(bucket)

    def run(self, observations: list[Observation]) -> None:
        """Feed every observation through the join, in true-time order."""
        for observation in observations:
            self._advance(observation.when)
            self._offer(observation)
        self._advance(None)

    def _advance(self, now: int | None) -> None:
        """Flush every bucket whose window has closed by `now`."""
        due = [b for b, closes in self._closes.items()
               if now is None or closes < now]
        for bucket in sorted(due):
            self._flush(bucket)

    def _flush(self, bucket: int) -> None:
        present = self._open.pop(bucket, {})
        self._closes.pop(bucket, None)
        width = self.config.width

        # EXACTLY ONE BUCKET COUNTS EACH THING, AND IT DECIDES ALONE.
        #
        # With `spread` on, one observation reaches 2*spread+1 buckets and one
        # pair is witnessed by every bucket both reached. Counting it in each
        # was the first implementation and it is WRONG in a way that looks like
        # it works: the multiplier is how many buckets the two observations
        # share, which is a function of how well the two CLOCKS agree. Two
        # surfaces seen by one machine scored 2*spread+1; the same two seen by
        # machines a bucket apart scored fewer. The counts encode clock skew
        # rather than co-occurrence, and nothing downstream can tell.
        # `test_buckets.ClocksThatDisagree` is where it was caught, at 5x.
        #
        # The fix needs no coordination, which is the only reason it is allowed:
        # a marginal is counted at the observation's OWN bucket, and a pair at
        # the bucket holding the two readings' midpoint. Every bucket that holds
        # the pair computes that midpoint from the same two numbers and gets the
        # same answer, so exactly one of them acts and the others stay silent.
        # An OCCASION is a bucket that counted at least one marginal, so the
        # count matches the single-process one exactly: every observation notes
        # its marginal in exactly one bucket, whatever `spread` is. Counting
        # non-empty buckets instead would multiply it by `2*spread+1`.
        #
        # This is a GLOBAL total and only `ppmi` needs it -- see
        # `CoOccurrence.moment`. It is maintained here so the two paths stay
        # comparable, NOT because a real node could compute it.
        noted = False
        for surface, reading in sorted(present.items()):
            if reading // width == bucket:
                self.index.note(surface)
                noted = True
        if noted:
            self.index.moment()
        for one, other in combinations(sorted(present.items()), 2):
            if ((one[1] + other[1]) // 2) // width == bucket:
                self.index.pair(one[0], other[0])

    def _offer(self, observation: Observation) -> None:
        config = self.config
        if config.drop and self._rng.random() < config.drop:
            self.lost_dropped += 1
            return
        delay = (self._rng.randint(0, config.lateness) if config.lateness
                 else 0)
        arrives = observation.when + delay
        # THE NODE ROUNDS ITS OWN CLOCK, NOT THE TRUE TIME. This is the whole
        # mechanism: two nodes agree because they do the same arithmetic, and
        # they disagree by exactly as much as their clocks do.
        reading = observation.when + self._offset[observation.observer]
        centre = reading // config.width
        landed = False
        for bucket in range(centre - config.spread, centre + config.spread + 1):
            self.messages += 1
            closes = (bucket + 1) * config.width + config.grace
            if arrives > closes:
                continue
            held = self._open.setdefault(bucket, {})
            # WHICH READING SURVIVES WHEN ONE SURFACE ARRIVES TWICE.
            #
            # A bucket holds each surface once -- it cannot tell two moments
            # apart and must not pretend to. But WHICH of the two readings it
            # keeps decides whether the marginal is ever counted, because
            # `_flush` counts a marginal only at the bucket the reading centres
            # on. Keeping the last arrival silently loses it: with `spread` on,
            # a surface present every occasion is written into each bucket by
            # several neighbouring moments, the final writer centres elsewhere,
            # and the marginal is counted NOWHERE. Measured at c(distractor)=1
            # against 8,000.
            #
            # So a reading that belongs to this bucket outranks one that does
            # not. Ties keep the earlier, which is arbitrary and stated so.
            if observation.surface in held:
                if held[observation.surface] // config.width == bucket:
                    continue
            held[observation.surface] = reading
            self._closes[bucket] = closes
            self._touched.add(bucket)
            landed = True
        if landed:
            self.delivered += 1
        else:
            self.lost_late += 1

    @property
    def messages_per_observation(self) -> float:
        """What `spread` costs. 1.0 is plain rounding."""
        total = self.delivered + self.lost_late + self.lost_dropped
        return self.messages / total if total else 0.0

    def busiest_share(self) -> float:
        """Largest share of buckets any one node owned.

        The option record's third objection is that a bucket is a hot spot. This
        measures the DURABLE half of it — whether ownership is even — and does
        not measure the instantaneous half, which is that everything happening at
        one moment routes to one node whatever the ring does.
        """
        seen: dict[int, int] = {}
        for bucket in self._touched:
            owner = self.bucket_owner(bucket)
            seen[owner] = seen.get(owner, 0) + 1
        total = sum(seen.values())
        return max(seen.values()) / total if total else 0.0
