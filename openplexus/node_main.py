"""Start ONE node and serve until told to stop. The entry point C1 needs.

**`bucket_peer`, `federated` and `deployment` all had no caller for one reason:
this file was not carried over in the restructure.** They are written and tested;
nothing launched one. `testbed/driver.py` still names this module in a comment,
which is how the gap was found.

What a node is here: it owns a share of the buckets, answers reads over a socket
for the ones it owns, and forwards the rest to whoever does. Nothing about that
is emulated — the peers are separate processes talking over TCP, which is the
whole point of C1 and the reason a single-process test cannot stand in for it.

**Peers are supplied with explicit ports and are known before the run starts.**
That is deliberate and it is the durable half: `BucketPeer` binds port 0 by
default and only learns its own port after construction, so any orchestration
that discovers ports has to hand them round afterwards. Choosing a protocol for
that would be inventing the part a later phase is meant to design, so this takes
addresses as an argument and prints its own on stdout for whatever does.

    python -m openplexus.node_main --node 0 --nodes 3 --port 8100 \\
        --peers 0=127.0.0.1:8100,1=127.0.0.1:8101,2=127.0.0.1:8102

Prints one machine-readable line once it is listening, then blocks:

    NODE 0 LISTENING 127.0.0.1:8100

so a launcher can wait for readiness rather than sleeping and hoping.
"""

from __future__ import annotations

import argparse
import signal
import sys
import threading

from openplexus.bucket_peer import BucketPeer
from openplexus.bucket_service import BucketService
from openplexus.buckets import BucketConfig


def addresses(text: str) -> dict[int, tuple[str, int]]:
    """`0=host:port,1=host:port` into what `Link` wants.

    Rejects a malformed entry rather than skipping it. A node silently missing
    one peer answers every read it owns and quietly fails the rest, which reads
    as a partition rather than as a typo.
    """
    out: dict[int, tuple[str, int]] = {}
    for entry in filter(None, (part.strip() for part in text.split(","))):
        node, _, where = entry.partition("=")
        host, _, port = where.rpartition(":")
        if not node.strip().isdigit() or not host or not port.isdigit():
            raise argparse.ArgumentTypeError(
                f"peer {entry!r} is not node=host:port")
        out[int(node)] = (host, int(port))
    if not out:
        raise argparse.ArgumentTypeError("no peers given")
    return out


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--node", type=int, required=True)
    parser.add_argument("--nodes", type=int, required=True)
    parser.add_argument("--peers", type=addresses, required=True)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=0)
    parser.add_argument("--width", type=int, default=8)
    parser.add_argument("--seed", type=int, default=0)
    args = parser.parse_args(argv)

    if args.node not in args.peers:
        parser.error(f"node {args.node} is not in its own peer list, so no "
                     "other node can reach it")
    if len(args.peers) != args.nodes:
        parser.error(f"{len(args.peers)} peer(s) for a network of {args.nodes}")

    config = BucketConfig(width=args.width, nodes=args.nodes, seed=args.seed)
    service = BucketService(args.node, config)
    peer = BucketPeer(service, args.peers, host=args.host, port=args.port)
    peer.start()

    # ONE PARSEABLE LINE, FLUSHED. A launcher that sleeps and hopes is a launcher
    # that reports a startup race as a failed read.
    print(f"NODE {args.node} LISTENING {args.host}:{peer.port}", flush=True)

    stop = threading.Event()
    for name in ("SIGINT", "SIGTERM"):
        if hasattr(signal, name):
            signal.signal(getattr(signal, name), lambda *_: stop.set())
    try:
        stop.wait()
    finally:
        # A NODE THAT CANNOT BE SHUT DOWN IS ONE THAT KEEPS ANSWERING AFTER IT
        # DEPARTS, which is what C3 measures. `BucketPeer.close` is what makes
        # that true and it is not optional here.
        peer.close()
    print(f"NODE {args.node} STOPPED forwarded {peer.forwarded} "
          f"fetched {peer.fetched} failures {len(peer.failures)}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
