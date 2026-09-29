"""The system's reasoning, checked with an ear that reads from a table.

No model runs here: each sentence's assertions and each question's query are
written out by hand, so a failure is the matcher's and never an ear's.
"""

from unfused.exam.world import Question
from unfused.store import HashEmbedder
from unfused.system import SystemArm

READINGS = {
    "Vessarine repairs clocks for a living.":
        [{"subject": "Vessarine", "relation": "repairs", "object": "clocks"}],
    "Vessarine keeps the lanterns in the scullery.":
        [{"subject": "Vessarine", "relation": "keeps", "object": "lanterns",
          "place": "scullery"}],
    "The lanterns are ochre.":
        [{"subject": "lanterns", "relation": "has colour", "object": "ochre"}],
    "Tolmick keeps the jars in the scullery.":
        [{"subject": "Tolmick", "relation": "keeps", "object": "jars", "place": "scullery"}],
    "Vessarine took the lanterns to the attic.":
        [{"subject": "Vessarine", "relation": "took", "object": "lanterns", "place": "attic"}],
    "There are 35 jars in the cellar.":
        [{"subject": "jars", "relation": "number", "quantity": "35", "place": "cellar"}],
    "There are 12 jars in the attic.":
        [{"subject": "jars", "relation": "number", "quantity": "12", "place": "attic"}],
}

QUERIES = {
    "chain": {"steps": [
        {"subject": "?p", "relation": "repairs", "object": "clocks"},
        {"subject": "?p", "relation": "keeps", "object": "?t", "place": "scullery"},
        {"subject": "?t", "relation": "has colour", "object": "?c"}],
        "answer": "?c", "count": False},
    "where now": {"steps": [
        {"subject": "Vessarine", "relation": "keeps", "object": "lanterns", "place": "?r"}],
        "answer": "?r", "count": False},
    "how many": {"steps": [
        {"subject": "?p", "relation": "keeps", "object": "?t", "place": "scullery"}],
        "answer": "?p", "count": True},
    "stranger": {"steps": [
        {"subject": "Brael", "relation": "repairs", "object": "?x"}],
        "answer": "?x", "count": False},
    "jars in the cellar": {"steps": [
        {"subject": "jars", "relation": "number", "quantity": "?n", "place": "cellar"}],
        "answer": "?n", "count": False},
}


class TableEar:
    """Reads a sentence from READINGS and a question from QUERIES."""

    name = "table"

    def read(self, sentence, relations=None, before=None):
        return READINGS.get(sentence, [])

    def rewrite(self, question):
        return QUERIES[question]

    def choose(self, prompt, options):
        return None

    def synonymous(self, asked, stored, example=""):
        return {asked, stored} == {"keeps", "put in"}


def arm(tmp_path, upto: int):
    a = SystemArm(tmp_path, TableEar(), HashEmbedder())
    for turn, sentence in enumerate(list(READINGS)[:upto]):
        a.hear(turn, sentence)
    return a


def q(text):
    return Question(text, None, "x", "x", 0, 0)


def test_a_chain_of_three_is_followed(tmp_path):
    assert arm(tmp_path, 4).answer(q("chain")) == "ochre"


def test_a_move_replaces_where_a_thing_is_kept(tmp_path):
    a = arm(tmp_path, 5)
    assert a.answer(q("where now")) == "attic"


def test_a_count_follows_the_moves(tmp_path):
    assert arm(tmp_path, 4).answer(q("how many")) == "2"
    assert arm(tmp_path / "later", 5).answer(q("how many")) == "1"


def test_nothing_matching_is_i_dont_know(tmp_path):
    assert arm(tmp_path, 7).answer(q("stranger")) == "I don't know."


def test_two_counts_of_one_thing_in_two_rooms_both_stand(tmp_path):
    assert arm(tmp_path, 7).answer(q("jars in the cellar")) == "35"


def test_a_move_by_the_owner_supersedes_a_telling_with_the_thing_as_subject(tmp_path):
    a = SystemArm(tmp_path, TableEar(), HashEmbedder())
    READINGS["The cards are Ada's, in the attic."] = [
        {"subject": "cards", "relation": "belong to", "object": "Ada", "place": "attic"}]
    READINGS["Ada took the cards to the cellar."] = [
        {"subject": "Ada", "relation": "took to", "object": "cards", "place": "cellar"}]
    QUERIES["where are the cards"] = {"steps": [
        {"subject": "Ada", "relation": "keeps", "object": "cards", "place": "?r"}],
        "answer": "?r", "count": False}
    a.hear(0, "The cards are Ada's, in the attic.")
    a.hear(1, "Ada took the cards to the cellar.")
    assert a.answer(q("where are the cards")) == "cellar"


