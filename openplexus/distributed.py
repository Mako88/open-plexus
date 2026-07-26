"""The same computation, split across processes that talk over sockets.

Everything measured in this project so far has been one process holding one array
and *calling* slices of it nodes. That is a faithful model of the arithmetic and
says nothing about whether the arithmetic survives being spread out, because no
packet has ever been sent.

This sends packets. Each node is a separate OS process with its own memory,
connected to a driver over loopback TCP. The driver sends a token id — **four
bytes**, per [note 012](../docs/notes/012-broadcast-the-token.md) — and each node
replies with its own complete vote. Nothing else crosses the wire.

## What a node holds

Nothing shared, and nothing large:

- its own contiguous slice of the width, `[lo, hi)`
- rows `[lo, hi)` of the associative memory — `(hi - lo) x d` numbers
- columns `[lo, hi)` of the **value** projection — `vocab x (hi - lo)`
- columns `[lo, hi)` of the readout — `vocab x (hi - lo)`
- the seed, from which it derives any **key** row it needs

It does **not** hold the key table, and that is the asymmetry that matters. `Wk`
is needed in full by every node — retrieval sums over every dimension — so storing
it would mean every node holding `vocab x d`. Being a frozen projection, row `t`
can instead be drawn from `(seed, t)` on demand, which is what makes a four-byte
token sufficient and why a departing node takes no key dimensions with it.

`Wv` is different: a node only ever needs *its own columns*, which is `vocab x w`
— 41 numbers for a width-1 node here. Small enough to hold, so it is handed over
once at startup rather than derived. Deriving it would also have made bit-identity
impossible, since the single-process model draws `Wv` from one stream over the
whole table.

## What this milestone claims

Only that the split is exact: running across `n` processes must produce **the
same predictions** as the single-process model, not merely similar ones. Delay,
loss and churn come after, and are meaningless until this holds.
"""

from __future__ import annotations

import select
import socket
import struct
from dataclasses import dataclass

import numpy as np

from openplexus.models.local_memory import LocalMemoryConfig

# Framing: every message is a length-prefixed blob. Fixed-size reads would be
# faster and would break the moment a vote size changed, so the length goes on
# the wire even though the driver could compute it.
_HEADER = struct.Struct("!I")

# Control tokens, outside the vocabulary because a vocabulary index is never
# negative. Sending them in band keeps the protocol to one message type.
_DONE = -1
_RESET = -2


def send(sock: socket.socket, payload: bytes) -> None:
    sock.sendall(_HEADER.pack(len(payload)) + payload)


def receive(sock: socket.socket) -> bytes:
    """Read one framed message, or raise if the peer went away mid-message."""
    header = _read_exactly(sock, _HEADER.size)
    (length,) = _HEADER.unpack(header)
    return _read_exactly(sock, length)


def _read_exactly(sock: socket.socket, count: int) -> bytes:
    chunks, seen = [], 0
    while seen < count:
        chunk = sock.recv(count - seen)
        if not chunk:
            raise ConnectionError(
                f"peer closed after {seen} of {count} bytes -- a partial "
                f"message is not a departure, it is a bug")
        chunks.append(chunk)
        seen += len(chunk)
    return b"".join(chunks)


@dataclass(frozen=True)
class Slice:
    """Which dimensions one node owns. Half-open, as everything else here is."""

    lo: int
    hi: int

    @property
    def width(self) -> int:
        return self.hi - self.lo


def slices_for(d_model: int, nodes: int) -> list[Slice]:
    """Contiguous, equal-sized, and refusing to be uneven.

    Uneven slices are a real deployment case — machines differ in power — but
    they are not this milestone, and silently rounding would make the bit-identity
    claim untestable. So it raises rather than guesses.
    """
    if d_model % nodes:
        raise ValueError(
            f"{nodes} nodes do not divide a width of {d_model}; uneven slices "
            f"are a later milestone, not something to round")
    width = d_model // nodes
    return [Slice(i * width, (i + 1) * width) for i in range(nodes)]


class Node:
    """One machine's share of the network, holding nothing it does not own."""

    def __init__(self, config: LocalMemoryConfig, own: Slice,
                 values: np.ndarray, readout: np.ndarray) -> None:
        self.config = config
        self.own = own
        self.values = np.array(values)      # vocab x own.width
        self.readout = np.array(readout)    # vocab x own.width
        self.memory = np.zeros((own.width, config.d_model))
        self._previous_key: np.ndarray | None = None
        self._spread = config.key_scale / np.sqrt(config.d_model)

    def key(self, token: int) -> np.ndarray:
        """Derived, not stored -- the whole reason a token id is enough.

        Row `t` depends on `(seed, t)` and on nothing drawn before it, so this
        node can produce any row without ever having seen the others.
        """
        return np.random.default_rng((self.config.seed, int(token))).normal(
            0.0, self._spread, self.config.d_model)

    def reset(self) -> None:
        """Forget the sequence, keep the model.

        The associative memory is per-sequence working state, not a parameter --
        the single-process model builds a fresh one on every call. A node process
        outlives the sequence, so somebody has to say when one ends, and without
        it a second sequence starts inside the first one's memory.

        Caught by a departure test whose answers changed BEFORE the departure
        step, which is not something a departure can do.
        """
        self.memory[:] = 0.0
        self._previous_key = None

    def step(self, token: int) -> np.ndarray:
        """Take one token, return this node's complete vote over the vocabulary."""
        key = self.key(token)
        value = self.values[token]

        if self._previous_key is not None:
            if self.config.decay < 1.0:
                self.memory *= self.config.decay
            self.memory += np.outer(value, self._previous_key)

        retrieved = self.memory @ key
        self._previous_key = key
        return self.readout @ retrieved


