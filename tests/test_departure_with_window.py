"""A node leaving while the driver is running ahead — C1 and C3 together.

Every departure test in this repository runs at **window 1**, and every window
test runs with **no departure**. The combination was never exercised, and it
deadlocked.

Why it matters more than an ordinary gap: window 1 is lock-step, which is exactly
the global synchronisation **C1 forbids**, and departure is **C3**. So the one
configuration this project actually cares about — asynchronous *and* losing
machines — was the one configuration nothing covered. It was found by the
container testbed on the first run that combined them.

The bug: the driver excluded departed nodes from the socket read set. Running
ahead means a node can have answered steps 20–29 and then be dropped at step 30,
and those answers — already sent, sitting unread in the socket — were never
collected. The step never reached its expected count and the run timed out
**before the departure it was testing**.

A departure stops a node being *sent to*. It cannot un-send a vote.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.distributed import Network
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH, NODES = 24, 16, 4
TOKENS = np.random.default_rng(17).integers(0, VOCAB, 80)
LEAVE_AT = 40


def config() -> LocalMemoryConfig:
    return LocalMemoryConfig(vocab_size=VOCAB, d_model=WIDTH, lr=0.05,
                             key_scale=0.5, decay=0.9, derived_keys=True,
                             seed=5)


def drive(window: int, absent=None, leave_at=None) -> np.ndarray:
    model = LocalAssociativeMemory(config())
    model.wo[:] = model.wv       # or every node is interchangeable
    with Network(config(), NODES, model.wv, model.wo) as net:
        return net.run(TOKENS, absent=absent, leave_at=leave_at, window=window)


class ADepartureDoesNotDeadlockAWindowedRun(unittest.TestCase):

    def test_a_node_leaves_while_the_driver_runs_ahead(self):
        """The deadlock. Before the fix this raised TimeoutError at a step
        EARLIER than the departure, having waited 30 seconds for votes that were
        already sitting in a socket it had stopped reading."""
        result = drive(window=8, absent={1}, leave_at=LEAVE_AT)
        self.assertEqual(len(result), len(TOKENS))

    def test_it_works_at_several_windows(self):
        for window in (1, 2, 4, 8, 16):
            with self.subTest(window=window):
                result = drive(window=window, absent={1}, leave_at=LEAVE_AT)
                self.assertEqual(len(result), len(TOKENS))


class TheWindowDoesNotChangeWhatADepartureMeans(unittest.TestCase):
    """The invariant the fix is really about.

    Running ahead is a scheduling choice. It must not alter the answer, with or
    without a node leaving — otherwise obeying C1 costs correctness, and the
    7.3x speedup in note 014 was bought with something.
    """

    def test_every_window_gives_the_same_answer_under_departure(self):
        answers = [drive(window=w, absent={1}, leave_at=LEAVE_AT)
                   for w in (1, 2, 8)]
        for window, answer in zip((2, 8), answers[1:]):
            np.testing.assert_array_equal(
                answers[0], answer,
                f"window {window} disagreed with lock-step under a departure, "
                f"so running ahead is changing the result rather than the "
                f"schedule")

    def test_answers_before_the_departure_are_untouched(self):
        """Causality. A node leaving at step 40 cannot change step 39."""
        whole = drive(window=8)
        departed = drive(window=8, absent={1}, leave_at=LEAVE_AT)
        np.testing.assert_array_equal(
            whole[:LEAVE_AT], departed[:LEAVE_AT],
            "answers before the departure step changed, so something other "
            "than the departure caused them")

    def test_and_the_departure_does_change_something_after_it(self):
        """Otherwise the test above passes on a departure that never happened."""
        whole = drive(window=8)
        departed = drive(window=8, absent={1}, leave_at=LEAVE_AT)
        self.assertFalse(
            np.array_equal(whole[LEAVE_AT:], departed[LEAVE_AT:]),
            "losing a quarter of the network changed nothing after the "
            "departure, so this fixture cannot detect a departure at all")


if __name__ == "__main__":
    unittest.main()
