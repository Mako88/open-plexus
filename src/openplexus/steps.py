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


class Path:
    """A partial solution under search: its steps, the evaluator's score of each, and whether
    its last step is an answer line. Paths are compared by mean score a step, as in the path
    search of task 19, so a short finished path does not win by having fewer scores to sum."""

    def __init__(self, steps: list[str], scores: list[float]):
        self.steps = steps
        self.scores = scores
        self.done = bool(steps) and answer_of(steps[-1]) is not None

    def mean(self) -> float:
        return sum(self.scores) / len(self.scores) if self.scores else 0.0

    def answer(self) -> str | None:
        return answer_of(self.steps[-1]) if self.steps else None


def search_many(problems: list[str], propose_many, judge_many, k: int = 4, beam: int = 1,
                depth: int = 12) -> list[Path]:
    """Beam search over solution steps for every problem at once, in lockstep so the trunk is
    asked in batches. `propose_many([(problem, steps), ...], k)` returns k candidate next steps
    for each; `judge_many([(problem, steps, candidate), ...])` returns the evaluator's score of
    each. At every depth each live path is extended by its candidates, every path is scored by
    mean step score, and the best `beam` are kept, finished paths among them; the search ends
    when the kept paths are all finished or the depth is spent. Returns the best path a problem.
    With k=1 and beam=1 the evaluator chooses nothing: that is the control."""
    beams: list[list[Path]] = [[Path([], [])] for _ in problems]
    for _ in range(depth):
        asks = [(i, p) for i, b in enumerate(beams) for p in b if not p.done]
        if not asks:
            break
        proposals = propose_many([(problems[i], p.steps) for i, p in asks], k)
        judge_asks, owners = [], []
        for (i, p), cands in zip(asks, proposals):
            for c in dict.fromkeys(c.strip() for c in cands if c.strip()):
                judge_asks.append((problems[i], p.steps, c))
                owners.append((i, p))
        scores = judge_many(judge_asks) if judge_asks else []
        children: list[list[Path]] = [[] for _ in problems]
        for (i, p), (_, _, c), s in zip(owners, judge_asks, scores):
            children[i].append(Path(p.steps + [c], p.scores + [float(s)]))
        for i, b in enumerate(beams):
            pool = children[i] + [p for p in b if p.done]
            if not children[i] and not pool:
                pool = b  # nothing proposed: the live paths stand as they are, unfinished
            beams[i] = sorted(pool, key=Path.mean, reverse=True)[:beam] if pool else b
            if not children[i]:
                for p in beams[i]:
                    p.done = True  # a path the trunk could not extend ends without an answer
    return [max(b, key=Path.mean) for b in beams]
