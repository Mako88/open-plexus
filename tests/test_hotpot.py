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
