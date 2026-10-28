"""bAbI as stories: the data file has one row per question, each carrying the story so far;
this groups the rows into stories, each a list of sentences heard in order with its questions
asked at the moments they were asked, and maps the archive's supporting line numbers (which
count question lines) onto sentence indices.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent.parent


@dataclass
class Question:
    text: str
    answer: str
    heard: int  # sentences of the story heard when it was asked
    supporting: list[int]  # sentence indices, 0-based, in the order the archive gives them
    task: int
    story: int


@dataclass
class Story:
    task: int
    index: int
    sentences: list[str] = field(default_factory=list)
    questions: list[Question] = field(default_factory=list)

    @property
    def episode(self) -> str:
        return f"t{self.task}s{self.index}"


def stories(tasks: list[int], split: str = "test", path: Path | None = None) -> list[Story]:
    path = path or ROOT / "data" / "babi" / f"babi_{split}.jsonl"
    by_key: dict[tuple[int, int], list[dict]] = {}
    with open(path, encoding="utf-8") as f:
        for line in f:
            r = json.loads(line)
            if r["task"] in tasks:
                by_key.setdefault((r["task"], r["story"]), []).append(r)
    out = []
    for (task, index), rows in by_key.items():
        s = Story(task=task, index=index)
        # The archive numbers facts and questions in one sequence; a question's passage holds
        # every fact before it, so the facts between two questions are the passage's growth.
        line_of_sentence: list[int] = []
        cursor = 1
        for r in rows:
            sents = r["passage"].strip("\n").split("\n")
            for j in range(len(s.sentences), len(sents)):
                s.sentences.append(sents[j])
                line_of_sentence.append(cursor)
                cursor += 1
            cursor += 1  # the question's own line
            supporting = [line_of_sentence.index(n) for n in r["supporting"]]
            s.questions.append(Question(text=r["question"], answer=r["answer"],
                                        heard=len(sents), supporting=supporting,
                                        task=task, story=index))
        out.append(s)
    out.sort(key=lambda s: (tasks.index(s.task), s.index))
    return out
