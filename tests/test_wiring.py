"""A run that declares its architecture, and fails when it used another one.

`openplexus/wiring.py` exists because three separate `CoOccurrence` instances
lived in a project whose design says one, and every part was individually wired
and tested. Nothing asked how many there were.

So the thing worth fixing is that it COUNTS rather than notices:

- **too many fails, and so does too few** — a run that declares a graph and
  builds none has not passed a weaker test, it has failed to do the thing;
- **a real exception wins** — reporting a wiring mismatch caused by a crash
  halfway through would bury the crash;
- **unnamed parts are ignored**, so a declaration says what a run is about
  rather than enumerating everything a process touches.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from openplexus import wiring  # noqa: E402
from openplexus.grounding import CoOccurrence  # noqa: E402


class ItCountsInstances(unittest.TestCase):

    def setUp(self):
        wiring.reset()

    def test_the_declared_count_passes(self):
        with wiring.expect(graph=1):
            CoOccurrence()

    def test_TOO_MANY_fails(self):
        with self.assertRaises(wiring.WiringError):
            with wiring.expect(graph=1):
                CoOccurrence()
                CoOccurrence()

    def test_TOO_FEW_fails_as_well(self):
        """The companion. A check that only caught extras would pass a run
        whose graph was never built at all."""
        with self.assertRaises(wiring.WiringError):
            with wiring.expect(graph=1):
                pass

    def test_the_count_FOLLOWS_what_was_built(self):
        """The connection test. A check hard-coded to one would pass both of
        the tests above by accident."""
        with wiring.expect(graph=3):
            CoOccurrence()
            CoOccurrence()
            CoOccurrence()

    def test_the_message_names_the_part_and_both_numbers(self):
        with self.assertRaises(wiring.WiringError) as caught:
            with wiring.expect(graph=1):
                CoOccurrence()
                CoOccurrence()
        said = str(caught.exception)
        self.assertIn("graph", said)
        self.assertIn("declared 1", said)
        self.assertIn("built 2", said)


class ItStaysOutOfTheWay(unittest.TestCase):

    def setUp(self):
        wiring.reset()

    def test_an_unnamed_part_is_ignored(self):
        with wiring.expect(graph=1):
            CoOccurrence()
            wiring.touch("something-else-entirely")

    def test_a_real_exception_wins(self):
        """A wiring mismatch caused by a crash must not replace the crash."""
        with self.assertRaises(ZeroDivisionError):
            with wiring.expect(graph=1):
                1 / 0

    def test_a_block_that_declares_nothing_never_raises(self):
        with wiring.expect():
            CoOccurrence()
            CoOccurrence()


if __name__ == "__main__":
    unittest.main()
