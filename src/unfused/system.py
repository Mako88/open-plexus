"""The system answers: assertions stored once, questions matched and chained.

The ear reads each turn into assertions, which go into an exact table with the
turn they were heard at. A question is read into steps with unknowns, and the
system finds bindings for them one step at a time: a later step sees what an
earlier one bound, which is how a chain is followed. Nothing the faculty does
here composes two facts.

Matching is generic and has three rules, none of them about this house:

- A known filler in a step must appear in the row, in whichever slot the ear
  put it, because one ear writes "the lanterns are Drael's" with the lanterns as
  subject and another with Drael.
- An unknown binds to the same slot of the row, or if that slot is empty, to a
  filler of the row that no known in the step accounts for.
- The relation must match where any row with that wording matches; where none
  does, it is dropped, but only for a step that still pins two fillers, so a
  step naming one person cannot bind everything said about them.

Where several bindings answer, the one resting on the latest assertion wins,
which is how "moved to the pantry" overrides "keeps in the laundry". Where none
does, the answer is "I don't know", and that is the system's own evidence rather
than the faculty's judgement.
"""

from __future__ import annotations

import json
import re
import sqlite3
from pathlib import Path

import numpy as np

from .exam.world import Question

SLOTS = ("subject", "object", "place", "quantity")
NULLS = {"", "none", "null", "n/a", "unknown", "nothing"}
ARTICLES = ("the ", "a ", "an ", "some ", "all the ", "all ")

SCHEMA = """
CREATE TABLE IF NOT EXISTS assertions (
    id INTEGER PRIMARY KEY, subject TEXT, relation TEXT, object TEXT, place TEXT,
    quantity TEXT, turn INTEGER NOT NULL, heard TEXT NOT NULL, superseded INTEGER
);
CREATE TABLE IF NOT EXISTS names (name TEXT PRIMARY KEY, embedding BLOB NOT NULL);
CREATE TABLE IF NOT EXISTS readings (text TEXT PRIMARY KEY, assertions TEXT NOT NULL);
"""


def norm(value) -> str | None:
    if value is None:
        return None
    text = str(value).strip().strip(".,;:!?\"").lower()
    text = re.sub(r"'s$", "", text)
    for article in ARTICLES:
        if text.startswith(article):
            text = text[len(article):]
    return None if text in NULLS else text


def same(a: str, b: str) -> bool:
    """Equal, or one a whole-word part of the other ("wool cards" and "cards")."""
    if a == b:
        return True
    short, long = sorted((a, b), key=len)
    return len(short) > 2 and re.search(rf"\b{re.escape(short)}\b", long) is not None


def is_unknown(value) -> bool:
    return isinstance(value, str) and value.strip().startswith("?")


