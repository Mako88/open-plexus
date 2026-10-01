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
THING_PRONOUNS = {"it", "them"}
PERSON_PRONOUNS = {"he", "she", "him", "her"}

SCHEMA = """
CREATE TABLE IF NOT EXISTS assertions (
    id INTEGER PRIMARY KEY, subject TEXT, relation TEXT, object TEXT, place TEXT,
    quantity TEXT, turn INTEGER NOT NULL, heard TEXT NOT NULL, superseded INTEGER
);
CREATE TABLE IF NOT EXISTS names (name TEXT PRIMARY KEY, embedding BLOB NOT NULL);
CREATE TABLE IF NOT EXISTS readings (text TEXT PRIMARY KEY, assertions TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS synonyms (asked TEXT NOT NULL, stored TEXT NOT NULL,
                                     same INTEGER NOT NULL, PRIMARY KEY (asked, stored));
CREATE TABLE IF NOT EXISTS said (turn INTEGER PRIMARY KEY, text TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS learnt (shape TEXT NOT NULL, plan TEXT NOT NULL,
                                  hits INTEGER NOT NULL, misses INTEGER NOT NULL,
                                  PRIMARY KEY (shape, plan));
CREATE TABLE IF NOT EXISTS holds (relation TEXT PRIMARY KEY, yes INTEGER NOT NULL,
                                  no INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS kin (a TEXT NOT NULL, b TEXT NOT NULL, PRIMARY KEY (a, b));
CREATE TABLE IF NOT EXISTS kinds (filler TEXT NOT NULL, kind TEXT NOT NULL,
                                  yes INTEGER NOT NULL, PRIMARY KEY (filler, kind));
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


NUMBERS = ("zero one two three four five six seven eight nine ten eleven twelve "
           "thirteen fourteen fifteen sixteen seventeen eighteen nineteen twenty").split()


def numeral(word: str) -> int | None:
    """A number said as digits, as a word, or as 'none'."""
    if word.isdigit():
        return int(word)
    if word in ("none", "no"):
        return 0
    return NUMBERS.index(word) if word in NUMBERS else None


def unsaid(found: list, question: str) -> list:
    """The answers a question did not say itself: 'now' was heard as a place in 'it
    gets dark so early now', and 'Who has the kite now?' never asks for 'now'."""
    return [f for f in found
            if not (f[0] and re.search(rf"\b{re.escape(f[0])}\b", question, re.I))]


def is_unknown(value) -> bool:
    return isinstance(value, str) and value.strip().startswith("?")


