"""Parallelism that changes an answer is not a speedup, it is a bug.

`spread` exists because the matrix already spreads jobs across runners but
nothing uses the cores *within* one -- the arrays here are small enough that BLAS
will not thread them, so a four-core runner runs a job on about one core.

The only property that matters is that **the worker count is invisible in the
results**. A sweep whose numbers move when it is parallelised has not been made
faster; it has been made wrong, and the wrongness would show up as a scientific
finding rather than as a crash.
"""

from __future__ import annotations

import os
import unittest

from experiments.harness import spread


def square(value: int) -> int:
    return value * value


def where_am_i(value: int) -> tuple[int, int]:
    """Returns the value and the pid that computed it."""
    return value, os.getpid()


class TheWorkerCountIsInvisible(unittest.TestCase):

    def test_one_worker_and_several_agree(self):
        items = list(range(12))
        self.assertEqual(spread(square, items, 1), spread(square, items, 3))

    def test_order_is_preserved(self):
        items = [5, 1, 4, 2, 3]
        self.assertEqual(spread(square, items, 3), [25, 1, 16, 4, 9])

    def test_zero_and_negative_workers_run_serially_rather_than_failing(self):
        """A misconfigured job should be slow, never broken."""
        self.assertEqual(spread(square, [2, 3], 0), [4, 9])
        self.assertEqual(spread(square, [2, 3], -1), [4, 9])


class ItActuallyLeavesThisProcess(unittest.TestCase):
    """Without this, every test above passes on a `spread` that ignores its
    worker count entirely and runs serially -- which is exactly what the
    `workers <= 1` branch does, so the mistake is one character away."""

    def test_several_workers_use_other_processes(self):
        pids = {pid for _, pid in spread(where_am_i, list(range(8)), 3)}
        self.assertNotIn(os.getpid(), pids,
                         "work ran in the calling process, so nothing was "
                         "parallelised and the tests above prove nothing")

    def test_one_worker_stays_here(self):
        pids = {pid for _, pid in spread(where_am_i, list(range(4)), 1)}
        self.assertEqual(pids, {os.getpid()})


if __name__ == "__main__":
    unittest.main()
