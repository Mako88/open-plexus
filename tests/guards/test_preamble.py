"""The preamble is the core's only framing, and the first one taught it to echo.

WHAT HAPPENED, because these tests are meaningless without it. The first
preamble's Assistant turn was a paraphrase of its own User turn: "Remember
them... answer in a few words... say that you do not know", answered by "I will
remember what you tell me, answer briefly, and say when I do not know". That is
the ONLY example of an Assistant turn a fresh state contains, and a recurrent
model learns in context. It learned that an Assistant turn restates the User
turn, and started answering questions by repeating them back.

It cost five Phase 1 readings and was read as forgetting for most of a session,
because it is diluted by real replies as the conversation goes on -- so the
score IMPROVES with delay, which nobody expects and nobody therefore checks.

TWO PROPERTIES ARE CHECKED HERE, and both are invisible to a human reviewer who
is reading the preamble for whether it says the right thing:

  1. No Assistant turn restates its User turn.
  2. Nothing in the preamble can be scored as a correct answer by the exam.

Neither is about what the preamble MEANS, which is why neither was noticed.
"""

from __future__ import annotations

import re

import pytest

from sylvatica.exam import world
from sylvatica.loop.turn import CORE_PREFIX, PREAMBLE, PREAMBLES, USER_PREFIX

# Words that carry no content and would inflate any overlap measure.
_STOP = {
    "i", "a", "an", "the", "and", "or", "if", "is", "are", "am", "will", "you",
    "me", "my", "it", "that", "them", "to", "in", "on", "of", "not", "do", "so",
    "have", "has", "when", "what", "your", "be", "at", "for", "with", "about",
}


def _turns(preamble: str) -> list[tuple[str, str]]:
    """The preamble as (user, assistant) pairs."""
    parts = re.split(rf"{re.escape(USER_PREFIX)}|{re.escape(CORE_PREFIX)}", preamble)
    parts = [p.strip() for p in parts if p.strip()]
    return list(zip(parts[0::2], parts[1::2]))


def _content(text: str) -> set[str]:
    return {w for w in re.findall(r"[a-z']+", text.lower()) if w not in _STOP and len(w) > 2}


def overlap(user: str, assistant: str) -> float:
    """Share of the assistant's content words that came from the user's turn."""
    a = _content(assistant)
    if not a:
        return 0.0
    return len(a & _content(user)) / len(a)


# How much of an Assistant turn may be borrowed from the User turn it answers.
# The version that taught echoing scores far above this; the replacement scores
# at or near zero.
MAX_OVERLAP = 0.34


def test_the_shipped_preamble_does_not_demonstrate_echoing():
    """RULE 1. The core's only example of a reply must not be a restatement.

    This is the whole bug, as an assertion. A reviewer reads a preamble for
    whether it asks for the right behaviour; nobody reads it for whether its
    example accidentally demonstrates the wrong one.
    """
    worst = [(u, a, overlap(u, a)) for u, a in _turns(PREAMBLE)]
    assert worst, "the preamble has no User/Assistant pairs to check"
    for user, assistant, share in worst:
        assert share <= MAX_OVERLAP, (
            f"{share:.0%} of {assistant!r} is borrowed from {user!r}. That is a "
            "one-shot demonstration of repeating the user back, and the core "
            "will do exactly that."
        )


def test_the_guard_catches_the_preamble_that_caused_this():
    """A check that cannot fire reads as a pass. This is the check firing.

    `v1-restating` is kept in the codebase precisely so this test has something
    real to catch, and so nobody reinvents it from scratch.
    """
    shares = [overlap(u, a) for u, a in _turns(PREAMBLES["v1-restating"])]
    assert max(shares) > MAX_OVERLAP, (
        "the preamble known to have taught echoing now passes the guard; the "
        "overlap measure has drifted and is no longer catching anything"
    )


def test_nothing_in_the_preamble_can_be_scored_as_a_right_answer():
    """RULE 2. The example's vocabulary must not collide with the generator's.

    The preamble demonstrates the wanted behaviour with a kettle on a shelf. If
    a word in it were also a possible answer -- a room, a colour, a trade -- the
    core could produce it out of the example rather than out of memory and the
    exam would score it correct. The generator's word lists WILL change, and
    this constraint is invisible when they do.
    """
    answerable = set()
    for words in (world._ROOMS, world._OBJECTS, world._COLOURS):
        answerable |= {w.lower() for w in words}
    # Trades are scored on their object: "repairs clocks" -> "clocks".
    answerable |= {t.split()[1].lower() for t in world._TRADES}

    said = _content(PREAMBLE)
    collisions = sorted(w for w in answerable if w in said)
    assert not collisions, (
        f"the preamble contains {collisions}, which the exam can score as a "
        "correct answer. Pick example words outside the generator's lists."
    )


def test_the_preamble_shows_how_to_decline():
    """The exam's negatives are only fair if declining was demonstrated.

    Scoring a core for inventing an answer to a question nobody told it, without
    ever showing it what declining looks like, measures the harness's manners
    rather than the core's honesty.
    """
    assistant_turns = " ".join(a for _, a in _turns(PREAMBLE)).lower()
    assert "not told" in assistant_turns or "not know" in assistant_turns


def test_every_named_preamble_is_a_well_formed_dialogue():
    for name, text in PREAMBLES.items():
        if not text:
            continue
        assert text.endswith("\n\n"), f"{name} does not close its last turn"
        assert text.count(USER_PREFIX) == text.count(CORE_PREFIX), (
            f"{name} has unbalanced User and Assistant turns"
        )


@pytest.mark.parametrize("name", ["none", "v1-restating", "v2-example", "v3-identity"])
def test_the_named_preambles_are_all_present(name):
    """A reading names its preamble. If the name stops resolving, so does the reading."""
    assert name in PREAMBLES


# `v1-restating` is the one preamble allowed to fail the echo check: it is kept
# deliberately as the negative control, and a test above asserts the guard still
# fires on it.
KNOWN_BAD = {"v1-restating"}


@pytest.mark.parametrize("name", sorted(set(PREAMBLES) - KNOWN_BAD - {"none"}))
def test_no_candidate_preamble_demonstrates_echoing(name):
    """EVERY arm, not just the one that ships.

    A preamble variant is written to be swept, which means it goes on the card
    and produces a reading. If it teaches echoing the way `v1-restating` did,
    that reading measures the framing bug rather than the thing the arm is
    about -- and the arm would be reported as having failed on its own merits.
    """
    for user, assistant in _turns(PREAMBLES[name]):
        share = overlap(user, assistant)
        assert share <= MAX_OVERLAP, (
            f"{name}: {share:.0%} of {assistant!r} is borrowed from {user!r}"
        )


@pytest.mark.parametrize("name", sorted(set(PREAMBLES) - {"none"}))
def test_no_preamble_leaks_an_answer_the_exam_can_score(name):
    """Applies to every variant, because a leak is scored as memory.

    A word that is both in a preamble and in the generator's answer sets can be
    produced from the example rather than from the state, and the exam has no
    way to tell the difference.
    """
    answerable = set()
    for words in (world._ROOMS, world._OBJECTS, world._COLOURS):
        answerable |= {w.lower() for w in words}
    answerable |= {t.split()[1].lower() for t in world._TRADES}

    collisions = sorted(w for w in answerable if w in _content(PREAMBLES[name]))
    assert not collisions, f"{name} contains scoreable answers: {collisions}"
