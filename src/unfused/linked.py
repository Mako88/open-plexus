"""Linked recall: what a turn mentions becomes a symbol, and recall spreads along them.

Hearing a turn writes it to the store and asks the faculty what it mentions:
people, things, places, numbers. Each mention is a symbol, and the symbol is
linked to the fragment. Symbols and links only ever grow, so two nodes' tables
merge by union.

Recall starts from the symbols a question refers to, including by another word:
the embedding shortlists known symbols and the faculty decides which the
question means, because a sentence encoder does not know that a larder is a
pantry and the faculty does. Activation then spreads the way it does in ACT-R:
each symbol's share is divided among the fragments it links to, and each
fragment's among its symbols, so a word that is in everything passes on almost
nothing (the fan effect). What surfaces joins the text recall hits.

This is the co-occurrence flood from the Python lineage, rebuilt with senses
that can read.
"""

from __future__ import annotations

import json
import re
from collections import defaultdict
from pathlib import Path

import numpy as np

from .arms import Recall, ask_with_notes

MENTIONS = (
    "List the particular people, objects, places, numbers, colours and occupations this "
    "text mentions, as a JSON list of short noun phrases copied from it, without articles. "
    "Leave out pronouns and anything generic. Reply with the JSON list only."
)

RESOLVE = (
    "Here is a question and a numbered list of things from earlier conversation. Which "
    "listed things does the question refer to, including by a different word for the same "
    "thing? Reply with a JSON list of their numbers only, or [] if none."
)

LINKS = """
CREATE TABLE IF NOT EXISTS symbols (name TEXT PRIMARY KEY, embedding BLOB NOT NULL);
CREATE TABLE IF NOT EXISTS links (symbol TEXT NOT NULL, fragment TEXT NOT NULL,
                                  PRIMARY KEY (symbol, fragment));
CREATE TABLE IF NOT EXISTS read_cache (text TEXT PRIMARY KEY, mentions TEXT NOT NULL);
"""


def parse_list(reply: str) -> list:
    match = re.search(r"\[.*\]", reply, re.S)
    if not match:
        return []
    try:
        value = json.loads(match.group(0))
    except json.JSONDecodeError:
        return []
    return value if isinstance(value, list) else []


def canonical(mention: str) -> str:
    words = str(mention).strip().strip(".,;:!?'\"").split()
    while words and words[0].lower() in ("the", "a", "an", "some", "all"):
        words = words[1:]
    text = " ".join(words)
    return text if any(c.isupper() for c in text[:1]) else text.lower()


class LinkedRecall(Recall):
    name = "linked"

    def __init__(self, directory: Path, faculty, embedder, k: int = 5, spread: int = 5,
                 decay: float = 0.5, shortlist: int = 20, **store_dials) -> None:
        super().__init__(directory, faculty, embedder, k=k, hops=1, **store_dials)
        self.spread = spread
        self.decay = decay
        self.shortlist = shortlist
        self.db = self.store.db
        self.db.executescript(LINKS)
        self.db.commit()

    def dials(self) -> dict:
        return {**super().dials(), "spread": self.spread, "decay": self.decay,
                "shortlist": self.shortlist}

    def mentions(self, text: str) -> list[str]:
        row = self.db.execute("SELECT mentions FROM read_cache WHERE text = ?",
                              (text,)).fetchone()
        if row:
            return json.loads(row[0])
        found = [canonical(m) for m in parse_list(self.faculty.chat(MENTIONS, text, 80))
                 if isinstance(m, (str, int, float))]
        found = sorted({m for m in found if m})
        self.db.execute("INSERT OR IGNORE INTO read_cache VALUES (?, ?)",
                        (text, json.dumps(found)))
        self.db.commit()
        return found

    def hear(self, turn: int, text: str) -> None:
        self.store.now = float(turn)
        fragment = self.store.write(text, provenance=f"turn:{turn}")
        for symbol in self.mentions(text):
            if not self.db.execute("SELECT 1 FROM symbols WHERE name = ?", (symbol,)).fetchone():
                vector = self.store.embedder.encode([symbol])[0].astype(np.float32)
                self.db.execute("INSERT INTO symbols VALUES (?, ?)", (symbol, vector.tobytes()))
            self.db.execute("INSERT OR IGNORE INTO links VALUES (?, ?)", (symbol, fragment.id))
        self.db.commit()

    def resolve(self, question: str) -> list[str]:
        """The known symbols a question refers to, by name or by another word."""
        rows = self.db.execute("SELECT name, embedding FROM symbols").fetchall()
        if not rows:
            return []
        names = [r[0] for r in rows]
        exact = [n for n in names if re.search(rf"\b{re.escape(n)}\b", question, re.I)]
        vectors = np.vstack([np.frombuffer(r[1], dtype=np.float32) for r in rows])
        cues = [question] + self.mentions(question)
        scores = (vectors @ self.store.embedder.encode(cues).T).max(axis=1)
        candidates = [names[i] for i in np.argsort(-scores)[: self.shortlist]]
        candidates = [c for c in candidates if c not in exact]
        chosen: list[str] = []
        if candidates:
            listing = "\n".join(f"{i}. {c}" for i, c in enumerate(candidates))
            reply = self.faculty.chat(RESOLVE, f"Question: {question}\n\n{listing}", 40)
            for i in parse_list(reply):
                if isinstance(i, int) and 0 <= i < len(candidates):
                    chosen.append(candidates[i])
        return exact + chosen

    def recall(self, query: str) -> list[str]:
        seeds = set(self.resolve(query))
        first: dict[str, float] = defaultdict(float)
        for symbol in seeds:
            fragments = self._fragments(symbol)
            for fid in fragments:
                first[fid] += 1.0 / len(fragments)
        second: dict[str, float] = defaultdict(float)
        for fid, amount in first.items():
            symbols = [s for s in self._symbols(fid) if s not in seeds]
            for symbol in symbols:
                fragments = self._fragments(symbol)
                for other in fragments:
                    second[other] += self.decay * amount / len(symbols) / len(fragments)
        activation = defaultdict(float, first)
        for fid, amount in second.items():
            activation[fid] += amount
        spread = sorted(activation, key=lambda f: -activation[f])[: self.spread]
        found = {h.fragment.id: h.fragment for h in self.store.search(query, k=self.k)}
        for fid in spread:
            if fid not in found:
                fragment = self.store.get(fid)
                if fragment is not None and fragment.archived_at is None:
                    found[fid] = fragment
        self.store.mark_recalled(found)
        return self.render(list(found.values()))

    def answer(self, question) -> str:
        self.last_notes = self.recall(question.text)
        return ask_with_notes(self.faculty, self.last_notes, question.text)

    def _fragments(self, symbol: str) -> list[str]:
        return [r[0] for r in self.db.execute(
            "SELECT fragment FROM links WHERE symbol = ?", (symbol,))]

    def _symbols(self, fid: str) -> list[str]:
        return [r[0] for r in self.db.execute(
            "SELECT symbol FROM links WHERE fragment = ?", (fid,))]

