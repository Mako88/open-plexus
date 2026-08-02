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

**`RANK` is handled HERE rather than in the service, and that placement is the
point.** Ranking needs `count(y)` from every candidate's owner, so it needs a
network — and `bucket_service` is deliberately transport-free, taking those
marginals as an argument. So this peer collects them with `SEEN` messages and
hands the service a dict. The decision stays in the service; only the fetching
lives here.

A caller therefore asks `RANK` and gets **a short list of surface ids**, never a
row. The peer pays one `SEEN` per candidate on the caller's behalf, which is
`g33-03`'s measured cost arriving on a real link.

## Connections are HELD, and the failure mode that bought is handled

`Link` opens one connection per destination and reuses it. The first version
opened one per message, which `g35-01` and `g35-03` measured at **96x** and
**142x** under a 40 ms link — every message paying a connection setup on top of
its request.

**The reason it was one-per-message first was honest and is now paid for**: a
pool has its own failure mode, a socket left stale by a restarted peer, and a
stale socket does not announce itself — it fails on the next write, or worse
reads as a hang. So `Link.ask` **retries exactly once on a fresh connection** and
raises on the second failure. Once distinguishes *"the socket went stale"* from
*"the peer is gone"*; retrying further would turn a departure into a hang, which
is the thing C3 says must not happen.

`peer.py` holds connections the same way and is the precedent rather than a thing
to extend: its cache carries a fingerprint handshake, because a diverged peer
there answers with zeros that decode to a real token. The equivalent guard here
is `bucket_service`'s ownership refusal, which travels in the reply.

## What this does NOT duplicate, and what was searched

Searched by capability — socket, serve, peer, wire, frame, protocol — across
`openplexus/`, `tools/`, `tests/` and `testbed/`.

- **`openplexus/framing.py` owns the FRAMING** and `send`/`receive` are
  imported rather than re-written. A second length-prefix implementation is two
  chances to disagree about a header.
- **`openplexus/peer.py` WAS DELETED.** It served the superposed store — `read(concept, key)`
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
from openplexus.framing import receive, send


def ask(host: str, port: int, message: tuple, timeout: float = 10.0):
    """Send one message on a FRESH connection and return its reply.

    Kept for callers that make one request and stop — a test, a probe. Anything
    sending more than a handful should use `Link`, which holds its connections.

    Raises rather than returning a default on any failure. **A message that did
    not arrive must not look like a zero** — that is the missing-marginal failure
    `bucket_service.rank` refuses, one layer down.
    """
    with socket.create_connection((host, port), timeout=timeout) as sock:
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        send(sock, json.dumps(list(message)).encode("utf-8"))
        return json.loads(receive(sock).decode("utf-8"))


