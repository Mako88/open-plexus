"""The instrument, checked before it is trusted.

AN EXAM IS THE ONE THING IN THIS REPOSITORY THAT CANNOT BE WRONG QUIETLY.
Everything else fails loudly or fails in a way a reading exposes; a broken
scorer produces a number that looks exactly like a real number and gets
committed. The two earlier branches were beaten by a blind rule for a whole
session because nothing was checking the instrument.

All of this runs on `StubCore` in milliseconds. What it cannot check is whether
a real core's answers are judged sensibly, which is why `judge` is deliberately
dumb enough to be checked by hand.
"""

from __future__ import annotations

import pytest

from sylvatica.exam import (
    BlindBaseline,
    Tier,
    generate_house,
    is_refusal,
    judge,
    questions_at,
    run_blind,
    run_exam,
)

from .stub import StubCore


@pytest.fixture
def house():
    return generate_house(seed=7, n_facts=25, n_turns=120, delays=(1, 5, 20), negatives=6)


def test_a_seed_gives_the_same_house_twice():
    """A reading names a seed. If the seed does not pin the house, it names nothing."""
    a = generate_house(seed=3, n_facts=20, n_turns=100, delays=(1, 5, 20))
    b = generate_house(seed=3, n_facts=20, n_turns=100, delays=(1, 5, 20))
    assert [f.told for f in a.facts] == [f.told for f in b.facts]
    assert a.turns == b.turns
    assert a.told_at == b.told_at
    assert generate_house(seed=4, n_facts=20, n_turns=100, delays=(1, 5, 20)).turns != a.turns


def test_every_fact_survives_to_the_longest_delay(house):
    """THE LONG-DELAY COLUMN MUST NOT BECOME A SAMPLE OF THE EARLY FACTS.

    A fact told near the end cannot be asked 150 turns later, and dropping those
    questions silently would make the interesting column -- the one that says
    how fast the state forgets -- a measurement of a different, earlier subset of
    the house. The generator refuses to build such a house at all.
    """
    longest = max(house.delays)
    for fact in house.facts:
        assert house.told_at[fact.id] + longest < len(house.turns)

    asked_per_delay = {d: 0 for d in house.delays}
    for q in house.questions:
        if q.fact_id is not None:
            asked_per_delay[q.delay_turns] += 1
    assert len(set(asked_per_delay.values())) == 1, (
        f"the delays are not asked equally often: {asked_per_delay}"
    )


def test_a_house_that_cannot_ask_every_delay_refuses_to_exist():
    with pytest.raises(ValueError, match="cannot all be told before"):
        generate_house(seed=0, n_facts=50, n_turns=40, delays=(1, 5, 150))


def test_a_negative_does_not_announce_itself_in_its_kind(house):
    """A baseline that can see which questions are negatives gets a free pass.

    The blind rule would refuse them all and score a perfect zero inventions by
    reading a label the core never gets. Negatives are place-shaped questions
    labelled `place`; what makes one a negative is `fact_id is None`, which
    lives on the scorer's side of the line.
    """
    negatives = [q for q in house.questions if q.fact_id is None]
    assert negatives
    for q in negatives:
        assert q.kind in {f.kind for f in house.facts}
        assert q.answer is None


def test_the_entropy_report_says_how_strong_blind_is(house):
    """STANDING OBJECTION 5. A blind score without this is uninterpretable."""
    entropy = house.answer_entropy()
    assert set(entropy) == {f.kind for f in house.facts}

    # Five kinds over 25 facts is five answers a kind; distinct answers give
    # log2(5) bits, and that is the number that says blind should score ~1/5.
    for kind, bits in entropy.items():
        assert 0.0 <= bits <= 3.0, (kind, bits)


def test_blind_invents_on_every_negative(house):
    """Blind cannot refuse, and the reading must show what that costs it.

    An arm that beats blind on score while matching its invention rate has not
    beaten it -- confabulating fluently and remembering look identical on the
    positive questions alone.
    """
    result = run_blind(house)
    negatives = [a for a in result.answers if a.fact_id is None]
    assert negatives
    assert all(a.invented for a in negatives)


def test_blind_scores_about_one_over_the_answers_per_kind(house):
    result = run_blind(house).score()
    assert 0.05 < result.score < 0.45, (
        f"blind scored {result.score} -- outside the range a five-answers-a-kind "
        "house implies, so either the generator or the blind rule has drifted"
    )


