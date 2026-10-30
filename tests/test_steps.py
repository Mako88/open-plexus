"""PRM800K candidates load with their steps so far and a label; the judge prompt and the step
turn carry the pieces; an answer line is told from a step."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.steps import (answer_of, clean_step, judge_prompt, prm_examples, rollout_many,  # noqa: E402
                              search_many, step_messages)


def test_candidates_load_with_their_steps_so_far_and_a_label():
    exs = prm_examples("phase1_test")
    assert len(exs) > 100
    assert {x["label"] for x in exs} == {-1, 1}
    assert all(x["rating"] in (-1, 0, 1) for x in exs)
    first = [x for x in exs if x["record"] == 0]
    assert first[0]["steps"] == [] and any(len(x["steps"]) >= 2 for x in first)
    assert all(x["label"] == (-1 if x["rating"] == -1 else 1) for x in exs)
    balanced = prm_examples("phase1_test", n=40, seed=1)
    assert len(balanced) == 40 and sum(x["label"] > 0 for x in balanced) == 20


def test_the_judge_prompt_and_the_step_turn_carry_the_pieces():
    p = judge_prompt("Three pencils cost 1.24.", ["Let p be the price.", "3p = 1.24."], "p = 0.41.")
    assert p.startswith("Problem: Three pencils") and "1. Let p" in p and "2. 3p" in p
    assert p.endswith("Next step: p = 0.41.\nIs the next step right:")
    m = step_messages("Three pencils cost 1.24.", [])
    assert m[1]["content"].endswith("Steps so far:\n(none yet)\nNext step:")
    short = judge_prompt("P", [f"step {i}" for i in range(10)], "c", last=3)
    assert "(7 earlier steps left out)\n8. step 7\n9. step 8\n10. step 9\n" in short and "1. step 0" not in short
    assert "Next step: # Answer\n\n1250\nIs the next step right:" in judge_prompt("P", [], "**Answer:** $1,250")


def test_a_proposed_step_loses_the_numbering_the_trunk_copies():
    assert clean_step("4. 4. Multiply 9 by 60 to get 540 meters.\nThen") == "Multiply 9 by 60 to get 540 meters."
    assert clean_step("  2) Answer: 540") == "Answer: 540"
    assert clean_step("Next step: 3. His change is 4 dollars.") == "His change is 4 dollars."


def test_an_answer_line_is_told_from_a_step():
    assert answer_of("Answer: 18") == "18"
    assert answer_of("**Answer:** $1,250") == "1250"
    assert answer_of("She makes 18 dollars a day.") is None


# A stub trunk: for each problem a right solution and a wrong one, step by step; the stub
# evaluator scores a step by whether it lies on the right solution.
RIGHT = {"a": ["2 + 2 = 4.", "Answer: 4"], "b": ["3 * 3 = 9.", "9 - 1 = 8.", "Answer: 8"]}
WRONG = {"a": ["2 + 2 = 5.", "Answer: 5"], "b": ["3 * 3 = 6.", "6 - 1 = 5.", "Answer: 5"]}


def propose_many(asks, k):
    out = []
    for problem, steps in asks:
        n = len(steps)
        cands = [WRONG[problem][n]] if n < len(WRONG[problem]) else []
        if k > 1 and n < len(RIGHT[problem]):
            cands.append(RIGHT[problem][n])
        out.append(cands[:k])
    return out


def judge_many(asks):
    return [1.0 if candidate in RIGHT[problem] else -1.0 for problem, steps, candidate in asks]


def test_the_search_follows_the_evaluator_and_the_control_follows_the_trunk():
    best = search_many(["a", "b"], propose_many, judge_many, k=2, beam=1)
    assert [p.answer() for p in best] == ["4", "8"] and all(p.done for p in best)
    assert best[1].steps == RIGHT["b"] and best[1].mean() == 1.0
    control = search_many(["a", "b"], propose_many, judge_many, k=1, beam=1)
    assert [p.answer() for p in control] == ["5", "5"]


def test_a_candidate_that_repeats_a_step_on_its_path_is_dropped():
    asked = []

    def judge(asks):
        asked.extend(c for _, _, c in asks)
        return [0.0] * len(asks)

    # the first problem's only proposal repeats its step: the second problem must still get its own
    def propose(asks, k):
        return [[steps[0]] if problem == "a" and steps else [f"{problem} step {len(steps)}"]
                for problem, steps in asks]

    best = search_many(["a", "b"], propose, judge, k=1, depth=3)
    assert best[0].steps == ["a step 0"] and best[0].done and best[0].answer() is None
    assert best[1].steps == ["b step 0", "b step 1", "b step 2"]
    assert asked == ["a step 0", "b step 0", "b step 1", "b step 2"]


def test_a_rollout_runs_to_an_answer_line_or_is_closed_with_a_forced_one():
    # "a" answers on its own; "b" repeats itself and is closed; "c" runs out of depth and is closed
    def propose(asks, k):
        out = []
        for problem, steps in asks:
            out.append([{"a": ["a1", "Answer: 4"], "b": ["b1", "b1"], "c": ["c1", "c2", "c3", "c4"]}
                        [problem][min(len(steps), 3)]])
        return out

    asked = []

    def answer(asks):
        asked.extend(p for p, _ in asks)
        return ["Answer: 9" for _ in asks]

    added = rollout_many([("a", []), ("b", []), ("c", [])], propose, answer, depth=3)
    assert added == [["a1", "Answer: 4"], ["b1", "Answer: 9"], ["c1", "c2", "c3", "Answer: 9"]]
    assert asked == ["b", "c"]


def test_a_path_the_trunk_cannot_extend_ends_without_an_answer():
    best = search_many(["a"], lambda asks, k: [[] for _ in asks], judge_many, k=2, beam=2, depth=3)
    assert best[0].done and best[0].answer() is None and best[0].steps == []
