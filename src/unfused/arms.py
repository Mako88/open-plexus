"""The arms an exam compares. Each hears the conversation once and answers questions.

An arm keeps everything it knows under its own directory, and the exam closes
and reopens it during the conversation, so an answer given after a reopen came
from disk. The faculty is frozen and shared; it holds nothing between calls.

`Blind` and `FullContext` are the two baselines every reading carries. `Blind`
answers the commonest answer for the kind of question and reads nothing; the
earlier branches lost to it. `FullContext` hands the faculty the whole
transcript, which is how a stateless model would do it and what the memory has
to be worth against.
"""

from __future__ import annotations

from pathlib import Path
from typing import Protocol

from .exam.world import House, Question

SYSTEM = (
    "You answer questions about a household from the notes given with each question. "
    "The notes may use different words for the same thing: a lamp may be called a "
    "lantern, a job may be described instead of named. Answer in a few words. Only if "
    "nothing in the notes answers the question, answer: I don't know."
)


class Arm(Protocol):
    name: str

    def hear(self, turn: int, text: str) -> None: ...

    def answer(self, question: Question) -> str: ...

    def dials(self) -> dict: ...

    def close(self) -> None: ...


def ask_with_notes(faculty, notes: list[str], question: str) -> str:
    lines = "\n".join(f"- {n}" for n in notes) if notes else "(none)"
    return faculty.chat(SYSTEM, f"Notes:\n{lines}\n\nQuestion: {question}", budget=24)


class Blind:
    name = "blind"

    def __init__(self, house: House) -> None:
        self.table = house.modal_answers()

    def hear(self, turn: int, text: str) -> None:
        pass

    def answer(self, question: Question) -> str:
        return self.table.get(question.kind, "")

    def dials(self) -> dict:
        return {}

    def close(self) -> None:
        pass


class FullContext:
    """The whole transcript as notes, every time. Kept on disk like any arm."""

    name = "full-context"

    def __init__(self, directory: Path, faculty) -> None:
        self.path = Path(directory) / "transcript.txt"
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.faculty = faculty
        self.turns = self.path.read_text(encoding="utf-8").splitlines() if self.path.exists() else []

    def hear(self, turn: int, text: str) -> None:
        self.turns.append(text)
        with self.path.open("a", encoding="utf-8") as f:
            f.write(text + "\n")

    def answer(self, question: Question) -> str:
        return ask_with_notes(self.faculty, self.turns, question.text)

    def dials(self) -> dict:
        return {}

    def close(self) -> None:
        pass


class Recall:
    """Every turn written to the store; every question arrives with what it calls up.

    Recall is automatic: the question itself is the query, nothing asks for it.
    With `hops` above one, the recalled fragments are themselves used as queries
    and what they call up joins the notes, which is how a fact that shares no
    words with the question can still surface.
    """

    name = "recall"

    def __init__(self, directory: Path, faculty, embedder, k: int = 5, hops: int = 1,
                 **store_dials) -> None:
        from .store import SqliteStore

        self.faculty = faculty
        self.k = k
        self.hops = hops
        self.store = SqliteStore(Path(directory) / "store.db", embedder=embedder,
                                 **store_dials)
        self.last_notes: list[str] = []

    def hear(self, turn: int, text: str) -> None:
        self.store.now = float(turn)
        self.store.write(text, provenance=f"turn:{turn}")

    def recall(self, query: str) -> list[str]:
        seen: dict[str, str] = {}
        frontier = [query]
        for _ in range(self.hops):
            found = []
            for q in frontier:
                for hit in self.store.search(q, k=self.k):
                    if hit.fragment.id not in seen:
                        seen[hit.fragment.id] = hit.fragment.text
                        found.append(hit.fragment.text)
            frontier = found
        self.store.mark_recalled(seen)
        return list(seen.values())

    def answer(self, question: Question) -> str:
        self.last_notes = self.recall(question.text)
        return ask_with_notes(self.faculty, self.last_notes, question.text)

    def dials(self) -> dict:
        return {"k": self.k, "hops": self.hops, **self.store.dials()}

    def close(self) -> None:
        self.store.close()
