"""The plan the trunk writes is parsed or refused, earlier answers are substituted forward, the
answer is checked only against the system's own kind, and the loop falls back to the whole
question when there is no plan."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.decompose import PLAN_SYSTEM, contradicts, parse_plan, solve_decomposed, substitute  # noqa: E402


def test_the_plan_is_parsed_or_refused():
    assert parse_plan("Kind: a person\n1. Who portrayed Corliss Archer in Kiss and Tell?\n2. What government position did #1 hold?") == \
        {"kind": "a person", "steps": ["Who portrayed Corliss Archer in Kiss and Tell?", "What government position did #1 hold?"]}
    assert parse_plan("kind: yes or no.\n1) Were both American?") == {"kind": "yes or no", "steps": ["Were both American?"]}
    assert parse_plan("1. Who is older?")["kind"] == ""
    assert parse_plan("I think the answer is Tim Burton.") is None
    assert parse_plan("Kind: a name\n" + "\n".join(f"{i}. q" for i in range(1, 9))) is None  # too many to be a plan


def test_earlier_answers_fill_later_questions():
    assert substitute("What position did #1 hold?", ["Shirley Temple"]) == "What position did Shirley Temple hold?"
    assert substitute("Is #1 older than #2?", ["Annie Morton", "Terry Richardson"]) == "Is Annie Morton older than Terry Richardson?"
    assert substitute("What did #2 do?", ["only one"]) == "What did #2 do?"  # not yet answered, left as written
    assert substitute("What did #1 do?", [""]) == "What did #1 do?"  # an empty answer is not substituted


def test_the_answer_is_checked_against_the_system_s_own_kind_only():
    assert contradicts("a person", "Yes") and not contradicts("a person", "Peter Schmeichel")
    assert contradicts("yes or no", "Terry Richardson") and not contradicts("yes or no", "no.")
    assert contradicts("a date", "the Nixon years") and not contradicts("a year", "1969")
    assert contradicts("a place", None)
    assert not contradicts("", "anything")  # no kind predicted, nothing to contradict


def test_the_loop_runs_the_plan_and_falls_back_without_one():
    passages = ["Kiss and Tell (1945 film): a film starring Shirley Temple as Corliss Archer.",
                "Shirley Temple: an actress who became Chief of Protocol of the United States."]
    seen = []

    def ask(messages):
        seen.append(messages)
        user = messages[1]["content"]
        if messages[0]["content"] == PLAN_SYSTEM:
            return "Kind: a government position\n1. Who portrayed Corliss Archer in Kiss and Tell?\n2. What government position did #1 hold?"
        if "Facts already found" in user:
            assert "- Who portrayed Corliss Archer in Kiss and Tell? Shirley Temple" in user
            return "Answer: Chief of Protocol"
        if "Who portrayed" in user:
            return "She was Shirley Temple.\nAnswer: Shirley Temple"
        if "did Shirley Temple hold" in user:
            return "Answer: Chief of Protocol of the United States"
        return "Answer: ?"

    out = solve_decomposed(ask, passages, "What government position was held by the woman who portrayed Corliss Archer in Kiss and Tell?")
    assert out["reply"] == "Answer: Chief of Protocol" and out["answers"] == ["Shirley Temple", "Chief of Protocol of the United States"]
    assert out["steps"][1] == "What government position did Shirley Temple hold?" and not out["fallback"] and not out["retried"]
    assert out["turns"] == 4 and all("Passages:" in m[1]["content"] for m in seen)

    def wrong_kind(messages):
        if messages[0]["content"] == PLAN_SYSTEM:
            return "Kind: a person\n1. Who founded Kaiser Ventures?"
        return "Answer: Henry J. Kaiser" if "(The answer is a person.)" in messages[1]["content"] else "Answer: Yes"

    out = solve_decomposed(wrong_kind, passages, "Kaiser Ventures was founded by whom?")
    assert out["retried"] and out["reply"] == "Answer: Henry J. Kaiser" and out["turns"] == 4

    out = solve_decomposed(lambda m: "Tim Burton directed it.\nAnswer: Tim Burton", passages, "Who directed Ed Wood?")
    assert out["fallback"] and out["reply"].endswith("Answer: Tim Burton") and out["plan"] is None and out["turns"] == 2
