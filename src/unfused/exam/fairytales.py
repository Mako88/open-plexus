"""FairytaleQA: a standard test a rung above the stream (John's, 2026-10-07).

278 fairy tales from Project Gutenberg, each split into sections by its coders, with
questions written by education experts for kindergarten to eighth grade (Xu et al., ACL
2022). Each question names the sections that answer it and is labelled by what it asks
about (character, setting, action, feeling, causal relationship, outcome resolution,
prediction) and whether its answer is said in the text (explicit) or must be inferred
(implicit). Nothing in it was written for this system, so it reads the system against
someone else's idea of understanding a story.

A tale is told sentence by sentence, each section split as its authors split, and each
question is asked once the last section it names has been told, as a reader is checked at
the end of a passage. The scorer is the benchmark's: ROUGE-L F1 against the two
annotators' answers, the better of the two.

Fetched from https://github.com/uci-soe/FairytaleQAData into `data/fairytaleqa/`.
"""

from __future__ import annotations

import csv
import hashlib
import re
from dataclasses import dataclass

from unfused.home import home

DATA = home() / "data" / "fairytaleqa" / "FairytaleQAData-main" / "data-by-train-split"
URL = "https://github.com/uci-soe/FairytaleQAData/archive/refs/heads/main.zip"


@dataclass
class Question:
    at: int  # sentences of the tale told before it is asked
    question: str
    answers: tuple[str, ...]  # the two annotators' answers, the benchmark's references
    attribute: str  # what it asks about, by the dataset's seven narrative elements
    explicit: bool  # its answer is said in the text, by the first annotator's label
    local: bool  # about one section rather than a summary of several


@dataclass
class Tale:
    name: str
    told: list[str]  # the tale's sentences in order
    questions: list[Question]


