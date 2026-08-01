"""Starting one node, and refusing to start a node nobody can reach.

`openplexus/node_main.py` is the entry point whose absence left `bucket_peer`,
`federated` and `deployment` with no caller at all. These fix the two things a
launcher depends on:

- **a peer list is parsed or rejected, never half-read** — a node silently
  missing one peer answers every read it owns and quietly fails the rest, which
  reads as a partition rather than as a typo;
- **it announces its real port before it blocks** — `BucketPeer` binds port 0 by
  default and only learns its port after construction, so a launcher that
  cannot read the port has to sleep and hope, and then reports a startup race
  as a failed read.
"""

from __future__ import annotations

import argparse
import contextlib
import io
import pathlib
import sys
import threading
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus import node_main  # noqa: E402
from openplexus.bucket_peer import ask  # noqa: E402


class APeerListIsParsedOrRejected(unittest.TestCase):

    def test_it_reads_every_entry(self):
        got = node_main.addresses("0=127.0.0.1:8100,1=10.0.0.2:9000")
        self.assertEqual(got, {0: ("127.0.0.1", 8100), 1: ("10.0.0.2", 9000)})

    def test_the_result_FOLLOWS_the_text(self):
        """The connection test. A parser returning a fixed dict would pass the
        test above."""
        one = node_main.addresses("0=127.0.0.1:8100")
        two = node_main.addresses("0=127.0.0.1:8101")
        self.assertNotEqual(one, two)

    def test_a_malformed_entry_is_refused_not_skipped(self):
        for bad in ("0=127.0.0.1", "=127.0.0.1:1", "x=127.0.0.1:1",
                    "0=127.0.0.1:port", ""):
            with self.assertRaises(argparse.ArgumentTypeError, msg=bad):
                node_main.addresses(bad)


class ItRefusesToStartANodeNobodyCanReach(unittest.TestCase):

    def refuses(self, argv):
        """argparse writes usage to stderr on exit; a noisy suite gets skimmed."""
        with contextlib.redirect_stderr(io.StringIO()):
            with self.assertRaises(SystemExit):
                node_main.main(argv)

    def test_a_node_missing_from_its_own_peer_list(self):
        self.refuses(["--node", "1", "--nodes", "2",
                      "--peers", "0=127.0.0.1:8100"])

    def test_a_peer_list_that_does_not_match_the_network(self):
        self.refuses(["--node", "0", "--nodes", "3",
                      "--peers", "0=127.0.0.1:8100"])


class ItServesOverASocket(unittest.TestCase):
    """The one that matters: a real listener answering a real connection.

    Port 0, so this cannot collide with anything else on the machine.
    """

    def test_it_announces_a_port_and_answers_on_it(self):
        from openplexus.bucket_peer import BucketPeer
        from openplexus.bucket_service import BucketService
        from openplexus.buckets import BucketConfig

        config = BucketConfig(width=8, nodes=1, seed=0)
        peer = BucketPeer(BucketService(0, config), {}, port=0)
        peer.addresses[0] = ("127.0.0.1", peer.port)
        peer.start()
        try:
            self.assertGreater(peer.port, 0)
            reply = ask("127.0.0.1", peer.port, ("no-such-message",),
                        timeout=5.0)
            # A REPLY AT ALL is the assertion. The content is bucket_peer's
            # business; that something answered is this file's.
            self.assertIsNotNone(reply)
        finally:
            peer.close()
        # AND THE COMPANION: it must actually stop. A peer that keeps answering
        # after close is a node that keeps serving after it departs.
        with self.assertRaises(Exception):
            ask("127.0.0.1", peer.port, ("no-such-message",), timeout=1.0)


if __name__ == "__main__":
    unittest.main()
