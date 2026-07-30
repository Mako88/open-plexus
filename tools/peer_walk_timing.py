"""Time a driver-free walk across real peers, and check it against the arithmetic.

## Why this exists

`note 101` established that a hop is **two dependent round trips** — follow, then
look up what the follow decoded to — so a walk costs `2 * depth` rounds and depth
10 lands at 1,000 ms against `d_max` 640 ms.

**That number is counted, not measured.** `tools/walk_rounds.py` counts rounds and
multiplies by an RTT you supply; every peer timing in the project is loopback,
priced at an assumed 50 ms. `DECISIONS.md` says so: *"measured in: loopback only,
priced at an assumed 50 ms RTT."*

This closes the gap by running the walk and timing it. Two modes:

    --inprocess     peers as threads. A SELF-CHECK, not a network measurement:
                    it verifies the harness agrees with the round arithmetic
                    before anything is spent on containers
    --peers a:1,b:2 peers already running as `node_main` in peer mode, which is
                    where `tc netem` can impose a real link

## What this does not duplicate

Searched before writing, by capability rather than by name, across `openplexus/`,
`tools/`, `tests/`, `testbed/` and `docs/archive/`:

    testbed/run.py           stands up containers under `tc netem`. THE netem
                             runner, verified on Docker Desktop and on Actions.
                             It drives the DIMENSION path, and pointing it at
                             peers is an extension of it, not a rival
    tools/cluster_driver.py  drives the dimension-partitioned cluster
    tools/walk_rounds.py     COUNTS rounds and multiplies by a supplied --rtt.
                             Analytic. This measures what that predicts, and
                             the two are meant to be compared
    peer.reader_for          already adapts RemoteConcepts to `beam`
    RemoteConcepts.rounds    already counts rounds
    tests/test_peer_reads.py already asserts a walk costs 2*depth rounds

**So nothing here re-implements a walk, a reader or a round counter.** What did
not exist is the timing loop and the refusal below. `notes 094` and `101` both
say in their own words that the netem harness *"has never been pointed at the
peer path"*, which is the gap this fills.

## What the number means, and what it does not

The reported quantity is **milliseconds per round**, which is the measured RTT.
Wall-clock alone is not comparable across depths; RTT is, and it is the thing
`d_max` is denominated in.

**The walk's ANSWER is not scored here and must not be.** Latency is a property
of the round structure, and `remote.rounds == 2 * depth` is the guard that the
walk actually traversed rather than short-circuiting. A walk that ends somewhere
useless costs the same rounds as one that does not — which is why this tool
reports rounds beside every timing and refuses a result when they disagree.

That refusal is the point. `CLAUDE.md`: a caveat printed next to a number does
not attach to the number.
"""

from __future__ import annotations

import argparse
import statistics
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from openplexus.node_main import derive, populate, scaffold_facts  # noqa: E402
from openplexus.peer import ConceptPeer, RemoteConcepts, reader_for  # noqa: E402
from openplexus.retrieval import SuperposedRead  # noqa: E402
from openplexus.search import beam  # noqa: E402

FACT = 0
#: C2's bound, from decision 128: 3x a measured p99 on the worst simulated link.
#: A FLOOR from six simulated links, not a universal constant -- a real WAN
#: raises it, which is the direction that makes a miss worse.
D_MAX_MS = 640.0


def _spawn(nodes, width, vocab, seed, ring_seed, replicas, facts):
    """Peers as in-process threads. Returns `(peers, addresses)`."""
    peers = []
    for index in range(nodes):
        store, keys, _ = populate(index, nodes, width, vocab, seed,
                                  ring_seed, replicas, facts)
        peers.append(ConceptPeer(store, keys, peers=nodes,
                                 seed=ring_seed).start())
    return peers, {i: ("127.0.0.1", p.port) for i, p in enumerate(peers)}


