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
CREATE TABLE IF NOT EXISTS synonyms (asked TEXT NOT NULL, stored TEXT NOT NULL,
                                     same INTEGER NOT NULL, PRIMARY KEY (asked, stored));
CREATE TABLE IF NOT EXISTS plans (shape TEXT PRIMARY KEY, plan TEXT NOT NULL,
                                  used INTEGER NOT NULL DEFAULT 0);
"""
SLOT = re.compile(r"^<(\d+)>$")


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
                 relations_shown: int = 30, plans: bool = False,
                 known_plans: dict | None = None, planner=None, judge=None,
                 searched: bool = False, depth: int = 3, frontier: int = 20000) -> None:
        self.ear = ear
        # what writes a question's plan; the ear unless a different faculty is given
        self.planner = planner or ear
        # what says whether two wordings mean the same; judged once a pair and kept
        self.judge = judge or ear
        # whether the system finds a question's chain itself, from the plan's anchors
        # and goal, instead of following the plan's steps
        self.searched = searched
        self.depth = depth
        self.frontier = frontier
        self.embedder = embedder
        self.names_shown = names_shown
        self.relations_shown = relations_shown
        self.plans = plans
        Path(directory).mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(str(Path(directory) / "system.db"))
        self.db.executescript(SCHEMA)
        # plans learnt elsewhere hold no facts, only how a question is asked, so a
        # system may start with them the way a node starts with another's tables
        self.db.executemany("INSERT OR IGNORE INTO plans (shape, plan) VALUES (?, ?)",
                            [(k, json.dumps(v)) for k, v in (known_plans or {}).items()])
        self.db.commit()
        self.last_notes: list[str] = []
        self.pending: tuple[str, dict] | None = None

    def dials(self) -> dict:
        return {"ear": self.ear.name, "planner": self.planner.name, "judge": self.judge.name,
                "searched": self.searched, "depth": self.depth,
                "planner_calls": getattr(self.planner, "calls", None) if self.planner
                is not self.ear else None, "names_shown": self.names_shown,
                "relations_shown": self.relations_shown, "plans": self.plans,
                **dict(zip(("shapes", "plan_uses"), self.db.execute(
                    "SELECT COUNT(*), COALESCE(SUM(used), 0) FROM plans").fetchone())),
                "ear_calls": getattr(self.ear, "calls", None),
                "ear_unparsed": len(getattr(self.ear, "failures", []))}

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
        pair = (row["subject"], row["object"])
        for rid, subject, obj, place in self.db.execute(
                "SELECT id, subject, object, place FROM assertions"
                " WHERE superseded IS NULL AND object IS NOT NULL AND place IS NOT NULL"
        ).fetchall():
            # the pair in either order: "the cards are Ada's" and "Ada took the cards"
            # name the same owner and thing in swapped slots
            same_pair = ((same(subject, pair[0]) and same(obj, pair[1]))
                         or (same(subject, pair[1]) and same(obj, pair[0])))
            if same_pair and not same(place, row["place"]):
                self.db.execute("UPDATE assertions SET superseded = ? WHERE id = ?",
                                (turn, rid))

    # -- answering -------------------------------------------------------------

    def resolve(self, value: str, question: str = "") -> str:
        """A filler the system has never heard, mapped to a name it has, if the ear
        says one means it. The embedding shortlists; the ear chooses."""
        rows = self.db.execute("SELECT name, embedding FROM names").fetchall()
        if not rows or any(same(value, r[0]) for r in rows):
            return value
        names = [r[0] for r in rows]
        vectors = np.vstack([np.frombuffer(r[1], dtype=np.float32) for r in rows])
        order = np.argsort(-(vectors @ self.embedder.encode([value])[0]))[: self.names_shown]
        options = [names[i] for i in order]
        choice = self.judge.choose(
            f"In the question '{question}', which of these is '{value}' or another word for "
            "the same thing? If none is, answer -1.", options)
        return options[choice] if choice is not None else value

    def shape(self, question: str) -> tuple[str, list[str]]:
        """The question with every filler it names cut out: a capitalised word past the
        first, or a name the system has heard. Two questions of one shape ask the same
        thing about different things, so the plan for one is the plan for the other."""
        names = sorted((r[0] for r in self.db.execute("SELECT name FROM names")
                        if r[0] and len(r[0]) > 2), key=len, reverse=True)
        spans: list[tuple[int, int]] = []
        for m in re.finditer(r"\b[A-Z][a-z]+(?:'s)?", question):
            if m.start() > 0:
                spans.append((m.start(), m.start() + len(m.group(0).removesuffix("'s"))))
        for name in names:
            for m in re.finditer(rf"\b{re.escape(name)}\b", question, re.I):
                if not any(a < m.end() and m.start() < b for a, b in spans):
                    spans.append((m.start(), m.end()))
        fillers, shape, at = [], "", 0
        for a, b in sorted(spans):
            shape += question[at:a] + f"<{len(fillers)}>"
            fillers.append(question[a:b])
            at = b
        return shape + question[at:], fillers

    def plan(self, question: str) -> dict | None:
        """The steps for a question: the plan kept for its shape with this question's
        fillers put in, or the ear's reading of it, held until it finds an answer."""
        self.pending = None
        if not self.plans:
            return self.planner.rewrite(question)
        shape, fillers = self.shape(question)
        row = self.db.execute("SELECT plan FROM plans WHERE shape = ?", (shape,)).fetchone()
        if row:
            kept = json.loads(row[0])
            slots = [int(m.group(1)) for st in kept["steps"] for v in st.values()
                     if isinstance(v, str) and (m := SLOT.match(v))]
            if all(i < len(fillers) for i in slots):
                self.db.execute("UPDATE plans SET used = used + 1 WHERE shape = ?", (shape,))
                return {**kept, "steps": [
                    {k: (fillers[int(m.group(1))] if isinstance(v, str)
                         and (m := SLOT.match(v)) else v) for k, v in st.items()}
                    for st in kept["steps"]]}
        rewritten = self.planner.rewrite(question)
        if rewritten and fillers:
            lowered = [norm(f) or "" for f in fillers]

            def slot(k, v):
                # a filler the planner wrote as part of what the question said ("dairy"
                # for "in the dairy") is that filler: left literal, the kept plan would
                # ask about the dairy for every room
                if k == "relation" or not isinstance(v, str) or is_unknown(v) or not norm(v):
                    return v
                hit = next((i for i, f in enumerate(lowered) if f and same(norm(v), f)), None)
                return v if hit is None else f"<{hit}>"

            steps = [{k: slot(k, v) for k, v in st.items()}
                     for st in rewritten.get("steps", [])]
            used = {v for st in steps for v in st.values() if isinstance(v, str)}
            # a plan that leaves out something the question named has dropped a
            # constraint, and kept it would answer every question of the shape alike
            if all(f"<{i}>" in used for i in range(len(fillers))):
                self.pending = (shape, {**rewritten, "steps": steps})
        return rewritten

    def query(self, question: str) -> dict | None:
        """The question as steps with unknowns, read by the ear from worked examples,
        and every known filler resolved to a name the system has."""
        rewritten = self.plan(question)
        if not rewritten:
            return None
        steps = []
        for raw in rewritten.get("steps", []):
            step = {"relation": raw.get("relation")}
            for slot in SLOTS:
                value = raw.get(slot)
                if is_unknown(value):
                    step[slot] = value.strip()
                elif (v := norm(value)) is not None and len(v) > 2:
                    # A name is an identity and never a paraphrase: a person never
                    # heard of stays unheard of, rather than becoming someone who was.
                    named = re.search(rf"\b{re.escape(v)}\b", question, re.I)
                    is_name = named and named.group(0)[:1].isupper() and named.start() > 0
                    step[slot] = v if is_name else self.resolve(v, question)
            steps.append(step)
        answer = str(rewritten.get("answer", "")).strip()
        # A quantity that was told is read, not counted: "how many jars are in the
        # cellar" asks for the number heard, not for how many rows match.
        counted = bool(rewritten.get("count")) and not any(
            step.get("quantity") == answer for step in steps)
        return {"steps": steps, "answer": answer, "count": counted}

    def means(self, asked: str, stored: str, example: str = "") -> bool:
        """Whether a stored relation answers an asked one: the same words, or a pair the
        ear once judged to mean the same. Judged once and remembered, so the table is
        what the system has learnt about its own vocabulary; it only grows, and two
        nodes' tables merge by union."""
        if same(asked, stored):
            return True
        row = self.db.execute("SELECT same FROM synonyms WHERE asked = ? AND stored = ?",
                              (asked, stored)).fetchone()
        if row is not None:
            return bool(row[0])
        verdict = self.judge.synonymous(asked, stored, example)
        self.db.execute("INSERT OR IGNORE INTO synonyms VALUES (?, ?, ?)",
                        (asked, stored, int(verdict)))
        self.db.commit()
        return verdict

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
                      and self.means(relation, r["relation"], r["heard"])]
            if worded:
                candidates = worded
            elif len(fixed) < 2:
                # a relation worded differently from anything stored about these
                # fillers: the ear sees the stored facts whole and says which one is
                # of the kind asked, which is word meaning and composes nothing
                facts = sorted({" ".join(v for v in (r["subject"], r["relation"], r["object"],
                                                     r["place"], r["quantity"]) if v)
                                for r in candidates})
                ask = f"Which of these facts is about '{relation}'? If none is, answer -1."
                choice = self.judge.choose(ask, facts) if facts and relation else None
                if choice is None:
                    candidates = []
                else:
                    chosen = facts[choice]
                    candidates = [r for r in candidates if chosen == " ".join(
                        v for v in (r["subject"], r["relation"], r["object"], r["place"],
                                    r["quantity"]) if v)]
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

    def search(self, query: dict) -> list[tuple[dict, int, list[str]]]:
        """The chain found by the system rather than written by the planner.

        Of the plan only the anchors (every filler it names) and the goal (the step
        holding the answer: its relation and the slot the answer sits in) are read.
        The shortest chains of assertions, each sharing a filler with the next, that
        touch every anchor and end on an assertion whose relation means the goal's
        are the answers, found breadth first up to `self.depth` assertions."""
        want = str(query.get("answer", "")).strip()
        goal = next((s for s in reversed(query.get("steps", []))
                     if any(isinstance(v, str) and v.strip() == want for v in s.values())),
                    None)
        if goal is None:
            return []
        relation = norm(goal.get("relation"))
        slot = next((k for k in SLOTS if isinstance(goal.get(k), str)
                     and goal[k].strip() == want), None)
        anchors = {norm(v) for s in query["steps"] for k, v in s.items()
                   if k != "relation" and isinstance(v, str) and not is_unknown(v) and norm(v)}
        rows = self.rows()
        fill = [[r[k] for k in SLOTS if r[k]] for r in rows]
        # which assertions each filler links, by the exact filler: a chain passes from
        # one assertion to the next through a thing both name
        by_filler: dict[str, list[int]] = {}
        for i, fs in enumerate(fill):
            for f in set(fs):
                by_filler.setdefault(f, []).append(i)
        touching = {a: {i for i, fs in enumerate(fill) if any(same(a, f) for f in fs)}
                    for a in anchors}
        ending: dict[str, bool] = {}

        def ends(i):
            stored = rows[i]["relation"]
            if not relation or not stored:
                return False
            if stored not in ending:
                ending[stored] = self.means(relation, stored, rows[i]["heard"])
            return ending[stored]

        def answer_of(i):
            free = [f for f in fill[i] if not any(same(a, f) for a in anchors)]
            if slot and rows[i][slot] in free:
                return rows[i][slot]
            return free[0] if free else None

        paths = [(i,) for i in sorted(set().union(*touching.values()))] if anchors else []
        for _ in range(self.depth):
            done = [p for p in paths if ends(p[-1]) and answer_of(p[-1])
                    and all(touching[a] & set(p) for a in anchors)]
            if done:
                return [({want: answer_of(p[-1])}, max(rows[i]["turn"] for i in p),
                         [rows[i]["heard"] for i in p]) for p in done]
            grown = []
            for p in paths:
                for j in sorted({j for f in fill[p[-1]] for j in by_filler[f]} - set(p)):
                    grown.append(p + (j,))
            # a filler named in hundreds of assertions makes the frontier explode
            # without making a chain likelier; past the cap the search gives up
            if len(grown) > self.frontier:
                return []
            paths = grown
        return []

    def answer(self, question: Question) -> str:
        query = self.query(question.text)
        self.last_notes = [json.dumps(query)] if query else ["(unreadable)"]
        if not query:
            return "I don't know."
        want = str(query.get("answer", "")).strip()
        found = [(b, t, h) for b, t, h in
                 (self.search(query) if self.searched else self.solve(query)) if b.get(want)]
        self.last_notes += sorted({x for _, _, h in found for x in h})
        if not found:
            return "I don't know."
        # a plan whose every answer is a name the question gave has read the question
        # back rather than looked anything up, and is not kept
        given = {norm(f) for f in self.shape(question.text)[1]}
        echoes = all(any(same(norm(b[want]) or "", g) for g in given if g)
                     for b, _, _ in found)
        if self.pending and not echoes:
            self.db.execute("INSERT OR IGNORE INTO plans (shape, plan) VALUES (?, ?)",
                            (self.pending[0], json.dumps(self.pending[1])))
            self.db.commit()
        if query.get("count"):
            return str(len({b[want] for b, _, _ in found}))
        best = max(found, key=lambda f: f[1])
        return best[0][want]
