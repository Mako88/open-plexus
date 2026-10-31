"""Step 3 on maths, fork (d): the store feeding the thinking.

The system's own solved problems, each a question with the solution that reached its gold
number, indexed by an embedding of the question; before the trunk solves, the most similar
solved problem is fetched and shown. The examples are the system's own: the rollout rows of the
step form (`step_rollouts.py`, a problem solved where an answer line reached the gold number,
its steps the walk's), or the whole solutions the trunk wrote on training problems
(`grpo_reading.py --split train`, a row solved where its answer was right). No human writes a
row. The plexus mechanism itself, read on the proposer: does a fetched analogue change what the
trunk writes next.
"""

from __future__ import annotations

import numpy as np

from openplexus.maths import same_number
from openplexus.steps import answer_of


def solved_examples(rows: list[dict]) -> list[dict]:
    """From rollout rows, one {"question", "steps", "answer"} a solved problem: the deepest
    right answer line's walk, so the steps are the fullest right solution the walk found."""
    best: dict[str, dict] = {}
    for r in rows:
        if not r.get("is_answer_line") or not same_number(answer_of(r["candidate"]), r["answer"]):
            continue
        if r["question"] not in best or len(r["steps"]) > len(best[r["question"]]["steps"]):
            best[r["question"]] = {"question": r["question"], "steps": list(r["steps"]), "answer": r["answer"]}
    return [x for x in best.values() if x["steps"]]


def solved_solutions(rows: list[dict]) -> list[dict]:
    """From reading rows (`grpo_reading.py`), one {"question", "solution", "answer"} a problem
    whose whole solution was right."""
    best: dict[str, dict] = {}
    for r in rows:
        if r.get("correct") and r.get("reply") and r["question"] not in best:
            best[r["question"]] = {"question": r["question"], "solution": r["reply"].strip(), "answer": r["answer"]}
    return list(best.values())


def example_text(ex: dict) -> str:
    if "solution" in ex:
        return f"Problem: {ex['question']}\nSolution:\n{ex['solution']}"
    steps = "".join(f"{i + 1}. {s}\n" for i, s in enumerate(ex["steps"]))
    return f"Problem: {ex['question']}\nSteps:\n{steps}{len(ex['steps']) + 1}. Answer: {ex['answer']}"


class SolvedIndex:
    """Solved problems under an embedder of their questions; `nearest(question)` is the most
    similar one's text, never the same question (a test question that leaked into the store
    would otherwise be answered by its own solution)."""

    def __init__(self, examples: list[dict], embed):
        self.examples = examples
        self.embed = embed
        self.keys = self._normalise(embed([x["question"] for x in examples])) if examples else np.zeros((0, 1))

    @staticmethod
    def _normalise(x) -> np.ndarray:
        x = np.asarray(x, dtype=np.float32)
        return x / np.maximum(np.linalg.norm(x, axis=1, keepdims=True), 1e-12)

    def nearest_many(self, questions: list[str]) -> list[str | None]:
        if not self.examples:
            return [None] * len(questions)
        q = self._normalise(self.embed(questions))
        scores = q @ self.keys.T
        out = []
        for i, question in enumerate(questions):
            order = np.argsort(-scores[i])
            pick = next((j for j in order if self.examples[j]["question"] != question), None)
            out.append(None if pick is None else example_text(self.examples[pick]))
        return out
