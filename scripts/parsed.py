"""The house's tellings read by a dependency parse instead of the 0.8B, offline.

    uv run python scripts/parsed.py --seed 0 --seed 1 --seed 2 --note "..."

The mapping from parse to assertion is the plainest one and knows no English word: the
subject is a verb's subject, the object its direct object or complement, a place the
object of a preposition, a quantity a number on a noun; "N of M" under a copula puts M in
object and N in the relation; "it" and "they" are the sentence's own subject. Scored as
`scripts/ears.py` scores the ear, so the numbers stand beside the ear's.
"""

from __future__ import annotations

import argparse
import json
import time
from datetime import UTC, datetime
from pathlib import Path

import spacy

from unfused.ears import _fillers, gold, score_reading
from unfused.exam.world import _FILLER, generate_house

ROOT = Path(__file__).resolve().parents[1]
SUBJECTS = {"nsubj", "nsubjpass"}
OBJECTS = {"dobj", "attr", "acomp", "oprd", "dative"}


def phrase(tok) -> str:
    """A noun's words without its determiners and numbers: 'the 45 glass jars' is
    'glass jars'. A possessed noun keeps its possessor out ('Dethol's cousin')."""
    keep = [t for t in tok.subtree
            if t.dep_ in ("compound", "amod") and t.head == tok or t == tok]
    return " ".join(t.text for t in sorted(keep, key=lambda t: t.i)).lower()


def number(tok) -> str | None:
    for t in tok.children:
        if t.dep_ == "nummod" or t.like_num:
            return t.text
    return None


def assertions(doc) -> list[dict]:
    out = []
    for verb in doc:
        subj = next((c for c in verb.children if c.dep_ in SUBJECTS), None)
        expl = next((c for c in verb.children if c.dep_ == "expl"), None)
        objs = [c for c in verb.children if c.dep_ in OBJECTS]
        if not subj and expl and objs:
            # 'There are 65 wool cards in the dairy': the thing is the subject
            subj, objs = objs[0], objs[1:]
        if not subj:
            continue
        if subj.pos_ == "PRON" and subj.lower_ not in ("i", "you", "we", "someone"):
            first = next((t for t in doc if t.dep_ in SUBJECTS and t.pos_ != "PRON"), None)
            subj = first or subj
        a = {"subject": phrase(subj), "relation": verb.lower_, "object": None,
             "place": None, "quantity": number(subj)}
        rel = [verb.lower_] + [c.lower_ for c in verb.children if c.dep_ == "prt"]
        for o in objs:
            poss = next((c for c in o.children if c.dep_ == "poss"), None)
            of = next((c for c in o.children if c.dep_ == "prep" and c.lower_ == "of"), None)
            if poss is not None:
                # 'Sailoun is Dethol's cousin'
                rel.append(o.lower_)
                a["object"] = phrase(poss)
            elif of is not None and any(c.dep_ == "pobj" for c in of.children):
                # 'a cousin of Drenham', 'a sort of bone colour'
                rel += [o.lower_, "of"]
                a["object"] = phrase(next(c for c in of.children if c.dep_ == "pobj"))
            elif a["object"] is None:
                a["object"] = phrase(o)
            else:
                a["place"] = a["place"] or phrase(o)
            a["quantity"] = a["quantity"] or number(o)
        heads = [verb] + [c for c in verb.children if c.dep_ in ("advmod", "prt")] + objs
        if expl is not None:
            # 'There are 65 wool cards in the dairy': the place hangs off the thing
            heads.append(subj)
        for h in heads:
            for prep in (c for c in h.children if c.dep_ == "prep"):
                pobj = next((c for c in prep.children if c.dep_ == "pobj"), None)
                if pobj is None or prep.lower_ == "of":
                    continue
                if pobj.pos_ == "PRON":
                    # 'has 45 glass jars in it': the room is the subject
                    a["place"] = a["place"] or a["subject"]
                elif a["place"] is None:
                    a["place"] = phrase(pobj)
        # a conjoined subject: 'Bretaerk and Groplourk are cousins'
        conj = next((c for c in subj.children if c.dep_ == "conj"), None)
        if conj is not None and a["object"] in (None, "cousins"):
            rel.append(a["object"] or "")
            a["object"] = phrase(conj)
        a["relation"] = " ".join(r for r in rel if r)
        out.append(a)
    # clauses of one sentence about one subject are one fact: 'the wool cards are
    # Rondnesith's, and they live in the morning room'
    merged: list[dict] = []
    for a in out:
        same = next((m for m in merged if m["subject"] == a["subject"]), None)
        if same is None:
            merged.append(dict(a))
            continue
        for k in ("object", "place", "quantity"):
            same[k] = same[k] or a[k]
        same["relation"] = f"{same['relation']} {a['relation']}"
    return merged


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--seed", type=int, action="append", required=True)
    p.add_argument("--note", default="")
    p.add_argument("--model", default="en_core_web_sm")
    args = p.parse_args()

    nlp = spacy.load(args.model)
    nlp("Warm up.")
    out = []
    for seed in args.seed:
        house = generate_house(seed)
        rows, whole, named, named_whole, seconds = [], 0, 0, 0, 0.0
        for fact in house.facts:
            started = time.perf_counter()
            read = assertions(nlp(fact.told))
            seconds += time.perf_counter() - started
            scored = score_reading(fact, read)
            subject = fact.subject.lower()
            mine = [a for a in read if subject in _fillers(a)]
            wanted = [w.lower() for w in gold(fact)]
            named += len(mine)
            named_whole += sum(all(w in _fillers(a) for w in wanted) for a in mine)
            whole += scored["whole"]
            rows.append({"kind": fact.kind, "told": fact.told, "gold": gold(fact),
                         "assertions": read, **scored})
        filler = {s: assertions(nlp(s)) for s in _FILLER}
        by_kind = {k: round(sum(r["whole"] for r in rows if r["kind"] == k)
                            / max(1, sum(r["kind"] == k for r in rows)), 3)
                   for k in sorted({r["kind"] for r in rows})}
        summary = {"seed": seed, "recall": round(whole / len(rows), 3),
                   "precision": round(named_whole / named, 3) if named else 0.0,
                   "filler_assertions_per_sentence": round(
                       sum(len(v) for v in filler.values()) / len(filler), 2),
                   "by_kind": by_kind, "ms_per_read": round(1000 * seconds / len(rows), 1)}
        print(summary)
        out.append({**summary, "rows": rows, "filler": filler})

    taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = ROOT / "readings" / f"parsed-{taken}.json"
    path.write_text(json.dumps({
        "kind": "parsed", "taken_at": taken, "note": args.note, "parser": args.model,
        "scorer": "each gold filler in a slot of its own", "seeds": out}, indent=1),
        encoding="utf-8")
    print("->", path.name)


if __name__ == "__main__":
    main()
