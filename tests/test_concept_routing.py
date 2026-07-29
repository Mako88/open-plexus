"""Can the model still LEARN when its store is split by concept?

Note 042's item 2 was measured as a data structure (decision 134: pooled capacity
identical, lone-node capacity 16x at 16 nodes) and never as a component. The
model had never read or written through it, so "the arrangement works" was a
claim about `ConceptStore` and not about anything that learns.

`concept_nodes` is the seam that asks. What it must satisfy:

- **`concept_nodes=1` is bit-identical to the monolithic store.** One node owns
  everything, so any difference is a bug in the routing rather than an effect of
  it. This is the control that makes every other number here readable.
- **Learning survives.** Measured on MQAR, where the local rule is known to work.
- **Routing actually routes.** A seam that quietly did nothing would pass the
  first two and mean nothing.

## What the probe found, and the part that must not be over-read

MQAR at `n_pairs=32, seq_len=256, n_keys=64`, base width 32, 200 sequences x 2
epochs, one seed:

    partitioned                     monolithic AT EQUAL STATE
    w32 x  1 node    0.360          w  32     0.360
    w32 x  4 nodes   0.903          w  64     0.954
    w32 x 16 nodes   0.988          w 128     1.000

**Routing does not break learning** -- that is the falsifier and it passes.
**And the apparent gain is the extra state, not the arrangement:** `n` nodes of
width `w` hold `n*w^2` numbers, and a monolithic store given the same numbers
does slightly BETTER. Quoting 0.360 to 0.988 as a win for partitioning would be
exactly the comparison g10-09 was retracted for.

That agrees with decision 134 rather than contradicting it: pooled capacity was
measured identical, and this shows the same thing inside a model that learns.
**The case for concept partitioning is lone-node capability, which is a C1
property, not an accuracy one.**

One seed, one task, exploratory -- these numbers were looked at before any
prediction was registered, so they are a reason to run a sweep and not a result.
"""

from __future__ import annotations

import unittest
from dataclasses import replace

import numpy as np

from openplexus.concepts import Shared
from openplexus.models.local_memory import (LocalAssociativeMemory,
                                            LocalMemoryConfig)
from openplexus.tasks.mqar import IGNORE, MqarConfig, dataset

WIDTH = 32
TASK = MqarConfig(seed=0, n_pairs=16, seq_len=128, n_keys=32)


def build(nodes: int, width: int = WIDTH, **kw) -> LocalAssociativeMemory:
    return LocalAssociativeMemory(LocalMemoryConfig(
        d_model=width, vocab_size=TASK.vocab_size, seed=0,
        concept_nodes=nodes, **kw))


def train_and_score(nodes: int, sequences: int = 60) -> float:
    model = build(nodes)
    train = dataset(TASK, sequences)
    test = dataset(replace(TASK, seed=500_000), 30)
    for sequence in train:
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        model.run(tokens, targets, targets != IGNORE, learn=True)
    hits = total = 0
    for sequence in test:
        predicted = model.run(np.array(sequence.tokens, dtype=np.int64))
        for position in sequence.query_positions:
            total += 1
            hits += int(predicted[position] == sequence.targets[position])
    return hits / total


class OneNodeIsTheMonolithicStore(unittest.TestCase):
    """The control. Without it, every other number here is unattributable."""

    def test_predictions_are_IDENTICAL(self):
        sequence = dataset(TASK, 1)[0]
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        scored = targets != IGNORE

        monolithic, routed = build(0), build(1)
        for _ in range(3):
            plain = monolithic.run(tokens, targets, scored, learn=True)
            through = routed.run(tokens, targets, scored, learn=True)
        np.testing.assert_array_equal(plain, through)

    def test_the_readout_matches_too(self):
        """Equal predictions could come from two different readouts that happen
        to argmax the same way, and on a task this easy they might for a while.
        The parameter is the thing that has to match."""
        sequence = dataset(TASK, 1)[0]
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        monolithic, routed = build(0), build(1)
        for _ in range(3):
            monolithic.run(tokens, targets, targets != IGNORE, learn=True)
            routed.run(tokens, targets, targets != IGNORE, learn=True)
        np.testing.assert_allclose(monolithic.grouped_wo, routed.grouped_wo)


class RoutingActuallyRoutes(unittest.TestCase):
    """A seam that quietly did nothing would pass every test above.

    This is the shape of the bug that was in `ownership.py` until the model was
    wired to it: every concept below `REPLICAS` drew the same ring position as
    one node's own label, so token ids -- which are all small -- routed to a
    single node and a partitioned model was a monolithic one.
    """

    def test_more_nodes_change_what_a_read_returns(self):
        sequence = dataset(TASK, 1)[0]
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        strengths = {}
        for nodes in (0, 4, 16):
            trace: list = []
            build(nodes).run(tokens, targets, targets != IGNORE, learn=True,
                             trace=trace)
            strengths[nodes] = [entry["strength"] for entry in trace]
        self.assertNotEqual(strengths[0], strengths[4],
                            "four nodes retrieved exactly what one store did, "
                            "so nothing was routed")
        self.assertNotEqual(strengths[4], strengths[16])

    def test_a_partitioned_read_is_CLEANER_not_dirtier(self):
        """The direction matters and it is the opposite of the intuition.

        Splitting sounds like losing information. It is not: a read for concept
        `c` no longer sums over bindings held elsewhere, so the interference
        term shrinks. What is lost is the ability to retrieve a DIFFERENT
        concept's binding from a similar key -- which with random keys is
        nothing, and with content-derived keys (item 0c) would be the point.
        Note 044 records that tension.
        """
        sequence = dataset(TASK, 1)[0]
        tokens = np.array(sequence.tokens, dtype=np.int64)
        targets = np.array(sequence.targets, dtype=np.int64)
        totals = {}
        for nodes in (0, 16):
            trace: list = []
            build(nodes).run(tokens, targets, targets != IGNORE, learn=True,
                             trace=trace)
            totals[nodes] = sum(entry["strength"] for entry in trace)
        self.assertLess(totals[16], totals[0],
                        "partitioned reads returned MORE total signal than a "
                        "store holding every binding, which cannot happen if "
                        "each node holds a subset")


