"""Step 3, first form: fit the evaluator in closed form and read it teacher-forced.

The walker finds the right row in its top four nearly always and in first place about half
the time (`walker_fit.py`), and the trunk given exactly the right rows far outscores the trunk
given those rows among others. So the evaluator judges each row the walker proposes: the
trunk reads the question, the rows read so far and one candidate (`judge_prompt`), its
last-token state at a chosen layer is the input, and the target is +1 if the candidate is the
next supporting row and -1 otherwise. Ridge regression, seconds, no training loop.

Examples come from the train split, teacher-forced: at each hop of each question the walker's
top-k among the story's rows heard so far, with the rows already read excluded, and one more
state per question after every supporting row has been read, where every candidate is wrong
and the right call is to stop. Layer, ridge and the stop threshold are picked on held-out
train stories. The reading is on the test stories: recall@1 of the re-ranked top-k against
the walker's own, per task and hop, and how often the stop state is recognised.

    .venv/Scripts/python scripts/evaluator_fit.py --walker state/walker-step2/walker-minilm.npz \
        --out state/walker-step2 --store state/store-step1

Writes <out>/evaluator.npz and readings/evaluator-fit-*.json. Needs the card to itself.
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

from openplexus.babi import Story, stories  # noqa: E402
from openplexus.heads import LinearHead, hop_prompt, judge_prompt  # noqa: E402
from openplexus.store import Store  # noqa: E402
from openplexus.trunk import LocalTrunk  # noqa: E402


def states(ss: list[Story]) -> list[dict]:
    """One state per hop of each question, plus the done state after the last supporting row;
    `target` is the next supporting row's index, or None when the walk should stop."""
    out = []
    for s in ss:
        for qi, q in enumerate(s.questions):
            for hop in range(len(q.supporting) + 1):
                read = q.supporting[:hop]
                target = q.supporting[hop] if hop < len(q.supporting) else None
                out.append({"story": s, "qi": qi, "hop": hop, "read": read, "target": target,
                            "prompt": hop_prompt(q.text, [s.sentences[i] for i in read])})
    return out


def candidates(walker: LinearHead, hidden: np.ndarray, exs: list[dict], key_of, k: int) -> None:
    """The walker's top-k among the story's rows heard so far, read rows excluded, written
    onto each example as `cands` with `labels`."""
    queries = walker.query(hidden)
    for q, ex in zip(queries, exs):
        s, question = ex["story"], ex["story"].questions[ex["qi"]]
        pool = [i for i in range(question.heard) if i not in ex["read"]]
        if not pool:  # every row heard has been read: nothing to judge, the walk stops
            ex["cands"], ex["labels"] = [], []
            continue
        keys = np.stack([key_of(s, i) for i in pool])
        scores = keys @ (q / max(float(np.linalg.norm(q)), 1e-12))
        order = np.lexsort((np.arange(len(scores)), -scores))[:k]
        ex["cands"] = [pool[i] for i in order]
        ex["labels"] = [1.0 if c == ex["target"] else -1.0 for c in ex["cands"]]


def judge_prompts(exs: list[dict]) -> list[str]:
    return [judge_prompt(ex["story"].questions[ex["qi"]].text,
                         [ex["story"].sentences[i] for i in ex["read"]],
                         ex["story"].sentences[c])
            for ex in exs for c in ex["cands"]]


def flat_labels(exs: list[dict]) -> np.ndarray:
    return np.array([lab for ex in exs for lab in ex["labels"]], dtype=np.float32)