class Link:
    """Held connections to a set of peers, one socket per destination.

    Attributes:
        sent: Messages sent successfully.
        reconnects: Times a cached socket failed and a fresh one then worked.
            **Not noise — read it.** A number climbing during a run means peers
            are restarting or the link is dropping connections, and every message
            it counts is one that would have been LOST without the retry.
    """

    def __init__(self, addresses: dict[int, tuple[str, int]],
                 timeout: float = 10.0) -> None:
        self.addresses = dict(addresses)
        self.timeout = timeout
        self.sent = 0
        self.reconnects = 0
        self._open: dict[int, socket.socket] = {}
        self._locks: dict[int, threading.Lock] = {}
        self._locks_guard = threading.Lock()

    def _connect(self, node: int) -> socket.socket:
        host, port = self.addresses[node]
        opened = socket.create_connection((host, port), timeout=self.timeout)
        opened.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        self._open[node] = opened
        return opened

    def ask(self, node: int, message: tuple):
        """Send on the held connection, reopening ONCE if it has gone stale.

        **Locked PER DESTINATION, not globally.** A shared socket used by two
        threads at once interleaves two length-prefixed frames and both replies
        come back to the wrong caller — a corruption that reads as data. One
        global lock would fix that and serialise every outbound message in the
        node, including ones to different peers that have nothing to do with each
        other, which on an impaired link is the cost this class exists to remove.
        """
        payload = json.dumps(list(message)).encode("utf-8")
        with self._lock_for(node):
            return self._ask_locked(node, payload)

    def _lock_for(self, node: int) -> threading.Lock:
        with self._locks_guard:
            return self._locks.setdefault(node, threading.Lock())

    def _ask_locked(self, node: int, payload: bytes):
        for attempt in (0, 1):
            cached = self._open.get(node)
            sock = cached if cached is not None else self._connect(node)
            try:
                send(sock, payload)
                reply = json.loads(receive(sock).decode("utf-8"))
            except (OSError, ValueError):
                self._drop(node)
                if attempt or cached is None:
                    # Already retried, or this WAS a fresh connection. A second
                    # failure is the peer being gone rather than the socket
                    # being stale, and retrying past here turns a departure into
                    # a hang.
                    raise
                self.reconnects += 1
                continue
            self.sent += 1
            return reply

    def _drop(self, node: int) -> None:
        sock = self._open.pop(node, None)
        if sock is not None:
            try:
                sock.close()
            except OSError:
                pass

    def close(self) -> None:
        for node in list(self._open):
            self._drop(node)