def _flat(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


_nlp = None


def _sentencer():
    """The world's own sentencer, never the system's: spaCy's, as the dataset's authors
    split."""
    global _nlp
    if _nlp is None:
        import spacy

        _nlp = spacy.load("en_core_web_sm", exclude=["ner", "lemmatizer"])
    return _nlp


def tale(name: str, split: str = "test") -> Tale:
    sections = [(int(r["section"]), _flat(r["text"])) for r in
                csv.DictReader((DATA / "section-stories" / split / f"{name}-story.csv")
                               .open(encoding="utf-8"))]
    # each section split on its own, so no sentence crosses a section's edge (the
    # dataset's own sentence files splice a few across them)
    sentences: list[str] = []
    ends: dict[int, int] = {}
    for number, text in sections:
        sentences += [_flat(x.text) for x in _sentencer()(text).sents if x.text.strip()]
        ends[number] = len(sentences)
    questions = []
    for r in csv.DictReader((DATA / "questions" / split / f"{name}-questions.csv")
                            .open(encoding="utf-8")):
        named = [int(x) for x in re.findall(r"\d+", r["cor_section"])]
        at = max((ends.get(n, len(sentences)) for n in named), default=len(sentences))
        answers = tuple(a for a in (r["answer1"], r["answer4"]) if a.strip())
        questions.append(Question(at=at, question=_flat(r["question"]), answers=answers,
                                  attribute=r["attribute1"],
                                  explicit=r["ex-or-im1"] == "explicit",
                                  local=r["local-or-sum"] == "local"))
    questions.sort(key=lambda q: q.at)
    return Tale(name=name, told=sentences, questions=questions)


def tales(split: str = "test", limit: int | None = None, resolved: bool = False) -> list[Tale]:
    folder = DATA / "questions" / split
    if not folder.exists():
        raise SystemExit(f"FairytaleQA is not in {DATA}; fetch {URL} and unzip it into "
                         f"{DATA.parents[1]}")
    names = sorted(p.name.removesuffix("-questions.csv") for p in folder.glob("*.csv"))
    out = [tale(n, split) for n in names[:limit]]
    if resolved:
        for t in out:
            t.told = resolve(t.told)
    return out


# each tale's sentences with their pronouns resolved, kept by the text: bump on any change
CACHE = home() / "state" / "fairytales-resolved"
RESOLVED = "resolved-3"
_coref = None


def _named(doc, chain) -> str | None:
    """What a chain is called: its commonest name ('Ian'), else 'the' and its commonest
    noun ('the deer'), by each mention's head, the word whose head is outside it. A
    representative mention whole carries its clauses ('a wife with golden hair')."""
    from collections import Counter

    names, nouns = Counter(), Counter()
    for m in chain.mentions:
        words = doc.sentences[m.sentence].words
        span = range(m.start_word, m.end_word)
        head = next((words[i] for i in span if words[i].head - 1 not in span), None)
        if head is None or head.upos not in ("PROPN", "NOUN"):
            continue
        if head.upos == "PROPN":
            run = [words[i].text for i in span if words[i].upos == "PROPN"]
            names[" ".join(run)] += 1
        else:
            nouns[head.text.lower()] += 1
    if names:
        return names.most_common(1)[0][0]
    if nouns:
        return f"the {nouns.most_common(1)[0][0]}"
    return None


def resolve(sentences: list[str]) -> list[str]:
    """A control (THE ORDER, FairytaleQA): the tale with every personal pronoun replaced
    by the naming mention of its chain ('she ran on' to 'the deer ran on'), by Stanza's
    coreference model, so how much binding pronouns is worth is read apart from the
    system's own binding. The world's reading, never the system's; a possessive keeps its
    's, and a chain named only by pronouns is left as told."""
    import json

    key = hashlib.sha256(f"{RESOLVED}|{'\n'.join(sentences)}".encode()).hexdigest()
    path = CACHE / f"{key}.json"
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    global _coref
    if _coref is None:
        import stanza

        from unfused.parsing import STANZA

        _coref = stanza.Pipeline("en", dir=str(STANZA),
                                 processors="tokenize,mwt,pos,lemma,depparse,coref",
                                 tokenize_no_ssplit=True, download_method=None, verbose=False)
    # read in windows of about WINDOW words, since a whole tale's mention pairs fill the
    # card; a pronoun whose naming mention is in an earlier window is left as told
    out, window = [], []
    for s in sentences:
        if window and sum(len(x.split()) for x in window) + len(s.split()) > WINDOW:
            out += _resolved(window)
            window = []
        window.append(s)
    if window:
        out += _resolved(window)
    CACHE.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(out), encoding="utf-8")
    return out


WINDOW = 800


def _resolved(sentences: list[str]) -> list[str]:
    text = "\n\n".join(sentences)
    doc = _coref(text)
    swaps: list[tuple[int, int, str]] = []
    for chain in doc.coref:
        name = _named(doc, chain)
        if name is None:
            continue
        for m in chain.mentions:
            if m.end_word - m.start_word != 1:
                continue
            w = doc.sentences[m.sentence].words[m.start_word]
            feats = w.feats or ""
            if w.upos != "PRON" or "PronType=Prs" not in feats or w.start_char is None:
                continue
            swaps.append((w.start_char, w.end_char,
                          f"{name}'s" if "Poss=Yes" in feats else name))
    for start, end, said in sorted(swaps, reverse=True):
        text = text[:start] + said + text[end:]
    out = text.split("\n\n")
    assert len(out) == len(sentences)
    return [_flat(s) for s in out]


def fingerprint(ts: list[Tale]) -> str:
    h = hashlib.sha256()
    for t in ts:
        h.update(f"{t.name}|{len(t.told)}".encode())
        for q in t.questions:
            h.update(f"{q.at}|{q.question}|{q.answers}".encode())
    return h.hexdigest()[:12]


_scorer = None


def rouge_l(answers: tuple[str, ...], said: str) -> float:
    """The benchmark's score: ROUGE-L F1, the better of the references. A refusal is 0."""
    global _scorer
    if _scorer is None:
        from rouge_score import rouge_scorer

        _scorer = rouge_scorer.RougeScorer(["rougeL"], use_stemmer=True)
    if not said or "don't know" in said.lower():
        return 0.0
    return max(_scorer.score(a, said)["rougeL"].fmeasure for a in answers)
