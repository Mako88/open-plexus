"""What a late-signal gate costs a node, and the dependency that makes it cheap.

Note 015's first cost model made competitive capture look cheap and was wrong in
the direction that flattered the mechanism. The corrected version showed the
obvious implementation is MORE expensive than superposition for exactly the tiny
nodes this project exists for, and that it survives only because keys are
derived.

`tools/gate_cost.py` is that arithmetic for the g9 line, in code rather than in a
note, for that reason. These pin the two facts a reader would otherwise have to
re-derive: that the cost of remembering what to undo does not depend on the
node's width, and that it explodes without derived keys.
"""

from __future__ import annotations

import unittest

from tools import gate_cost


class RememberingWhatToUndoDoesNotScaleWithWidth(unittest.TestCase):
    """The whole shape of the result. A late signal means writing everything and
    undoing what nothing vouched for, and undoing needs a record of the writes --
    which is one entry per write however wide the node is."""

    def test_subtract_ignores_width_when_keys_are_derived(self):
        costs = {gate_cost.subtract_cost(width, 256) for width in (1, 8, 64)}
        self.assertEqual(len(costs), 1,
                         "the cost of the pending list moved with width, so an "
                         "entry is carrying something wider than a token id")

    def test_the_store_it_gates_does_scale_with_width(self):
        """Guard: if both were flat the crossover below would be meaningless."""
        self.assertLess(gate_cost.superposed(1, 256),
                        gate_cost.superposed(8, 256))

    def test_they_cross_and_the_crossover_is_where_it_says(self):
        d_model = 256
        width = gate_cost.crossover(d_model)
        self.assertAlmostEqual(gate_cost.superposed(width, d_model),
                               gate_cost.subtract_cost(1, d_model), places=6)

    def test_below_the_crossover_the_gate_costs_more_than_the_memory(self):
        """A width-1 node at d_model 256 spends more remembering what it might
        undo than it spends on the memory itself. It is the only cell in the
        table where that is true, and it is the cell this project cares most
        about."""
        d_model = 256
        self.assertLess(gate_cost.crossover(d_model), 2.0)
        self.assertGreater(gate_cost.subtract_cost(1, d_model),
                           gate_cost.superposed(1, d_model))

    def test_rebuild_is_the_cheaper_implementation_exactly_there(self):
        """And only there. The two implementations swap over, so 'what does the
        gate cost' has no single answer -- it has a width."""
        self.assertLess(gate_cost.rebuild_cost(1, 256),
                        gate_cost.subtract_cost(1, 256))
        self.assertGreater(gate_cost.rebuild_cost(8, 256),
                           gate_cost.subtract_cost(8, 256))


class ItRestsOnDerivedKeys(unittest.TestCase):
    """The same hard dependency note 015 named for competitive capture, now
    carrying the entire g9 line. If `derived_keys` is ever withdrawn, the reward
    gate and the tag go with it."""

    def test_without_derived_keys_a_tiny_node_cannot_afford_it(self):
        stored = gate_cost.subtract_cost(1, 256, derived_keys=False)
        self.assertGreater(stored / gate_cost.superposed(1, 256), 100,
                           "storing keys should make the gate cost two orders "
                           "of magnitude more than the memory it gates")

    def test_the_penalty_shrinks_with_width_but_never_vanishes(self):
        """An entry must carry the FULL key, because retrieval sums over every
        dimension. So the cost stops depending on this node's width and starts
        depending on the whole network's -- which is note 012's argument
        arriving at a second mechanism."""
        ratios = [gate_cost.subtract_cost(w, 256, derived_keys=False)
                  / gate_cost.superposed(w, 256) for w in (1, 8, 64)]
        self.assertGreater(ratios[0], ratios[1])
        self.assertGreater(ratios[1], ratios[2])
        self.assertGreater(ratios[-1], 1.0,
                           "even a wide node pays more for the gate than for "
                           "its own slice of the store")


if __name__ == "__main__":
    unittest.main()
