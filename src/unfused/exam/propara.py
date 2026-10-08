"""ProPara: where a thing is now, as someone else asks it (John's, 2026-10-08).

Paragraphs describing a process ('Plants die. They are buried in sediment. ...'), each
with its participants and, after every sentence, where each one is: a place, '?' where it
exists somewhere unsaid, '-' where it does not exist (Dalvi et al., NAACL 2018; the grids
of Tandon et al., EMNLP 2018). Nothing in it was written for this system, and no question
about it is written through a coreference model: the place is the annotators'.

A paragraph is told sentence by sentence with a break before it, and after each sentence
every participant with a place is asked where it is, in a parent's words ('Where are the
plants?'). A question is due where the place changed since it was last asked as well, so
what moved is read apart from what stayed.

Fetched from https://github.com/allenai/propara (`data/emnlp18/grids.v1.*.json`) into
`data/propara/`.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass

from unfused.home import home

DATA = home() / "data" / "propara"
URL = "https://raw.githubusercontent.com/allenai/propara/master/data/emnlp18/grids.v1.{split}.json"


@dataclass
class Question:
    at: int  # sentences told before it is asked
    participant: str
    question: str
    places: tuple[str, ...]  # the annotators' place, with its alternatives
    moved: bool  # the place differs from the one before this sentence
    told: bool  # the place's words were said in the paragraph so far


@dataclass
class Paragraph:
    name: str
    told: list[str]
    questions: list[Question]


def _alternatives(cell: str) -> tuple[str, ...]:
    return tuple(a.strip() for a in cell.split(";") if a.strip())


def _asked(participant: str) -> str:
    # the world's own wording: a plural said with 'are', as a parent says it
    verb = "are" if participant.endswith("s") and not participant.endswith("ss") else "is"
    return f"Where {verb} the {participant}?"


def paragraphs(split: str = "test", n: int | None = None) -> list[Paragraph]:
    path = DATA / f"grids.v1.{split}.json"
    if not path.exists():
        raise SystemExit(f"ProPara is not here; fetch {URL.format(split=split)} into {path}")
    out = []
    with path.open(encoding="utf-8") as f:
        for line in f:
            if n is not None and len(out) >= n:
                break
            raw = json.loads(line)
            told = raw["sentence_texts"]
            questions = []
            for p, states in zip(raw["participants"], raw["states"]):
                name = _alternatives(p)[0]
                for k in range(1, len(told) + 1):
                    if states[k] in ("?", "-"):
                        continue
                    places = _alternatives(states[k])
                    so_far = " ".join(told[:k]).lower()
                    questions.append(Question(
                        k, name, _asked(name), places, states[k] != states[k - 1],
                        any(a.lower() in so_far for a in places)))
            out.append(Paragraph(raw["para_id"], told, questions))
    return out


def fingerprint(ps: list[Paragraph]) -> str:
    h = hashlib.sha256()
    for p in ps:
        h.update(p.name.encode())
        for s in p.told:
            h.update(s.encode())
    return h.hexdigest()[:12]
