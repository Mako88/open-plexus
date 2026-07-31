"""The pin check has to refuse a bare constant and accept a cited one.

**This does not duplicate** `tools/check_constants.py`'s own run, which answers
*"does today's tree carry a new unprovenanced pin"*. That run is green whenever
nobody has added one, which is most days — so it says nothing about whether the
detector works. These fixtures say that.

## Why it exists

`CLAUDE.md`'s carried-constant rule has four calibrations, the largest worth
**0.71** (`g41-01`, `d_model` on CLUTRR) and the next **0.58** (`g9-11`, `slots`
carried from a node width it was not chosen at). `g9-09` named the risk in its
own file before dispatch and `g9-11` repeated it one sweep later, which is the
evidence that a warning is not the instrument.

**The negative case is the load-bearing one.** A help string reading *"width of
the model"* explains the parameter and says nothing about where the value came
from, and a check that accepted it would be green everywhere while catching
nothing — which is exactly the vacuous-region failure `tools/mutate.py` exists
to find elsewhere.
"""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from tools.check_constants import PROVENANCE, pins

BARE = """
WIDTH = 256
"""

CITED = """
#: note 065's width, carried into every CLUTRR figure. `g41-01` measured it
#: undertuned at depth.
WIDTH = 256
"""

CHOSEN_HERE = """
# Chosen for this grid rather than carried.
WIDTH = 256
"""

ARGPARSE_BARE = """
def build(parser):
    parser.add_argument("--beam-width", type=int, default=4)
"""

ARGPARSE_EXPLAINED = """
def build(parser):
    parser.add_argument("--beam-width", type=int, default=4,
                        help="how many partial walks the beam keeps")
"""

ARGPARSE_CITED = """
def build(parser):
    # note 065's beam width, carried.
    parser.add_argument("--beam-width", type=int, default=4)
"""


def unprovenanced(source: str) -> list[str]:
    """The names this source would be pulled up for."""
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "probe.py"
        path.write_text(source, encoding="utf-8")
        return [name for name, _, context in pins(path)
                if not PROVENANCE.search(context)]


class ABarePinIsRefused(unittest.TestCase):

    def test_a_module_constant_with_no_comment(self):
        self.assertEqual(unprovenanced(BARE), ["WIDTH"])

    def test_and_a_CITED_one_passes(self):
        # THE COMPANION. Without it the check above passes for a detector that
        # flags everything, which would be suppressed within a day.
        self.assertEqual(unprovenanced(CITED), [])

    def test_saying_it_was_chosen_here_also_passes(self):
        # A value genuinely chosen for this run has no cell to cite, and
        # refusing it would push people to invent citations.
        self.assertEqual(unprovenanced(CHOSEN_HERE), [])


class AnArgparseDefaultIsAPinToo(unittest.TestCase):
    """The shape that carried `note 065`'s beam width into `g41-01`'s finding.

    `tools/clutrr_recovery.py` pinned `--width 64`, `--branches 4` and
    `--beam-width 4` as bare argparse defaults. None was a module constant, so a
    check looking only at `NAME = value` would have missed every one.
    """

    def test_a_bare_default_is_flagged(self):
        self.assertEqual(unprovenanced(ARGPARSE_BARE), ["--beam-width"])

    def test_a_help_string_that_EXPLAINS_is_still_flagged(self):
        # THE NEGATIVE THAT MATTERS. "how many partial walks the beam keeps"
        # describes the parameter and says nothing about where 4 came from. A
        # check that accepted prose would pass on almost every argument in the
        # repository and catch nothing.
        self.assertEqual(unprovenanced(ARGPARSE_EXPLAINED), ["--beam-width"])

    def test_a_cited_default_passes(self):
        self.assertEqual(unprovenanced(ARGPARSE_CITED), [])


class TheRealTreeIsCoveredBothWays(unittest.TestCase):

    def test_it_finds_pins_of_both_kinds_in_the_repo(self):
        """A fixture-only test would pass against a scanner pointed at nothing.

        Naming two real files rather than globbing, so a rename fails loudly
        instead of going vacuous.
        """
        from tools.check_constants import ROOT
        module = pins(ROOT / "experiments"
                      / "g41_01_the_pipeline_on_the_published_protocol.py")
        argument = pins(ROOT / "tools" / "clutrr_recovery.py")
        self.assertIn("BRANCHES", [n for n, _, _ in module])
        self.assertIn("--beam-width", [n for n, _, _ in argument])


if __name__ == "__main__":
    unittest.main()
