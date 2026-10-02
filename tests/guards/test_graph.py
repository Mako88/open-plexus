"""The graphed arm, on the small parser so the guards stay fast."""

from unfused.exam.world import Question
from unfused.graph import GraphArm, extract, nlp

MODEL = "en_core_web_sm"


def q(text, answer=None):
    return Question(text, answer, "k", "direct", 0, 0)


def arm(tmp_path, **kw):
    return GraphArm(tmp_path, model=MODEL, cache=None, **kw)


def test_a_particle_before_a_place_is_not_an_argument():
    events = extract(nlp(MODEL)("Mary went back to the bathroom."))
    go = next(e for e in events if e["lemma"] == "go")
    assert ["prep:to", "n:bathroom"] in go["edges"]
    assert not any(t == "n:back" for _, t in go["edges"])


def test_a_lesson_is_a_path_followed_for_another_name(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.hear(1, "Bren keeps the rope in the attic.")
    a.teach("Where does Ada keep the kettle?", "shed")
    assert a.answer(q("Where does Bren keep the rope?")) == "attic"


def test_the_latest_event_wins(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.teach("Where does Ada keep the kettle?", "shed")
    a.hear(1, "Ada keeps the kettle in the attic.")
    assert a.answer(q("Where does Ada keep the kettle?")) == "attic"


def test_a_word_held_every_time_is_frame_not_a_slot(tmp_path):
    a = arm(tmp_path)
    for i, (who, other) in enumerate([("Ada", "Bren"), ("Cael", "Dov"), ("Eli", "Fen")]):
        a.hear(i, f"{who} is a cousin of {other}.")
        a.teach(f"Whose cousin is {who}?", other)
    shape, fillers = a.shape("Whose cousin is Ada?")
    assert shape == "Whose cousin is <0>?" and fillers == ["ada"]


def test_nothing_learnt_is_nothing_said(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    assert a.answer(q("Where does Ada keep the kettle?")) == "I don't know."


def test_what_did_not_happen_or_only_might_is_an_event_of_its_own():
    lemmas = {e["lemma"]: e["mood"] for t in ("Ada doesn't keep the kettle in the attic.",
                                              "Ada might move the kettle to the attic.")
              for e in extract(nlp(MODEL)(t))}
    assert lemmas.get("not keep") == "not" and lemmas.get("might move") == "might"


def test_a_denial_or_a_hedge_never_makes_an_answer_the_latest(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.teach("Where does Ada keep the kettle?", "shed")
    a.hear(1, "Ada doesn't keep the kettle in the attic.")
    a.hear(2, "Ada might keep the kettle in the cellar.")
    assert a.answer(q("Where does Ada keep the kettle?")) == "shed"


def test_a_wording_no_lesson_used_borrows_the_nearest_taught_shape(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.hear(1, "Bren keeps the rope in the attic.")
    a.teach("Where does Ada keep the kettle?", "shed")
    assert a.answer(q("In which room does Bren keep the rope?")) == "attic"
