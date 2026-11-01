"""HotpotQA (Yang et al., 2018), `data/hotpotqa/*.jsonl`: questions answered from two Wikipedia
paragraphs together, with a short answer as the key. Step 6's set: the knowledge a question
needs is absent from the trunk and present in the store. The distractor form gives each question
ten paragraphs, two supporting and eight distractors, which are the store's rows for that
episode; the full form puts the Wikipedia abstracts in one store.

Scoring is the benchmark's own: exact match and token F1 on the normalised answer (lower case,
punctuation and articles dropped).
"""

from __future__ import annotations

import json
import re
import string
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent.parent


def questions(n: int | None = None, split: str = "validation", skip: int = 0) -> list[dict]:
    """The questions of a split, each with its paragraphs: {"id", "question", "answer", "type",
    "level", "paragraphs": [{"title", "sentences", "supporting"}]}, the first `skip` passed
    over. Readings are on "validation"; "train" (90k) is for the loop."""
    out = []
    with open(ROOT / "data" / "hotpotqa" / f"{split}.jsonl", encoding="utf-8") as f:
        for i, line in enumerate(f):
            if i < skip:
                continue
            if n is not None and i >= skip + n:
                break
            r = json.loads(line)
            gold = set(r["supporting_titles"])
            paragraphs = [{"title": t, "sentences": s, "supporting": t in gold}
                          for t, s in zip(r["titles"], r["sentences"])]
            out.append({"id": r["id"], "question": r["question"], "answer": r["answer"], "type": r["type"],
                        "level": r["level"], "paragraphs": paragraphs})
    return out


def paragraph_text(p: dict) -> str:
    return f"{p['title']}: " + "".join(p["sentences"]).strip()


def normalise(text: str) -> str:
    text = text.lower()
    text = "".join(ch for ch in text if ch not in set(string.punctuation))
    text = re.sub(r"\b(a|an|the)\b", " ", text)
    return " ".join(text.split())


def exact(said: str | None, gold: str) -> bool:
    return said is not None and normalise(said) == normalise(gold)


def f1(said: str | None, gold: str) -> float:
    if said is None:
        return 0.0
    s, g = normalise(said).split(), normalise(gold).split()
    if normalise(said) in ("yes", "no", "noanswer") or normalise(gold) in ("yes", "no", "noanswer"):
        return float(normalise(said) == normalise(gold))
    common = Counter(s) & Counter(g)
    overlap = sum(common.values())
    if overlap == 0:
        return 0.0
    precision, recall = overlap / len(s), overlap / len(g)
    return 2 * precision * recall / (precision + recall)


def recall(paragraphs: list[str], question: str, embed, k: int = 2) -> list[int]:
    """Automatic recall, as memory does, nothing asked for (John's form, 2026-10-31): the first
    paragraph is the one nearest the question, and each next is the one nearest the text of the
    paragraph just recalled, so the first paragraph cues the second (the bridge entity is named
    in it, not in the question). Returns the indices in the order recalled, none twice."""
    import numpy as np

    keys = np.asarray(embed(paragraphs), dtype=np.float32)
    keys /= np.maximum(np.linalg.norm(keys, axis=1, keepdims=True), 1e-12)
    out: list[int] = []
    cue = question
    for _ in range(min(k, len(paragraphs))):
        q = np.asarray(embed([cue]), dtype=np.float32)[0]
        q /= max(float(np.linalg.norm(q)), 1e-12)
        order = np.argsort(-(keys @ q))
        pick = next(int(j) for j in order if int(j) not in out)
        out.append(pick)
        cue = paragraphs[pick]
    return out


def answer_of(reply: str) -> str | None:
    """The answer in the trunk's reply: the text after the last "Answer:" line, else the reply's
    last line; None where there is nothing."""
    m = re.findall(r"answer\s*:\s*\**\s*([^\n]+)", reply, re.I)
    text = (m[-1] if m else reply.strip().split("\n")[-1] if reply.strip() else "").strip().strip("*. ")
    return text or None