def test_a_relation_worded_differently_is_learnt_once_and_remembered(tmp_path):
    ear = TableEar()
    asked = []
    judge = ear.synonymous
    ear.synonymous = lambda a, b, e="": asked.append((a, b)) or judge(a, b, e)
    a = SystemArm(tmp_path, ear, HashEmbedder())
    READINGS["Ada put the kettle in the shed."] = [
        {"subject": "Ada", "relation": "put in", "object": "kettle", "place": "shed"}]
    QUERIES["who keeps things in the shed"] = {"steps": [
        {"subject": "?p", "relation": "keeps", "object": "?t", "place": "shed"}],
        "answer": "?p", "count": True}
    a.hear(0, "Ada put the kettle in the shed.")
    assert a.answer(q("who keeps things in the shed")) == "1"
    assert a.answer(q("who keeps things in the shed")) == "1"
    assert asked.count(("keeps", "put in")) == 1


def test_a_plan_is_learnt_once_per_shape_and_reused_without_the_ear(tmp_path):
    ear = TableEar()
    rewrites = []
    ear.rewrite = lambda question: rewrites.append(question) or {"steps": [
        {"subject": "Vessarine", "relation": "keeps", "object": "lanterns", "place": "?r"}],
        "answer": "?r", "count": False}
    a = SystemArm(tmp_path, ear, HashEmbedder(), plans=True)
    for turn, sentence in enumerate(list(READINGS)[:4]):
        a.hear(turn, sentence)
    assert a.answer(q("Where does Vessarine keep the lanterns?")) == "scullery"
    assert a.answer(q("Where does Tolmick keep the jars?")) == "scullery"
    assert rewrites == ["Where does Vessarine keep the lanterns?"]
    assert a.shape("Where does Tolmick keep the jars?") == (
        "Where does <0> keep the <1>?", ["Tolmick", "jars"])


def test_a_plan_that_found_nothing_is_not_kept(tmp_path):
    ear = TableEar()
    rewrites = []
    ear.rewrite = lambda question: rewrites.append(question) or {"steps": [
        {"subject": "Brael", "relation": "keeps", "object": "lanterns", "place": "?r"}],
        "answer": "?r", "count": False}
    a = SystemArm(tmp_path, ear, HashEmbedder(), plans=True)
    for turn, sentence in enumerate(list(READINGS)[:4]):
        a.hear(turn, sentence)
    assert a.answer(q("Where does Brael keep the lanterns?")) == "I don't know."
    a.answer(q("Where does Orrin keep the lanterns?"))
    assert len(rewrites) == 2


def test_a_plan_learnt_elsewhere_is_used_without_the_ear(tmp_path):
    ear = TableEar()
    ear.rewrite = lambda question: (_ for _ in ()).throw(AssertionError(question))
    known = {"Where does <0> keep the <1>?": {"steps": [
        {"subject": "<0>", "relation": "keeps", "object": "<1>", "place": "?r"}],
        "answer": "?r", "count": False}}
    a = SystemArm(tmp_path, ear, HashEmbedder(), plans=True, known_plans=known)
    for turn, sentence in enumerate(list(READINGS)[:4]):
        a.hear(turn, sentence)
    assert a.answer(q("Where does Tolmick keep the jars?")) == "scullery"


def test_a_plan_that_answers_with_the_question_own_name_is_not_kept(tmp_path):
    ear = TableEar()
    rewrites = []
    ear.rewrite = lambda question: rewrites.append(question) or {"steps": [
        {"subject": "?a", "relation": "keeps", "object": "lanterns", "place": "scullery"}],
        "answer": "?a", "count": False}
    a = SystemArm(tmp_path, ear, HashEmbedder(), plans=True)
    for turn, sentence in enumerate(list(READINGS)[:4]):
        a.hear(turn, sentence)
    a.answer(q("Whose cousin is Vessarine?"))
    a.answer(q("Whose cousin is Tolmick?"))
    assert len(rewrites) == 2


