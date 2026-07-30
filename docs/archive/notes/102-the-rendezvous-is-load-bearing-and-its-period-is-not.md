# 102 — the rendezvous is load-bearing and its period is not

2026-07-30. Answers the blocker note 101 named. `tools/prune_period.py`, CLUTRR
`gen_train23_test2to10` test split, 1,146 puzzles, 3 seeds, width 64, beam 4, branches 4.

## The question

Note 101 found the driver-free read path misses `d_max` past depth 7 and located the
remaining factor of two: `owner` sends a hop's look-up and the next hop's follow to the
**same concept**, so a walk that MIGRATED peer to peer would visit one peer per hop
instead of returning to the caller between them. The obstacle it named was **pruning** —
ranking all `width` partial walks against each other is a meeting, and the caller is
where that meeting happens.

So: is the meeting load-bearing, and how often does it have to happen?

## Measured

    prune_every   recovery      sd    vs k=1    reads   vs k=1   migrated   verdict
              1     0.8877  0.0305   +0.0000     36.6    1.00x     750 ms   OVER
              2     0.8860  0.0312   -0.0017     83.9    2.29x     500 ms   fits
              3     0.8831  0.0336   -0.0047    209.7    5.74x     417 ms   fits
              5     0.8773  0.0279   -0.0105  1,398.2   38.25x     350 ms   fits
          never     0.7987  0.0206   -0.0890     36.6    1.00x     300 ms   fits

**The seed spread is 0.0305, which is larger than every period effect except one.** So
the honest reading is not a ranking of periods: `k` of 2, 3 and 5 are indistinguishable
from meeting every hop, and only `never` separates — at about three standard deviations.

    the MEETING buys 0.089 chain recovery
    the PERIOD buys nothing measurable

That is the answer to note 101's blocker. A migrating walk does not need to meet every
hop; it needs to meet.

## Why the read column had to be there

Meeting less often means the population is not capped between meetings, so each walk
keeps its own top `branches` and the beam grows by `branches` per unpruned hop:

    k = 2    2.29x the reads
    k = 3    5.74x
    k = 5   38.25x   for an accuracy difference of one third of a standard deviation

Reporting accuracy against latency alone would have made `k = 5` look like the pick. It
is 38 times the traffic for nothing. **`k = 2` is the choice**: inside `d_max` at depth
10 with recovery unchanged, for slightly over double the reads.

This column exists because note 100's whole lesson is that a one-axis table invites the
wrong conclusion, and I nearly wrote one again.

## What is NOT established, stated plainly

**The latency column is a design estimate, not a measurement.** It prices a migrating
walk — half a round trip per hop because the look-up and next follow co-locate, one full
round trip per meeting — and *that walk is not built*. What exists is the batched caller
path, and `tests/test_prune_period.py` pins its round count at `2 × depth` for every
period, precisely so this arithmetic is not read as already achieved.

Also unestablished: that `owner`'s co-location holds under `route="current"` (it does
not — the routing rule is different), that any of this survives a real link rather than
`SCALE.md`'s assumed 50 ms, and that a domain other than kinship prunes the same way.
The `never` arm is the floor for CLUTRR's branching factor and would look different on a
graph with higher out-degree.

## The order this arrived in, which is the part worth keeping

I nearly skipped the measurement. The local-prune rule reduces to `width` independent
greedy walks, which is close enough to `search(branches=width)` that I argued from
`clutrr_recovery.py`'s existing arms — 0.6588 for search against 0.8735 for beam — that
local pruning would cost 0.21 and the blocker was hard. **The equivalence was wrong.**
`never` costs 0.089, not 0.21, because a beam re-decodes at every step where `search`
commits at the root, and the difference between those two is most of the gap.

An argument from an equivalence I had not checked would have overstated the obstacle by
more than double and sent me looking for a harder fix than the one needed.
