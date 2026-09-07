"""The declaratives arm, and the one property that makes templates safe.

WHY THIS ARM EXISTS AT ALL, from the raw arm's measured failure: overfitting
twenty episodes for twenty epochs got 4 of 20, and the misses were WELL-FORMED
SENTENCES OF THE HOUSE WITH THE WRONG FILLER -- "There are 18 flour sacks in the
pantry" when the answer was 13. Raw episodes teach the shape of the sentence and
not the binding inside it. If a fact is only ever seen as one sentence, then the
sentence and the fact are the same object and the sentence is the easier one to
learn.

SO THE ARM'S JOB IS TO VARY THE SURFACE AND HOLD THE BINDING FIXED, and the risk
it introduces is obvious: a paraphrase that changes the binding is a confident
false fact, trained in with the same weight as a true one. That is worse than a
raw episode, and it is why the doc's refutation for this arm is an ERROR RATE
read by hand rather than a score.

TEMPLATES CANNOT DO THAT AND THE CORE CAN. A template only rearranges words that
are already in the sentence, so it cannot invent a room or a number -- and
`test_a_template_never_introduces_a_content_word` is what keeps that true as
templates are added. Core paraphrases are more varied and are not safe, so every
declarative records which produced it and the hand-read can tell the two failures
apart.
"""

from __future__ import annotations

import re

from sylvatica.learn.extract import Declarative, templates, usable

FACTS = [
    "Marta keeps the glass jars in the pantry.",
    "There are 65 lanterns in the cellar.",
    "The cracked plates are slate.",
    "Osric is Marta's cousin.",
    "Henound carves spoons for a living.",
]

# Words a rearrangement may add without adding a claim.
FUNCTION_WORDS = {
    "the", "a", "an", "is", "are", "in", "of", "to", "and", "that", "it", "by",
    "those", "who", "one", "holds", "colour", "cousins", "trade", "at", "for",
    "his", "her", "s",
}


def content(text: str) -> set[str]:
    return {
        w for w in re.findall(r"[a-z]+", text.lower())
        if w not in FUNCTION_WORDS and len(w) > 1
    }


def test_a_template_never_introduces_a_content_word():
    """THE PROPERTY THAT MAKES TEMPLATES THE SAFE HALF OF THIS ARM.

    A paraphrase that adds a room, a number or a name is a confident false
    binding, trained in with the same weight as a true one -- worse than the raw
    episode it replaced. A rearrangement cannot do that BY CONSTRUCTION, and this
    is what keeps that true as templates are added by someone who has forgotten
    why it mattered.
    """
    for fact in FACTS:
        source = content(fact)
        for variant in templates(fact):
            added = content(variant) - source
            assert not added, (
                f"template turned\n  {fact}\ninto\n  {variant}\nadding {sorted(added)}. "
                "A template may only rearrange; anything it adds is a claim "
                "nobody made."
            )


def test_every_fact_kind_the_generator_makes_has_a_template():
    """A kind with no template gets only core paraphrases, which are the unsafe
    half -- so it would quietly become the arm's weakest point without anything
    saying so."""
    for fact in FACTS:
        assert templates(fact), f"no template matched {fact!r}"


def test_a_template_keeps_the_answer_in_the_sentence():
    """Varying the surface is the point; losing the binding is the failure. A
    paraphrase the exam could no longer score correct has removed the fact."""
    answers = ["pantry", "65", "slate", "cousin", "spoons"]
    for fact, answer in zip(FACTS, answers):
        for variant in templates(fact):
            assert answer.lower() in variant.lower(), (
                f"{variant!r} dropped {answer!r} out of {fact!r}"
            )


def test_scaffolding_is_not_mistaken_for_a_fact():
    """A small core asked to list facts also emits "Here are the facts:" and
    numbered blanks. Training on those teaches the model to produce list
    furniture, which is the raw arm's disease in a new costume."""
    for junk in [
        "Here are the facts:", "The following are stated:", "Okay.", "1.",
        "ok", "Sure, here you go:", "In summary:", "- ",
    ]:
        assert not usable(junk), f"{junk!r} was accepted as a fact"

    for real in FACTS:
        assert usable(real), f"{real!r} was rejected"


def test_a_declarative_records_what_made_it():
    """The doc's refutation for this arm is an error rate read BY HAND. A hand
    reader who cannot tell a template from a core paraphrase cannot attribute the
    errors, and the two need opposite fixes."""
    d = Declarative(text="x", source="template", parent="y")
    assert d.row()["source"] == "template"
    assert d.row()["parent"] == "y"
