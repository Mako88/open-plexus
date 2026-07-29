"""A step that settles SHORT must wait the deadline first, and nothing tested that.

Note 054 and decision 168. `the-deadline-fires-immediately` survived a CI mutation
shard while being caught on every local run, and chasing why found something larger
than a flaky test:

    `steps_settled_short` was asserted in exactly ONE place in this repository,
    and it was asserted to be EMPTY.

So the branch the whole `deadline` parameter exists for — settle on what arrived
rather than stall forever — had **no test that it ever ran**, let alone that it
waited the stated time. That is rule 10's named pattern, *a test that something did
NOT change needs a companion asserting that something DID*, with the companion
missing on a C1/C3 mechanism.

## Why it could not be tested before

Every route to `1 <= votes < expected` was closed:

    a node is killed        resets the socket -> dropped from `expected`
    a node is not asked     `expected[step] = len(speaking)`, so not counted
    a send to it fails      `speaking.discard(index)`, so not counted
    a node HANGS            <- the only one that produces the condition

Nothing in the harness could create the last one. **This file creates it.** A
`SilentPeer` completes the slice handshake, reads every request, and answers none —
so the driver counts it as a voter that never votes, which is the only situation a
deadline is for.

`spawn=False` is what makes this possible without touching production code: it
already exists so a node can be a container on the other end of an emulated link
(decision 128's line), and a silent socket is the degenerate case of that.

## What is deliberately NOT re-implemented

`TalkingPeer` returns a fixed vote rather than computing one. The subject here is
**when the driver settles**, not what it answers, and reproducing `Node.step` in a
test would be the duplicated-logic hazard rule 9 is about — a second implementation
that drifts and keeps producing plausible numbers.
"""

from __future__ import annotations

import socket
import struct
import threading
import time
import unittest

import numpy as np

from openplexus.distributed import Network, receive, send, slices_for
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

TOKENS = np.array([3, 9, 1, 7, 3])
#: Long enough that a settle-short is unambiguous and short enough that the test
#: costs one deadline rather than five.
DEADLINE = 0.5
#: Mirrored from `distributed.py` rather than imported, because they are private
#: there and a fake node is a wire-protocol client like any other. If these drift
#: the peer stops being asked anything and `test_the_silent_node_is_asked_and_does
#: _not_answer` is what says so.
_DONE, _RESET = -1, -2


def configured(partitions: int, width: int = 32):
    config = LocalMemoryConfig(
        vocab_size=14, d_model=width, partitions=partitions, key_scale=0.5,
        derived_keys=True, seed=5)
    model = LocalAssociativeMemory(config)
    model.wo[:] = np.random.default_rng(0).normal(0.0, 0.1, (14, width))
    return config, model


class _Peer(threading.Thread):
    """A node that speaks the wire protocol and nothing else."""

    def __init__(self, host: str, port: int, lo: int, hi: int, vocab: int):
        super().__init__(daemon=True)
        self.host, self.port, self.lo, self.hi = host, port, lo, hi
        self.vocab = vocab
        self.requests = 0
        self.error: BaseException | None = None

    def answers(self) -> bool:
        raise NotImplementedError

    def run(self) -> None:
        try:
            # RETRIED, because the driver binds its listener AFTER these threads
            # start -- the port is reserved by the test but nothing is listening on
            # it yet, so the first attempts are refused. The first version did not
            # retry: the peers died on ConnectionRefused, the driver blocked in
            # `accept()` for a connection that was never coming, and the test hung
            # rather than failing.
            deadline = time.monotonic() + 10.0
            sock = None
            while sock is None:
                try:
                    sock = socket.create_connection((self.host, self.port),
                                                    timeout=1.0)
                except OSError:
                    if time.monotonic() > deadline:
                        raise
                    time.sleep(0.01)
            # Back to blocking. `create_connection`'s timeout would otherwise
            # apply to every subsequent `recv`, and a node waiting for the next
            # token is supposed to wait.
            sock.settimeout(None)
            sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
            # THE SLICE HANDSHAKE, which the driver requires before it will index
            # this connection -- `_accept` raises if the announced slices do not
            # match the ones it expects.
            send(sock, struct.pack("!ii", self.lo, self.hi))
            step = 0
            while True:
                token, asked = struct.unpack("!i?", receive(sock))
                if token == _DONE:
                    break
                if token == _RESET:
                    # THE STEP COUNTER RESTARTS AND DOES NOT ADVANCE HERE.
                    #
                    # `run` sends _RESET before the first token. Counting it as a
                    # step puts every vote one ahead of the step the driver is
                    # waiting on, and the driver's `if step not in pending:
                    # continue` then DISCARDS each vote in silence -- so votes
                    # stay at 0, the `votes >= 1` clause never becomes true, the
                    # overdue branch never fires, and the run blocks forever.
                    #
                    # The first version of this peer did exactly that and the test
                    # HUNG instead of failing. Worth the comment: a malformed vote
                    # is dropped without complaint, which is correct for a real
                    # network and merciless for a fake node.
                    step = 0
                    continue
                self.requests += 1
                if asked and self.answers():
                    # step, then one vote. The driver counts a vote per message
                    # and reads the choice for `combine="vote"`.
                    send(sock, struct.pack("!i", step)
                         + struct.pack("!i", token % self.vocab))
                step += 1
            sock.close()
        except BaseException as exc:            # surfaced in the test, not lost
            self.error = exc


