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

    def read(self, sentence, relations=None):
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


BABI = {
    "Mary got the milk.": [{"subject": "Mary", "relation": "got", "object": "milk"}],
    "Mary moved to the hallway.": [
        {"subject": "Mary", "relation": "moved to", "place": "hallway"}],
    "John got the football.": [{"subject": "John", "relation": "got", "object": "football"}],
    "Sandra went to the garden.": [
        {"subject": "Sandra", "relation": "went to", "place": "garden"}],
    "John travelled to the kitchen.": [
        {"subject": "John", "relation": "travelled to", "place": "kitchen"}],
}


def test_a_plan_taught_on_one_story_answers_another_with_other_words(tmp_path):
    READINGS.update(BABI)
    teacher = SystemArm(tmp_path / "taught", TableEar(), HashEmbedder(), taught=True)
    for turn, s in enumerate(["Mary got the milk.", "Mary moved to the hallway."]):
        teacher.hear(turn, s)
    teacher.teach("Where is the milk?", "hallway")
    learnt = teacher.db.execute("SELECT shape, plan, hits, misses FROM learnt").fetchall()
    assert learnt and learnt[0][0] == "Where is the <0>?"

    ear = TableEar()
    ear.rewrite = lambda question: (_ for _ in ()).throw(AssertionError(question))
    pupil = SystemArm(tmp_path / "pupil", ear, HashEmbedder(), taught=True,
                      known_learnt=learnt)
    for turn, s in enumerate(["John got the football.", "Sandra went to the garden.",
                              "John travelled to the kitchen."]):
        pupil.hear(turn, s)
    assert pupil.answer(q("Where is the football?")) == "kitchen"


def test_a_plan_that_binds_nothing_is_not_charged_a_miss(tmp_path):
    READINGS.update(BABI)
    teacher = SystemArm(tmp_path, TableEar(), HashEmbedder(), taught=True)
    for turn, s in enumerate(["Mary got the milk.", "Mary moved to the hallway."]):
        teacher.hear(turn, s)
    teacher.teach("Where is the milk?", "hallway")
    # John was never heard anywhere, so the plan binds nothing and says nothing
    teacher.hear(2, "John got the football.")
    teacher.teach("Where is the football?", "kitchen")
    assert teacher.db.execute("SELECT hits, misses FROM learnt").fetchall() == [(1, 0)]


def test_a_plan_taught_through_history_keeps_before_as_an_order_in_time(tmp_path):
    READINGS.update(BABI)
    READINGS.update({
        "Mary moved to the kitchen.": [
            {"subject": "Mary", "relation": "moved to", "place": "kitchen"}],
        "John went to the hallway.": [
            {"subject": "John", "relation": "went to", "place": "hallway"}],
        "John went to the garden.": [
            {"subject": "John", "relation": "went to", "place": "garden"}],
        "John went to the office.": [
            {"subject": "John", "relation": "went to", "place": "office"}],
    })
    teacher = SystemArm(tmp_path / "taught", TableEar(), HashEmbedder(), taught=True,
                        moves=True)
    for turn, s in enumerate(["Mary got the milk.", "Mary moved to the hallway.",
                              "Mary moved to the kitchen."]):
        teacher.hear(turn, s)
    teacher.teach("Where was the milk before the kitchen?", "hallway")
    learnt = teacher.db.execute("SELECT shape, plan, hits, misses FROM learnt").fetchall()
    assert learnt and all('"history": true' in plan for _, plan, _, _ in learnt)

    ear = TableEar()
    ear.rewrite = lambda question: (_ for _ in ()).throw(AssertionError(question))
    pupil = SystemArm(tmp_path / "pupil", ear, HashEmbedder(), taught=True, moves=True,
                      known_learnt=learnt)
    for turn, s in enumerate(["John got the football.", "John went to the hallway.",
                              "John went to the garden.", "John travelled to the kitchen.",
                              "John went to the office."]):
        pupil.hear(turn, s)
    # the garden is the place nearest before the kitchen; the hallway is earlier and
    # the office is after
    assert pupil.answer(q("Where was the football before the kitchen?")) == "garden"


def test_a_taught_count_learns_which_relations_hold_and_counts_after_a_drop(tmp_path):
    said = {
        "Mary took the apple.": ("Mary", "took", "apple"),
        "Mary grabbed the milk.": ("Mary", "grabbed", "milk"),
        "Mary dropped the apple.": ("Mary", "dropped", "apple"),
        "Mary dropped the milk.": ("Mary", "dropped", "milk"),
        "John took the ball.": ("John", "took", "ball"),
        "John grabbed the cup.": ("John", "grabbed", "cup"),
        "John dropped the ball.": ("John", "dropped", "ball"),
    }
    READINGS.update({k: [{"subject": a, "relation": r, "object": o}]
                     for k, (a, r, o) in said.items()})
    teacher = SystemArm(tmp_path / "taught", TableEar(), HashEmbedder(), taught=True)
    for turn, s in enumerate(["Mary took the apple.", "Mary grabbed the milk."]):
        teacher.hear(turn, s)
    teacher.teach("How many objects is Mary carrying?", "two")
    for turn, s in enumerate(["Mary dropped the apple.", "Mary dropped the milk."], 2):
        teacher.hear(turn, s)
    teacher.teach("How many objects is Mary carrying?", "none")
    learnt = teacher.db.execute("SELECT shape, plan, hits, misses FROM learnt").fetchall()
    holds = teacher.db.execute("SELECT relation, yes, no FROM holds").fetchall()

    pupil = SystemArm(tmp_path / "pupil", TableEar(), HashEmbedder(), taught=True,
                      known_learnt=learnt, known_holds=holds)
    for turn, s in enumerate(["John took the ball.", "John grabbed the cup.",
                              "John dropped the ball."]):
        pupil.hear(turn, s)
    assert pupil.answer(q("How many objects is John carrying?")) == "one"


