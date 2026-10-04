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
from collections import Counter, deque
from pathlib import Path

from unfused.kinds import kinds

SCHEMA = """
CREATE TABLE IF NOT EXISTS events (id INTEGER PRIMARY KEY, turn INTEGER NOT NULL,
    lemma TEXT NOT NULL, heard TEXT NOT NULL, mood TEXT NOT NULL DEFAULT '');
CREATE TABLE IF NOT EXISTS edges (event INTEGER NOT NULL, label TEXT NOT NULL,
    node TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS edges_event ON edges (event);
CREATE INDEX IF NOT EXISTS edges_node ON edges (node);
CREATE INDEX IF NOT EXISTS events_turn ON events (turn);
CREATE TABLE IF NOT EXISTS learnt (shape TEXT NOT NULL, plan TEXT NOT NULL,
    hits INTEGER NOT NULL, misses INTEGER NOT NULL, PRIMARY KEY (shape, plan));
CREATE TABLE IF NOT EXISTS positions (template TEXT NOT NULL, pos INTEGER NOT NULL,
    filler TEXT NOT NULL, n INTEGER NOT NULL, PRIMARY KEY (template, pos, filler));
CREATE TABLE IF NOT EXISTS aliases (word TEXT NOT NULL, name TEXT NOT NULL,
    hits REAL NOT NULL, found INTEGER NOT NULL, PRIMARY KEY (word, name));
CREATE TABLE IF NOT EXISTS agreement (name TEXT NOT NULL, pronoun TEXT NOT NULL,
    n INTEGER NOT NULL, PRIMARY KEY (name, pronoun));
CREATE TABLE IF NOT EXISTS boundaries (turn INTEGER PRIMARY KEY);
CREATE TABLE IF NOT EXISTS asks (wh TEXT NOT NULL, mark TEXT NOT NULL, n INTEGER NOT NULL,
    PRIMARY KEY (wh, mark));
CREATE TABLE IF NOT EXISTS contexts (word TEXT PRIMARY KEY, names TEXT NOT NULL,
    heard INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS called (node TEXT NOT NULL, name TEXT NOT NULL,
    turn INTEGER NOT NULL, PRIMARY KEY (node, name));
CREATE INDEX IF NOT EXISTS called_name ON called (name);
CREATE TABLE IF NOT EXISTS heard_as (name TEXT NOT NULL, pos TEXT NOT NULL,
    n INTEGER NOT NULL, PRIMARY KEY (name, pos));
CREATE TABLE IF NOT EXISTS circumstances (wh TEXT NOT NULL, link TEXT NOT NULL,
    n INTEGER NOT NULL, PRIMARY KEY (wh, link));
CREATE TABLE IF NOT EXISTS counts (shape TEXT NOT NULL, walk TEXT NOT NULL,
    word TEXT NOT NULL, size INTEGER NOT NULL);
"""
# what a taught arm carries to the next conversation: no facts, only how to read and ask
CARRIED = ("learnt", "positions")

# every extraction kept across runs: the parser is deterministic, and the version is in
# the key so a change to what is extracted re-reads every sentence
CACHE = Path(__file__).resolve().parents[2] / "state" / "parses.sqlite"
VERSION = "graph-14"
# every text MiniLM has encoded, by its text
VECTORS = Path(__file__).resolve().parents[2] / "state" / "vectors.sqlite"
# every text the parser has read, as its reading, by model and text
DOCS = Path(__file__).resolve().parents[2] / "state" / "docs.sqlite"

# a clause's links that are not arguments, as Universal Dependencies names them: each is
# kept as a link to a function word's node, which walks skip
SKIP = {"punct", "det", "det:predet", "aux", "aux:pass", "cc", "cc:preconj", "mark",
        "discourse", "case", "dep", "advmod", "compound:prt", "expl", "expl:pv", "cop",
        "fixed", "goeswith", "reparandum", "list", "orphan"}
# the links that start a clause of its own
CLAUSES = {"conj", "advcl", "ccomp", "xcomp", "acl", "acl:relcl", "parataxis", "csubj",
           "csubj:pass"}
# the links a blank can stand in, besides a preposition's
BLANKS = {"nsubj", "nsubj:pass", "obj", "iobj", "xcomp", "appos", "nmod:poss", "attr",
          "acomp"}
# the parser every reading is taken with, and where its models are kept: Stanza's
# transformer package, which reads 'Someone painted the jars slate' with the jars as the
# object where its default package took them as part of 'slate'
PARSER = "stanza-accurate"
# Stanza's package for each name a parser goes by
PACKAGES = {"stanza": "default", "stanza-accurate": "default_accurate"}
# how many texts the parser reads in one batch
BATCH = 256
STANZA = Path(__file__).resolve().parents[2] / "state" / "stanza"

_NLP: dict = {}
# texts read this run, and the open store of readings
_DOCS: dict = {}
_DOCS_DB: dict = {}

# how much nearer a word's likest candidate must be than the next for a vote: right picks'
# gaps sat at 0.14 to 0.21 and wrong ones' at 0.02 to 0.06 (readings/unheard-*)
MARGIN = 0.08
# how fast a hearing fades within an episode (ACT-R's base-level decay)
DECAY = 0.5
# how many of a node's steps of one label a walk follows, the latest first
REACH = 100
# how many nodes' steps one question may look at before its plans give up
EFFORT = 200_000
# how many hearings a kind's fit is worth beside a name's own (a Dirichlet prior's weight)
PRIOR = 2.0


class Spent(Exception):
    """A question's effort is used up."""
_VECTORS: dict = {}
# the encoder apart from the words, or a story about a model reads the encoder as a vector
_ENCODER: dict = {}


def nlp(model: str):
    """The parser, as a function from text to a spaCy `Doc`. Stanza's reading carries the
    Universal Dependencies relations and features (`PronType=Int`, `Polarity=Neg`,
    `Definite`), which mark in any language what a rule on a word would otherwise hold
    by hand. A Stanza parser also reads many texts at once (`.many`)."""
    if model not in _NLP:
        if model in PACKAGES:
            _NLP[model] = _stanza(PACKAGES[model])
        else:
            import spacy

            _NLP[model] = spacy.load(model)
    return _NLP[model]


def _stanza(package: str):
    import spacy
    import stanza
    from spacy.tokens import Doc

    pipe = stanza.Pipeline("en", dir=str(STANZA), package=package, processors="tokenize,"
                           "mwt,pos,lemma,depparse", download_method=None, verbose=False)
    vocab = spacy.blank("xx").vocab

    def convert(read) -> Doc:
        words, spaces, heads, deps, pos, tags, lemmas, morphs, starts = ([] for _ in range(9))
        for sent in read.sentences:
            base = len(words)
            tokens = sent.tokens
            for n, tok in enumerate(tokens):
                after = tokens[n + 1].start_char if n + 1 < len(tokens) else None
                for j, w in enumerate(tok.words):
                    words.append(w.text)
                    last = j == len(tok.words) - 1
                    spaces.append(bool(last and after is not None and after > tok.end_char))
                    heads.append(base + (w.head - 1 if w.head else w.id - 1))
                    deps.append("ROOT" if w.deprel == "root" else w.deprel)
                    pos.append(w.upos)
                    tags.append(w.xpos or "")
                    lemmas.append(w.lemma or w.text)
                    morphs.append(w.feats or "")
                    starts.append(len(words) - 1 == base)
        return Doc(vocab, words=words, spaces=spaces, heads=heads, deps=deps, pos=pos,
                   tags=tags, lemmas=lemmas, morphs=morphs, sent_starts=starts)

    def read(text: str) -> Doc:
        return convert(pipe(text))

    def many(texts: list[str]) -> list[Doc]:
        # a batch keeps the card busy: one sentence at a time leaves it two-thirds idle
        return [convert(r) for r in pipe.bulk_process(texts)]

    read.many = many
    return read


def parse_many(model: str, texts) -> int:
    """Every text not yet read, read in batches into the store of readings, so a stream
    is parsed ahead of being heard. How many were read."""
    db = _store()
    todo = list(dict.fromkeys(t for t in texts if t.strip() and db.execute(
        "SELECT 1 FROM docs WHERE key = ?", (f"{model}|{t}",)).fetchone() is None))
    reader = nlp(model)
    for at in range(0, len(todo), BATCH):
        chunk = todo[at:at + BATCH]
        docs = reader.many(chunk) if hasattr(reader, "many") else [reader(t) for t in chunk]
        db.executemany("INSERT OR REPLACE INTO docs VALUES (?, ?)",
                       [(f"{model}|{t}", d.to_bytes(exclude=["tensor", "user_data"]))
                        for t, d in zip(chunk, docs)])
        db.commit()
    return len(todo)


def parse(model: str, text: str):
    """The parser's reading of a text, kept on disk whole: what is extracted from it can
    change without the transformer reading anything again. The parser is loaded only for
    a text never read."""
    from spacy.tokens import Doc

    key = f"{model}|{text}"
    if key in _DOCS:
        return _DOCS[key]
    db = _store()
    row = db.execute("SELECT doc FROM docs WHERE key = ?", (key,)).fetchone()
    if row is not None:
        doc = Doc(_DOCS_DB["vocab"]).from_bytes(row[0])
    else:
        doc = nlp(model)(text)
        db.execute("INSERT OR REPLACE INTO docs VALUES (?, ?)",
                   (key, doc.to_bytes(exclude=["tensor", "user_data"])))
        db.commit()
    _DOCS[key] = doc
    return doc


def _store() -> sqlite3.Connection:
    """The open store of readings."""
    import spacy

    if "db" not in _DOCS_DB:
        DOCS.parent.mkdir(parents=True, exist_ok=True)
        _DOCS_DB["db"] = sqlite3.connect(str(DOCS), timeout=60)
        # a reading lost to a crash is read again, so none waits on the disk
        _DOCS_DB["db"].execute("PRAGMA journal_mode=WAL")
        _DOCS_DB["db"].execute("PRAGMA synchronous=OFF")
        _DOCS_DB["db"].execute("CREATE TABLE IF NOT EXISTS docs (key TEXT PRIMARY KEY, "
                               "doc BLOB NOT NULL)")
        # a vocabulary of no one language: a word's lower case and nothing else
        _DOCS_DB["vocab"] = spacy.blank("xx").vocab
    return _DOCS_DB["db"]


