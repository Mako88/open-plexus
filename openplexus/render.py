"""Turning a set answer into words, without being able to invent one.

## IN PLAIN TERMS

The model's answer is a set of concepts. A person wants a sentence. This turns the
first into the second.

**The whole design constraint is that it must be incapable of adding anything.** A
fluent renderer is dangerous here in a specific way: given a wrong answer it will
produce a confident, plausible sentence, and given a right-looking one it may produce
the correct sentence for the wrong reason — because a language model brings its own
knowledge of the world. Then an end-to-end score measures the renderer, not the
model. So this is templates: it can only arrange words it was handed.

## Why templates first, and what comes after

John's ruling: templates first, then a retrieval realiser that emits the surfaces each
visited concept already carries, and a small learned renderer only after a traversal
number exists. An off-the-shelf language model as renderer is refused — see
DECISIONS.md §7.

**This is the cheap end of that ladder and it is deliberately stupid.** If a template
sentence reads sensibly, the concept set genuinely carried the answer, and that is an
honest demonstration rather than a fluent one.

## The property that makes it safe, stated so it can be tested

Every content word in the output comes from the caller's own surfaces. The only other
words are `FRAME`, a fixed and short list. `content_words()` exists so a test can
assert exactly that, and `tests/test_render.py` does:

    content_words(render(...)) - FRAME == {subject} | set(values)

That is the faithfulness check a learned renderer would also have to pass, written
here first where it is trivially true — so the bar exists before anything can fail it.

## Dependency-free

Part of the ruler, not the model: no numpy, and nothing here imports from
`openplexus.models`. A renderer that needed the model to run could not be used to
check the model.
"""

from __future__ import annotations

from collections.abc import Iterable, Sequence

#: Every word this module can emit that did not come from the caller. Short and
#: fixed on purpose: it is the exhaustive list of what the renderer contributes, so
#: a reader can see the whole of what it is allowed to say.
FRAME = frozenset({
    "and", "are", "but", "does", "hold", "holds", "i", "know", "no", "not",
    "nothing", "of", "one", "only", "or", "recorded", "about", "also", "is",
    "the", "there", "these", "value", "values", "which",
})


def content_words(text: str) -> set[str]:
    """Every distinct word in `text`, lowercased, punctuation stripped.

    The contract: what a faithfulness test compares against. It deliberately does
    not know which words are framing and which are content — that separation is the
    caller's, by subtracting `FRAME`, so this cannot quietly excuse a word by
    reclassifying it.
    """
    cleaned = "".join(c.lower() if c.isalnum() or c.isspace() else " "
                      for c in text)
    return {word for word in cleaned.split() if word}


def render(subject: str, values: Sequence[str],
           relation: str | None = None) -> str:
    """One sentence for a set-valued answer.

    Args:
        subject: What was asked about, as a surface string.
        values: The answer set's surfaces, in any order. **Order is not meaningful**
            and is preserved rather than sorted, because sorting would impose a
            ranking the model did not produce.
        relation: What the values are, if the task names it. `None` gives the
            generic phrasing.

    Returns:
        A sentence containing `subject`, the `values`, and words from `FRAME`.

    An EMPTY `values` renders as declining to answer rather than as a sentence with
    a hole in it. That is the honest surface for the gate finding nothing, and it is
    the one case where a renderer can say something the set does not contain — so it
    says the least possible.
    """
    if not subject:
        raise ValueError("a sentence about nothing has no subject to render")
    kept: list[str] = []
    for value in values:
        if not value:
            raise ValueError(
                "an empty surface would render as a gap in the sentence, which "
                "reads as a fact the model did not have rather than as one it "
                "could not name")
        if value not in kept:
            # A SET, so a repeat is not a second answer. `answer_set` already
            # returns a frozenset; this holds the property for any caller.
            kept.append(value)
    noun = relation or "value"
    if not kept:
        # DECLINING, and the only branch that does not name a value. ARCHITECTURE
        # row C4: nothing in this project lets the model say it does not know, and
        # this is the surface for it if something ever does.
        return f"I know of no recorded {noun} about {subject}."
    if len(kept) == 1:
        return f"{subject} holds one recorded {noun}: {kept[0]}."
    joined = ", ".join(kept[:-1]) + f" and {kept[-1]}"
    return (f"{subject} holds these recorded {noun}s: {joined}.")


def unfaithful(subject: str, values: Iterable[str], text: str) -> set[str]:
    """Words in `text` that came from neither the caller nor `FRAME`.

    The contract: empty means faithful. Returned as the offending set rather than a
    boolean, because a faithfulness failure is only actionable if you can see which
    word was invented.

    **This is the check that matters for the renderers that come later.** A retrieval
    realiser or a learned model has to pass exactly this, and having it here means
    the bar is written before anything can fail it.
    """
    allowed = FRAME | content_words(subject)
    for value in values:
        allowed |= content_words(value)
    return content_words(text) - allowed
