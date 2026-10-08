"""TinyStories as one stream: the target (John's, 2026-10-03).

Short stories in a small child's vocabulary (Eldan and Li, 2023), told one after another
to one system that never restarts. Nothing is templated and nothing is written for the
system. A story is told sentence by sentence; one later sentence that names something
the story told earlier is held back and asked with that name blanked, as the Children's
Book Test asks ('She found what under the bed?'). The teacher reacts, then the held-back
sentence and the rest of the story are told.

The measure is the curve: the share answered right as the stories heard grow, beside
what each answer costs. A system that keeps learning keeps rising.
"""

from __future__ import annotations

import hashlib
import json
import re
import sqlite3
from collections import Counter
from dataclasses import asdict, dataclass, field
from pathlib import Path

from unfused.home import home

DATA = home() / "data" / "tinystories"
# each story as made, kept by its text and how it is made: bump on any change to `make`
CACHE = home() / "state" / "stories.sqlite"
MADE = "stories-11"
VALID = DATA / "TinyStories-valid.txt"
URL = "https://huggingface.co/datasets/roneneldan/TinyStories/resolve/main/TinyStories-valid.txt"

DEFINITE = {"the", "his", "her", "their", "its", "my", "your", "our"}

# A sentence ends at . ! or ? with any closing quotes kept on it.
_END = re.compile(r'(?<=[.!?])["”\']?\s+(?=["“\']?[A-Z])')


@dataclass
class Check:
    """A comprehension question: about a sentence already told, asked a few sentences on in
    a parent's words ('Where did Roxy put the leaves?'), so its answer is in what was heard
    and only its wording is new."""

    at: int  # sentences of the story told before it is asked
    question: str
    answer: str
    answers: tuple[str, ...]
    earlier: list[str]  # the story's nouns told before it is asked
    # 'check' a few sentences on; 'far' once the story is told, FAR or more sentences on;
    # 'joined' a few sentences on, its doer named by another thing told of them;
    # 'changed' where a thing is now, after the story moved it
    form: str = "check"


@dataclass
class Story:
    index: int
    told: list[str]  # before the question
    question: str | None
    answer: str | None
    answers: tuple[str, ...] = ()  # every form counted right: surface and lemma
    after: list[str] = field(default_factory=list)  # the held-back sentence and the rest
    earlier: list[str] = field(default_factory=list)  # the story's nouns before the question
    checks: list[Check] = field(default_factory=list)


_TITLES = ("Mr.", "Mrs.", "Ms.", "Dr.", "St.")


def sentences(text: str) -> list[str]:
    out: list[str] = []
    for piece in _END.split(" ".join(text.split())):
        if out and out[-1].endswith(_TITLES):
            out[-1] += " " + piece
        elif piece.strip():
            out.append(piece.strip())
    return out


def raw(path: Path = VALID) -> list[str]:
    if not path.exists():
        raise FileNotFoundError(f"{path}: fetch it with `curl -L -o {path} {URL}`")
    out = []
    for chunk in path.read_text(encoding="utf-8").split("<|endoftext|>"):
        chunk = chunk.strip()
        # the file's first chunk starts mid-story
        if chunk and chunk[0].isupper() and len(chunk.split()) >= 20:
            out.append(chunk)
    return out


def _parser():
    import spacy

    # the world's own reader, never the system's: it only finds the noun to blank
    return spacy.load("en_core_web_sm")


def sense(word: str, pos: str) -> str | None:
    """A word's commonest sense's WordNet category ('noun.location', 'verb.motion'). The
    world's knowledge, never the system's."""
    from nltk.corpus import wordnet as wn

    tag = wn.NOUN if pos == "n" else wn.VERB
    try:
        got = wn.synsets(wn.morphy(word, tag) or word, pos=tag)
    except LookupError:
        import nltk

        nltk.download("wordnet", quiet=True)
        got = wn.synsets(wn.morphy(word, tag) or word, pos=tag)
    return got[0].lexname() if got else None