def read(scores: np.ndarray, exs: list[dict], threshold: float | None) -> dict:
    """Per task and hop: among states whose target is in the walker's top-k, how often the
    evaluator's best is the target (recall@1), beside the walker's own first choice; at done
    states, how often no candidate clears the threshold; at other states, how often the best
    does, so a true stop is not an early one."""
    at = 0
    per: dict[str, dict] = {}
    for ex in exs:
        n = len(ex["cands"])
        sc = scores[at:at + n]
        at += n
        t = ex["story"].task
        name = f"task{t}-hop{ex['hop'] + 1}" if ex["target"] is not None else f"task{t}-done"
        d = per.setdefault(name, {"n": 0, "in_topk": 0, "eval@1": 0, "walker@1": 0,
                                  "stop": 0, "go": 0})
        d["n"] += 1
        if n == 0:
            d["stop"] += 1  # nothing left to follow
            continue
        best = int(np.argmax(sc))
        if ex["target"] is None:
            if threshold is not None and sc[best] <= threshold:
                d["stop"] += 1
            continue
        if threshold is not None and sc[best] > threshold:
            d["go"] += 1
        if ex["target"] in ex["cands"]:
            d["in_topk"] += 1
            d["eval@1"] += int(ex["cands"][best] == ex["target"])
            d["walker@1"] += int(ex["cands"][0] == ex["target"])
    out = {}
    for name, d in sorted(per.items()):
        if name.endswith("done"):
            out[name] = {"n": d["n"], "stopped": round(d["stop"] / d["n"], 3)}
        else:
            out[name] = {"n": d["n"], "target_in_topk": round(d["in_topk"] / d["n"], 3),
                         "eval_recall@1": round(d["eval@1"] / max(1, d["in_topk"]), 3),
                         "walker_recall@1": round(d["walker@1"] / max(1, d["in_topk"]), 3),
                         "went_on": round(d["go"] / d["n"], 3)}
    return out


