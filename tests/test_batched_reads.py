"""A hop's reads share one round trip, and the two ways that goes quietly wrong.

`d_max` bounds a WALK, so the quantity that binds is how many round trips are strung end
to end — not how many reads happen. Note 100 corrected two of my own notes for counting
the wrong one, and `tools/walk_rounds.py` is the arithmetic. A hop's `width` reads are
independent of each other, all their pairs known before any is issued, so they belong in
one request.

Both failure modes here are silent:

- **Positional misassignment.** Answers pair to requests by position, so one
  wrong-length reply hands every later answer to the wrong read. The walk does not
  fail — it goes somewhere else and reports a number.
- **A batch that is not a batch.** A `read_many` that looped over `read` would pass
  every equality test and buy nothing, which is precisely the situation note 100
  describes. So `rounds` is asserted and not only the answers.

The fixture is imported rather than rebuilt: `tests/test_peer_reads.py` already
constructs peers, stores and a ring that agree with each other, and a second copy is
where the two would drift apart.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.peer import ConceptPeer, RemoteConcepts
from tests.test_peer_reads import (
    FACTS, NODES, REPLICAS, RING_SEED, WIDTH, fixture)


class AHopsReadsShareOneRoundTrip(unittest.TestCase):
    def setUp(self):
        self.values, self.keys, self.stores = fixture()
        self.peers = [ConceptPeer(self.stores[i], self.keys, peers=NODES,
                                  seed=RING_SEED).start() for i in range(NODES)]
        self.remote = RemoteConcepts(
            {i: ("127.0.0.1", self.peers[i].port) for i in range(NODES)},
            WIDTH, self.keys, seed=RING_SEED, replicas=REPLICAS)

    def tearDown(self):
        self.remote.close()
        for peer in self.peers:
            peer.close()

    def _requests(self):
        return [(self.keys.owner(entity, relation), entity, relation)
                for entity, relation, _ in FACTS]

    def test_a_batch_equals_the_same_reads_one_at_a_time(self):
        """Positional pairing is right, across peers and in the order asked."""
        requests = self._requests()
        batched = self.remote.read_many(requests)
        self.assertEqual(len(batched), len(requests))
        for answer, (concept, previous, token) in zip(batched, requests):
            np.testing.assert_allclose(
                answer, self.remote.read(concept, previous, token), atol=1e-12)

    def test_the_batch_spans_SEVERAL_peers(self):
        """Otherwise pairing across separate replies is never exercised at all."""
        owners = {self.remote.owner(concept) for concept, _, _ in self._requests()}
        self.assertGreater(
            len(owners), 1,
            "every request landed on one peer, so the case that can misassign -- a "
            "batch split across replies -- never happened and the test above is weak")

    def test_a_batch_costs_ONE_round_and_not_one_per_read(self):
        requests = self._requests()
        before = self.remote.rounds
        self.remote.read_many(requests)
        self.assertEqual(
            self.remote.rounds - before, 1,
            f"{len(requests)} reads took more than one round, so `read_many` is a "
            f"loop over `read` and note 100's arithmetic is unchanged by it")

    def test_every_read_is_still_counted(self):
        """`rounds` is the new quantity and `reads` the old one. Both must be true."""
        before = self.remote.reads
        self.remote.read_many(self._requests())
        self.assertEqual(self.remote.reads - before, len(FACTS))

    def test_a_WRONG_LENGTH_reply_is_refused_rather_than_misassigned(self):
        """The failure with no symptom, made loud."""
        requests = self._requests()[:3]
        original = self.remote.width
        try:
            # A caller expecting a different width mispairs every answer after the
            # first. Refusing is the only readable outcome; a short read is not an
            # absence, it means the peer answered a different question.
            self.remote.width = original + 1
            with self.assertRaises(ValueError) as raised:
                self.remote.read_many(requests)
            self.assertIn("BY POSITION", str(raised.exception))
        finally:
            self.remote.width = original

    def test_a_batch_SURVIVES_a_holder_leaving(self):
        """C3 says peers go. A batch must fail over the way a single read does.

        Asserted by DECODE and not by bit-equality against the pre-departure answer,
        which was the first version of this test and failed 6 of 24. The reason is worth
        keeping: a replica holds a **different set of concepts** than the owner, its
        store superposes them into one matrix, so the same key read from a replica
        carries different interference. That is not a failover fault — the binding is
        there and it decodes correctly — and it is why every other failover test in this
        suite asserts the token rather than the vector.
        """
        requests = self._requests()
        gone = self.remote.owner(requests[0][0])
        self.peers[gone].close()
        self.remote._drop(gone)
        after = self.remote.read_many(requests)
        recovered = sum(int(np.argmax(self.values @ answer)) == obj
                        for answer, (_, _, obj) in zip(after, FACTS))
        self.assertEqual(
            recovered, len(requests),
            f"{len(requests) - recovered} of {len(requests)} reads stopped decoding "
            f"to the right object after one holder left, so the batch does not fail "
            f"over and C3 is not met on this path")

    def test_the_DEPARTURE_is_what_the_test_above_survives(self):
        """A control: with no replica to fall back to, the same reads must break.

        Without it, `test_a_batch_SURVIVES_a_holder_leaving` would pass just as well if
        killing a peer did nothing at all — which is the vacuous-control failure this
        project has now recorded three times.
        """
        requests = self._requests()
        solo = RemoteConcepts(self.remote.peers, WIDTH, self.keys,
                              seed=RING_SEED, replicas=1)
        try:
            gone = solo.owner(requests[0][0])
            self.peers[gone].close()
            solo._drop(gone)
            solo.read_many(requests)
            self.assertGreater(
                solo.absent, 0,
                "killing the only holder cost nothing, so the peer did not actually "
                "depart and the failover test above proves nothing")
        finally:
            solo.close()

    def test_a_FAILED_holder_costs_a_round_and_not_the_answer(self):
        """Stated in `read_many`'s docstring as a real cost, so it is measured."""
        requests = self._requests()
        self.remote.read_many(requests)
        gone = self.remote.owner(requests[0][0])
        self.peers[gone].close()
        self.remote._drop(gone)
        before = self.remote.rounds
        self.remote.read_many(requests)
        self.assertEqual(
            self.remote.rounds - before, 2,
            "a batch with one dead holder should cost exactly one extra round: the "
            "first for the live holders, the second for the group that missed")


if __name__ == "__main__":
    unittest.main()
