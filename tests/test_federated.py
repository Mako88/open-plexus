"""The rows really are split, and asking across the split really does cost.

`buckets.Join` addresses the bucket half of the grounding design for real and
leaves the other half asserted: one `CoOccurrence` held every surface in the
world, so *"the link is written to `owner(surface)`"* was a property of the
design rather than of the code. This file is where that stops being a claim.

Two things are load-bearing and neither is about arithmetic:

- **`holdings` is checkable from outside.** A node holding a row it does not own
  fails a test rather than losing an argument.
- **A statistic that needs another node's number must PAY for it.** The count is
  the whole reason to build this, and a version that quietly used a local value
  instead would be faster, wrong, and indistinguishable from correct — which is
  exactly what happened on the first run, when every score collapsed to zero
  because `seen(other)` was read from the asker's own table.
"""

from __future__ import annotations

import unittest
from itertools import combinations

from openplexus.federated import Federation
from openplexus.grounding import (STATISTICS, CoOccurrence, conditional,
                                  equivalence_classes, local_conditional, ppmi)
from openplexus.tasks.occasions import OccasionConfig, generate


def _both(nodes: int = 8, concepts: int = 16, occasions: int = 800):
    """The same stream into a single table and into a federation."""
    config = OccasionConfig(concepts=concepts, surfaces=3, presence=0.7,
                            noise=3, distractors=1, occasions=occasions,
                            seed=0)
    single = CoOccurrence()
    federated = Federation(nodes=nodes, seed=0)
    for occasion in generate(config):
        single.observe(occasion.surfaces)
        for surface in occasion.surfaces:
            federated.note(surface)
        for one, other in combinations(sorted(occasion.surfaces), 2):
            federated.link(one, other)
    return config, single, federated


class TheRowsAreSplit(unittest.TestCase):
    """The locality proof, asserted over the data rather than over a docstring."""

    def setUp(self) -> None:
        self.config, self.single, self.federated = _both()

    def test_no_node_holds_a_row_it_does_not_own(self):
        for node, rows in enumerate(self.federated.holdings()):
            for surface in rows:
                self.assertEqual(
                    self.federated.owner(surface), node,
                    f"node {node} holds a row for {surface}, owned by "
                    f"{self.federated.owner(surface)}")

    def test_every_surface_is_held_somewhere(self):
        """The companion. A federation that stored nothing passes the test above."""
        held = set().union(*self.federated.holdings())
        self.assertEqual(held, set(self.single.rows()))

    def test_more_than_one_node_holds_something(self):
        """The other companion: one node holding everything is also 'no node
        holds a row it does not own', and is the arrangement this replaces."""
        busy = [rows for rows in self.federated.holdings() if rows]
        self.assertGreater(len(busy), 1)

    def test_ownership_matches_the_ring_the_join_uses(self):
        """Two rings would drift and a link would be filed where nobody looks."""
        from openplexus.buckets import BucketConfig, Join
        join = Join(BucketConfig(width=10, nodes=8, seed=0))
        for surface in (0, 5, 41, 999):
            self.assertEqual(self.federated.owner(surface),
                             join.bucket_owner(surface))


class ItComputesTheSameThing(unittest.TestCase):
    """Splitting the table must not change the answer."""

    def setUp(self) -> None:
        self.config, self.single, self.federated = _both()

    def test_the_walk_agrees_with_the_single_table_computation(self):
        expected = equivalence_classes(self.single, conditional, 2)
        for surface in range(self.config.concept_surfaces):
            self.assertEqual(self.federated.walk(surface, conditional, 2),
                             expected[surface],
                             f"federated walk disagrees at surface {surface}")

    def test_a_pair_is_counted_once_at_each_end(self):
        federated = Federation(nodes=4, seed=0)
        federated.link(3, 7)
        rows = federated.holdings()
        self.assertIn(3, rows[federated.owner(3)])
        self.assertIn(7, rows[federated.owner(7)])
        self.assertEqual(federated.writes, 2)


