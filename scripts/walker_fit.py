"""Step 2: fit the walker in closed form and read it teacher-forced.

For every question in the train split, each supporting row in the archive's order is one
example: the trunk reads the question and the supporting rows before it (`hop_prompt`), its
last-token hidden state at a chosen layer is the input, and the MiniLM key of the next
supporting row is the target. Ridge regression gives the map in seconds. Layer and ridge
strength are picked on held-out train stories, never on test.

The reading is teacher-forced recall: given the right rows so far, does the walker's query
rank the next right row first among the story's rows heard so far (recall@1), or in the top
four. Beside it, the untrained line: the same ranking with the question's MiniLM key at the
first hop and the last-read row's own key after that, which is what `hops:minilm` does.
End-to-end, with the walker choosing its own rows, is `store_reading.py ask --trunk local`.

    .venv/Scripts/python scripts/walker_fit.py --tasks 1,2,3 --layers 8,12,16,20,24 \
        --lams 1,10,100,1000 --out state/walker-step2 --store state/store-step1

Writes <out>/walker-minilm.npz and readings/walker-fit-*.json. Needs the card to itself.
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

from openplexus.babi import Story, stories  # noqa: E402
from openplexus.store import Store  # noqa: E402
from openplexus.trunk import LocalTrunk  # noqa: E402
from openplexus.walker import LinearWalker, hop_prompt  # noqa: E402


def examples(ss: list[Story]) -> list[dict]:
    """One example per supporting row per question: what was read before it, and it."""
    out = []
    for s in ss:
        for qi, q in enumerate(s.questions):
            for hop, target in enumerate(q.supporting):
                read = [s.sentences[i] for i in q.supporting[:hop]]
                out.append({"story": s, "qi": qi, "hop": hop, "target": target,
                            "prompt": hop_prompt(q.text, read),
                            "last_read": q.supporting[hop - 1] if hop else None})
    return out


def recall(queries: np.ndarray, exs: list[dict], key_of, at: tuple[int, ...] = (1, 4)) -> dict:
    """Rank of the target among the story's rows heard so far, by cosine to the query. Rows
    already read are not candidates, as a walk never follows a row twice; without this the
    untrained line, whose query is the last-read row's own key, would rank that row first."""
    ranks = []
    for q, ex in zip(queries, exs):
        s, question = ex["story"], ex["story"].questions[ex["qi"]]
        read = set(question.supporting[:ex["hop"]])
        candidates = [i for i in range(question.heard) if i not in read]
        keys = np.stack([key_of(s, i) for i in candidates])
        scores = keys @ (q / max(float(np.linalg.norm(q)), 1e-12))
        # ties by time, earliest first, as the store does
        order = np.lexsort((np.arange(len(scores)), -scores))
        ranks.append(int(np.nonzero(order == candidates.index(ex["target"]))[0][0]) + 1)
    ranks_a = np.array(ranks)
    out = {f"recall@{n}": round(float(np.mean(ranks_a <= n)), 3) for n in at}
    out["n"] = len(ranks)
    return out