def person(noun: str) -> bool:
    """Whether a common noun is a person by WordNet's commonest sense, so the cloze asks
    'who' of 'mommy' as a reader would (John's, 2026-10-06), where it asked 'who' of a
    name alone. The world's knowledge, never the system's. Its sense order misses a few
    ('mum' is a flower first, 'queen' an animal, 'bunny' a person)."""
    from nltk.corpus import wordnet as wn

    try:
        got = wn.synsets(wn.morphy(noun, wn.NOUN) or noun, pos=wn.NOUN)
    except LookupError:
        import nltk

        nltk.download("wordnet", quiet=True)
        got = wn.synsets(wn.morphy(noun, wn.NOUN) or noun, pos=wn.NOUN)
    return bool(got) and got[0].lexname() == "noun.person"


def make(text: str, index: int, nlp) -> Story:
    """The story's last sentence, after its first three, that names a noun the story told
    before it, unquoted, asked with that noun's phrase replaced by 'who' for a name or a person, else 'what'."""
    sents = sentences(text)
    docs = list(nlp.pipe(sents))
    seen: list[set] = []
    acc: set = set()
    for d in docs:
        seen.append(set(acc))
        acc |= {t.lemma_.lower() for t in d if t.pos_ in ("NOUN", "PROPN")}
    for i in range(len(sents) - 1, 2, -1):
        d, s = docs[i], sents[i]
        if any(q in s for q in '"“”') or len(d) < 5 or s.endswith("?"):
            continue
        # a name, or a noun said as one already known ('the net', 'his mum'): a reference
        # back into the story, where 'that day' or 'others' refers to nothing told
        targets = [t for t in d if t.pos_ in ("NOUN", "PROPN") and t.lemma_.lower() in seen[i]
                   and t.dep_ != "compound" and (t.pos_ == "PROPN" or any(
                       c.dep_ in ("det", "poss") and c.lower_ in DEFINITE
                       for c in t.children))]
        if not targets:
            continue
        t = targets[-1]
        chunk = next((c for c in d.noun_chunks if c.start <= t.i < c.end), d[t.i:t.i + 1])
        if chunk.root.i != t.i:
            continue
        wh = "who" if t.pos_ == "PROPN" or person(t.lemma_.lower()) else "what"
        before = d[:chunk.start].text_with_ws
        rest = d[chunk.end:].text_with_ws.rstrip()
        rest = re.sub(r"[.!]+$", "", rest).rstrip()
        q = (before + wh + (" " + rest if rest and not rest.startswith((",", "'")) else rest))
        q = q[0].upper() + q[1:] + "?"
        return Story(index, sents[:i], q, t.text.lower(),
                     tuple(sorted({t.text.lower(), t.lemma_.lower()})), sents[i:],
                     _nouns(docs[:i]), _checks(text, sents, docs, i))
    return Story(index, sents, None, None, checks=_checks(text, sents, docs, None))


def _nouns(docs) -> list[str]:
    return [w for w in Counter(tok.lemma_.lower() for d in docs for tok in d
                               if tok.pos_ in ("NOUN", "PROPN")).elements()]


# how many sentences after the one it asks about a comprehension question comes
GAP = 2
# at most this many a story, each about a sentence of its own
CHECKS = 2
# a far check is asked once the story is told, about a sentence at least this many
# sentences before its end (THE ORDER, harder checks: further from their sentence)
FAR = 5
_PLACES = {"in", "on", "under", "into", "onto", "at", "behind", "near", "inside", "to"}
_KEPT = {"dobj", "dative", "prt", "prep", "acomp", "oprd"}


def _phrase(t) -> str:
    return "".join(x.text_with_ws for x in t.subtree).strip()


def asked(d) -> list[tuple[str, object]]:
    """What a parent asks of a told sentence, as (question, the answer's token): its object
    ('What did Roxy find?'), where it happened ('Where did Roxy put the leaves?') and who
    did it ('Who found an icy hill?'). Only a plain past-tense clause whose doer is named;
    the verb goes to its base form under 'did', so the wording is the parent's."""
    return [(q, t) for q, t, _ in _asks(d)]


# a told sentence whose doer is one of these answers what a named doer's question asks:
# 'she saw dark clouds' answers 'What did Lily see?' as well as 'Lily saw a bird' does
_PRONOUNS = {"he": "Sing", "she": "Sing", "they": "Plur"}