class LearningSurvivesRouting(unittest.TestCase):
    """The falsifier note 042's item 2 was missing."""

    def test_a_partitioned_model_still_learns(self):
        routed = train_and_score(8)
        self.assertGreater(
            routed, 0.5,
            f"a concept-partitioned model scored {routed:.3f} on MQAR, so "
            f"routing broke the thing partitioning exists to distribute")

    def test_it_is_not_WORSE_than_the_monolithic_store_it_replaces(self):
        """At equal WIDTH, which favours partitioning -- 8 nodes hold 8x the
        state. A loss under that bias would be unambiguous; a win is not
        evidence of capacity and `test_...EQUAL_STATE...` says why."""
        self.assertGreaterEqual(train_and_score(8) + 1e-9,
                                train_and_score(0))

    def test_at_EQUAL_STATE_partitioning_does_not_win(self):
        """**The comparison g10-09 was retracted for missing**, run in the
        direction that could embarrass this arrangement rather than flatter it.

        8 nodes of width 32 hold what one store of width 32*sqrt(8) does. If
        partitioning were a capacity win the monolithic control would lose here;
        decision 134 measured pooled capacity as identical, so it should not.
        """
        equal = int(WIDTH * np.sqrt(8))
        model = build(0, width=equal)
        train = dataset(TASK, 60)
        for sequence in train:
            tokens = np.array(sequence.tokens, dtype=np.int64)
            targets = np.array(sequence.targets, dtype=np.int64)
            model.run(tokens, targets, targets != IGNORE, learn=True)
        hits = total = 0
        for sequence in dataset(replace(TASK, seed=500_000), 30):
            predicted = model.run(np.array(sequence.tokens, dtype=np.int64))
            for position in sequence.query_positions:
                total += 1
                hits += int(predicted[position] == sequence.targets[position])
        self.assertGreaterEqual(
            hits / total + 1e-9, train_and_score(8),
            "a monolithic store given the SAME number of numbers lost to the "
            "partitioned one -- if that is real it contradicts decision 134's "
            "identical pooled capacity and wants a sweep, not a passing test")


class TwoSurfacesOfOneConceptSHAREItsFacts(unittest.TestCase):
    """The indirection, checked through the model rather than in isolation.

    `openplexus/concepts.py` proves the mapping merges. This proves the model
    USES it: a fact written under one surface must be readable under another
    surface of the same concept, which is the whole point of John's picture-of-a-
    dog framing and is impossible while the surface is the address.
    """

    def test_a_fact_written_under_one_surface_reads_under_the_other(self):
        sequence = dataset(TASK, 1)[0]
        tokens = np.array(sequence.tokens, dtype=np.int64)
        seen = sorted({int(t) for t in tokens})
        a, b = seen[1], seen[2]

        model = build(4)
        model.surfaces = Shared(TASK.vocab_size, [[a, b]])
        self.assertEqual(model.surfaces.of(a), model.surfaces.of(b),
                         "the two surfaces did not merge, so this test is not "
                         "exercising what it claims")

        targets = np.array(sequence.targets, dtype=np.int64)
        trace: list = []
        model.run(tokens, targets, targets != IGNORE, learn=True, trace=trace)

        apart = build(4)
        split: list = []
        apart.run(tokens, targets, targets != IGNORE, learn=True, trace=split)
        self.assertNotEqual([e["strength"] for e in trace],
                            [e["strength"] for e in split],
                            "merging two surfaces changed nothing, so the "
                            "model is still addressing by token")

    def test_the_DEFAULT_is_still_one_concept_per_token(self):
        """The control that protects every existing number. If the seam is not
        the identity by default it has quietly invalidated the comparison set --
        decision 74's failure, which is why this is tested and not intended."""
        model = build(0)
        for token in range(TASK.vocab_size):
            self.assertEqual(model.surfaces.of(token), token)


class WhatCannotBeCombinedIsREFUSED(unittest.TestCase):
    """Note 044: the soft hop names no concept, so there is no node to ask.

    Each refusal is a scope boundary stated up front rather than a wrong number
    discovered later.
    """

    def test_hops(self):
        with self.assertRaises(ValueError):
            build(4, hops=2)

    def test_consolidation(self):
        with self.assertRaises(ValueError):
            build(4, consolidation=0.01, decay=0.99)

    def test_memory_cap(self):
        with self.assertRaises(ValueError):
            build(4, memory_cap=1.0)

    def test_carry_store(self):
        with self.assertRaises(ValueError):
            build(4, carry_store=True)

    def test_a_mid_sequence_DIMENSION_departure(self):
        """`leave` clears dimension rows, which is the other arrangement's
        failure mode entirely. `ConceptStore.lose` is this one's."""
        sequence = dataset(TASK, 1)[0]
        with self.assertRaises(ValueError):
            build(4).run(np.array(sequence.tokens, dtype=np.int64),
                         leave=(3, (0,)))


if __name__ == "__main__":
    unittest.main()
