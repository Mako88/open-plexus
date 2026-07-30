"""A renderer must be incapable of inventing a fact.

ARCHITECTURE row F3's other half, and DECISIONS.md §7. The model's answer is a set of
concepts; a person wants a sentence. The danger is specific and it is not fluency for
its own sake: **a fluent renderer given a wrong answer produces a confident plausible
sentence, and given a right-looking one may produce the correct sentence for the wrong
reason**, because a language model brings its own knowledge. Then an end-to-end score
measures the renderer.

So the load-bearing test in this file is not that the sentences read well. It is
`TheFaithfulnessBar`: every content word out came from the caller. That is the bar a
retrieval realiser and a learned renderer would also have to clear, written here first
where it is trivially true, so the check exists before anything can fail it.
"""

from __future__ import annotations

import unittest

from openplexus.render import FRAME, content_words, render, unfaithful


class TheFaithfulnessBar(unittest.TestCase):
    """The one that matters. Nothing may appear that was not handed in."""

    def test_a_rendered_answer_invents_nothing(self):
        text = render("Rosalind", ["seven", "three"])
        self.assertEqual(unfaithful("Rosalind", ["seven", "three"], text), set())

    def test_the_content_words_are_exactly_the_answer(self):
        # Stated as an equality rather than a subset, so a renderer that silently
        # DROPPED a value would fail too. Under-reporting is as wrong as inventing
        # and looks better, which is why both directions are asserted.
        subject, values = "Rosalind", ["seven", "three", "eleven"]
        text = render(subject, values)
        self.assertEqual(content_words(text) - FRAME,
                         {subject.lower()} | {v.lower() for v in values})

    def test_the_check_itself_catches_an_invented_word(self):
        # RULE 10. A faithfulness check nobody has seen fail is not a check. This
        # feeds it a sentence with a word the caller never supplied.
        smuggled = "Rosalind holds these recorded values: seven and probably three."
        self.assertEqual(
            unfaithful("Rosalind", ["seven", "three"], smuggled), {"probably"})

    def test_and_catches_a_plausible_completion(self):
        # The realistic failure, not a nonsense word: a renderer that "helpfully"
        # names the family the values belong to. Reads perfectly and is invented.
        smuggled = "Rosalind, of the Arden household, holds seven and three."
        self.assertIn("arden", unfaithful("Rosalind", ["seven", "three"], smuggled))

    def test_frame_is_small_enough_to_read(self):
        # The frame is the exhaustive list of what the renderer contributes. If it
        # grows into a vocabulary, "invents nothing" stops meaning anything, because
        # the allowed set would cover most sentences.
        self.assertLess(len(FRAME), 40)


class ItRendersTheShapesTheTaskProduces(unittest.TestCase):

    def test_two_values_read_as_a_set(self):
        # `families.py`'s actual answer shape: a family's value AND its exception,
        # which is the conjunction no single token could express.
        text = render("Rosalind", ["seven", "three"], relation="value")
        self.assertIn("seven", text)
        self.assertIn("three", text)
        self.assertTrue(text.endswith("."))

    def test_one_value_is_not_phrased_as_a_list(self):
        self.assertIn("one recorded value", render("Rosalind", ["seven"]))

    def test_order_is_preserved_rather_than_sorted(self):
        # Sorting would impose a ranking the model did not produce. `answer_set`
        # returns a frozenset precisely because order carries no meaning, so the
        # renderer must not invent one either.
        text = render("Rosalind", ["three", "seven"])
        self.assertLess(text.index("three"), text.index("seven"))

    def test_a_repeat_is_not_a_second_answer(self):
        self.assertEqual(render("Rosalind", ["seven", "seven"]),
                         render("Rosalind", ["seven"]))

    def test_an_empty_set_declines_rather_than_leaving_a_hole(self):
        # ARCHITECTURE row C4. Nothing in this project lets the model say it does
        # not know; this is the surface for it if something ever does, and it is the
        # one branch that names no value.
        text = render("Rosalind", [])
        self.assertIn("no recorded", text)
        self.assertEqual(unfaithful("Rosalind", [], text), set())

    def test_the_relation_is_used_when_the_task_names_one(self):
        self.assertIn("colour", render("Rosalind", ["red"], relation="colour"))


class ItRefusesWhatWouldReadAsAFact(unittest.TestCase):

    def test_an_empty_subject_is_refused(self):
        with self.assertRaises(ValueError):
            render("", ["seven"])

    def test_an_empty_surface_is_refused(self):
        # It would render as a gap in the sentence, which reads as a fact the model
        # did not have rather than as one it could not name.
        with self.assertRaises(ValueError):
            render("Rosalind", ["seven", ""])


class TheRulerTakesNoDependencies(unittest.TestCase):

    def test_render_imports_neither_numpy_nor_the_model(self):
        # A renderer that needed the model to run could not be used to check the
        # model. Parsed rather than grepped -- the mistake `tests/test_answers.py`
        # made was matching the module's own prose.
        import ast
        import pathlib
        tree = ast.parse((pathlib.Path(__file__).resolve().parents[1]
                          / "openplexus" / "render.py")
                         .read_text(encoding="utf-8"))
        imported = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                imported.update(a.name.split(".")[0] for a in node.names)
            elif isinstance(node, ast.ImportFrom) and node.module:
                imported.add(node.module)
        self.assertNotIn("numpy", imported)
        self.assertFalse({m for m in imported if "models" in m})
        self.assertIn("collections.abc", imported)


if __name__ == "__main__":
    unittest.main()
