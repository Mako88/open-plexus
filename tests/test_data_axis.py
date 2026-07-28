"""`--chars` has to reach the training stream, or g11-05 sweeps nothing.

g11-04 spent a full matrix and answered nothing because its backprop control was
**data-limited, not width-limited**: 250,000 characters was not enough text for a
wider attention model to have more to learn, so the reference was flat and no
comparison against it meant anything. The re-run moves data instead of width.

That only works if the axis is connected. `TRAIN_CHARS` was a module constant and
`--cap` — the flag that looks like it would do this — is the *memory store's norm
cap*, nothing to do with the corpus. A sweep passing an axis the script ignores
runs a grid of identical cells and reports a flat exponent, which is
indistinguishable from the real thing.

So: perturb the input, assert the output moves. These are the connection tests,
not the capability tests — none of them trains a model.
"""

from __future__ import annotations

import unittest
from types import SimpleNamespace

from experiments.g11_04_scaling_exponent import TRAIN_CHARS, run_one, split

#: Long enough to slice, short enough that nothing here is slow.
TEXT = "".join(chr(ord("a") + i % 26) for i in range(10_000))
CORPUS = SimpleNamespace(train=(TEXT,), test=(TEXT[:500],), vocab_size=26)


class TheDataAxisIsConnected(unittest.TestCase):

    def test_fewer_characters_means_fewer_fitting_chunks(self):
        few, _, _ = split(CORPUS, chunk=100, chars=1_000)
        many, _, _ = split(CORPUS, chunk=100, chars=8_000)
        self.assertLess(len(few), len(many))

    def test_the_fitting_text_is_actually_the_requested_length(self):
        """Not merely 'smaller'. A cap applied to the wrong slice, or applied
        after the split, still produces a monotone chunk count while putting
        every cell at a different point on the axis than the grid says."""
        for chars in (1_000, 4_000, 8_000):
            fitting, calibration, _ = split(CORPUS, chunk=100, chars=chars)
            used = sum(len(c) for c in fitting) + sum(len(c) for c in calibration)
            self.assertLessEqual(used, chars)
            self.assertGreater(used, chars - 2 * 100)

    def test_the_test_text_does_not_move_with_the_axis(self):
        """The held-out set has to be the same text at every point on the axis,
        or the arms are being scored on different problems and the exponent is
        fitted through a moving target."""
        _, _, small = split(CORPUS, chunk=100, chars=1_000)
        _, _, large = split(CORPUS, chunk=100, chars=8_000)
        self.assertEqual([list(c) for c in small], [list(c) for c in large])

    def test_the_default_is_still_g11_04s_cap(self):
        """g11-04's numbers stay comparable only while the default is unchanged.
        Changing a default invalidates the comparison set."""
        self.assertEqual(TRAIN_CHARS, 250_000)


class AskingForMoreTextThanExists(unittest.TestCase):

    def test_it_refuses_rather_than_truncating(self):
        """Silently truncating puts the top of the grid at whatever the corpus
        happens to hold, so the axis stops moving and the top cells collapse
        onto one point -- while the summary still prints the requested value."""
        with self.assertRaises(SystemExit) as raised:
            run_one((1, 64, 128, "shakespeare", "single", 99_000_000, 0, {}))
        self.assertIn("99000000", str(raised.exception).replace(",", ""))


if __name__ == "__main__":
    unittest.main()
