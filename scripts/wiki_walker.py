"""Step 6: recall keyed on the trunk's state, over the full store. The second hop of a HotpotQA
question is the one the asker describes rather than names ("the series with companion books
about enslaved worlds"), where a word index cannot follow and a sentence key over 5.2 million
abstracts is weak. Here the query is not text at all: the trunk reads the question and the first
supporting abstract (`hop_prompt`), its last-token hidden state at one layer is mapped by a
ridge-fitted linear head (`LinearHead`, step 2's walker) into MiniLM's key space, and that vector
searches the store's dense keys. Fitted on HotpotQA train pairs, read teacher-forced on the
validation questions every other reading uses: given the right first abstract, is the other
supporting article in the top k among 5.2 million. Beside it the two text senses from
`wiki_recall.py`: MiniLM of the question with the abstract appended (0.235 any@2) and BM25 of the
same (0.335).

    .venv/Scripts/python scripts/wiki_walker.py --stage states --n-train 2000     # the card, resumable
    .venv/Scripts/python scripts/wiki_walker.py --stage fit,read                  # fit on CPU, read with the store on the card

Writes state/wiki-walker/ (states, the head) and readings/wiki-walker-*.json.
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
sys.path.insert(0, str(ROOT / "scripts"))

from openplexus.heads import LinearHead, hop_prompt  # noqa: E402
from openplexus.hotpot import paragraph_text, questions  # noqa: E402
from openplexus.lexical import LexicalIndex  # noqa: E402
from openplexus.wiki import DIM, WikiStore, WikiTexts  # noqa: E402
from wiki_recall import hits  # noqa: E402

OUT = ROOT / "state" / "wiki-walker"


def rows_of(store_path: Path, wanted: set[str]) -> dict[str, int]:
    """Title -> store row for the titles wanted, one pass over titles.txt."""
    out = {}
    with open(store_path / "titles.txt", encoding="utf-8") as f:
        for i, line in enumerate(f):
            t = line.rstrip("\n")
            if t in wanted:
                out.setdefault(t, i)
    return out


def pairs(qs: list[dict], rows: dict[str, int]) -> list[dict]:
    """One example per (question, supporting abstract read): the prompt, the row read, the row
    wanted. Questions whose two supporting titles are not both in the store are left out."""
    out = []
    for q in qs:
        gold = [p for p in q["paragraphs"] if p["supporting"]]
        if len(gold) != 2 or any(p["title"] not in rows for p in gold):
            continue
        for read, want in ((gold[0], gold[1]), (gold[1], gold[0])):
            out.append({"id": q["id"], "prompt": hop_prompt(q["question"], [paragraph_text(read)]),
                        "read": rows[read["title"]], "want": rows[want["title"]], "want_title": want["title"],
                        "question": q["question"], "read_text": paragraph_text(read)})
    return out


def keys_of(store_path: Path, n: int, rows: list[int]) -> np.ndarray:
    keys = np.memmap(store_path / "keys.f16", dtype=np.float16, mode="r", shape=(n, DIM))
    return np.stack([np.array(keys[r], dtype=np.float32) for r in rows])


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--stage", default="states,fit,read")
    p.add_argument("--n-train", type=int, default=2000, help="train questions (two examples each)")
    p.add_argument("--n-val", type=int, default=200)
    p.add_argument("--layers", default="12,16,20")
    p.add_argument("--lams", default="10,100,1000,10000")
    p.add_argument("--k", default="1,2,5,10")
    p.add_argument("--store", default=str(ROOT / "data" / "wiki-abstracts" / "minilm"))
    p.add_argument("--bm25", default=str(ROOT / "data" / "wiki-abstracts" / "bm25"))
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--note", default="")
    args = p.parse_args()
    stages = args.stage.split(",")
    layers = [int(x) for x in args.layers.split(",")]
    ks = [int(k) for k in args.k.split(",")]
    OUT.mkdir(parents=True, exist_ok=True)
    store_path = Path(args.store)
    texts = WikiTexts(store_path)

    train_qs = questions(args.n_train, split="train")
    val_qs = questions(args.n_val)
    wanted = {p["title"] for q in train_qs + val_qs for p in q["paragraphs"] if p["supporting"]}
    rows = rows_of(store_path, wanted)
    train, val = pairs(train_qs, rows), pairs(val_qs, rows)
    print(f"train examples {len(train)} of {2 * len(train_qs)}, val {len(val)} of {2 * len(val_qs)}", flush=True)

    if "states" in stages:
        from openplexus.trunk import LocalTrunk, states_resumable

        trunk = LocalTrunk(batch=args.batch)
        states_resumable(trunk, [x["prompt"] for x in train], layers, OUT / "train.part")
        states_resumable(trunk, [x["prompt"] for x in val], layers, OUT / "val.part")
        trunk.close()

    def states(name, n):
        # states_resumable names the files by with_suffix, so "train.part" becomes train.l12.f32
        return {layer: np.fromfile((OUT / name).with_suffix(f".l{layer}.f32"), dtype=np.float32).reshape(n, -1)
                for layer in layers}

    reading: dict = {"kind": "wiki-walker", "train_examples": len(train), "val_examples": len(val), "layers": layers,
                     "store_rows": texts.n, "k": ks}
    if "fit" in stages:
        h_train = states("train.part", len(train))
        targets = keys_of(store_path, texts.n, [x["want"] for x in train])
        held = len(train) // 5  # the last fifth of train, by question, chooses layer and ridge
        best, table = None, {}
        for layer in layers:
            for lam in [float(x) for x in args.lams.split(",")]:
                head = LinearHead.fit(h_train[layer][:-held], targets[:-held], lam, layer, "minilm")
                q = head.query(h_train[layer][-held:])
                cos = float(np.mean(np.sum(q * targets[-held:], axis=1) / np.maximum(np.linalg.norm(q, axis=1), 1e-6)))
                table[f"l{layer}-lam{lam:g}"] = round(cos, 4)
                if best is None or cos > best[0]:
                    best = (cos, layer, lam)
        _, layer, lam = best
        head = LinearHead.fit(h_train[layer], targets, lam, layer, "minilm")
        head.save(OUT / "walker-minilm.npz")
        reading["fit"] = {"held_out_cosine": table, "chosen": {"layer": layer, "lam": lam}}
        print(f"fit: chose layer {layer} lam {lam:g}; held-out cosine {table}", flush=True)

    if "read" in stages:
        from openplexus.embed import MiniLM
        from openplexus.heads import load_head

        head = load_head(OUT / "walker-minilm.npz")
        h_val = states("val.part", len(val))[head.layer]
        store = WikiStore(store_path)
        deep = max(ks)
        golds = [{x["want_title"]} for x in val]
        exclude = [x["read"] for x in val]

        def ranked(queries_or_rows):
            return [[r for r in rs if r != ex][:deep] for rs, ex in zip(queries_or_rows, exclude)]

        senses = {}
        t0 = time.perf_counter()
        q = head.query(h_val)
        q = q / np.maximum(np.linalg.norm(q, axis=1, keepdims=True), 1e-6)
        senses["walker"] = ranked(store.nearest(q.astype(np.float32), deep + 1))
        timings = {"walker_s": round(time.perf_counter() - t0, 1)}
        embed = MiniLM()
        text_queries = [x["question"] + " " + x["read_text"] for x in val]
        t0 = time.perf_counter()
        senses["minilm"] = ranked(store.nearest(embed(text_queries), deep + 1))
        timings["minilm_s"] = round(time.perf_counter() - t0, 1)
        t0 = time.perf_counter()
        senses["bm25"] = ranked(LexicalIndex(args.bm25).nearest(text_queries, deep + 1))
        timings["bm25_s"] = round(time.perf_counter() - t0, 1)
        reading["second_hop"] = {name: hits(r, golds, texts.title, ks) for name, r in senses.items()}
        reading["timings"] = timings
        reading["walker"] = {"layer": head.layer, "lam": head.lam, "prompt": hop_prompt("<question>", ["<abstract read>"])}
        for name, r in reading["second_hop"].items():
            print(f"== {name}: second hop any@1 {r['1']['any']} any@2 {r['2']['any']} any@10 {r['10']['any'] if '10' in r else '-'}", flush=True)
        store.close()

    reading.update({
        "command": "scripts/wiki_walker.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
        "trunk": "Qwen/Qwen3.5-2B fp32 in process", "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"wiki-walker-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {out}", flush=True)


if __name__ == "__main__":
    main()
