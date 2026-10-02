"""The graphed arm: the conversation kept as its parse, and plans learnt as paths in it.

Every sentence is parsed, and nothing maps the parse to slots. A clause is an event: its
verb's lemma, the turn it was heard, and an edge to each of its arguments labelled with
the grammatical link (nsubj, dobj, attr, prep:in, prep:to, ...). A noun with dependents of
its own ("Trapairk's godparent", "45 glass jars") is an event too, joined by `self` to the
noun's name. Every other noun, number or adjective is a name, one node however often it is
said, which is what joins one sentence to the next.

A question told with its answer is a lesson. Its names are the noun phrases the graph
already holds; the shortest path from the first name to the answer, with where each other
name hangs off it, is kept as a plan under the question's shape, holding the lemmas met at
each event. Plans are scored on every later lesson of their shape and only those that held
more often than they failed are followed. An answer is the end of a followed path; of
several, the one resting on the latest event, which is how a move or a correction wins.
No path, no answer: "I don't know."
"""

from __future__ import annotations

import hashlib
import json
import re
import sqlite3
from collections import deque
from pathlib import Path

SCHEMA = """
CREATE TABLE IF NOT EXISTS events (id INTEGER PRIMARY KEY, turn INTEGER NOT NULL,
    lemma TEXT NOT NULL, heard TEXT NOT NULL, mood TEXT NOT NULL DEFAULT '');
CREATE TABLE IF NOT EXISTS edges (event INTEGER NOT NULL, label TEXT NOT NULL,
    node TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS edges_event ON edges (event);
CREATE INDEX IF NOT EXISTS edges_node ON edges (node);
CREATE TABLE IF NOT EXISTS learnt (shape TEXT NOT NULL, plan TEXT NOT NULL,
    hits INTEGER NOT NULL, misses INTEGER NOT NULL, PRIMARY KEY (shape, plan));
CREATE TABLE IF NOT EXISTS positions (template TEXT NOT NULL, pos INTEGER NOT NULL,
    filler TEXT NOT NULL, n INTEGER NOT NULL, PRIMARY KEY (template, pos, filler));
"""

# every extraction kept across runs: the parser is deterministic, and the version is in
# the key so a change to what is extracted re-reads every sentence
CACHE = Path(__file__).resolve().parents[2] / "state" / "parses.sqlite"
VERSION = "graph-6"

# a clause's links that are not arguments
SKIP = {"punct", "det", "aux", "auxpass", "cc", "mark", "neg", "intj", "case", "dep",
        "advmod", "prt", "predet", "preconj", "expl", "attr_of"}
PRONOUNS = {"it", "they", "them", "its", "their", "he", "she", "him", "her", "his"}

_NLP: dict = {}


def nlp(model: str):
    if model not in _NLP:
        import spacy

        _NLP[model] = spacy.load(model)
    return _NLP[model]


def phrase(tok) -> str:
    """A noun's name: its compounds and adjectives with it, no determiner or number."""
    keep = [t for t in tok.children if t.dep_ in ("compound", "amod") and t.i < tok.i] + [tok]
    return " ".join(t.text for t in sorted(keep, key=lambda t: t.i)).lower()


def extract(doc) -> list[dict]:
    """A sentence as events: [{"lemma", "edges": [[label, target], ...]}], a target being
    "n:<name>" or "e:<index into this list>". "it" and "they" are the sentence's first
    subject that is not a pronoun."""
    first = next((t for t in doc if t.dep_ in ("nsubj", "nsubjpass") and t.pos_ != "PRON"
                  and t.lower_ not in PRONOUNS), None)
    events: list[dict] = []
    index: dict[int, int] = {}

    def heads(tok) -> bool:
        # by structure, not by tag: 'Dethol shoes horses' is tagged a noun and has a
        # subject and an object, and '22 years old' hangs a number off an adjective
        # a preposition's object is its head's argument, never an event of its own
        return tok.dep_ not in ("prep", "dative", "agent") and any(
            c.dep_ not in SKIP and c.dep_ not in ("compound", "amod", "conj")
            for c in tok.children)

    for tok in doc:
        if heads(tok):
            index[tok.i] = len(events)
            # what did not happen, and what only might or will, is a different event from
            # what did: 'might move' and 'not keep' are lemmas of their own, and plans
            # learn from lessons which ones they follow
            modal = [c.lemma_.lower() for c in tok.children if c.dep_ == "aux" and c.tag_ == "MD"]
            neg = ["not"] if any(c.dep_ == "neg" for c in tok.children) else []
            lemma = " ".join(modal + neg + [tok.lemma_.lower()] + [
                c.lower_ for c in tok.children if c.dep_ == "prt"])
            events.append({"lemma": lemma, "mood": " ".join(modal + neg), "edges": []})

    def target(tok) -> str | None:
        if tok.i in index:
            return f"e:{index[tok.i]}"
        if tok.lower_ in PRONOUNS:
            return f"n:{phrase(first)}" if first is not None else None
        if tok.pos_ in ("NOUN", "PROPN", "NUM", "ADJ") or tok.like_num:
            return f"n:{phrase(tok)}"
        return None

    for tok in doc:
        if tok.i not in index:
            continue
        ev = events[index[tok.i]]
        if tok.pos_ in ("NOUN", "PROPN", "ADJ") and not any(
                c.dep_.startswith("nsubj") for c in tok.children):
            ev["edges"].append(["self", f"n:{phrase(tok)}"])
        for c in tok.children:
            if c.dep_ in ("prep", "dative", "agent"):
                for p in c.children:
                    if p.dep_ == "pobj" and (t := target(p)):
                        ev["edges"].append([f"prep:{c.lower_}", t])
                        for cc in p.children:
                            if cc.dep_ == "conj" and (t2 := target(cc)):
                                ev["edges"].append([f"prep:{c.lower_}", t2])
                if c.dep_ == "dative" and not any(p.dep_ == "pobj" for p in c.children):
                    if t := target(c):
                        ev["edges"].append(["dative", t])
                continue
            if c.dep_ in SKIP or c.dep_ == "compound" or c.dep_ == "amod":
                # an adverb is not an argument, but a particle's object is reached through
                # its own preposition, as in 'went back to the bathroom'
                if c.dep_ == "advmod":
                    for p in c.children:
                        if p.dep_ == "prep":
                            for o in p.children:
                                if o.dep_ == "pobj" and (t := target(o)):
                                    ev["edges"].append([f"prep:{p.lower_}", t])
                continue
            if t := target(c):
                ev["edges"].append([c.dep_, t])
                for cc in c.children:
                    if cc.dep_ == "conj" and (t2 := target(cc)):
                        ev["edges"].append([c.dep_, t2])
        # a conjoined verb shares its head's subject: 'picked up the milk and went'
        if tok.dep_ == "conj" and tok.head.i in index and not any(
                e[0].startswith("nsubj") for e in ev["edges"]):
            for label, t in events[index[tok.head.i]]["edges"]:
                if label.startswith("nsubj"):
                    ev["edges"].append([label, t])
    # an event left with no edges goes, and every reference to the rest is renumbered
    kept = [i for i, e in enumerate(events) if e["edges"]]
    renumber = {f"e:{old}": f"e:{new}" for new, old in enumerate(kept)}
    out = []
    for i in kept:
        edges = [[label, renumber.get(t, t)] for label, t in events[i]["edges"]
                 if not t.startswith("e:") or t in renumber]
        out.append({"lemma": events[i]["lemma"], "mood": events[i]["mood"], "edges": edges})
    return out


