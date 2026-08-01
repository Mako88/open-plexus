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


class AGraphHoldsWhatTheRunDeclared(unittest.TestCase):
    """The check counting instances cannot make. `expect(graph=1)` passes on
    every arm in this project and always did; what was never true is that any
    one graph held pictures AND sounds AND words AND facts."""

    def setUp(self):
        wiring.reset()

    def test_all_the_declared_kinds_arriving_passes(self):
        with wiring.expect(holding={"image", "audio", "word"}):
            for name in ("image", "audio", "word"):
                wiring.kind(name)

    def test_A_KIND_THAT_NEVER_ARRIVES_FAILS(self):
        """The real case, and the one this module exists for."""
        with self.assertRaises(wiring.WiringError) as caught:
            with wiring.expect(holding={"image", "audio", "word", "fact"}):
                wiring.kind("image")
                wiring.kind("word")
        said = str(caught.exception)
        self.assertIn("audio", said)
        self.assertIn("fact", said)

    def test_AN_UNDECLARED_KIND_FAILS_TOO(self):
        """The companion. A merge that quietly gains a kind nobody declared is
        doing something its author did not describe."""
        with self.assertRaises(wiring.WiringError) as caught:
            with wiring.expect(holding={"image"}):
                wiring.kind("image")
                wiring.kind("audio")
        self.assertIn("audio", str(caught.exception))

    def test_repeats_do_not_change_the_verdict(self):
        """Kinds are a SET. A graph fed a thousand pictures and one sound holds
        both, and how lopsided that is belongs to a different measurement."""
        with wiring.expect(holding={"image", "audio"}):
            for _ in range(50):
                wiring.kind("image")
            wiring.kind("audio")

    def test_declaring_no_kinds_checks_none(self):
        with wiring.expect(graph=1):
            CoOccurrence()
            wiring.kind("anything-at-all")


class KindsMustNotSHARE_NodeNumbers(unittest.TestCase):
    """The fault the kind check is blind to, and the merge's real risk.

    Every source in this project numbers from zero. Merged naively, image code
    0 and concept surface 0 and entity 0 are ONE node: every declared kind
    arrives, `holding` passes, and the counts are silently added together.
    """

    def setUp(self):
        wiring.reset()

    def test_a_namespaced_merge_passes(self):
        with wiring.expect(holding={"image", "fact"}, disjoint=True):
            wiring.kind("image", range(0, 100))
            wiring.kind("fact", range(100, 200))

    def test_COLLIDING_IDS_FAIL(self):
        with self.assertRaises(wiring.WiringError) as caught:
            with wiring.expect(holding={"image", "fact"}, disjoint=True):
                wiring.kind("image", range(0, 100))
                wiring.kind("fact", range(0, 100))
        self.assertIn("share 100", str(caught.exception))

    def test_ONE_shared_id_is_enough_to_fail(self):
        """A boundary that only fires on wholesale collision would pass the
        off-by-one that is the likeliest real mistake."""
        with self.assertRaises(wiring.WiringError):
            with wiring.expect(disjoint=True):
                wiring.kind("image", range(0, 100))
                wiring.kind("fact", range(99, 200))

    def test_the_kind_check_ALONE_would_pass_a_collision(self):
        """States the blind spot as a test, so nobody re-derives it. Without
        `disjoint`, a fully collided merge is declared healthy."""
        with wiring.expect(holding={"image", "fact"}):
            wiring.kind("image", range(0, 100))
            wiring.kind("fact", range(0, 100))

    def test_overlaps_reports_nothing_when_namespaced(self):
        wiring.kind("image", range(0, 10))
        wiring.kind("fact", range(10, 20))
        self.assertEqual(wiring.overlaps(), {})


if __name__ == "__main__":
    unittest.main()
