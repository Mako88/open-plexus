"""Tests for the linear algebra, the frozen substrate, and the readout.

The substrate is the control every later claim is measured against, so a
substrate that silently ignored half its configuration would quietly set the bar
in the wrong place. Most of what follows is rule 6: perturb an input, assert the
output moves.
"""

from __future__ import annotations

import unittest
from dataclasses import replace

from openplexus import linalg
from openplexus.models.readout import RidgeReadout
from openplexus.models.reservoir import Reservoir, ReservoirConfig

CFG = ReservoirConfig(n_units=24, seed=3)
VOCAB = 12


class TestSolve(unittest.TestCase):
    def test_solves_a_system_with_a_known_answer(self):
        # 2x + y = 5 ; x + 3y = 10  ->  x = 1, y = 3
        x = linalg.solve([[2.0, 1.0], [1.0, 3.0]], [[5.0], [10.0]])
        self.assertAlmostEqual(x[0][0], 1.0, places=9)
        self.assertAlmostEqual(x[1][0], 3.0, places=9)

    def test_solves_multiple_right_hand_sides_at_once(self):
        x = linalg.solve([[2.0, 0.0], [0.0, 4.0]], [[2.0, 6.0], [4.0, 8.0]])
        self.assertAlmostEqual(x[0][0], 1.0)
        self.assertAlmostEqual(x[0][1], 3.0)
        self.assertAlmostEqual(x[1][0], 1.0)
        self.assertAlmostEqual(x[1][1], 2.0)

    def test_needs_pivoting_to_get_this_right(self):
        """A zero in the first pivot position. Without partial pivoting this
        divides by zero instead of solving."""
        x = linalg.solve([[0.0, 1.0], [1.0, 0.0]], [[3.0], [7.0]])
        self.assertAlmostEqual(x[0][0], 7.0)
        self.assertAlmostEqual(x[1][0], 3.0)

    def test_singular_raises_rather_than_returning_a_plausible_answer(self):
        """A silently wrong solve would produce a readout score that looks like
        a measurement."""
        with self.assertRaises(ValueError):
            linalg.solve([[1.0, 2.0], [2.0, 4.0]], [[1.0], [2.0]])

    def test_spectral_radius_of_a_diagonal_matrix_is_its_largest_entry(self):
        r = linalg.spectral_radius([[0.5, 0.0, 0.0], [0.0, -2.0, 0.0], [0.0, 0.0, 1.0]])
        self.assertAlmostEqual(r, 2.0, places=6)