class GraphArm:
    name = "graphed"

    def __init__(self, directory: Path, model: str = "en_core_web_trf",
                 known_learnt: list | None = None, known_positions: list | None = None,
                 cache: Path | None = CACHE) -> None:
        directory.mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(str(directory / "graph.db"))
        self.db.executescript(SCHEMA)
        self.db.executemany("INSERT OR IGNORE INTO learnt VALUES (?, ?, ?, ?)",
                            [tuple(x) for x in (known_learnt or [])])
        self.db.executemany("INSERT OR IGNORE INTO positions VALUES (?, ?, ?, ?)",
                            [tuple(x) for x in (known_positions or [])])
        self.db.commit()
        self.model = model
        self.cache = cache
        self.last_notes: list[str] = []
        self.parsed = 0
        # the pairs of events already counted towards a plan's order, this world
        self.ordered: set = set()
        # a question this arm answered, waiting for the turn that reacts to it
        self.pending: tuple[str, str] | None = None
        # events already found replaced by a later one, cleared whenever one is heard
        self._replaced: dict = {}
        # taught shapes' signatures, rebuilt after a lesson; questions' parses
        self._sigs: list | None = None
        self._tokens: dict = {}

    # -- reading ---------------------------------------------------------------

    def _kept(self, key: str):
        if self.cache is None:
            return None
        if not hasattr(self, "_cdb"):
            self.cache.parent.mkdir(parents=True, exist_ok=True)
            self._cdb = sqlite3.connect(str(self.cache))
            self._cdb.execute("CREATE TABLE IF NOT EXISTS parses (key TEXT PRIMARY KEY, "
                              "value TEXT NOT NULL)")
        row = self._cdb.execute("SELECT value FROM parses WHERE key = ?", (key,)).fetchone()
        return json.loads(row[0]) if row else None

    def _keep(self, key: str, value) -> None:
        if self.cache is not None:
            self._cdb.execute("INSERT OR REPLACE INTO parses VALUES (?, ?)",
                              (key, json.dumps(value)))
            self._cdb.commit()

    def read(self, text: str) -> list[dict]:
        key = hashlib.sha256(f"{VERSION}|{self.model}|events|{text}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            kept = extract(nlp(self.model)(text))
            self.parsed += 1
            self._keep(key, kept)
        return kept

    def names_in(self, question: str) -> list[tuple[int, int, str]]:
        """The question's noun phrases, as spans with their names."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|names|{question}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            doc = nlp(self.model)(question)
            kept = []
            for tok in doc:
                if tok.pos_ in ("NOUN", "PROPN") and tok.dep_ != "compound":
                    left = [t for t in tok.children if t.dep_ in ("compound", "amod")
                            and t.i < tok.i]
                    start = min([t.idx for t in left] + [tok.idx])
                    verb = tok.head if tok.dep_ == "dobj" else None
                    between = (doc[verb.i + 1:min([t.i for t in left] + [tok.i])]
                               if verb is not None and verb.i < tok.i else None)
                    governs = ([verb.idx, verb.lemma_.lower()] if between is not None
                               and all(t.dep_ == "det" for t in between) else [None, None])
                    kept.append([start, tok.idx + len(tok.text), phrase(tok), *governs])
            self._keep(key, kept)
        return [tuple(k) for k in kept]

    def hear(self, turn: int, text: str) -> None:
        self._replaced = {}
        events = self.read(text)
        ids = []
        for ev in events:
            cur = self.db.execute("INSERT INTO events (turn, lemma, heard, mood) VALUES "
                                  "(?, ?, ?, ?)", (turn, ev["lemma"], text, ev["mood"]))
            ids.append(cur.lastrowid)
        for ev, eid in zip(events, ids):
            for label, t in ev["edges"]:
                node = f"e:{ids[int(t[2:])]}" if t.startswith("e:") else t
                self.db.execute("INSERT INTO edges VALUES (?, ?, ?)", (eid, label, node))
        self.db.commit()

    # -- the graph -------------------------------------------------------------

    def holding(self, word: str) -> list[str]:
        """The names that hold a word whole: 'ochre colour' for 'ochre'."""
        return [n for (n,) in self.db.execute(
            "SELECT DISTINCT node FROM edges WHERE node LIKE ?", (f"n:%{word}%",))
            if said_in(word, n[2:])]

    def known(self, name: str) -> bool:
        return self.db.execute("SELECT 1 FROM edges WHERE node = ? LIMIT 1",
                               (f"n:{name}",)).fetchone() is not None

    def around(self, node: str) -> list[tuple[str, int, str]]:
        """Every step from a node: (label, direction, next node). Direction 1 goes from an
        event to its argument, -1 back from an argument to its event."""
        if node.startswith("e:"):
            eid = int(node[2:])
            out = [(label, 1, n) for label, n in self.db.execute(
                "SELECT label, node FROM edges WHERE event = ?", (eid,))]
        else:
            out = []
        out += [(label, -1, f"e:{e}") for e, label in self.db.execute(
            "SELECT event, label FROM edges WHERE node = ?", (node,))]
        return out

    def event(self, node: str) -> tuple[str, int]:
        lemma, turn = self.db.execute("SELECT lemma, turn FROM events WHERE id = ?",
                                      (int(node[2:]),)).fetchone()
        return lemma, turn

    def replaced(self, node: str) -> int:
        """Whether a later event that happened has exactly this one's arguments, the
        prepositions aside: 'Mary dropped the football' replaces 'Mary picked up the
        football', 'Mary went to the hallway' replaces 'Mary went to the kitchen', and
        'Mary picked up the football' replaces neither, its arguments being others. The
        turn of the latest that replaces it, or 0."""
        if node not in self._replaced:
            eid = int(node[2:])

            def args(e: int) -> frozenset:
                return frozenset(self.db.execute(
                    "SELECT label, node FROM edges WHERE event = ? AND label NOT LIKE "
                    "'prep:%'", (e,)).fetchall())

            mine = args(eid)
            turn = self.db.execute("SELECT turn FROM events WHERE id = ?", (eid,)).fetchone()[0]
            later: dict = {}
            if mine:
                label, n = next(iter(mine))
                later = dict(self.db.execute(
                    "SELECT edges.event, events.turn FROM edges JOIN events ON events.id = "
                    "edges.event WHERE edges.label = ? AND edges.node = ? AND events.turn > ?"
                    " AND events.mood = ''", (label, n, turn)).fetchall())
            self._replaced[node] = max((t for e, t in later.items() if args(e) == mine),
                                       default=0)
        return self._replaced[node]

    def mood(self, node: str) -> str:
        return self.db.execute("SELECT mood FROM events WHERE id = ?",
                               (int(node[2:]),)).fetchone()[0]

    def paths(self, start: str, goal: str, limit: int = 8,
              avoid: set | None = None) -> list[list]:
        """The shortest paths from one node to another, each a list of nodes and steps
        alternating, at most `limit` steps."""
        found, frontier, best = [], deque([[start]]), None
        seen = {start: 0}
        while frontier:
            path = frontier.popleft()
            steps = (len(path) - 1) // 2
            if best is not None and steps >= best:
                continue
            if steps >= limit:
                continue
            for label, direction, nxt in self.around(path[-1]):
                if nxt in path[::2] or (avoid and nxt in avoid and nxt != goal):
                    continue
                if nxt == goal:
                    best = steps + 1
                    found.append(path + [(label, direction), nxt])
                    continue
                if seen.get(nxt, 99) < steps + 1:
                    continue
                seen[nxt] = steps + 1
                frontier.append(path + [(label, direction), nxt])
        return found

    # -- plans -----------------------------------------------------------------

    def template(self, question: str) -> tuple[str, list]:
        """The question with every noun phrase cut out, and the phrases. A phrase is
        cut to the longest ending of it the graph knows: 'how many walking sticks' holds
        'walking sticks', with no list of words like 'many'."""
        spans = []
        for a, b, name, verb_at, verb in sorted(self.names_in(question)):
            words = name.split()
            for i in range(len(words)):
                tail = " ".join(words[i:])
                if self.known(tail):
                    at = question.lower().find(tail, a)
                    if at >= 0:
                        a, b, name = at, at + len(tail), tail
                    break
            # 'the person who repairs clocks': a name's own verb, said just before it, is
            # cut with it, as the taught arm's `joined` does, so every trade is one shape
            if verb and self.db.execute(
                    "SELECT 1 FROM edges JOIN events ON events.id = edges.event WHERE "
                    "edges.node = ? AND events.lemma = ? LIMIT 1", (f"n:{name}", verb)
            ).fetchone():
                a = verb_at
            spans.append((a, b, name))
        out, at = "", 0
        for i, (a, b, _) in enumerate(spans):
            out += question[at:a] + f"<{i}>"
            at = b
        return out + question[at:], spans

    def heard_at(self, question: str) -> None:
        """A lesson's noun phrases counted by their place in its template."""
        template, spans = self.template(question)
        for i, (_, _, n) in enumerate(spans):
            self.db.execute("INSERT INTO positions VALUES (?, ?, ?, 1) ON CONFLICT"
                            "(template, pos, filler) DO UPDATE SET n = n + 1",
                            (template, i, n))

    def slot(self, template: str, pos: int, name: str, capital: bool) -> bool:
        """Whether a noun phrase is a slot of its question or a word of its frame. A
        place whose word varies across lessons of one template is a slot; one that held
        the same word every time, twice or more, is frame ('Whose cousin is X?', 'do
        for a living'); one never taught is a slot if the graph knows the name."""
        seen = self.db.execute("SELECT filler, n FROM positions WHERE template = ? AND"
                               " pos = ?", (template, pos)).fetchall()
        if len(seen) == 1 and seen[0][1] >= 2:
            return False
        # a place that varies holds a slot only where its word names something here:
        # 'colour' and 'shade' share a place across lessons and name nothing
        return self.known(name) or capital

    def shape(self, question: str) -> tuple[str, list[str]]:
        template, every = self.template(question)
        spans = [(a, b, n) for i, (a, b, n) in enumerate(every)
                 if self.slot(template, i, n, question[a:a + 1].isupper() and a > 0)]
        shape, fillers, at = "", [], 0
        for a, b, n in sorted(spans):
            shape += question[at:a] + f"<{len(fillers)}>"
            fillers.append(n)
            at = b
        return shape + question[at:], fillers

    def plan_of(self, path: list, others: list[str]) -> dict | None:
        """A path as a plan: its steps, the lemma at each event on it, and where each other
        name of the question hangs off an event of it."""
        steps = [list(s) for s in path[1::2]]
        nodes = path[::2]
        lemmas, moods, attach, order = {}, {}, [], {}
        events = [(i, node, *self.event(node)) for i, node in enumerate(nodes)
                  if node.startswith("e:")]
        evidence = {}
        for i, node, lemma, _ in events:
            lemmas[str(i)] = [lemma]
            moods[str(i)] = [self.mood(node)]
        # whether each event came before or after the one before it on the path, by that
        # one's lemma: after 'got' the answer's move came later, after 'put down' earlier.
        # The two events are kept beside it, so a pair seen again is not counted again
        for (i, node_i, lemma_i, turn_i), (j, node_j, _, turn_j) in zip(events, events[1:]):
            order[f"{i}-{j}"] = {lemma_i: [int(turn_j < turn_i), int(turn_j >= turn_i)]}
            evidence[f"{i}-{j}"] = f"{node_i}>{node_j}"
        for k, name in enumerate(others):
            # the shortest way from any node of the path to the other name: 'I counted 35
            # walking sticks in the cellar' has the cellar on the counting, a step off the
            # path from the sticks to their number
            hook = None
            for i, node in enumerate(nodes):
                for way in self.paths(node, f"n:{name}", limit=3, avoid=set(nodes)):
                    if hook is None or len(way) < len(hook[2]) * 2 + 1:
                        hook = [k + 1, i, [list(st) for st in way[1::2]]]
            if hook is None:
                return None
            attach.append(hook)
        return {"steps": steps, "lemmas": lemmas, "moods": moods, "attach": attach,
                "order": order, "evidence": evidence}

    def reaches(self, node: str, steps: list, goal: str) -> bool:
        """Whether a way of these steps leads from a node to a goal."""
        here = [node]
        for label, direction in steps:
            here = [n for h in here for lab, d, n in self.around(h)
                    if lab == label and d == direction][:200]
        return goal in here

    @staticmethod
    def key(plan: dict) -> str:
        return json.dumps({"steps": plan["steps"], "attach": plan["attach"],
                           "count": plan.get("count", False)})

    def walks(self, start: str, depth: int = 4, cap: int = 5000) -> dict:
        """Every way of up to `depth` steps from a node to a name, grouped by its steps:
        {steps: (ends, one path)}."""
        out: dict = {}
        frontier = [[start]]
        for _ in range(depth):
            nxt_frontier = []
            for path in frontier:
                for label, direction, nxt in self.around(path[-1]):
                    if nxt in path[::2]:
                        continue
                    way = path + [(label, direction), nxt]
                    if nxt.startswith("n:"):
                        key = json.dumps([list(s) for s in way[1::2]])
                        ends, rep = out.get(key, (set(), way))
                        ends.add(nxt)
                        out[key] = (ends, rep)
                    nxt_frontier.append(way)
            frontier = nxt_frontier[:cap]
        return out

    def counted(self, fillers: list[str], want: int) -> list[dict]:
        """Plans that count: from the question's first name, every way whose distinct
        ends number what was taught, shortest first."""
        found = []
        for key, (ends, rep) in self.walks(f"n:{fillers[0]}").items():
            if len(ends) == want:
                plan = self.plan_of(rep, fillers[1:])
                if plan is not None:
                    found.append({**plan, "count": True})
        found.sort(key=lambda p: len(p["steps"]))
        return [p for p in found if len(p["steps"]) == len(found[0]["steps"])] if found else []

    def follow(self, plan: dict, fillers: list[str], strict: bool,
               present: bool | None = None) -> list[tuple[str, int]]:
        """Every end the plan reaches from the question's first name, with the latest turn
        of the events it passed. A plan that asks about the present passes no event a later
        one replaced; whether it does is learnt from its lessons, since 'where was it
        before' needs exactly those."""
        if not fillers:
            return []
        if present is None:
            now, then = plan.get("present", (0, 0))
            present = now > then
        out = []
        def hooked(node: str, pos: int) -> bool:
            return all(self.reaches(node, h[2], f"n:{fillers[h[0]]}")
                       for h in plan["attach"] if h[1] == pos and h[0] < len(fillers))

        def ordered(last, j: int, turn_j: int) -> bool:
            if last is None:
                return True
            i, lemma_i, turn_i = last
            before, after = plan.get("order", {}).get(f"{i}-{j}", {}).get(lemma_i, (0, 0))
            if before >= 3 and before > 2 * after:
                return turn_j <= turn_i
            if after >= 3 and after > 2 * before:
                return turn_j >= turn_i
            return True

        self.emptied = 0
        start = f"n:{fillers[0]}"
        # a walk: its nodes, the turns of its events in order, and its last event
        walks = [([start], (), None)] if hooked(start, 0) else []
        for i, (label, direction) in enumerate(plan["steps"]):
            nxt_walks = []
            for nodes, turns, last in walks:
                here = nodes[-1]
                for lab, d, nxt in self.around(here):
                    if lab != label or d != direction or nxt in nodes:
                        continue
                    t, now = turns, last
                    if nxt.startswith("e:"):
                        lemma, et = self.event(nxt)
                        pos = str(i + 1)
                        if strict and lemma not in plan["lemmas"].get(pos, [lemma]):
                            continue
                        # loosely, any verb will do, but never one that did not happen
                        # where the lessons' did, or the other way round
                        mood = self.mood(nxt)
                        if mood not in plan.get("moods", {}).get(pos, [mood]):
                            continue
                        if not ordered(last, i + 1, et):
                            continue
                        if present and (by := self.replaced(nxt)):
                            self.emptied = max(self.emptied, by)
                            continue
                        # what did not happen, or only might, changed nothing, so it is
                        # walked through and never makes an answer the latest
                        t, now = turns + ((0 if mood else et),), (i + 1, lemma, et)
                    if not hooked(nxt, i + 1):
                        continue
                    nxt_walks.append((nodes + [nxt], t, now))
            walks = nxt_walks[:2000]
        for nodes, turns, _ in walks:
            end = nodes[-1]
            if end.startswith("n:"):
                # the most recent thing that happened to the first name first, then what
                # followed from it: the football's last event, then its carrier's move
                out.append((end[2:], turns))
        return out

    def signature(self, question: str, fillers: list[str]) -> list[str]:
        """A question's parse as a set of (word, link, head) triples and words, each name
        replaced by its slot and each wh-word kept as itself, so two wordings of one
        question share what their grammar shares: 'Where are <0>'s <1> kept?' and 'Where
        does <0> keep the <1>?' share 'keep', 'where' and 'where advmod keep'."""
        tokens = self.tokens(question)
        heads = {f.split()[-1]: i for i, f in enumerate(fillers)}
        inside = {w for f in fillers for w in f.split()[:-1]}

        def label(t) -> str | None:
            lower, lemma, tag, dep, _ = t
            if lower in heads:
                return f"<{heads[lower]}>"
            if lower in inside or dep in ("punct", "det", "aux", "case"):
                return None
            return lower if tag in ("WDT", "WP", "WP$", "WRB") else lemma

        out = set()
        for i, t in enumerate(tokens):
            me = label(t)
            if me is None:
                continue
            out.add(me)
            head = label(tokens[t[4]]) if t[4] != i else "ROOT"
            if head is not None:
                out.add(f"{me} {t[3]} {head}")
        return sorted(out)

    def tokens(self, question: str) -> list:
        """A question's parse, one row a token: its word, lemma, tag, link and head."""
        if question not in self._tokens:
            key = hashlib.sha256(f"{VERSION}|{self.model}|tokens|{question}".encode()
                                 ).hexdigest()
            kept = self._kept(key)
            if kept is None:
                kept = [[t.lower_, t.lemma_.lower(), t.tag_, t.dep_, t.head.i]
                        for t in nlp(self.model)(question)]
                self._keep(key, kept)
            self._tokens[question] = kept
        return self._tokens[question]

    def nearest(self, question: str, fillers: list[str],
                skip: str | None = None) -> tuple[float, list[str]]:
        """The taught shapes nearest a question no lesson was worded as: those of its
        number of slots whose signature overlaps the question's most, and by how much."""
        mine = set(self.signature(question, fillers))
        if self._sigs is None:
            self._sigs = []
            for shape, plan in self.db.execute(
                    "SELECT shape, plan FROM learnt WHERE hits > misses").fetchall():
                sig = json.loads(plan).get("sig")
                if sig:
                    self._sigs.append((shape, set(sig)))
        best, shapes = 0.0, []
        for shape, sig in self._sigs:
            if shape.count("<") != len(fillers) or shape == skip:
                continue
            near = len(mine & sig) / len(mine | sig)
            if near > best:
                best, shapes = near, [shape]
            elif near == best and shape not in shapes:
                shapes.append(shape)
        return best, shapes

    def borrowed(self, question: str,
                 skip: str | None = None) -> list[tuple[list[str], list[str]]]:
        """A wording no lesson used has no history saying which of its names are slots
        and which are frame ('cousin' in 'Which person is X's cousin?'), nor in what order
        its slots run. Every reading is scored, each name a slot or frame and the slots in
        any order, nearest a taught shape first."""
        from itertools import combinations, permutations

        names = [n for _, _, n in self.template(question)[1]][:4]
        readings = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                for order in permutations(chosen):
                    near, shapes = self.nearest(question, list(order), skip)
                    if shapes:
                        readings.append((near, list(order), shapes))
        readings.sort(key=lambda r: -r[0])
        return [(f, sh) for _, f, sh in readings]

    def answers(self, shape: str, fillers: list[str], question: str,
                borrow: bool = True) -> list[tuple[str, int]]:
        ranked = self.db.execute(
            "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
            "ORDER BY hits - misses DESC, hits DESC", (shape,)).fetchall()
        if ranked:
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        if not borrow or not all(self.known(f) for f in fillers):
            # of a name the conversation never used nothing can be known, so its
            # question is not borrowed for
            return []
        # a wording no lesson used, or one whose plans reach nothing here, borrows the
        # plans of the nearest other taught shape under the nearest reading of it that
        # reaches anything in the graph: 'Who is X's cousin?' learnt one direction, and
        # 'Whose cousin is X?' holds the other
        for fillers, nearest in self.borrowed(question, shape)[:12]:
            ranked = [r for near in nearest for r in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
                "ORDER BY hits - misses DESC, hits DESC", (near,)).fetchall()]
            found = self.followed(ranked, fillers, question)
            if found:
                return found
        return []

    def followed(self, ranked: list, fillers: list[str], question: str) -> list:
        for strict in (True, False):
            found = []
            for (plan,) in ranked:
                plan = json.loads(plan)
                said = self.said(plan, fillers, strict, question)
                if said and plan.get("count"):
                    # a count is of a set, not of one latest event, so the plan that held
                    # most often answers it rather than whichever passed the latest turn
                    return said
                found += said
            if found:
                return found
        return []

    @staticmethod
    def visits(ends: list, name: str) -> tuple[list, int | None]:
        """A plan's ends as visits in turn order, and where the latest visit to a name
        sits among them: the apple's rooms, and the bathroom's place in that list."""
        seen = sorted({(t[-1] if t else 0, e) for e, t in ends})
        at = max((i for i, (_, e) in enumerate(seen) if e == name), default=None)
        return seen, at

    def relative(self, plan: dict, ends: list, fillers: list[str]) -> list | None:
        """Where lessons showed the answer one visit before or after another name of
        the question ('where was it before the bathroom'), that visit."""
        for k, (before, after) in plan.get("relative", {}).items():
            k = int(k)
            if k >= len(fillers):
                continue
            seen, at = self.visits(ends, fillers[k])
            if at is None:
                continue
            step = -1 if before >= 3 and before > 2 * after else (
                1 if after >= 3 and after > 2 * before else 0)
            if step and 0 <= at + step < len(seen):
                t, e = seen[at + step]
                return [(e, (t,))]
        return None

    def said(self, plan: dict, fillers: list[str], strict: bool,
             question: str, present: bool | None = None) -> list[tuple[str, int]]:
        ends = self.follow(plan, fillers, strict, present)
        chosen = self.relative(plan, ends, fillers)
        if chosen is not None:
            return chosen
        if plan.get("count"):
            if ends:
                return [(str(len({e for e, _ in ends})), max(t for _, t in ends))]
            # nothing left to count is an answer, resting on what emptied the set: 'Mary
            # dropped the football' is later than her picking it up
            return [("none", (self.emptied,))] if self.emptied else []
        return [f for f in ends if not said_in(f[0], question)]

    def teach(self, question: str, answer: str) -> None:
        self._sigs = None
        self.heard_at(question)
        shape, fillers = self.shape(question)
        want = answer.lower()
        for rowid, plan in self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                           (shape,)).fetchall():
            plan = json.loads(plan)

            def holds(present: bool | None) -> bool | None:
                found = (self.said(plan, fillers, True, question, present)
                         or self.said(plan, fillers, False, question, present))
                if not found:
                    return None
                said = max(found, key=lambda f: f[1])[0]
                return said_in(want, said) or (numeral(want) is not None
                                               and numeral(said) == numeral(want))

            # whether the answer is the visit just before or after another name of the
            # question, counted wherever that name is among the plan's ends
            ends = self.follow(plan, fillers, False)
            for k in range(1, len(fillers)):
                seen, at = self.visits(ends, fillers[k])
                if at is None:
                    continue
                into = plan.setdefault("relative", {}).setdefault(str(k), [0, 0])
                if at > 0 and said_in(want, seen[at - 1][1]):
                    into[0] += 1
                if at + 1 < len(seen) and said_in(want, seen[at + 1][1]):
                    into[1] += 1
                self.db.execute("UPDATE learnt SET plan = ? WHERE rowid = ?",
                                (json.dumps(plan), rowid))
            # whether the plan asks about the present, counted on every lesson where
            # reading only what is still so and reading everything differ
            # right over no answer over wrong: reading the present and finding nothing
            # beats reading history and naming a room no longer so
            def worth(h: bool | None) -> int:
                return 0 if h is None else (1 if h else -1)

            now, then = worth(holds(True)), worth(holds(False))
            if now != then:
                tally = plan.get("present", [0, 0])
                tally[0 if now > then else 1] += 1
                plan["present"] = tally
                self.db.execute("UPDATE learnt SET plan = ? WHERE rowid = ?",
                                (json.dumps(plan), rowid))
            held = holds(None)
            if held is None:
                continue
            column = "hits" if held else "misses"
            self.db.execute(f"UPDATE learnt SET {column} = {column} + 1 WHERE rowid = ?",
                            (rowid,))
        goals = self.holding(want)
        if not fillers:
            self.db.commit()
            return
        found = [p for g in goals[:5] for p in self.paths(f"n:{fillers[0]}", g)]
        shortest = min((len(p) for p in found), default=0)
        plans = [self.plan_of(p, fillers[1:]) for p in found if len(p) == shortest][:20]
        if not any(plans) and numeral(want) is not None:
            # a number no telling said is a number of things: 'How many people keep
            # things in the pantry?' taught 3
            plans = self.counted(fillers, numeral(want))
        for plan in plans:
            if plan is None:
                continue
            k = self.key(plan)
            row = self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                  (shape,)).fetchall()
            same = next(((r, json.loads(p)) for r, p in row if self.key(json.loads(p)) == k),
                        None)
            if same:
                rid, kept = same
                for pos, lemmas in plan["lemmas"].items():
                    kept["lemmas"][pos] = sorted(set(kept["lemmas"].get(pos, [])) | set(lemmas))
                for pos, moods in plan["moods"].items():
                    into = kept.setdefault("moods", {})
                    into[pos] = sorted(set(into.get(pos, [])) | set(moods))
                for pair, by_lemma in plan.get("order", {}).items():
                    # one pair of events is one piece of evidence, however often a lesson
                    # asks about it: a house asks of one fact several times
                    seen = (shape, k, pair, plan["evidence"].get(pair))
                    if seen in self.ordered:
                        continue
                    self.ordered.add(seen)
                    into = kept.setdefault("order", {}).setdefault(pair, {})
                    for lemma, (b, a) in by_lemma.items():
                        was = into.get(lemma, [0, 0])
                        into[lemma] = [was[0] + b, was[1] + a]
                self.db.execute("UPDATE learnt SET plan = ?, hits = hits + 1 WHERE rowid = ?",
                                (json.dumps(kept), rid))
            else:
                for pair, ev in plan["evidence"].items():
                    self.ordered.add((shape, k, pair, ev))
                stored = {key: v for key, v in plan.items() if key != "evidence"}
                stored["sig"] = self.signature(question, fillers)
                self.db.execute("INSERT OR IGNORE INTO learnt VALUES (?, ?, 1, 0)",
                                (shape, json.dumps(stored)))
        self.db.commit()

    def answer(self, question) -> str:
        said = self.answered(question.text)
        self.last_notes = ["(nothing)"] if said is None else ["(found)"]
        return "I don't know." if said is None else said

    def answered(self, text: str, depth: int = 0) -> str | None:
        """An answer from the plans of the question's shape, or, where they find nothing,
        from the question taken apart: its innermost noun phrase holding a name is asked
        as a question of its own ('Who is Kroudroth's cousin?'), its answer put in its
        place, and what is left asked again. Plans are whole paths and do not compose; a
        question's grammar does."""
        shape, fillers = self.shape(text)
        found = self.answers(shape, fillers, text, borrow=False)
        if found:
            return max(found, key=lambda f: f[1])[0]
        if depth < 3 and (said := self.joined(text, depth)) is not None:
            return said
        found = self.answers(shape, fillers, text)
        if found:
            return max(found, key=lambda f: f[1])[0]
        # borrowed before taken apart, by the first house: taking 'X's lanterns' apart
        # first read 0.756 0.784 0.779 against 0.760 0.788 0.809
        if depth >= 3 or not all(self.known(f) for f in fillers):
            return None
        return self.apart(text, depth)

    def joined(self, text: str, depth: int) -> str | None:
        """A question holding a clause about something ('the things X keeps in the
        cellar') answered as a join: the clause is a relation some taught shape holds,
        solved for the thing with the clause's names bound, and the thing put in the
        clause's place. A shape's plans are its relation's disjuncts, one a wording it was
        taught in, so the clause is found however the fact was told."""
        for a, b in self.inner(text):
            names = [n for _, _, n in self.template(text[a:b])[1] if self.known(n)]
            if not names:
                continue
            for thing in self.related(text[a:b], names):
                said = self.answered(text[:a] + thing.title() + text[b:], depth + 1)
                if said is not None:
                    return said
        return None

    def related(self, phrase: str, names: list[str]) -> list[str]:
        """What a phrase's clause leaves open, from the taught shapes with a variable for
        some of its names and one more, nearest the phrase first. Some, because a name
        the graph knows may be the shape's frame ('cousin' in "X's cousin"). Each way of
        binding the names to its variables with the rest free, and what the first binding
        to reach anything finds, latest first."""
        from itertools import combinations, permutations

        rows: dict[str, list] = {}
        for shape, plan in self.db.execute(
                "SELECT shape, plan FROM learnt WHERE hits > misses "
                "ORDER BY hits - misses DESC, hits DESC").fetchall():
            rows.setdefault(shape, []).append(json.loads(plan))
        sigs = {shape: next((set(p["sig"]) for p in plans if p.get("sig")), set())
                for shape, plans in rows.items()}
        shapes = []
        for k in range(len(names), 0, -1):
            for chosen in combinations(names, k):
                mine = set(self.signature(phrase, list(chosen)))
                for shape, sig in sigs.items():
                    if shape.count("<") == k:
                        shapes.append((len(mine & sig) / max(1, len(mine | sig)), chosen, shape))
        shapes.sort(key=lambda r: -r[0])
        for _, chosen, shape in shapes[:8]:
            plans = [p for p in rows[shape] if not p.get("count")]
            variables = list(range(len(chosen))) + ["a"]
            for free in variables:
                rest = [v for v in variables if v != free]
                for order in permutations(chosen):
                    bound = dict(zip(rest, order))
                    found = [f for plan in plans for f in self.solve(plan, bound, free)
                             if f[0] not in names]
                    if found:
                        return [e for e, _ in sorted(found, key=lambda f: f[1], reverse=True)]
        return []

    def solve(self, plan: dict, bound: dict, free) -> list[tuple[str, tuple]]:
        """A plan read as a pattern and matched with any of its variables free: the
        path's first node is variable 0, its end 'a', and each hook's end the slot it
        names. Matched outward from a bound variable; the values found for `free`, each
        with the latest turn of what happened on the way."""
        steps = plan["steps"]
        edges = [(("p", i), ("p", i + 1), lab, d) for i, (lab, d) in enumerate(steps)]
        var = {0: ("p", 0), "a": ("p", len(steps))}
        for h, (k, i, hsteps) in enumerate(plan["attach"]):
            prev = ("p", i)
            for j, (lab, d) in enumerate(hsteps):
                edges.append((prev, ("h", h, j), lab, d))
                prev = ("h", h, j)
            var[k] = prev
        if free not in var or not bound or not all(v in var for v in bound):
            return []
        want = {var[v]: f"n:{n}" for v, n in bound.items()}
        root = next(iter(want))
        moods = plan.get("moods", {})
        partials = [{root: want[root]}]
        todo = list(edges)
        while todo and partials:
            seen = partials[0]
            edge = next((e for e in todo if (e[0] in seen) != (e[1] in seen)), None)
            if edge is None:
                break
            todo.remove(edge)
            u, v, lab, d = edge
            here, there, way = (u, v, d) if u in seen else (v, u, -d)
            nxt = []
            for got in partials:
                for label, direction, node in self.around(got[here]):
                    if label != lab or direction != way or node in got.values():
                        continue
                    if there in want and node != want[there]:
                        continue
                    # never through what did not happen where the lessons' did, or the
                    # other way round, as a plan is followed
                    if node.startswith("e:") and there[0] == "p" and self.mood(node) not in \
                            moods.get(str(there[1]), [self.mood(node)]):
                        continue
                    nxt.append({**got, there: node})
            partials = nxt[:2000]
        out = []
        for got in partials:
            end = got.get(var[free], "")
            if not end.startswith("n:") or len(got) < len({n for e in edges for n in e[:2]}):
                continue
            turns = [self.event(n)[1] for n in got.values()
                     if n.startswith("e:") and not self.mood(n)]
            out.append((end[2:], (max(turns, default=0),)))
        return out

    def apart(self, text: str, depth: int) -> str | None:
        for a, b in self.inner(text):
            phrase = text[a:b]
            for ask in (f"Who is {phrase}?", f"What is {phrase}?"):
                named = self.answered(ask, depth + 1)
                if named is None:
                    continue
                said = self.answered(text[:a] + named.title() + text[b:], depth + 1)
                if said is not None:
                    return said
        return None

    def inner(self, text: str) -> list[tuple[int, int]]:
        """The noun phrases of a question that hold a name and something said of it, as
        character spans, innermost first: 'the person who repairs clocks' before 'the
        cousin of the person who repairs clocks'. A phrase is a noun's whole subtree, kept
        where a possessor, a clause or a preposition hangs off it and the wh-word is not
        inside it."""
        rows = self.spans(text)
        kids: dict[int, list[int]] = {}
        for i, r in enumerate(rows):
            if r[4] != i:
                kids.setdefault(r[4], []).append(i)

        def subtree(i: int) -> list[int]:
            out = [i]
            for k in kids.get(i, []):
                out += subtree(k)
            return out

        found = []
        for i, (_, _, tag, _, _, pos) in enumerate(rows):
            if pos not in ("NOUN", "PROPN"):
                continue
            if not any(rows[k][3] in ("poss", "relcl", "acl", "prep") for k in kids.get(i, [])):
                continue
            span = sorted(subtree(i))
            # the question's own wh-word, not a relative one ('the person who repairs')
            if span[0] == 0:
                continue
            if len(span) >= len(rows) - 2 or not any(
                    rows[k][1][:1].isupper() or self.known(rows[k][1].lower()) for k in span
                    if k != i):
                continue
            a = rows[span[0]][0]
            b = rows[span[-1]][0] + len(rows[span[-1]][1])
            while text[a:b].lower().startswith(("the ", "a ")):
                a = text.index(" ", a) + 1
            found.append((b - a, a, b))
        return [(a, b) for _, a, b in sorted(found)]

    def spans(self, text: str) -> list:
        """A sentence's parse with where each word sits: offset, word, tag, link, head, and
        part of speech."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|spans|{text}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            kept = [[t.idx, t.text, t.tag_, t.dep_, t.head.i, t.pos_]
                    for t in nlp(self.model)(text)]
            self._keep(key, kept)
        return kept

    # -- a conversation -------------------------------------------------------

    def turn(self, turn: int, text: str) -> str | None:
        """One turn of a conversation, sorted by the arm itself. A question is answered
        and kept, never stored as a telling. The turn after a question is the reaction to
        the answer given: what it names that the question did not is the answer, and a
        reaction naming nothing and not negated confirms the answer given. Every other
        turn is a telling."""
        if asked(text):
            said = self.answer(_Asked(text))
            self.pending = (text, said)
            return said
        # a reaction is about the answer, not the world: no event in it has a named
        # subject ('it's the shed', 'that's right'), where a telling has ('Ada keeps...')
        about_world = any(label.startswith("nsubj") for ev in self.read(text)
                          for label, _ in ev["edges"])
        if self.pending is not None and not about_world:
            question, said = self.pending
            self.pending = None
            named, negated = self.reaction(text, question)
            if named:
                self.teach(question, named)
                return None
            if not negated and said != "I don't know.":
                self.teach(question, said)
                return None
            if negated:
                return None
        self.pending = None
        self.hear(turn, text)
        return None

    def reaction(self, text: str, question: str) -> tuple[str | None, bool]:
        """What a reaction names that the question did not, and whether it says no."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|reaction|{text}".encode()).hexdigest()
        rows = self._kept(key)
        if rows is None:
            doc = nlp(self.model)(text)
            # one row a token: no, whether it may name something, its name, and whether
            # it is a number of things read as the arm reads one ('none')
            rows = []
            for t in doc:
                no = t.dep_ == "neg" or (t.dep_ == "intj" and t.lower_ in ("no", "nope"))
                part = t.dep_ in ("compound", "amod") and t.head.pos_ in ("NOUN", "PROPN")
                number = t.like_num or numeral(t.lower_) is not None
                names = not part and (t.pos_ in ("NOUN", "PROPN", "NUM", "ADJ") or number)
                rows.append([no, names, t.lower_ if number else phrase(t), number])
            self._keep(key, rows)
        negated = any(no for no, _, _, _ in rows)
        for _, names, name, number in rows:
            # 'right' in 'that's right' is an adjective as a colour is, and names nothing
            # here, so a name must be one the graph holds
            if names and not said_in(name, question) and (number or self.holding(name)):
                return name, negated
        return None, negated

    def export(self) -> tuple[list, list]:
        return (self.db.execute("SELECT shape, plan, hits, misses FROM learnt").fetchall(),
                self.db.execute("SELECT template, pos, filler, n FROM positions").fetchall())

    def dials(self) -> dict:
        n_events, n_edges = (self.db.execute("SELECT COUNT(*) FROM events").fetchone()[0],
                             self.db.execute("SELECT COUNT(*) FROM edges").fetchone()[0])
        return {"model": self.model, "version": VERSION, "events": n_events, "edges": n_edges,
                "shapes": self.db.execute("SELECT COUNT(DISTINCT shape) FROM learnt")
                .fetchone()[0], "parsed": self.parsed}

    def close(self) -> None:
        self.db.close()
        if hasattr(self, "_cdb"):
            self._cdb.close()


def asked(text: str) -> bool:
    return text.rstrip().endswith("?")


class _Asked:
    def __init__(self, text: str) -> None:
        self.text = text


WORDS = ["none", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
         "ten", "eleven", "twelve"]


def numeral(text: str) -> int | None:
    """A number said as digits or as a word, or None."""
    t = text.strip().lower()
    if t.isdigit():
        return int(t)
    return WORDS.index(t) if t in WORDS else None


def said_in(answer: str, question: str) -> bool:
    return re.search(rf"\b{re.escape(answer)}\b", question.lower()) is not None
