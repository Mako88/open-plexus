"""PreCo: a standard test of the system's own binding (John's, 2026-10-08).

Coreference annotated on 38,000 English documents of reading-comprehension passages
written for preschool and primary pupils (Chen et al., EMNLP 2018), every mention marked,
singletons too, so a pronoun's gold chain says which earlier mentions it refers to. Nothing
in it was written for this system, and no coreference model is in the system (DECIDED):
the test reads what the system binds against a person's answer.

A document is told sentence by sentence, as the dataset splits it, with a break before
each; a paragraph break (a sentence holding only a space) is not told, since a turn with
no words in it is an episode's end. Each sentence is the dataset's tokens joined by
spaces, its quotation marks said as quotation marks, and each token's place in it is kept
so a mention the system heard is found among the gold ones by where it was said.

Fetched from https://huggingface.co/datasets/coref-data/preco_raw (`dev.jsonl`, the 500
documents of the published dev split) into `data/preco/`.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass

from unfused.home import home

DATA = home() / "data" / "preco"
URL = "https://huggingface.co/datasets/coref-data/preco_raw/resolve/main/{split}.jsonl"

# the third-person personal pronouns, the ones that refer back to something heard; 'I' and
# 'you' name the speaker and the one spoken to, whom the system never binds
PRONOUNS = {"he", "him", "his", "himself", "she", "her", "hers", "herself", "it", "its",
            "itself", "they", "them", "their", "theirs", "themselves"}
SAID = {"``": '"', "''": '"', "-LRB-": "(", "-RRB-": ")"}


@dataclass
class Mention:
    sentence: int
    start: int  # characters into the sentence's text
    end: int
    chain: int
    word: str  # the mention's own words, lower case


@dataclass
class Document:
    name: str
    told: list[str]  # the sentences in order, paragraph breaks left out
    mentions: list[Mention]  # in the order said


def _sentence(tokens: list[str]) -> tuple[str, list[tuple[int, int]]]:
    """Tokens as one sentence, with each token's span in it."""
    text, spans = "", []
    for tok in tokens:
        tok = SAID.get(tok, tok)
        if text:
            text += " "
        spans.append((len(text), len(text) + len(tok)))
        text += tok
    return text, spans


def documents(split: str = "dev", n: int | None = None) -> list[Document]:
    path = DATA / f"{split}.jsonl"
    if not path.exists():
        raise SystemExit(f"PreCo is not here; fetch {URL.format(split=split)} into {path}")
    docs = []
    with path.open(encoding="utf-8") as f:
        for line in f:
            if n is not None and len(docs) >= n:
                break
            raw = json.loads(line)
            told, where, spans = [], {}, {}
            for i, tokens in enumerate(raw["sentences"]):
                if not any(t.strip() for t in tokens):
                    continue
                where[i] = len(told)
                text, spans[i] = _sentence(tokens)
                told.append(text)
            mentions = []
            for chain, cluster in enumerate(raw["mention_clusters"]):
                for s, a, b in cluster:
                    if s not in where:
                        continue
                    words = " ".join(raw["sentences"][s][a:b]).lower()
                    mentions.append(Mention(where[s], spans[s][a][0], spans[s][b - 1][1],
                                            chain, words))
            mentions.sort(key=lambda m: (m.sentence, m.start, -m.end))
            docs.append(Document(raw["id"], told, mentions))
    return docs


def fingerprint(docs: list[Document]) -> str:
    h = hashlib.sha256()
    for d in docs:
        h.update(d.name.encode())
        for s in d.told:
            h.update(s.encode())
    return h.hexdigest()[:12]
