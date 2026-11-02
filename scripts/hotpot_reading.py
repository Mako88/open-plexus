"""Step 6's lines on HotpotQA (`openplexus.hotpot`), the distractor form: the trunk answers a
question with the paragraphs an arm puts in front of it. Arms: `floor` (no paragraphs, what the
trunk knows), `gold` (the two supporting paragraphs, the ceiling), `episode` (all ten, the
whole-episode ceiling), `top:K` (the K paragraphs nearest the question by MiniLM, the store with
no learnt asking, the control for the asking arm), `ask` (the trunk given the search tool over
the ten, asking as it chooses, up to `--max-turns`; with `--adapter` the trained asker);
`recall:K` (automatic recall: the paragraph nearest the question, then the one nearest the text
of the paragraph just recalled, K in all, shown with no action; John's memory-like form);
`wiki:top:K` and `wiki:ask`, the same two over the full-form store, every Wikipedia abstract
(`--wiki`, built by `wiki_build.py`), where the supporting paragraphs are two rows in 5.2 million
and `gold_shown` counts them by title; `--sense lexical|dense` is how that store is searched
(BM25 from `--bm25`, the default since it read better at both hops and as the asker's tool; or the
MiniLM keys), named in the tag unless dense. Each
arm is compared paired with `--against` on the same questions by exact match, and stops once
clear of the first named; an entry is an arm read plain (`ask`, `top:2`) or a part file's tag
(`ask-ask-round2`, a trained asker's reading). Rows are appended to state/ and a
re-run skips the questions read. Short answers, so the Q8 server is the engine (`--served`,
default); `--adapter` names a GGUF LoRA the server has loaded to apply.

    .venv/Scripts/python scripts/hotpot_reading.py --arms floor,gold,episode,top:2 --n 200

Writes readings/hotpot-*.json.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from ask import SYSTEM as ASK_SYSTEM  # noqa: E402
from ask import Store, embedder  # noqa: E402
from grpo_reading import paired  # noqa: E402
from openplexus.asking import solve  # noqa: E402
from openplexus.hotpot import answer_of, exact, f1, paragraph_text, questions, recall  # noqa: E402
from openplexus.maths import rows_of  # noqa: E402
from openplexus.lexical import LexicalIndex  # noqa: E402
from openplexus.wiki import WikiSearch, WikiStore  # noqa: E402

ASKING = ("ask", "wiki:ask")

SYSTEM = ("Answer the question. Use the passages where they are given; they may contain the facts needed. "
          "Think briefly, then end with a last line of the form 'Answer: X' where X is the short answer "
          "(a name, a number, a date, or yes or no), and nothing after it.")


def ask_text(q: dict, paragraphs: list[dict]) -> str:
    if not paragraphs:
        return q["question"]
    passages = "\n\n".join(paragraph_text(p) for p in paragraphs)
    return f"Passages:\n\n{passages}\n\nQuestion: {q['question']}"


def gold_titles(q: dict) -> set[str]:
    return {p["title"] for p in q["paragraphs"] if p["supporting"]}


def as_paragraph(store: WikiStore, i: int, gold: set[str]) -> dict:
    """A store row in the shape of a question's paragraph, so the prompt and the counts read alike."""
    title, text = store.text(i).split(": ", 1)
    return {"title": title, "sentences": [text], "supporting": title in gold}


def sense_of(sense: str, store: WikiStore, embed, bm25: str):
    """The full-form store's search as a function of queries, k and rows to skip: `lexical` is BM25,
    `named` is BM25 with a title named in the query ranked first, `dense` the MiniLM keys. A rank
    fusion of dense and lexical was read 2026-11-02 (`hotpot-20261102T130522Z`), lost to BM25 alone
    and is gone."""
    if sense == "dense":
        return lambda queries, k, exclude: store.nearest(embed(queries), k, exclude)
    return LexicalIndex(bm25, title_of=store.title if sense == "named" else None).nearest