def vectors(texts: list[str]):
    """MiniLM's vector for each text, each encoded once and kept on disk: a word's vector
    never changes, so it is what everyone knows about the word, read once and kept. The
    model is loaded only for a text never encoded."""
    import numpy as np

    new = [t for t in dict.fromkeys(texts) if t not in _VECTORS]
    if new:
        VECTORS.parent.mkdir(parents=True, exist_ok=True)
        db = sqlite3.connect(str(VECTORS))
        db.execute("CREATE TABLE IF NOT EXISTS vectors (text TEXT PRIMARY KEY, v BLOB)")
        for t in new:
            row = db.execute("SELECT v FROM vectors WHERE text = ?", (t,)).fetchone()
            if row is not None:
                _VECTORS[t] = np.frombuffer(row[0], dtype=np.float32)
        missing = [t for t in new if t not in _VECTORS]
        if missing:
            if "model" not in _ENCODER:
                from unfused.store import MiniLmEmbedder

                _ENCODER["model"] = MiniLmEmbedder()
            for t, v in zip(missing, _ENCODER["model"].encode(missing)):
                _VECTORS[t] = np.asarray(v, dtype=np.float32)
                db.execute("INSERT OR REPLACE INTO vectors VALUES (?, ?)",
                           (t, _VECTORS[t].tobytes()))
            db.commit()
        db.close()
    return [_VECTORS[t] for t in texts]


def phrase(tok) -> str:
    """A noun as described: its own words with it, no determiner or number. What a
    question or a reaction names, and what finds an individual."""
    keep = [t for t in tok.children if t.dep_ in DESCRIBE] + [tok]
    return " ".join(t.text for t in sorted(keep, key=lambda t: t.i)).lower()


def called_by(tok) -> str:
    """A noun's label: the noun alone. What its adjectives and compounds say ('the
    still room', 'the sitting room') is said of the thing, as links of their own, so
    two rooms are two rooms and both are rooms."""
    return tok.lower_


# the links by which a noun's own words describe it, as the parse names them
DESCRIBE = ("amod", "compound", "flat")


def feature(tok, name: str) -> list[str]:
    return tok.morph.get(name)


def personal(tok) -> bool:
    """A personal pronoun, by what the parse marks: 'she', 'it', 'them'."""
    return tok.pos_ == "PRON" and "Prs" in feature(tok, "PronType")


def asking(tok) -> bool:
    """A word that asks, or relates a clause: 'who', 'where', 'which'."""
    return bool({"Int", "Rel"} & set(feature(tok, "PronType")))


def negates(tok) -> bool:
    """A word that says no: negation (`Polarity=Neg`), or a negative word as the parse
    marks it ('never', 'nothing': `PronType=Neg`)."""
    return "Neg" in feature(tok, "Polarity") or "Neg" in feature(tok, "PronType")


def agreement(tok) -> str:
    """What a pronoun agrees in, by its features less its case: 'him' and 'he' agree."""
    return "|".join(f"{k}={v}" for k, v in sorted(tok.morph.to_dict().items())
                    if k not in ("Case", "PronType", "Poss", "Reflex"))


def mood(tok) -> str:
    """What did not happen, and what only might or will, by what the parse marks: a
    negation (`Polarity=Neg`), and an auxiliary marked finite with no mood of its own,
    as a modal is ('might', 'will') where 'did' and 'has' are indicative."""
    return " ".join(c.lemma_.lower() for c in tok.children
                    if negates(c) or (c.dep_ == "aux" and "Fin" in feature(c, "VerbForm")
                                      and not feature(c, "Mood")))


def link(c) -> str:
    """The link an argument hangs by: an oblique or a nominal modifier by its case
    word ('prep:in' for 'in the room'), anything else by its relation."""
    case = [k.lower_ for k in c.children if k.dep_ == "case"]
    if case and (c.dep_.startswith("obl") or (c.dep_.startswith("nmod")
                                               and c.dep_ != "nmod:poss")):
        return "prep:" + " ".join(case)
    return c.dep_


def extract(doc) -> list[dict]:
    """A sentence as events: [{"lemma", "edges": [[label, target], ...]}], a target being
    "n:<name>", "e:<index into this list>" or "f:<a function word>". A personal pronoun
    with something to refer to in its own sentence is that sentence's first subject.
    Each edge to a thing has its mention beside it, in "mentions" (the token's index, or
    None), and "things" holds each mention's label, whether it opens an individual, and
    what its own words say of it."""
    first = next((t for t in doc if t.dep_ in ("nsubj", "nsubj:pass") and t.pos_ != "PRON"),
                 None)
    events: list[dict] = []
    index: dict[int, int] = {}
    # each mention of a thing, by its token: its label, whether it opens an individual
    # of its own, and its own words. One marked indefinite ('a ball') opens one, by the
    # feature the parse marks in any language; any other joins one already in mind
    things: dict[int, list] = {}

    def thing(tok) -> str:
        if tok.pos_ not in ("NOUN", "PROPN"):
            return f"n:{phrase(tok)}"
        opens = any("Ind" in feature(c, "Definite") for c in tok.children if c.dep_ == "det")
        mods = [[c.dep_, c.lower_] for c in tok.children if c.dep_ in DESCRIBE]
        things[tok.i] = [called_by(tok), opens, mods, tok.pos_]
        # the mention rides with the target until the events are written out
        return f"n:{called_by(tok)}\x1f{tok.i}"

    def heads(tok) -> bool:
        # by structure, not by tag: anything with an argument of its own is an event
        return any(c.dep_ not in SKIP and c.dep_ not in DESCRIBE and c.dep_ != "conj"
                   for c in tok.children)

    for tok in doc:
        if heads(tok):
            index[tok.i] = len(events)
            # a predicate with a copula is the copula's event ('the ball was red' is a
            # 'be' with the ball and red), as a verb's is the verb's; the copula's word
            # is kept as its link
            cop = next((c for c in tok.children if c.dep_ == "cop"), None)
            lemma = (cop or tok).lemma_.lower()
            # the verb alone is its lemma, so 'will not give back' and 'gave' are one verb;
            # what did not happen or only might is marked on the event
            # a clause a negated predicate is about was never so ('It isn't true that Ada
            # keeps the jars'): it takes the negation
            said = mood(tok)
            if tok.dep_ in ("csubj", "ccomp") and any(negates(c) for c in tok.head.children):
                said = " ".join(w for w in (said, mood(tok.head)) if w)
            events.append({"lemma": lemma, "mood": said, "edges": []})
            # the word as heard, where it is not the lemma: the inflection a mouth will
            # say again
            if cop is None and tok.lower_ != lemma:
                events[-1]["edges"].append(["form", f"f:{tok.lower_}"])

    def keep(ev: dict, label: str, tok) -> None:
        # what the parse gives of an argument beyond the thing it names: a pronoun's own
        # word beside what it resolves to, and the argument's function words ('the'),
        # each a link to a node of their own kind, which walks skip. A clause keeps its
        # own, as the event it is
        if tok.i in index:
            return
        if personal(tok) and "3" in feature(tok, "Person"):
            ev["edges"].append([label, f"f:{tok.lower_}"])
        for c in tok.children:
            if c.dep_ in SKIP and c.dep_ != "punct":
                ev["edges"].append([f"{label}>{c.dep_}", f"f:{c.lower_}"])

    def target(tok) -> str | None:
        if tok.i in index:
            return f"e:{index[tok.i]}"
        if personal(tok):
            # the speaker and the one spoken to are no one heard of before: only a third
            # person refers back
            if "3" not in feature(tok, "Person"):
                return f"f:{tok.lower_}"
            # one with nothing to refer to in its own sentence is resolved when heard,
            # against the episode, by what it agrees in and the slot it fills
            if first is not None:
                return thing(first)
            return f"p:{tok.dep_}|{agreement(tok)}"
        if tok.pos_ in ("NOUN", "PROPN", "NUM", "ADJ") or "Card" in feature(tok, "NumType"):
            return thing(tok)
        # any other word is kept, as a function word is
        return f"f:{tok.lower_}"

    for tok in doc:
        if tok.i not in index:
            continue
        ev = events[index[tok.i]]
        cop = any(c.dep_ == "cop" for c in tok.children)
        if cop:
            # the predicate is the copula's argument: 'red', 'a girl', or 'in the park',
            # by its case word as an oblique is; its own function words are the event's
            case = [k.lower_ for k in tok.children if k.dep_ == "case"]
            label = "prep:" + " ".join(case) if case else (
                "acomp" if tok.pos_ == "ADJ" else "attr")
            ev["edges"].append([label, thing(tok)])
        elif tok.pos_ in ("NOUN", "PROPN", "ADJ") and not any(
                c.dep_.startswith("nsubj") for c in tok.children):
            ev["edges"].append(["self", thing(tok)])
            keep(ev, "self", tok)
        for c in tok.children:
            if c.dep_ in DESCRIBE:
                continue
            if c.dep_ in SKIP:
                # a function word is kept as a link to a node of its own kind ('did',
                # 'not', 'back', 'because'), which walks skip
                if c.dep_ != "punct":
                    ev["edges"].append([c.dep_, f"f:{c.lower_}"])
                continue
            label = link(c)
            if t := target(c):
                ev["edges"].append([label, t])
                keep(ev, label, c)
                for cc in c.children:
                    if cc.dep_ == "conj" and (t2 := target(cc)):
                        ev["edges"].append([label, t2])
                        keep(ev, label, cc)
        # a conjoined verb shares its head's subject: 'picked up the milk and went'
        if tok.dep_ == "conj" and tok.head.i in index and not any(
                e[0].startswith("nsubj") for e in ev["edges"]):
            for label, t in events[index[tok.head.i]]["edges"]:
                if label.startswith("nsubj"):
                    ev["edges"].append([label, t])
    # an adjective is said of its thing, as a link of its own: 'the red ball' tells the
    # ball and that it is red, so 'red' is a node, and the ball painted blue stays one
    # ball. The link's name is the one the parse gives it, in the order heard
    for i, (_, _, mods, _) in sorted(things.items()):
        for label, m in mods:
            events.append({"lemma": label, "mood": "", "edges": [
                ["self", f"n:{called_by(doc[i])}\x1f{i}"], [label, f"n:{m}"]]})
    # an event left with no arguments goes, and every reference to the rest is
    # renumbered: its function words alone say nothing of anything
    kept = [i for i, e in enumerate(events)
            if any(not t.startswith("f:") for _, t in e["edges"])]
    renumber = {f"e:{old}": f"e:{new}" for new, old in enumerate(kept)}
    out = []
    for i in kept:
        edges = [[label, renumber.get(t, t)] for label, t in events[i]["edges"]
                 if not t.startswith("e:") or t in renumber]
        mentions = [int(t.split("\x1f")[1]) if "\x1f" in t else None for _, t in edges]
        out.append({"lemma": events[i]["lemma"], "mood": events[i]["mood"],
                    "edges": [[label, t.split("\x1f")[0]] for label, t in edges],
                    "mentions": mentions,
                    "things": {str(m): things[m] for m in mentions if m is not None}})
    return out


