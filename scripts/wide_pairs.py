"""The wide sleep's finding (Fable, 2026-11-07): round 6's rows came only from pairs the day read one
after the other, which on the full store are the questions it had already found, so a sleep must
find what the day missed. Offline, budget is free: for each of the day's questions, BM25's top 200
reordered by the cross-encoder gives three first hops; from each, the second hops are the store
titles named verbatim in its text (the bridge is almost always the second passage's title,
mentioned in the first) and BM25 of the question with the first hop's title, reordered by the
cross-encoder against the question, five kept per first hop. No key is used to choose; the key only
counts, after, how many of the day's wrong questions have their gold pair among the kept pairs.
Read on round 6's day (88 wrong): 17 from the histories, 36 here, 62 anywhere in the pool. Scored
against the question with the first passage 33, with the first title 30; two kept a first hop 26,
24 and 21 (`state/wide/round6-choosers.log`; the comparison's code was not kept).

    .venv/Scripts/python scripts/wide_pairs.py --from hotpot-20261107T063014Z --skip 1000
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.hotpot import normalise, questions  # noqa: E402
from openplexus.lexical import LexicalIndex  # noqa: E402
from openplexus.rerank import Reranker  # noqa: E402
from openplexus.wiki import WikiTexts  # noqa: E402

LONGEST = 6  # words in a title looked for in a passage


def title_rows(wiki: WikiTexts, log=print) -> dict[str, int]:
    """Every store title, normalised, to its row; the first row wins where titles collide."""
    out: dict[str, int] = {}
    t0 = time.time()
    for i in range(wiki.n):
        out.setdefault(normalise(wiki.title(i)), i)
        if i and i % 1_000_000 == 0:
            log(f"titles {i}/{wiki.n} in {time.time() - t0:.0f}s")
    return out


def named(text: str, titles: dict[str, int], self_row: int) -> list[int]:
    """The rows whose titles appear in the text, as runs of up to LONGEST words starting at a capital."""
    body = text.split(": ", 1)[-1]
    tokens = re.findall(r"[\w'&.-]+", body)
    rows = []
    for at, w in enumerate(tokens):
        if not w[:1].isupper():
            continue
        for n in range(LONGEST, 0, -1):
            span = " ".join(tokens[at:at + n])
            r = titles.get(normalise(span))
            if r is not None and r != self_row and r not in rows:
                rows.append(r)
                break
    return rows


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--from", dest="source", required=True, help="the day's asking reading, for which questions were wrong")
    p.add_argument("--skip", type=int, default=1000)
    p.add_argument("--n", type=int, default=200)
    p.add_argument("--deep", type=int, default=200, help="BM25 rows the cross-encoder reorders for the first hop")
    p.add_argument("--first", type=int, default=3)
    p.add_argument("--second", type=int, default=5, help="second hops kept a first hop")
    p.add_argument("--wiki", default=str(ROOT / "data" / "wiki-abstracts" / "minilm"))
    p.add_argument("--bm25", default=str(ROOT / "data" / "wiki-abstracts" / "bm25"))
    p.add_argument("--out", help="the candidate pairs as jsonl, for the sleep to write lines from")
    args = p.parse_args()

    day = {r["id"]: r for r in json.loads((ROOT / "readings" / f"{args.source}.json").read_text(encoding="utf-8"))["arms"]["wiki:ask"]["rows"]}
    qs = [q for q in questions(args.n, skip=args.skip) if q["id"] in day]
    wiki = WikiTexts(Path(args.wiki))
    bm25 = LexicalIndex(Path(args.bm25))
    rr = Reranker(wiki.text)
    titles = title_rows(wiki)
    print(f"{len(titles)} titles; {len(qs)} questions, {sum(not day[q['id']]['lenient'] for q in qs)} wrong on the day", flush=True)

    counts = {"wrong": 0, "wrong_pair": 0, "wrong_pool": 0, "right": 0, "right_pair": 0}
    out = open(args.out, "w", encoding="utf-8") if args.out else None
    t0 = time.time()
    for k, q in enumerate(qs):
        rows = bm25.nearest([q["question"]], args.deep)[0]
        s = rr.scores(q["question"], rows)
        first = [r for _, r in sorted(zip(s, rows), key=lambda x: -x[0])][:args.first]
        pairs, pool = [], set()
        for f in first:
            cands = named(wiki.text(f), titles, f)
            for r in bm25.nearest([f"{q['question']} {wiki.title(f)}"], 50)[0]:
                if r != f and r not in cands:
                    cands.append(r)
            pool.update((f, c) for c in cands)
            cs = rr.scores(q["question"], cands) if cands else []
            pairs.extend((f, c, score) for score, c in sorted(zip(cs, cands), key=lambda x: -x[0])[:args.second])
        # the key only counts, after the choosing
        gold = {p_["title"] for p_ in q["paragraphs"] if p_["supporting"]}
        has = lambda ps: any({wiki.title(a), wiki.title(b)} == gold for a, b, *_ in ps)
        if not day[q["id"]]["lenient"]:
            counts["wrong"] += 1
            counts["wrong_pair"] += has(pairs)
            counts["wrong_pool"] += has(pool)
        else:
            counts["right"] += 1
            counts["right_pair"] += has(pairs)
        if out:
            for a, b, score in pairs:
                out.write(json.dumps({"from": q["id"], "a_row": a, "b_row": b, "a": wiki.title(a), "b": wiki.title(b),
                                      "score": score}, ensure_ascii=False) + "\n")
        if (k + 1) % 20 == 0:
            print(f"{k + 1}/{len(qs)} {time.time() - t0:.0f}s {counts}", flush=True)
    print(f"== wide pairs: of {counts['wrong']} wrong on the day, the gold pair among the kept pairs for "
          f"{counts['wrong_pair']} (in the whole pool {counts['wrong_pool']}); of {counts['right']} right, {counts['right_pair']}",
          flush=True)

if __name__ == "__main__":
    main()
