"""Experiments refuse to run while the mutation harness has the source edited.

This exists because a pre-dispatch control was one command away from being run
against a model `tools/mutate.py` had deliberately broken. It would have produced
numbers, they would have looked plausible, and **nothing in the output would have
said otherwise** — which is precisely the failure mode this project's standards
are written against: not the crash, but the thing that looks connected and is not.

The harness writes a sibling `.py.bak` before every edit and removes it after, so
its presence means either a run is in flight or a run was killed. Either way the
source on disk is not the source anyone means to measure.
"""

from __future__ import annotations

import unittest

from experiments.harness import ROOT, refuse_if_mutating


class ItRefusesWhileTheSourceIsEdited(unittest.TestCase):

    def test_a_leftover_bak_stops_everything(self):
        marker = ROOT / "openplexus" / "_guard_probe.py.bak"
        marker.write_text("# temporary, written by a test\n", encoding="utf-8")
        try:
            with self.assertRaises(SystemExit) as caught:
                refuse_if_mutating()
        finally:
            marker.unlink()
        self.assertIn("REFUSING TO RUN", str(caught.exception))

    def test_it_names_the_file(self):
        """A refusal that does not say which file is a refusal nobody can act on."""
        marker = ROOT / "openplexus" / "_guard_probe.py.bak"
        marker.write_text("# temporary\n", encoding="utf-8")
        try:
            with self.assertRaises(SystemExit) as caught:
                refuse_if_mutating()
        finally:
            marker.unlink()
        self.assertIn("_guard_probe.py.bak", str(caught.exception))

    def test_a_clean_tree_passes(self):
        """Otherwise the tests above pass on a guard that always refuses, which
        would block every experiment in the project."""
        leftovers = sorted(ROOT.glob("**/*.py.bak"))
        if leftovers:
            self.skipTest(f"harness is running: {leftovers[0].name}")
        refuse_if_mutating()


if __name__ == "__main__":
    unittest.main()
