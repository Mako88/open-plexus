"""A network using PAIR keys must agree with one process, exactly.

## The gap this closes

Every distributed test used single-token keys, and every relational result was measured
in one process. So nobody asked whether the two fit together, and note 086 found they did
not: `Node.key` derived a key from `(seed, token)`, a pair key needs `(seed, previous,
token)`, and the driver broadcasts one token id. The node built a different address than
the driver assumed and **the split silently stopped being exact** — no error, no crash,
just votes about the wrong arrays.

`PairKeys` is the chosen option for relational work, so until this test existed **no
relational result had ever crossed a wire.**

## Why the fix remembers rather than receives

A node processes tokens in order and already keeps `_previous_key`. Holding the token id
beside it costs one integer. Widening the broadcast would spend note 012's four-byte
finding on every configuration, including the ones that never needed it.

## Non-vacuity

`wo` is learned by the delta rule and starts at zeros, so an untrained model predicts token
0 forever and every node becomes interchangeable — a comparison against it passes whatever
the nodes send. `tests/test_connection_order.py` names this and `tools/cluster_driver.py`
was caught by it. So `wo` is seeded from `wv` here and the fixture asserts the bar actually
varies.
"""

from __future__ import annotations

import socket
import threading
import unittest

import numpy as np

from openplexus.distributed import Network, slices_for
from openplexus.keys import PairKeys
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH, NODES = 24, 16, 4
TOKENS = np.random.default_rng(11).integers(0, VOCAB, 48)


def config(context_keys: bool) -> LocalMemoryConfig:
    return LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, lr=0.05,
                             key_scale=0.5, decay=0.9, derived_keys=True,
                             context_keys=context_keys, seed=5)


def model_for(settings: LocalMemoryConfig) -> LocalAssociativeMemory:
    model = LocalAssociativeMemory(settings)
    # See the module docstring: without this the comparison cannot fail.
    model.wo[:] = model.wv
    return model


def free_port() -> int:
    """A port chosen before the driver starts.

    `Network` binds in `__enter__`, not `__init__`, so `net.port` is unset until the
    driver thread has entered — and reading it first sends every node to port 0.
    `tests/test_deadline_settles_short.py` passes an explicit port for the same
    reason.
    """
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 0))
        return int(probe.getsockname()[1])


def drive(context_keys: bool) -> tuple[np.ndarray, np.ndarray]:
    """Run the same tokens through a network and through one process."""
    settings = config(context_keys)
    model = model_for(settings)
    wanted = model_for(settings).run(TOKENS)

    port = free_port()
    net = Network(settings, NODES, model.wv, model.wo, port=port, spawn=False)
    box: dict = {}

    def driver() -> None:
        with net as running:
            box["got"] = running.run(TOKENS, window=1)

    thread = threading.Thread(target=driver, daemon=True)
    thread.start()
    # `Network.__enter__` blocks until every node connects, so the nodes cannot be
    # started from inside its own `with`.
    from openplexus.distributed import serve
    workers = []
    for own in slices_for(WIDTH, NODES):
        worker = threading.Thread(
            target=serve,
            args=(settings, own, "127.0.0.1", port,
                  model.wv[:, own.lo:own.hi].copy(),
                  model.wo[:, own.lo:own.hi].copy()),
            daemon=True)
        worker.start()
        workers.append(worker)
    thread.join(timeout=60)
    return box["got"], wanted


class PairKeysSurviveTheTransport(unittest.TestCase):

    def test_the_fixture_can_fail(self):
        """A bar that predicts one token forever passes whatever the nodes send."""
        wanted = model_for(config(True)).run(TOKENS)
        self.assertGreaterEqual(
            len(set(wanted.tolist())), 3,
            "the single-process bar must vary, or exactness is vacuous")

    def test_pair_keys_are_exact_across_a_network(self):
        """The load-bearing one. Note 086 measured this FALSE before the fix."""
        got, wanted = drive(context_keys=True)
        np.testing.assert_array_equal(
            got, wanted,
            "a pair-keyed network disagreed with one process, which is note 086's "
            "defect: the node cannot see `previous` and builds a different address")

    def test_single_token_keys_are_still_exact(self):
        """The control, and the arrangement every earlier number was taken under."""
        got, wanted = drive(context_keys=False)
        np.testing.assert_array_equal(got, wanted)


class TheNodeRebuildsTheSameKeyAsPairKeys(unittest.TestCase):
    """Asserted directly, because the network test would pass if BOTH sides were
    wrong in the same way — and they share a seed, so that is a real possibility."""

    def test_the_key_matches_PairKeys_pair_exactly(self):
        from openplexus.distributed import Node, Slice
        settings = config(True)
        model = model_for(settings)
        node = Node(settings, Slice(0, WIDTH), model.wv, model.wo)
        # `local_memory` builds `PairKeys(seed, spread, d, vocab_size)`, so
        # `vocab_size` is what stands in for "no previous token".
        reference = PairKeys(settings.seed,
                             settings.key_scale / np.sqrt(WIDTH),
                             WIDTH, settings.vocab_size)
        previous = settings.vocab_size
        for token in (3, 7, 7, 0, 19):
            np.testing.assert_allclose(node.key(token),
                                       reference.pair(previous, token))
            node.step(token)
            previous = token

    def test_reset_clears_the_remembered_token(self):
        """Otherwise the next sequence's first key depends on the last one's end."""
        from openplexus.distributed import Node, Slice
        settings = config(True)
        model = model_for(settings)
        node = Node(settings, Slice(0, WIDTH), model.wv, model.wo)
        node.step(5)
        first_after_reset = None
        node.reset()
        first_after_reset = node.key(9)
        fresh = Node(settings, Slice(0, WIDTH), model.wv, model.wo)
        np.testing.assert_allclose(first_after_reset, fresh.key(9))


if __name__ == "__main__":
    unittest.main()