def chosen(spec: str, q: dict, index=None) -> list[dict]:
    if spec == "floor" or spec in ASKING:
        return []
    if spec.startswith("wiki:top:"):
        k = int(spec.split(":")[2])
        picks = index.nearest([q["question"]], k, set())[0]
        return [as_paragraph(index.wiki, i, gold_titles(q)) for i in picks]
    if spec == "gold":
        return [p for p in q["paragraphs"] if p["supporting"]]
    if spec == "episode":
        return q["paragraphs"]
    if spec.startswith("top:"):
        k = int(spec.split(":")[1])
        return index(q, k)
    if spec.startswith("recall:"):
        k = int(spec.split(":")[1])
        return [q["paragraphs"][j] for j in recall([paragraph_text(p) for p in q["paragraphs"]], q["question"], index.embed, k)]
    raise SystemExit(f"unknown arm {spec}")


def tag_of(spec: str, args) -> str:
    tag = spec.replace(":", "")
    if spec.startswith("wiki:") and getattr(args, "sense", "dense") != "dense":
        tag += "-" + args.sense
    if args.adapter:
        tag += "-" + Path(args.adapter).stem.replace("-lora", "")
    if getattr(args, "think", False):
        tag += "-think"
    if getattr(args, "per_search", 1) != 1 and spec in ASKING:
        tag += f"-k{args.per_search}"
    return tag


def part_rows(tag: str) -> dict:
    part = ROOT / "state" / f"hotpot-{tag}.part.jsonl"
    return {r["id"]: r for r in rows_of(part)} if part.exists() else {}


def other_rows(name: str, plain) -> dict:
    """The rows of a comparison arm, named by a part tag (`ask-ask-round2`) or by its spec read
    without this arm's adapter (`ask`, `top:2`)."""
    return part_rows(name) or part_rows(tag_of(name, plain))


