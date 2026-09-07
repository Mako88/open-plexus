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


def test_the_gate_measures_the_model_and_not_the_sampler():
    """GREEDY, and this is a fault that was caught by its own calibration.

    Sampled at temperature 1.0, the QA score over 25 questions carries about 0.10
    of standard deviation from the sampler alone. The first calibration made it
    visible: three repeats spread 0.60 to 0.64, and the very next measurement of
    the SAME untouched model came in at 0.480 -- outside its own calibrated
    range. A threshold from that spread sits below one standard deviation of its
    own measurement, so the gate would have rolled back cycles at random and
    every rollback would have been recorded as a finding about consolidation.

    Greedy makes it deterministic: 0.88, 0.88, 0.88 on repeated runs.
    """
    import inspect

    from sylvatica.learn import gate as module

    source = inspect.getsource(module.general_qa)
    assert "greedy=True" in source, (
        "the gate's QA measurement is sampled again; its own noise will exceed "
        "any threshold derived from it"
    )


def test_greedy_is_off_by_default_everywhere_else():
    """It is right for a yardstick and wrong for a conversation: argmax text is
    flat and repetitive. A default of greedy would quietly change what the REPL
    and every exam sound like."""
    from sylvatica.core.base import Sampling

    assert Sampling().greedy is False


def test_the_replay_anchor_is_never_the_gates_own_yardstick():
    """THE MOST FLATTERING CORRUPTION AVAILABLE TO A GATE.

    The general slice anchors the adapter to language the house did not produce.
    If it were the gate's held-out text, perplexity would IMPROVE on the very
    text used to decide whether the model had got worse -- so a cycle that
    damaged the model everywhere else would sail through, and every gate reading
    would be meaningless in the direction nobody checks.
    """
    import pytest

    from sylvatica.learn.general import check_disjoint
    from sylvatica.learn.heldout import PERPLEXITY_TEXT

    check_disjoint(["Entirely unrelated prose about entirely unrelated things."])

    with pytest.raises(ValueError, match="overlaps the gate's held-out text"):
        check_disjoint([PERPLEXITY_TEXT[:600]])


def test_a_cycle_cannot_silently_train_on_house_text_alone():
    """The anchor is a named ingredient of the doc's replay mix, and the first
    Phase 3 run showed what its absence does: one cycle of ~700 tokens of house
    text moved held-out perplexity from 22.94 to 27.56. The gate rolled it back
    correctly, and the reading would have blamed consolidation for a mix that was
    missing a third of itself. `run_cycle` raises rather than proceeding."""
    import inspect

    from sylvatica.learn import consolidate

    source = inspect.getsource(consolidate.run_cycle)
    assert "FileNotFoundError" in source and "general" in source, (
        "run_cycle no longer insists on the general anchor"
    )


def test_the_replay_mix_is_by_tokens_and_not_by_list_length():
    """A DISTINCTION THAT COST A RUN.

    `ReplaySpec` names three shares -- recent 0.4, rehearsal 0.4, general 0.2 --
    and the first two are counted in FRAGMENTS while a general chunk is a
    hundred-odd words. Thirty-two chunks beside forty-eight fragments looks
    balanced and is 87% general by token. The adapter would have spent every
    cycle on English it already knew, barely seen the house, and Tier C landing
    at blind would have read as the doc's named refutation of the `raw` arm
    rather than as a mixing error.
    """
    import tempfile
    from pathlib import Path

    from sylvatica.learn.consolidate import build_training_set
    from sylvatica.store import HashEmbedder, ReplaySpec, SqliteStore

    class Counter:
        """Just enough core to count tokens: words, roughly."""

        def encode(self, text: str) -> list[int]:
            return [0] * max(1, len(text.split()))

    with tempfile.TemporaryDirectory() as d:
        store = SqliteStore(Path(d) / "s.db", embedder=HashEmbedder(dims=32))
        for i in range(60):
            store.write_turn(f"Fact number {i} about a room and a thing.", f"t#turn:{i}")

        spec = ReplaySpec(n=40, recent=0.5, rehearsal=0.3, general=0.2, seed=1)
        training = build_training_set(store, spec, core=Counter())

        counter = Counter()
        house = sum(len(counter.encode(t)) for t in training.texts[: -training.general])
        anchor = sum(len(counter.encode(t)) for t in training.texts[-training.general :])
        share = anchor / (house + anchor)
        assert 0.08 < share < 0.45, (
            f"the general slice is {share:.0%} of the training tokens; the spec "
            f"asked for {spec.general:.0%}"
        )
        store.close()


def test_the_qa_threshold_permits_exactly_the_drop_it_names():
    """A THRESHOLD THAT IS A COUNT IN DISGUISE, and floats do not respect that.

    The QA floor of 0.04 means "one question of twenty-five may go". But
    0.88 - 0.84 is 0.040000000000000036 in floats, which is greater than 0.04 --
    so exactly the drop the threshold was chosen to permit tripped the gate, on
    every cycle of a run where nothing else had gone wrong. The trips looked like
    a finding about consolidation and were a rounding error.
    """
    gate = Gate(_m(20.0, 0.88), perplexity_threshold=1.02, qa_threshold=0.04)
    assert not gate.check(_m(20.0, 0.84)).tripped, (
        "a one-question drop tripped a threshold that is one question wide"
    )
    assert gate.check(_m(20.0, 0.80)).tripped, "a two-question drop must still trip"


def test_the_perplexity_threshold_permits_exactly_its_ratio():
    gate = Gate(_m(20.0, 0.88), perplexity_threshold=1.02, qa_threshold=0.04)
    assert not gate.check(_m(20.4, 0.88)).tripped, "exactly 2% must not trip"
    assert gate.check(_m(20.5, 0.88)).tripped
