"""Feed a stream of occasions through bucket peers and check what they hold.

The driver side of `OPENPLEXUS_MODE=bucket`. It observes, flushes, and then asks
every surface's owner for its marginal — and compares the lot against the same
stream run in one process.

**That comparison is the whole point.** Note 014 established the property for the
slice path and it is the same here: a network of containers must agree with the
single-process model EXACTLY, or every later number is measuring the harness.

## Why the driver is not a node

It observes and it asks. It owns nothing, stores nothing, and holds no part of
the answer — every count lives at a peer, and the driver reads them back one at a
time. **A driver that accumulated the counts itself would be the gather amended
C1 forbids**, and it would agree with one process for the wrong reason.

The stream is generated identically on both sides from `(seed, config)`, so
nothing about it crosses the wire except one observation at a time.

## What this does NOT duplicate

- **`tools/peer_walk_timing.py`** is the driver for `MODE=peer`: it times a
  walk over the superposed store. This checks counts on the sparse table. Same
  role, different question, and neither can answer the other's.
- **`openplexus/bucket_peer.py`** owns the wire; `ask` is imported.
- **`openplexus/bucket_service.py`** owns the bucket arithmetic; this decides
  only *when* a bucket has closed, which is the one thing an observer knows and
  a bucket owner does not.
- **`testbed/run.py`** owns the containers and the impairment.
"""

from __future__ import annotations

import argparse
import json
import pathlib
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus.bucket_peer import ask  # noqa: E402
from openplexus.buckets import BucketConfig, observations  # noqa: E402
from openplexus.grounding import (STATISTICS,  # noqa: E402
                                  CoOccurrence, equivalence_classes)
from openplexus.ownership import Ring  # noqa: E402
from openplexus.tasks.occasions import OccasionConfig, generate  # noqa: E402


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--peers", required=True,
                        help="comma-separated host:port, in node order")
    parser.add_argument("--width", type=int, default=50, help="bucket width")
    parser.add_argument("--tempo", type=int, default=100)
    parser.add_argument("--concepts", type=int, default=6)
    parser.add_argument("--occasions", type=int, default=60)
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument("--walk", action="store_true",
                        help="after loading, WALK from every surface and time "
                             "it. The read path, which the write path does not "
                             "exercise")
    args = parser.parse_args(argv)

    addresses = []
    for entry in args.peers.split(","):
        host, port = entry.rsplit(":", 1)
        addresses.append((host, int(port)))
    nodes = len(addresses)
    ring = Ring(nodes=nodes, seed=args.seed)

    occasions = OccasionConfig(concepts=args.concepts, surfaces=3,
                               presence=0.7, noise=2, distractors=1,
                               occasions=args.occasions, seed=args.seed)
    stream = generate(occasions)
    buckets = BucketConfig(width=args.width, nodes=nodes, observers=3,
                           seed=args.seed)

    # PROGRESS TO STDERR. This tool's stdout is a JSON document and a caller
    # pipes it to a file -- `g12-04` produced six artifacts that were not valid
    # JSON because two progress lines sat above the object.
    print(f"driving {len(stream)} occasions through {nodes} peers",
          file=sys.stderr, flush=True)

    started = time.time()
    closes: dict[int, int] = {}
    sent = 0

    def at(key: int) -> tuple[str, int]:
        return addresses[ring.owner(key)]

    def advance(now: int | None) -> int:
        flushed = 0
        due = [b for b, shut in closes.items() if now is None or shut < now]
        for bucket in sorted(due):
            closes.pop(bucket)
            ask(*at(bucket), ("FLUSH", bucket))
            flushed += 1
        return flushed

    flushes = 0
    for observation in observations(stream, buckets, tempo=args.tempo):
        flushes += advance(observation.when)
        bucket = observation.when // args.width
        closes[bucket] = (bucket + 1) * args.width
        ask(*at(bucket), ("OBSERVE", bucket, observation.surface,
                          observation.when))
        sent += 1
    flushes += advance(None)

    single = CoOccurrence()
    for occasion in stream:
        single.observe(occasion.surfaces)

    mismatches = []
    for surface in single.surfaces():
        held = ask(*at(surface), ("SEEN", surface))
        if held != single.seen(surface):
            mismatches.append({"surface": surface, "held": held,
                               "expected": single.seen(surface)})

    walk = None
    if args.walk:
        # THE READ PATH. Every `RANK` is answered AT the owner, which fetches
        # `count(y)` from each candidate's owner itself -- so the row never
        # travels and the driver never gathers. Mutuality needs the other end's
        # ranking too, which is a second `RANK` and a second set of fetches.
        began = time.time()
        asked, reached = 0, []
        recovered: dict[int, frozenset[int]] = {}
        for surface in single.surfaces():
            ranked = {}

            def rank_once(node: int):
                nonlocal asked
                if node not in ranked:
                    asked += 1
                    got = ask(*at(node), ("RANK", node, None))
                    ranked[node] = got if isinstance(got, list) else []
                return ranked[node]

            found, frontier = {surface}, [surface]
            while frontier:
                here = frontier.pop()
                for other in rank_once(here):
                    if here not in rank_once(other):
                        continue
                    if other not in found:
                        found.add(other)
                        frontier.append(other)
            reached.append(len(found))
            recovered[surface] = frozenset(found)

        # THE READ PATH GETS THE SAME IDENTITY CHECK AS THE WRITE PATH. Timing a
        # walk that returns the wrong classes would be timing the harness. The
        # single-process comparison uses the same statistic and the same derived
        # bound, so a difference is the network and nothing else.
        expected = equivalence_classes(single, STATISTICS["conditional"], None)
        differing = [s for s in single.surfaces()
                     if recovered.get(s) != expected.get(s)]
        walk = {
            "walks": len(reached),
            "rank_messages": asked,
            "rank_messages_per_walk": round(asked / max(len(reached), 1), 1),
            "mean_class_size": round(sum(reached) / max(len(reached), 1), 2),
            "classes_differing_from_one_process": len(differing),
            "walk_agrees_with_one_process": not differing,
            "seconds": round(time.time() - began, 2),
        }
        print(f"walked from {len(reached)} surfaces", file=sys.stderr,
              flush=True)

    result = {
        # WRITTEN FROM WHAT ACTUALLY RAN, not from what was asked for. Rule 11b:
        # only the data says what happened, and a run identified by its
        # directory name is a run nobody can verify.
        "condition": (f"nodes={nodes} width={args.width} tempo={args.tempo} "
                      f"concepts={args.concepts} occasions={len(stream)} "
                      f"seed={args.seed}"),
        "nodes": nodes,
        "observations_sent": sent,
        "flushes": flushes,
        "surfaces_checked": len(single.surfaces()),
        "mismatches": len(mismatches),
        "detail": mismatches[:10],
        "agrees_with_one_process": not mismatches,
        "seconds": round(time.time() - started, 2),
        "walk": walk,
    }
    print(json.dumps(result, indent=1))
    return 0 if not mismatches else 1


if __name__ == "__main__":
    raise SystemExit(main())
