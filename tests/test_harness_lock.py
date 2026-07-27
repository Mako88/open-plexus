"""Two mutation harnesses must not run at once.

I started a second run while a background one was still going. The second called
`restore_any_leftovers()` on startup — **correct behaviour for recovering from a
killed run** — and in doing so reverted the first run's in-flight mutation. Both
sets of results became meaningless, and neither said so: the second printed a
confident `4/4 caught`.

The `.bak` convention cannot prevent this, because it cannot distinguish *a run
died and left this* from *a run is using this right now*, and those want opposite
responses. A lock carrying a PID can.

The conservative direction matters: an unknown PID counts as **alive**. A false
"alive" costs a wait; a false "dead" costs two harnesses interleaving edits,
which is the failure being prevented.
"""

from __future__ import annotations

import os
import unittest

from tools import mutate


class TheLockStopsAConcurrentRun(unittest.TestCase):

    def setUp(self):
        self.held = mutate.LOCK.exists()
        if self.held:
            self.skipTest("a harness is running; its lock is not ours to move")

    def tearDown(self):
        mutate.release_the_lock()

    def test_a_lock_owned_by_a_live_process_refuses(self):
        mutate.LOCK.write_text(str(os.getpid()), encoding="utf-8")
        with self.assertRaises(SystemExit) as caught:
            mutate.claim_the_lock()
        self.assertIn("another mutation harness is running",
                      str(caught.exception))

    def test_a_lock_owned_by_a_dead_process_is_cleared(self):
        """Otherwise a killed run locks the harness out permanently, and the
        first thing anyone would do is delete the check."""
        mutate.LOCK.write_text("999999999", encoding="utf-8")
        mutate.claim_the_lock()
        self.assertEqual(mutate.LOCK.read_text(encoding="utf-8").strip(),
                         str(os.getpid()))

    def test_an_unreadable_lock_does_not_wedge_it(self):
        mutate.LOCK.write_text("not a pid", encoding="utf-8")
        mutate.claim_the_lock()
        self.assertEqual(mutate.LOCK.read_text(encoding="utf-8").strip(),
                         str(os.getpid()))

    def test_claiming_writes_our_own_pid(self):
        mutate.claim_the_lock()
        self.assertEqual(mutate.LOCK.read_text(encoding="utf-8").strip(),
                         str(os.getpid()))

    def test_releasing_removes_it(self):
        mutate.claim_the_lock()
        mutate.release_the_lock()
        self.assertFalse(mutate.LOCK.exists())

    def test_releasing_a_lock_that_is_gone_is_not_an_error(self):
        """`finally: release()` runs even when the claim never happened."""
        mutate.release_the_lock()
        mutate.release_the_lock()


class LivenessIsConservative(unittest.TestCase):

    def test_our_own_process_is_alive(self):
        self.assertTrue(mutate._is_running(os.getpid()))

    def test_an_implausible_pid_is_not(self):
        """Without this the liveness check could return True always, and the
        stale-lock path above would never run."""
        self.assertFalse(mutate._is_running(999999999))


if __name__ == "__main__":
    unittest.main()
