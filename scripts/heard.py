"""The cloze split by how many earlier stories told its answer in its slot, offline.

    uv run python scripts/heard.py readings/stories-graphed-s0-n1000-....json

The cloze blanks a word the story already named, but naming it is not knowing that it fits
('She ate all of what?' wants 'lunch'; whether anything was ever eaten as lunch before is
another matter). Each cloze is tagged by the world, never the system: with the exam's own
parser, every story's sentences are read for (verb, slot, noun) pairings, and each cloze
is banded by how many earlier stories told its verb, its blank's slot and its answer
together: a fact seen across stories, asked in a sentence never heard (John's,
2026-10-07). A pairing told only earlier in its own story is `own`. None at all asks for
world knowledge no stream this size need hold; many asks whether what was learnt is used,
and should read highest. Each band is read as its own curve.
"""

from __future__ import annotations

import argparse
import json
import os
from collections import defaultdict
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.exam.stories import _parser, bucket, stream  # noqa: E402
from unfused.reading import utc_stamp  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
ASKING = {"who", "what", "whom"}


def slot(t) -> str:
    """A noun's slot: its dependency, an object of a preposition by the preposition."""
    if t.dep_ == "pobj" and t.head.dep_ == "prep":
        return "prep:" + t.head.lower_
    return t.dep_


def verb(t):
    """The verb a noun hangs from, through a preposition."""
    h = t.head.head if t.dep_ == "pobj" else t.head
    return h.lemma_.lower() if h.pos_ in ("VERB", "AUX") else None


def pairings(doc) -> set[tuple[str, str, str]]:
    return {(v, slot(t), t.lemma_.lower()) for t in doc if t.pos_ in ("NOUN", "PROPN")
            and (v := verb(t)) is not None}


def blank(doc) -> tuple[str, str] | None:
    t = next((t for t in doc if t.lower_ in ASKING), None)
    if t is None or (v := verb(t)) is None:
        return None
    return v, slot(t)


def band(n: int) -> str:
    return "0" if n == 0 else "1" if n == 1 else "2-4" if n < 5 else "5+"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("reading", type=Path)
    ap.add_argument("--seed", type=int, default=0)
    args = ap.parse_args()
    got = json.loads(args.reading.read_text(encoding="utf-8"))
    rows = [r for r in got["rows"] if r["form"] == "cloze"]
    n = max(r["story"] for r in got["rows"]) + 1
    stories = stream(n, args.seed)
    nlp = _parser()
    # how many stories told each pairing
    stories_of: dict = defaultdict(int)
    tagged = {}
    for s in stories:
        own = set()
        for d in nlp.pipe(s.told):
            own |= pairings(d)
        if s.question is not None:
            b = blank(nlp(s.question))
            seen = max((stories_of[(*b, a)] for a in s.answers), default=0) if b else 0
            tagged[s.index] = band(seen) if seen or b is None or not any(
                (*b, a) in own for a in s.answers) else "own"
        for d in nlp.pipe(s.after):
            own |= pairings(d)
        for x in own:
            stories_of[x] += 1
    parts = defaultdict(list)
    for r in rows:
        parts[tagged.get(r["story"], "0")].append(r)
    out = {"kind": "heard", "taken_at": utc_stamp(), "of": args.reading.name, "parts": {}}
    for name, rs in sorted(parts.items()):
        curve = defaultdict(list)
        for r in rs:
            curve[bucket(r["story"])].append(r["correct"])
        out["parts"][name] = {
            "n": len(rs), "score": round(sum(r["correct"] for r in rs) / len(rs), 3),
            "curve": {b: [round(sum(v) / len(v), 3), len(v)] for b, v in curve.items()}}
        print(name, json.dumps(out["parts"][name]))
    path = ROOT / "readings" / f"heard-{args.reading.stem.removeprefix('stories-')}.json"
    path.write_text(json.dumps(out, indent=1), encoding="utf-8")
    print(path.name)


if __name__ == "__main__":
    main()
