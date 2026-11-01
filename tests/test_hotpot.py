"""HotpotQA rows load with their supporting paragraphs marked, and the benchmark's scorer reads
answers the way its authors did."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.hotpot import answer_of, exact, f1, normalise, paragraph_text, questions  # noqa: E402


def test_the_first_validation_question_has_two_supporting_paragraphs_of_ten():
    q = questions(1)[0]
    assert q["answer"] == "yes" and q["type"] == "comparison"
    assert len(q["paragraphs"]) == 10 and sum(p["supporting"] for p in q["paragraphs"]) == 2
    assert paragraph_text(q["paragraphs"][1]).startswith("Scott Derrickson: ")
    assert questions(2, skip=1)[0]["id"] == questions(3)[1]["id"]


def test_the_scorer_normalises_as_the_benchmark_does():
    assert normalise("The Eiffel Tower.") == "eiffel tower"
    assert exact("the Eiffel Tower", "Eiffel Tower") and not exact("Eiffel", "Eiffel Tower")
    assert round(f1("the Eiffel Tower in Paris", "Eiffel Tower"), 3) == 0.667
    assert f1("yes", "no") == 0.0 and f1(None, "yes") == 0.0


def test_the_answer_is_the_last_answer_line_else_the_last_line():
    assert answer_of("Both are American.\nAnswer: yes") == "yes"
    assert answer_of("**Answer:** Chief of Protocol.") == "Chief of Protocol"
    assert answer_of("I think\nArthur's Magazine") == "Arthur's Magazine"
    assert answer_of("   ") is None


def test_recall_follows_the_cue_from_the_first_paragraph_to_the_second():
    import numpy as np
    from openplexus.hotpot import recall
    # a question about a film's director; the film paragraph names the director, whose own paragraph answers
    def embed(texts):  # a text points toward each thing it mentions: films, Burton, towns
        return np.array([[float("film" in t), float("direct" in t or "Burton" in t), float("town" in t)]
                         for t in texts], dtype=np.float32)

    paragraphs = ["Woodson: a town in Arkansas.", "Tim Burton: an American director.", "Ed Wood: a 1994 film by Burton."]
    # the question's words reach the film; the film's text names Burton, which cues the director
    assert recall(paragraphs, "Who directed the film Ed Wood?", embed, k=2) == [2, 1]
    assert recall(paragraphs, "Who directed the film Ed Wood?", embed, k=5) == [2, 1, 0]


def test_a_comparison_arm_is_a_part_tag_first_and_a_plain_spec_otherwise(tmp_path, monkeypatch):
    import argparse
    import json
    sys.path.insert(0, str(ROOT / "scripts"))
    import hotpot_reading as hr
    (tmp_path / "state").mkdir()
    for tag, ids in (("ask", ["a", "b"]), ("ask-ask-round2", ["a"]), ("top2", ["a", "b", "c"])):
        (tmp_path / "state" / f"hotpot-{tag}.part.jsonl").write_text(
            "".join(json.dumps({"id": i}) + "\n" for i in ids), encoding="utf-8")
    monkeypatch.setattr(hr, "ROOT", tmp_path)
    plain = argparse.Namespace(adapter=None)
    assert set(hr.other_rows("ask-ask-round2", plain)) == {"a"}  # a trained asker's reading, by its tag
    assert set(hr.other_rows("ask", plain)) == {"a", "b"}  # the plain arm, whatever adapter this arm has
    assert set(hr.other_rows("top:2", plain)) == {"a", "b", "c"}  # a spec, resolved to its tag
    assert hr.other_rows("never-read", plain) == {}
