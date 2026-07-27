"""Which axis a scaling grid moved, decided from the records.

`summarise_scaling_exponent` fits `bits ~ axis^b` PER ARM. Two things follow,
and they pull in opposite directions:

- A second axis moving **inside** an arm confounds that arm's slope, and the
  tool must refuse rather than pick one silently.
- A difference **between** arms is often the comparison itself. g11-06 runs a
  cache arm against a wider superposed arm holding the same number of values,
  precisely so the extra state is controlled for. `width` differs across arms
  by design and is constant within each.

So the check is per-arm. A global one would refuse the controlled grid, and the
obvious repair -- deleting the check -- would stop it refusing the grids it
exists for.
"""

from __future__ import annotations

import unittest

from tools.summarise_scaling_exponent import axis_of


def record(arm: str, chars: int, width: int) -> dict:
    return {"arm": arm, "chars": chars, "width": width, "bits_calibrated": 5.0}


class ReadingTheAxisOffTheGrid(unittest.TestCase):

    def test_a_data_sweep_at_one_width_reads_as_chars(self):
        rows = [record("single", n, 64) for n in (1000, 2000, 4000)]
        self.assertEqual(axis_of(rows), "chars")

    def test_a_width_sweep_at_one_data_size_reads_as_width(self):
        rows = [record("single", 1000, d) for d in (16, 32, 64)]
        self.assertEqual(axis_of(rows), "width")

    def test_width_differing_BETWEEN_arms_is_not_a_second_axis(self):
        """The state-matched control: a cache arm at width 64 against a
        superposed arm at width 143 holding the same number of values. Width is
        the comparison, not a confound, and it is constant within each arm."""
        rows = ([record("cache128", n, 64) for n in (1000, 2000, 4000)]
                + [record("single", n, 143) for n in (1000, 2000, 4000)])
        self.assertEqual(axis_of(rows), "chars")

    def test_two_axes_moving_INSIDE_one_arm_is_refused(self):
        """This is the grid the check exists for. A slope fitted through it is
        confounded, and it would look exactly like a clean result."""
        rows = [record("single", 1000, 16), record("single", 2000, 32),
                record("single", 4000, 64)]
        with self.assertRaises(SystemExit) as raised:
            axis_of(rows)
        self.assertIn("within a single arm", str(raised.exception))

    def test_one_arm_confounded_refuses_the_whole_file(self):
        """A clean arm alongside a confounded one does not rescue it -- the
        confounded arm's exponent would still be printed and read."""
        rows = ([record("clean", n, 64) for n in (1000, 2000, 4000)]
                + [record("muddled", 1000, 16), record("muddled", 2000, 32)])
        with self.assertRaises(SystemExit):
            axis_of(rows)

    def test_a_grid_that_moved_nothing_falls_back_to_width(self):
        rows = [record("single", 1000, 64), record("single", 1000, 64)]
        self.assertEqual(axis_of(rows), "width")

    def test_records_lacking_the_field_do_not_count_as_variation(self):
        """g11-04's artifacts predate `chars`. Reading a missing field as a
        distinct value would make every old sweep look two-axis."""
        rows = [{"arm": "single", "width": d, "bits_calibrated": 5.0}
                for d in (16, 32, 64)]
        self.assertEqual(axis_of(rows), "width")


if __name__ == "__main__":
    unittest.main()
