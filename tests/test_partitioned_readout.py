"""The partitioned readout, and whether its independence claim is real.

The claim note 009 §4 needs is that no machine's readout update reads any other
machine's activity. That is not a property of the arithmetic looking local; it is
a property that has to be broken to be believed, so the central test here
perturbs one group and requires the others to come out **bit-identical**.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.tasks.mqar import MqarConfig, dataset

TASK = MqarConfig(n_pairs=3, seq_len=32, n_keys=16, n_values=6,
                  autoregressive=True, filler="random", seed=4242)


def _train(partitions: int, *, width: int = 16, seed: int = 1,
           seed_readout=None, sequences: int = 24):
    """Train briefly and hand back the model. Deterministic given its arguments."""
    model = LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=TASK.vocab_size, d_model=width, partitions=partitions,
        lr=0.05, key_scale=0.5, seed=seed))
    if seed_readout is not None:
        seed_readout(model)
    for sequence in dataset(TASK, sequences):
        tokens = np.asarray(sequence.tokens)
        targets = np.roll(tokens, -1)
        scored = np.ones(len(tokens), dtype=bool)
        scored[-1] = False
        model.run(tokens, targets, scored, learn=True)
    return model


class PartitionsAreIndependent(unittest.TestCase):
    """The C1 claim: one group's state cannot influence another's."""

    def test_disturbing_one_group_leaves_the_others_bit_identical(self):
        """Start group 0 somewhere else entirely; groups 1-3 must not notice.

        This is the assertion the whole partitioning exists to support. If any
        group's update summed over the full retrieved vector -- or over the
        pooled prediction, which is the easy mistake -- group 0's altered weights
        would leak into every other group's error term within one step.

        Bit-identical, not close: an influence that only shows up in the seventh
        decimal is still an influence, and at scale it is still a machine waiting
        on another machine.
        """
        def disturb(model):
            model.grouped_wo[:, 0, :] = 3.7

        untouched = _train(4, seed_readout=None)
        disturbed = _train(4, seed_readout=disturb)

        self.assertFalse(
            np.array_equal(untouched.grouped_wo[:, 0, :],
                           disturbed.grouped_wo[:, 0, :]),
            "the disturbance did not survive training, so the test proves "
            "nothing about the other groups")
        for group in (1, 2, 3):
            np.testing.assert_array_equal(
                untouched.grouped_wo[:, group, :],
                disturbed.grouped_wo[:, group, :],
                f"group {group} moved when group 0 was disturbed")

    def test_a_group_needs_every_key_but_only_its_own_values(self):
        """The row-split, checked rather than argued.

        Note 009 claims the memory splits by ROWS without a reduction: machine g
        owns rows R_g of M, computes `r[R_g]` complete, and needs no partial
        result from anyone -- at the cost of needing the FULL key vector, since
        `r[i] = sum_j M[i,j] k[j]` runs over every j.

        That claim was arithmetic on a whiteboard. Both halves are testable:

        - **Only its own values.** M row i accumulates `v[i] * k_prev[j]`, so row
          i depends on `wv[:, i]` alone. Perturbing another group's value
          projection must leave this group's retrieval untouched, exactly.
        - **Every key.** Perturbing another group's KEY projection must change it,
          because that is the broadcast the design pays for. A version that
          survived this would not be doing content-addressed retrieval across the
          full width, and the cheerful reading -- "look, fully independent!" --
          would be the wrong one.

        This corrects an earlier version of this test which asserted a group was
        equivalent to a standalone narrow model. It is not, and cannot be: that
        model's key is 4-dimensional where the group's is 16-dimensional. The
        assertion was wrong, not the code.
        """
        def retrieval(model, tokens):
            d = model.config.d_model
            memory = np.zeros((d, d))
            previous_key = None
            out = []
            for token in tokens:
                key, value = model.wk[token], model.wv[token]
                if previous_key is not None:
                    memory += np.outer(value, previous_key)
                out.append(memory @ key)
                previous_key = key
            return np.array(out)

        tokens = np.asarray(dataset(TASK, 1)[0].tokens)
        group = slice(4, 8)
        other = slice(8, 12)

        base = _train(4, width=16)
        baseline = retrieval(base, tokens)[:, group]

        values_changed = _train(4, width=16)
        values_changed.wv[:, other] += 1.0
        np.testing.assert_array_equal(
            retrieval(values_changed, tokens)[:, group], baseline,
            "this group's retrieval moved when another group's VALUES changed, "
            "so the memory is not row-split and note 009 is wrong")

        keys_changed = _train(4, width=16)
        keys_changed.wk[:, other] += 1.0
        self.assertFalse(
            np.array_equal(retrieval(keys_changed, tokens)[:, group], baseline),
            "this group's retrieval ignored another group's KEYS, so the full "
            "key is not actually needed and the test is not measuring the "
            "broadcast the design pays for")


class GroupsOwnContiguousDimensions(unittest.TestCase):
    """Which retrieved dimensions belong to which group, checked through `run`.

    A group is a machine. If the width were split by interleaving rather than by
    contiguous blocks -- taking every other dimension instead of the first half
    -- every arithmetic identity above would still hold, every independence test
    would still pass, and the partition would still look clean. What would break
    is the correspondence to hardware: one machine leaving would damage *every*
    group a little instead of *one* group entirely.

    The mutation harness found this hole. An earlier version of these tests
    checked the row-split by recomputing retrieval outside the model, which never
    touched the reshape at all, so `split-the-retrieved-vector-the-wrong-way`
    survived. The fix is to make the claim through the public path.
    """

    def test_losing_one_group_s_values_leaves_the_other_group_untouched(self):
        """Take away group 0's values and readout; group 1 must be bit-identical.

        **What a group owns is its VALUES and its readout, not its keys.** The
        first version of this test removed a whole machine with `ablate`, which
        also zeroes that machine's columns of the key projection -- and every
        group needs the full key, so every group moved. That failure was the
        architecture answering correctly, not a bug: `r[i] = sum_j M[i,j] k[j]`
        runs over every j, which is precisely the broadcast note 009 says the
        row-split pays for.

        Worth stating plainly, because it corrects the churn story: **a departing
        machine degrades every surviving group**, not just its own. Not by making
        anyone wait -- nothing synchronises -- but because a slice of a shared
        broadcast quantity has gone missing. G3 measured that the system recovers
        from it; that measurement stands and this only names the mechanism.

        So the ownership claim is tested on the half that is genuinely owned.
        """
        tokens = np.asarray(dataset(TASK, 1)[0].tokens)

        before = _train(2, width=16).run(tokens, partition=1)

        damaged = _train(2, width=16)
        damaged.wv[:, :8] = 0.0
        damaged.grouped_wo[:, 0, :] = 0.0

        np.testing.assert_array_equal(
            damaged.run(tokens, partition=1), before,
            "group 1 changed when group 0's values were removed, so the groups "
            "do not own contiguous blocks of the retrieved vector")

        self.assertEqual(len(set(damaged.run(tokens, partition=0).tolist())), 1,
                         "group 0 still says something after losing every value "
                         "it can retrieve")


class PartitioningChangesLearning(unittest.TestCase):
    """It has to be a different rule, not a different way of reporting one."""

    def test_four_groups_learn_something_other_than_one_group(self):
        """Guards the einsum against silently reproducing the global update.

        The pooled prediction of a partitioned model has the same form as the
        global readout -- both sum `Wo[:, j] r[j]` over every j. Only the ERROR
        term differs. So a bug that fed the pooled prediction back into every
        group would leave predictions looking right while quietly restoring the
        global reduction, and nothing about the output shape would give it away.
        """
        self.assertFalse(np.array_equal(_train(1).wo, _train(4).wo),
                         "partitioning changed nothing, so the error term is "
                         "still global")

    def test_one_partition_is_the_plain_delta_rule(self):
        """P=1 must be the untouched original, so every earlier result stands.

        G0 through G3 were all measured before partitions existed. If P=1 is not
        the old computation, those results describe a model that is no longer in
        the repository.

        The readout now runs through an `einsum` where it used to be a matmul,
        which reassociates the sum: the two agree to a measured 6.45e-17
        relative, about a quarter of one double-precision epsilon. A bare
        tolerance around that number would be an unfalsifiable test, so this
        compares the gap against a **scale measured in the same run** -- the gap
        a genuinely different update rule (P=2) opens against the same reference.
        The assertion is that rounding noise and a changed rule are separated by
        many orders of magnitude, which is a claim a broken implementation
        cannot satisfy by being slightly wrong.
        """
        model = _train(1)
        reference = LocalAssociativeMemory(LocalMemoryConfig(
            vocab_size=TASK.vocab_size, d_model=16, lr=0.05, key_scale=0.5,
            seed=1))
        for sequence in dataset(TASK, 24):
            tokens = np.asarray(sequence.tokens)
            targets = np.roll(tokens, -1)
            memory = np.zeros((16, 16))
            previous_key = None
            for t, token in enumerate(tokens):
                key, value = reference.wk[token], reference.wv[token]
                if previous_key is not None:
                    memory += np.outer(value, previous_key)
                retrieved = memory @ key
                if t != len(tokens) - 1:
                    target = np.zeros(TASK.vocab_size)
                    target[targets[t]] = 1.0
                    error = target - reference.wo @ retrieved
                    reference.wo += 0.05 * np.outer(error, retrieved)
                previous_key = key
        rounding = np.abs(_train(1).wo - reference.wo).max()
        rule_change = np.abs(_train(2).wo - reference.wo).max()
        self.assertGreater(
            rule_change, rounding * 1e6,
            f"a changed update rule differs by {rule_change:.3g} and rounding "
            f"by {rounding:.3g} -- too close for this test to tell them apart")
        self.assertLess(
            rounding, np.abs(reference.wo).max() * 1e-14,
            f"P=1 differs from the plain delta rule by {rounding:.3g}, which is "
            f"far more than reassociating a sum can explain")


class ReadingOffOneGroup(unittest.TestCase):
    """`partition=` is what a machine that cannot afford the pool would see."""

    def test_a_single_group_answers_differently_from_the_pool(self):
        model = _train(4, width=32)
        tokens = np.asarray(dataset(TASK, 1)[0].tokens)
        pooled = model.run(tokens)
        alone = model.run(tokens, partition=0)
        self.assertEqual(len(pooled), len(alone))
        self.assertFalse(np.array_equal(pooled, alone),
                         "one group and the pool agree everywhere, so the "
                         "argument selects nothing")

    def test_every_group_is_reachable_and_out_of_range_is_refused(self):
        model = _train(4, width=32)
        tokens = np.asarray(dataset(TASK, 1)[0].tokens)
        for group in range(4):
            self.assertEqual(len(model.run(tokens, partition=group)),
                             len(tokens))
        with self.assertRaises(ValueError):
            model.run(tokens, partition=4)
        with self.assertRaises(ValueError):
            model.run(tokens, partition=-1)


class GroupedViewAliases(unittest.TestCase):
    """Two names for the readout, one array -- rule 9's one-implementation rule."""

    def test_ablate_is_visible_through_the_grouped_view(self):
        model = _train(4, width=16)
        self.assertTrue(np.any(model.grouped_wo[:, 0, :]))
        model.ablate([0, 1, 2, 3])
        np.testing.assert_array_equal(model.grouped_wo[:, 0, :],
                                      np.zeros_like(model.grouped_wo[:, 0, :]))

    def test_writes_through_the_grouped_view_reach_the_readout(self):
        model = _train(2, width=16)
        model.grouped_wo[:, 1, :] = 2.0
        np.testing.assert_array_equal(model.wo[:, 8:],
                                      np.full_like(model.wo[:, 8:], 2.0))


class ConfigRefusesImpossibleShapes(unittest.TestCase):

    def test_width_must_divide_into_the_partitions(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=10, d_model=10, partitions=4)

    def test_partitions_must_be_positive(self):
        with self.assertRaises(ValueError):
            LocalMemoryConfig(vocab_size=10, d_model=8, partitions=0)


if __name__ == "__main__":
    unittest.main()
