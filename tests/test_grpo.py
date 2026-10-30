"""The GRPO rewards: the gold number on the answer line scores, the last number stands in when
there is no answer line, and the form earns its small bonus only at the end."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from grpo import answer_in, answer_line, correct  # noqa: E402


def test_the_answer_line_is_read_and_the_last_number_stands_in():
    assert answer_in("3 + 4 = 7.\nAnswer: 7") == "7"
    assert answer_in("**Answer:** $1,250") == "1250"
    assert answer_in("She has 7 left and sells 2, so 5.") == "5"
    assert answer_in("nothing numeric") is None


def test_rewards_follow_the_gold_number_and_the_form():
    done = [[{"role": "assistant", "content": "3 + 4 = 7.\nAnswer: 7"}],
            [{"role": "assistant", "content": "Answer: 8\nActually 7."}],
            [{"role": "assistant", "content": "I think 7."}]]
    assert correct(done, ["7", "7", "7"]) == [1.0, 0.0, 1.0]
    assert answer_line(done) == [0.1, 0.0, 0.0]
    assert correct(["Answer: 7"], ["7"]) == [1.0]
