"""What a departing node takes with it, and what the survivors keep.

G3 measured churn with a stored key table, where each node owns columns of `Wk`.
A departure then removes key dimensions that *every surviving node* needed, which
is why [note 009](../docs/notes/009-splitting-the-memory.md) had to record that
churn damage is global rather than local.

[Note 012](../docs/notes/012-broadcast-the-token.md) removed the premise. With
`derived_keys`, no node holds any of `Wk` — each computes the full key from the
token id — so a departure takes only that node's own values and readout.

The difference is worth up to 0.256 at heavy churn, so getting it wrong would
misreport how survivable the design is.
"""

from __future__ import annotations

import unittest

import numpy as np

from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

VOCAB, WIDTH = 20, 32


def build(derived: bool):
    return LocalAssociativeMemory(LocalMemoryConfig(
        vocab_size=VOCAB, d_model=WIDTH, key_scale=0.5,
        derived_keys=derived, seed=7))


class WhatDeparturesTake(unittest.TestCase):

    def test_a_stored_key_table_loses_columns_when_a_node_leaves(self):
        """The old model, and still correct when keys are a stored table."""
        model = build(False)
        model.ablate(range(8))
        self.assertTrue((model.wk[:, :8] == 0).all(),
                        "a node holding key columns should take them")
        self.assertTrue((model.wv[:, :8] == 0).all())
        self.assertTrue((model.wo[:, :8] == 0).all())

    def test_derived_keys_survive_a_departure_untouched(self):
        """The correction: nothing to take, so nothing is taken."""
        model = build(True)
        before = model.wk.copy()
        model.ablate(range(8))
        np.testing.assert_array_equal(
            model.wk, before,
            "a node that derives its keys holds none of Wk, so a departure "
            "must not remove any")
        self.assertTrue((model.wv[:, :8] == 0).all(),
                        "it should still take its own values")
        self.assertTrue((model.wo[:, :8] == 0).all(),
                        "and its own readout")

    def test_survivors_still_retrieve_through_the_full_key(self):
        """The consequence that matters, stated through behaviour.

        A surviving node's retrieval is `M[R_g,:] k`, which runs over every
        dimension of `k`. If a departure had blanked part of `k`, the survivors'
        retrievals would change even for keys they still hold rows for. This
        checks the key a survivor uses is the same before and after.
        """
        model = build(True)
        token = 5
        before = model.wk[token].copy()
        model.ablate(range(8))
        np.testing.assert_array_equal(model.wk[token], before)
        self.assertGreater(np.linalg.norm(model.wk[token]), 0.0)


class DeparturesArePermanent(unittest.TestCase):
    """C3's failure is not a dropped message; the node does not come back."""

    def test_a_departed_node_contributes_nothing_afterwards(self):
        model = build(True)
        model.ablate(range(8))
        tokens = np.array([3, 9, 1, 7, 3])
        # Writing to a dead node's readout must not revive it: its values are
        # zero, so its retrieved slice stays zero whatever the readout says.
        model.wo[:, :8] = 1.0
        first = model.run(tokens)
        model.wo[:, :8] = -1.0
        np.testing.assert_array_equal(
            model.run(tokens), first,
            "a departed node still influences the answer, so it did not "
            "actually leave")

    def test_surviving_width_counts_only_the_living(self):
        model = build(True)
        self.assertEqual(model.surviving_width(), WIDTH)
        model.ablate(range(8))
        self.assertEqual(model.surviving_width(), WIDTH - 8)


if __name__ == "__main__":
    unittest.main()