class GraphArm:
    name = "graphed"

    def __init__(self, directory: Path, model: str = PARSER,
                 known: dict | None = None, cache: Path | None = CACHE) -> None:
        directory.mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(str(directory / "graph.db"))
        # a commit after every sentence is kept, and written ahead, without waiting on
        # the disk for each one
        self.db.execute("PRAGMA journal_mode=WAL")
        self.db.execute("PRAGMA synchronous=NORMAL")
        self.db.executescript(SCHEMA)
        for table, rows in (known or {}).items():
            self.db.executemany(f"INSERT OR IGNORE INTO {table} VALUES (?, ?, ?, ?)",
                                [tuple(x) for x in rows])
        self.db.commit()
        self.model = model
        self.cache = cache
        self.last_notes: list[str] = []
        self.parsed = 0
        # the pairs of events already counted towards a plan's order, this world
        self.ordered: set = set()
        # a question this arm answered, waiting for the turn that reacts to it
        self.pending: tuple[str, str] | None = None
        # the words never heard and answers already counted towards an alias, this world
        self.aliased: set = set()
        # events already found replaced by a later one, cleared whenever one is heard
        self._replaced: dict = {}
        # taught shapes' signatures, rebuilt after a lesson; questions' parses
        self._sigs: list | None = None
        self._tokens: dict = {}
        # every node's steps, kept across sentences and questions: a walk over a hub asks
        # for the same node's steps thousands of times. A sentence drops only the nodes it
        # gives a new step, so what was loaded stays loaded
        self._steps: dict = {}
        # each node's steps by label and direction, kept beside the steps they sort
        self._labelled: dict = {}
        # every event's lemma, turn and mood, kept for good: what was heard never changes
        self._events: dict = {}
        # every name's mark, dropped only for names a sentence names
        self._marks: dict = {}
        # each individual's label and how it is said, and each name's individuals
        self._labels: dict = {}
        self._described: dict = {}
        self._nodes: dict = {}
        # steps looked at by the question being answered, or None outside answering
        self.spent: int | None = None
        # the kinds last read off the graph, and when: (episode, events then, kinds,
        # slots each kind filled, everything each kind filled)
        self._kinds: tuple | None = None

    # -- reading ---------------------------------------------------------------

    def _kept(self, key: str):
        if self.cache is None:
            return None
        if not hasattr(self, "_cdb"):
            self.cache.parent.mkdir(parents=True, exist_ok=True)
            self._cdb = sqlite3.connect(str(self.cache), timeout=60)
            self._cdb.execute("PRAGMA journal_mode=WAL")
            self._cdb.execute("PRAGMA synchronous=OFF")
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
            kept = extract(parse(self.model, text))
            self.parsed += 1
            self._keep(key, kept)
        return kept

    def names_in(self, question: str) -> list[tuple[int, int, str]]:
        """The question's noun phrases, as spans with their names."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|names-2|{question}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            doc = parse(self.model, question)
            kept = []
            for tok in doc:
                if tok.pos_ in ("NOUN", "PROPN") and tok.dep_ not in DESCRIBE:
                    left = [t for t in tok.children if t.dep_ in DESCRIBE and t.i < tok.i]
                    start = min([t.idx for t in left] + [tok.idx])
                    # never the question's own verb: 'keep' in 'Where does Ada keep the
                    # jars?' is cut with the jars only where the graph holds Ada's keeping
                    # told actively, so one relation would be two shapes by how its facts
                    # were told
                    verb = tok.head if tok.dep_ == "obj" and tok.head.dep_ != "ROOT" else None
                    between = (doc[verb.i + 1:min([t.i for t in left] + [tok.i])]
                               if verb is not None and verb.i < tok.i else None)
                    governs = ([verb.idx, verb.lemma_.lower()] if between is not None
                               and all(t.dep_ == "det" for t in between) else [None, None])
                    kept.append([start, tok.idx + len(tok.text), phrase(tok), *governs])
            self._keep(key, kept)
        return [tuple(k) for k in kept]

    def hear(self, turn: int, text: str) -> None:
        self._replaced = {}
        # a turn holding no words is a break in the text ('***', a new page): what is
        # heard after it is another episode
        if not any(ch.isalnum() for ch in text):
            self.db.execute("INSERT OR IGNORE INTO boundaries VALUES (?)", (turn,))
            self.db.commit()
            return
        events = self.read(text)
        resolved = {t: self.referent(t[2:]) for ev in events for _, t in ev["edges"]
                    if t.startswith("p:")}
        # each thing the sentence names, as an individual: one opened here, or the one
        # in mind it joins. Its id is where it was first heard, so a run reproduces it
        mentions: dict[str, list] = {}
        for ev in events:
            mentions.update(ev.get("things", {}))
        fresh = self.db.execute("SELECT COUNT(*) FROM called WHERE turn = ?",
                                (turn,)).fetchone()[0]
        who: dict[str, str] = {}
        here: list[tuple[str, set, str]] = []
        for m, (name, opens, mods, pos) in sorted(mentions.items(),
                                                  key=lambda kv: int(kv[0])):
            mods = [w for _, w in mods]
            self.db.execute("INSERT INTO heard_as VALUES (?, ?, 1) ON CONFLICT(name, pos) "
                            "DO UPDATE SET n = n + 1", (name, pos))
            node = None
            if not opens:
                # one named earlier in this sentence first, then the episode's
                node = next((n for lab, said, n in here if lab == name and set(mods) <= said),
                            None) or self.in_mind(name, mods)
            if node is None:
                node, fresh = f"i:{turn}.{fresh}", fresh + 1
                self.db.execute("INSERT INTO called VALUES (?, ?, ?)", (node, name, turn))
            here.append((name, set(mods), node))
            who[m] = node
        ids = []
        # the words of what this sentence touched: a name or a description holding none
        # of them stands for what it stood for before
        touched: set[str] = set()
        for ev in events:
            cur = self.db.execute("INSERT INTO events (turn, lemma, heard, mood) VALUES "
                                  "(?, ?, ?, ?)", (turn, ev["lemma"], text, ev["mood"]))
            ids.append(cur.lastrowid)
        for ev, eid in zip(events, ids):
            for (label, t), m in zip(ev["edges"], ev.get("mentions", [None] * len(ev["edges"]))):
                node = (f"e:{ids[int(t[2:])]}" if t.startswith("e:") else
                        who[str(m)] if m is not None else resolved.get(t, t))
                if node is None:
                    continue
                self.db.execute("INSERT INTO edges VALUES (?, ?, ?)", (eid, label, node))
                self._steps.pop(node, None)
                self._steps.pop(f"e:{eid}", None)
                self._described.pop(node, None)
                if (name := self.label(node)) is not None:
                    self._steps.pop(f"n:{name}", None)
                    self._marks.pop(name, None)
                    touched.update(name.split())
        for key in [k for k in self._nodes
                    if touched & set((k[1] if isinstance(k, tuple) else k).split())]:
            del self._nodes[key]
        # a description's steps are its individuals', and what was said of them changed
        for key in [k for k in self._steps if k.startswith("n:") and " " in k]:
            del self._steps[key]
        for t, node in resolved.items():
            if node is not None and t.startswith("p:"):
                # agreement is what everyone knows, so it is counted by label
                self.db.execute("INSERT INTO agreement VALUES (?, ?, 1) ON CONFLICT(name, "
                                "pronoun) DO UPDATE SET n = n + 1",
                                (f"n:{self.label(node)}", t[2:].split("|", 1)[1]))
        self.db.commit()

    def referent(self, pronoun: str) -> str | None:
        """What a pronoun refers to, from this episode: the latest individual in the slot
        it fills, a subject for a subject and a thing that was not one for anything else,
        as parallelism in centering has it. Given as its slot and what it agrees in
        ('nsubj|Gender=Fem|Number=Sing|Person=3'), by what the parse marks. A label that
        has been called something else and never this is passed over, so agreement is
        learnt from what pronouns have been resolved to."""
        slot, agrees = pronoun.split("|", 1)
        subject = slot.startswith("nsubj")
        for node, label in self.db.execute(
                "SELECT edges.node, edges.label FROM edges JOIN events ON events.id = "
                "edges.event WHERE events.turn > ? AND edges.node LIKE 'i:%' "
                "ORDER BY edges.event DESC LIMIT 200", (self.episode(),)):
            if label.startswith("nsubj") != subject or label == "self":
                continue
            called = dict(self.db.execute("SELECT pronoun, n FROM agreement WHERE name = ?",
                                          (f"n:{self.label(node)}",)).fetchall())
            if called and agrees not in called and max(called.values()) >= 2:
                continue
            return node
        return None

    # -- the graph -------------------------------------------------------------

    def holding(self, word: str) -> list[str]:
        """The names that hold a word whole: 'ochre colour' for 'ochre', and a
        description that finds something: 'still room'."""
        out = [f"n:{n}" for n in self.names_like(f"%{word}%") if said_in(word, n)]
        if f"n:{word}" not in out and self.known(word):
            out.append(f"n:{word}")
        return out

    def names_like(self, like: str = "%") -> list[str]:
        """The names heard, as labels of individuals or as words held themselves."""
        return list(dict.fromkeys(
            [n for (n,) in self.db.execute("SELECT DISTINCT name FROM called WHERE name "
                                           "LIKE ?", (like,))]
            + [n[2:] for (n,) in self.db.execute(
                "SELECT DISTINCT node FROM edges WHERE node LIKE ?", (f"n:{like}",))]))

    def names(self) -> list[str]:
        return self.names_like()

    def known(self, name: str) -> bool:
        return len(self.nodes(name)) > 1 or self.db.execute(
            "SELECT 1 FROM edges WHERE node = ? LIMIT 1", (f"n:{name}",)).fetchone() is not None

    # -- individuals ------------------------------------------------------------

    def label(self, node: str) -> str | None:
        """What a node is called: a name's own words, an individual's label, and None for
        an event."""
        if node.startswith("n:"):
            return node[2:]
        if not node.startswith("i:"):
            return None
        got = self._labels.get(node)
        if got is None:
            got = self._labels[node] = self.db.execute(
                "SELECT name FROM called WHERE node = ? ORDER BY turn DESC LIMIT 1",
                (node,)).fetchone()[0]
        return got

    def nodes(self, name: str) -> list[str]:
        """Everything a name stands for: the name itself, where it is held as a word
        ('red', '3'), and each individual it labels, the latest first. An index, so a
        walk from a name reaches every individual it labels without holding a copy.
        Where no individual has the name as its label, it is read as a description:
        the longest run of its words that is a label, and the individuals of that label
        of which every other word was said ('still room' finds the room said to be
        still, in whatever order the words came)."""
        got = self._nodes.get(name)
        if got is None:
            got = [f"n:{name}"] + self.labelled(name)
            words = name.split()
            for size in range(len(words) - 1, 0, -1) if len(got) == 1 else ():
                found = []
                for at in range(len(words) - size + 1):
                    rest = set(words[:at] + words[at + size:])
                    found += [n for n in self.labelled(" ".join(words[at:at + size]))
                              if rest <= set(self.said_of(n))]
                if found:
                    got += found
                    break
            self._nodes[name] = got
            self._nodes[("set", name)] = set(got)
        return got

    def labelled(self, name: str) -> list[str]:
        return [n for (n,) in self.db.execute(
            "SELECT node FROM called WHERE name = ? ORDER BY turn DESC", (name,))]

    def said_of(self, node: str) -> list[str]:
        """What was said of an individual, the latest first: the adjectives it was heard
        with, and what a clause made it or called it ('painted the ball blue', 'the ball
        was red'), by the links the parse gives those."""
        return [n[2:] for (n,) in self.db.execute(
            "SELECT b.node FROM edges a JOIN edges b ON a.event = b.event WHERE a.node = ? "
            "AND b.label IN ('amod', 'compound', 'flat', 'acomp', 'xcomp') AND b.node LIKE 'n:%' "
            "GROUP BY b.node ORDER BY MAX(b.event) DESC", (node,))]

    def describe(self, node: str) -> str:
        """How an individual is said: its own words in the order first heard, then its
        label ('still room'), so an answer tells apart what its label alone would not."""
        if not node.startswith("i:"):
            return self.label(node) or node
        got = self._described.get(node)
        if got is None:
            mods = [n[2:] for (n,) in self.db.execute(
                "SELECT b.node FROM edges a JOIN edges b ON a.event = b.event WHERE "
                "a.node = ? AND a.label = 'self' AND b.label IN ('amod', 'compound', 'flat') "
                "GROUP BY b.node "
                "ORDER BY MIN(b.event)", (node,))]
            got = self._described[node] = " ".join(mods + [self.label(node)])
        return got

    def is_(self, node: str, goal: str) -> bool:
        """Whether a node is the goal, or an individual the goal's name finds."""
        if node == goal:
            return True
        if not (goal.startswith("n:") and node.startswith("i:")):
            return False
        self.nodes(goal[2:])
        return node in self._nodes[("set", goal[2:])]

    def among(self, node: str, nodes) -> bool:
        """Whether a walk has been at a node already: at it, or at a name or description
        that finds it, so a walk from 'lily' does not come back through a Lily."""
        return node in nodes or (node.startswith("i:") and (
            f"n:{self.label(node)}" in nodes or f"n:{self.describe(node)}" in nodes))

    # -- concepts ---------------------------------------------------------------

    def concepts(self) -> tuple[dict, Counter, Counter]:
        """Each label's kind (`kinds`), with what each kind filled: by (kind, verb, link)
        and in all. Kinds are what everyone knows, so they are read again once the
        graph has grown by a quarter, not with each episode."""
        events = self.db.execute("SELECT COUNT(*) FROM events").fetchone()[0]
        got = self._kinds
        if got is None or events > got[1] * 1.25:
            rows = [(e, lemma, link, self.label(n)) for e, lemma, link, n in self.db.execute(
                "SELECT edges.event, events.lemma, edges.label, edges.node FROM edges JOIN "
                "events ON events.id = edges.event WHERE edges.node NOT LIKE 'e:%' AND "
                "edges.node NOT LIKE 'f:%'")]
            kind = kinds(rows)
            slots, filled = Counter(), Counter()
            for _, lemma, link, label in rows:
                slots[(kind[label], lemma, link)] += 1
                filled[kind[label]] += 1
            got = self._kinds = (self.episode(), events, kind, slots, filled)
        return got[2], got[3], got[4]

    def kind_of(self, word: str) -> int | None:
        """The kind a label is of, learnt from how it connects; None for one never
        heard."""
        return self.concepts()[0].get(word)

    def in_mind(self, name: str, mods=()) -> str | None:
        """The individual a mention joins: the one with this label, opened in this
        episode, mentioned latest, of which every adjective the mention has was said
        already. Until concepts say which adjectives exclude each other, one never said
        of it is read as contradicting it, so an unsure mention opens rather than
        merges. Across episodes nothing joins until joining is learnt."""
        for (node,) in self.db.execute(
                "SELECT edges.node FROM edges JOIN called ON called.node = edges.node WHERE "
                "called.name = ? AND called.turn > ? GROUP BY edges.node "
                "ORDER BY MAX(edges.event) DESC LIMIT 20", (name, self.episode())):
            if set(mods) <= set(self.said_of(node)):
                return node
        return None

    def individuals(self, name: str, episode: bool = False) -> list[str]:
        """The individuals a name labels, the latest first, this episode's alone with
        `episode`."""
        return [n for (n,) in self.db.execute(
            "SELECT node FROM called WHERE name = ? AND turn > ? ORDER BY turn DESC",
            (name, self.episode() if episode else -2))]

    def around(self, node: str) -> list[tuple[str, int, str]]:
        """Every step from a node: (label, direction, next node). Direction 1 goes from an
        event to its argument, -1 back from an argument to its event."""
        # a question's effort is the steps it looks at; past `EFFORT` it stops looking
        if self.spent is not None:
            self.spent += 1
            if self.spent > EFFORT:
                raise Spent
        steps = self._steps.get(node)
        if steps is None:
            steps = self._steps[node] = self._around(node)
        return steps

    def around_as(self, node: str, label: str, direction: int) -> list[str]:
        """The nodes one step of this label and direction away, in the order `around`
        gives them and counted as one look as `around` is: a hub's steps are sorted by
        label once, so a pattern does not read all of them for each of its partials."""
        steps = self.around(node)
        kept = self._labelled.get(node)
        if kept is None or kept[0] is not steps:
            by: dict = {}
            for lab, d, nxt in steps:
                by.setdefault((lab, d), []).append(nxt)
            kept = self._labelled[node] = (steps, by)
        return kept[1].get((label, direction), [])

    def _around(self, node: str) -> list[tuple[str, int, str]]:
        if node.startswith("e:"):
            eid = int(node[2:])
            out = [(label, 1, n) for label, n in self.db.execute(
                "SELECT label, node FROM edges WHERE event = ? AND node NOT LIKE 'f:%'",
                (eid,))]
        else:
            out = []
        # the latest first, so a walk cut short keeps what was heard most recently. A
        # name's steps are its individuals' steps, read through the label's index
        held = self.nodes(node[2:]) if node.startswith("n:") else [node]
        out += [(label, -1, f"e:{e}") for e, label in self.db.execute(
            f"SELECT event, label FROM edges WHERE node IN ({','.join('?' * len(held))}) "
            "ORDER BY event DESC", held)]
        return out

    def _event(self, node: str) -> tuple[str, int, str]:
        got = self._events.get(node)
        if got is None:
            got = self._events[node] = self.db.execute(
                "SELECT lemma, turn, mood FROM events WHERE id = ?", (int(node[2:]),)).fetchone()
        return got

    def event(self, node: str) -> tuple[str, int]:
        lemma, turn, _ = self._event(node)
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
                    "'prep:%' AND node NOT LIKE 'f:%'", (e,)).fetchall())

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
        return self._event(node)[2]

    def paths(self, start: str, goal: str, limit: int = 8, avoid: set | None = None,
              most: int | None = None) -> list[list]:
        """The shortest paths from one node to another, each a list of nodes and steps
        alternating, at most `limit` steps, the first `most` of them in the order the
        nodes' steps are kept. Distances come first, from a search that copies no path;
        then only the ways along which each node is at its distance are followed, so a
        hub that joins every story costs its steps once, not once a path through it."""
        dist, layer, best = {start: 0}, [start], None
        for depth in range(1, limit + 1):
            nxt_layer = []
            for node in layer:
                for _, _, nxt in self.around(node):
                    if self.is_(nxt, goal):
                        best = depth
                    elif not self.among(nxt, dist) and not (avoid and self.among(nxt, avoid)):
                        dist[nxt] = depth
                        nxt_layer.append(nxt)
            if best is not None or not nxt_layer:
                break
            layer = nxt_layer
        if best is None:
            return []
        found: list[list] = []
        # nodes a walk left with nothing found: every way on from them is a dead end,
        # since a node's distance, and so where a walk may go from it, is fixed
        dead: set = set()

        def walk(path: list, depth: int) -> None:
            had = len(found)
            for label, direction, nxt in self.around(path[-1]):
                if most is not None and len(found) >= most:
                    return
                if self.is_(nxt, goal):
                    if depth + 1 == best:
                        found.append(path + [(label, direction), nxt])
                elif depth + 1 < best and dist.get(nxt) == depth + 1 and nxt not in dead:
                    walk(path + [(label, direction), nxt], depth + 1)
            if len(found) == had:
                dead.add(path[-1])

        walk([start], 0)
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
                        # the words before it are a name of their own where the graph
                        # knows them: 'Who is Silfem cousins with?' is parsed as one
                        # compound, 'Silfem cousins', as 'the Smith cousins' would be
                        head = " ".join(words[:i])
                        if head and self.known(head) and (
                                h := question.lower().find(head, a)) >= 0 and h < at:
                            spans.append((h, h + len(head), head))
                        a, b, name = at, at + len(tail), tail
                    break
            # 'the person who repairs clocks': a name's own verb, said just before it, is
            # cut with it, as the taught arm's `joined` does, so every trade is one shape
            held = self.nodes(name)
            if verb and self.db.execute(
                    "SELECT 1 FROM edges JOIN events ON events.id = edges.event WHERE "
                    f"edges.node IN ({','.join('?' * len(held))}) AND events.lemma = ? "
                    "LIMIT 1", (*held, verb)).fetchone():
                a = verb_at
            spans.append((a, b, name))
        out, at = "", 0
        for i, (a, b, _) in enumerate(spans):
            out += question[at:a] + f"<{i}>"
            at = b
        return out + question[at:], spans

    def proper_at(self, text: str, at: int) -> bool:
        """Whether the word a text has at this offset is a proper name, by the part of
        speech the parse gives it: a capital letter is a mark of some scripts only."""
        return any(r[0] <= at < r[0] + len(r[1]) and r[5] == "PROPN" for r in self.spans(text))

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
                 if self.slot(template, i, n, self.proper_at(question, a))]
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
                for way in self.paths(node, f"n:{name}", limit=3, avoid=set(nodes),
                                      most=1):
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
        return any(self.is_(n, goal) for n in here)

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
                    if self.among(nxt, path[::2]):
                        continue
                    way = path + [(label, direction), nxt]
                    if not nxt.startswith("e:"):
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
                # a hub ('Lily' in a thousand stories) is walked through its latest
                # `REACH` steps of the label, as recall searches the recent first
                ways = [(lab, d, nxt) for lab, d, nxt in self.around(here)
                        if lab == label and d == direction][:REACH]
                for lab, d, nxt in ways:
                    if self.among(nxt, nodes):
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
            if not end.startswith("e:"):
                # the most recent thing that happened to the first name first, then what
                # followed from it: the football's last event, then its carrier's move
                out.append((self.describe(end), turns))
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
            return lower if tag == "ask" else lemma

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

    def wh_word(self, question: str) -> str:
        """The question's first word that asks, by what the parse marks, or ''."""
        return next((t[0] for t in self.tokens(question) if t[2] == "ask"), "")

    def tokens(self, question: str) -> list:
        """A question's parse, one row a token: its word, lemma, tag, link and head."""
        if question not in self._tokens:
            key = hashlib.sha256(f"{VERSION}|{self.model}|tokens-2|{question}".encode()
                                 ).hexdigest()
            kept = self._kept(key)
            if kept is None:
                kept = [[t.lower_, t.lemma_.lower(), "ask" if asking(t) else "", t.dep_,
                         t.head.i]
                        for t in parse(self.model, question)]
                self._keep(key, kept)
            self._tokens[question] = kept
        return self._tokens[question]

    def taught(self) -> list[tuple[str, set]]:
        """Every taught shape's plans' signatures, rebuilt after a lesson."""
        if self._sigs is None:
            self._sigs = []
            for shape, plan in self.db.execute(
                    "SELECT shape, plan FROM learnt WHERE hits > misses").fetchall():
                sig = json.loads(plan).get("sig")
                if sig:
                    self._sigs.append((shape, set(sig)))
        return self._sigs

    def nearest(self, question: str, fillers: list[str],
                skip: str | None = None) -> tuple[float, list[str]]:
        """The taught shapes nearest a question no lesson was worded as: those of its
        number of slots whose signature overlaps the question's most, and by how much."""
        mine = set(self.signature(question, fillers))
        best, shapes = 0.0, []
        for shape, sig in self.taught():
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

    def reaching(self, question: str, skip: str, tries: int = 60) -> list:
        """Where the nearest shapes reach nothing, every taught shape with as many slots
        as the question names known things, nearest first, read under each order of
        those names: the first whose plans reach anything is the relation asked, a count
        never, since a count of anything is a number. Last of all, after taking the
        question apart, which reads it more closely. Two
        wordings are one relation where the house holds a solution for both: 'What is
        the number of X in the Y?' shares little grammar with 'How many X are in the
        Y?' and the house relates X and Y by little else."""
        from itertools import combinations, permutations

        names = [n for _, _, n in self.template(question)[1] if self.known(n)][:4]
        readings = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                for order in permutations(chosen):
                    mine = set(self.signature(question, list(order)))
                    readings += [(len(mine & sig) / len(mine | sig), list(order), shape)
                                 for shape, sig in self.taught()
                                 if shape != skip and shape.count("<") == k]
        readings.sort(key=lambda r: -r[0])
        for _, fillers, shape in readings[:tries]:
            ranked = [(p,) for (p,) in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses "
                "ORDER BY hits - misses DESC, hits DESC", (shape,)) if not json.loads(p).get("count")]
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
                return [(self.say_number(len({e for e, _ in ends})), max(t for _, t in ends))]
            # nothing left to count is an answer, resting on what emptied the set: 'Mary
            # dropped the football' is later than her picking it up
            return [(self.say_number(0), (self.emptied,))] if self.emptied else []
        return [f for f in ends if not said_in(f[0], question)]

    def teach(self, question: str, answer: str) -> None:
        want = answer.lower()
        # what the question's wh-word asked for, by the mark the answer is heard with
        if (wh := self.wh_word(question)) and (mark := self.mark(want)):
            self.db.execute("INSERT INTO asks VALUES (?, ?, 1) ON CONFLICT(wh, mark) DO "
                            "UPDATE SET n = n + 1", (wh, mark))
        # where each word sat is counted as heard, so a word held in its place is frame
        self.heard_at(question)
        put = self.alias(question, want)
        if put != question:
            self.heard_at(put)
        question = put
        self._sigs = None
        shape, fillers = self.shape(question)
        for rowid, plan in self.db.execute("SELECT rowid, plan FROM learnt WHERE shape = ?",
                                           (shape,)).fetchall():
            plan = json.loads(plan)

            def holds(present: bool | None) -> bool | None:
                found = (self.said(plan, fillers, True, question, present)
                         or self.said(plan, fillers, False, question, present))
                if not found:
                    return None
                said = max(found, key=lambda f: f[1])[0]
                return said_in(want, said) or (self.number(want) is not None
                                               and self.number(said) == self.number(want))

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
        self.learn_circumstance(question, want)
        if not fillers:
            self.db.commit()
            return
        if not goals and not want.isdigit():
            # an answer nothing heard holds may be a number word, whose worth is learnt
            self.heard_count(shape, fillers, want)
        # only the first twenty of the shortest are ever made plans
        found = [p for g in goals[:5] for p in self.paths(f"n:{fillers[0]}", g, most=20)]
        shortest = min((len(p) for p in found), default=0)
        plans = [self.plan_of(p, fillers[1:]) for p in found if len(p) == shortest][:20]
        if not any(plans) and self.number(want) is not None:
            # a number no telling said is a number of things: 'How many people keep
            # things in the pantry?' taught 3
            plans = self.counted(fillers, self.number(want))
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

    # -- words never heard ----------------------------------------------------

    def unheard(self, question: str) -> list[tuple[int, int, str]]:
        """The question's common nouns the conversation never used, no ending of them
        either: 'the chipped dishes' where only cracked plates were told of. A proper
        name is never one, since a person never mentioned is someone nobody told of; nor
        is a word lessons held in its place every time ('do all day'), which is frame."""
        template, spans = self.template(question)
        out = []
        for i, (a, b, name) in enumerate(spans):
            words = name.split()
            if self.proper_at(question, a):
                continue
            if any(self.known(" ".join(words[j:])) for j in range(len(words))):
                continue
            seen = self.db.execute("SELECT filler, n FROM positions WHERE template = ? AND"
                                   " pos = ?", (template, i)).fetchall()
            if len(seen) == 1 and seen[0][1] >= 2:
                continue
            out.append((a, b, name))
        return out

    def aliases(self, word: str) -> list[str]:
        """The heard name a word stood for in lessons, if one: two facts' worth or more,
        since one fact cannot show two wordings agree, twice any other name's share, and
        found in more than half the lessons that found anything for the word. A word
        that is the question's frame ('shade', 'a wage') fits many names and none that
        often. The row named '' counts those lessons."""
        rows = self.db.execute("SELECT name, hits, found FROM aliases WHERE word = ? "
                               "ORDER BY name = '', hits DESC", (word,)).fetchall()
        tried = next((f for n, _, f in rows if n == ""), 0)
        rows = [r for r in rows if r[0] != ""]
        if not rows:
            return []
        name, hits, found = rows[0]
        second = rows[1][1] if len(rows) > 1 else 0
        return [name] if hits >= 2 and hits >= 2 * second and 2 * found > tried else []

    def unaliased(self, question: str, heard: bool = False) -> str:
        """The question with each word never heard put as the name it stood for, where
        lessons settled one and that name is heard here, and with `heard` where this
        conversation's hearings of it pinned one or MiniLM voted for one."""
        out, at = "", 0
        for a, b, w in self.unheard(question):
            # by its longest ending lessons settled: 'chipped dishes' before 'dishes'. A
            # name the question says already is never put again
            ws = w.split()
            for i in range(len(ws)):
                tail = " ".join(ws[i:])
                name = next((n for n in self.aliases(tail) if self.known(n)
                             and not said_in(n, out + question[at:])), None)
                if name is not None:
                    out += question[at:b - len(tail)] + name
                    at = b
                    break
            else:
                # no lesson settled it: what this conversation's hearings of it left
                name = (self.pinned(w) or self.voted(w)) if heard else None
                if name is not None and self.known(name) and not said_in(
                        name, out + question[at:]):
                    out += question[at:a] + name
                    at = b
        return out + question[at:]

    def readings(self, question: str, spans: list) -> list[tuple[float, tuple, list]]:
        """Every reading of a question holding words never heard, each name a slot or
        frame and the slots in any order, nearest taught shapes with each. A slot left
        unbound claims less than one bound, so the readings with fewest unbound slots go
        first, nearest within that; each word is counted by the first reading whose
        solving finds anything for it, as borrowing reads a question."""
        from itertools import combinations, permutations

        names = [n for _, _, n in self.template(question)[1]][:4]
        out = []
        for k in range(1, len(names) + 1):
            for chosen in combinations(names, k):
                if not any(w in chosen for _, _, w in spans):
                    continue
                for order in permutations(chosen):
                    near, shapes = self.nearest(question, list(order))
                    if shapes:
                        out.append((near, order, shapes))
        out.sort(key=lambda r: (sum(not self.known(n) for n in r[1]), -r[0]))
        return out

    def heard_in(self, question: str) -> None:
        """A question holding a word never heard is one context of it, answered or not:
        the names its slot could take given the question's other names, by the plans of
        the nearest taught shape with the answer left free. Each hearing keeps only the
        names every earlier one allowed, as a child narrows a new word over the
        situations it is heard in."""
        spans = self.unheard(question)
        if not spans:
            return
        names = [n for _, _, n in self.template(question)[1]][:4]
        unheard = {u for _, _, u in spans}
        done: set[str] = set()
        for _, order, shapes in self.readings(question, spans):
            plans = [p for sh in shapes for (raw,) in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (sh,))
                if not (p := json.loads(raw)).get("count")]
            for free, w in enumerate(order):
                if w not in unheard or w in done:
                    continue
                bound = {i: n for i, n in enumerate(order) if i != free and self.known(n)}
                if bound:
                    here = {e for p in plans for e, _ in self.solve(p, bound, free)}
                else:
                    here = {n for n in self.names()
                            if any(self.solve(p, {free: n}, "a") for p in plans)}
                here -= set(names)
                if not here:
                    continue
                done.add(w)
                row = self.db.execute("SELECT names, heard FROM contexts WHERE word = ?",
                                      (w,)).fetchone()
                if row:
                    here &= set(json.loads(row[0]))
                self.db.execute("INSERT OR REPLACE INTO contexts VALUES (?, ?, ?)",
                                (w, json.dumps(sorted(here)), (row[1] if row else 0) + 1))
        self.db.commit()

    def pinned(self, word: str) -> str | None:
        """The one name every hearing of a word left, where two or more agreed: one
        hearing cannot show a word names something rather than being frame, as one fact
        cannot settle an alias. Hearings that left nothing in common mark a word of the
        frame ('shade', 'a wage'), which names nothing and is never pinned."""
        row = self.db.execute("SELECT names, heard FROM contexts WHERE word = ?",
                              (word,)).fetchone()
        left = json.loads(row[0]) if row else []
        return left[0] if len(left) == 1 and row[1] >= 2 else None

    def voted(self, word: str) -> str | None:
        """Of the names a word's hearings still allow, the one MiniLM puts it nearest,
        where that is nearer than the next by `MARGIN`: what everyone knows of a word
        decides at its first hearing, and the hearings decide once they leave one name. A
        word of the frame ('shade') is about as near every name it allows, so it is never
        voted for any."""
        row = self.db.execute("SELECT names FROM contexts WHERE word = ?",
                              (word,)).fetchone()
        left = json.loads(row[0]) if row else []
        if len(left) < 2:
            return None
        w, *vs = vectors([word, *left])
        near = sorted(((float(w @ v), n) for v, n in zip(vs, left)), reverse=True)
        return near[0][1] if near[0][0] - near[1][0] >= MARGIN else None

    def alias(self, question: str, want: str) -> str:
        """A lesson naming words never heard: each is read as a slot of the nearest
        taught shape, and that shape's plans are solved with the answer bound and the
        word's slot free. Two wordings are one name where their solutions agree, so each
        name found counts a share of one, and a lesson that many names fit says little.
        A lesson that found other names counts against one. The question comes back
        with whatever lessons have settled put in."""
        spans = self.unheard(question)
        goals = self.holding(want)[:1]
        if not spans or not goals:
            return question
        names = [n for _, _, n in self.template(question)[1]][:4]
        unheard = {u for _, _, u in spans}
        found: dict[str, set] = {}
        for near, order, shapes in self.readings(question, spans):
            plans = [json.loads(p) for sh in shapes for (p,) in self.db.execute(
                "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (sh,))]
            for free, w in enumerate(order):
                if w not in unheard or found.get(w):
                    continue
                bound = {i: n for i, n in enumerate(order) if i != free and self.known(n)}
                bound["a"] = goals[0][2:]
                # a name the question says is never what another of its words stands for
                found[w] = {e for plan in plans if not plan.get("count")
                            for e, _ in self.solve(plan, bound, free) if e not in names}
        # a name two of the words found is not evidence for either
        shared = [n for w, ends in found.items() for n in ends
                  if any(n in other for v, other in found.items() if v != w)]
        found = {w: ends - set(shared) for w, ends in found.items()}
        for w, ends in found.items():
            # one fact asked of several times is one piece of evidence
            if (w, want) in self.aliased:
                continue
            self.aliased.add((w, want))
            # credited to every ending of the word, so 'wax forms' learns from 'many wax
            # forms'
            if not ends:
                continue
            for tail in (" ".join(w.split()[i:]) for i in range(len(w.split()))):
                for n, share in [(n, 1 / len(ends)) for n in ends] + [("", 0)]:
                    self.db.execute("INSERT INTO aliases VALUES (?, ?, ?, 1) ON CONFLICT("
                                    "word, name) DO UPDATE SET hits = hits + excluded.hits, "
                                    "found = found + 1", (tail, n, share))
        return self.unaliased(question)

    def answer(self, question) -> str:
        # what the plans of either wording's own shape find comes before anything
        # borrowed or joined: a wording taught before an alias settled holds the lessons
        self.heard_in(question.text)
        put = self.unaliased(question.text)
        said, self.spent = None, 0
        try:
            said = next((self.latest(found) for text in dict.fromkeys((put, question.text))
                         if (found := self.answers(*self.shape(text), text, borrow=False))),
                        None)
            if said is None:
                said = self.answered(put)
            # a pin is used only where the question as heard finds nothing: frame words a
            # question's names keep company with agree across hearings too ('What is the
            # number of X in the cellar?' pinned 'number'), and they broke answers found
            again = self.unaliased(question.text, heard=True) if said is None else put
            if again != put:
                said = self.answered(again)
        except Spent:
            pass
        given_up, self.spent = self.spent > EFFORT, None
        # what the plans found from outside focus is another conversation's, as often as
        # not; what is in focus and fits the asked slot comes first
        planned, fit = said, self.focused(question.text)
        if said is None or not self.in_focus(said):
            said = fit or said
        # what the episode says outright, read from the question's grammar, comes first
        match = self.matched(question.text)
        said = match or said
        self.last_notes = (["(gave up)"] if given_up else []) + (
            ["(nothing)"] if said is None else ["(found)"]) + [
            f"plans:{planned}", f"focus:{fit}", f"match:{match}", "by:" + (
                "none" if said is None else "match" if said == match else
                "plans" if said == planned else "focus")]
        return "I don't know." if said is None else said

    def now(self) -> int:
        return self.db.execute("SELECT COALESCE(MAX(turn), 0) FROM events").fetchone()[0]

    def episode(self) -> int:
        """The turn of the last break in the text: what was heard after it is in focus."""
        return self.db.execute("SELECT COALESCE(MAX(turn), -1) FROM boundaries").fetchone()[0]

    def in_focus(self, name: str) -> bool:
        held = self.nodes(name)
        return self.db.execute(
            "SELECT 1 FROM edges JOIN events ON events.id = edges.event WHERE edges.node IN "
            f"({','.join('?' * len(held))}) AND events.turn > ? LIMIT 1",
            (*held, self.episode())).fetchone() is not None

    def blank(self, question: str) -> tuple[str, str] | None:
        """The slot the question's wh-word stands in: ('eat', 'dobj') for 'Lily ate
        what?', ('land', 'prep:on') for 'The bird landed on what?'."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|blank-3|{question}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            kept = list(_slot(parse(self.model, question)) or [])
            self._keep(key, kept)
        return tuple(kept) if kept else None

    def pattern(self, question: str) -> tuple[str, list[str], list] | None:
        """The question read as an event with one slot free: its verb's lemma as a telling's
        event is named, the links the wh-word could stand in, and the names it is bound to,
        as (label, name). 'Who loved her veil?' is a 'love' event with 'nsubj' free and
        ('dobj', 'veil') bound; 'Where did Roxy put the leaves?' is a 'put' event with any
        link of place free. A pronoun binds nothing, since a question's pronoun names no
        one of its own."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|pattern-6|{question}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            kept = []
            doc = parse(self.model, question)
            wh = next((t for t in doc if asking(t)), None)
            if wh is not None:
                case = [k.lower_ for k in wh.children if k.dep_ == "case"]
                if any(c.dep_ == "cop" for c in wh.children):
                    # the word that asks is a copula's predicate ('Who is cousins with
                    # Ann?'): a copula equates its two sides, so it stands in either
                    verb, free = wh, ["nsubj", "attr"]
                elif wh.pos_ == "ADV":
                    # a word that asks for a circumstance ('where', 'when') stands in an
                    # oblique; which ones its answers hang by is learnt (`circumstances`)
                    verb, free = wh.head, ["prep:*"]
                elif case:
                    verb, free = wh.head, ["prep:" + " ".join(case)]
                elif wh.dep_ != "det":
                    # an object asked for is either of the core objects the parse tells
                    # apart ('told the eye', 'asked his mommy' are read as indirect), the
                    # direct first
                    verb, free = wh.head, (["obj", "iobj"] if wh.dep_ == "obj" else [wh.dep_])
                else:
                    verb, free = None, []
                cop = next((c for c in verb.children if c.dep_ == "cop"), None) if verb \
                    is not None else None
                if verb is not None and (verb.pos_ in ("VERB", "AUX") or cop is not None):
                    lemma = (cop or verb).lemma_.lower()
                    mood_ = mood(verb)
                    bound = []
                    if cop is not None and verb is not wh:
                        vcase = [k.lower_ for k in verb.children if k.dep_ == "case"]
                        bound.append(["prep:" + " ".join(vcase) if vcase else (
                            "acomp" if verb.pos_ == "ADJ" else "attr"), phrase(verb)])
                    for c in verb.children:
                        if c is wh or c.pos_ == "PRON":
                            continue
                        if c.dep_ not in SKIP and c.pos_ in ("NOUN", "PROPN"):
                            bound.append([link(c), phrase(c)])
                    # every name inside the verb's own arguments, however deep: 'with
                    # Nogael' in 'Who is cousins with Nogael?' hangs off 'cousins', not the
                    # verb. Another clause ('and he puffed', 'which was his dog house') is
                    # another event and binds nothing here
                    named, todo = set(), [c for c in verb.children if c.dep_ not in CLAUSES]
                    while todo:
                        t = todo.pop()
                        if t.pos_ in ("NOUN", "PROPN") and t.dep_ not in DESCRIBE:
                            named.add(phrase(t))
                        todo += [c for c in t.children if c.dep_ not in CLAUSES]
                    named = sorted(named)
                    kept = [lemma, free, bound, named, mood_, wh.lemma_.lower()]
            self._keep(key, kept)
        return (kept[0], kept[1], [tuple(b) for b in kept[2]], kept[3], kept[4], kept[5]) \
            if kept else None

    def matched(self, question: str) -> str | None:
        """The question's pattern matched against this episode: the latest event with the
        question's lemma that holds every name the question binds it to, read at the free
        slot. A question needs no lesson in its wording, as a plan's shape does; what it
        asks is in its grammar."""
        got = self.pattern(question)
        if got is None:
            return None
        lemma, free, bound, named, mood, wh = got
        # the links lessons showed come first, then any oblique: a lesson ranks where a
        # circumstance is found, and never rules out one it has not shown
        free = self.circumstances(wh) + ["prep:*"] if free == ["prep:*"] else free

        def rank(label: str) -> int | None:
            for r, f in enumerate(free):
                if label == f or (f == "prep:*" and label.startswith("prep:")
                                  and ">" not in label):
                    return r
            return None

        for (eid,) in self.db.execute(
                "SELECT id FROM events WHERE lemma = ? AND mood = ? AND turn > ? ORDER BY id "
                "DESC", (lemma, mood, self.episode())).fetchall():
            # a possessed noun ('her veil') is an event of its own whose `self` is the
            # name, so an argument is read through it
            edges = [(label, n) for label, node in self.db.execute(
                "SELECT label, node FROM edges WHERE event = ? AND node NOT LIKE 'f:%'",
                (eid,)).fetchall()
                for n in ([n for lab, d, n in self.around(node)
                           if lab == "self" and d == 1] if node.startswith("e:")
                          else [node])]
            # a copula equates its two sides, so where the question asks for either, a
            # name bound to one is found on the other as well
            either = {"nsubj", "attr"} if set(free) == {"nsubj", "attr"} else set()
            if not all(any((lab == label or {lab, label} <= either)
                           and self.is_(n, f"n:{name}") for lab, n in edges)
                       for label, name in bound):
                continue
            # each other name the question says is two steps from the event or nearer
            near = {n for _, _, a in self.around(f"e:{eid}") for n in
                    [a] + ([b for _, d, b in self.around(a) if d == 1]
                           if a.startswith("e:") else [])}
            if not all(any(self.is_(x, f"n:{n}") for x in near) for n in named):
                continue
            best = None
            for label, node in edges:
                r = rank(label)
                if r is None or node.startswith("e:") or any(
                        self.is_(node, f"n:{name}") for _, name in bound):
                    continue
                said = self.describe(node)
                if not said_in(said, question) and (best is None or r < best[0]):
                    best = (r, said)
            if best is not None:
                return best[1]
        return None

    def focused(self, question: str) -> str | None:
        """The name in this episode that best fits the asked slot: how strongly it is in focus,
        each hearing fading as ACT-R's base level does, times how often it has filled the
        asked verb's slot, out of everything it has filled, times how often the wh-word's
        answers bore its mark."""
        scores = self.focus(question)
        if not scores:
            return None
        return max((a * f * w, n) for n, (a, f, w) in scores.items())[1]

    def focus(self, question: str) -> dict[str, tuple[float, float, float]] | None:
        """Each name focus weighs for a question, with its three factors: how strongly it
        is in focus, how it fits the asked slot, and how often the wh-word asks for its
        mark. None where the question is not guessed at."""
        slot = self.blank(question)
        # a question about someone never mentioned is not guessed at
        if slot is None or not all(self.known(f) for f in self.shape(question)[1]):
            return None
        now = self.now()
        act: dict[str, float] = {}
        for node, turn in self.db.execute(
                "SELECT edges.node, events.turn FROM edges JOIN events ON events.id = "
                "edges.event WHERE edges.node NOT LIKE 'e:%' AND edges.node NOT LIKE 'f:%' "
                "AND events.turn > ?", (self.episode(),)):
            act[node] = act.get(node, 0.0) + (now - turn + 1) ** -DECAY
        labels = self.db.execute("SELECT COUNT(DISTINCT label) FROM edges WHERE node NOT "
                                 "LIKE 'f:%'").fetchone()[0] or 1
        kind, slots, filled = self.concepts()

        def fit(name: str) -> float:
            # a share of what the name filled: in the asked verb's slot, and, much less,
            # by the link alone, so a slot no one here filled still ranks. A name heard
            # little fits as its kind does until its own hearings decide
            k = kind.get(name)
            prior = slots[(k, *slot)] / filled[k] if filled[k] else 0.0
            held = self.nodes(name)
            rows = self.db.execute(
                "SELECT edges.label, events.lemma = ?, COUNT(*) FROM edges JOIN events ON "
                f"events.id = edges.event WHERE edges.node IN ({','.join('?' * len(held))})"
                " GROUP BY 1, 2", (slot[0], *held)).fetchall()
            total = sum(n for _, _, n in rows)
            here = sum(n for lab, verb, n in rows if lab == slot[1] and verb)
            link = sum(n for lab, _, n in rows if lab == slot[1])
            return ((here + PRIOR * prior) / (total + PRIOR)
                    + 0.1 * (link + 0.5) / (total + 0.5 * labels))

        asks = dict(self.db.execute("SELECT mark, n FROM asks WHERE wh = ?",
                                    (self.wh_word(question),)).fetchall())

        def asked_for(name: str) -> float:
            # how often this wh-word's answers bore the name's mark, as lessons have it
            mark = self.mark(name)
            return 0.5 if mark is None else (asks.get(mark, 0) + 1) / (sum(asks.values()) + 2)

        # in mind as an individual, said by its description; how it fits and what it
        # is asked for are what everyone knows, so they are read by its label
        out: dict[str, tuple[float, float, float]] = {}
        for node, a in act.items():
            said, name = self.describe(node), self.label(node)
            if said_in(said, question):
                continue
            was = out.get(said)
            out[said] = (a + (was[0] if was else 0.0), fit(name), asked_for(name))
        return out

    def mark(self, name: str) -> str | None:
        """How a name is heard: the part of speech its mentions were given most ('PROPN'
        for Lily, 'NOUN' for the ball), or None where nothing was heard of it by that
        name. A capital letter is a mark of some scripts only."""
        if name not in self._marks:
            self._marks[name] = self._mark(name)
        return self._marks[name]

    def _mark(self, name: str) -> str | None:
        row = self.db.execute("SELECT pos FROM heard_as WHERE name = ? ORDER BY n DESC "
                              "LIMIT 1", (name.split()[-1] if name.strip() else "",)
                              ).fetchone()
        return row[0] if row else None

    def circumstances(self, wh: str) -> list[str]:
        """The links a word asking for a circumstance ('where', 'when') has had its
        answers hang by, learnt from lessons, as the parse marks only that it asks, the
        commonest first. `matched` tries them first and any other oblique after."""
        got = [lab for (lab,) in self.db.execute(
            "SELECT link FROM circumstances WHERE wh = ? ORDER BY n DESC", (wh,))]
        return got

    def learn_circumstance(self, question: str, want: str) -> None:
        """A lesson's answer to a word asking for a circumstance: the oblique the answer
        hangs by in the episode's event the question matches."""
        got = self.pattern(question)
        if got is None or got[1] != ["prep:*"]:
            return
        lemma, _, bound, _, mood_, wh = got
        for (eid,) in self.db.execute(
                "SELECT id FROM events WHERE lemma = ? AND mood = ? AND turn > ? ORDER BY "
                "id DESC LIMIT 50", (lemma, mood_, self.episode())).fetchall():
            edges = self.db.execute("SELECT label, node FROM edges WHERE event = ? AND node "
                                    "NOT LIKE 'f:%'", (eid,)).fetchall()
            if not all(any(lab == label and self.is_(n, f"n:{name}") for lab, n in edges)
                       for label, name in bound):
                continue
            for lab, n in edges:
                if lab.startswith("prep:") and not n.startswith("e:") and said_in(
                        want, self.describe(n)):
                    self.db.execute("INSERT INTO circumstances VALUES (?, ?, 1) ON CONFLICT"
                                    "(wh, link) DO UPDATE SET n = n + 1", (wh, lab))
                    return

    # -- numbers ---------------------------------------------------------------

    def number(self, text: str) -> int | None:
        """A number said as digits, or as a word whose value lessons settled."""
        t = text.strip().lower()
        if t.isdigit():
            return int(t)
        return self.numbers().get(t)

    def say_number(self, n: int) -> str:
        """A number in the word lessons said it with, or as digits."""
        return next((w for w, v in self.numbers().items() if v == n), str(n))

    def numbers(self) -> dict[str, int]:
        """What each number word is worth, learnt as a child learns to count: over the
        lessons whose answer was a word nothing heard holds, one walk's count went with
        each word, each word always with the same count and each count with the same
        word. That walk is the counting, and the words' values are its counts. Two words
        at least, since one cannot show a pairing."""
        total = self.db.execute("SELECT COUNT(*) FROM counts").fetchone()[0]
        if getattr(self, "_numbers", (None,))[0] == total:
            return self._numbers[1]
        by: dict = {}
        for shape, walk, word, size in self.db.execute("SELECT * FROM counts"):
            by.setdefault((shape, walk), []).append((word, size))
        best: tuple = (0, {})
        for items in by.values():
            fwd: dict = {}
            back: dict = {}
            if all(fwd.setdefault(w, n) == n and back.setdefault(n, w) == w
                   for w, n in items) and len(fwd) >= 2 and len(items) > best[0]:
                best = (len(items), fwd)
        self._numbers = (total, best[1])
        return best[1]

    def heard_count(self, shape: str, fillers: list[str], want: str) -> None:
        """A lesson whose answer may be a number word: every walk from the question's
        first name, with how many it reached, and none for a walk this shape had before
        that reaches nothing now."""
        ways = self.walks(f"n:{fillers[0]}")
        rows = [(shape, key, want, len(ends)) for key, (ends, _) in ways.items()]
        rows += [(shape, key, want, 0) for (key,) in self.db.execute(
            "SELECT DISTINCT walk FROM counts WHERE shape = ?", (shape,)) if key not in ways]
        self.db.executemany("INSERT INTO counts VALUES (?, ?, ?, ?)", rows)

    def answered(self, text: str, depth: int = 0) -> str | None:
        """An answer from the plans of the question's shape, or, where they find nothing,
        from the question taken apart: its innermost noun phrase holding a name is asked
        as a question of its own ('Who is Kroudroth's cousin?'), its answer put in its
        place, and what is left asked again. Plans are whole paths and do not compose; a
        question's grammar does."""
        shape, fillers = self.shape(text)
        found = self.answers(shape, fillers, text, borrow=False)
        if found:
            return self.latest(found)
        if depth < 3 and (said := self.joined(text, depth)) is not None:
            return said
        found = self.answers(shape, fillers, text)
        if found:
            return max(found, key=lambda f: f[1])[0]
        # borrowed before taken apart, by the first house: taking 'X's lanterns' apart
        # first read 0.756 0.784 0.779 against 0.760 0.788 0.809
        if depth >= 3 or not all(self.known(f) for f in fillers):
            return None
        said = self.apart(text, depth)
        if said is None and depth == 0 and (found := self.reaching(text, shape)):
            said = self.latest(found)
        if said is None and depth == 0 and (found := self.kinded(text, shape, fillers)):
            said = self.latest(found)
        return said

    @staticmethod
    def latest(found: list) -> str:
        return max(found, key=lambda f: f[1])[0]

    def roles(self, shapes: list[str]) -> set[str]:
        """The links an answer hangs by at the end of these shapes' plans: 'prep:in' for
        where a thing is kept, 'nummod' for how many. A name's kind is the links it is
        held by, so an answer held by none of these is of another kind."""
        out = set()
        for shape in shapes:
            for (p,) in self.db.execute(
                    "SELECT plan FROM learnt WHERE shape = ? AND hits > misses", (shape,)):
                plan = json.loads(p)
                if plan["steps"] and not plan.get("count") and plan["steps"][-1][1] == 1:
                    out.add(plan["steps"][-1][0])
        return out

    def kinded(self, text: str, shape: str, fillers: list[str],
               limit: int = 6) -> list[tuple[str, tuple]]:
        """Where no plan reaches, a fact told in a shape no lesson's fact was: the
        shortest path from the question's first name to a name in the role the answer
        plays at the end of the shape's plans, through nothing that did not happen, with
        each other name of the question two steps or fewer from the path. 'Gopaiff has the
        fishing floats tucked away in the dairy' has the dairy at the end of a 'prep:in'
        as 'keeps the floats in the dairy' has, by a longer way."""
        if not fillers:
            return []
        roles = self.roles([shape]) or self.roles(self.nearest(text, fillers)[1])
        if not roles:
            return []
        start = f"n:{fillers[0]}"
        frontier, seen, found = [[start]], {start}, []
        for _ in range(limit):
            nxt = []
            for path in frontier:
                for label, direction, node in self.around(path[-1]):
                    if self.among(node, seen):
                        continue
                    if node.startswith("e:") and self.mood(node):
                        continue
                    way = path + [(label, direction), node]
                    if (not node.startswith("e:") and direction == 1 and label in roles
                            and not said_in(self.describe(node), text) and all(
                                any(self.paths(n, f"n:{f}", limit=2, most=1) for n in way[::2]
                                    if n.startswith("e:")) for f in fillers[1:])):
                        turns = [self.event(n)[1] for n in way[::2] if n.startswith("e:")]
                        found.append((self.describe(node), (max(turns, default=0),)))
                    nxt.append(way)
            if found:
                return found
            for way in nxt:
                seen.add(way[-1])
            frontier = nxt[:2000]
        return []

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
                for node in self.around_as(got[here], lab, way):
                    if self.among(node, set(got.values())):
                        continue
                    if there in want and not self.is_(node, want[there]):
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
            if not end or end.startswith("e:") or len(got) < len(
                    {n for e in edges for n in e[:2]}):
                continue
            turns = [self.event(n)[1] for n in got.values()
                     if n.startswith("e:") and not self.mood(n)]
            out.append((self.describe(end), (max(turns, default=0),)))
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
            if not any(rows[k][3] in ("nmod:poss", "acl:relcl", "acl", "nmod")
                       for k in kids.get(i, [])):
                continue
            span = sorted(subtree(i))
            # the question's own wh-word, not a relative one ('the person who repairs')
            if span[0] == 0:
                continue
            if len(span) >= len(rows) - 2 or not any(
                    rows[k][5] == "PROPN" or self.known(rows[k][1].lower()) for k in span
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
                    for t in parse(self.model, text)]
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
        about_world = any(label.startswith("nsubj") and t.startswith("n:")
                           for ev in self.read(text) for label, t in ev["edges"])
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
        key = hashlib.sha256(f"{VERSION}|{self.model}|reaction-2|{text}".encode()).hexdigest()
        rows = self._kept(key)
        if rows is None:
            doc = parse(self.model, text)
            # one row a token: no, whether it may name something, its name, and whether
            # it is a number of things read as the arm reads one ('none')
            rows = []
            for t in doc:
                no = negates(t)
                part = t.dep_ in DESCRIBE and t.head.pos_ in ("NOUN", "PROPN")
                number = "Card" in feature(t, "NumType") or t.lower_.isdigit()
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

    def export(self) -> dict:
        return {table: self.db.execute(f"SELECT * FROM {table}").fetchall()
                for table in CARRIED}

    def dials(self) -> dict:
        n_events, n_edges = (self.db.execute("SELECT COUNT(*) FROM events").fetchone()[0],
                             self.db.execute("SELECT COUNT(*) FROM edges").fetchone()[0])
        individuals = self.db.execute("SELECT COUNT(DISTINCT node) FROM called").fetchone()[0]
        return {"model": self.model, "version": VERSION, "events": n_events, "edges": n_edges,
                "individuals": individuals,
                "shapes": self.db.execute("SELECT COUNT(DISTINCT shape) FROM learnt")
                .fetchone()[0], "parsed": self.parsed}

    def close(self) -> None:
        self.db.close()
        if hasattr(self, "_cdb"):
            self._cdb.close()


def _slot(doc) -> tuple[str, str] | None:
    """The verb and link the word asking for a thing fills (a pronoun that asks, by
    `PronType`). One joined to another by 'and' fills the other's slot ('Tom and what had
    fun' is a subject of 'have'), and an oblique is named by its case word ('The bird
    landed on what?' is 'land' 'prep:on'). A predicate with a copula is the copula's."""
    t = next((t for t in doc if asking(t) and t.pos_ == "PRON"), None)
    if t is None:
        return None
    while t.dep_ == "conj" and t.head.i != t.i:
        t = t.head
    if t.dep_ in ("det",):
        return None
    if any(c.dep_ == "cop" for c in t.children):
        # 'The ball was what?': the word is the copula's predicate
        cop = next(c for c in t.children if c.dep_ == "cop")
        return cop.lemma_.lower(), "attr"
    lab, head = link(t), t.head
    while head.pos_ in ("ADV", "ADP") and head.head.i != head.i:
        head = head.head
    cop = next((c for c in head.children if c.dep_ == "cop"), None)
    return (cop or head).lemma_.lower(), lab


def asked(text: str) -> bool:
    return text.rstrip().endswith("?")


class _Asked:
    def __init__(self, text: str) -> None:
        self.text = text


def said_in(answer: str, question: str) -> bool:
    return re.search(rf"\b{re.escape(answer)}\b", question.lower()) is not None
