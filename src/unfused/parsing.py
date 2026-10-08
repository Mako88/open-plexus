"""Reading: the parser, its caches, and what a parse is turned into.

A text is parsed once, by Stanza's transformer package, and its reading is kept on disk by
model and text; a sentence's reading is then `extract`ed into events, each a verb's lemma
with an edge to each of its arguments by the link the parse gives. Nothing here knows the
graph an event is written to: the graph reads through `parse`, `parse_many`, `extract` and
`vectors`, and a different parser or encoder is another module with the same four names.
"""

from __future__ import annotations

import sqlite3

from unfused.home import home

# every extraction kept across runs: the parser is deterministic, and the version is in
# the key so a change to what is extracted re-reads every sentence
CACHE = home() / "state" / "parses.sqlite"
VERSION = "graph-14"
# every text MiniLM has encoded, by its text
VECTORS = home() / "state" / "vectors.sqlite"
# every text the parser has read, as its reading, by model and text
DOCS = home() / "state" / "docs.sqlite"

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
# how many texts the parser reads in one batch: 256 stalled for over fifteen minutes on a
# card a game was sharing, its memory full
BATCH = 64
STANZA = home() / "state" / "stanza"

_NLP: dict = {}
# texts read this run, and the open store of readings
_DOCS: dict = {}
_DOCS_DB: dict = {}


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
            events.append({"lemma": lemma, "mood": said, "edges": [], "head": tok.i})
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
            # against the episode, by what it agrees in and the slot it fills. Where it
            # was said rides with it, so what it was bound to can be read against it
            if first is not None:
                return thing(first) + f"\x1e{tok.idx}"
            return f"p:{tok.dep_}|{agreement(tok)}\x1e{tok.idx}"
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
            events.append({"lemma": label, "mood": "", "head": i, "edges": [
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
        # a pronoun's place in the text, beside the edge it became
        pronouns = [int(t.split("\x1e")[1]) if "\x1e" in t else None for _, t in edges]
        edges = [[label, t.split("\x1e")[0]] for label, t in edges]
        mentions = [int(t.split("\x1f")[1]) if "\x1f" in t else None for _, t in edges]
        # the token the event was heard at, so a mouth can say it again
        out.append({"lemma": events[i]["lemma"], "mood": events[i]["mood"], "head": events[i]["head"],
                    "edges": [[label, t.split("\x1f")[0]] for label, t in edges],
                    "mentions": mentions, "pronouns": pronouns,
                    "things": {str(m): things[m] for m in mentions if m is not None},
                    "place": {str(m): doc[m].idx for m in mentions if m is not None}})
    return out