def arm(spec: str, qs: list[dict], args, trunk, index) -> dict:
    tag = tag_of(spec, args)
    part = ROOT / "state" / f"hotpot-{tag}.part.jsonl"
    done = part_rows(tag)
    # the arms compared with are read without this arm's adapter: the plain tags
    plain = type(args)(**{**vars(args), "adapter": None})
    # a trained asker is compared with the plain asker too, so only the arm's own rows are left out
    others = {o: other_rows(o, plain) for o in args.against.split(",") if o and o != tag and tag_of(o, plain) != tag}
    others = {o: rows for o, rows in others.items() if rows}
    other = next(iter(others.values()), {})
    started = time.perf_counter()
    todo = [q for q in qs if q["id"] not in done]
    key = lambda rows, q: rows.get(q["id"])  # noqa: E731
    for at in range(0, len(todo), args.batch):
        chunk = todo[at:at + args.batch]
        before_out = trunk.tokens_out
        asked: list[dict | None] = [None] * len(chunk)
        if spec in ASKING:
            embedder()  # once, before the threads: a first import inside several threads at once fails

            def one(q):
                if spec == "wiki:ask":
                    tool = WikiSearch(index.wiki, index.embed, index.nearest, per_search=args.per_search)
                    titles = tool.titles
                else:
                    tool = Store()
                    tool.reset(paragraphs=[{"text": paragraph_text(p), "supporting": p["supporting"]} for p in q["paragraphs"]])
                    titles = lambda: [q["paragraphs"][j]["title"] for j in tool.shown]  # noqa: E731
                try:
                    out = solve(trunk.chat, ASK_SYSTEM, q["question"], tool.search, max_turns=args.max_turns)
                except Exception as e:  # noqa: BLE001
                    # a turn the server refused (a tool call it could not parse): the question
                    # counts as unanswered, with the refusal on the row, and the arm goes on
                    out = {"reply": "", "searches": list(tool.searches), "turns": 1 + len(tool.searches),
                           "messages": [], "failed": str(e)[:200]}
                out["shown"] = titles()
                out["gold_shown"] = sum(title in gold_titles(q) for title in out["shown"])
                return out

            from concurrent.futures import ThreadPoolExecutor
            with ThreadPoolExecutor(max_workers=args.batch) as pool:
                asked = list(pool.map(one, chunk))
            replies = [a["reply"] for a in asked]
        else:
            turns = [[{"role": "system", "content": SYSTEM}, {"role": "user", "content": ask_text(q, chosen(spec, q, index))}]
                     for q in chunk]
            replies = trunk.ask_many(turns)
        tokens_out = trunk.tokens_out - before_out
        with open(part, "a", encoding="utf-8") as f:
            for q, reply, a in zip(chunk, replies, asked):
                said = answer_of(reply)
                shown = chosen(spec, q, index)
                row = {"id": q["id"], "question": q["question"], "answer": q["answer"], "type": q["type"],
                       "level": q["level"], "said": said, "correct": exact(said, q["answer"]),
                       "f1": round(f1(said, q["answer"]), 3), "reply": reply,
                       "shown": a["shown"] if a else [p["title"] for p in shown],
                       "gold_shown": a["gold_shown"] if a else sum(p["supporting"] for p in shown),
                       "passes": round(tokens_out / len(chunk) + (a["turns"] if a else 1), 1)}
                if a:
                    row["searches"] = a["searches"]
                    row["turns"] = a["turns"]
                    if a.get("failed"):
                        row["failed"] = a["failed"]
                done[q["id"]] = row
                f.write(json.dumps(row, ensure_ascii=False) + "\n")
        n = len(done)
        score = sum(done[q["id"]]["correct"] for q in qs if q["id"] in done) / n
        print(f"{tag} {n}/{len(qs)} running {score:.3f}", flush=True)
        if n >= args.min_n and other:
            pr = paired({r["question"]: r for r in done.values()},
                        {r["question"]: r for r in other.values()}, qs)
            if pr and pr["n"] >= args.min_n and abs(pr["standard_errors"]) >= args.clear:
                print(f"{tag} stopped at {n}: {pr['made_right']} made right and {pr['made_wrong']} made wrong "
                      f"against {next(iter(others))}, {pr['standard_errors']} standard errors", flush=True)
                break
    rows = [done[q["id"]] for q in qs if q["id"] in done]
    n = len(rows)
    by_type = {}
    for t in ("bridge", "comparison"):
        sub = [r for r in rows if r["type"] == t]
        if sub:
            by_type[t] = round(sum(r["correct"] for r in sub) / len(sub), 3)
    return {
        "paragraphs": spec, "served_model": trunk.served_model(), "system_prompt": SYSTEM,
        "inputs_the_deployed_system_has": {"floor": "the question", "gold": "NOT: the supporting paragraphs are the key's choice",
                                           "episode": "the question's ten paragraphs, the store's rows",
                                           }.get(spec, "the question and the store"),
        "exact": round(sum(r["correct"] for r in rows) / n, 3), "f1": round(sum(r["f1"] for r in rows) / n, 3),
        "by_type": by_type, "n": n, "stopped_early": n < len(qs), "unanswered": sum(r["said"] is None for r in rows),
        "gold_shown_mean": round(sum(r["gold_shown"] for r in rows) / n, 2),
        "searches_mean": round(sum(len(r.get("searches", [])) for r in rows) / n, 2) if spec in ASKING else None,
        "never_asked": sum(not r.get("searches") for r in rows) if spec in ASKING else None,
        "failed_turns": sum(bool(r.get("failed")) for r in rows) if spec in ASKING else None,
        "store_rows": index.wiki.n if spec.startswith("wiki:") else 10,
        "sense": args.sense if spec.startswith("wiki:") else "the ten paragraphs by MiniLM",
        "passes_per_question": round(sum(r["passes"] for r in rows) / n, 1),
        "paired": {o: paired({r["question"]: r for r in rows}, {r["question"]: r for r in rows_o.values()}, qs)
                   for o, rows_o in others.items()},
        "seconds": round(time.perf_counter() - started, 1), "rows": rows,
    }


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--arms", default="floor,gold,episode,top:2")
    p.add_argument("--n", type=int, default=200)
    p.add_argument("--skip", type=int, default=0)
    p.add_argument("--split", default="validation")
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--max-new", type=int, default=160)
    p.add_argument("--max-turns", type=int, default=4, help="assistant turns an `ask` arm may take")
    p.add_argument("--served", default="http://127.0.0.1:8093")
    p.add_argument("--adapter", default=None, help="a GGUF LoRA file the server has loaded, applied at 1")
    p.add_argument("--think", action="store_true",
                   help="the trunk's thinking mode on (the server must run without --reasoning-budget 0; raise --max-new)")
    p.add_argument("--wiki", default=str(ROOT / "data" / "wiki-abstracts" / "minilm"), help="the full-form store")
    p.add_argument("--bm25", default=str(ROOT / "data" / "wiki-abstracts" / "bm25"), help="its lexical index")
    p.add_argument("--sense", default="lexical", choices=["lexical", "named", "dense"], help="how the full-form store is searched")
    p.add_argument("--per-search", type=int, default=1, help="rows the asker's search returns, nearest first")
    p.add_argument("--against", default="floor")
    p.add_argument("--min-n", type=int, default=100)
    p.add_argument("--clear", type=float, default=2.5)
    p.add_argument("--note", default="")
    args = p.parse_args()
    from openplexus.trunk import ServedTrunk

    qs = questions(args.n, split=args.split, skip=args.skip)
    trunk = ServedTrunk(url=args.served, max_new=args.max_new, parallel=args.batch, think=args.think)
    loaded = trunk.adapters()
    if args.adapter:
        want = Path(args.adapter).name
        if not any(Path(a["path"]).name == want for a in loaded):
            raise SystemExit(f"the server has not loaded {want}: {[a['path'] for a in loaded]}")
    trunk.lora = [{"id": a["id"], "scale": 1.0 if args.adapter and Path(a["path"]).name == Path(args.adapter).name else 0.0}
                  for a in loaded]
    index = None
    if any(s.startswith(("top:", "recall:", "wiki:")) for s in args.arms.split(",")):
        import numpy as np
        from openplexus.embed import MiniLM

        embed = MiniLM()

        def index(q, k):
            keys = np.asarray(embed([paragraph_text(p) for p in q["paragraphs"]]), dtype=np.float32)
            keys /= np.maximum(np.linalg.norm(keys, axis=1, keepdims=True), 1e-12)
            query = np.asarray(embed([q["question"]]), dtype=np.float32)[0]
            query /= max(float(np.linalg.norm(query)), 1e-12)
            order = np.argsort(-(keys @ query))[:k]
            return [q["paragraphs"][int(j)] for j in order]

        index.embed = embed
        if any(s.startswith("wiki:") for s in args.arms.split(",")):
            index.wiki = WikiStore(args.wiki)
            index.nearest = sense_of(args.sense, index.wiki, embed, args.bm25)
            print(f"the full-form store: {index.wiki.n} abstracts on {index.wiki.device}, searched {args.sense}", flush=True)
    reading = {"kind": "hotpot-reading", "set": f"HotpotQA distractor {args.split}, data/hotpotqa/{args.split}.jsonl",
               "skip": args.skip, "n": len(qs), "scorer": "exact match and token F1 on the normalised answer, HotpotQA's own",
               "adapter": args.adapter, "arms": {}}
    for spec in args.arms.split(","):
        a = arm(spec, qs, args, trunk, index)
        reading["arms"][spec] = a
        print(f"== {spec}: exact {a['exact']} f1 {a['f1']} on {a['n']} at {a['passes_per_question']} passes, "
              f"{a['unanswered']} unanswered, by type {a['by_type']}, {a['seconds']}s", flush=True)
    reading.update({
        "command": "scripts/hotpot_reading.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                              cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"hotpot-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
