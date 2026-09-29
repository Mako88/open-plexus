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
                              "count", "negative"}


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
