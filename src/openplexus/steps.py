"""Step 3 on maths: the evaluator over steps of a solution, and the search that uses it.

The step is one line of a solution, which the trunk writes reliably in one pass; the thing the
one-pass trunk cannot do is hold a whole solution right. The evaluator reads the problem, the
steps so far and one candidate next step through the trunk, and a head over the last token's
hidden state says whether the candidate is a good step. Its labels are PRM800K (openai/prm800k
on GitHub, `data/prm800k/`, not tracked): human ratings of each step of model solutions to
MATH problems, +1 right, 0 reasonable but not progress, -1 wrong. The search on GSM8K has the
trunk propose several next steps, keeps those the evaluator rates, and stops at an answer line
the evaluator accepts. The deployed system has every input here: the problem.
"""

from __future__ import annotations

import json
import random
import re
from pathlib import Path

from openplexus.maths import number_in

ROOT = Path(__file__).resolve().parent.parent.parent
PRM = ROOT / "data" / "prm800k"

STEP_SYSTEM = (
    "Solve the problem one step at a time. Reply with only the next step of the solution: one "
    "sentence that names a quantity and works out its number. When the steps so far already "
    "give the final answer, reply with 'Answer: N' where N is the final number, and nothing else."
)


def prm_examples(split: str, n: int | None = None, seed: int = 0) -> list[dict]:
    """Labelled candidate steps from a PRM800K split: each {"problem", "steps", "candidate",
    "rating", "label", "record"}, where `steps` are the solution's steps before the candidate
    (the labeller's chosen completion at each, or the human's), `rating` is the labeller's and
    `label` is +1 unless the rating is -1. With `n`, that many drawn at random with as many
    wrong as right; the draw is by record so one solution's steps do not straddle a split."""
    out = []
    with open(PRM / f"{split}.jsonl", encoding="utf-8") as f:
        for i, line in enumerate(f):
            r = json.loads(line)
            problem = r["question"]["problem"]
            steps: list[str] = []
            for s in r["label"]["steps"]:
                for c in s["completions"] or []:  # None where the labeller wrote the step
                    if c["rating"] is None:
                        continue
                    out.append({"problem": problem, "steps": list(steps), "candidate": c["text"].strip(),
                                "rating": c["rating"], "label": -1 if c["rating"] == -1 else 1,
                                "record": i})
                chosen = s["human_completion"]["text"] if s["chosen_completion"] is None and s["human_completion"] \
                    else s["completions"][s["chosen_completion"]]["text"] if s["chosen_completion"] is not None \
                    else None
                if chosen is None:
                    break
                steps.append(chosen.strip())
    if n is None:
        return out
    rng = random.Random(seed)
    good = [x for x in out if x["label"] > 0]
    bad = [x for x in out if x["label"] < 0]
    rng.shuffle(good)
    rng.shuffle(bad)
    half = min(n // 2, len(bad), len(good))
    picked = good[:half] + bad[:half]
    rng.shuffle(picked)
    return picked


def steps_text(steps: list[str]) -> str:
    return "".join(f"{i + 1}. {s}\n" for i, s in enumerate(steps)) or "(none yet)\n"


def judge_prompt(problem: str, steps: list[str], candidate: str) -> str:
    """What the trunk reads before its state is taken: the problem, the steps so far, the
    candidate, and a cue that a judgement follows."""
    return f"Problem: {problem}\nSolution so far:\n{steps_text(steps)}Next step: {candidate}\nIs the next step right:"


def step_messages(problem: str, steps: list[str]) -> list[dict]:
    """The trunk's turn to propose the next step."""
    return [{"role": "system", "content": STEP_SYSTEM},
            {"role": "user", "content": f"Problem: {problem}\nSteps so far:\n{steps_text(steps)}Next step:"}]


def answer_of(step: str) -> str | None:
    """The final number if the step is an answer line, else None."""
    m = re.match(r"\s*\**\s*answer\s*:?\**\s*(.+)", step, re.I)
    return number_in(m.group(1)) if m else None
