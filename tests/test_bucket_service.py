"""A node holds its own share and reaches everything else by message.

`federated.Federation` holds every node's table in one object, so it shows the
rows are SEPARABLE. This shows one node SEPARATED: it owns a slice, it refuses
anything else, and the protocol is complete enough to drive it with no method
call crossing an ownership boundary.

The load-bearing assertions are the refusals. A service that quietly served
another node's surface would produce **correct numbers from an arrangement that
is not the one being claimed**, and nothing downstream could tell — which is the
failure this whole file exists to make impossible.
"""

from __future__ import annotations

import unittest

from openplexus.bucket_service import BucketService
from openplexus.buckets import BucketConfig, observations
from openplexus.grounding import CoOccurrence, conditional
from openplexus.tasks.occasions import OccasionConfig, generate

CONFIG = BucketConfig(width=50, nodes=6, observers=3, seed=0)


def _network(config: BucketConfig = CONFIG) -> list[BucketService]:
    return [BucketService(n, config) for n in range(config.nodes)]


def _run(services: list[BucketService], stream) -> None:
    """Drive a whole stream through the message protocol and nothing else."""
    config = services[0].config
    closes: dict[int, int] = {}

    def deliver() -> None:
        pending = [item for service in services for item in service.take()]
        while pending:
            for destination, message in pending:
                services[destination].handle(message)
            pending = [item for service in services for item in service.take()]

    def advance(now: int | None) -> None:
        due = [b for b, shut in closes.items() if now is None or shut < now]
        for bucket in sorted(due):
            closes.pop(bucket)
            services[services[0].owner(bucket)].handle(("FLUSH", bucket))
            deliver()

    for observation in observations(stream, config, tempo=100):
        advance(observation.when)
        bucket = observation.when // config.width
        closes[bucket] = (bucket + 1) * config.width
        services[services[0].owner(bucket)].handle(
            ("OBSERVE", bucket, observation.surface, observation.when))
    advance(None)


class ItRefusesWhatItDoesNotOwn(unittest.TestCase):
    """The property that makes the arrangement real rather than described."""

    def setUp(self) -> None:
        self.services = _network()

    def _elsewhere(self, node: int) -> int:
        for key in range(500):
            if self.services[node].owner(key) != node:
                return key
        raise AssertionError("every key routed to one node")

    def test_a_note_for_another_nodes_surface_is_refused(self):
        stranger = self._elsewhere(0)
        with self.assertRaises(ValueError):
            self.services[0].handle(("NOTE", stranger))

    def test_a_seen_for_another_nodes_surface_is_refused(self):
        stranger = self._elsewhere(0)
        with self.assertRaises(ValueError):
            self.services[0].handle(("SEEN", stranger))

    def test_an_observe_for_another_nodes_bucket_is_refused(self):
        stranger = self._elsewhere(0)
        with self.assertRaises(ValueError):
            self.services[0].handle(("OBSERVE", stranger, 1, 10))

    def test_and_its_OWN_keys_are_served(self):
        """The companion. A service refusing everything passes every test above."""
        mine = next(k for k in range(500) if self.services[0].owns(k))
        self.services[0].handle(("NOTE", mine))
        self.assertEqual(self.services[0].handle(("SEEN", mine)), 1)

    def test_a_node_outside_the_network_is_refused(self):
        with self.assertRaises(ValueError):
            BucketService(99, CONFIG)

    def test_an_unknown_message_is_refused(self):
        with self.assertRaises(ValueError):
            self.services[0].handle(("GOSSIP", 1))


class ItAgreesWithOneProcess(unittest.TestCase):
    """Splitting across services must not change what was counted."""

    def setUp(self) -> None:
        self.occasions = OccasionConfig(concepts=16, surfaces=3, presence=0.7,
                                        noise=3, distractors=1, occasions=500,
                                        seed=0)
        self.stream = generate(self.occasions)
        self.services = _network()
        _run(self.services, self.stream)
        self.single = CoOccurrence()
        for occasion in self.stream:
            self.single.observe(occasion.surfaces)

    def test_no_service_holds_a_row_it_does_not_own(self):
        for service in self.services:
            for surface in service.index.rows():
                self.assertEqual(service.owner(surface), service.node)

    def test_every_marginal_matches(self):
        for surface in self.single.surfaces():
            owner = self.services[0].owner(surface)
            self.assertEqual(self.services[owner].index.seen(surface),
                             self.single.seen(surface), f"surface {surface}")

    def test_every_pair_matches_at_the_owner_of_each_end(self):
        for surface in self.single.surfaces():
            owner = self.services[0].owner(surface)
            for other in self.single.partners(surface):
                self.assertEqual(
                    self.services[owner].index.together(surface, other),
                    self.single.together(surface, other),
                    f"pair {surface},{other}")

    def test_more_than_one_service_ended_up_holding_something(self):
        """The companion: one service holding everything satisfies the rest."""
        busy = [s for s in self.services if s.index.rows()]
        self.assertGreater(len(busy), 1)


class WhatANodeCannotKnow(unittest.TestCase):
    """Two things never cross the wire, and both refuse rather than default."""

    def setUp(self) -> None:
        self.services = _network()
        self.stream = generate(OccasionConfig(concepts=12, surfaces=3,
                                              occasions=300, seed=0))
        _run(self.services, self.stream)

    def test_the_worlds_occasion_count_is_never_sent_anywhere(self):
        """`MOMENT` is dropped at the bucket owner, so no service accumulates it.

        `ppmi` divides by it. That it stays at zero here is not a bug to fix — it
        is why `conditional` is the statistic a real network can serve.
        """
        for service in self.services:
            self.assertEqual(service.index.occasions, 0)

    def test_ranking_without_a_fetched_marginal_RAISES(self):
        """Defaulting to zero is how the first federated walk silently returned
        every surface alone. It has to be loud."""
        service = next(s for s in self.services if s.index.rows())
        surface = next(iter(sorted(service.index.rows())))
        with self.assertRaises(KeyError):
            service.rank(surface, conditional, 1, seen={})

    def test_and_it_ranks_once_the_marginals_are_supplied(self):
        """The companion. The refusal is about missing data, not about ranking."""
        service = next(s for s in self.services if s.index.rows())
        surface = next(iter(sorted(service.index.rows())))
        needed = service.candidates(surface)
        seen = {}
        for other in needed:
            owner = self.services[0].owner(other)
            seen[other] = self.services[owner].handle(("SEEN", other))
        self.assertLessEqual(len(service.rank(surface, conditional, 1, seen)), 1)

    def test_candidates_names_exactly_what_must_be_fetched(self):
        service = next(s for s in self.services if s.index.rows())
        surface = next(iter(sorted(service.index.rows())))
        self.assertEqual(service.candidates(surface),
                         service.index.partners(surface))


if __name__ == "__main__":
    unittest.main()
