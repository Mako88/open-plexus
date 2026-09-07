"""The gate refuses to invent a threshold, and its yardstick cannot move quietly.

TWO PROPERTIES, and both are about the gate being trustworthy rather than about
it being correct on any particular model.

FIRST: THE HELD-OUT SETS ARE PINNED BY HASH. The doc says "both unchanging for
the life of the branch", and the reason is that a gate whose yardstick moves
cannot tell a regression from a new yardstick. A session that adds three
sentences because they seemed representative would silently invalidate every gate
reading before it, and nothing would look wrong. So the hash is asserted, and
changing it is a decision with a commit message rather than a tidy-up.

SECOND: THERE IS NO DEFAULT THRESHOLD. `Gate` requires both, and `calibrate`
produces them from measured noise. A default would be exactly the invented number
the doc's rule -- "calibrate the thresholds on noise FIRST" -- exists to prevent,
and it would be invisible, because a plausible default produces plausible
verdicts.

All of this runs without a checkpoint. What it cannot check is whether the gate
catches a real regression; that is Phase 3's reading and needs a trained adapter.
"""

from __future__ import annotations

import pytest

from sylvatica.learn.gate import Gate, Measurement
from sylvatica.learn.heldout import GENERAL_QA, PERPLEXITY_TEXT, fingerprint

# PINNED. If this fails, the held-out sets changed and every gate reading taken
# before the change is incomparable to every one taken after. Update it in the
# same commit that changes `heldout.py`, and say in the message why the sets
# moved and which readings are being invalidated.
FINGERPRINT = "68406389111c1b93"


def test_the_held_out_sets_have_not_moved():
    assert fingerprint() == FINGERPRINT, (
        f"the held-out sets changed ({fingerprint()} != {FINGERPRINT}). Every "
        "gate reading before this change is now incomparable to every one after. "
        "If that is intended, update FINGERPRINT in the same commit and say which "
        "readings it invalidates."
    )


def test_the_gate_will_not_invent_a_threshold():
    """Both are required arguments. A default would be the invented number the
    doc's calibrate-on-noise-first rule exists to prevent, and it would be
    invisible: a plausible default produces plausible verdicts."""
    import inspect

    params = inspect.signature(Gate.__init__).parameters
    for name in ("perplexity_threshold", "qa_threshold"):
        assert params[name].default is inspect.Parameter.empty, (
            f"{name} acquired a default; thresholds come from `calibrate`"
        )


def _m(ppl: float, qa: float) -> Measurement:
    return Measurement(perplexity=ppl, qa_score=qa, qa_correct=int(qa * 25),
                       qa_asked=25, tokens=100, seconds=0.1)


def test_a_cycle_within_both_thresholds_passes():
    gate = Gate(_m(20.0, 0.80), perplexity_threshold=1.05, qa_threshold=0.08)
    result = gate.check(_m(20.5, 0.78))
    assert not result.tripped and not result.rolled_back


def test_perplexity_is_compared_as_a_ratio_and_qa_as_a_drop():
    """Because that is what each one is. A perplexity of 21 against 20 means the
    same as 42 against 40; a QA score of 0.60 against 0.64 does not mean the same
    as 0.06 against 0.10."""
    gate = Gate(_m(20.0, 0.80), perplexity_threshold=1.05, qa_threshold=0.08)
    assert gate.check(_m(21.5, 0.80)).tripped, "a 7.5% perplexity rise should trip"
    assert not gate.check(_m(20.9, 0.80)).tripped, "a 4.5% rise should not"

    big = Gate(_m(200.0, 0.80), perplexity_threshold=1.05, qa_threshold=0.08)
    assert big.check(_m(215.0, 0.80)).tripped, "the ratio must not depend on the scale"


def test_a_qa_collapse_trips_even_with_perplexity_unchanged():
    """The two fail differently and that is why there are two. Perplexity can sit
    still while the model stops being able to answer anything."""
    gate = Gate(_m(20.0, 0.80), perplexity_threshold=1.05, qa_threshold=0.08)
    result = gate.check(_m(20.0, 0.60))
    assert result.tripped and result.rolled_back


def test_getting_better_never_trips_it():
    gate = Gate(_m(20.0, 0.80), perplexity_threshold=1.05, qa_threshold=0.08)
    assert not gate.check(_m(18.0, 0.92)).tripped


def test_comparing_across_a_changed_yardstick_raises(monkeypatch):
    """The failure this prevents is a gate that reports a clean pass because the
    held-out set got easier, which reads exactly like a well-behaved cycle."""
    base = _m(20.0, 0.80)
    after = _m(20.1, 0.80)
    object.__setattr__(after, "fingerprint", "somethingelse")
    gate = Gate(base, perplexity_threshold=1.05, qa_threshold=0.08)
    with pytest.raises(ValueError, match="held-out sets changed"):
        gate.check(after)


def test_the_perplexity_text_is_about_nothing_a_house_contains():
    """Phase 3 trains on invented people; the gate must measure language it did
    NOT train on. Overlap here would let a cycle score well on the gate by
    memorising the house, which is the exact failure the gate is for."""
    from sylvatica.exam import world

    lowered = PERPLEXITY_TEXT.lower()
    for words in (world._ROOMS, world._OBJECTS, world._COLOURS):
        for w in words:
            assert w.lower() not in lowered, f"the gate text contains {w!r}"
    for trade in world._TRADES:
        assert trade.split()[1].lower() not in lowered, f"the gate text contains {trade!r}"


def test_the_qa_set_is_answerable_and_unambiguous():
    assert len(GENERAL_QA) >= 20, "too few questions to move less than one at a time"
    for question, answers in GENERAL_QA:
        assert question.endswith("?")
        assert answers and all(a == a.lower() for a in answers), (
            f"{question!r} has answers that are not lowercase; the judge lowercases"
        )