class TestReservoirIsConnected(unittest.TestCase):
    """Each of these would pass against a substrate ignoring the field it names."""

    def test_state_has_one_vector_per_position_of_the_right_width(self):
        states = Reservoir(CFG, VOCAB).run((1, 2, 3, 4, 5))
        self.assertEqual(len(states), 5)
        self.assertTrue(all(len(s) == CFG.n_units for s in states))

    def test_same_config_gives_identical_states(self):
        tokens = (1, 5, 2, 8, 3)
        self.assertEqual(Reservoir(CFG, VOCAB).run(tokens),
                         Reservoir(CFG, VOCAB).run(tokens))

    def test_seed_changes_the_substrate(self):
        tokens = (1, 5, 2, 8, 3)
        self.assertNotEqual(Reservoir(replace(CFG, seed=1), VOCAB).run(tokens),
                            Reservoir(replace(CFG, seed=2), VOCAB).run(tokens))

    def test_input_actually_reaches_the_state(self):
        """The connection test. A substrate whose input weights were unused
        would produce identical states for different sequences while looking
        entirely healthy."""
        r = Reservoir(CFG, VOCAB)
        self.assertNotEqual(r.run((1, 1, 1, 1))[-1], r.run((1, 1, 1, 7))[-1])

    def test_state_does_not_depend_on_later_tokens(self):
        """Causality. A substrate that peeked would score for reasons unrelated
        to the task and would raise the bar every model is compared against."""
        r = Reservoir(CFG, VOCAB)
        a = r.run((1, 2, 3, 4, 5, 6))
        b = r.run((1, 2, 3, 9, 9, 9))
        self.assertEqual(a[:3], b[:3])

    def test_n_units_changes_the_state_width(self):
        self.assertEqual(len(Reservoir(replace(CFG, n_units=8), VOCAB).run((1,))[0]), 8)
        self.assertEqual(len(Reservoir(replace(CFG, n_units=40), VOCAB).run((1,))[0]), 40)

    def test_spectral_radius_changes_how_long_the_state_remembers(self):
        """The dial that sets memory length. If scaling were disconnected, the
        substrate's capacity would be whatever the random draw happened to give
        and would not respond to configuration at all.

        Measured as how much a one-token perturbation still moves the state 25
        steps later. A larger radius must retain more.
        """
        def persistence(radius):
            r = Reservoir(replace(CFG, spectral_radius=radius), VOCAB)
            base = r.run((3,) + (1,) * 25)[-1]
            perturbed = r.run((9,) + (1,) * 25)[-1]
            return sum(abs(x - y) for x, y in zip(base, perturbed))

        self.assertGreater(persistence(1.1), persistence(0.3))

    def test_leak_changes_the_state(self):
        tokens = (1, 5, 2, 8, 3)
        self.assertNotEqual(Reservoir(replace(CFG, leak=0.1), VOCAB).run(tokens),
                            Reservoir(replace(CFG, leak=0.9), VOCAB).run(tokens))

    def test_input_scale_changes_the_state(self):
        tokens = (1, 5, 2, 8, 3)
        self.assertNotEqual(Reservoir(replace(CFG, input_scale=0.1), VOCAB).run(tokens),
                            Reservoir(replace(CFG, input_scale=2.0), VOCAB).run(tokens))

    def test_rejects_a_token_outside_the_vocabulary(self):
        with self.assertRaises(ValueError):
            Reservoir(CFG, VOCAB).run((0, VOCAB))

    def test_rejects_impossible_configurations(self):
        for bad in (dict(n_units=0), dict(leak=0.0), dict(leak=1.5),
                    dict(density=0.0), dict(spectral_radius=0.0)):
            with self.subTest(**bad):
                with self.assertRaises(ValueError):
                    ReservoirConfig(**bad)


class TestReadout(unittest.TestCase):
    def test_learns_a_separable_mapping(self):
        states = [[1.0, 0.0], [0.9, 0.1], [0.0, 1.0], [0.1, 0.9]]
        labels = ["a", "a", "b", "b"]
        readout = RidgeReadout().fit(states, labels)
        self.assertEqual(readout.predict([0.95, 0.05]), "a")
        self.assertEqual(readout.predict([0.05, 0.95]), "b")

    def test_fitting_data_actually_changes_predictions(self):
        """The connection test for the readout. A decoder ignoring its labels
        would report the substrate's score as whatever one class happened to be
        called."""
        states = [[1.0, 0.0], [0.0, 1.0]]
        a = RidgeReadout().fit(states, ["x", "y"])
        b = RidgeReadout().fit(states, ["y", "x"])
        self.assertNotEqual(a.predict([1.0, 0.0]), b.predict([1.0, 0.0]))

    def test_ridge_must_be_positive(self):
        with self.assertRaises(ValueError):
            RidgeReadout(ridge=0.0)

    def test_unfitted_readout_raises_rather_than_guessing(self):
        with self.assertRaises(ValueError):
            RidgeReadout().predict([1.0, 2.0])

    def test_rejects_mismatched_state_width(self):
        readout = RidgeReadout().fit([[1.0, 0.0], [0.0, 1.0]], ["a", "b"])
        with self.assertRaises(ValueError):
            readout.predict([1.0, 0.0, 0.0])


if __name__ == "__main__":
    unittest.main()
