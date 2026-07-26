"""Clusters: reading an answer off a named group of machines.

`run(partition=...)` takes an integer for one machine, an iterable for a cluster,
or `None` for the whole network. The three are one dial and it decides how small
a machine can be: a lone machine must be wide enough to answer by itself, while a
handful pooling locally act as one wider machine without any of them being wide.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset

TASK = MqarConfig(n_pairs=3, seq_len=32, n_keys=16, n_values=6,
                  autoregressive=True, filler="random", seed=808)


def trained(partitions: int = 8, width: int = 32):
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=width, partitions=partitions,
        lr=0.05, key_scale=0.5, seed=2))
    for sequence in dataset(TASK, 20):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        model.run(tokens, targets, scored, learn=True)
    return model, np.asarray(dataset(TASK, 1)[0].tokens)


class AClusterIsItsMembersPooled(unittest.TestCase):

    def test_a_one_member_cluster_equals_that_member_alone(self):
        """The two spellings must agree, or the dial has a discontinuity at 1."""
        model, tokens = trained()
        for machine in range(8):
            with self.subTest(machine=machine):
                np.testing.assert_array_equal(
                    model.run(tokens, partition=machine),
                    model.run(tokens, partition=[machine]))

    def test_a_cluster_of_everyone_equals_pooling_everyone(self):
        """And the other end, so `None` is not a fourth behaviour."""
        model, tokens = trained()
        np.testing.assert_array_equal(
            model.run(tokens, partition=list(range(8))),
            model.run(tokens))

    def test_a_cluster_differs_from_its_parts(self):
        """Otherwise pooling is not happening and the argument selects nothing."""
        model, tokens = trained()
        pair = model.run(tokens, partition=[0, 1])
        self.assertFalse(
            np.array_equal(pair, model.run(tokens, partition=0))
            and np.array_equal(pair, model.run(tokens, partition=1)),
            "a two-machine cluster agreed with both members everywhere, so "
            "nothing was pooled")


class ClustersRefuseImpossibleMembership(unittest.TestCase):

    def test_a_machine_cannot_appear_twice(self):
        """Otherwise a cluster can inflate itself by listing a member twice.

        Caught by the mutation harness: with the guard removed, every other test
        here still passed, because none of them ever asked for a duplicate. A
        cluster that double-counts one machine's vote is a different and better
        ensemble than the one being measured, which would quietly flatter small
        clusters — exactly the regime this project cares most about.
        """
        model, tokens = trained()
        with self.assertRaises(ValueError):
            model.run(tokens, partition=[0, 0])
        with self.assertRaises(ValueError):
            model.run(tokens, partition=[1, 2, 1])

    def test_an_empty_cluster_is_refused(self):
        model, tokens = trained()
        with self.assertRaises(ValueError):
            model.run(tokens, partition=[])

    def test_a_member_outside_the_network_is_refused(self):
        model, tokens = trained()
        with self.assertRaises(ValueError):
            model.run(tokens, partition=[0, 8])
        with self.assertRaises(ValueError):
            model.run(tokens, partition=[-1])


class ClustersGrowMonotonicallyInInformation(unittest.TestCase):
    """Not in accuracy — in what they are computing."""

    def test_adding_a_member_changes_the_pooled_answer(self):
        """A member that contributed nothing would mean the split is degenerate."""
        model, tokens = trained()
        smaller = model.run(tokens, partition=[0, 1])
        larger = model.run(tokens, partition=[0, 1, 2])
        self.assertFalse(np.array_equal(smaller, larger),
                         "adding a third machine changed nothing, so that "
                         "machine holds no independent information")


if __name__ == "__main__":
    unittest.main()
