from unfused.arms import Blind, Recall
from unfused.exam.run import judge, run
from unfused.exam.world import Question, generate_house
from unfused.store import HashEmbedder


def q(answer, form="direct", stale=None):
    return Question("Where does Vessarine keep the lanterns?", answer, "room", form, 1, 2,
                    stale)


def test_judge_counts_contains_match():
    assert judge(q("attic"), "In the attic.")["correct"]
    assert not judge(q("attic"), "I don't know.")["correct"]


def test_a_negative_answered_is_invented_and_a_refusal_is_not():
    assert judge(q(None, "negative"), "In the cellar.")["invented"]
    assert not judge(q(None, "negative"), "I don't know.")["invented"]


def test_the_old_place_is_stale_not_just_wrong():
    r = judge(q("attic", "update", stale="scullery"), "The scullery.")
    assert r["stale"] and not r["correct"]


class Parrot:
    """A faculty stub that answers with its first note, so plumbing is checked without a model."""

    name = "parrot"

    def chat(self, system, user, budget=32):
        lines = [x[2:] for x in user.splitlines() if x.startswith("- ")]
        return lines[0] if lines else "I don't know."


def test_recall_answers_from_disk_after_reopening(tmp_path):
    house = generate_house(0)
    result = run(house, lambda: Recall(tmp_path, Parrot(), HashEmbedder()), reopen_every=10,
                 limit=40)
    assert len(result["rows"]) == 40
    assert result["summary"]["score"] > 0


def test_blind_answers_every_negative_so_invents_on_all_of_them():
    house = generate_house(0)
    result = run(house, lambda: Blind(house))
    assert result["summary"]["invented"] == 1.0