class BucketPeer:
    """A `BucketService` reachable over TCP, which forwards what it emits.

    Attributes:
        service: The node this serves.
        port: The bound port. Read it rather than assuming — binding to 0 asks
            the OS for a free one, which is what the tests do.
        forwarded: Messages this peer has pushed to other peers, successfully.
            The write-side traffic count, where `bucket_service` counts the read
            side.
        fetched: `SEEN` messages this peer sent to OTHERS while ranking, on a
            caller's behalf. The read-side traffic, where `forwarded` counts the
            write side.
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
        # SHARES the dict rather than copying it. Every peer has to exist before
        # any of their ports are known, so callers fill this in AFTER
        # construction -- and a Link holding a snapshot taken at construction
        # would raise KeyError on the first message to a peer added later, which
        # is what happened.
        self.link = Link({})
        self.link.addresses = self.addresses
        self.forwarded = 0
        self.fetched = 0
        self.failures: list[tuple[int, tuple, str]] = []
        self._lock = threading.Lock()
        self._listener = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._listener.bind((host, port))
        self._listener.listen(64)
        # A TIMEOUT SO `close` CAN ACTUALLY STOP IT, and this is a portability
        # bug Windows hid. Closing a socket another thread is blocked on inside
        # `accept` wakes that thread on Windows and does NOT on Linux, so a
        # "closed" peer went on accepting connections there: CI failed two
        # tests that pass locally -- a connection to a shut-down peer succeeded,
        # and a forward to a "dead" node was delivered.
        #
        # **A peer that cannot be shut down is not a test problem.** It is a node
        # that keeps serving after it departs, which is exactly what C3 is about,
        # and it would have made any churn measurement here meaningless.
        self._listener.settimeout(0.25)
        self.port = self._listener.getsockname()[1]
        self._running = False
        self._thread: threading.Thread | None = None

    def start(self) -> "BucketPeer":
        self._running = True
        self._thread = threading.Thread(target=self._serve, daemon=True)
        self._thread.start()
        return self

    def close(self) -> None:
        """Stop serving, and do not return until the port is actually free.

        The join is not tidiness: a caller that closes a peer and immediately
        asserts it is unreachable is asserting against a listener that may still
        be inside `accept`. Waiting is what makes *"this node has left"* true
        rather than requested.
        """
        self._running = False
        self.link.close()
        if self._thread is not None:
            self._thread.join(timeout=3.0)
        try:
            self._listener.close()
        except OSError:
            pass

    def _serve(self) -> None:
        while self._running:
            try:
                connection, _ = self._listener.accept()
            except TimeoutError:
                continue          # the timeout exists so `_running` gets read
            except OSError:
                return
            threading.Thread(target=self._one, args=(connection,),
                             daemon=True).start()

    def _one(self, connection: socket.socket) -> None:
        """Serve every message on one connection until the caller closes it.

        **The loop is what makes `Link` worth having.** A held connection that
        answered once would still pay a setup round trip per message, and the
        client-side cache would buy nothing.
        """
        with connection:
            connection.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
            while self._running and self._exchange(connection):
                pass

    def _exchange(self, connection: socket.socket) -> bool:
        """One request and one reply. False when the caller has gone away."""
        if True:
            try:
                message = tuple(json.loads(receive(connection).decode("utf-8")))
            except (ConnectionError, ValueError, OSError):
                return False
            # The service is not thread-safe and does not need to be: one lock
            # around the whole handling keeps a flush's outbox from interleaving
            # with another connection's. Contention is irrelevant at the message
            # rates this is for, and a lock-free version would be a second
            # concurrency design nobody asked for.
            if message and message[0] == "RANK":
                # Not a service message: it needs the network, and the service
                # has none. Handled outside the lock because it BLOCKS on peers,
                # and holding the lock across a remote call is how two peers
                # ranking into each other would deadlock.
                reply = self._rank(message)
                send(connection, json.dumps(reply).encode("utf-8"))
                return True
            with self._lock:
                try:
                    reply = self.service.handle(message)
                except ValueError as refused:
                    reply = {"refused": str(refused)}
                outbox = self.service.take()
            # FORWARD BEFORE REPLYING, so the reply means the work has LANDED.
            #
            # The first version replied first and forwarded after. In one
            # process that raced fast enough to pass every test; across three
            # OS processes it does not, and a `FLUSH` returned while its `NOTE`
            # and `LINK` messages were still in flight -- so a caller reading a
            # count straight afterwards read one that had not arrived yet.
            #
            # There is no deadlock in forwarding under a peer's own handler:
            # the listener thread only accepts and spawns, so a peer can serve
            # an incoming forward while one of its own handlers is blocked
            # sending one. Two peers flushing into each other is therefore two
            # handler threads waiting on two other handler threads, not a cycle.
            for destination, forward in outbox:
                self._forward(destination, forward)
            send(connection, json.dumps(reply).encode("utf-8"))
            return True

    def _rank(self, message: tuple):
        """`("RANK", surface, k)` — rank at the owner, fetching what it lacks.

        Returns the chosen partner ids, or a `refused` object if this node does
        not own the surface. `k` may be `null`, which selects the derived
        per-surface bound `g33-04` measured as the best arm.

        **Every `SEEN` this sends is charged to `fetched`.** That is the read
        cost `g33-03` counted in one process, arriving on a real link, and a
        version that cached marginals across queries would report a smaller
        number for a different mechanism — so it does not.
        """
        _, surface, k = message
        try:
            with self._lock:
                candidates = self.service.candidates(surface)
        except ValueError as refused:
            return {"refused": str(refused)}

        seen: dict[int, int] = {}
        for other in candidates:
            owner = self.service.owner(other)
            if owner == self.service.node:
                with self._lock:
                    seen[other] = self.service.handle(("SEEN", other))
                continue
            try:
                seen[other] = self.link.ask(owner, ("SEEN", other))
            except OSError as unreachable:
                # A departed peer. Leaving the marginal ABSENT makes
                # `_Borrowed.seen` raise and the candidate is dropped, which is
                # `Federation`'s graceful-degradation rule arriving over a wire.
                self.failures.append((owner, ("SEEN", other), str(unreachable)))
                continue
            self.fetched += 1

        from openplexus.grounding import STATISTICS
        with self._lock:
            chosen = self.service.rank(surface, STATISTICS["conditional"], k,
                                       seen)
        return chosen

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
        try:
            self.link.ask(destination, message)
        except OSError as unreachable:
            self.failures.append((destination, message, str(unreachable)))
            return
        self.forwarded += 1
