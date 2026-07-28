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
import time
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
          values: np.ndarray, readout: np.ndarray,
          combine: str = "sum") -> None:
    """Run one node against a driver. Blocks until the driver hangs up.

    Values and readout are handed over rather than learned here: this milestone
    asks whether the SPLIT is exact, and training across processes is a separate
    question with its own failure modes.
    """
    node = Node(config, own, values, readout)
    step = 0
    with socket.create_connection((host, port)) as sock:
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        # Announce which slice this is, before anything else crosses the wire.
        # The driver cannot infer it: accept order is arrival order, and arrival
        # order is whatever the network felt like. See Network.__enter__.
        send(sock, struct.pack("!ii", own.lo, own.hi))
        while True:
            try:
                message = receive(sock)
            except (ConnectionError, OSError):
                return
            if not message:
                return
            # A token, and whether the driver wants to hear back. Five bytes
            # rather than four: the flag is what lets a node stay silent
            # without the driver waiting for a vote that is never coming.
            token, wanted = struct.unpack("!i?", message)
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
            if wanted:
                if combine == "vote":
                    # This node's own complete answer, in four bytes. Its score
                    # vector already spans the whole vocabulary -- its slice's
                    # retrieval through its own readout columns -- so the argmax
                    # is a whole opinion rather than a fragment of one.
                    send(sock, struct.pack("!ii", step, int(vote.argmax())))
                else:
                    send(sock, struct.pack("!i", step)
                         + vote.astype(">f8").tobytes())
            # The step advances either way. A silent node has still heard the
            # token and still updated its own store -- it has simply not paid to
            # say so.
            step += 1


