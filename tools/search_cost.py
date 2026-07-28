"""What does a search branch cost on the wire, and where does it stop fitting?

`openplexus/search.py` adds a capability decision 108 named as missing, and the
capability is not free. Search does not add a new KIND of communication -- every
step is a token id and a dot product, and `PairKeys.pair` rebuilds a key from two
integers rather than looking one up. What it adds is MORE of the crossing the
readout already makes.

Asked by arithmetic, the way `slot_cost.py` and `gate_cost.py` ask it, because
the number is decision-relevant before any sweep: **G4 currently passes on one
seed with training traffic never measured at all**, and search is the first
mechanism that multiplies traffic rather than merely adding to it.

## What crosses the network, and what does not

A retrieval is `readable @ key`. Each node holds a slice of the store and
computes its own part, so **the read itself is local**. What crosses is the
DECODE: turning a retrieved value vector into a token id needs scores pooled
across groups, which is the same `parts.sum(0)` crossing the readout already
makes.

So the unit of cost is **one pooled decode**, and the question is how many of
them a walk performs.

    plain traversal, depth d        d       decodes
    search, b branches, depth d     b(2d-1) decodes

A walk of depth `d` performs `d - 1` follow steps and `d - 1` look-up steps plus
the first read and the endpoint, and every one of those that must yield a TOKEN
ID costs a decode. The first read is shared across branches -- it is what
produced the candidates -- so it is counted once rather than `b` times.

## Why this is a traffic multiplier and NOT a C1 violation

Amended C1 asks whether progress stalls when one participant is slow or gone, not
how many bytes moved. **Nothing in a search waits on a barrier**: each decode is
the same pooled vote the readout already performs, branches are independent of
one another, and a missing node degrades a vote rather than blocking it.

What it is NOT allowed to hide behind that: **a global all-reduce is still out
even at twelve bytes**, and a decode IS a collective. Search does not introduce
that collective -- the readout already requires it, and note 009 section 4 has
carried it as an outstanding violation since before any of this -- but search
makes it `b(2d-1)/d` times more frequent, so it raises the stakes on the item
STATE.md lists as costing a reading rather than a run.

## The measured constants this uses

From `openplexus/distributed.py`, recorded in STATE.md:

    token broadcast to all nodes        5 bytes
    each node's reply, combine="vote"   8 bytes

A node's readout spans the whole vocabulary from its own slice, so its argmax is
a complete opinion rather than a fragment, which is why 8 bytes is legitimate
rather than lossy.
"""

from __future__ import annotations

import argparse

#: Bytes on the wire per pooled decode, from `distributed.py`.
BROADCAST_BYTES = 5
REPLY_BYTES = 8

#: A consumer uplink. Upload binds -- note 004 measured it 5-20x slower than
#: download on consumer connections, which is why the budget is stated against
#: it rather than against a nominal line rate.
DEFAULT_UPLINK_MBPS = 10.0


def decodes(branches: int, depth: int) -> int:
    """Pooled decodes one answered position costs.

    The first read is shared: it produced the candidates, so it happens once
    however many branches follow it.
    """
    if branches < 1 or depth < 1:
        raise ValueError("branches and depth are both at least 1")
    # Per branch: (depth - 1) follows + (depth - 1) look-ups + 1 endpoint.
    per_branch = 2 * (depth - 1) + 1
    return 1 + branches * per_branch


def bytes_per_position(nodes: int, branches: int, depth: int) -> int:
    """Wire bytes for one answered position across the whole network."""
    return decodes(branches, depth) * (BROADCAST_BYTES + nodes * REPLY_BYTES)


def positions_per_second(nodes: int, branches: int, depth: int,
                         uplink_mbps: float) -> float:
    """How many answered positions a node's uplink supports.

    Charged per NODE: each node sends its own reply per decode, so the binding
    quantity is one node's outbound traffic rather than the aggregate.
    """
    per_node = decodes(branches, depth) * REPLY_BYTES
    return (uplink_mbps * 1e6 / 8.0) / per_node


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--nodes", type=int, default=1024)
    parser.add_argument("--uplink", type=float, default=DEFAULT_UPLINK_MBPS)
    parser.add_argument("--depth", type=int, default=2)
    args = parser.parse_args()

    print(f"nodes {args.nodes}, depth {args.depth}, "
          f"uplink {args.uplink:g} Mbps per node\n")
    print(f"{'branches':>9}  {'decodes':>8}  {'bytes/position':>15}  "
          f"{'x greedy':>9}  {'positions/s':>12}")

    base = decodes(1, args.depth)
    for branches in (1, 2, 4, 8, 16):
        count = decodes(branches, args.depth)
        total = bytes_per_position(args.nodes, branches, args.depth)
        rate = positions_per_second(args.nodes, branches, args.depth,
                                    args.uplink)
        print(f"{branches:>9}  {count:>8}  {total:>15,}  "
              f"{count / base:>8.1f}x  {rate:>12,.0f}")

    print("\nDEPTH is the harsher axis, because a walk costs 2d-1 decodes:")
    print(f"{'depth':>9}  {'b=1':>8}  {'b=4':>8}  {'ratio':>8}")
    for depth in (2, 3, 4, 5):
        greedy, wide = decodes(1, depth), decodes(4, depth)
        print(f"{depth:>9}  {greedy:>8}  {wide:>8}  {wide / greedy:>7.1f}x")

    print("\nWHAT THIS DOES NOT SAY. Every figure is the DECODE traffic, which "
          "is\nthe crossing the readout already makes -- search makes it more "
          "frequent\nrather than introducing it. The pooled decode is a "
          "collective, and note 009\nsection 4 has carried that as an "
          "outstanding C1 item since before search\nexisted. This raises the "
          "stakes on it; it does not create it.\n\nNor is any of it measured. "
          "It is arithmetic over two constants read from\n`distributed.py`, and "
          "the model has never run a search on more than one\nmachine.")


if __name__ == "__main__":
    main()
