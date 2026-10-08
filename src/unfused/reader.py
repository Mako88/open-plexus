"""Reader: what the parser says of a text, kept so no text is parsed twice.

Every method here is a function of one text and the parser's model: the events a sentence
tells, the noun phrases and the slot a question asks for, its tokens and its lemmas. Each is
read once and kept in a sqlite cache beside the parses, keyed by the parser's version, so a
run that hears a sentence again reads it back. Nothing here knows the graph.
"""

from __future__ import annotations

import hashlib
import json
import sqlite3
from pathlib import Path

from unfused.parsing import (
    CACHE,
    CLAUSES,
    DESCRIBE,
    PARSER,
    SKIP,
    VERSION,
    asking,
    extract,
    feature,
    link,
    mood,
    negates,
    parse,
    phrase,
)


class Reader:
    def __init__(self, model: str = PARSER, cache: Path | None = CACHE) -> None:
        self.model = model
        self.cache = cache
        # how many texts were parsed rather than read back from the cache
        self.parsed = 0
        self._tokens: dict = {}

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

    def words(self, text: str) -> list[str]:
        """A text's words as the parse lemmatises them, so a cue in other words of
        the same kind ('find' for 'found') reaches the episode that heard them. Kept with
        the parses, so a sentence heard again is not read back as a whole document for its
        lemmas, which was a third of hearing."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|words|{text}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            kept = [t.lemma_.lower() for t in parse(self.model, text) if t.is_alpha]
            self._keep(key, kept)
        return kept

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

    def relatives(self, text: str) -> list:
        """The phrases of a question holding a relative clause ('the one who found a shell'),
        each as the character span of the phrase and of its clause: a relative clause is
        a question about the noun it hangs from, and its relative word is the parse's
        (`PronType=Rel`)."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|relatives|{text}".encode()).hexdigest()
        kept = self._kept(key)
        if kept is None:
            kept = []
            doc = list(parse(self.model, text))
            for verb in doc:
                if not verb.dep_.startswith("acl") or not any(asking(c) for c in verb.children):
                    continue
                noun = verb.head

                def under(tok) -> list:
                    out = [tok]
                    for c in tok.children:
                        out += under(c)
                    return out

                phrase = sorted(under(noun), key=lambda x: x.i)
                clause = sorted(under(verb), key=lambda x: x.i)
                # the question's own wh-word is never inside the phrase it asks about
                if phrase[0].i == 0 or len(phrase) >= len(doc) - 2:
                    continue
                kept.append([phrase[0].idx, phrase[-1].idx + len(phrase[-1].text),
                             clause[0].idx, clause[-1].idx + len(clause[-1].text)])
            self._keep(key, kept)
        return kept

    def reaction_rows(self, text: str) -> list:
        """A reaction to an answer, one row a token: no, yes, whether it may name
        something, its name, and whether it is a number of things."""
        key = hashlib.sha256(f"{VERSION}|{self.model}|reaction-3|{text}".encode()).hexdigest()
        rows = self._kept(key)
        if rows is None:
            doc = parse(self.model, text)
            # one row a token: no, yes, whether it may name something, its name, and
            # whether it is a number of things read as the arm reads one ('none')
            rows = []
            for t in doc:
                no = negates(t)
                yes = "Pos" in feature(t, "Polarity")
                part = t.dep_ in DESCRIBE and t.head.pos_ in ("NOUN", "PROPN")
                number = "Card" in feature(t, "NumType") or t.lower_.isdigit()
                names = not part and (t.pos_ in ("NOUN", "PROPN", "NUM", "ADJ") or number)
                rows.append([no, yes, names, t.lower_ if number else phrase(t), number])
            self._keep(key, rows)
        return rows

    def close(self) -> None:
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