def _time_one(remote, keys, values, depth, width, branches):
    """One walk. Returns `(milliseconds, rounds)`."""
    before = remote.rounds
    started = time.perf_counter()
    beam(None, SuperposedRead(), keys, values, FACT, 2, values[5], depth,
         width=width, branches=branches,
         reader=reader_for(remote, keys))
    elapsed = (time.perf_counter() - started) * 1000.0
    return elapsed, remote.rounds - before


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inprocess", action="store_true",
                        help="spawn peers as threads; a self-check, not a link")
    parser.add_argument("--peers", type=str, default=None,
                        help="comma-separated host:port of running peers")
    parser.add_argument("--depths", type=int, nargs="+", default=[1, 2, 3, 5])
    parser.add_argument("--repeats", type=int, default=5,
                        help="walks per depth; one walk is an anecdote")
    parser.add_argument("--nodes", type=int, default=4)
    parser.add_argument("--width", type=int, default=64)
    parser.add_argument("--vocab", type=int, default=40)
    parser.add_argument("--seed", type=int, default=5)
    parser.add_argument("--ring-seed", type=int, default=0)
    parser.add_argument("--replicas", type=int, default=2)
    parser.add_argument("--facts", type=int, default=24)
    parser.add_argument("--beam-width", type=int, default=2)
    parser.add_argument("--branches", type=int, default=2)
    args = parser.parse_args()

    if not args.inprocess and not args.peers:
        print("give --inprocess or --peers host:port,host:port", file=sys.stderr)
        return 2

    facts = scaffold_facts(args.facts, args.vocab)
    values, keys = derive(args.width, args.vocab, args.seed)

    spawned = []
    if args.inprocess:
        spawned, addresses = _spawn(args.nodes, args.width, args.vocab,
                                    args.seed, args.ring_seed, args.replicas,
                                    facts)
        print(f"{args.nodes} peers in process -- SELF-CHECK, not a network "
              f"measurement")
    else:
        addresses = {}
        for index, entry in enumerate(args.peers.split(",")):
            host, _, port = entry.rpartition(":")
            addresses[index] = (host, int(port))
        print(f"{len(addresses)} peers over the wire")

    remote = RemoteConcepts(addresses, args.width, keys,
                            seed=args.ring_seed, replicas=args.replicas)
    failures = 0
    try:
        print(f"\n{'depth':>6}{'rounds':>9}{'expected':>10}"
              f"{'wall ms':>12}{'ms/round':>11}{'vs d_max':>12}")
        for depth in args.depths:
            timings, rounds_seen = [], set()
            for _ in range(args.repeats):
                elapsed, rounds = _time_one(remote, keys, values, depth,
                                            args.beam_width, args.branches)
                timings.append(elapsed)
                rounds_seen.add(rounds)
            wall = statistics.median(timings)
            rounds = sorted(rounds_seen)[0] if len(rounds_seen) == 1 else -1
            expected = 2 * depth
            if rounds != expected:
                # REFUSED rather than annotated. A walk that did not take the
                # rounds the arithmetic says it takes is not a slower walk, it is
                # a different computation, and dividing by its round count
                # produces an RTT that describes nothing.
                print(f"{depth:>6}{str(sorted(rounds_seen)):>9}{expected:>10}"
                      f"{'--':>12}{'REFUSED':>11}{'--':>12}")
                failures += 1
                continue
            per_round = wall / rounds
            verdict = "fits" if wall <= D_MAX_MS else "OVER"
            print(f"{depth:>6}{rounds:>9}{expected:>10}{wall:>12.1f}"
                  f"{per_round:>11.2f}{verdict:>12}")
    finally:
        remote.close()
        for peer in spawned:
            peer.close()

    if failures:
        print(f"\n{failures} depth(s) REFUSED: rounds did not match 2*depth, so "
              f"no per-round time is reported for them.")
        print("Rounds varying across repeats means the walk is not doing the "
              "same work each time -- pruning, or a peer dropping.")
        return 1
    print(f"\nd_max is {D_MAX_MS:.0f} ms (decision 128), a FLOOR from six "
          f"simulated links. A real WAN raises it.")
    if args.inprocess:
        print("These are loopback numbers and are NOT the measurement this tool "
              "exists for -- they only show the harness agrees with 2*depth.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
