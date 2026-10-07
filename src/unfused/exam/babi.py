"""bAbI (Weston et al., 2015) as a second world: somebody else's test of the same abilities.

Each story is told a line at a time and each question is asked where it stands in the
story, so an arm meets it exactly as it meets the house. The data is the 1000 test
questions per task in `data/babi/babi_{split}.jsonl`, fetched by `scripts/fetch_babi.sh`;
each row there carries the whole story up to its question, and rows whose stories
continue one another are one story.
"""

from __future__ import annotations

import hashlib
import json

from unfused.home import home

from .world import House, Question, modal_answers  # noqa: F401

DATA = home() / "data" / "babi"

TASKS = {
    1: "single supporting fact", 2: "two supporting facts", 3: "three supporting facts",
    4: "two argument relations", 5: "three argument relations", 6: "yes/no questions",
    7: "counting", 8: "lists/sets", 9: "simple negation", 10: "indefinite knowledge",
    11: "basic coreference", 12: "conjunction", 13: "compound coreference",
    14: "time reasoning", 15: "basic deduction", 16: "basic induction",
    17: "positional reasoning", 18: "size reasoning", 19: "path finding",
    20: "agent's motivations",
}


def stories(task: int, limit: int | None = None, split: str = "test") -> list[House]:
    """The task's stories, each a world of its own lines and the questions asked in it."""
    out: list[House] = []
    lines: list[str] = []
    questions: list[Question] = []
    with (DATA / f"babi_{split}.jsonl").open(encoding="utf-8") as f:
        for raw in f:
            row = json.loads(raw)
            if row["task"] != task:
                continue
            told = [x for x in row["passage"].split("\n") if x]
            if lines and told[: len(lines)] != lines:
                out.append(_house(task, len(out), lines, questions))
                questions = []
                if limit is not None and len(out) >= limit:
                    return out
            lines = told
            questions.append(Question(text=row["question"], answer=row["answer"],
                                      kind=f"qa{task}", form=f"qa{task}", delay=0,
                                      asked_at=len(told) - 1))
    if lines and (limit is None or len(out) < limit):
        out.append(_house(task, len(out), lines, questions))
    return out


def _house(task: int, index: int, lines: list[str], questions: list[Question]) -> House:
    return House(seed=task * 10000 + index, n_turns=len(lines), facts=[], turns=list(lines),
                 questions=list(questions), told_at={})


def fingerprint(worlds: list[House]) -> str:
    return hashlib.sha256("".join(w.fingerprint() for w in worlds).encode()).hexdigest()[:16]
