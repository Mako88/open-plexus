"""The split, run for real across processes.

Every result in this project until now came from one process holding one array and
calling slices of it nodes. That is a faithful model of the arithmetic and says
nothing about whether the arithmetic survives being spread out, because no packet
had ever been sent.

These send packets. Each node is a separate OS process with its own memory,
reached over loopback TCP, receiving a four-byte token id and replying with a
complete vote.

**The claim is exactness, not similarity.** If running across `n` processes gave
merely close answers, every earlier measurement would need redoing against the
distributed version. It gives identical ones.
"""

from __future__ import annotations

import os
import unittest
from dataclasses import replace

import numpy as np

from openplexus.distributed import Network, Slice, slices_for
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

TOKENS = np.array([3, 9, 1, 7, 3, 5, 11, 2, 9, 4])

# These spawn OS processes, which costs about half a second each. Run inside the
# mutation harness -- once per mutation, sixty times over -- that turned a five
# minute check into a ten minute one, and a check slow enough to skip is a check
# that eventually is. The harness therefore skips them EXCEPT when the mutation
# is in distributed.py itself, which is the only case where they are the tests
# that can catch it.
SKIP_SLOW = os.environ.get("OPENPLEXUS_SKIP_PROCESS_TESTS") == "1"
skip_if_fast = unittest.skipIf(
    SKIP_SLOW, "process-spawning tests skipped for an unrelated mutation")


def configured(partitions: int, width: int = 32):
    config = LocalMemoryConfig(
        vocab_size=14, d_model=width, partitions=partitions, key_scale=0.5,
        derived_keys=True, seed=5)
    model = LocalAssociativeMemory(config)
    model.wo[:] = np.random.default_rng(0).normal(0.0, 0.1, (14, width))
    return config, model


@skip_if_fast
class TheSplitDoesNotCARRY_THE_GATE(unittest.TestCase):
    """The scope of every "the split is exact" result, written down.

    `Node.step` is a REIMPLEMENTATION of the model's inner loop, not a call into
    it. It holds a memory, a previous key and a readout, and that is all — there
    is no `pending` list, no reward token, no tag, no consolidation. So a config
    carrying gate settings is accepted, ignored, and produces a confident wrong
    answer rather than an error.

    That matters because the whole g9 line is about gates, John's priority is
    tiny nodes, and BACKLOG has carried *"the testbed has never run a gated
    model"* as a one-line item. It is worse than never having run one: the
    distributed path cannot run one, and nothing said so.

    These tests pin the boundary in both directions, so that when the gate is
    implemented on the node the first of them starts failing and says why.
    """

    def gated(self, nodes: int):
        """The same configuration twice, once with the reward gate switched on.

        The reward token is inside the vocabulary and appears in TOKENS, so the
        gate genuinely fires rather than being configured and never reached —
        which would make this test pass for the wrong reason.
        """
        config, model = configured(nodes)
        gate = replace(config, reward_token=int(TOKENS[5]), reward_window=1)
        gated = LocalAssociativeMemory(gate)
        gated.wo[:] = model.wo
        return gate, gated

    def test_the_gate_changes_the_single_process_answer_at_all(self):
        """The guard on the test below. If the gate were inert here, the
        agreement test would pass while measuring nothing."""
        config, model = configured(2)
        gate, gated = self.gated(2)
        self.assertFalse(
            np.array_equal(model.run(TOKENS), gated.run(TOKENS)),
            "the reward gate changed no prediction, so the divergence test "
            "below would be vacuous")

    def test_a_gated_config_is_ACCEPTED_and_then_IGNORED_by_the_nodes(self):
        """The finding, stated as the failure it would cause.

        A network handed a gated config still runs, still votes, and still
        returns an answer. It is simply not the model it was configured as —
        and `Node.step` has no gate, so what comes back is the UNGATED answer.
        """
        gate, gated = self.gated(2)
        ungated, plain = configured(2)
        with Network(gate, 2, gated.wv, gated.wo) as network:
            got = network.run(TOKENS)
        np.testing.assert_array_equal(got, plain.run(TOKENS))
        self.assertFalse(
            np.array_equal(got, gated.run(TOKENS)),
            "the distributed run matched the GATED model, so the gate has "
            "reached the node -- delete this test and its class docstring")


@skip_if_fast
class TheSplitIsExact(unittest.TestCase):

    def test_processes_reproduce_the_single_process_model_exactly(self):
        """Not close. Identical, at every node count that divides the width.

        A difference here of any size would mean the distributed system is a
        different model from the one every sweep measured, and the whole record
        would have to be re-earned against it.
        """
        for nodes in (1, 2, 4, 8):
            with self.subTest(nodes=nodes):
                config, model = configured(nodes)
                expected = model.run(TOKENS)
                with Network(config, nodes, model.wv, model.wo) as network:
                    np.testing.assert_array_equal(network.run(TOKENS), expected)

    def test_pooling_every_node_is_partition_independent(self):
        """One node and eight give the SAME answers, and that is correct.

        Summing every node reconstructs the full matrix product exactly, so with
        a fixed readout the pooled answer cannot depend on how the width was
        cut. Partitioning matters for LEARNING, where each group fits its own
        error, and for reading one node alone. It does not matter here.

        Written as a test because the first version of it asserted the opposite
        and failed -- a good reminder that "the split must change something" is
        an intuition, not a theorem.
        """
        answers = []
        for nodes in (1, 8):
            config, model = configured(nodes)
            with Network(config, nodes, model.wv, model.wo) as network:
                answers.append(network.run(TOKENS))
        np.testing.assert_array_equal(*answers)

    def test_the_predictions_are_not_degenerate(self):
        """The real vacuity guard: a broken driver returning zeros would still
        match a broken single-process model if both were equally broken."""
        config, model = configured(4)
        with Network(config, 4, model.wv, model.wo) as network:
            predictions = network.run(TOKENS)
        self.assertGreater(len(set(predictions.tolist())), 2,
                           "predictions barely vary, so the comparison above "
                           "could pass on a model that computes nothing")

    def test_running_twice_gives_the_same_answer(self):
        """A node process outlives the sequence; its memory must not.

        The first version of the departure test failed with answers changing
        BEFORE the departure step, which is not something a departure can do.
        The cause was nodes carrying their memory into the next sequence.
        """
        config, model = configured(4)
        with Network(config, 4, model.wv, model.wo) as network:
            first = network.run(TOKENS)
            second = network.run(TOKENS)
        np.testing.assert_array_equal(first, second)