class AskingAcrossTheSplitCosts(unittest.TestCase):
    """The number the design owes an answer for."""

    def setUp(self) -> None:
        self.config, self.single, self.federated = _both()

    def test_ranking_by_a_statistic_that_needs_a_peer_charges_remote_reads(self):
        before = self.federated.remote_reads
        self.federated.rank(0, conditional, 2)
        self.assertGreater(self.federated.remote_reads, before)

    def test_the_statistic_that_needs_no_peer_charges_NOTHING(self):
        """The companion, and it is the finding rather than the control.

        `local_conditional` divides by the asker's own marginal, so its owner can
        rank without a single message. `g33-01` measured it failing — so the
        remote read is a price paid for something, and this is what shows the
        price is real rather than incidental to how ranking is written.
        """
        before = self.federated.remote_reads
        self.federated.rank(0, local_conditional, 2)
        self.assertEqual(self.federated.remote_reads, before)

    def test_a_read_served_by_the_asker_itself_is_not_remote(self):
        federated = Federation(nodes=1, seed=0)
        federated.link(1, 2)
        federated.note(1)
        federated.note(2)
        before = federated.remote_reads
        federated.rank(1, conditional, 1)
        self.assertEqual(federated.remote_reads, before,
                         "a one-node federation sent a message to itself")

    def test_a_walk_costs_hops(self):
        before = self.federated.hops
        self.federated.walk(0, conditional, 2)
        self.assertGreater(self.federated.hops, before)


class WhenANodeLeaves(unittest.TestCase):
    """Nothing is replicated, so a departure is a permanent loss."""

    def setUp(self) -> None:
        self.config, self.single, self.federated = _both()

    def test_its_surfaces_stop_being_present(self):
        gone = [s for s in self.single.rows()
                if self.federated.owner(s) == 0]
        self.assertTrue(gone, "node 0 owned nothing, so this tests nothing")
        self.federated.lose(0)
        for surface in gone:
            self.assertFalse(self.federated.present(surface))

    def test_everything_else_is_untouched(self):
        """The companion. A `lose` that emptied the world passes the test above."""
        self.federated.lose(0)
        for surface in self.single.rows():
            if self.federated.owner(surface) != 0:
                self.assertTrue(self.federated.present(surface))

    def test_asking_a_departed_owner_RAISES_rather_than_returning_zero(self):
        """A marginal of zero is an ordinary count. A departed peer is not."""
        gone = next(s for s in self.single.rows()
                    if self.federated.owner(s) == 0)
        self.federated.lose(0)
        with self.assertRaises(KeyError):
            self.federated.seen(gone)

    def test_a_walk_still_returns_a_SMALLER_answer_rather_than_failing(self):
        """Dropping an unscoreable candidate is local and graceful. Scoring it
        as zero would make a departed peer look like a surface nobody saw."""
        alive = next(s for s in sorted(self.single.rows())
                     if self.federated.owner(s) != 0)
        before = self.federated.walk(alive, conditional, 2)
        self.federated.lose(0)
        after = self.federated.walk(alive, conditional, 2)
        self.assertIn(alive, after)
        self.assertLessEqual(len(after), len(before))

    def test_the_unreachable_reads_are_COUNTED(self):
        """A departure that left no trace in the accounting would make a
        degraded run indistinguishable from a healthy one."""
        self.federated.lose(0)
        before = self.federated.unreachable
        for surface in sorted(self.single.rows())[:20]:
            if self.federated.present(surface):
                self.federated.walk(surface, conditional, 2)
        self.assertGreater(self.federated.unreachable, before)


class WhatNoNodeCanKnow(unittest.TestCase):
    """PPMI is refused rather than approximated, and that is deliberate."""

    def test_asking_a_node_for_the_worlds_occasion_count_raises(self):
        _, _, federated = _both()
        with self.assertRaises(NotImplementedError):
            federated.rank(0, ppmi, 2)

    def test_and_the_deployable_statistic_does_not_raise(self):
        """The companion: the refusal is about `occasions`, not about ranking."""
        _, _, federated = _both()
        self.assertIsInstance(federated.rank(0, conditional, 2), list)

    def test_ppmi_is_still_available_on_a_single_table(self):
        """It is a reference statistic, not a deleted one. `g33-01` uses it as
        the yardstick `conditional` is checked against."""
        single = CoOccurrence()
        for _ in range(5):
            single.observe([0, 1])
            single.observe([2, 3])
        self.assertGreater(STATISTICS["ppmi"](single, 0, 1), 0.0)


class Validation(unittest.TestCase):
    def test_a_federation_needs_a_node(self):
        with self.assertRaises(ValueError):
            Federation(nodes=0)

    def test_k_must_be_at_least_one(self):
        with self.assertRaises(ValueError):
            Federation(nodes=2).rank(0, conditional, 0)

    def test_a_surface_cannot_link_to_itself(self):
        with self.assertRaises(ValueError):
            Federation(nodes=2).link(5, 5)


if __name__ == "__main__":
    unittest.main()