class SystemArm:
    name = "system"

    def __init__(self, directory: Path, ear, embedder, names_shown: int = 25,
                 relations_shown: int = 30, plans: bool = False,
                 known_plans: dict | None = None, planner=None, judge=None,
                 searched: bool = False, depth: int = 3, frontier: int = 20000, hub: int = 4,
                 asked: bool = False, shares: float = 0.8,
                 moves: bool = False, taught: bool = False,
                 known_learnt: list | None = None,
                 cleans: bool = False, known_holds: list | None = None) -> None:
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
        self.hub = hub
        # whether a question is read like a statement, one fact with '?' for the answer,
        # and the chain to it left to search: no planner at all
        self.asked = asked
        self.searched = searched or asked
        # how closely two wordings' properties must match for a verdict about one to
        # answer for the other; 0 asks the judge about every pair
        self.shares = shares
        # whether a subject heard somewhere is no longer where it was heard before
        self.moves = moves
        # whether a question is answered by what was taught for its shape, and only by
        # that: a shape nobody taught goes unanswered, so the faculty only reads
        self.taught = taught
        # whether the ear's assertions are held to the words of their sentence
        self.cleans = cleans
        self.embedder = embedder
        self.names_shown = names_shown
        self.relations_shown = relations_shown
        self.plans = plans
        Path(directory).mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(str(Path(directory) / "system.db"))
        self.db.executescript(SCHEMA)
        self.db.executemany("INSERT OR IGNORE INTO learnt VALUES (?, ?, ?, ?)",
                            [tuple(x) for x in (known_learnt or [])])
        self.db.executemany("INSERT OR IGNORE INTO holds VALUES (?, ?, ?)",
                            [tuple(x) for x in (known_holds or [])])
        # plans learnt elsewhere hold no facts, only how a question is asked, so a
        # system may start with them the way a node starts with another's tables
        self.db.executemany("INSERT OR IGNORE INTO plans (shape, plan) VALUES (?, ?)",
                            [(k, json.dumps(v)) for k, v in (known_plans or {}).items()])
        self.db.commit()
        self.last_notes: list[str] = []
        self.pending: tuple[str, dict] | None = None

    def dials(self) -> dict:
        return {"ear": self.ear.name, "planner": self.planner.name, "judge": self.judge.name,
                "searched": self.searched, "asked": self.asked, "shares": self.shares, "moves": self.moves, "taught": self.taught, "cleans": self.cleans, "depth": self.depth,
                "hub": self.hub,
                "planner_calls": getattr(self.planner, "calls", None) if self.planner
                is not self.ear else None, "names_shown": self.names_shown,
                "relations_shown": self.relations_shown, "plans": self.plans,
                **dict(zip(("shapes", "plan_uses"), self.db.execute(
                    "SELECT COUNT(*), COALESCE(SUM(used), 0) FROM plans").fetchone())),
                "ear_calls": getattr(self.ear, "calls", None),
                "ear_cached": getattr(self.ear, "cached", None),
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
        """The ear's reading of a sentence, once: the same words are the same assertions."""
        row = self.db.execute("SELECT assertions FROM readings WHERE text = ?",
                              (text,)).fetchone()
        if row:
            return json.loads(row[0])
        assertions = self.ear.read(text, self.relations())
        self.db.execute("INSERT INTO readings VALUES (?, ?)", (text, json.dumps(assertions)))
        return assertions

    def hear(self, turn: int, text: str) -> None:
        assertions = self.read(text)
        self.db.execute("INSERT OR REPLACE INTO said VALUES (?, ?)", (turn, text))
        heard: list[dict] = []
        for a in assertions:
            row = {s: norm(a.get(s)) for s in SLOTS}
            relation = norm(a.get("relation"))
            if self.cleans:
                row = self.clean(row, text)
            row = self.bind(row, heard)
            if not row["subject"] or not relation:
                continue
            heard.append(row)
            self.supersede(row, turn, relation)
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

    @staticmethod
    def clean(row: dict, text: str) -> dict:
        """The ear's assertion held to the sentence it came from. A filler none of whose
        words were said is dropped ('left the milk' read with the place 'house'); a
        quantity stays only where the sentence has a number; a place loses its leading
        preposition; an object that repeats the place is dropped. The ear may be small
        and careless, and the system does not have to believe it."""
        said = set(re.findall(r"[a-z0-9']+", text.lower()))
        # 'Bren's cousin' says Bren
        said |= {re.sub(r"'s?$", "", w) for w in said}
        numbers = {"one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
                   "ten", "eleven", "twelve", "twenty"}
        out = dict(row)
        for k, v in row.items():
            if v and not any(w in said for w in re.findall(r"[a-z0-9']+", v)):
                out[k] = None
        if out["quantity"] and not (said & numbers or any(w.isdigit() for w in said)):
            out["quantity"] = None
        if out["place"]:
            out["place"] = norm(re.sub(r"^(back )?(to|into|in|at|on|from) ", "", out["place"]))
        if out["object"] and out["place"] and same(out["object"], out["place"]):
            out["object"] = None
        return out

    def bind(self, row: dict, heard: list[dict]) -> dict:
        """A pronoun the ear wrote as heard, bound to what is in focus: the latest
        assertion of the same sentence, its subject first, as centering theory ranks
        them. 'It' and 'them' take a thing, never a name; 'she' and 'him' take a name.
        Only within the sentence: across turns, the 'it' of 'it gets dark' is nothing."""
        out = dict(row)
        for k, v in row.items():
            want = (False if v in THING_PRONOUNS else True if v in PERSON_PRONOUNS
                    else None)
            if want is None:
                continue
            others = [x for j, x in row.items() if x and j != k]
            focus = (r[j] for r in reversed(heard) for j in SLOTS if j != "quantity")
            out[k] = next((f for f in focus
                           if f and f not in THING_PRONOUNS | PERSON_PRONOUNS
                           and self.named(f) == want
                           and not any(same(f, x) for x in others)), v)
        return out

    def named(self, value: str) -> bool:
        """Whether a filler is a name: heard capitalised past a sentence's first word, or
        never heard in lower case. A sentence's first word is capitalised whatever it
        is, so 'Candle moulds are in the attic' says nothing either way, and a number
        has no case and is never a name."""
        pattern = re.compile(rf"\b{re.escape(value)}\b", re.I)
        seen = False
        for (text,) in self.db.execute("SELECT text FROM said"):
            for m in pattern.finditer(text):
                if not m.group(0)[:1].isupper():
                    return False
                seen = True
                if m.start() > 0:
                    return True
        return seen

    def supersede(self, row: dict, turn: int, relation: str = "") -> None:
        """A later assertion that the same subject's same thing is somewhere else
        replaces the earlier one. It is kept, marked, and no longer matched. Only
        where a thing is named: two counts of one kind of object in two rooms are
        two facts, not a correction."""
        if self.moves and row["place"] and not row["object"] and not row["quantity"]:
            # the subject itself is somewhere ('John moved to the bedroom'): where it
            # was before is no longer where it is, whatever verb said so either time
            for rid, place in self.db.execute(
                    "SELECT id, place FROM assertions WHERE superseded IS NULL AND"
                    " object IS NULL AND quantity IS NULL AND place IS NOT NULL AND"
                    " subject = ?", (row["subject"],)).fetchall():
                if not same(place, row["place"]):
                    self.db.execute("UPDATE assertions SET superseded = ? WHERE id = ?",
                                    (turn, rid))
            return
        if not row["object"] or not row["place"]:
            return
        pair = (row["subject"], row["object"])
        for rid, subject, obj, place, said in self.db.execute(
                "SELECT id, subject, object, place, relation FROM assertions"
                " WHERE superseded IS NULL AND object IS NOT NULL AND place IS NOT NULL"
        ).fetchall():
            # the pair in either order: "the cards are Ada's" and "Ada took the cards"
            # name the same owner and thing in swapped slots
            same_pair = ((same(subject, pair[0]) and same(obj, pair[1]))
                         or (same(subject, pair[1]) and same(obj, pair[0])))
            if same_pair and not same(place, row["place"]):
                self.db.execute("UPDATE assertions SET superseded = ? WHERE id = ?",
                                (turn, rid))
            # one owner and one thing told twice in two wordings ("keeps", then "took
            # to") is one relation worded twice: the system's own evidence about its
            # vocabulary, with no judge asked
            if same_pair and relation and said and said != relation:
                self.db.executemany("INSERT OR IGNORE INTO kin VALUES (?, ?)",
                                    [(said, relation), (relation, said)])

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
            start = self.joined(question, a, b)
            shape += question[at:start] + f"<{len(fillers)}>"
            fillers.append(question[a:b])
            at = b
        return shape + question[at:], fillers

    def joined(self, question: str, a: int, b: int) -> int:
        """Where a filler's cut starts once a relation heard with it, said just before it,
        joins it: 'the person who repairs clocks' is cut to 'the person who <0>', so the
        clock mender and the bookbinder ask one thing."""
        value = norm(question[a:b])
        before = question[:a].lower()
        for (relation,) in self.db.execute(
                "SELECT DISTINCT relation FROM assertions WHERE object = ?", (value,)):
            if relation and (m := re.search(rf"\b{re.escape(relation)}\s+$", before)):
                return m.start()
        return a

    def read_question(self, question: str) -> dict | None:
        """The ear's one fact with the slot it says is asked for, as a goal for search,
        and the question's own fillers, found by the system, as its anchors."""
        read = self.ear.ask(question)
        if not read or not isinstance(read.get("assertion"), dict):
            return None
        asked = read.get("asked")
        if asked not in SLOTS:
            return None
        step = {k: v for k, v in read["assertion"].items() if k != asked}
        step[asked] = "?a"
        fillers = self.shape(question)[1]
        return {"steps": [step], "answer": "?a", "count": bool(read.get("count")),
                "anchors": fillers, "kind": norm(read.get("kind")),
                # a capitalised filler is a name, and a name never heard of must still
                # pin the answer to nothing
                "names": [norm(f) for f in fillers if f[:1].isupper() and norm(f)]}

    def plan(self, question: str) -> dict | None:
        """The steps for a question: the plan kept for its shape with this question's
        fillers put in, or the ear's reading of it, held until it finds an answer."""
        self.pending = None
        if self.asked:
            return self.read_question(question)
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
        anchors = [norm(f) for f in rewritten.get("anchors", []) if norm(f)]
        return {"steps": steps, "answer": answer, "count": counted,
                **({"anchors": anchors} if "anchors" in rewritten else {}),
                **{k: rewritten[k] for k in ("kind", "names") if rewritten.get(k)}}

    def means(self, asked: str, stored: str, example: str = "") -> bool:
        """Whether a stored relation answers an asked one: the same words, or a pair the
        ear once judged to mean the same. Judged once and remembered, so the table is
        what the system has learnt about its own vocabulary; it only grows, and two
        nodes' tables merge by union."""
        if same(asked, stored):
            return True
        if self.kindred(asked, stored):
            return True
        row = self.db.execute("SELECT same FROM synonyms WHERE asked = ? AND stored = ?",
                              (asked, stored)).fetchone()
        if row is not None:
            return bool(row[0])
        inherited = self.inherited(asked, stored) if self.shares else None
        verdict = (inherited if inherited is not None
                   else self.judge.synonymous(asked, stored, example))
        self.db.execute("INSERT OR IGNORE INTO synonyms VALUES (?, ?, ?)",
                        (asked, stored, int(verdict)))
        self.db.commit()
        return verdict

    def properties(self) -> dict[str, set[str]]:
        """Each stored wording's properties: the slots it fills and what it links, read
        off every assertion made with it. What a wording means is what it does."""
        rows = self.rows()
        agents = {r["subject"] for r in rows}
        props: dict[str, set[str]] = {}
        for r in rows:
            p = props.setdefault(r["relation"], set())
            p |= {f"fills:{k}" for k in ("object", "place") if r[k]}
            if r["quantity"] and any(c.isdigit() for c in r["quantity"]):
                p.add("fills:number")
            if r["object"] in agents:
                p.add("object:agent")
            if r["subject"] and any(r["subject"] == x["object"] for x in rows):
                p.add("subject:named-elsewhere")
        return props

    def inherited(self, asked: str, stored: str) -> bool | None:
        """A verdict already given for the asked wording against a stored wording whose
        properties match this one's closely enough, or None. One ruling about 'carves'
        answers for 'binds' when the two fill the same slots and link the same kinds."""
        judged = self.db.execute("SELECT stored, same FROM synonyms WHERE asked = ?",
                                 (asked,)).fetchall()
        if not judged:
            return None
        props = self.properties()
        mine = props.get(stored, set())
        best, verdict = 0.0, None
        for other, same_ in judged:
            theirs = props.get(other, set())
            if not mine or not theirs:
                continue
            overlap = len(mine & theirs) / len(mine | theirs)
            if overlap > best:
                best, verdict = overlap, bool(same_)
        return verdict if best >= self.shares else None

    def kindred(self, asked: str, stored: str) -> bool:
        """Whether two wordings are joined by a chain of kin: a heard "keeps" followed by
        "took to", and a "took to" by "put", make all three one relation. A wording is
        joined to its own words ("keeps" and "keeps in") before the chain is followed."""
        pairs = self.db.execute("SELECT a, b FROM kin").fetchall()
        seen = {w for pair in pairs for w in pair if same(asked, w)}
        frontier = list(seen)
        while frontier:
            w = frontier.pop()
            for a, b in pairs:
                if a == w and b not in seen:
                    seen.add(b)
                    frontier.append(b)
        return any(same(stored, w) for w in seen)

    def rows(self, history: bool = False) -> list[dict]:
        """What is asserted now; with `history`, also what was and has been replaced."""
        cols = ("subject", "relation", "object", "place", "quantity", "turn", "heard")
        where = "" if history else " WHERE superseded IS NULL"
        return [dict(zip(cols, r)) for r in self.db.execute(
            f"SELECT {', '.join(cols)} FROM assertions{where} ORDER BY turn, id")]

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

    # -- learning from taught examples ---------------------------------------

    def chains_to(self, anchors: list[str], answer: str,
                  history: bool = False) -> list[list[dict]]:
        """The shortest chains of assertions, each sharing a filler with the next, that
        touch every anchor and end on one holding the answer. With `history` the chain
        may pass through what has been replaced; a filler is still a hub by how many
        things it is in now, since everyone's past visits to a room are no evidence
        that the room links them."""
        rows = self.rows(history)
        fill = [[r[k] for k in SLOTS if r[k]] for r in rows]
        by_filler: dict[str, list[int]] = {}
        for i, fs in enumerate(fill):
            for f in set(fs):
                by_filler.setdefault(f, []).append(i)
        degree: dict[str, int] = {}
        for r in self.rows() if history else rows:
            for f in {r[k] for k in SLOTS if r[k]}:
                degree[f] = degree.get(f, 0) + 1
        touching = {a: {i for i, fs in enumerate(fill) if any(same(a, f) for f in fs)}
                    for a in anchors}
        anchored = {f for a in anchors for f in by_filler if same(a, f)}
        paths = [(i,) for i in sorted(set().union(*touching.values()))] if anchors else []
        for _ in range(self.depth):
            done = [p for p in paths if all(touching[a] & set(p) for a in anchors)
                    and any(same(answer, f) and not any(same(a, f) for a in anchors)
                            for f in fill[p[-1]])]
            if done:
                return [[rows[i] for i in p] for p in done]
            grown = []
            for p in paths:
                links = [f for f in fill[p[-1]]
                         if f in anchored or degree.get(f, 0) <= self.hub]
                for j in sorted({j for f in links for j in by_filler[f]} - set(p)):
                    grown.append(p + (j,))
            if len(grown) > self.frontier:
                return []
            paths = grown
        return []

    @staticmethod
    def generalise(chain: list[dict], fillers: list[str], answer: str,
                   history: bool = False) -> dict:
        """A chain with the question's fillers made slots, the answer made '?ans', and each
        filler that links two assertions made a variable. Every step keeps its relation
        and which slots its assertion filled, which stands in for the relation where a
        wording was never taught: 'moved to' and 'travelled to' both put a subject in a
        place. A chain through what was replaced
        keeps the order in time of every pair of its steps, which is all 'before' says."""
        lowered = [norm(f) or "" for f in fillers]
        names: dict[str, str] = {}
        steps = []
        for n, r in enumerate(chain):
            step = {"filled": sorted(k for k in SLOTS if r[k] and k != "quantity"),
                    "relations": [r["relation"]]}
            for k in SLOTS:
                v = r[k]
                if not v:
                    continue
                hit = next((i for i, f in enumerate(lowered) if f and same(v, f)), None)
                if hit is not None:
                    step[k] = f"<{hit}>"
                elif n == len(chain) - 1 and same(v, answer):
                    step[k] = "?ans"
                elif any(v in (x[j] for j in SLOTS) for x in chain if x is not r):
                    step[k] = names.setdefault(v, f"?v{len(names)}")
            steps.append(step)
        if not history:
            return {"steps": steps}
        order = ["<" if a["turn"] < b["turn"] else ">" if a["turn"] > b["turn"] else "="
                 for i, a in enumerate(chain) for b in chain[i + 1:]]
        return {"steps": steps, "history": True, "order": order}

    def follow(self, plan: dict, fillers: list[str],
               worded: set[str] | None = None) -> list[tuple[str, tuple[int, int]]]:
        """A taught plan run with this question's fillers: every answer it binds, with the
        latest turn it rests on and then the turn of the step holding the answer, which
        is the order answers are preferred in. Slots are strict: a step's assertion must
        fill exactly the slots the taught one did, and each value sits in its own slot.
        A plan that binds nothing strictly is followed again with each step's
        assertion filling as many slots, a question's filler in any of them, and an
        unknown in its taught slot or else the one slot left over. Only then, because
        a count of slots does not carry the relation: 'got the milk' and 'went to the
        kitchen' both fill two. `worded` holds every step to the relations taught for
        the question's shape, so 'is godparent of' never answers where someone works.
        A plan taught through history runs on it and keeps its steps' order in time."""
        if plan.get("count"):
            return self.count(plan, fillers)
        found = self.followed(plan, fillers, loosely=False, worded=worded)
        if not found:
            found = self.followed(plan, fillers, loosely=True, worded=worded)
        return found

    def followed(self, plan: dict, fillers: list[str], loosely: bool,
                 worded: set[str] | None = None) -> list[tuple[str, tuple[int, int]]]:
        """One pass of `follow`, strict or loose."""
        history = plan.get("history", False)
        rows = self.rows(history)
        out: list[tuple[str, tuple[int, int]]] = []
        turns: list[int] = []

        def ordered() -> bool:
            signs = iter(plan["order"])
            return all(next(signs) == ("<" if a < b else ">" if a > b else "=")
                       for i, a in enumerate(turns) for b in turns[i + 1:])

        def step(i: int, bound: dict, latest: int) -> None:
            if i == len(plan["steps"]):
                if bound.get("?ans") and (not history or ordered()):
                    out.append((bound["?ans"], (latest, turns[-1])))
                return
            s = plan["steps"][i]
            for r in rows:
                if worded is not None and not (r["relation"] and any(
                        same(w, r["relation"]) for w in worded)):
                    continue
                filled = sorted(k for k in SLOTS if r[k] and k != "quantity")
                if loosely:
                    if (len(filled) == len(s["filled"])
                            and (new := self.loosely(s, r, bound, fillers)) is not None):
                        turns.append(r["turn"])
                        step(i + 1, new, max(latest, r["turn"]))
                        turns.pop()
                    continue
                if filled != s["filled"]:
                    continue
                new, ok = dict(bound), True
                for k in SLOTS:
                    token = s.get(k)
                    if token is None:
                        continue
                    if r[k] is None:
                        # the quantity is not among the filled slots, so a step naming
                        # one can meet an assertion without it
                        ok = False
                    elif (m := SLOT.match(token)):
                        idx = int(m.group(1))
                        ok = idx < len(fillers) and same(norm(fillers[idx]) or "", r[k])
                    elif token in new:
                        ok = new[token] == r[k]
                    else:
                        new[token] = r[k]
                    if not ok:
                        break
                if ok:
                    turns.append(r["turn"])
                    step(i + 1, new, max(latest, r["turn"]))
                    turns.pop()

        step(0, {}, -1)
        return out

    @staticmethod
    def loosely(s: dict, r: dict, bound: dict, fillers: list[str]) -> dict | None:
        """One taught step matched against a row without caring which slot the ear chose,
        or None. A quantity stays in its slot, since a number is never a person or a
        place. What is known (a question's filler, a variable already bound) takes the
        slot holding it, its taught slot first; what is not takes its taught slot, or
        the one slot nothing else took, and never a value the plan already names."""
        new = dict(bound)
        free = [k for k in SLOTS if r[k] and k != "quantity"]
        tokens = [(k, t) for k, t in s.items() if k in SLOTS and isinstance(t, str)]

        def value(t: str) -> str | None:
            if (m := SLOT.match(t)):
                idx = int(m.group(1))
                return (norm(fillers[idx]) or "") if idx < len(fillers) else None
            return new.get(t)

        def holds(t: str, v: str) -> bool:
            return same(value(t) or "", v) if SLOT.match(t) else new[t] == v

        if (q := s.get("quantity")) is not None:
            if r["quantity"] is None:
                return None
            if SLOT.match(q) or q in new:
                if value(q) is None or not holds(q, r["quantity"]):
                    return None
            else:
                new[q] = r["quantity"]
        known = [(k, t) for k, t in tokens if k != "quantity" and (SLOT.match(t) or t in new)]
        unknown = [(k, t) for k, t in tokens if k != "quantity" and (k, t) not in known]
        for k, t in known:
            if value(t) is None:
                return None
            hit = next((x for x in [k, *free] if x in free and holds(t, r[x])), None)
            if hit is None:
                return None
            free.remove(hit)
        # an unknown never binds what the plan already names: loosely, 'John got the
        # football' would answer where John went with the football
        named = [v for v in [*(norm(f) or "" for f in fillers), *new.values()] if v]
        for k, t in unknown:
            if t in new:
                # the same unknown in two slots of one step: the second must agree
                hit = next((x for x in free if holds(t, r[x])), None)
                if hit is None:
                    return None
                free.remove(hit)
                continue
            hit = k if k in free else free[0] if len(free) == 1 else None
            if hit is None or any(same(v, r[hit]) for v in named):
                return None
            new[t] = r[hit]
            free.remove(hit)
        return new

    def last_relations(self, anchor: str, at: str, counted: str) -> dict[str, str]:
        """Each value heard in slot `counted` of an assertion with the anchor in slot
        `at`, and the last relation it was heard with."""
        last: dict[str, str] = {}
        for r in self.rows():
            if r[at] and r[counted] and same(anchor, r[at]):
                last[r[counted]] = r["relation"]
        return last

    def count(self, plan: dict, fillers: list[str]) -> list[tuple[str, tuple[int, int]]]:
        """How many values fill the plan's counted slot beside the question's one
        filler in its anchor slot, as a word. A plan that counts holding keeps only
        the values last heard with a relation taught to mean holding: a thing taken and
        then dropped is last heard dropped. A plan that counts names keeps only values
        heard capitalised: a small ear writes things where people should be."""
        anchors = [norm(f) for f in fillers if norm(f)]
        if len(anchors) != 1:
            return []
        last = self.last_relations(anchors[0], plan["anchor"], plan["counted"])
        if plan["holding"]:
            holds = {r: y > n for r, y, n in self.db.execute("SELECT * FROM holds")}
            last = {v: rel for v, rel in last.items() if holds.get(rel)}
        if plan.get("named"):
            last = {v: rel for v, rel in last.items() if self.named(v)}
        n = len(last)
        return [("none" if n == 0 else NUMBERS[n] if n < len(NUMBERS) else str(n), (0, 0))]

    def learn_count(self, shape: str, anchors: list[str], n: int) -> None:
        """A taught answer that is a number no fact holds is a count of something. It is
        first evidence about which relations mean holding, where it is unambiguous: none
        says every last relation lets go, and all of them says every one holds. Then
        every pair of slots, one holding the question's filler and one counted, becomes
        a count plan wherever it counts `n`, with holding and without, and counting
        names only and everything."""
        if len(anchors) != 1:
            return
        pairs = [(at, counted) for at in SLOTS for counted in SLOTS
                 if counted not in (at, "quantity")]
        for at, counted in pairs:
            last = self.last_relations(anchors[0], at, counted)
            if not last or n not in (0, len(last)):
                continue
            column = "yes" if n else "no"
            for relation in set(last.values()):
                self.db.execute("INSERT OR IGNORE INTO holds VALUES (?, 0, 0)", (relation,))
                self.db.execute(f"UPDATE holds SET {column} = {column} + 1 WHERE relation = ?",
                                (relation,))
        for at, counted in pairs:
            if not self.last_relations(anchors[0], at, counted):
                continue
            for holding, named in ((False, False), (True, False), (False, True), (True, True)):
                plan = {"count": True, "anchor": at, "counted": counted, "holding": holding,
                        "named": named}
                if numeral(self.count(plan, anchors)[0][0]) == n:
                    self.db.execute("INSERT OR IGNORE INTO learnt (shape, plan, hits, misses)"
                                    " VALUES (?, ?, 1, 0)",
                                    (shape, json.dumps(plan, sort_keys=True)))

    def taught_answer(self, question: str) -> str | None:
        """The answer of the best taught plan for the question's shape: the one whose
        predictions on later taught examples held most often, if it held more often than
        it failed. A plan that binds nothing says nothing, and the next best is asked."""
        shape, fillers = self.shape(question)
        ranked = self.db.execute(
            "SELECT plan FROM learnt WHERE shape = ? AND hits > misses"
            " ORDER BY hits - misses DESC, hits DESC", (shape,)).fetchall()
        if not ranked:
            return None
        # every plan held to the relations taught for the shape before any is followed
        # without them: a step's own relations are too few, since 'dropped' taught in
        # one plan is what places the football in another
        for worded in (self.worded(shape), None):
            for (plan,) in ranked:
                found = unsaid(self.follow(json.loads(plan), fillers, worded), question)
                if found:
                    return max(found, key=lambda f: f[1])[0]
        return "I don't know."

    def teach(self, question: str, answer: str) -> None:
        """A question told with its answer. Every plan already taught for the question's
        shape is scored on it; then the chains from the question's fillers to the
        answer are generalised and kept as plans, each counted as having held once."""
        shape, fillers = self.shape(question)
        want = norm(answer) or ""
        taught = self.worded(shape)
        for rid, plan in self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                         (shape,)).fetchall():
            found = (unsaid(self.follow(json.loads(plan), fillers, taught), question)
                     or unsaid(self.follow(json.loads(plan), fillers), question))
            if not found:
                # a plan that binds nothing says nothing: the facts it needs were not
                # heard, or not read, which is no evidence that it asks the wrong thing
                continue
            said = max(found, key=lambda f: f[1])[0]
            # a number is the same number in either form: '1' is taught, 'one' is said
            held = bool(said) and (same(norm(said) or "", want) or (
                numeral(norm(said) or "") is not None
                and numeral(norm(said) or "") == numeral(want)))
            column = "hits" if held else "misses"
            self.db.execute(f"UPDATE learnt SET {column} = {column} + 1 WHERE rowid = ?",
                            (rid,))
        anchors = [norm(f) for f in fillers if norm(f)]
        chains, history = self.chains_to(anchors, want), False
        if not chains:
            chains, history = self.chains_to(anchors, want, history=True), True
        n = numeral(want)
        if not chains and n is not None:
            self.learn_count(shape, anchors, n)
        for chain in chains:
            self.keep(shape, self.generalise(chain, fillers, want, history))
        self.db.commit()

    def worded(self, shape: str) -> set[str]:
        """Every relation a plan for the shape was taught through."""
        return {w for (text,) in self.db.execute("SELECT plan FROM learnt WHERE shape = ?",
                                                 (shape,))
                for st in json.loads(text).get("steps", []) for w in st.get("relations", [])}

    def keep(self, shape: str, plan: dict) -> None:
        """A plan kept under its shape. One already kept with the same steps but other
        relations is the same plan heard in other words: its relations gain these, and
        its record of holding and failing stays its own."""
        def bare(p: dict) -> str:
            return json.dumps({**p, "steps": [{k: v for k, v in st.items() if k != "relations"}
                                              for st in p["steps"]]}, sort_keys=True)

        for rid, text in self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                         (shape,)).fetchall():
            kept = json.loads(text)
            if kept.get("count") or bare(kept) != bare(plan):
                continue
            for mine, theirs in zip(kept["steps"], plan["steps"]):
                mine["relations"] = sorted(set(mine.get("relations", []))
                                           | set(theirs["relations"]))
            self.db.execute("UPDATE learnt SET plan = ? WHERE rowid = ?",
                            (json.dumps(kept, sort_keys=True), rid))
            return
        self.db.execute("INSERT OR IGNORE INTO learnt (shape, plan, hits, misses)"
                        " VALUES (?, ?, 1, 0)", (shape, json.dumps(plan, sort_keys=True)))

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
        anchors = (set(query["anchors"]) if "anchors" in query else
                   {norm(v) for s in query["steps"] for k, v in s.items()
                    if k != "relation" and isinstance(v, str) and not is_unknown(v) and norm(v)})
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
        anchored = {f for a in anchors for f in by_filler if same(a, f)}
        for _ in range(self.depth):
            covering = [p for p in paths if answer_of(p[-1])
                        and all(touching[a] & set(p) for a in anchors)]
            done = [p for p in covering if ends(p[-1])]
            if not done and covering and relation:
                # no stored wording is known to mean the goal's: the judge sees the facts
                # these chains end on and says which is of the kind asked, as the step
                # solver's fallback does, or none, and the search goes one link further
                facts = sorted({rows[p[-1]]["heard"] for p in covering})
                choice = self.judge.choose(
                    f"Which of these tells you '{relation}'? If none does, answer -1.", facts)
                if choice is not None:
                    done = [p for p in covering if rows[p[-1]]["heard"] == facts[choice]]
            if done:
                return [({want: answer_of(p[-1])}, max(rows[i]["turn"] for i in p),
                         [rows[i]["heard"] for i in p]) for p in done]
            grown = []
            for p in paths:
                # a link through a thing named in many assertions (a room most people
                # use) joins facts that have nothing else in common, so only a question's
                # own anchor may be passed through when it is that common
                links = [f for f in fill[p[-1]]
                         if f in anchored or len(by_filler[f]) <= self.hub]
                for j in sorted({j for f in links for j in by_filler[f]} - set(p)):
                    grown.append(p + (j,))
            # a filler named in hundreds of assertions makes the frontier explode
            # without making a chain likelier; past the cap the search gives up
            if len(grown) > self.frontier:
                return []
            paths = grown
        return []

    def is_kind(self, filler: str, kind: str, example: str = "") -> bool:
        row = self.db.execute("SELECT yes FROM kinds WHERE filler = ? AND kind = ?",
                              (filler, kind)).fetchone()
        if row is not None:
            return bool(row[0])
        verdict = self.judge.is_a(filler, kind, example)
        self.db.execute("INSERT OR IGNORE INTO kinds VALUES (?, ?, ?)",
                        (filler, kind, int(verdict)))
        self.db.commit()
        return verdict

    def search_kind(self, query: dict) -> list[tuple[dict, int, list[str]]]:
        """The chain found from the question's anchors to a thing of the kind it asks for.

        No relation is matched. The question's names must all be on the chain; its other
        anchors are covered as far as any chain covers them, since a word the ear heard
        in small talk ('now') can be an anchor and touch nothing that matters. Of the
        chains that cover the most anchors, the shortest end on the answers: a filler of
        the asked kind that is not itself an anchor."""
        want = str(query.get("answer", "")).strip()
        kind = query["kind"]
        anchors = set(query.get("anchors") or [])
        names = {n for n in query.get("names", []) if n in anchors}
        rows = self.rows()
        fill = [[r[k] for k in SLOTS if r[k]] for r in rows]
        by_filler: dict[str, list[int]] = {}
        for i, fs in enumerate(fill):
            for f in set(fs):
                by_filler.setdefault(f, []).append(i)
        touching = {a: {i for i, fs in enumerate(fill) if any(same(a, f) for f in fs)}
                    for a in anchors}
        anchored = {f for a in anchors for f in by_filler if same(a, f)}

        def answers(i):
            return [f for f in fill[i] if not any(same(a, f) for a in anchors)
                    and self.is_kind(f, kind, rows[i]["heard"])]

        best, found = (0, 0), []
        paths = [(i,) for i in sorted(set().union(*touching.values()))] if anchors else []
        for depth in range(1, self.depth + 1):
            for p in paths:
                on = set(p)
                if not all(touching[n] & on for n in names):
                    continue
                covered = sum(bool(touching[a] & on) for a in anchors)
                if (covered, -depth) < best:
                    continue
                got = answers(p[-1])
                if not got:
                    continue
                if (covered, -depth) > best:
                    best, found = (covered, -depth), []
                found += [({want: f}, max(rows[i]["turn"] for i in p),
                           [rows[i]["heard"] for i in p]) for f in got]
            grown = []
            for p in paths:
                links = [f for f in fill[p[-1]]
                         if f in anchored or len(by_filler[f]) <= self.hub]
                for j in sorted({j for f in links for j in by_filler[f]} - set(p)):
                    grown.append(p + (j,))
            if len(grown) > self.frontier:
                break
            paths = grown
        return found

    def answer(self, question: Question) -> str:
        if self.taught:
            said = self.taught_answer(question.text)
            if said is not None:
                self.last_notes = ["(taught)"]
                return said
            self.last_notes = ["(untaught)"]
            return "I don't know."
        query = self.query(question.text)
        self.last_notes = [json.dumps(query)] if query else ["(unreadable)"]
        if not query:
            return "I don't know."
        want = str(query.get("answer", "")).strip()
        found = [(b, t, h) for b, t, h in
                 (self.search_kind(query) if self.searched and query.get("kind") else
                  self.search(query) if self.searched else self.solve(query)) if b.get(want)]
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