def _q(answer: str | None, text: str = "Where does Henound keep the floats?"):
    from sylvatica.exam import Question

    return Question(
        fact_id=None if answer is None else "f000",
        text=text,
        answer=answer,
        kind="place",
        delay_turns=1,
    )


def test_the_judge_is_contains_match_and_refusal_detection():
    assert judge(_q("pantry"), "I think it's in the pantry somewhere.") == (True, False, False)
    assert judge(_q("pantry"), "The cellar, probably.") == (False, False, False)
    # Case and surrounding words do not matter; the doc chose a weak scorer, and
    # weak identically for every arm is the property that matters.
    assert judge(_q("Verdigris"), "verdigris")[0]

    # On a negative there is no right answer; what is scored is whether it made
    # one up.
    assert judge(_q(None), "I don't know, you never told me.") == (False, False, False)
    assert judge(_q(None), "In the scullery.") == (False, True, False)


def test_an_echo_is_not_counted_as_an_invented_answer():
    """TWO DIFFERENT DEFECTS, and lumping them overstated one by a factor of two.

    A core that answers "Where does Thonreal keep the brass keys?" has not
    invented a location -- it has lost track of who is speaking, which is what
    the missing turn separator used to cause. A core that answers "in the
    cellar" about a room nobody named has invented one. The first is a harness
    smell; the second is the confabulation the negatives exist to measure.
    """
    question = _q(None, "Where does Thonreal keep the brass keys?")
    correct, invented, echoed = judge(question, "Where does Thonreal keep the brass keys?")
    assert (correct, invented, echoed) == (False, False, True)

    correct, invented, echoed = judge(question, "In the cellar, I think.")
    assert (correct, invented, echoed) == (False, True, False)

    # A leading acknowledgement does not stop it being an echo.
    assert judge(question, "I see. Where does Thonreal keep the brass keys?")[2]


def test_refusals_are_recognised_without_being_generous():
    assert is_refusal("I'm not sure about that one.")
    assert is_refusal("I do not know.")
    assert not is_refusal("It is in the dairy.")
    assert not is_refusal("Nobody knows the answer better than the pantry.") is False


def test_no_tier_can_be_run_as_another_one_in_disguise(house):
    """A tier reading that was secretly another tier is the most expensive wrong
    number this branch could produce: it would look like the phase working.

    Tier C has no adapter and raises. Tier B WITHOUT A STORE raises too, and
    that case is the one worth guarding -- it is the one that would otherwise
    run happily, because a Tier B with no store is just Tier A with extra steps
    and would score like it.
    """
    core = StubCore()

    with pytest.raises(NotImplementedError, match="Phase 3"):
        run_exam(core, house, Tier.C)

    with pytest.raises(ValueError, match="no store was given"):
        run_exam(core, house, Tier.B)


def test_tier_c_refuses_to_run_with_the_store_on(tmp_path, house):
    """THE MOST FLATTERING WRONG NUMBER AVAILABLE TO THIS BRANCH.

    Tier C measures what got into the WEIGHTS -- that is the whole of complaint
    4's bar and the first north star's added line. Run with retrieval on, it
    would be Tier B wearing a different label, and Tier B already scores 0.97.
    A Tier C reading of 0.97 would look like the bet paying off.
    """
    from sylvatica.store import HashEmbedder, SqliteStore

    core = StubCore()
    store = SqliteStore(tmp_path / "c.db", embedder=HashEmbedder(dims=64))
    with pytest.raises(ValueError, match="store DISABLED"):
        run_exam(core, house, Tier.C, store=store, adapter=object())
    store.close()


def test_tier_b_answers_from_the_store_and_not_from_the_conversation(tmp_path, house):
    """The claim Tier B makes: everything it knows arrived through retrieval.

    `StubCore`'s state counts tokens fed, so an answering state built from a
    fresh prime plus injected hits is far smaller than one that carried three
    hundred turns. If Tier B were quietly reusing the conversation state, this
    would be the size of Tier A's.
    """
    from sylvatica.store import HashEmbedder, SqliteStore

    core = StubCore()
    store = SqliteStore(tmp_path / "b.db", embedder=HashEmbedder(dims=64))
    result = run_exam(
        core, house, Tier.B, reply_budget=2, answer_budget=2,
        state_path=tmp_path / "b.pt", progress=False, store=store, k=4,
    )
    assert result.answers
    assert len(result.retrieved) == len(result.answers)

    # Every turn was written, in and out.
    assert store.tiers()["total"] >= len(house.turns)

    # And the retrieval rows say whether the answering fragment was in front of
    # the core -- without which a bad Tier B is unattributable.
    positives = [r for r in result.retrieved if r["wanted_fragment"]]
    assert positives and all(isinstance(r["hit"], bool) for r in positives)
    store.close()


