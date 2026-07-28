"""Naming a model by what it is made of, so two models cannot share an arm.

`--mode` conflated arm identity with component choice. g11-06 needed a `matched`
arm that is the SAME model as `single` under a different name, purely so the
summariser would not average two configurations into one column — the arm name
was carrying information the config could not express.

The failure this prevents is specific and has happened: **two different models
recorded under one arm name**, averaged together, reported as one number. It runs
cleanly and announces nothing.
"""

from __future__ import annotations

import unittest

from experiments.components import CHOICES, DEFAULTS, grid, parse
from openplexus.models.local_memory import LocalMemoryConfig


class ASpecBecomesAConfig(unittest.TestCase):

    def test_an_empty_spec_is_the_baseline(self):
        overrides, name = parse("")
        self.assertEqual(overrides, {})
        self.assertEqual(name, "keys=dense,retrieval=plain,readout=linear,"
                               "write=plain")

    def test_choices_become_overrides(self):
        overrides, _ = parse("keys=sparse4,retrieval=cache128,readout=hidden128")
        self.assertEqual(overrides,
                         {"key_active": 4, "cache_slots": 128, "hidden": 128})

    def test_an_omitted_component_takes_its_default(self):
        overrides, name = parse("readout=hidden64")
        self.assertEqual(overrides, {"hidden": 64})
        self.assertEqual(name, "keys=dense,retrieval=plain,readout=hidden64,"
                               "write=plain")

    def test_the_overrides_actually_build(self):
        """A spec that names a combination the model refuses is a spec that
        would fail at dispatch, in every cell, after the matrix was spent."""
        for spec in grid(keys=list(CHOICES["keys"]),
                         retrieval=list(CHOICES["retrieval"]),
                         readout=list(CHOICES["readout"])):
            overrides, _ = parse(spec)
            if overrides.get("key_active") and overrides.get("context_keys"):
                continue
            config = LocalMemoryConfig(vocab_size=17, d_model=64,
                                       derived_keys=True, **overrides)
            self.assertEqual(config.d_model, 64)


class TheBaselineSpecIsStillASpec(unittest.TestCase):
    """**The empty override dict is the trap, and it caught the sweep.**

    `keys=dense,retrieval=plain,readout=linear` is a perfectly good spec whose
    overrides happen to be `{}`. Code branching on `if overrides:` treats it as
    "no spec given" -- so the ONE combination that is the baseline takes a
    different path from the seventeen that are not.

    That is the worst possible cell to lose: the identity check, whose job is to
    prove the grid reproduces a known number. g11-07's first dispatch lost
    exactly that cell and nothing else.
    """

    def test_the_baseline_spec_has_no_overrides(self):
        self.assertEqual(parse("keys=dense,retrieval=plain,readout=linear")[0],
                         {})

    def test_but_it_still_has_a_label(self):
        """Emptiness of the overrides must not be confused with absence of a
        spec. The label is how a caller tells the two apart."""
        overrides, name = parse("keys=dense,retrieval=plain,readout=linear")
        self.assertFalse(overrides)
        self.assertTrue(name)

    def test_an_empty_spec_and_the_baseline_spec_agree(self):
        self.assertEqual(parse(""), parse("keys=dense,retrieval=plain,readout=linear"))


class AddingAComponentChangesEveryLabel(unittest.TestCase):
    """The cost of a COMPLETE label, stated so it is not discovered later.

    A label naming every component is self-describing -- a table read cold says
    what each row is. It is also unstable: adding `write` to the component set
    turned `keys=dense,retrieval=plain,readout=linear` into that plus
    `,write=plain`, so **records from different component-set versions must not
    be merged by arm name**.

    The alternative -- naming only non-default choices -- is stable across
    additions and unreadable cold, because a row labelled `keys=sparse4` does not
    say what the rest of the model was. Completeness was chosen; this is the
    bill, and the tests below make it visible rather than surprising.
    """

    def test_the_label_names_every_component(self):
        _, name = parse("")
        self.assertEqual(sorted(name.split(",")), sorted(
            f"{c}={v}" for c, v in DEFAULTS.items()))

    def test_a_label_lists_as_many_parts_as_there_are_components(self):
        """If this fails after a component is added, every stored arm name from
        before that change is a different string for the same model."""
        _, name = parse("keys=sparse4")
        self.assertEqual(len(name.split(",")), len(CHOICES))


class TwoModelsCannotShareAName(unittest.TestCase):

    def test_the_label_is_complete_even_when_the_spec_is_not(self):
        """A partial spec must not produce a partial name. `readout=hidden128`
        and `keys=dense,readout=hidden128` are the same model, and if they got
        different labels the same configuration would appear as two arms."""
        self.assertEqual(parse("readout=hidden128")[1],
                         parse("keys=dense,readout=hidden128")[1])

    def test_order_does_not_change_the_label(self):
        self.assertEqual(parse("readout=hidden128,keys=sparse4")[1],
                         parse("keys=sparse4,readout=hidden128")[1])

    def test_different_models_get_different_labels(self):
        seen = {parse(s)[1] for s in grid(keys=["dense", "sparse4"],
                                          readout=["linear", "hidden128"])}
        self.assertEqual(len(seen), 4)


class TyposAreRefused(unittest.TestCase):

    def test_an_unknown_component_is_refused(self):
        """Not defaulted. A typo that silently selects the baseline would
        report the baseline twice and call it a comparison."""
        with self.assertRaises(ValueError):
            parse("readuot=hidden128")

    def test_an_unknown_choice_is_refused(self):
        with self.assertRaises(ValueError):
            parse("keys=sparse5")

    def test_a_malformed_piece_is_refused(self):
        with self.assertRaises(ValueError):
            parse("keys sparse4")

    def test_grid_refuses_unknown_choices_before_dispatch(self):
        """The whole point of costing a grid in code is that it fails in a
        second rather than in every cell of a spent matrix."""
        with self.assertRaises(ValueError):
            grid(keys=["dense", "sparse5"])


class Grids(unittest.TestCase):

    def test_it_is_the_full_product(self):
        self.assertEqual(len(grid(keys=["dense", "sparse4", "pair"],
                                  retrieval=["plain", "cache128"],
                                  readout=["linear", "hidden128"])), 12)

    def test_every_spec_it_produces_parses(self):
        for spec in grid(keys=["dense", "sparse4"], retrieval=["plain"]):
            _, name = parse(spec)
            self.assertIn("readout=", name)


if __name__ == "__main__":
    unittest.main()
