"""Solved problems come out of the rollout rows, the nearest one is fetched into the step turn,
and a question never fetches its own solution."""

import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.fetch import SolvedIndex, example_text, solved_examples, solved_solutions  # noqa: E402
from openplexus.steps import step_messages  # noqa: E402

ROWS = [
    {"question": "Ann has 2 apples and buys 3. How many?", "answer": "5", "steps": [], "candidate": "Answer: 5",
     "is_answer_line": True, "fraction": 1.0},
    {"question": "Ann has 2 apples and buys 3. How many?", "answer": "5", "steps": ["She buys 3."],
     "candidate": "Answer: 5", "is_answer_line": True, "fraction": 1.0},
    {"question": "Bo runs 3 km twice. How far?", "answer": "6", "steps": ["3 * 2 = 6."], "candidate": "Answer: 7",
     "is_answer_line": True, "fraction": 0.0},
    {"question": "Bo runs 3 km twice. How far?", "answer": "6", "steps": ["3 * 2 = 6."], "candidate": "6 km.",
     "is_answer_line": False, "fraction": 1.0},
]


def embed(texts):  # a stub embedder: apples and running are different directions
    return np.array([[1.0, 0.0] if "apple" in t else [0.0, 1.0] for t in texts], dtype=np.float32)


def test_solved_problems_are_the_deepest_right_answer_lines():
    got = solved_examples(ROWS)
    assert [(x["question"][:3], x["steps"]) for x in got] == [("Ann", ["She buys 3."])]
    assert example_text(got[0]) == "Problem: Ann has 2 apples and buys 3. How many?\nSteps:\n1. She buys 3.\n2. Answer: 5"


def test_the_nearest_solved_problem_is_fetched_but_never_the_question_itself():
    index = SolvedIndex(solved_examples(ROWS) + [{"question": "Cy eats 1 apple a day for 4 days. How many?",
                                                   "steps": ["1 * 4 = 4."], "answer": "4"}], embed)
    got = index.nearest_many(["Di has 6 apples and gives 1 away. How many?", "Ann has 2 apples and buys 3. How many?"])
    assert got[0].startswith("Problem: Ann has") and got[1].startswith("Problem: Cy eats")
    assert SolvedIndex([], embed).nearest_many(["anything"]) == [None]
    turn = step_messages("Di has 6 apples.", [], got[0])
    assert turn[1]["content"].startswith("A solved problem like this one:\nProblem: Ann has")
    assert turn[1]["content"].endswith("Problem: Di has 6 apples.\nSteps so far:\n(none yet)\nNext step:")


def test_whole_solutions_are_solved_where_right_and_shown_as_one():
    rows = [{"question": "Q1", "answer": "5", "reply": "2 + 3 = 5.\nAnswer: 5\n", "correct": True},
            {"question": "Q2", "answer": "6", "reply": "Answer: 7", "correct": False},
            {"question": "Q1", "answer": "5", "reply": "again", "correct": True}]
    got = solved_solutions(rows)
    assert [x["question"] for x in got] == ["Q1"]
    assert example_text(got[0]) == "Problem: Q1\nSolution:\n2 + 3 = 5.\nAnswer: 5"
