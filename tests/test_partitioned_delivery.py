"""C2 against a partitioned model, which G2 never tested.

[g2-01](../experiments/sweeps/g2-01-latency.txt) established that below the
buffer bound a scrambled, jittered, lossy network changes the learned weights not
at all -- bit-identically, 6/6 seeds. It did that against a model with one global
readout, because that was the only model there was.

The readout is now splittable, and note 009 §5 lists the row-split under delay as
still open. The expectation is that partitioning is orthogonal to delivery: it
changes which weights an update touches, not the order tokens arrive in. **That
expectation is exactly the kind this project has been wrong about twice** -- a
group was assumed equivalent to a narrow model (it is not), and churn damage was
assumed local (it is not). Both were caught by tests written against the
assumption rather than around it.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset
from openplexus.transport import DeliveryConfig, delivered_order

TASK = MqarConfig(n_pairs=3, seq_len=32, n_keys=16, n_values=6,
                  autoregressive=True, filler="random", seed=515)
PARTITIONS = (1, 2, 4, 8)


def _train(partitions: int, order_of=None, sequences: int = 20):
    """Train through a delivery order. `order_of(n)` picks the arrival order."""
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=16, partitions=partitions,
        lr=0.05, key_scale=0.5, seed=3))
    for sequence in dataset(TASK, sequences):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        if order_of is not None:
            index = np.asarray(order_of(len(tokens)))
            tokens, targets, scored = tokens[index], targets[index], scored[index]
        model.run(tokens, targets, scored, learn=True)
    return model


class DelayBelowTheBoundChangesNothing(unittest.TestCase):
    """g2-01's result, re-checked at every partition count."""

    def test_a_scrambled_network_is_bit_identical_at_every_partition_count(self):
        """Not close. Identical, to the last bit, for P = 1, 2, 4 and 8.

        If partitioning interacted with delivery at all, the interaction would
        show up as a difference in the low bits long before it showed up in
        accuracy -- which is why this asserts exact equality rather than a
        tolerance.
        """
        config = DeliveryConfig(max_delay=8, jitter=8, drop=0.0, seed=11)
        self.assertTrue(config.within_bound)
        for partitions in PARTITIONS:
            with self.subTest(partitions=partitions):
                quiet = _train(partitions)
                delayed = _train(partitions,
                                 lambda n: delivered_order(n, config))
                np.testing.assert_array_equal(
                    quiet.wo, delayed.wo,
                    f"P={partitions}: delay below the bound changed the weights")

    def test_the_network_really_did_scramble_the_arrival_order(self):
        """Otherwise the test above compares a stream against itself.

        This is the control g2-01 needed and it is needed again here: a transport
        that quietly delivered in order would make every assertion above pass
        while testing nothing.
        """
        config = DeliveryConfig(max_delay=8, jitter=8, drop=0.0, seed=11)
        from openplexus.transport import arrivals
        landed = [index for _, index in arrivals(32, config)]
        self.assertNotEqual(landed, sorted(landed),
                            "arrivals were already in emission order, so "
                            "nothing was reordered and the test is vacuous")


class PartitioningIsOrthogonalToDelivery(unittest.TestCase):
    """The stronger claim: delay tolerance does not degrade as machines multiply."""

    def test_a_reordering_that_breaks_one_partition_count_breaks_all_of_them(self):
        """Past the bound, every partition count must fail together.

        Bit-identity below the bound could be explained by the delay never
        biting. This checks the other side: with jitter past `max_delay` the
        stream genuinely is corrupted, and the corruption must not depend on P.
        If a higher P survived a reordering that a lower P did not, delivery and
        partitioning would be coupled and note 009 §5 would need rewriting.
        """
        broken = DeliveryConfig(max_delay=2, jitter=12, drop=0.0, seed=5)
        self.assertFalse(broken.within_bound)
        differs = {}
        for partitions in PARTITIONS:
            quiet = _train(partitions)
            mangled = _train(partitions, lambda n: delivered_order(n, broken))
            differs[partitions] = not np.array_equal(quiet.wo, mangled.wo)
        self.assertEqual(set(differs.values()), {True},
                         f"past the bound some partition counts were unaffected: "
                         f"{differs} -- delivery and partitioning are coupled")


if __name__ == "__main__":
    unittest.main()
