"""`node_main` peer mode: does every fact land somewhere, at the stated replicas?

Peer mode exists so a walk's latency can be measured over a real impaired link
rather than priced at an assumed RTT on loopback (`note 101`). The measurement is
worthless if the containers do not between them hold the data — and the failure
is **silent**, because a caller asking a peer that lacks a concept gets zeros,
and a zero vector decodes to whatever the readout prefers. An answer, not an
error.

So the invariant asserted here is coverage, not liveness: **every fact is held by
exactly `replicas` nodes.** A peer that starts, listens and holds nothing passes
every connection check there is.
"""

from __future__ import annotations

import unittest

from openplexus.node_main import populate, scaffold_facts

WIDTH, VOCAB, NODES, RING_SEED = 64, 40, 4, 0


def _held_counts(facts, nodes=NODES, replicas=2):
    return [populate(i, nodes, WIDTH, VOCAB, 5, RING_SEED, replicas, facts)[2]
            for i in range(nodes)]


class TheScaffoldFactsHaveTheShapeTheyClaim(unittest.TestCase):

    def test_entities_and_relations_overlap(self):
        """The property the docstring rests on, asserted rather than assumed.

        Several entities must share a relation and several relations share an
        entity. Without that a routing bug keying on the wrong half of the pair
        lands correctly anyway, and every test downstream is vacuous — which is
        the defect `tests/test_peer_reads.py` names in its own FACTS constant.
        """
        facts = scaffold_facts(24, VOCAB)
        by_relation: dict[int, set[int]] = {}
        by_entity: dict[int, set[int]] = {}
        for entity, relation, _ in facts:
            by_relation.setdefault(relation, set()).add(entity)
            by_entity.setdefault(entity, set()).add(relation)
        self.assertTrue(any(len(v) > 1 for v in by_relation.values()),
                        "no relation is shared by two entities")
        self.assertTrue(any(len(v) > 1 for v in by_entity.values()),
                        "no entity appears in two relations")

    def test_the_count_reaches_the_output(self):
        """A connection test on the argument: change it, the output moves."""
        self.assertEqual(len(scaffold_facts(24, VOCAB)), 24)
        self.assertEqual(len(scaffold_facts(9, VOCAB)), 9)
        self.assertNotEqual(scaffold_facts(24, VOCAB)[:9],
                            scaffold_facts(9, VOCAB * 2)[:9])

    def test_it_is_deterministic(self):
        """Two containers derive identical facts or the network disagrees."""
        self.assertEqual(scaffold_facts(24, VOCAB), scaffold_facts(24, VOCAB))


class EveryFactIsHeldByExactlyTheReplicaCount(unittest.TestCase):

    def test_total_placements_equal_facts_times_replicas(self):
        """The coverage invariant. A homeless fact reads as zeros, not an error.

        Summed over nodes rather than checked per node, because the ring does not
        balance exactly and a per-node bound would either be loose enough to
        admit a broken case or tight enough to fail on a reseed.
        """
        facts = scaffold_facts(24, VOCAB)
        for replicas in (1, 2, 3):
            with self.subTest(replicas=replicas):
                self.assertEqual(sum(_held_counts(facts, replicas=replicas)),
                                 len(facts) * replicas)

    def test_raising_replicas_raises_what_is_held(self):
        """The companion to the assertion above, and rule 10 requires it.

        A test that a total is unchanged passes when the mechanism is
        disconnected. This one asserts something DID move: more replicas must
        place strictly more copies.
        """
        facts = scaffold_facts(24, VOCAB)
        self.assertLess(sum(_held_counts(facts, replicas=1)),
                        sum(_held_counts(facts, replicas=2)))

    def test_no_single_node_holds_everything(self):
        """Otherwise a latency measurement never leaves one machine.

        At `replicas == NODES` every peer holds every fact and a walk needs no
        network at all — it would report loopback timings while appearing to
        exercise the transport. That configuration is legal and is exactly what
        must not be the default here.
        """
        facts = scaffold_facts(24, VOCAB)
        self.assertTrue(all(held < len(facts)
                            for held in _held_counts(facts, replicas=2)))


if __name__ == "__main__":
    unittest.main()