def test_the_planner_can_be_another_faculty(tmp_path):
    ear, planner = TableEar(), TableEar()
    ear.rewrite = lambda question: (_ for _ in ()).throw(AssertionError(question))
    planner.rewrite = lambda question: {"steps": [
        {"subject": "Vessarine", "relation": "keeps", "object": "lanterns", "place": "?r"}],
        "answer": "?r", "count": False}
    a = SystemArm(tmp_path, ear, HashEmbedder(), plans=True, planner=planner)
    for turn, sentence in enumerate(list(READINGS)[:4]):
        a.hear(turn, sentence)
    assert a.answer(q("Where does Vessarine keep the lanterns?")) == "scullery"


def test_a_plan_that_drops_a_named_filler_is_not_kept(tmp_path):
    ear = TableEar()
    rewrites = []
    ear.rewrite = lambda question: rewrites.append(question) or {"steps": [
        {"subject": "?a", "relation": "keeps", "object": "?b", "place": "scullery"}],
        "answer": "?a", "count": False}
    a = SystemArm(tmp_path, ear, HashEmbedder(), plans=True)
    for turn, sentence in enumerate(list(READINGS)[:4]):
        a.hear(turn, sentence)
    a.answer(q("Who keeps the lanterns in the scullery?"))
    a.answer(q("Who keeps the jars in the scullery?"))
    assert len(rewrites) == 2


def test_a_filler_written_as_part_of_a_longer_one_becomes_its_slot(tmp_path):
    ear = TableEar()
    ear.rewrite = lambda question: {"steps": [
        {"subject": "?a", "relation": "keeps", "object": "lanterns", "place": "scullery"}],
        "answer": "?a", "count": False}
    a = SystemArm(tmp_path, ear, HashEmbedder(), plans=True)
    READINGS["Orrin keeps the rope in the scullery."] = [
        {"subject": "Orrin", "relation": "keeps", "object": "rope", "place": "in the scullery"}]
    for turn, sentence in enumerate(list(READINGS)[:4] + ["Orrin keeps the rope in the scullery."]):
        a.hear(turn, sentence)
    a.answer(q("Who keeps the lanterns in the scullery?"))
    kept = a.db.execute("SELECT plan FROM plans").fetchone()[0]
    assert "scullery" not in kept


def searched(tmp_path, upto: int):
    a = SystemArm(tmp_path, TableEar(), HashEmbedder(), searched=True)
    for turn, sentence in enumerate(list(READINGS)[:upto]):
        a.hear(turn, sentence)
    return a


def test_search_finds_a_chain_of_three_from_its_anchors_and_goal(tmp_path):
    a = searched(tmp_path, 4)
    assert a.answer(q("chain")) == "ochre"
    # the same anchors and goal with the middle step missing: search needs no path
    QUERIES["chain, unwritten"] = {"steps": [
        {"subject": "?p", "relation": "repairs", "object": "clocks"},
        {"subject": "?t", "relation": "has colour", "object": "?c"}],
        "answer": "?c", "count": False}
    assert a.answer(q("chain, unwritten")) == "ochre"


def test_search_counts_and_refuses(tmp_path):
    assert searched(tmp_path, 4).answer(q("how many")) == "2"
    assert searched(tmp_path / "s", 7).answer(q("stranger")) == "I don't know."


def test_a_question_read_as_one_fact_is_answered_by_search_with_no_planner(tmp_path):
    ear = TableEar()
    ear.rewrite = lambda question: (_ for _ in ()).throw(AssertionError(question))
    ear.ask = lambda question: {"asked": "object", "assertion": {
        "subject": "things", "relation": "has colour", "object": "", "place": None,
        "quantity": None}, "count": False}
    a = SystemArm(tmp_path, ear, HashEmbedder(), asked=True)
    for turn, sentence in enumerate(list(READINGS)[:4]):
        a.hear(turn, sentence)
    assert a.answer(q("What colour are the things the person who repairs clocks keeps "
                      "in the scullery?")) == "ochre"
    assert a.answer(q("What colour are the things Brael keeps in the scullery?")) == (
        "I don't know.")