class TalkingPeer(_Peer):
    def answers(self) -> bool:
        return True


class SilentPeer(_Peer):
    """Accepts every request and answers none. The case a deadline exists for."""

    def answers(self) -> bool:
        return False


class ADeadlineIsActuallyWaited(unittest.TestCase):

    def _run(self, deadline):
        config, model = configured(2)
        parts = slices_for(config.d_model, 2)
        listener = socket.socket()
        listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        listener.bind(("127.0.0.1", 0))
        port = listener.getsockname()[1]
        listener.close()

        network = Network(config, 2, model.wv, model.wo, port=port,
                          spawn=False, combine="vote")
        talker = TalkingPeer("127.0.0.1", port, parts[0].lo, parts[0].hi,
                             config.vocab_size)
        silent = SilentPeer("127.0.0.1", port, parts[1].lo, parts[1].hi,
                            config.vocab_size)
        # Started before `__enter__`, which blocks in `accept()` until both
        # connect. `create_connection` retries nothing, so the listener has to be
        # up -- hence the bind-then-close-then-rebind above, which reserves a port
        # the driver can take.
        for peer in (talker, silent):
            peer.start()
        with network as live:
            # TIMED AROUND `run` ALONE, and the first version timed the whole
            # `with` block. Entering the network waits for both peers to connect,
            # which includes their retry sleeps -- measured at 0.585 s against a
            # 1 ms deadline, so setup DOMINATED and the lower-bound assertion
            # below would have passed under the mutation it exists to catch.
            #
            # `test_the_wait_tracks_the_DEADLINE_and_not_fixed_overhead` is what
            # caught that, and it is the reason this line is here rather than four
            # lines earlier. Rule 2: measure the quantity the claim is about.
            started = time.monotonic()
            predictions = live.run(TOKENS, window=1, deadline=deadline)
            elapsed = time.monotonic() - started
        for peer in (talker, silent):
            peer.join(timeout=5)
            if peer.error is not None:
                raise AssertionError(f"peer failed: {peer.error!r}")
        return network, predictions, elapsed, talker, silent

    def test_the_silent_node_is_asked_and_does_not_answer(self):
        # THE COMPANION ASSERTION. Everything below is about a step settling
        # short, and none of it means anything if the silent peer was never in the
        # conversation -- an unreached peer and a well-behaved one look identical
        # from the driver's side.
        _, _, _, talker, silent = self._run(DEADLINE)
        self.assertGreater(silent.requests, 0,
                           "the silent node was never spoken to, so nothing was "
                           "waiting on it and no deadline was under test")
        self.assertGreater(talker.requests, 0)

    def test_every_step_settles_short(self):
        network, _, _, _, _ = self._run(DEADLINE)
        # One of two voters never votes, so every step is short by exactly one.
        self.assertEqual(len(network.steps_settled_short), len(TOKENS))
        self.assertEqual(set(network.steps_settled_short.values()), {1})

    def test_the_run_waits_at_least_one_deadline(self):
        # THE ASSERTION THAT CATCHES `the-deadline-fires-immediately`
        # DETERMINISTICALLY. Under that mutation the overdue test becomes
        # `>= 0`, so each step settles the instant the talker's vote lands and the
        # run finishes in milliseconds.
        #
        # A LOWER bound is the safe direction: a slow or loaded machine only makes
        # this pass more easily. The previous detection was an upper-bound race in
        # a healthy run, which is what made it flaky on a 2-vCPU runner.
        _, _, elapsed, _, _ = self._run(DEADLINE)
        self.assertGreaterEqual(
            elapsed, DEADLINE,
            "the run finished faster than a single deadline, so a step settled "
            "on the first vote instead of waiting -- the answer is then "
            "whichever node replied first")

    def test_the_wait_tracks_the_DEADLINE_and_not_fixed_overhead(self):
        # RULE 10's SENSITIVITY CHECK. `test_the_run_waits_at_least_one_deadline`
        # would pass just as well if the run always took half a second for reasons
        # having nothing to do with the deadline -- process startup, socket setup,
        # anything. So move the input and require the output to move.
        #
        # A near-zero deadline is what the mutation effectively installs, reached
        # here through the public parameter instead of by editing source: every
        # step settles on the talker's vote at once. If the slow arm is not
        # decisively slower than this one, the assertion above is measuring
        # overhead and not the wait.
        _, _, quick, _, _ = self._run(0.001)
        _, _, slow, _, _ = self._run(DEADLINE)
        self.assertLess(quick, DEADLINE,
                        "a 1 ms deadline took longer than a 500 ms one, so "
                        "elapsed time here is dominated by setup rather than by "
                        "the deadline, and the lower-bound assertion is vacuous")
        self.assertGreater(slow, quick)

    def test_it_still_answers(self):
        # Settling short must produce the PARTIAL answer, not `argmax` of a zero
        # vector -- which would be token 0 wearing the appearance of an answer,
        # the case the `votes >= 1` clause exists for.
        _, predictions, _, _, _ = self._run(DEADLINE)
        self.assertEqual(len(predictions), len(TOKENS))
        expected = [int(t) % 14 for t in TOKENS]
        self.assertEqual([int(p) for p in predictions], expected)


if __name__ == "__main__":
    unittest.main()
