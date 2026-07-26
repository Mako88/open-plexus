"""The tc command, and the refusal that keeps a clean run from being mislabelled.

`testbed/run.py` needs Docker to run end to end, and that is not available in
every environment this suite runs in. But its most safety-critical part is a pure
function, so it is tested here directly.

**The property that matters is the failure case.** A node whose `tc` command
fails and which joins anyway contributes an unimpaired vote to a run labelled
impaired. That does not crash, does not look wrong, and produces a number that
would be recorded as a latency result. It has to exit instead.
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from testbed.run import impairment  # noqa: E402


class AFailedImpairmentStopsTheNode(unittest.TestCase):

    def test_the_command_exits_rather_than_continuing(self):
        command = impairment("80ms", "20ms", "2%")
        self.assertIn("exit", command,
                      "a node whose tc failed would go on to join, and its "
                      "clean vote would be recorded as an impaired one")

    def test_the_exit_comes_before_the_node_starts(self):
        """Ordering, not just presence: exiting after joining is no use."""
        command = impairment("80ms", None, None)
        self.assertTrue(command.rstrip().endswith(";"),
                        "the impairment must be a prefix to the node command")


class ItBuildsWhatWasAskedFor(unittest.TestCase):

    def test_delay_jitter_and_loss_all_appear(self):
        command = impairment("80ms", "20ms", "2%")
        for fragment in ("delay 80ms", "20ms", "loss 2%"):
            self.assertIn(fragment, command)

    def test_jitter_without_delay_is_not_emitted_alone(self):
        """netem jitter is a modifier on a delay and is meaningless without one."""
        self.assertNotIn("20ms", impairment(None, "20ms", None))

    def test_no_impairment_is_no_command(self):
        self.assertEqual(impairment(None, None, None), "",
                         "a clean run must not invoke tc at all, or it would "
                         "need NET_ADMIN it does not otherwise require")

    def test_loss_alone_is_enough_to_act(self):
        self.assertIn("loss 2%", impairment(None, None, "2%"))


if __name__ == "__main__":
    unittest.main()
