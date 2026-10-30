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
    "sentence that names a quantity and works out its number, never a step already taken. When "
    "the steps so far already give the final answer, reply with 'Answer: N' where N is the final "
    "number, and nothing else.\n"
    "Example. Problem: Tom buys 3 pens at 2 dollars each and pays with a 10 dollar note. What is "
    "his change?\nSteps so far:\n(none yet)\nNext step: The pens cost 3 * 2 = 6 dollars.\n"
    "Steps so far:\n1. The pens cost 3 * 2 = 6 dollars.\nNext step: His change is 10 - 6 = 4 "
    "dollars.\nSteps so far:\n1. The pens cost 3 * 2 = 6 dollars.\n2. His change is 10 - 6 = 4 "
    "dollars.\nNext step: Answer: 4"
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


def steps_text(steps: list[str], last: int | None = None) -> str:
    """The steps numbered one a line; with `last`, only that many of the most recent, the
    earlier ones noted as left out, which bounds a MATH solution of fifty steps to a prompt the
    card reads in a second."""
    if last is not None and len(steps) > last:
        skipped = len(steps) - last
        return f"({skipped} earlier steps left out)\n" + "".join(
            f"{i + 1}. {s}\n" for i, s in enumerate(steps) if i >= skipped)
    return "".join(f"{i + 1}. {s}\n" for i, s in enumerate(steps)) or "(none yet)\n"


def judge_prompt(problem: str, steps: list[str], candidate: str, last: int | None = None) -> str:
    """What the trunk reads before its state is taken: the problem, the steps so far (the last
    `last` of them if given), the candidate, and a cue that a judgement follows. An answer line
    is shown as PRM800K writes its final step, "# Answer" and the number on its own line, so
    the evaluator meets the search's answers in the form it was trained on."""
    answer = answer_of(candidate)
    if answer is not None:
        candidate = f"# Answer\n\n{answer}"
    return (f"Problem: {problem}\nSolution so far:\n{steps_text(steps, last)}Next step: {candidate}\n"
            "Is the next step right:")


def clean_step(text: str) -> str:
    """A proposed step as one line without the list numbering the trunk copies from the steps
    so far ("4. 4. Multiply ..." was seen), nor the "Next step:" it copies from the turn."""
    line = re.sub(r"^\s*next step\s*:\s*", "", text.strip().split("\n")[0], flags=re.I)
    return re.sub(r"^(\s*\d+[.)]\s*)+", "", line).strip()


def step_messages(problem: str, steps: list[str], example: str | None = None) -> list[dict]:
    """The trunk's turn to propose the next step; with `example`, a solved problem fetched from
    the store is shown first (`openplexus.fetch`)."""
    shown = f"A solved problem like this one:\n{example}\n\n" if example else ""
    return [{"role": "system", "content": STEP_SYSTEM},
            {"role": "user", "content": f"{shown}Problem: {problem}\nSteps so far:\n{steps_text(steps)}Next step:"}]


def answer_of(step: str) -> str | None:
    """The final number if the step is an answer line, else None."""
    m = re.match(r"\s*\**\s*answer\s*:?\**\s*(.+)", step, re.I)
    return number_in(m.group(1)) if m else None


def rollout_many(starts: list[tuple[str, list[str]]], propose_many, answer_many,
                 depth: int = 10) -> list[list[str]]:
    """Each start, (problem, steps so far), continued by the trunk's own next steps, one a
    round, until an answer line or `depth` more steps; a path still open then, or one the trunk
    can only extend with a step already on it, is closed with the trunk's forced answer line
    from `answer_many([(problem, steps), ...])`. In lockstep so the trunk is asked in batches.
    Returns the steps added to each start, the last of them an answer line. This is how a
    candidate step is labelled by outcome: roll it out several times and count the right
    answers."""
    paths = [list(s) for _, s in starts]
    added: list[list[str]] = [[] for _ in starts]
    live = list(range(len(starts)))
    to_close: list[int] = []
    for _ in range(depth):
        if not live:
            break
        proposals = propose_many([(starts[i][0], paths[i]) for i in live], 1)
        still = []
        for i, cands in zip(live, proposals):
            c = clean_step(cands[0]) if cands else ""
            if not c or c in paths[i]:
                to_close.append(i)
                continue
            paths[i].append(c)
            added[i].append(c)
            if answer_of(c) is None:
                still.append(i)
        live = still
    to_close.extend(live)
    if to_close:
        answers = answer_many([(starts[i][0], paths[i]) for i in to_close])
        for i, a in zip(to_close, answers):
            a = clean_step(a)
            paths[i].append(a)
            added[i].append(a)
    return added


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
    when the kept paths are all finished or the depth is spent. A candidate that repeats a step
    already on its path is dropped: the trunk re-emits its last step when it has nothing to add,
    and a judge trained on PRM800K rates a repeat as right. Returns the best path a problem.
    With one candidate a step the evaluator chooses nothing: that is the control."""
    beams: list[list[Path]] = [[Path([], [])] for _ in problems]
    for _ in range(depth):
        asks = [(i, p) for i, b in enumerate(beams) for p in b if not p.done]
        if not asks:
            break
        proposals = propose_many([(problems[i], p.steps) for i, p in asks], k)
        judge_asks, owners = [], []
        for (i, p), cands in zip(asks, proposals):
            for c in dict.fromkeys(clean_step(c) for c in cands):
                if c and c not in p.steps:
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