class SystemArm:
    name = "system"

    def __init__(self, directory: Path, ear, embedder, names_shown: int = 25,
                 relations_shown: int = 30) -> None:
        self.ear = ear
        self.embedder = embedder
        self.names_shown = names_shown
        self.relations_shown = relations_shown
        Path(directory).mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(str(Path(directory) / "system.db"))
        self.db.executescript(SCHEMA)
        self.db.commit()
        self.last_notes: list[str] = []

    def dials(self) -> dict:
        return {"ear": self.ear.name, "names_shown": self.names_shown,
                "relations_shown": self.relations_shown}

    def close(self) -> None:
        self.db.close()

    # -- hearing ---------------------------------------------------------------

    def relations(self) -> list[str]:
        rows = self.db.execute(
            "SELECT relation, COUNT(*) c FROM assertions GROUP BY relation ORDER BY c DESC LIMIT ?",
            (self.relations_shown,)).fetchall()
        return [r[0] for r in rows if r[0]]

    def read(self, text: str) -> list[dict]:
        """The ear's reading of a sentence, once: the same words read again are the same
        assertions, heard again at a new turn."""
        row = self.db.execute("SELECT assertions FROM readings WHERE text = ?",
                              (text,)).fetchone()
        if row:
            return json.loads(row[0])
        assertions = self.ear.read(text, self.relations())
        self.db.execute("INSERT INTO readings VALUES (?, ?)", (text, json.dumps(assertions)))
        return assertions

    def hear(self, turn: int, text: str) -> None:
        for a in self.read(text):
            row = {s: norm(a.get(s)) for s in SLOTS}
            relation = norm(a.get("relation"))
            if not row["subject"] or not relation:
                continue
            self.supersede(row, turn)
            self.db.execute(
                "INSERT INTO assertions (subject, relation, object, place, quantity, turn, heard)"
                " VALUES (?,?,?,?,?,?,?)",
                (row["subject"], relation, row["object"], row["place"], row["quantity"],
                 turn, text))
            for name in (v for v in row.values() if v):
                if not self.db.execute("SELECT 1 FROM names WHERE name = ?", (name,)).fetchone():
                    vector = self.embedder.encode([name])[0].astype(np.float32)
                    self.db.execute("INSERT INTO names VALUES (?, ?)", (name, vector.tobytes()))
        self.db.commit()

    def supersede(self, row: dict, turn: int) -> None:
        """A later assertion that the same subject's same thing is somewhere else
        replaces the earlier one. It is kept, marked, and no longer matched. Only
        where a thing is named: two counts of one kind of object in two rooms are
        two facts, not a correction."""
        if not row["object"] or not row["place"]:
            return
        for rid, subject, obj, place in self.db.execute(
                "SELECT id, subject, object, place FROM assertions"
                " WHERE superseded IS NULL AND object IS NOT NULL AND place IS NOT NULL"
        ).fetchall():
            if (same(subject, row["subject"]) and same(obj, row["object"])
                    and not same(place, row["place"])):
                self.db.execute("UPDATE assertions SET superseded = ? WHERE id = ?",
                                (turn, rid))

    # -- answering -------------------------------------------------------------

    def resolve(self, value: str) -> str:
        """A filler the system has never heard, mapped to a name it has, if the ear
        says one means it. The embedding shortlists; the ear chooses."""
        rows = self.db.execute("SELECT name, embedding FROM names").fetchall()
        if not rows or any(same(value, r[0]) for r in rows):
            return value
        names = [r[0] for r in rows]
        vectors = np.vstack([np.frombuffer(r[1], dtype=np.float32) for r in rows])
        order = np.argsort(-(vectors @ self.embedder.encode([value])[0]))[: self.names_shown]
        options = [names[i] for i in order]
        choice = self.ear.choose(f"Which of these is '{value}', or another word for it?",
                                 options)
        return options[choice] if choice is not None else value

    def query(self, question: str) -> dict | None:
        """The question as steps: rewritten as the statements that would answer it,
        each read by the same reader that reads what is heard, and every known filler
        resolved to a name the system has."""
        rewritten = self.ear.rewrite(question)
        if not rewritten:
            return None
        # An unknown is handed to the reader as a word it copies like a name, since
        # it drops a bare "?a" to "a"; the word is turned back into the unknown after.
        steps = []
        for statement in rewritten.get("statements", []):
            unknowns = sorted(set(re.findall(r"\?[a-z]", statement)))
            words = {u: f"Qx{u[1].upper()}" for u in unknowns}
            for u, w in words.items():
                statement = statement.replace(u, w)
            back = {w.lower(): u for u, w in words.items()}
            for a in self.ear.read(statement):
                step = {"relation": a.get("relation")}
                for slot in SLOTS:
                    raw = a.get(slot)
                    if is_unknown(raw):
                        step[slot] = raw.strip()
                        continue
                    v = norm(raw)
                    if v is None:
                        continue
                    if v in back:
                        step[slot] = back[v]
                    elif len(v) > 2:
                        step[slot] = self.resolve(v)
                steps.append(step)
        return {"steps": steps, "answer": rewritten.get("answer", ""),
                "count": bool(rewritten.get("count")), "statements":
                rewritten.get("statements", [])}

    def rows(self) -> list[dict]:
        cols = ("subject", "relation", "object", "place", "quantity", "turn", "heard")
        return [dict(zip(cols, r)) for r in self.db.execute(
            f"SELECT {', '.join(cols)} FROM assertions WHERE superseded IS NULL")]

    def solve(self, query: dict) -> list[tuple[dict, int, list[str]]]:
        """Every binding of the query's unknowns, with the latest turn it rests on and
        the sentences it came from."""
        rows = self.rows()
        results: list[tuple[dict, int, list[str]]] = []

        def step(i: int, bound: dict, latest: int, heard: list[str]) -> None:
            if i == len(query["steps"]):
                results.append((dict(bound), latest, heard))
                return
            s = query["steps"][i]
            wanted = {slot: s.get(slot) for slot in SLOTS if s.get(slot) not in (None, "")}
            knowns = {slot: (bound.get(v.strip()) if is_unknown(v) else norm(v))
                      for slot, v in wanted.items()}
            fixed = [v for slot, v in knowns.items() if v is not None]
            relation = norm(s.get("relation"))
            candidates = [r for r in rows if all(
                any(r[slot] and same(k, r[slot]) for slot in SLOTS) for k in fixed)]
            worded = [r for r in candidates if relation and r["relation"]
                      and same(relation, r["relation"])]
            if worded:
                candidates = worded
            elif len(fixed) < 2:
                # a relation worded differently from anything stored about these
                # fillers: the ear says which stored relation, if any, it means
                options = sorted({r["relation"] for r in candidates if r["relation"]})
                choice = (self.ear.choose(f"Which of these relations means '{relation}'?",
                                          options) if options and relation else None)
                candidates = ([r for r in candidates if r["relation"] == options[choice]]
                              if choice is not None else [])
            for r in candidates:
                new = dict(bound)
                used = {r[slot] for slot in SLOTS
                        if r[slot] and any(same(k, r[slot]) for k in fixed)}
                ok = True
                for slot, v in wanted.items():
                    if not is_unknown(v) or knowns[slot] is not None:
                        continue
                    value = r[slot] if r[slot] and r[slot] not in used else next(
                        (r[x] for x in SLOTS if r[x] and r[x] not in used), None)
                    if value is None:
                        ok = False
                        break
                    new[v.strip()] = value
                    used.add(value)
                if ok:
                    step(i + 1, new, max(latest, r["turn"]), heard + [r["heard"]])

        if query.get("steps"):
            step(0, {}, -1, [])
        return results

    def answer(self, question: Question) -> str:
        query = self.query(question.text)
        self.last_notes = [json.dumps(query)] if query else ["(unreadable)"]
        if not query:
            return "I don't know."
        want = str(query.get("answer", "")).strip()
        found = [(b, t, h) for b, t, h in self.solve(query) if b.get(want)]
        self.last_notes += sorted({x for _, _, h in found for x in h})
        if not found:
            return "I don't know."
        if query.get("count"):
            return str(len({b[want] for b, _, _ in found}))
        best = max(found, key=lambda f: f[1])
        return best[0][want]
