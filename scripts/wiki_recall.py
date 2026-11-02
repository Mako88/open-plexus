"""Title recall over the full-form store, no trunk: the cheapest reading that decides which sense
the asker searches with. For the first `--n` HotpotQA validation questions, each sense (dense
MiniLM keys, lexical BM25, and their rank fusion) ranks the 5.2 million abstracts, and the
reading counts how often a supporting article is in the top k (the first hop, from the question
alone) and how often the other supporting article is in the top k when the query is the question
with one supporting abstract's text appended (the second hop, as an asker that quotes what it
read would search). Writes readings/wiki-recall-*.json.

    .venv/Scripts/python scripts/wiki_recall.py --n 200 --k 1,2,5,10
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.hotpot import questions  # noqa: E402
from openplexus.lexical import LexicalIndex, fused  # noqa: E402
from openplexus.wiki import WikiStore  # noqa: E402


def hits(ranked: list[list[int]], golds: list[set[str]], title, ks: list[int]) -> dict:
    """Per k: the share of queries with any gold title in the top k, with every gold title, and
    the mean number of gold titles there."""
    out = {}
    for k in ks:
        any_, all_, count = 0, 0, 0
        for rows, gold in zip(ranked, golds):
            found = {title(r) for r in rows[:k]} & gold
            any_ += bool(found)
            all_ += found == gold
            count += len(found)
        n = len(ranked)
        out[str(k)] = {"any": round(any_ / n, 3), "all": round(all_ / n, 3), "mean_found": round(count / n, 2)}
    return out


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--n", type=int, default=200)
    p.add_argument("--k", default="1,2,5,10")
    p.add_argument("--store", default=str(ROOT / "data" / "wiki-abstracts" / "minilm"))
    p.add_argument("--bm25", default=str(ROOT / "data" / "wiki-abstracts" / "bm25"))
    p.add_argument("--note", default="")
    args = p.parse_args()
    ks = [int(k) for k in args.k.split(",")]
    deep = max(ks)
    from openplexus.embed import MiniLM

    qs = questions(args.n)
    golds = [{x["title"] for x in q["paragraphs"] if x["supporting"]} for q in qs]
    store = WikiStore(args.store)
    lexical = LexicalIndex(args.bm25)
    embed = MiniLM()
    present = set()
    with open(Path(args.store) / "titles.txt", encoding="utf-8") as f:
        wanted = set().union(*golds)
        for line in f:
            t = line.rstrip("\n")
            if t in wanted:
                present.add(t)
    in_store = sum(g <= present for g in golds)

    # the first hop: the question alone
    first = {"question": [q["question"] for q in qs]}
    # the second hop: the question with one supporting abstract's text, the other article wanted
    second_queries, second_golds, second_exclude = [], [], []
    for q, gold in zip(qs, golds):
        for para in q["paragraphs"]:
            if para["supporting"]:
                other = gold - {para["title"]}
                text = para["title"] + ": " + "".join(para["sentences"])
                second_queries.append(q["question"] + " " + text)
                second_golds.append(other)
                second_exclude.append(para["title"])
    reading = {"kind": "wiki-recall", "set": f"HotpotQA distractor validation, first {len(qs)} questions",
               "store_rows": store.n, "gold_pairs_in_store": in_store, "k": ks, "senses": {}}
    timings = {}
    for hop, queries, gold_sets in (("first", first["question"], golds), ("second", second_queries, second_golds)):
        t0 = time.perf_counter()
        dense = store.nearest(embed(queries), deep + 1)
        timings[f"dense_{hop}_s"] = round(time.perf_counter() - t0, 1)
        t0 = time.perf_counter()
        lex = lexical.nearest(queries, deep + 1)
        timings[f"lexical_{hop}_s"] = round(time.perf_counter() - t0, 1)
        if hop == "second":
            # the article already read is not a find
            dense = [[r for r in rows if store.title(r) != ex] for rows, ex in zip(dense, second_exclude)]
            lex = [[r for r in rows if store.title(r) != ex] for rows, ex in zip(lex, second_exclude)]
        hybrid = [fused([d, x], deep) for d, x in zip(dense, lex)]
        for name, ranked in (("dense", dense), ("lexical", lex), ("hybrid", hybrid)):
            reading["senses"].setdefault(name, {})[hop] = hits(ranked, gold_sets, store.title, ks)
    reading["timings"] = timings
    reading["second_hop_queries"] = len(second_queries)
    for name, r in reading["senses"].items():
        print(f"== {name}: first hop any@2 {r['first']['2']['any']} all@2 {r['first']['2']['all']} any@10 {r['first']['10']['any'] if '10' in r['first'] else '-'}; "
              f"second hop any@1 {r['second']['1']['any']} any@2 {r['second']['2']['any']}", flush=True)
    reading.update({
        "command": "scripts/wiki_recall.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"wiki-recall-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {out}", flush=True)


if __name__ == "__main__":
    main()