def pick_threshold(scores: np.ndarray, labels: np.ndarray) -> float:
    """The score that best separates right candidates from wrong ones on the held-out set, by
    balanced accuracy over a sweep of the scores' range."""
    best = (0.0, 0.0)
    for tau in np.quantile(scores, np.linspace(0.02, 0.98, 97)):
        tpr = float(np.mean(scores[labels > 0] > tau))
        tnr = float(np.mean(scores[labels < 0] <= tau))
        if (tpr + tnr) / 2 > best[0]:
            best = ((tpr + tnr) / 2, float(tau))
    return best[1]


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--tasks", default="1,2,3")
    p.add_argument("--layers", default="12,16,20,24")
    p.add_argument("--lams", default="1,10,100,1000")
    p.add_argument("--k", type=int, default=4)
    p.add_argument("--holdout", type=int, default=160)
    p.add_argument("--train-stories", type=int, default=None,
                   help="cap on train stories a task; a small cap smokes the whole script in a "
                        "minute before a long run")
    p.add_argument("--walker", default="state/walker-step2/walker-minilm.npz")
    p.add_argument("--out", default="state/walker-step2")
    p.add_argument("--store", default="state/store-step1")
    p.add_argument("--test-stories", type=int, default=100)
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--note", default="")
    args = p.parse_args()
    tasks = [int(t) for t in args.tasks.split(",")]
    layers = [int(x) for x in args.layers.split(",")]
    lams = [float(x) for x in args.lams.split(",")]
    walker = LinearHead.load(args.walker)
    Path(args.out).mkdir(parents=True, exist_ok=True)

    train = stories(tasks, split="train")
    if args.train_stories:
        train = [s for s in train if s.index < args.train_stories]
    fit_part = [s for s in train if s.index < args.holdout]
    held = [s for s in train if s.index >= args.holdout]
    test = [s for s in stories(tasks) if s.index < args.test_stories]
    store = Store(args.store)
    first_id: dict[str, int] = {}
    for r in store.rows:
        first_id.setdefault(r.episode, r.id)

    from openplexus.embed import MiniLM

    minilm = MiniLM()
    train_keys = {s.episode: minilm(s.sentences) for s in train}
    minilm.close()

    def train_key(s: Story, i: int) -> np.ndarray:
        return train_keys[s.episode][i]

    def test_key(s: Story, i: int) -> np.ndarray:
        return store.vector("minilm", first_id[s.episode] + i)

    fit_ex, held_ex, test_ex = states(fit_part), states(held), states(test)
    print(f"states: fit {len(fit_ex)} held {len(held_ex)} test {len(test_ex)}", flush=True)

    trunk = LocalTrunk(batch=args.batch)
    started = time.perf_counter()
    for exs, key_of in ((fit_ex, train_key), (held_ex, train_key), (test_ex, test_key)):
        h = trunk.hidden([ex["prompt"] for ex in exs], [walker.layer])[walker.layer]
        candidates(walker, h, exs, key_of, args.k)
    walk_seconds = time.perf_counter() - started
    started = time.perf_counter()
    fit_h = trunk.hidden(judge_prompts(fit_ex), layers)
    held_h = trunk.hidden(judge_prompts(held_ex), layers)
    test_h = trunk.hidden(judge_prompts(test_ex), layers)
    judge_seconds = time.perf_counter() - started
    print(f"walker states in {walk_seconds:.0f}s, judge states in {judge_seconds:.0f}s", flush=True)
    trunk.close()

    fit_y, held_y, test_y = flat_labels(fit_ex), flat_labels(held_ex), flat_labels(test_ex)
    sweep, best = [], None
    for layer in layers:
        for lam in lams:
            head = LinearHead.fit(fit_h[layer], fit_y[:, None], lam, layer, "label")
            sc = head.query(held_h[layer])[:, 0]
            tau = pick_threshold(sc, held_y)
            r = read(sc, held_ex, tau)
            # the number to pick on: recall@1 over every held state whose target is in the top-k
            hits = sum(v["eval_recall@1"] * v["target_in_topk"] * v["n"]
                       for k_, v in r.items() if not k_.endswith("done"))
            n = sum(v["target_in_topk"] * v["n"] for k_, v in r.items() if not k_.endswith("done"))
            score = hits / max(1, n)
            sweep.append({"layer": layer, "lam": lam, "threshold": round(tau, 4),
                          "held_recall@1": round(score, 3)})
            print(f"layer {layer} lam {lam}: held recall@1 {score:.3f} tau {tau:.3f}", flush=True)
            if best is None or score > best[0]:
                best = (score, layer, lam, tau)
    _, layer, lam, tau = best
    all_h = np.concatenate([fit_h[layer], held_h[layer]])
    all_y = np.concatenate([fit_y, held_y])
    evaluator = LinearHead.fit(all_h, all_y[:, None], lam, layer, "label")
    evaluator.threshold = tau
    evaluator.save(Path(args.out) / "evaluator.npz")

    held_head = LinearHead.fit(fit_h[layer], fit_y[:, None], lam, layer, "label")
    reading = {
        "kind": "evaluator-fit",
        "step": 3,
        "trunk": trunk.served_model(),
        "walker": {"path": args.walker, "layer": walker.layer, "lam": walker.lam},
        "k": args.k,
        "tasks": tasks,
        "train_stories_fit": args.holdout,
        "states": {"fit": len(fit_ex), "held": len(held_ex), "test": len(test_ex)},
        "judgements": {"fit": int(len(fit_y)), "held": int(len(held_y)), "test": int(len(test_y))},
        "prompt_example": judge_prompts(test_ex[1:2])[0] if len(test_ex) > 1 else None,
        "chosen": {"layer": layer, "lam": lam, "threshold": round(tau, 4)},
        "sweep": sweep,
        "seconds": {"walker_states": round(walk_seconds, 1),
                    "judge_states": round(judge_seconds, 1)},
        "teacher_forced": {
            "test": read(evaluator.query(test_h[layer])[:, 0], test_ex, tau),
            "held": read(held_head.query(held_h[layer])[:, 0], held_ex, tau),
        },
        "evaluator_path": str(Path(args.out) / "evaluator.npz"),
        "command": " ".join(sys.argv),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True,
                              text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
    }
    print(json.dumps(reading["teacher_forced"]["test"], indent=1))
    path = ROOT / "readings" / f"evaluator-fit-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print("wrote", path)


if __name__ == "__main__":
    main()
