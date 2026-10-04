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

DATA = Path(__file__).resolve().parents[3] / "data" / "tinystories"
# each story as made, kept by its text and how it is made: bump on any change to `make`
CACHE = Path(__file__).resolve().parents[3] / "state" / "stories.sqlite"
MADE = "stories-1"
VALID = DATA / "TinyStories-valid.txt"
URL = "https://huggingface.co/datasets/roneneldan/TinyStories/resolve/main/TinyStories-valid.txt"

DEFINITE = {"the", "his", "her", "their", "its", "my", "your", "our"}

# A sentence ends at . ! or ? with any closing quotes kept on it.
_END = re.compile(r'(?<=[.!?])["”\']?\s+(?=["“\']?[A-Z])')


@dataclass
class Story:
    index: int
    told: list[str]  # before the question
    question: str | None
    answer: str | None
    answers: tuple[str, ...] = ()  # every form counted right: surface and lemma
    after: list[str] = field(default_factory=list)  # the held-back sentence and the rest
    earlier: list[str] = field(default_factory=list)  # the story's nouns before the question


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


def make(text: str, index: int, nlp) -> Story:
    """The story's last sentence, after its first three, that names a noun the story told
    before it, unquoted, asked with that noun's phrase replaced by 'what' or 'who'."""
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
        wh = "who" if t.pos_ == "PROPN" else "what"
        before = d[:chunk.start].text_with_ws
        rest = d[chunk.end:].text_with_ws.rstrip()
        rest = re.sub(r"[.!]+$", "", rest).rstrip()
        q = (before + wh + (" " + rest if rest and not rest.startswith((",", "'")) else rest))
        q = q[0].upper() + q[1:] + "?"
        earlier = [w for w in Counter(
            tok.lemma_.lower() for dd in docs[:i] for tok in dd
            if tok.pos_ in ("NOUN", "PROPN")).elements()]
        return Story(index, sents[:i], q, t.text.lower(),
                     tuple(sorted({t.text.lower(), t.lemma_.lower()})), sents[i:], earlier)
    return Story(index, sents, None, None)


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
        out.append(story)
    db.commit()
    db.close()
    return out


def fingerprint(stories: list[Story]) -> str:
    h = hashlib.sha256()
    for s in stories:
        h.update(f"{s.question}|{s.answer}|{len(s.told)}".encode())
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
