"""The GRPO rewards: the gold number on the answer line scores, the last number stands in when
there is no answer line, and the form earns its small bonus only at the end."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

import grpo  # noqa: E402
from grpo import agreement, answer_in, answer_line, correct, select_rows  # noqa: E402
from grpo_reading import agreement_curve, arm_tag, ask_text, lora_scales, majority, paired  # noqa: E402


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


def test_the_paired_gap_counts_flips_on_shared_questions_only():
    ps = [{"question": q} for q in "abcde"]
    mine = {q: {"correct": c} for q, c in zip("abcd", [1, 1, 0, 1])}
    other = {q: {"correct": c} for q, c in zip("abce", [1, 0, 1, 1])}
    pr = paired(mine, other, ps)
    assert (pr["n"], pr["made_right"], pr["made_wrong"]) == (3, 1, 1)
    assert pr["gap"] == 0 and pr["standard_errors"] == 0
    assert paired(mine, {}, ps) is None


def test_arm_tags_and_the_fetched_ask():
    import argparse
    test = argparse.Namespace(split="test", skip=0)
    train = argparse.Namespace(split="train", skip=1000)
    assert arm_tag("base", test) == "base"
    assert arm_tag("state/grpo/round1/adapter", test) == "round1"
    assert arm_tag("state/grpo/round1/adapter:fetch", test) == "round1-fetch"
    assert arm_tag("state/grpo/round1/adapter", train) == "round1-train-skip1000"
    assert ask_text("Q", None) == "Q"
    assert ask_text("Q", "Problem: P\nSolution:\nS").endswith("Now solve this one.\n\nQ")


def test_the_chosen_problems_are_every_wrong_one_and_as_many_right():
    rows = [{"question": f"w{i}", "answer": "1", "correct": False} for i in range(3)]
    rows += [{"question": f"r{i}", "answer": "2", "correct": True} for i in range(10)]
    chosen = select_rows(rows, wrong_share=0.5, seed=0)
    assert len(chosen) == 6 and sum(q["question"].startswith("w") for q in chosen) == 3
    assert select_rows(rows, wrong_share=0.25, seed=0).__len__() == 12
    assert select_rows(rows, wrong_share=1.0)[0]["question"].startswith("w")
    assert set(chosen[0]) == {"question", "answer"}


def test_each_served_arm_asks_for_its_own_adapter_only():
    import argparse
    import pytest
    loaded = [{"id": 0, "path": "state/gguf/round1-lora.gguf", "scale": 0.0},
              {"id": 1, "path": "state/gguf/round2-lora.gguf", "scale": 0.0}]
    assert lora_scales("state/grpo/round2/adapter", loaded) == [{"id": 0, "scale": 0.0}, {"id": 1, "scale": 1.0}]
    assert lora_scales(None, loaded) == [{"id": 0, "scale": 0.0}, {"id": 1, "scale": 0.0}]
    with pytest.raises(SystemExit):
        lora_scales("state/grpo/round3/adapter", loaded)
    served = argparse.Namespace(split="test", skip=0, served="http://127.0.0.1:8093")
    assert arm_tag("base", served) == "base-served"
    assert arm_tag("state/grpo/round2/adapter", served) == "round2-served"


def test_the_second_fetch_form_has_its_own_tag():
    import argparse
    test = argparse.Namespace(split="test", skip=0)
    assert arm_tag("state/grpo/round2/adapter:fetch2", test) == "round2-fetch2"


def test_the_majority_answer_and_its_agreement():
    assert majority(["7", "7.0", "8", None]) == ("7", 0.5)
    assert majority([None, None]) == (None, 0.0)
    assert majority(["1250", "1250.0", "1250.00"]) == ("1250", 1.0)
    curve = agreement_curve([{"agreement": 1.0, "correct": True}, {"agreement": 0.5, "correct": False},
                             {"agreement": 1.0, "correct": True}])
    assert curve == [{"agreement_at_least": 1.0, "n": 2, "accuracy": 1.0},
                     {"agreement_at_least": 0.5, "n": 3, "accuracy": 0.667}]
    import argparse
    assert arm_tag("state/grpo/round2/adapter:vote4", argparse.Namespace(split="test", skip=0)) == "round2-vote4"


def test_the_agreement_reward_is_the_group_majority_with_no_key():
    grpo.GROUP = 4
    done = ["Answer: 7", "so 7.0", "Answer: 8", "nothing here",           # group one: 7 wins
            "Answer: 3", "Answer: 4", "Answer: 4", "Answer: 3"]           # group two: a tie, the first group counted
    assert agreement(done) == [1.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0]
    assert agreement(["nothing", "nothing", "nothing", "nothing"]) == [0.0] * 4


def test_a_group_below_the_agreement_floor_is_not_scored():
    grpo.GROUP, grpo.AGREE_MIN = 4, 1.0
    try:
        assert agreement(["Answer: 7", "Answer: 7", "Answer: 7", "Answer: 7"]) == [1.0] * 4
        assert agreement(["Answer: 7", "Answer: 7", "Answer: 7", "Answer: 8"]) == [0.0] * 4
    finally:
        grpo.AGREE_MIN = 0.0