def _asks(d, pronoun: bool = False) -> list[tuple[str, object, tuple]]:
    """`asked`, each with what it asks as (what is asked, the verb, the doer, the other
    parts), so a question can be matched against every telling that answers it. With
    `pronoun`, a clause whose doer is a personal pronoun is read too, for matching only."""
    root = d[:].root
    if root.pos_ != "VERB" or root.tag_ != "VBD":
        return []
    kids = list(root.children)
    if any(c.dep_ in ("aux", "auxpass", "neg") for c in kids):
        return []
    subj = next((c for c in kids if c.dep_ == "nsubj"), None)
    if subj is None or any(c.dep_ == "conj" for c in subj.children):
        return []
    if subj.pos_ == "PRON":
        if not pronoun or subj.lower_ not in _PRONOUNS:
            return []
    elif subj.pos_ not in ("PROPN", "NOUN"):
        return []
    elif subj.pos_ == "NOUN" and not any(c.lower_ in DEFINITE for c in subj.children):
        return []  # 'a little boy' asked back is not a parent's question
    # what follows the verb, in order; a phrase fronted before it ('At last,') is dropped
    parts = [c for c in kids if c.dep_ in _KEPT and c.i > root.i]
    if not parts:
        return []

    def rest(without) -> str:
        return " ".join(_phrase(c) for c in parts if c is not without)

    def others(without) -> frozenset:
        return frozenset(_phrase(c).lower() for c in parts if c is not without)

    doer = _phrase(subj)
    doer = doer if subj.pos_ == "PROPN" else doer[0].lower() + doer[1:]
    who = (subj.lower_ if subj.pos_ == "PRON" else doer.lower(),
           _PRONOUNS.get(subj.lower_) or ("Plur" if subj.tag_ in ("NNS", "NNPS") else "Sing"))
    out = []
    for c in parts:
        if c.dep_ == "dobj" and c.pos_ in ("NOUN", "PROPN"):
            wh = "Who" if c.pos_ == "PROPN" else "What"
            out.append((f"{wh} did {doer} {root.lemma_} {rest(c)}".strip() + "?", c,
                        ("dobj", root.lemma_, who, others(c))))
        elif c.dep_ == "prep" and c.lower_ in _PLACES:
            obj = next((g for g in c.children if g.dep_ == "pobj"), None)
            if obj is not None and obj.pos_ in ("NOUN", "PROPN"):
                out.append((f"Where did {doer} {root.lemma_} {rest(c)}".strip() + "?", obj,
                            ("where", root.lemma_, who, others(c))))
    if subj.pos_ != "PRON" and any(c.dep_ in ("dobj", "prep") for c in parts):
        # a story's doers are its people and animals, so a parent asks 'who'
        out.append((f"Who {root.text} {rest(None)}?", subj,
                    ("who", root.lemma_, None, others(None))))
    return out


def _answers(asking: tuple, telling: tuple) -> bool:
    """Whether a telling answers a question: what is asked and the verb the same, the doer
    the same or a pronoun that can stand for it, and every other part the question names
    in it, so 'she saw dark clouds in the sky' answers 'What did Lily see?'."""
    form, lemma, doer, rest = asking
    t_form, t_lemma, t_doer, t_rest = telling
    if (form, lemma) != (t_form, t_lemma) or not rest <= t_rest:
        return False
    if doer is None or t_doer == doer:
        return True
    # a pronoun stands for a doer of its number
    return t_doer[0] in _PRONOUNS and t_doer[1] == doer[1]


def _checks(text, sents, docs, held) -> list[Check]:
    """Up to CHECKS comprehension questions, each about its own told sentence and asked GAP
    sentences after it, and up to CHECKS far ones about other sentences, asked once the
    story is told. The cloze's held-back sentence is never asked about, so the curves
    stay apart. Which are asked is fixed by the story's text."""
    found, tellings = [], []
    for j, (s, d) in enumerate(zip(sents, docs)):
        if j == held or any(q in s for q in '"“”') or s.endswith("?"):
            continue
        for q, t, what in _asks(d):
            key = hashlib.sha256(f"{text}|{j}|{q}".encode()).hexdigest()
            found.append((key, j, q, t, what))
        tellings += [(j, t, what) for _, t, what in _asks(d, pronoun=True)]
    out, used = [], set()
    for _, j, q, t, what in sorted(found):
        # a parent asks a question once, whichever telling it was of
        if j in used or q in used or len(out) == CHECKS:
            continue
        used |= {j, q}
        out.append(_check(q, t, what, min(j + 1 + GAP, len(sents)), "check",
                          tellings, docs))
    far = []
    for _, j, q, t, what in sorted(found):
        if j in used or q in used or len(far) == CHECKS or len(sents) - (j + 1) < FAR:
            continue
        used |= {j, q}
        far.append(_check(q, t, what, len(sents), "far", tellings, docs))
    return sorted(out + far + _joined(sents, docs, found, tellings) + _changed(sents, docs,
                                                                            held),
                  key=lambda c: c.at)


