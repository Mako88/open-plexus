"""Count a walk's sequential ROUND TRIPS, which is the quantity `d_max` bounds.

## Why this is counted rather than reasoned about

Note 100 corrected two of my own notes for measuring the wrong axis: 093 counted
messages per read, 094 timed a walk on loopback where an RTT is 0.05 ms. Both are true
numbers about quantities `d_max` is not a deadline on. `d_max` bounds a WALK, so the
axis is how many round trips are strung end to end -- and the way to be sure of that
number is to make the reader count them, not to multiply reads by anything.

## What it found, and it is not what note 100 predicted

Note 100 said batching turns `reads x RTT` into `depth x RTT`. **That was wrong by a
factor of two**, and the reason is visible only once the rounds are counted:

    a hop is TWO dependent rounds, not one

        FOLLOW    read (entity, relation)  ->  the next entity, after a decode
        LOOK UP   read (FACT, that entity) ->  its outgoing relations

The look-up's key contains what the follow decoded to, so the two cannot share a
request however much is batched. Within each of them the `width` reads ARE independent,
which is what batching recovers.

So rounds are `2 * depth`, and at note 100's numbers:

    depth   reads   rounds   at 50 ms RTT   against d_max 640 ms
        3      21        6          300 ms   fits
        5      37       10          500 ms   fits, 78%
        8      61       16          800 ms   OVER
       10      77       20        1,000 ms   OVER by 1.6x

**Batching is necessary and not sufficient.** It took depth 10 from 3,850 ms to
1,000 ms, and 1,000 ms still misses the deadline. The remaining factor of two needs the
follow and the look-up to happen without a return trip between them, which means the
decode moves to the peer -- the same move the retrieval already made when the driver
went away (`peer.py`: *"whoever holds the binding decides how it is read"*).

Run with `--rtt` to price a link this machine cannot produce.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from openplexus.keys import PairKeys  # noqa: E402
from openplexus.retrieval import SuperposedRead  # noqa: E402
from openplexus.search import beam  # noqa: E402

#: `d_max` from GOALS' C2, in milliseconds. A walk that exceeds it is not slow, it is
#: outside the constraint the architecture is built to hold.
D_MAX_MS = 640.0


class Counting:
    """A reader that answers from one matrix and counts rounds and reads separately.

    **Separately on purpose.** They are the two quantities note 100 is about, and a
    counter that conflated them would reproduce the mistake it exists to measure.
    """

    def __init__(self, matrix, retrieval, keys):
        self.matrix = matrix
        self.retrieval = retrieval
        self.keys = keys
        self.rounds = 0
        self.reads = 0
        self.widest = 0
        self.concepts: list[list[int]] = []
        self.many = self._many

    def __call__(self, previous: int, token: int) -> np.ndarray:
        return self._many([(previous, token)])[0]

    def _many(self, pairs) -> list[np.ndarray]:
        self.rounds += 1
        self.reads += len(pairs)
        self.widest = max(self.widest, len(pairs))
        self.concepts.append([self.keys.owner(previous, token)
                              for previous, token in pairs])
        return [self.retrieval.read(self.matrix, self.keys.pair(previous, token))
                for previous, token in pairs]

    def colocated(self) -> tuple[int, int]:
        """How many rounds ask a peer the PREVIOUS round already spoke to.

        Measured because it decides whether the remaining factor of two is reachable.
        `PairKeys.owner` under `route="first-concept"` sends `(FACT, landed)` to
        `landed` and `(landed, relation)` to `landed` as well -- the look-up at one hop
        and the FOLLOW at the next are the same concept, so they are the same peer. A
        walk that migrated would visit one peer per hop instead of returning to the
        caller between the two.

        Counted over the walk that actually ran rather than derived from the routing
        rule, because the rule holds for `route="first-concept"` and not for
        `route="current"`, and which is configured is not this file's business.
        """
        hits = sum(bool(set(before) & set(after)) for before, after
                   in zip(self.concepts, self.concepts[1:]))
        return hits, max(len(self.concepts) - 1, 0)


def fixture(depth: int, width: int, seed: int = 0):
    """A chain long enough to walk `depth` hops, with a branch at every entity.

    Branches matter: out-degree 1 makes `_top` return one candidate and the beam
    narrows to a single partial walk, so the batch would be one read wide and the
    measurement would flatter itself.
    """
    entities = depth + 2
    relations = 4
    vocab = 1 + entities + relations
    fact = 0
    rng = np.random.default_rng(seed)
    values = rng.normal(0.0, 1.0, (vocab, 8))
    values /= np.linalg.norm(values, axis=1, keepdims=True)
    keys = PairKeys(seed=1, spread=1.0 / np.sqrt(8), width=8, start=vocab,
                    route="first-concept", markers=frozenset({fact}))
    matrix = np.zeros((8, 8))
    for entity in range(1, entities):
        for offset in range(min(relations, width)):
            relation = 1 + entities + offset
            landed = 1 + (entity + offset) % (entities - 1)
            for previous, token, value in ((fact, entity, relation),
                                           (entity, relation, landed)):
                matrix += np.outer(values[value], keys.pair(previous, token))
    return matrix, keys, values, fact


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--depths", type=int, nargs="+",
                        default=[2, 3, 5, 8, 10])
    parser.add_argument("--width", type=int, default=4)
    parser.add_argument("--branches", type=int, default=4)
    parser.add_argument("--rtt", type=float, default=50.0,
                        help="one round trip in ms. SCALE.md's assumption is 50")
    args = parser.parse_args()

    print(f"beam width {args.width}, branches {args.branches}, "
          f"RTT {args.rtt:.0f} ms, d_max {D_MAX_MS:.0f} ms\n")
    print(f"{'depth':>6s} {'reads':>7s} {'rounds':>7s} {'widest':>7s} "
          f"{'batched':>9s} {'one-by-one':>11s}  {'reuse':>6s} verdict")
    failed = 0
    for depth in args.depths:
        matrix, keys, values, fact = fixture(depth, args.branches)
        counting = Counting(matrix, SuperposedRead(), keys)
        beam(None, SuperposedRead(), keys, values, fact, 1, values[2], depth,
             width=args.width, branches=args.branches, reader=counting)
        batched = counting.rounds * args.rtt
        serial = counting.reads * args.rtt
        verdict = "fits" if batched <= D_MAX_MS else "OVER d_max"
        failed += batched > D_MAX_MS
        hits, pairs = counting.colocated()
        print(f"{depth:6d} {counting.reads:7d} {counting.rounds:7d} "
              f"{counting.widest:7d} {batched:8.0f}ms {serial:10.0f}ms  "
              f"{hits}/{pairs:<4d} {verdict}")
        # The identity the whole file rests on. If a hop ever costs one round, this
        # is where it would show, and it would mean the look-up stopped depending on
        # the follow -- a real change, not a speedup to accept quietly.
        assert counting.rounds == 2 * depth, (
            f"depth {depth} took {counting.rounds} rounds, not {2 * depth}. The "
            f"follow/look-up dependency changed shape; this file's arithmetic and "
            f"note 101 are both about 2*depth and need rereading.")
    print(f"\n{failed} of {len(args.depths)} depths miss d_max even batched. "
          f"Batching is necessary and not sufficient.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
