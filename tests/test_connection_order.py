"""A node's identity comes from what it says, not from when it arrives.

The driver's accept loop carried a comment saying it asked each node who it was.
**It did not.** Connections were kept in accept order and then indexed as though
that were slice order.

Nothing caught it for a long time, and the reason is worth stating: summing votes
is order-independent, so the bit-identity tests -- the strongest checks in this
file's neighbourhood -- cannot see a permutation at all. It only bites where a
node is named BY INDEX, which is `absent`, i.e. every departure and churn result.
On loopback, accept order is usually spawn order and usually right.

Under a link with delay it is neither. The wrong slice departs, the run completes,
and the number looks entirely plausible. These tests connect the nodes in reverse
so the two orders cannot agree by luck.
"""

from __future__ import annotations

import threading
import time
import unittest

import numpy as np

from openplexus.distributed import Network, serve, slices_for
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH, NODES = 24, 16, 4
TOKENS = np.random.default_rng(11).integers(0, VOCAB, 40)
STAGGER = 0.05          # long enough that arrival order is the start order


def config() -> LocalMemoryConfig:
    return LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, lr=0.05,
                             key_scale=0.5, decay=0.9, derived_keys=True,
                             seed=5)


def drive(order, absent=None, leave_at=None):
    """Run a network whose nodes connect in `order` rather than in slice order.

    The driver runs in a thread because `__enter__` blocks until every node has
    connected -- so the connections cannot be made from inside its own `with`.
    Nodes are started one at a time with a pause between them, which is what
    makes arrival order a fixture instead of a race.
    """
    model = LocalAssociativeMemory(config())
    # `wo` is learned by the delta rule and starts at zeros, so an untrained
    # model scores every token 0 and predicts token 0 forever. Every node would
    # then be interchangeable and a departure would change nothing -- which is
    # exactly what the vacuity guard below caught on the first attempt.
    model.wo[:] = model.wv
    slices = slices_for(WIDTH, NODES)
    net = Network(config(), NODES, model.wv, model.wo, spawn=False)
    box: dict = {}

    def driver():
        try:
            with net:
                box["result"] = net.run(TOKENS, absent=absent, leave_at=leave_at)
        except BaseException as error:          # surfaced by the caller
            box["error"] = error

    thread = threading.Thread(target=driver, daemon=True)
    thread.start()
    while net.port == 0 and "error" not in box:
        time.sleep(0.01)

    workers = []
    for index in order:
        own = slices[index]
        worker = threading.Thread(
            target=serve,
            args=(config(), own, "127.0.0.1", net.port,
                  model.wv[:, own.lo:own.hi].copy(),
                  model.wo[:, own.lo:own.hi].copy()),
            daemon=True)
        worker.start()
        workers.append(worker)
        time.sleep(STAGGER)

    thread.join(timeout=30)
    for worker in workers:
        worker.join(timeout=5)
    if "error" in box:
        raise box["error"]
    return box["result"]


class ArrivalOrderIsNotIdentity(unittest.TestCase):

    def test_reversed_arrival_still_matches_forward_arrival(self):
        """Weak on its own -- a permuted sum is still the same sum."""
        np.testing.assert_array_equal(drive(range(NODES)),
                                      drive(list(reversed(range(NODES)))))

    def test_the_named_node_departs_whatever_order_it_connected_in(self):
        """The test the bug would have failed.

        Node 0 leaves. Under the old code, with nodes connecting in reverse, the
        driver dropped whichever connection arrived first -- node 3 -- so these
        two runs would disagree while both looked like ordinary results.
        """
        np.testing.assert_array_equal(
            drive(range(NODES), absent={0}, leave_at=10),
            drive(list(reversed(range(NODES))), absent={0}, leave_at=10),
            "a departure depended on the order the nodes happened to connect "
            "in, so `absent` is naming an arrival rather than a slice")

    def test_departing_different_nodes_gives_different_answers(self):
        """Without this, the test above passes on a departure that does nothing.

        If the slices were interchangeable, dropping any of them would give the
        same predictions and the check above would be vacuous.
        """
        self.assertFalse(
            np.array_equal(drive(range(NODES), absent={0}, leave_at=10),
                           drive(range(NODES), absent={3}, leave_at=10)),
            "losing node 0 and losing node 3 gave identical predictions, so "
            "this fixture cannot tell which slice departed")


class TheDriverRefusesAnUnexpectedSlice(unittest.TestCase):

    def test_a_node_announcing_a_slice_that_was_not_asked_for_is_rejected(self):
        """Silently accepting it would mean a network of the wrong shape."""
        model = LocalAssociativeMemory(config())
        model.wo[:] = model.wv
        net = Network(config(), 2, model.wv, model.wo, spawn=False)
        box: dict = {}

        def driver():
            try:
                with net:
                    box["result"] = net.run(TOKENS)
            except BaseException as error:
                box["error"] = error

        thread = threading.Thread(target=driver, daemon=True)
        thread.start()
        while net.port == 0 and "error" not in box:
            time.sleep(0.01)

        halves = slices_for(WIDTH, 2)
        quarter = slices_for(WIDTH, 4)[0]       # a quarter, where halves are due
        for own in (halves[0], quarter):
            threading.Thread(
                target=lambda o=own: serve(
                    config(), o, "127.0.0.1", net.port,
                    model.wv[:, o.lo:o.hi].copy(),
                    model.wo[:, o.lo:o.hi].copy()),
                daemon=True).start()
            time.sleep(STAGGER)

        thread.join(timeout=30)
        self.assertIsInstance(box.get("error"), ValueError)


if __name__ == "__main__":
    unittest.main()
