"""The bucket protocol over real sockets, so the messages actually cross a wire.

`bucket_service.BucketService` is a node's share and the five messages that reach
it. Everything about it is transport-agnostic and every test of it runs in one
process, which is deliberate — a protocol that needs a network to be exercised is
a protocol nobody exercises.

This is the transport. It is thin on purpose: **if wrapping a socket around the
service required changing the service, the service was not really separated.**

## What crosses, and what does not

A message is a JSON array — `["SEEN", 41]` — inside `distributed`'s length
prefix. JSON because the payload is a handful of small integers and a verb, and
because a wire format anyone can read with `cat` is worth more here than bytes
saved on a link this project has already measured as latency-bound rather than
bandwidth-bound.

**A row never crosses.** `SEEN` returns one integer. There is no verb that
returns a table, for the reason `bucket_service` gives: moving the ranking to the
asker is the gather amended C1 forbids, and it would be the easier design.

## One connection per message, stated as a cost rather than hidden

Every message opens a socket, sends, reads a reply and closes. That is one extra
round trip per message and it is **not** how a real deployment would do it.

It is chosen because the quantity this exists to establish is *correctness across
a real process boundary*, and connection reuse is a pool with its own failure
modes — a stale socket after a peer restarts, a half-open connection that reads
as a hang. **Timings from this are therefore not comparable to `peer.py`'s**,
which holds its connections, and `g24-01`'s 161 ms a round is the number to quote
for a walk rather than anything measured here.

## What this does NOT duplicate, and what was searched

Searched by capability — socket, serve, peer, wire, frame, protocol — across
`openplexus/`, `tools/`, `tests/` and `testbed/`.

- **`openplexus/distributed.py` owns the FRAMING** and `send`/`receive` are
  imported rather than re-written. A second length-prefix implementation is two
  chances to disagree about a header.
- **`openplexus/peer.py` serves the SUPERPOSED STORE** — `read(concept, key)`
  against a `d x d` matrix, with a fingerprint handshake because a diverged peer
  there answers with zeros that decode to a real token. This serves counting
  questions against a sparse table, where a wrong answer is a wrong integer and
  the `_require` refusal in `bucket_service` is the equivalent guard. Neither can
  serve the other's questions.
- **`openplexus/bucket_service.py` owns every decision**; this file makes none.
  It does not know what a bucket is, when one closes, or which node owns
  anything — it moves tuples.
- **`openplexus/node_main.py`** is where a container starts one of these, and
  this adds no launcher.
"""

from __future__ import annotations

import json
import socket
import threading

from openplexus.bucket_service import BucketService
from openplexus.distributed import receive, send


def ask(host: str, port: int, message: tuple, timeout: float = 10.0):
    """Send one message to a peer and return its reply.

    Raises rather than returning a default on any failure. **A message that did
    not arrive must not look like a zero** — that is the missing-marginal failure
    `bucket_service.rank` refuses, one layer down.
    """
    with socket.create_connection((host, port), timeout=timeout) as sock:
        send(sock, json.dumps(list(message)).encode("utf-8"))
        return json.loads(receive(sock).decode("utf-8"))


class BucketPeer:
    """A `BucketService` reachable over TCP, which forwards what it emits.

    Attributes:
        service: The node this serves.
        port: The bound port. Read it rather than assuming — binding to 0 asks
            the OS for a free one, which is what the tests do.
        forwarded: Messages this peer has pushed to other peers, successfully.
            The write-side traffic count, where `bucket_service` counts the read
            side.
        failures: `(destination, message, reason)` for every push that could not
            be delivered. **Read this before trusting any count taken from a
            run** — a non-empty list means writes were lost, and lost writes look
            like a weaker signal rather than like a fault.
    """

    def __init__(self, service: BucketService,
                 addresses: dict[int, tuple[str, int]],
                 host: str = "127.0.0.1", port: int = 0) -> None:
        self.service = service
        self.addresses = dict(addresses)
        self.forwarded = 0
        self.failures: list[tuple[int, tuple, str]] = []
        self._lock = threading.Lock()
        self._listener = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._listener.bind((host, port))
        self._listener.listen(64)
        self.port = self._listener.getsockname()[1]
        self._running = False
        self._thread: threading.Thread | None = None

    def start(self) -> "BucketPeer":
        self._running = True
        self._thread = threading.Thread(target=self._serve, daemon=True)
        self._thread.start()
        return self

    def close(self) -> None:
        self._running = False
        try:
            self._listener.close()
        except OSError:
            pass
        if self._thread is not None:
            self._thread.join(timeout=2.0)

    def _serve(self) -> None:
        while self._running:
            try:
                connection, _ = self._listener.accept()
            except OSError:
                return
            threading.Thread(target=self._one, args=(connection,),
                             daemon=True).start()

    def _one(self, connection: socket.socket) -> None:
        with connection:
            try:
                message = tuple(json.loads(receive(connection).decode("utf-8")))
            except (ConnectionError, ValueError):
                return
            # The service is not thread-safe and does not need to be: one lock
            # around the whole handling keeps a flush's outbox from interleaving
            # with another connection's. Contention is irrelevant at the message
            # rates this is for, and a lock-free version would be a second
            # concurrency design nobody asked for.
            with self._lock:
                try:
                    reply = self.service.handle(message)
                except ValueError as refused:
                    reply = {"refused": str(refused)}
                outbox = self.service.take()
            send(connection, json.dumps(reply).encode("utf-8"))
        for destination, forward in outbox:
            self._forward(destination, forward)

    def _forward(self, destination: int, message: tuple) -> None:
        """Push one emitted message to the node that owns its key.

        **A failure is RECORDED, because raising here would be swallowing.**
        This runs on a server thread with no caller to catch anything: an
        exception prints a traceback, the message is lost anyway, and the run
        continues looking healthy. The first version of this said *"raised, not
        swallowed"* and was wrong in exactly that way — the traceback appeared in
        the test log and the tests passed.

        A dropped `LINK` is a count that silently never happened, and `g33-01`
        measured what missing writes do to a recovery: they read as a weaker
        signal rather than as a fault. So the only useful thing a thread can do
        is leave evidence, and `failures` is that evidence.
        """
        host, port = self.addresses[destination]
        try:
            ask(host, port, message)
        except OSError as unreachable:
            self.failures.append((destination, message, str(unreachable)))
            return
        self.forwarded += 1