class Network:
    """A driver and its nodes, each in its own process, talking over loopback.

    The driver holds no model state. It broadcasts a token and sums whatever
    comes back, which is the only reduction in the system and the one g4-01
    established is optional at read time. It is done here because *somebody* has
    to produce a single answer for the benchmark to score.
    """

    def __init__(self, config: LocalMemoryConfig, nodes: int,
                 values: np.ndarray, readout: np.ndarray,
                 host: str = "127.0.0.1", port: int = 0,
                 spawn: bool = True, combine: str = "sum") -> None:
        """`spawn=False` waits for nodes started elsewhere to connect.

        The default starts them locally and is what every existing measurement
        used. Turning it off is what lets a node be a container on the other end
        of an emulated link, which is the only way G2, G3 and G4 stop being
        modelled -- and, less obviously, the only way to make a node connect in
        an order the driver did not choose, which is what the slice handshake
        exists to survive.
        """
        if combine not in ("sum", "vote"):
            raise ValueError(f"combine must be 'sum' or 'vote', got {combine!r}")
        self.config = config
        self.slices = slices_for(config.d_model, nodes)
        self._combine = combine
        #: step -> how many expected votes were missing when it settled.
        #: Empty unless `run(deadline=...)` forced a step through short, and
        #: reset by every `run`. **Observation only** -- nothing reads it back,
        #: exactly as the model's `trace` is observation only.
        self.steps_settled_short: dict[int, int] = {}
        self.port = port
        self._values = values
        self._readout = readout
        self._host = host
        self._spawn = spawn
        self._listener: socket.socket | None = None
        self._connections: list[socket.socket] = []
        self._processes: list = []

    def __enter__(self) -> "Network":
        self._listener = socket.socket()
        self._listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._listener.bind((self._host, self.port))
        self._listener.listen(len(self.slices))
        self.port = port = self._listener.getsockname()[1]
        if not self._spawn:
            return self._accept()

        import multiprocessing as mp

        context = mp.get_context("spawn")
        for own in self.slices:
            process = context.Process(
                target=serve,
                args=(self.config, own, "127.0.0.1", port,
                      self._values[:, own.lo:own.hi].copy(),
                      self._readout[:, own.lo:own.hi].copy(),
                      self._combine),
                daemon=True)
            process.start()
            self._processes.append(process)

        return self._accept()

    def _accept(self) -> "Network":
        # Accept in arrival order, then ask each who it is. **This comment used
        # to describe a handshake that was not implemented**: connections were
        # kept in accept order and indexed as though that were slice order.
        #
        # Summing votes is order-independent, so bit-identity could never catch
        # it -- but `absent` names nodes BY INDEX, so a departure test removed
        # whichever node happened to connect third rather than the third slice.
        # On loopback that is usually spawn order and usually right. Under a
        # network with delay it is neither, and the error is invisible: the run
        # still completes and still looks plausible.
        pending = []
        for _ in self.slices:
            sock = self._listener.accept()[0]
            sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
            pending.append((struct.unpack("!ii", receive(sock)), sock))

        order = {(s.lo, s.hi): i for i, s in enumerate(self.slices)}
        if sorted(k for k, _ in pending) != sorted(order):
            raise ValueError(
                f"nodes announced {sorted(k for k, _ in pending)}, "
                f"expected {sorted(order)}")
        self._connections = [sock for _, sock
                             in sorted(pending, key=lambda p: order[p[0]])]
        return self

    def __exit__(self, *exc) -> None:
        for sock in self._connections:
            try:
                send(sock, struct.pack("!i?", _DONE, False))
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
            leave_at: int | None = None, window: int = 1,
            speak: float = 1.0, deadline: float | None = None) -> np.ndarray:
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

        ## `deadline` -- the difference between a declared departure and a real one

        **None (the default) waits for every expected vote.** That is what every
        result in this project was measured under and it is correct while nobody
        leaves unannounced: `absent` and `leave_at` lower `expected`, so a
        DECLARED departure settles normally, which is what g12-02 measured across
        18 cells.

        **An UNDECLARED departure stalls it.** The step never reaches its count,
        the window fills, the driver stops sending, and 30 seconds later the
        select below raises. That is a barrier that stalls when a participant is
        slow or gone, which is exactly what amended C1 forbids -- and C3 says
        departure arrives without warning, so the declared case is the easy one.

        Setting `deadline` to a number of seconds settles a step once that long
        has passed since it was dispatched, with whatever votes arrived. This is
        note 003's design: `d_max` is simultaneously the C2 asynchrony bound and
        the C3 churn timeout, so a source inside it is a straggler and a source
        beyond it is a dropout. Two constraints, one parameter.

        **It costs bit-identity, and that is not a detail.** With a deadline the
        answer depends on who replied in time, so the property G2 was passed on
        -- weights bit-identical to a run with no network at all -- cannot hold.
        That is why it is off by default rather than simply better: the two modes
        answer different questions, and every earlier number belongs to the
        first.

        `steps_settled_short` records what it cost, per run, so a degraded answer
        is visible rather than silent.

        Args:
            deadline: Seconds to wait for a step before settling on whatever
                arrived. `None` waits for every expected vote.

        Raises:
            ValueError: If `deadline` is not positive.
        """
        if window < 1:
            raise ValueError("a window below 1 cannot make progress")
        if deadline is not None and deadline <= 0:
            raise ValueError("a deadline of zero settles every step on nothing")
        absent = absent or set()
        # Every sequence starts clean, matching the single-process contract.
        #
        # A node already gone when the sequence starts fails HERE, before any
        # dispatch -- which is the common case for a machine switched off
        # between sequences rather than during one. Same rule as in `dispatch`:
        # tolerated only when a deadline was asked for.
        starting_unreachable: set[int] = set()
        for index, sock in enumerate(self._connections):
            try:
                send(sock, struct.pack("!i?", _RESET, False))
            except (ConnectionError, OSError):
                if deadline is None:
                    raise
                starting_unreachable.add(index)
        predictions = np.zeros(len(tokens), dtype=np.int64)
        gone: set[int] = set()
        # Connections that have actually hung up, as opposed to nodes we have
        # stopped sending to. Only these are removed from the read set.
        dead: set[int] = set()
        pending: dict[int, list] = {}
        expected: dict[int, int] = {}
        #: When each pending step was dispatched, so its deadline can be judged
        #: from when it was ASKED rather than from when the driver last looped.
        asked_at: dict[int, float] = {}
        #: Nodes whose connection failed on the way out. Distinct from `gone`,
        #: which is a DECLARED departure, and from `dead`, which is a hang-up
        #: noticed while reading -- this is one noticed while writing.
        unreachable: set[int] = set(starting_unreachable)
        sent = settled = 0
        self.steps_settled_short = {}
        self.nodes_unreachable = unreachable

        def dispatch(step: int) -> None:
            nonlocal gone
            if leave_at is not None and step == leave_at:
                gone = set(absent)
            asked_at[step] = time.monotonic()
            live = [i for i in range(len(self._connections))
                    if i not in gone and i not in unreachable]
            # Who answers this step. Round-robin rather than random: it is
            # deterministic, it spreads the cost evenly, and it guarantees the
            # count exactly rather than in expectation -- which matters because
            # a step that happens to draw zero speakers has no answer at all.
            wanted = max(1, int(round(speak * len(live)))) if live else 0
            start = (step * wanted) % len(live) if live else 0
            speaking = {live[(start + n) % len(live)] for n in range(wanted)}
            pending[step] = [np.zeros(self.config.vocab_size), 0]
            for index in live:
                # Everyone HEARS the token -- a node that stopped listening
                # would stop updating its own store and leave the network in
                # all but name. Only some are asked to answer.
                try:
                    send(self._connections[index],
                         struct.pack("!i?", int(tokens[step]),
                                     index in speaking))
                except (ConnectionError, OSError):
                    # A NODE THAT IS ALREADY GONE, discovered on the way out.
                    #
                    # A machine switched off mid-session resets its connection,
                    # so the failure surfaces on SEND rather than as silence.
                    # Both are the same event -- a participant is gone -- and a
                    # design that survives one and crashes on the other has not
                    # survived churn.
                    #
                    # **Only tolerated when a deadline was asked for.** Without
                    # one the caller has opted into the strict mode every
                    # earlier result was measured under, where this raised; and
                    # turning a crash into silent degradation by default would
                    # hide real faults behind a fault-tolerance feature.
                    if deadline is None:
                        raise
                    unreachable.add(index)
                    speaking.discard(index)
            # AFTER the sends, so a node discovered unreachable while
            # dispatching is not counted as a voter this step. Counting it would
            # recreate the barrier one step at a time.
            expected[step] = len(speaking)

        while settled < len(tokens):
            while sent < len(tokens) and sent - settled < window:
                dispatch(sent)
                sent += 1

            # Read from whichever node is ready. Arrival order is the operating
            # system's business, not ours -- which is the point: the answer must
            # not depend on it.
            #
            # **Read from EVERY connection, including departed ones.** `gone`
            # stops a node being sent to; it cannot un-send a vote already
            # transmitted. Excluding departed nodes here was a deadlock: with a
            # window above 1 the driver runs ahead, so a node can have answered
            # steps 20-29 and then be dropped at step 30 -- and those answers,
            # still unread in the socket, would never be collected. The step
            # would never reach its expected count and the run would time out
            # BEFORE the departure it was testing.
            #
            # It was invisible at window 1, where nothing is ever in flight
            # across the departure, so every in-process test passed. The
            # container testbed found it in the first run that combined a
            # window with a departure -- which is C1 and C3 at the same time,
            # i.e. the only configuration this project actually cares about.
            live = [sock for i, sock in enumerate(self._connections)
                    if i not in dead]
            # Never block past the next deadline. Selecting for the full 30
            # seconds would make the deadline advisory -- a step could be due to
            # settle while the driver sat waiting for a node that is not coming,
            # which is the stall this parameter exists to remove.
            wait = 30.0
            if deadline is not None and settled in asked_at:
                wait = max(0.0, deadline - (time.monotonic()
                                            - asked_at[settled]))
            ready, _, _ = select.select(live, [], [], wait)
            if not ready and deadline is None:
                raise TimeoutError(
                    f"no node answered within 30s at step {settled}")
            for sock in ready:
                try:
                    message = receive(sock)
                except (ConnectionError, OSError):
                    # A RESET IS A HANG-UP, and the two arrive differently.
                    #
                    # A peer that closes cleanly gives an empty read; a peer
                    # whose process was killed resets the connection, which
                    # RAISES. The branch below has always handled the first and
                    # the second escaped it entirely -- so on any platform that
                    # reports a dead peer as a reset, the hang-up path never
                    # fired and a killed node took the run down with it.
                    #
                    # Unconditional, unlike the send-side guards: this is not
                    # fault tolerance being opted into, it is a case the
                    # existing branch always meant to cover.
                    message = b""
                if not message:
                    # A real hang-up, not a simulated departure. Stop selecting
                    # on it or select() returns it ready forever.
                    dead.add(self._connections.index(sock))
                    continue
                (step,) = struct.unpack("!i", message[:4])
                if step not in pending:
                    continue          # a vote for a step already settled
                slot = pending[step]
                if self._combine == "vote":
                    # One count per answer. Absence costs a voter, not a term of
                    # a sum, which is why this degrades where summing amputates.
                    (choice,) = struct.unpack("!i", message[4:8])
                    slot[0][choice] += 1.0
                else:
                    slot[0] += np.frombuffer(message[4:], dtype=">f8")
                slot[1] += 1

            while settled < sent:
                votes = pending[settled][1]
                complete = votes >= expected[settled]
                # OVERDUE, and it settles on what arrived. At least one vote is
                # required: settling on none would emit `argmax` of a zero
                # vector, which is token 0 wearing the appearance of an answer.
                overdue = (deadline is not None and votes >= 1
                           and time.monotonic() - asked_at[settled] >= deadline)
                if not (complete or overdue):
                    break
                if not complete:
                    # Recorded rather than logged. A degraded answer that leaves
                    # no trace is indistinguishable from a good one, and the
                    # whole point of a deadline is that the degradation is the
                    # thing being measured.
                    self.steps_settled_short[settled] = (
                        expected[settled] - votes)
                predictions[settled] = int(pending[settled][0].argmax())
                del pending[settled], expected[settled], asked_at[settled]
                settled += 1
        return predictions

    @property
    def bytes_per_step_inbound(self) -> int:
        """What one node receives per step. Five bytes, at any width.

        A token id and a flag saying whether an answer is wanted. Independent of
        width, of vocabulary and of how many nodes there are, because it is a
        broadcast of the same message -- which is the whole point of
        [note 012](../docs/notes/012-broadcast-the-token.md).
        """
        return _HEADER.size + struct.calcsize("!i?")

    @property
    def bytes_per_vote(self) -> int:
        """What one node sends when it answers. **This is the expensive one.**

        A step index and a complete vote, one float64 per token in the
        vocabulary. It scales with the VOCABULARY, which nothing inbound does:

            vocab     41  ->     ~336 bytes
            vocab 50,000  ->  ~400,000 bytes

        So the outbound cost of a network is `bytes_per_vote * votes per step`,
        and G4 turns on how few votes a step can get away with rather than on how
        small a vote is.
        """
        if self._combine == "vote":
            return _HEADER.size + struct.calcsize("!ii")
        return _HEADER.size + 4 + 8 * self.config.vocab_size
