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
    """Reads a sentence from READINGS, and a question as one statement whose reading
    is that question's steps in QUERIES."""

    name = "table"

    def read(self, sentence, relations=None):
        if sentence.startswith("Q:"):
            return QUERIES[sentence[2:]]["steps"]
        return READINGS.get(sentence, [])

    def rewrite(self, question):
        query = QUERIES[question]
        return {"statements": [f"Q:{question}"], "answer": query["answer"],
                "count": query["count"]}

    def choose(self, prompt, options):
        return None


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
