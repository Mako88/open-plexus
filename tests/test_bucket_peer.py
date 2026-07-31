"""The messages cross a real socket, and a lost one is not a zero.

`test_bucket_service.py` drives the protocol in one process, which is where the
protocol should be exercised. This asserts the thin part: that wrapping a socket
around the service changed nothing, and that the two ways a network can lie —
a refusal that looks like an answer, and a write that silently never arrived —
both fail loudly.

Sockets bind to port 0 on loopback, so nothing here depends on a fixed port
being free.
"""

from __future__ import annotations

import unittest

from openplexus.bucket_peer import BucketPeer, ask
from openplexus.bucket_service import BucketService
from openplexus.buckets import BucketConfig, observations
from openplexus.grounding import CoOccurrence
from openplexus.tasks.occasions import OccasionConfig, generate

CONFIG = BucketConfig(width=50, nodes=4, observers=3, seed=0)


class _Network:
    """Peers on loopback, wired to each other."""

    def __init__(self, config: BucketConfig = CONFIG) -> None:
        self.services = [BucketService(n, config) for n in range(config.nodes)]
        self.addresses: dict[int, tuple[str, int]] = {}
        self.peers = []
        for service in self.services:
            peer = BucketPeer(service, self.addresses, host="127.0.0.1").start()
            self.peers.append(peer)
            self.addresses[service.node] = ("127.0.0.1", peer.port)
        for peer in self.peers:
            peer.addresses.update(self.addresses)

    def owner(self, key: int) -> int:
        return self.services[0].owner(key)

    def send(self, key: int, message: tuple):
        host, port = self.addresses[self.owner(key)]
        return ask(host, port, message)

    def close(self) -> None:
        for peer in self.peers:
            peer.close()


class ItCrossesAWire(unittest.TestCase):

    def setUp(self) -> None:
        self.network = _Network()
        self.addCleanup(self.network.close)

    def test_a_write_and_a_read_round_trip(self):
        mine = next(k for k in range(500) if self.network.owner(k) == 0)
        self.network.send(mine, ("NOTE", mine))
        self.assertEqual(self.network.send(mine, ("SEEN", mine)), 1)

    def test_every_peer_bound_a_DIFFERENT_port(self):
        """The companion to everything else here: peers sharing a port would
        make one service answer for all of them and every count would still
        come out right."""
        ports = {peer.port for peer in self.network.peers}
        self.assertEqual(len(ports), len(self.network.peers))

    def test_a_refusal_arrives_as_a_refusal_and_not_as_an_answer(self):
        """Asking the wrong owner must not return a number.

        `peer.py` has the same hazard and answers it with a fingerprint: there a
        diverged peer returns zeros that decode to a real token. Here a wrong
        owner would return 0 for `SEEN`, which is a perfectly ordinary count.
        """
        stranger = next(k for k in range(500) if self.network.owner(k) != 0)
        host, port = self.network.addresses[0]
        reply = ask(host, port, ("SEEN", stranger))
        self.assertIsInstance(reply, dict)
        self.assertIn("refused", reply)


class ItForwardsWhatItEmits(unittest.TestCase):
    """A bucket owner's flush has to reach the surface owners over the wire."""

    def setUp(self) -> None:
        self.network = _Network()
        self.addCleanup(self.network.close)
        self.occasions = OccasionConfig(concepts=8, surfaces=3, presence=0.7,
                                        noise=2, distractors=1, occasions=90,
                                        seed=0)
        self.stream = generate(self.occasions)
        self._drive()

    def _drive(self) -> None:
        closes: dict[int, int] = {}

        def advance(now: int | None) -> None:
            due = [b for b, shut in closes.items() if now is None or shut < now]
            for bucket in sorted(due):
                closes.pop(bucket)
                self.network.send(bucket, ("FLUSH", bucket))

        for observation in observations(self.stream, CONFIG, tempo=100):
            advance(observation.when)
            bucket = observation.when // CONFIG.width
            closes[bucket] = (bucket + 1) * CONFIG.width
            self.network.send(bucket, ("OBSERVE", bucket, observation.surface,
                                       observation.when))
        advance(None)

    def test_the_counts_match_one_process_exactly(self):
        single = CoOccurrence()
        for occasion in self.stream:
            single.observe(occasion.surfaces)
        for surface in single.surfaces():
            self.assertEqual(self.network.send(surface, ("SEEN", surface)),
                             single.seen(surface), f"surface {surface}")

    def test_messages_really_were_forwarded_between_peers(self):
        """Without this, a run where every surface happened to be owned by the
        bucket's own node would pass the test above having sent nothing."""
        self.assertGreater(sum(p.forwarded for p in self.network.peers), 0)

    def test_no_peer_holds_a_row_it_does_not_own(self):
        for peer in self.network.peers:
            for surface in peer.service.index.rows():
                self.assertEqual(self.network.owner(surface),
                                 peer.service.node)


    def test_nothing_was_lost_in_transit(self):
        """`forwarded` alone cannot say this — a peer that dropped half its
        pushes would still report a positive count."""
        for peer in self.network.peers:
            self.assertEqual(peer.failures, [], f"peer {peer.service.node}")


class WhenThePeerIsGone(unittest.TestCase):

    def test_asking_a_closed_peer_RAISES(self):
        """A message that did not arrive must not read as a zero — the same
        rule `bucket_service.rank` enforces for a missing marginal."""
        network = _Network()
        host, port = network.addresses[0]
        network.close()
        with self.assertRaises(OSError):
            ask(host, port, ("SEEN", 0), timeout=1.0)

    def test_a_push_to_a_dead_peer_is_RECORDED_rather_than_vanishing(self):
        """The failure a server thread cannot raise its way out of.

        Forwarding runs with no caller to catch anything, so an exception would
        print a traceback, lose the message, and leave the run looking healthy.
        The first version of `_forward` did exactly that — the traceback was in
        the test log and every test still passed.
        """
        network = _Network()
        self.addCleanup(network.close)
        victim = 1
        network.peers[victim].close()

        peer = network.peers[0]
        peer.service.sent.append((victim, ("NOTE", 0)))
        peer._forward(*peer.service.take()[0])       # noqa: SLF001 - the unit

        self.assertEqual(peer.forwarded, 0)
        self.assertEqual(len(peer.failures), 1)
        self.assertEqual(peer.failures[0][0], victim)


if __name__ == "__main__":
    unittest.main()