def test_two_wordings_of_one_owner_and_thing_become_one_relation(tmp_path):
    ear = TableEar()
    ear.synonymous = lambda a, b, e="": False
    a = SystemArm(tmp_path, ear, HashEmbedder())
    READINGS["Orrin put the rope in the attic for safekeeping."] = [
        {"subject": "Orrin", "relation": "put for safekeeping", "object": "rope",
         "place": "attic"}]
    READINGS["Orrin keeps the rope in the attic."] = [
        {"subject": "Orrin", "relation": "keeps", "object": "rope", "place": "attic"}]
    READINGS["Ada put the kettle in the attic for safekeeping."] = [
        {"subject": "Ada", "relation": "put for safekeeping", "object": "kettle",
         "place": "attic"}]
    QUERIES["who keeps things in the attic"] = {"steps": [
        {"subject": "?p", "relation": "keeps", "object": "?t", "place": "attic"}],
        "answer": "?p", "count": True}
    for turn, s in enumerate(["Orrin put the rope in the attic for safekeeping.",
                              "Orrin keeps the rope in the attic.",
                              "Ada put the kettle in the attic for safekeeping."]):
        a.hear(turn, s)
    assert a.answer(q("who keeps things in the attic")) == "2"


def test_search_ends_on_the_asked_kind_past_a_junk_anchor(tmp_path):
    ear = TableEar()
    ear.ask = lambda question: {"asked": "object", "kind": "colour", "assertion": {
        "subject": "things", "relation": "keeps", "object": "", "place": None,
        "quantity": None}, "count": False}
    ear.is_a = lambda filler, kind, example="": filler in ("ochre",)
    a = SystemArm(tmp_path, ear, HashEmbedder(), asked=True)
    READINGS["Nothing is the same now."] = [
        {"subject": "nothing", "relation": "is", "object": "same", "place": "now"}]
    for turn, sentence in enumerate(list(READINGS)[:4] + ["Nothing is the same now."]):
        a.hear(turn, sentence)
    # 'now' was heard as a name, so it is an anchor here, and no chain touches it
    assert a.answer(q("What colour are the things the person who repairs clocks keeps "
                      "in the scullery now?")) == "ochre"
    assert a.answer(q("What colour are the things Brael keeps in the scullery?")) == (
        "I don't know.")


def test_a_verdict_about_one_wording_answers_for_one_with_the_same_properties(tmp_path):
    ear = TableEar()
    judged = []
    ear.synonymous = lambda a, b, e="": judged.append(b) or b == "carves"
    a = SystemArm(tmp_path, ear, HashEmbedder(), shares=0.8)
    READINGS["Ada carves spoons for a living."] = [
        {"subject": "Ada", "relation": "carves", "object": "spoons"}]
    READINGS["Orrin binds books for a living."] = [
        {"subject": "Orrin", "relation": "binds", "object": "books"}]
    for turn, s in enumerate(["Ada carves spoons for a living.",
                              "Orrin binds books for a living."]):
        a.hear(turn, s)
    assert a.means("works as", "carves") and a.means("works as", "binds")
    assert judged == ["carves"]


def test_a_subject_heard_somewhere_new_is_no_longer_where_it_was(tmp_path):
    a = SystemArm(tmp_path, TableEar(), HashEmbedder(), moves=True)
    READINGS["John went to the kitchen."] = [
        {"subject": "John", "relation": "went to", "place": "kitchen"}]
    READINGS["John moved to the garden."] = [
        {"subject": "John", "relation": "moved to", "place": "garden"}]
    QUERIES["where is John"] = {"steps": [
        {"subject": "John", "relation": "went to", "place": "?p"}],
        "answer": "?p", "count": False}
    a.hear(0, "John went to the kitchen.")
    a.hear(1, "John moved to the garden.")
    assert [r["place"] for r in a.rows() if r["subject"] == "john"] == ["garden"]
    # two counts in two rooms are two facts, never a move
    assert arm(tmp_path / "counts", 7).answer(q("jars in the cellar")) == "35"


def test_the_ear_is_shown_the_sentences_before(tmp_path):
    seen = []
    ear = TableEar()
    ear.read = lambda s, relations=None, before=None: seen.append(before) or []
    a = SystemArm(tmp_path, ear, HashEmbedder(), context=2)
    for turn, s in enumerate(["one.", "two.", "three."]):
        a.hear(turn, s)
    assert seen == [[], ["one."], ["one.", "two."]]
