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

#: `(kind, concept, previous, token)`. The key is REBUILT by the peer from
#: `(previous, token)` rather than sent: that is `derived_keys`' whole argument, and it
#: keeps a read request at thirteen bytes instead of a width-vector. A WRITE carries the
#: value after the header, which is the only place a width-vector crosses the wire in
#: this direction.
_REQUEST = struct.Struct("!Biii")
READ, WRITE, STOP = 0, 1, 2
#: The WIRE FORMAT's version, carried in the fingerprint so two peers speaking different
#: dialects refuse each other instead of misparsing.
#:
#: **1** was reads only: `(concept, previous, token)`.
#: **2** added a kind byte and the write payload.
#:
#: Note 096 recorded the gap this fills — *"two peers on different code with the same
#: config produce the same fingerprint, so a protocol change is invisible to it"* — and
#: note 098 hit it one commit later, when adding the write kind silently changed the
#: format. **`tests/test_peer_reads.py` pins this against `_REQUEST.format`**, so changing
#: the layout without bumping the number fails rather than shipping.
PROTOCOL = 2
#: A write is acknowledged so the caller knows it landed. One byte.
_ACK = b""
#: The handshake. A peer and a caller must agree about the ring AND the key source, and
#: every way of disagreeing is silent: a read routed by a different ring reaches a peer
#: that never received the write and returns zeros, and a zero vector decodes to whatever
#: the readout prefers -- an answer, not an error. Note 086 is the entry about a config
#: mismatch producing a full set of confident numbers about the wrong model.
_HELLO = struct.Struct("!16s")


def fingerprint(keys, peers: int, seed: int) -> bytes:
    """What both sides must agree on, as sixteen bytes.

    Covers the WIRE FORMAT (`PROTOCOL`), the ROUTING (peer count, ring seed) and the KEY
    SOURCE (its seed, spread, width, start token, route and markers). A difference in any
    of them sends a read somewhere the write never went, rebuilds a different key at the
    same address, or misparses the request entirely.

    Derived on each side from its own configuration rather than exchanged as data, so
    agreement means the configurations match rather than that one side was told what to
    claim.
    """
    parts = [f"protocol={PROTOCOL}", f"peers={peers}", f"ring={seed}",
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
        #: Served counts, so a measurement reports what happened rather than what was
        #: intended.
        self.writes = 0
        self._listener = socket.socket()
        self._listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._listener.bind((host, port))
        self._listener.listen(8)
        # A TIMEOUT so `accept` returns periodically and the loop can notice `_stop`.
        # Closing a listener from another thread while one is blocked in `accept` is
        # not portable: on Windows the accept fails and the peer stops, on Linux the
        # peer kept serving and a test asserting a departure read zeros got a real
        # answer instead -- in CI only, because this machine happened to behave the
        # other way.
        self._listener.settimeout(0.1)
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
            except socket.timeout:
                continue          # the point of the timeout: re-check `_stop`
            except OSError:
                return
            with connection:
                connection.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                # Same reason as the listener's timeout: without it a peer blocked in
                # `receive` on a live connection never re-checks `_stop`, so `close`
                # waits out its whole join and the peer keeps answering meanwhile.
                connection.settimeout(0.1)
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
                while not self._stop:
                    try:
                        message = receive(connection)
                    except socket.timeout:
                        continue
                    except (ConnectionError, OSError):
                        break
                    if not message:
                        break
                    header = message[:_REQUEST.size]
                    kind, concept, previous, token = _REQUEST.unpack(header)
                    if kind == STOP:
                        self._stop = True
                        return
                    key = self.keys.pair(previous, token)
                    if kind == WRITE:
                        # The value rides after the header in the SAME message, so a
                        # write is one round trip rather than two.
                        value = np.frombuffer(message[_REQUEST.size:],
                                              dtype=">f8").astype(float)
                        self.store.write(concept, key, value)
                        self.writes += 1
                        send(connection, _ACK)
                        continue
                    send(connection,
                         np.asarray(self.store.read(concept, key),
                                    dtype=">f8").tobytes())

    def close(self) -> None:
        """Stop serving, and do not return until the peer has actually stopped.

        **Synchronous on purpose.** A caller simulating a departure needs the peer to be
        gone by the time the next read happens, and an asynchronous close makes that a
        race whose outcome depends on the platform's `accept` semantics -- which is
        exactly how a churn test passed here and failed in CI.
        """
        self._stop = True
        thread, self._thread = self._thread, None
        if thread is not None and thread.is_alive():
            # At most one accept timeout plus the work in flight.
            thread.join(timeout=2.0)
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
        #: Writes that reached NOBODY. Counted for the same reason: a write that lands
        #: nowhere and says nothing is a fact the network believes it holds.
        self.lost = 0
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

    def write(self, concept: int, previous: int, token: int,
              value: np.ndarray) -> int:
        """Bind `value` at `pair(previous, token)` on every peer holding `concept`.

        Returns how many holders acknowledged. **Fewer than `replicas` is not an error
        here** -- a departed peer is what C3 says to expect, and `ConceptStore.write`
        takes the same view, skipping absent nodes rather than failing.

        ## The one place a write is not free

        This waits for every holder it can reach, which is an `R`-way wait. `R` is 2 or 3
        rather than `N`, so it is not the collective C1 forbids -- but it is not nothing,
        and the alternative is to acknowledge the owner and let the replicas catch up.
        That trades a wait for a window in which a read can miss a recent write, and
        which is right depends on a measurement nobody has taken.
        """
        payload = _REQUEST.pack(WRITE, concept, previous, token) + np.asarray(
            value, dtype=">f8").tobytes()
        #: Named so the fan-out has one anchor a mutation can target. `for node in
        #: self.holders(concept)` appears on the read path too, and a mutation cannot
        #: point at a line that occurs twice -- which is the duplication check arriving
        #: from the harness rather than from `check_duplication`.
        targets = self.holders(concept)
        landed = 0
        for node in targets:
            try:
                connection = self._connection(node)
                send(connection, payload)
                if receive(connection) == _ACK:
                    landed += 1
            except (ConnectionError, OSError, ValueError) as failure:
                if isinstance(failure, ValueError):
                    raise
                self._drop(node)
        if not landed:
            self.lost += 1
        return landed

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
                send(connection, _REQUEST.pack(READ, concept, previous, token))
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