def test_a_count_of_people_counts_names_and_not_things_the_ear_made_subjects(tmp_path):
    said = {
        "Hosior put the lanterns in the attic.": ("Hosior", "put", "lanterns", "attic"),
        "Candle moulds are in the attic.": ("candle moulds", "are", None, "attic"),
        "There are 97 in the attic.": ("97", "in", None, "attic"),
        "Daleth put the candle moulds in the attic.": ("Daleth", "put", "candle moulds",
                                                       "attic"),
        "Rakrim put the jars in the dairy.": ("Rakrim", "put", "jars", "dairy"),
        "Seed trays are in the dairy.": ("seed trays", "are", None, "dairy"),
        "Hosior put the seed trays in the dairy.": ("Hosior", "put", "seed trays", "dairy"),
    }
    READINGS.update({k: [{"subject": a, "relation": r, "object": o, "place": p}]
                     for k, (a, r, o, p) in said.items()})
    teacher = SystemArm(tmp_path / "taught", TableEar(), HashEmbedder(), taught=True)
    for turn, s in enumerate(list(said)[:4]):
        teacher.hear(turn, s)
    teacher.teach("How many people keep things in the attic?", "2")
    # told again, the plan says 'two' where '2' is taught, and that is a hit
    teacher.teach("How many people keep things in the attic?", "2")
    learnt = teacher.db.execute("SELECT shape, plan, hits, misses FROM learnt").fetchall()
    assert [p for _, p, h, m in learnt if '"named": true' in p and (h, m) == (2, 0)]

    pupil = SystemArm(tmp_path / "pupil", TableEar(), HashEmbedder(), taught=True,
                      known_learnt=[(s, p, h, m) for s, p, h, m in learnt
                                    if '"named": true' in p and '"holding": false' in p])
    for turn, s in enumerate(list(said)[4:]):
        pupil.hear(turn, s)
    assert pupil.answer(q("How many people keep things in the dairy?")) == "two"


def test_an_assertion_is_held_to_the_words_of_its_sentence():
    clean = SystemArm.clean
    got = clean({"subject": "john", "object": "milk", "place": "house", "quantity": "1"},
                "John left the milk.")
    assert got == {"subject": "john", "object": "milk", "place": None, "quantity": None}
    got = clean({"subject": "john", "object": "garden", "place": "to the garden",
                 "quantity": None}, "John travelled to the garden.")
    assert (got["object"], got["place"]) == (None, "garden")
    got = clean({"subject": "jars", "object": None, "place": "cellar", "quantity": "35"},
                "There are 35 jars in the cellar.")
    assert got["quantity"] == "35"


def test_a_step_naming_a_quantity_does_not_match_an_assertion_without_one(tmp_path):
    READINGS.update(BABI)
    a = SystemArm(tmp_path, TableEar(), HashEmbedder(), taught=True)
    a.hear(0, "Mary got the milk.")
    plan = {"steps": [{"filled": ["object", "subject"], "subject": "<0>", "quantity": "<1>",
                       "object": "?ans"}]}
    assert a.follow(plan, ["Mary", "3"]) == []


COUSINS = {
    "Ivo is Bren's cousin.": [{"subject": "Ivo", "relation": "is cousin of", "place": "Bren"}],
    "Tam is Oda's cousin.": [{"subject": "Tam", "relation": "is cousin of", "object": "Oda"}],
}


def test_a_loose_step_meets_a_filler_the_ear_put_in_another_slot(tmp_path):
    READINGS.update(COUSINS)
    teacher = SystemArm(tmp_path / "taught", TableEar(), HashEmbedder(), taught=True)
    teacher.hear(0, "Ivo is Bren's cousin.")
    teacher.teach("Who is Ivo's cousin?", "Bren")
    learnt = teacher.db.execute("SELECT shape, plan, hits, misses FROM learnt").fetchall()

    def pupil(name, loose):
        a = SystemArm(tmp_path / name, TableEar(), HashEmbedder(), taught=True,
                      known_learnt=learnt, loose=loose)
        a.hear(0, "Tam is Oda's cousin.")
        return a.answer(q("Who is Tam's cousin?"))

    assert pupil("strict", False) != "oda"
    assert pupil("loose", True) == "oda"


def test_cleaning_keeps_a_name_said_in_the_possessive():
    row = {"subject": "ivo", "object": "bren", "place": None, "quantity": None}
    assert SystemArm.clean(row, "Ivo is Bren's cousin.")["object"] == "bren"
    row = {"subject": "lanterns", "object": "drael", "place": "pantry", "quantity": None}
    assert SystemArm.clean(row, "The lanterns are Drael's, and they live in the pantry.")[
        "object"] == "drael"
