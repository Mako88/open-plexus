"""Reads that go to the one node holding the fact, with no driver in between.

## What this removes

`distributed.Network` has a driver that *"broadcasts a token and sums whatever comes back,
which is the only reduction in the system"*. That sum is the collective amended C1 forbids,
and it is measurable: on containers at 16 nodes, a window of 8 was **slower** than a window
of 4 (2.01 ms against 1.82) because the driver must collect every vote before the window
advances.

Message cost per read, at width 256:

    nodes    broadcast (msgs / bytes)    point to point
        4              8  /   8,212        2  /  2,056
       16             32  /  32,848        2  /  2,056
      256            512  / 525,568        2  /  2,056

**The semantics already exist.** `partitioned.ConceptStore.read` says: *"Read from ONE
surviving holder. No pooling, no vote, no barrier. This is the property the whole
arrangement exists for."* What was missing is the wire.

## The seam this costs, stated because it is a real loss

`ConceptStore.matrix` exists so a caller can keep its own retrieval strategy —
`SuperposedRead`, `SettlingRead`, `ExactCache` all take a matrix. **A remote store cannot
hand back a matrix**: at width 256 that is 512 KB per read against 2 KB for the answer.

So the owning node performs the retrieval and returns the vector, which means **the
retrieval strategy lives on the node rather than with the asker.** That is a consequence of
removing the driver, not a detail: whoever holds the binding decides how it is read.

## Any peer may ask

There is no distinguished process here. A peer serves the concepts it owns and asks about
the ones it does not, which is what *"each node is its own input and output point"* means.
`ownership.Ring` decides who owns what by consistent hashing, so the mapping needs no
coordinator.
"""

from __future__ import annotations

import socket
import struct
import threading

import numpy as np

from openplexus.distributed import receive, send
from openplexus.ownership import Ring

#: `(concept, previous, token)` — three ints. The key is REBUILT by the owner from
#: `(previous, token)` rather than sent: that is `derived_keys`' whole argument, and it
#: keeps a request at twelve bytes instead of a width-vector.
_REQUEST = struct.Struct("!iii")
#: Asks the peer to shut down. Not a concept id any task uses.
_STOP = -1


class ConceptPeer:
    """Serves reads for the concepts it owns. Runs until told to stop.

    Threaded rather than forked so a test can hold one in-process, and because a peer's
    work per request is a single matrix-vector product — the cost is the wire, not the
    arithmetic.
    """

    def __init__(self, store, keys, host: str = "127.0.0.1", port: int = 0) -> None:
        self.store = store
        self.keys = keys
        self._listener = socket.socket()
        self._listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._listener.bind((host, port))
        self._listener.listen(8)
        #: Bound before `serve` starts, so a caller may connect immediately. Binding
        #: in a background thread is what made a test race its own driver -- the
        #: window between choosing a port and accepting on it is not something a
        #: caller may be asked to guess.
        self.port = int(self._listener.getsockname()[1])
        self._thread: threading.Thread | None = None
        self._stop = False

    def start(self) -> ConceptPeer:
        self._thread = threading.Thread(target=self._serve, daemon=True)
        self._thread.start()
        return self

    def _serve(self) -> None:
        while not self._stop:
            try:
                connection, _ = self._listener.accept()
            except OSError:
                return
            with connection:
                connection.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                while True:
                    try:
                        message = receive(connection)
                    except (ConnectionError, OSError):
                        break
                    if not message:
                        break
                    concept, previous, token = _REQUEST.unpack(message)
                    if concept == _STOP:
                        self._stop = True
                        return
                    value = self.store.read(concept,
                                            self.keys.pair(previous, token))
                    send(connection, np.asarray(value, dtype=">f8").tobytes())

    def close(self) -> None:
        self._stop = True
        try:
            self._listener.close()
        except OSError:
            pass


class RemoteConcepts:
    """A store whose reads go over the wire to whichever peer owns the concept.

    Drop-in for the read path `search.py` uses, with one difference stated in the module
    docstring: it answers `read(concept, key)` rather than handing back a matrix, because
    a matrix is 512 KB at width 256 and the answer is 2 KB.
    """

    def __init__(self, peers: dict[int, tuple[str, int]], width: int,
                 keys, seed: int = 0) -> None:
        """`peers` maps a NODE index to its address. Ownership is the ring's business.

        **`Ring`, not `concept % len(peers)`.** The modulo was a placeholder and it is
        wrong for a network: changing the peer count reshuffles nearly every concept,
        so every binding written before the change is at an address nobody asks about.
        Consistent hashing moves about `1/n` instead, which is the property `Ring`
        exists for and `ConceptStore` already relies on.
        """
        self.peers = peers
        self.width = width
        self.keys = keys
        self.ring = Ring(len(peers), seed=seed)
        self._connections: dict[int, socket.socket] = {}
        #: Reads served, so a measurement can count messages rather than assume them.
        self.reads = 0

    def _connection(self, node: int) -> socket.socket:
        existing = self._connections.get(node)
        if existing is not None:
            return existing
        host, port = self.peers[node]
        opened = socket.create_connection((host, port))
        opened.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        self._connections[node] = opened
        return opened

    def owner(self, concept: int) -> int:
        """Which peer holds it. Every peer answers this identically, which is what
        makes the routing need no coordinator."""
        return self.ring.owner(concept)

    def read(self, concept: int, previous: int, token: int) -> np.ndarray:
        connection = self._connection(self.owner(concept))
        send(connection, _REQUEST.pack(concept, previous, token))
        payload = receive(connection)
        self.reads += 1
        return np.frombuffer(payload, dtype=">f8").astype(float)

    def close(self) -> None:
        for connection in self._connections.values():
            try:
                connection.close()
            except OSError:
                pass
        self._connections.clear()