# where a thing ends up: the prepositions of a place a thing is put or goes, what a place
# is, and the verbs that move a thing or its doer, by WordNet's categories
_TO = {"in", "on", "under", "into", "onto", "at", "behind", "inside", "to"}
_PLACE = {"noun.location", "noun.artifact", "noun.object", "noun.plant"}
_PUT = {"verb.motion", "verb.contact"}


def _placed(d) -> list[tuple[str, str, object]]:
    """What a told sentence puts somewhere, as (what is placed, how it is asked, the
    place's token): the object a doer puts ('put the ball in the box'), the doer itself
    where nothing is put ('Lily went to the park'), or what a sentence says is there ('The
    cat was on the mat'). Its main verb and the verbs joined to it ('went home and ran to
    her room'), each in the past and said plainly."""
    root = d[:].root
    verbs = [root] + [c for c in root.children if c.dep_ == "conj"]
    subj = next((c for c in root.children if c.dep_ == "nsubj"), None)
    out = []
    for verb in verbs:
        if verb.pos_ not in ("VERB", "AUX") or verb.tag_ != "VBD":
            continue
        kids = list(verb.children)
        if any(c.dep_ in ("aux", "auxpass", "neg") for c in kids):
            continue
        doer = next((c for c in kids if c.dep_ == "nsubj"), subj)
        obj = next((c for c in kids if c.dep_ == "dobj"), None)
        state = verb.lemma_ == "be"
        moves = None if state else sense(verb.lemma_.lower(), "v")
        # a thing put somewhere is moved by its doer; a doer goes there itself; a thing
        # said to be somewhere is there
        if not state and (obj is not None and moves not in _PUT
                          or obj is None and moves != "verb.motion"):
            continue
        thing = obj if obj is not None else doer
        if thing is None or thing.pos_ not in ("NOUN", "PROPN"):
            continue
        if thing.pos_ == "NOUN" and not any(c.lower_ in DEFINITE for c in thing.children):
            continue
        for prep in (c for c in kids if c.dep_ == "prep" and c.lower_ in _TO and c.i > verb.i):
            place = next((g for g in prep.children if g.dep_ == "pobj"), None)
            if place is None or place.pos_ not in ("NOUN", "PROPN"):
                continue
            if sense(place.lemma_.lower(), "n") not in _PLACE:
                continue
            said = thing.text if thing.pos_ == "PROPN" else "the " + thing.lemma_.lower()
            out.append((thing.lemma_.lower(), said, place))
    return out


def _changed(sents, docs, held) -> list[Check]:
    """Up to CHECKS questions about where a thing is now (THE ORDER, harder checks: what
    changed): a thing the story has put in two different places, asked GAP sentences after
    the later one ('Where is the ball now?'). Only the latest place is right; the earlier
    one is what a memory of what was told without its order would give."""
    where: dict[str, list] = {}
    out, used = [], set()
    for j, (s, d) in enumerate(zip(sents, docs)):
        if j == held or any(q in s for q in '"“”') or s.endswith("?"):
            continue
        for thing, said, place in _placed(d):
            before = where.get(thing, [])
            last = place.lemma_.lower()
            if (before and before[-1][1] != last and thing not in used
                    and len(out) < CHECKS):
                used.add(thing)
                at = min(j + 1 + GAP, len(sents))
                out.append(Check(at, f"Where is {said} now?", place.text.lower(),
                                 tuple(sorted({place.text.lower(), last})),
                                 _nouns(docs[:at]), "changed"))
            where.setdefault(thing, []).append((j, last))
    return out