def test_a_question_does_not_enter_the_conversation(tmp_path, house):
    """THE FORK IS THE DESIGN DECISION IN `run_exam` AND THIS IS IT ASSERTED.

    Asking a fact at five delays means asking it five times. If the asking went
    into the state, delay 20 would be measuring the delay since the last ASKING,
    and a wrong answer would be re-remembered as though somebody had said it.

    `StubCore`'s state counts tokens fed, so the count after an exam must equal
    the conversation's tokens alone. If questions leaked in, it would be higher.
    """
    core = StubCore()
    result = run_exam(
        core, house, Tier.A, reply_budget=2, answer_budget=2,
        state_path=tmp_path / "s.pt", progress=False,
    )
    assert result.answers

    from sylvatica.loop.turn import PREAMBLE, SEPARATOR, format_prompt

    # The separator that CLOSES each turn is fed too, and it has to be counted
    # here. It is not bookkeeping: leaving it out of the state is what made the
    # core start repeating questions back instead of answering them. See
    # `loop.turn.say`.
    closing = len(core.encode(SEPARATOR))

    # THE STATE ON DISK IS THE ONE FROM THE LAST TURN THAT HAD QUESTIONS, not
    # from the end of the conversation -- `run_exam` writes it inside the asking
    # branch. Comparing against the whole conversation would fail for a reason
    # that has nothing to do with leaking, which is how a good test gets deleted.
    last_asked = max(questions_at(house))
    expected = len(core.encode(PREAMBLE)) + sum(
        len(core.encode(format_prompt(line))) + 2 + closing
        for line in house.turns[: last_asked + 1]
    )
    final = core.load_state(tmp_path / "s.pt")
    assert final[0] == expected, (
        f"the state absorbed {final[0]} tokens and the conversation up to turn "
        f"{last_asked} is {expected}; a question leaked into the thread"
    )


def test_the_schedule_asks_each_question_after_its_delay(house):
    at = questions_at(house)
    for turn, questions in at.items():
        for q in questions:
            if q.fact_id is not None:
                assert house.told_at[q.fact_id] + q.delay_turns == turn

    scheduled = sum(len(v) for v in at.values())
    assert scheduled == len(house.questions), (
        f"{len(house.questions) - scheduled} questions are never asked"
    )


def test_blind_is_reported_per_kind_not_only_in_total(house):
    score = run_blind(house).score()
    assert set(score.by_kind) == {f.kind for f in house.facts}
    assert set(score.by_delay) == set(house.delays)


def test_the_blind_table_is_the_modal_answer(house):
    blind = BlindBaseline(house)
    for kind, answer in blind.table.items():
        of_kind = [f.answer for f in house.facts if f.kind == kind]
        assert of_kind.count(answer) == max(of_kind.count(a) for a in set(of_kind))


def test_the_full_context_baseline_runs_end_to_end(house):
    """CHECKED ON THE STUB BECAUSE ON THE REAL CORE IT IS 2.7 HOURS.

    The full-context baseline runs LAST in an exam, after the arm. A crash in it
    costs the whole run -- the arm's numbers included -- and it would be found
    at the end of the longest job this branch runs. So the code path is walked
    here in milliseconds: every question answered, every answer scored, the cost
    meter accumulating, and the same preamble the arm gets.
    """
    from sylvatica.exam import run_full_context

    core = StubCore()
    result = run_full_context(core, house, answer_budget=2, progress=False)

    assert len(result.answers) == len(house.questions)
    assert result.cost is not None and result.cost.tokens_in > 0
    score = result.score()
    assert score.asked == len([q for q in house.questions if q.fact_id is not None])


