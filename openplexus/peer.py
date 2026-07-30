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

import hashlib
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
#: The handshake. A peer and a caller must agree about the ring AND the key source, and
#: every way of disagreeing is silent: a read routed by a different ring reaches a peer
#: that never received the write and returns zeros, and a zero vector decodes to whatever
#: the readout prefers -- an answer, not an error. Note 086 is the entry about a config
#: mismatch producing a full set of confident numbers about the wrong model.
_HELLO = struct.Struct("!16s")


def fingerprint(keys, peers: int, seed: int) -> bytes:
    """What both sides must agree on, as sixteen bytes.

    Covers the ROUTING (peer count, ring seed) and the KEY SOURCE (its seed, spread,
    width, start token, route and markers). A difference in any of them sends a read
    somewhere the write never went, or rebuilds a different key at the same address.

    Derived on each side from its own configuration rather than exchanged as data, so
    agreement means the configurations match rather than that one side was told what to
    claim.
    """
    parts = [f"peers={peers}", f"ring={seed}",
             f"keyseed={getattr(keys, 'seed', None)}",
             f"spread={getattr(keys, 'spread', None):.12g}"
             if hasattr(keys, "spread") else "spread=None",
             f"width={getattr(keys, 'width', None)}",
             f"start={getattr(keys, 'start', None)}",
             f"route={getattr(keys, 'route', 'current')}",
             f"markers={sorted(getattr(keys, 'markers', ()))}"]
    return hashlib.sha256("|".join(parts).encode()).digest()[:16]


class ConceptPeer:
    """Serves reads for the concepts it owns. Runs until told to stop.

    Threaded rather than forked so a test can hold one in-process, and because a peer's
    work per request is a single matrix-vector product — the cost is the wire, not the
    arithmetic.
    """

    def __init__(self, store, keys, host: str = "127.0.0.1", port: int = 0,
                 peers: int = 1, seed: int = 0) -> None:
        self.store = store
        self.keys = keys
        #: What a caller must match. `peers` and `seed` are the RING's, which this peer
        #: does not use itself -- it serves whatever it is asked for -- but must agree
        #: about, or callers will ask the wrong peer and get zeros.
        self.fingerprint = fingerprint(keys, peers, seed)
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
                # The handshake first, and REFUSE on mismatch rather than serving.
                # Serving a caller that routes differently is the failure mode this
                # exists to make loud.
                try:
                    theirs, = _HELLO.unpack(receive(connection))
                except (ConnectionError, OSError, struct.error):
                    continue
                send(connection, _HELLO.pack(self.fingerprint))
                if theirs != self.fingerprint:
                    continue
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
                 keys, seed: int = 0, replicas: int = 3) -> None:
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
        #: How many distinct peers hold each concept. Matches `ConceptStore`'s default
        #: for the same reason it has one: John, 2026-07-29 -- *"when nodes drop you
        #: just lose concepts -- that doesn't sound like a very robust system."*
        self.replicas = replicas
        self.fingerprint = fingerprint(keys, len(peers), seed)
        #: Reads that found no living holder, so an absence is COUNTED rather than
        #: quietly returned. Zeros decode to whatever the readout prefers, which is
        #: the silent-answer failure notes 086 and 096 are both about.
        self.absent = 0
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
        send(opened, _HELLO.pack(self.fingerprint))
        theirs, = _HELLO.unpack(receive(opened))
        if theirs != self.fingerprint:
            opened.close()
            raise ValueError(
                f"peer {node} at {host}:{port} disagrees about the ring or the key "
                f"source: it reports {theirs.hex()[:12]} and this caller computes "
                f"{self.fingerprint.hex()[:12]}. Reads would be routed to peers that "
                f"never received the write and would come back as ZEROS, which decode "
                f"to whatever the readout prefers. Refusing rather than serving.")
        self._connections[node] = opened
        return opened

    def owner(self, concept: int) -> int:
        """Which peer holds it. Every peer answers this identically, which is what
        makes the routing need no coordinator."""
        return self.ring.owner(concept)

    def holders(self, concept: int) -> list[int]:
        """Every peer holding `concept`, owner first.

        `Ring.holders` walks clockwise for DISTINCT peers, so a departure needs no
        data movement: *"the remaining replicas are already there and already warm."*
        """
        return self.ring.holders(concept, self.replicas)

    def read(self, concept: int, previous: int, token: int) -> np.ndarray:
        """Ask the owner; on a dead peer, ask the next holder.

        **A departure costs a round trip, not the answer.** C3 says peers come and go,
        and asking only the owner makes every departure a lost concept even though the
        replicas already hold it.

        Returns zeros when no holder answers, matching `ConceptStore.read` -- *"an
        honest absence rather than a degraded answer"* -- and increments `absent`, so
        the absence is counted. An uncounted zero vector decodes to whatever the readout
        prefers and reads as an answer.
        """
        for node in self.holders(concept):
            try:
                connection = self._connection(node)
                send(connection, _REQUEST.pack(concept, previous, token))
                payload = receive(connection)
            except (ConnectionError, OSError, ValueError) as failure:
                if isinstance(failure, ValueError):
                    raise      # a fingerprint mismatch is a config fault, not churn
                self._drop(node)
                continue
            self.reads += 1
            return np.frombuffer(payload, dtype=">f8").astype(float)
        self.absent += 1
        return np.zeros(self.width)

    def _drop(self, node: int) -> None:
        """Forget a peer's socket so a later read reconnects rather than reusing it."""
        stale = self._connections.pop(node, None)
        if stale is not None:
            try:
                stale.close()
            except OSError:
                pass

    def close(self) -> None:
        for connection in self._connections.values():
            try:
                connection.close()
            except OSError:
                pass
        self._connections.clear()