def serve(config: LocalMemoryConfig, own: Slice, host: str, port: int,
          values: np.ndarray, readout: np.ndarray) -> None:
    """Run one node against a driver. Blocks until the driver hangs up.

    Values and readout are handed over rather than learned here: this milestone
    asks whether the SPLIT is exact, and training across processes is a separate
    question with its own failure modes.
    """
    node = Node(config, own, values, readout)
    step = 0
    with socket.create_connection((host, port)) as sock:
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        while True:
            try:
                message = receive(sock)
            except (ConnectionError, OSError):
                return
            if not message:
                return
            (token,) = struct.unpack("!i", message)
            if token == _DONE:
                return
            if token == _RESET:
                node.reset()
                step = 0
                continue
            # The step index rides with the vote. Without it the driver can
            # only match votes to steps by counting, which forces it to wait for
            # everyone before moving on -- the lock-step this window exists to
            # remove.
            vote = node.step(token)
            send(sock, struct.pack("!i", step) + vote.astype(">f8").tobytes())
            step += 1


class Network:
    """A driver and its nodes, each in its own process, talking over loopback.

    The driver holds no model state. It broadcasts a token and sums whatever
    comes back, which is the only reduction in the system and the one g4-01
    established is optional at read time. It is done here because *somebody* has
    to produce a single answer for the benchmark to score.
    """

    def __init__(self, config: LocalMemoryConfig, nodes: int,
                 values: np.ndarray, readout: np.ndarray) -> None:
        self.config = config
        self.slices = slices_for(config.d_model, nodes)
        self._values = values
        self._readout = readout
        self._listener: socket.socket | None = None
        self._connections: list[socket.socket] = []
        self._processes: list = []

    def __enter__(self) -> "Network":
        import multiprocessing as mp

        self._listener = socket.socket()
        self._listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._listener.bind(("127.0.0.1", 0))
        self._listener.listen(len(self.slices))
        port = self._listener.getsockname()[1]

        context = mp.get_context("spawn")
        for own in self.slices:
            process = context.Process(
                target=serve,
                args=(self.config, own, "127.0.0.1", port,
                      self._values[:, own.lo:own.hi].copy(),
                      self._readout[:, own.lo:own.hi].copy()),
                daemon=True)
            process.start()
            self._processes.append(process)

        # Accept in connection order, then ask each who it is -- accept order is
        # not slice order and assuming it would silently permute the network.
        pending = [self._listener.accept()[0] for _ in self.slices]
        for sock in pending:
            sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        self._connections = pending
        return self

    def __exit__(self, *exc) -> None:
        for sock in self._connections:
            try:
                send(sock, struct.pack("!i", _DONE))
            except OSError:
                pass
            sock.close()
        for process in self._processes:
            process.join(timeout=5)
            if process.is_alive():
                process.terminate()
        if self._listener is not None:
            self._listener.close()

    def run(self, tokens: np.ndarray, absent: set[int] | None = None,
            leave_at: int | None = None, window: int = 1) -> np.ndarray:
        """Broadcast each token, sum the votes, return a prediction per position.

        `absent` names nodes that stop answering from `leave_at` onward -- a real
        departure, in the sense that the driver never hears from them again and
        their share of the memory goes with their process.

        `window` is how far ahead the driver may run. **At 1 this is lock-step:
        every node must answer before anyone moves, which is precisely the
        global synchronisation C1 forbids.** Above 1, nodes proceed at their own
        pace and votes arrive interleaved, reassembled by the step index each one
        carries.

        [g2-01](../experiments/sweeps/g2-01-latency.txt) established in
        simulation that below a bound, delay changes nothing bit-for-bit. This is
        where that claim meets real sockets, real process scheduling and real
        arrival order.
        """
        if window < 1:
            raise ValueError("a window below 1 cannot make progress")
        absent = absent or set()
        # Every sequence starts clean, matching the single-process contract.
        for sock in self._connections:
            send(sock, struct.pack("!i", _RESET))
        predictions = np.zeros(len(tokens), dtype=np.int64)
        gone: set[int] = set()
        pending: dict[int, list] = {}
        expected: dict[int, int] = {}
        sent = settled = 0

        def dispatch(step: int) -> None:
            nonlocal gone
            if leave_at is not None and step == leave_at:
                gone = set(absent)
            live = [i for i in range(len(self._connections)) if i not in gone]
            expected[step] = len(live)
            pending[step] = [np.zeros(self.config.vocab_size), 0]
            for index in live:
                send(self._connections[index], struct.pack("!i", int(tokens[step])))

        while settled < len(tokens):
            while sent < len(tokens) and sent - settled < window:
                dispatch(sent)
                sent += 1

            # Read from whichever node is ready. Arrival order is the operating
            # system's business, not ours -- which is the point: the answer must
            # not depend on it.
            live = [self._connections[i] for i in range(len(self._connections))
                    if i not in gone]
            ready, _, _ = select.select(live, [], [], 30.0)
            if not ready:
                raise TimeoutError(
                    f"no node answered within 30s at step {settled}")
            for sock in ready:
                message = receive(sock)
                (step,) = struct.unpack("!i", message[:4])
                if step not in pending:
                    continue          # a vote for a step already settled
                slot = pending[step]
                slot[0] += np.frombuffer(message[4:], dtype=">f8")
                slot[1] += 1

            while settled < sent and pending[settled][1] >= expected[settled]:
                predictions[settled] = int(pending[settled][0].argmax())
                del pending[settled], expected[settled]
                settled += 1
        return predictions

    @property
    def bytes_per_step_inbound(self) -> int:
        """What one node receives per step. Four bytes, at any width."""
        return _HEADER.size + 4