def test_the_full_context_baseline_gets_the_same_preamble_as_the_arm(house):
    """Or the comparison is between two instructions rather than two memories."""
    from sylvatica.exam.baselines import FullContextBaseline
    from sylvatica.loop.turn import PREAMBLE

    core = StubCore()
    baseline = FullContextBaseline(core, budget=2)

    # EVERY call, not the last one: the stub's `generate` also calls `encode`,
    # for the reply it invents. Keeping only the most recent would assert on the
    # string "ok" and pass for the wrong reason.
    seen: list[str] = []
    original = core.encode

    def watching(text: str):
        seen.append(text)
        return original(text)

    core.encode = watching  # type: ignore[method-assign]
    baseline.answer_at(["a line", "another line"], house.questions[0])

    prompt = seen[0]
    assert prompt.startswith(PREAMBLE)
    assert "a line" in prompt and "another line" in prompt


def test_the_full_context_estimate_grows_with_the_house():
    """The number standing objection 8 is about. It must respond to the house."""
    from sylvatica.exam import estimate_full_context_tokens

    core = StubCore()
    small = generate_house(seed=1, n_facts=10, n_turns=60, delays=(1, 5, 20), negatives=2)
    big = generate_house(seed=1, n_facts=30, n_turns=200, delays=(1, 5, 20), negatives=2)
    assert estimate_full_context_tokens(core, big) > estimate_full_context_tokens(core, small)


def test_spread_reports_the_range_a_gap_must_beat():
    """The arithmetic a Phase 1 verdict rests on.

    Phase 1's refutation is "Tier A at delay 20 below blind". Whether a gap of
    0.06 counts depends on how far a Tier A score moves between houses, and that
    comparison is made against the RANGE rather than the deviation -- a standard
    deviation over five samples is itself noisy enough to mislead.
    """
    from sylvatica.exam import spread

    s = spread([0.30, 0.34, 0.28, 0.31, 0.33])
    assert s["min"] == 0.28 and s["max"] == 0.34
    assert s["range"] == pytest.approx(0.06, abs=1e-9)
    assert s["mean"] == pytest.approx(0.312, abs=1e-9)
    assert s["stdev"] > 0

    # One sample has no spread, and must not raise -- a single-seed run is a
    # legitimate thing to ask for, it just cannot support a threshold.
    one = spread([0.5])
    assert one["range"] == 0.0 and one["stdev"] == 0.0


def test_an_oblique_question_shares_no_content_words_with_its_telling():
    """THE PROPERTY THAT MAKES RETRIEVAL HARD, asserted rather than eyeballed.

    Phase 2's first reading found Tier B scoring 0.988 on the LEXICAL ranker
    alone with precision 1.000 -- better than the hybrid. The embeddings were
    contributing nothing, because "How many lanterns are in the cellar?" shares
    every content word with "There are 65 lanterns in the cellar" and FTS5 alone
    finds it. The store was passing a keyword lookup while being credited with
    retrieval.

    An oblique question keeps the ENTITY -- a person's name, without which the
    question is unanswerable rather than harder -- and must share nothing else.
    This is what says so when a synonym map goes stale as the word lists grow,
    which is the way this quietly stops working.
    """
    import re

    STOP = {
        "the", "a", "an", "is", "are", "in", "of", "to", "does", "do", "what",
        "where", "how", "many", "whose", "which", "for", "and", "at", "his",
        "her", "part", "house", "holds", "sit", "sits", "earn", "wage", "shade",
        "colour", "related", "whom", "living", "keep", "keeps", "there", "that",
    }

    def content(text: str) -> set[str]:
        return {
            w for w in re.findall(r"[a-z]+", text.lower())
            if w not in STOP and len(w) > 2
        }

    house = generate_house(
        seed=0, n_facts=40, n_turns=250, delays=(1, 5, 20), negatives=0,
        phrasing="oblique",
    )

    offenders = []
    for fact in house.facts:
        assert fact.oblique, f"{fact.kind} has no oblique phrasing"
        # Proper nouns are the entity and are allowed through; they are the key.
        names = {w.lower() for w in re.findall(r"[A-Z][a-z]+", fact.told)}
        shared = (content(fact.oblique) & content(fact.told)) - names
        if shared:
            offenders.append((fact.told, fact.oblique, sorted(shared)))

    assert not offenders, (
        "oblique questions still share content words with their telling, so a "
        "lexical index can shortcut them:\n"
        + "\n".join(f"  {t}\n  -> {o}   shared={sh}" for t, o, sh in offenders[:6])
    )