def by_task_and_hop(queries: np.ndarray, exs: list[dict], key_of) -> dict:
    out: dict = {}
    for t in sorted({ex["story"].task for ex in exs}):
        for hop in sorted({ex["hop"] for ex in exs if ex["story"].task == t}):
            idx = [i for i, ex in enumerate(exs) if ex["story"].task == t and ex["hop"] == hop]
            out[f"task{t}-hop{hop + 1}"] = recall(queries[idx], [exs[i] for i in idx], key_of)
    out["all"] = recall(queries, exs, key_of)
    return out


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--tasks", default="1,2,3")
    p.add_argument("--layers", default="8,12,16,20,24")
    p.add_argument("--lams", default="1,10,100,1000")
    p.add_argument("--holdout", type=int, default=160,
                   help="train stories from this index on are held out")
    p.add_argument("--out", default="state/walker-step2")
    p.add_argument("--store", default="state/store-step1", help="the test stories' store")
    p.add_argument("--test-stories", type=int, default=100)
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--note", default="")
    args = p.parse_args()
    tasks = [int(t) for t in args.tasks.split(",")]
    layers = [int(x) for x in args.layers.split(",")]
    lams = [float(x) for x in args.lams.split(",")]
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    train = stories(tasks, split="train")
    fit_part = [s for s in train if s.index < args.holdout]
    held = [s for s in train if s.index >= args.holdout]
    test = [s for s in stories(tasks) if s.index < args.test_stories]
    store = Store(args.store)
    first_id: dict[str, int] = {}
    for r in store.rows:
        first_id.setdefault(r.episode, r.id)

    # Keys: MiniLM for every train sentence (computed here) and for the test rows (the store's).
    from openplexus.embed import MiniLM

    minilm = MiniLM()
    train_keys: dict[str, np.ndarray] = {}
    for s in train:
        train_keys[s.episode] = minilm(s.sentences)
    question_keys = {(s.episode, qi): minilm([q.text])[0]
                     for s in train + test for qi, q in enumerate(s.questions)}
    minilm.close()

    def train_key(s: Story, i: int) -> np.ndarray:
        return train_keys[s.episode][i]

    def test_key(s: Story, i: int) -> np.ndarray:
        return store.vector("minilm", first_id[s.episode] + i)

    def untrained_queries(exs: list[dict], key_of) -> np.ndarray:
        return np.stack([
            question_keys[(ex["story"].episode, ex["qi"])] if ex["last_read"] is None
            else key_of(ex["story"], ex["last_read"]) for ex in exs])

    fit_ex, held_ex, test_ex = examples(fit_part), examples(held), examples(test)
    print(f"examples: fit {len(fit_ex)} held {len(held_ex)} test {len(test_ex)}", flush=True)

    trunk = LocalTrunk(batch=args.batch)
    started = time.perf_counter()
    fit_h = trunk.hidden([ex["prompt"] for ex in fit_ex], layers)
    held_h = trunk.hidden([ex["prompt"] for ex in held_ex], layers)
    test_h = trunk.hidden([ex["prompt"] for ex in test_ex], layers)
    hidden_seconds = time.perf_counter() - started
    print(f"hidden states in {hidden_seconds:.0f}s", flush=True)
    trunk.close()

    fit_targets = np.stack([train_key(ex["story"], ex["target"]) for ex in fit_ex])
    sweep = []
    best = None
    for layer in layers:
        for lam in lams:
            t0 = time.perf_counter()
            w = LinearWalker.fit(fit_h[layer], fit_targets, lam, layer, "minilm")
            r = recall(w.query(held_h[layer]), held_ex, train_key)
            sweep.append({"layer": layer, "lam": lam, "held": r,
                          "fit_seconds": round(time.perf_counter() - t0, 2)})
            print(f"layer {layer} lam {lam}: held {r}", flush=True)
            if best is None or r["recall@1"] > best[0]:
                best = (r["recall@1"], layer, lam)
    _, layer, lam = best
    all_ex = fit_ex + held_ex
    all_h = np.concatenate([fit_h[layer], held_h[layer]])
    all_targets = np.stack([train_key(ex["story"], ex["target"]) for ex in all_ex])
    walker = LinearWalker.fit(all_h, all_targets, lam, layer, "minilm")
    walker.save(out / "walker-minilm.npz")

    reading = {
        "kind": "walker-fit",
        "step": 2,
        "trunk": trunk.served_model(),
        "key": "minilm",
        "tasks": tasks,
        "train_stories_fit": args.holdout,
        "examples": {"fit": len(fit_ex), "held": len(held_ex), "test": len(test_ex)},
        "prompt_example": test_ex[0]["prompt"] if test_ex else None,
        "chosen": {"layer": layer, "lam": lam},
        "sweep": sweep,
        "hidden_seconds": round(hidden_seconds, 1),
        "teacher_forced": {
            "walker_test": by_task_and_hop(walker.query(test_h[layer]), test_ex, test_key),
            "untrained_test": by_task_and_hop(untrained_queries(test_ex, test_key), test_ex,
                                              test_key),
            "walker_held": by_task_and_hop(
                LinearWalker.fit(fit_h[layer], fit_targets, lam, layer, "minilm")
                .query(held_h[layer]), held_ex, train_key),
            "untrained_held": by_task_and_hop(untrained_queries(held_ex, train_key), held_ex,
                                              train_key),
        },
        "walker_path": str(out / "walker-minilm.npz"),
        "command": " ".join(sys.argv),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True,
                              text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
    }
    print(json.dumps(reading["teacher_forced"], indent=1))
    path = ROOT / "readings" / f"walker-fit-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print("wrote", path)


if __name__ == "__main__":
    main()