def _joined(sents, docs, found, tellings) -> list[Check]:
    """Up to CHECKS questions that need two told facts (THE ORDER, harder checks): one
    told sentence's question about a named doer, the doer named instead by what another
    told sentence says they did. After 'Lily found a shell' and 'Lily saw a crab', 'What
    did the one who found a shell see?' is answered by finding who found the shell, then
    what they saw. The describing sentence comes first and picks out one doer alone; the
    question is asked GAP sentences after the later of the two."""
    clauses = {}
    for _, j, q, t, what in found:
        if what[0] == "who" and t.pos_ == "PROPN":
            clauses.setdefault(j, []).append((q[len("Who "):-1], t.text, what))
    out, used = [], set()
    for _, j, q, t, what in sorted(found):
        if what[0] not in ("dobj", "where") or len(out) == CHECKS or j in used:
            continue
        doer = what[2][0]
        for k in sorted(clauses):
            if k >= j:
                break
            for clause, name, describes in clauses[k]:
                # the description is of this doer, of no one else the story has told, and
                # does not hold the answer
                doers = {u.text.lower() for i, u, told in tellings
                         if i < j and _answers(describes, told)}
                if (name.lower() != doer or doers != {doer} or t.text.lower() in
                        clause.lower() or f" did {name} " not in q):
                    continue
                asked = q.replace(f" did {name} ", f" did the one who {clause} ", 1)
                out.append(_check(asked, t, what, min(j + 1 + GAP, len(sents)), "joined",
                                  tellings, docs))
                used.add(j)
                break
            if j in used:
                break
    return out


def _check(q, t, what, at, form, tellings, docs) -> Check:
    """A question asked at `at`, right in every answer a telling of it by then supports:
    one more than one telling answers ('What did Lily see?' after she saw a bird and then
    dark clouds in the sky) has each of their answers right (John's, 2026-10-04)."""
    same = {t} | {u for k, u, told in tellings if k < at and _answers(what, told)}
    return Check(at, q, t.text.lower(), tuple(sorted(
        {u.text.lower() for u in same} | {u.lemma_.lower() for u in same})),
        _nouns(docs[:at]), form)


def stream(n: int, seed: int = 0, path: Path = VALID) -> list[Story]:
    """The first `n` stories of the file in an order fixed by the seed."""
    texts = raw(path)
    order = sorted(range(len(texts)),
                   key=lambda i: hashlib.sha256(f"{seed}|{i}".encode()).hexdigest())
    CACHE.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(str(CACHE))
    db.execute("CREATE TABLE IF NOT EXISTS stories (key TEXT PRIMARY KEY, value TEXT)")
    nlp, out = None, []
    for k, j in enumerate(order[:n]):
        key = hashlib.sha256(f"{MADE}|{texts[j]}".encode()).hexdigest()
        row = db.execute("SELECT value FROM stories WHERE key = ?", (key,)).fetchone()
        if row is None:
            nlp = nlp or _parser()
            story = make(texts[j], k, nlp)
            db.execute("INSERT OR REPLACE INTO stories VALUES (?, ?)",
                       (key, json.dumps(asdict(story))))
        else:
            story = Story(**{**json.loads(row[0]), "index": k})
            story.answers = tuple(story.answers)
            story.checks = [Check(**{**c, "answers": tuple(c["answers"])})
                            for c in story.checks]
        out.append(story)
    db.commit()
    db.close()
    return out


def fingerprint(stories: list[Story]) -> str:
    h = hashlib.sha256()
    for s in stories:
        h.update(f"{s.question}|{s.answer}|{len(s.told)}".encode())
        for c in s.checks:
            h.update(f"{c.at}|{c.question}|{c.answer}|{c.answers}|{c.form}".encode())
    return h.hexdigest()[:12]


def right(answers: tuple[str, ...], said: str) -> bool:
    lowered = (said or "").lower()
    return any(re.search(rf"\b{re.escape(a)}\b", lowered) for a in answers)


# the stories heard before a question, as the curve is read
BUCKETS = (0, 10, 30, 100, 300, 1000, 3000, 10000, 30000)


def bucket(index: int) -> str:
    lo = max(b for b in BUCKETS if b <= index)
    hi = next((b for b in BUCKETS if b > index), None)
    return f"{lo}-{hi}" if hi else f"{lo}+"
