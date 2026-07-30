"""A read that goes to the one node holding the fact, with no driver.

`distributed.Network`'s driver *"broadcasts a token and sums whatever comes back, which is
the only reduction in the system"* — the collective amended C1 forbids. Containers measured
its cost directly: at 16 nodes a window of 8 was slower than a window of 4, because the
driver must collect every vote before the window advances.

`ConceptStore.read` already promises the alternative — *"Read from ONE surviving holder. No
pooling, no vote, no barrier"* — and `openplexus/peer.py` is that over a socket.

## What is asserted

- **Exactness.** A peer-served read equals the in-process one, bit for bit.
- **That the ROUTING is what makes it work.** A deliberately misrouted read must NOT match.
  Without that control the first version of this measurement passed while `owner()` was
  effectively the identity, because the test handed it an already-computed owner.
- **No race by construction.** `ConceptPeer` binds and listens in `__init__`, before
  `start()`, so a caller may connect immediately. That is deliberate: binding inside the
  serving thread is what made `tests/test_pair_keys_distributed.py` race its own driver and
  fail in CI while passing locally.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.keys import PairKeys
from openplexus.ownership import Ring
from openplexus.partitioned import ConceptStore
from openplexus.peer import ConceptPeer, RemoteConcepts

WIDTH, VOCAB, NODES = 64, 40, 4
FACT = 0
#: Writer and reader must agree on who owns what, and they agree by using the SAME
#: ring parameters rather than by coordinating. `RemoteConcepts` builds `Ring(len(peers),
#: seed=seed)`, so a fixture that wrote by `concept % NODES` disagreed with it — which is
#: exactly the failure the ring exists to prevent, and it showed up as four broken tests
#: the moment the modulo placeholder was replaced.
RING_SEED = 0
#: `(entity, relation, object)`, chosen so several entities share a relation and
#: several relations share an entity — otherwise a routing bug that keyed on the
#: wrong element of the pair would still land correctly.
FACTS = tuple((1 + i, 20 + (i % 8), 1 + ((i + 3) % 12)) for i in range(24))


def fixture():
    rng = np.random.default_rng(0)
    values = rng.normal(0.0, 1.0, (VOCAB, WIDTH))
    values /= np.linalg.norm(values, axis=1, keepdims=True)
    keys = PairKeys(seed=1, spread=1.0 / np.sqrt(WIDTH), width=WIDTH, start=VOCAB,
                    route="first-concept", markers=frozenset({FACT}))
    stores = [ConceptStore(nodes=1, width=WIDTH, seed=0, replicas=1)
              for _ in range(NODES)]
    ring = Ring(NODES, seed=RING_SEED)
    for entity, relation, obj in FACTS:
        concept = keys.owner(entity, relation)
        stores[ring.owner(concept)].write(concept, keys.pair(entity, relation),
                                          values[obj])
    return values, keys, stores


class APeerServesTheConceptsItOwns(unittest.TestCase):

    def setUp(self):
        self.values, self.keys, self.stores = fixture()
        self.peers = [ConceptPeer(self.stores[i], self.keys).start()
                      for i in range(NODES)]
        self.remote = RemoteConcepts(
            {i: ("127.0.0.1", self.peers[i].port) for i in range(NODES)},
            WIDTH, self.keys, seed=RING_SEED)

    def tearDown(self):
        self.remote.close()
        for peer in self.peers:
            peer.close()

    def test_the_writer_and_the_reader_agree_on_every_owner(self):
        """The invariant everything else rests on, asserted directly.

        A write that lands on one peer and a read that asks another returns zeros, and a
        zero vector decodes to whatever the readout prefers -- an answer, not an error.
        """
        ring = Ring(NODES, seed=RING_SEED)
        for entity, relation, _ in FACTS:
            concept = self.keys.owner(entity, relation)
            self.assertEqual(ring.owner(concept), self.remote.owner(concept))

    def test_a_served_read_equals_the_in_process_one(self):
        for entity, relation, _ in FACTS:
            concept = self.keys.owner(entity, relation)
            local = self.stores[self.remote.owner(concept)].read(
                concept, self.keys.pair(entity, relation))
            np.testing.assert_allclose(
                self.remote.read(concept, entity, relation), local, atol=1e-9)

    def test_a_served_read_decodes_to_the_right_token(self):
        """Exactness against a zero vector would also be 'exact'."""
        for entity, relation, obj in FACTS:
            got = self.remote.read(self.keys.owner(entity, relation),
                                   entity, relation)
            self.assertEqual(int(np.argmax(self.values @ got)), obj)

    def _elsewhere(self, concept: int) -> int:
        """A concept the ring sends to a DIFFERENT peer.

        `concept + 1` was the first version and it stopped being a misroute the moment
        the modulo placeholder became a ring: consistent hashing puts adjacent concepts
        on the same peer most of the time, so three of twenty-four "misrouted" reads
        went to the right peer and matched. **A control has to be expressed in the
        quantity it is controlling** — the peer — not in an arithmetic that happened to
        change it.
        """
        mine = self.remote.owner(concept)
        for offset in range(1, 1000):
            if self.remote.owner(concept + offset) != mine:
                return concept + offset
        raise AssertionError("the ring sends every concept to one peer")

    def test_MISROUTING_breaks_it(self):
        """The control. Without this, `owner()` could be the identity and every
        assertion above would still pass — which is what the first version of this
        measurement did, because it was handed an owner rather than a concept.
        """
        matched = 0
        for entity, relation, _ in FACTS:
            concept = self.keys.owner(entity, relation)
            local = self.stores[self.remote.owner(concept)].read(
                concept, self.keys.pair(entity, relation))
            elsewhere = self.remote.read(self._elsewhere(concept),
                                         entity, relation)
            matched += bool(np.allclose(elsewhere, local, atol=1e-9))
        self.assertEqual(matched, 0,
                         "a misrouted read still matched, so the routing is not "
                         "what produces the answer")

    def test_one_request_and_one_answer_per_read(self):
        """The point of removing the driver: 2 messages rather than 2N."""
        before = self.remote.reads
        for entity, relation, _ in FACTS:
            self.remote.read(self.keys.owner(entity, relation), entity, relation)
        self.assertEqual(self.remote.reads - before, len(FACTS))


class ABeamTraversalRunsWithoutADriver(unittest.TestCase):
    """The integration note 093 named as missing: a WALK with no reduction anywhere.

    `search` takes `reader=` so a caller holding sockets can inject routing without
    `search` importing a transport. Every read in the walk goes to the one peer owning
    the concept.
    """

    #: A chain 2-20->3-21->4-22->5 with a branch off 2, so the beam has a choice to
    #: get wrong. Out-degree 1 would hide a routing fault, which is how decision 108's
    #: missing-search capability stayed hidden.
    WALK_FACTS = ((2, 20, 3), (3, 21, 4), (4, 22, 5), (2, 23, 6), (6, 21, 7))
    RELS = (20, 21, 22, 23)

    def setUp(self):
        rng = np.random.default_rng(3)
        self.values = rng.normal(0.0, 1.0, (VOCAB, WIDTH))
        self.values /= np.linalg.norm(self.values, axis=1, keepdims=True)
        self.keys = PairKeys(seed=1, spread=1.0 / np.sqrt(WIDTH), width=WIDTH,
                             start=VOCAB, route="first-concept",
                             markers=frozenset({FACT}))
        self.whole = np.zeros((WIDTH, WIDTH))
        self.stores = [ConceptStore(nodes=1, width=WIDTH, seed=0, replicas=1)
                       for _ in range(NODES)]
        for entity, relation, obj in self.WALK_FACTS:
            for previous, token, value in (
                    (FACT, entity, relation), (entity, relation, obj)):
                key = self.keys.pair(previous, token)
                self.whole += np.outer(self.values[value], key)
                concept = self.keys.owner(previous, token)
                self.stores[Ring(NODES, seed=RING_SEED).owner(concept)].write(
                    concept, key, self.values[value])
        self.peers = [ConceptPeer(self.stores[i], self.keys).start()
                      for i in range(NODES)]
        self.remote = RemoteConcepts(
            {i: ("127.0.0.1", self.peers[i].port) for i in range(NODES)},
            WIDTH, self.keys, seed=RING_SEED)

    def tearDown(self):
        self.remote.close()
        for peer in self.peers:
            peer.close()

    def _walk(self, reader):
        from openplexus.retrieval import SuperposedRead
        from openplexus.search import beam
        walks = beam(None if reader else self.whole, SuperposedRead(), self.keys,
                     self.values, FACT, 2, self.values[5], 3, width=2, branches=2,
                     allowed=np.array(self.RELS), reader=reader)
        return walks[0].relations if walks else None

    def test_the_driver_free_walk_equals_the_in_process_one(self):
        def routed(previous, token):
            return self.remote.read(self.keys.owner(previous, token),
                                    previous, token)
        self.assertEqual(self._walk(routed), self._walk(None))

    def test_the_walk_is_the_true_chain(self):
        """Equality against a wrong walk would also be equality."""
        self.assertEqual(self._walk(None), (20, 21, 22))

    def test_MISROUTING_changes_the_walk(self):
        """So the routing is what produces it, not the fixture."""
        def misrouted(previous, token):
            # A DIFFERENT PEER, not a different concept: with a ring, adjacent
            # concepts usually share an owner, so `+1` is often not a misroute.
            concept = self.keys.owner(previous, token)
            mine = self.remote.owner(concept)
            other = next(concept + k for k in range(1, 1000)
                         if self.remote.owner(concept + k) != mine)
            return self.remote.read(other, previous, token)
        self.assertNotEqual(self._walk(misrouted), self._walk(None))


class ThePeerIsListeningBeforeItIsStarted(unittest.TestCase):
    """Binding in `__init__` is the fix for the race that failed CI once already."""

    def test_the_port_is_known_and_bound_before_start(self):
        _, keys, stores = fixture()
        peer = ConceptPeer(stores[0], keys)
        try:
            self.assertGreater(peer.port, 0)
            # Connectable before `start()`: the listen backlog accepts, and the
            # serving thread picks it up when it runs. So a caller never has to
            # guess how wide the window is.
            import socket
            with socket.create_connection(("127.0.0.1", peer.port), timeout=2):
                pass
        finally:
            peer.close()


if __name__ == "__main__":
    unittest.main()
