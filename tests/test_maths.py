"""The GSM8K scorer reads a number from a reply or a program's output; every gold answer parses;
a program runs in a fresh interpreter and a hanging one is cut off."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.maths import number_in, problems, program_in, run_program, same_number  # noqa: E402


def test_a_number_is_read_from_a_reply():
    assert number_in("18") == "18"
    assert number_in("She makes $18 a day.") == "18"
    assert number_in("So 16 - 7 = 9 eggs, 9 * 2 = 18.\nAnswer: 18") == "18"
    assert number_in("Answer: 1,250.0") == "1250"
    assert number_in("no idea") is None
    assert same_number("18.0", "18") and not same_number("17", "18") and not same_number(None, "18")


def test_every_gold_answer_is_a_number():
    ps = problems()
    assert len(ps) == 1319
    assert all(same_number(p["answer"], p["answer"]) for p in ps)


def test_a_program_runs_and_a_hanging_one_is_cut_off():
    assert run_program(program_in("```python\neggs = 16 - 3 - 4\nprint(eggs * 2)\n```")) == "18"
    assert run_program("print('the answer is 7.0')") == "7"
    assert run_program("while True: pass", timeout=1.0) is None
    assert run_program("raise ValueError") is None
