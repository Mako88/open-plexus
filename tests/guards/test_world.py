from collections import Counter

from unfused.exam.world import generate_house


def test_a_seed_makes_the_same_house():
    a, b = generate_house(3), generate_house(3)
    assert a.turns == b.turns
    assert a.questions == b.questions


def test_every_house_has_every_form():
    for seed in range(5):
        forms = Counter(q.form for q in generate_house(seed).questions)
        assert set(forms) == {"direct", "oblique", "reverse", "twohop", "update", "chain3",
                              "count", "denied", "hedged", "negative"}


def test_a_denial_or_hedge_is_told_before_it_is_asked_and_names_another_room():
    for seed in range(5):
        house = generate_house(seed)
        for q in (q for q in house.questions if q.form in ("denied", "hedged")):
            assert q.stale and q.stale != q.answer
            fact = next(f for f in house.facts if f.id == q.needs[0])
            told = [t for t, text in enumerate(house.turns)
                    if q.stale in text and fact.subject in text and t != house.told_at[fact.id]]
            assert told and min(told) < q.asked_at


def test_no_question_is_asked_before_what_it_needs_was_told():
    house = generate_house(1)
    for q in house.questions:
        for fid in q.needs:
            assert q.asked_at > house.told_at[fid]


def test_an_update_is_told_before_it_is_asked_and_changes_the_answer():
    house = generate_house(2)
    for q in (q for q in house.questions if q.form == "update"):
        assert q.stale and q.stale != q.answer
        told = [t for t, text in enumerate(house.turns)
                if q.answer in text and ("moved" in text or "old spot" in text)]
        assert told and min(told) < q.asked_at


def test_negatives_name_nobody_in_the_house():
    house = generate_house(4)
    told = " ".join(house.turns)
    names = {w.strip("?.,'s") for q in house.questions if q.form == "negative"
             for w in q.text.split() if w[:1].isupper() and w not in ("Where", "What", "Whose")}
    assert names and not any(n in told for n in names)


def test_blind_table_is_built_from_the_answers():
    house = generate_house(0)
    table = house.modal_answers()
    assert set(table) == {q.kind for q in house.questions if q.answer is not None}


def test_every_telling_states_its_fact():
    """A telling phrased as a question asserts nothing, and no arm can be
    expected to answer from it. One did, for a whole phase."""
    for seed in range(5):
        for fact in generate_house(seed).facts:
            assert not fact.told.rstrip().endswith("?"), fact.told


def test_a_count_is_asked_after_every_move_that_changes_it():
    house = generate_house(0)
    moves = [t for t, text in enumerate(house.turns) if "moved" in text or "old spot" in text]
    for q in (q for q in house.questions if q.form == "count"):
        room = q.text.rsplit("the ", 1)[1].rstrip("?")
        for t in moves:
            text = house.turns[t]
            if room in text:
                assert q.asked_at > t


def test_the_second_house_asks_every_form_and_only_after_what_it_needs():
    from unfused.exam.second import generate_second_house

    assert generate_second_house(3).questions == generate_second_house(3).questions
    for seed in range(5):
        house = generate_second_house(seed)
        assert {q.form for q in house.questions} == {
            "direct", "oblique", "reverse", "twohop", "update", "chain3", "count", "negative"}
        for q in house.questions:
            assert all(q.asked_at > house.told_at[fid] for fid in q.needs)
        assert not any(f.told.rstrip().endswith("?") for f in house.facts)


def test_the_second_house_shares_no_relation_with_the_first():
    """Its tellings and questions use none of the first house's relation words, so
    a plan copied from the first house's practice binds nothing here."""
    from unfused.exam.second import generate_second_house
    from unfused.exam.world import _FILLER

    first = ("keep", "cousin", "for a living", "colour", "counted", " room")
    house = generate_second_house(1)
    for text in house.turns + [q.text for q in house.questions]:
        if text in _FILLER:
            continue  # small talk is the same in every house
        assert not any(w in text for w in first), text


def test_a_second_house_count_is_asked_after_every_passing_on_that_changes_it():
    from unfused.exam.second import generate_second_house

    house = generate_second_house(0)
    passes = [t for t, text in enumerate(house.turns)
              if "passed the" in text or "any more" in text]
    for q in (q for q in house.questions if q.form == "count"):
        who = q.text.split(" does ")[1].split(" have")[0]
        assert all(q.asked_at > t for t in passes if who in house.turns[t])