@skip_if_fast
@skip_if_fast
class RunningAheadChangesNothing(unittest.TestCase):
    """C2, over real sockets instead of a permuted array.

    A window of 1 is lock-step: every node answers before anyone advances, which
    is the global synchronisation C1 forbids. Above 1 the nodes proceed at their
    own pace and the operating system decides what arrives when.

    g2-01 established bit-identity under simulated delay. This is the same claim
    where the delay is real.
    """

    def test_every_window_gives_the_lock_step_answer(self):
        config, model = configured(8)
        expected = model.run(TOKENS)
        with Network(config, 8, model.wv, model.wo) as network:
            for window in (1, 2, 4, 8, len(TOKENS)):
                with self.subTest(window=window):
                    np.testing.assert_array_equal(
                        network.run(TOKENS, window=window), expected)

    def test_a_window_below_one_is_refused(self):
        config, model = configured(4)
        with Network(config, 4, model.wv, model.wo) as network:
            with self.assertRaises(ValueError):
                network.run(TOKENS, window=0)


@skip_if_fast
class NodesLeaveOverTheWire(unittest.TestCase):
    """A departure the driver experiences rather than one it simulates."""

    def test_answers_change_once_a_node_stops_being_asked(self):
        config, model = configured(4)
        with Network(config, 4, model.wv, model.wo) as network:
            intact = network.run(TOKENS)
            partial = network.run(TOKENS, absent={0, 1}, leave_at=4)
        self.assertTrue(np.array_equal(intact[:4], partial[:4]),
                        "answers before the departure must be untouched")
        self.assertFalse(np.array_equal(intact[4:], partial[4:]),
                         "answers after it must not be")

    def test_absent_WITHOUT_leave_at_is_silently_IGNORED(self):
        """The footgun, pinned rather than fixed.

        `absent={0}` with no `leave_at` is accepted and does nothing at all --
        measured at 0 of 3072 predictions changed on `reward_recall`, where
        adding `leave_at=1` changes 2358 of them.

        g10-08 lost three successive versions to this: it reported a
        dimension-sliced store degrading by exactly +0.000 under node loss, and
        the number was not robustness but a parameter that had never applied.

        Pinned rather than made to raise, because `leave_at` defaults to 0 and
        several existing results were produced through this path; changing the
        signature would silently alter what they mean. **A test that documents
        a trap is worth more than a fix that moves it.**
        """
        config, model = configured(4)
        with Network(config, 4, model.wv, model.wo) as network:
            intact = network.run(TOKENS)
            ignored = network.run(TOKENS, absent={0})
            applied = network.run(TOKENS, absent={0}, leave_at=1)
        np.testing.assert_array_equal(
            intact, ignored,
            "absent without leave_at now does something -- if that is a "
            "deliberate fix, delete this test and say so")
        self.assertFalse(np.array_equal(intact, applied),
                         "leave_at must make absent take effect")

    def test_a_departure_before_the_first_step_removes_them_throughout(self):
        config, model = configured(4)
        with Network(config, 4, model.wv, model.wo) as network:
            early = network.run(TOKENS, absent={0}, leave_at=0)
            late = network.run(TOKENS, absent={0}, leave_at=len(TOKENS) - 1)
        self.assertFalse(np.array_equal(early, late),
                         "when a node stopped answering made no difference")


class SlicesAreExactOrRefused(unittest.TestCase):

    def test_uneven_splits_raise_rather_than_round(self):
        """Rounding would make the exactness claim above untestable."""
        with self.assertRaises(ValueError):
            slices_for(32, 5)

    def test_slices_tile_the_width_without_gaps_or_overlap(self):
        pieces = slices_for(240, 16)
        self.assertEqual(pieces[0].lo, 0)
        self.assertEqual(pieces[-1].hi, 240)
        for before, after in zip(pieces, pieces[1:]):
            self.assertEqual(before.hi, after.lo)
        self.assertEqual(sum(p.width for p in pieces), 240)


@skip_if_fast
class TheWireCostIsWhatWasClaimed(unittest.TestCase):
    """note 012 said a token is enough. This is what a node actually receives."""

    def test_inbound_is_a_handful_of_bytes_at_any_width(self):
        narrow, narrow_model = configured(4, width=32)
        wide, wide_model = configured(4, width=256)
        with Network(narrow, 4, narrow_model.wv, narrow_model.wo) as small:
            with Network(wide, 4, wide_model.wv, wide_model.wo) as large:
                self.assertEqual(small.bytes_per_step_inbound,
                                 large.bytes_per_step_inbound)
                self.assertLessEqual(small.bytes_per_step_inbound, 16)


if __name__ == "__main__":
    unittest.main()
