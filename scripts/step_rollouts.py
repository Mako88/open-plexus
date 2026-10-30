"""Step 3 on maths, fork (a): candidate steps labelled by outcome, from the system's own rollouts.

No human label. On GSM8K's training problems the served trunk walks its own solution a step at
a time; at each step it proposes k sampled next steps and its forced answer line, and every
candidate is labelled by rolling it out: m continuations by the trunk's own steps to an answer
line, the label the fraction that reach the gold number (an answer line is labelled by itself).
The walk then follows one of the sampled steps at random, so the labelled steps lie on the
trunk's own paths, right and wrong, as they do under search. This is Math-Shepherd's method
(Wang et al. 2023). `features` then caches the trunk's state over the judge prompt of each
labelled candidate, fit/held/test split by problem and as many wrong as right, in the shape
`step_evaluator.py fit` reads, so the same head is fitted to these labels and read against the
PRM800K-trained one.

    .venv/Scripts/python scripts/step_rollouts.py rollouts --n 1500          # the server up
    .venv/Scripts/python scripts/step_rollouts.py features --out state/features/rollouts   # the card
    .venv/Scripts/python scripts/step_evaluator.py fit --features state/features/rollouts --out state/steps-rollouts

Rows go to state/rollouts/train.jsonl as each chunk of problems finishes; a re-run skips the
problems done. The deployed system has every input here: the problem and its own trunk.
"""

from __future__ import annotations

import argparse
import json
import random
import subprocess
import sys
import time
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.maths import problems, same_number  # noqa: E402
from openplexus.steps import answer_of, clean_step, judge_prompt, rollout_many, step_messages  # noqa: E402

ROWS = ROOT / "state" / "rollouts" / "train.jsonl"


def sha() -> str:
    return subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                          cwd=ROOT).stdout.strip()


def rollouts(args) -> None:
    from openplexus.trunk import ServedTrunk

    trunk = ServedTrunk(parallel=args.parallel)
    ps = problems(args.n, split="train")
    ROWS.parent.mkdir(parents=True, exist_ok=True)
    done = set()
    if ROWS.exists():
        done = {json.loads(line)["index"] for line in ROWS.read_text(encoding="utf-8").splitlines()}
    rng = random.Random(args.seed)
    seeds = [args.seed * 1_000_003]

    def sampled_many(asks, k):
        out = trunk.propose([step_messages(q, s) for q, s in asks], k, args.temperature, args.max_step,
                            seed=seeds[0])
        seeds[0] += k * len(asks)
        return out

    def answer_many(asks):
        return [a[0] for a in trunk.propose([step_messages(q, s) for q, s in asks], 1, 0.0, 16, prefix="Answer:")]

    started = time.perf_counter()
    todo = [(i, p) for i, p in enumerate(ps) if i not in done]
    for at in range(0, len(todo), args.chunk):
        chunk = todo[at:at + args.chunk]
        spines = {i: [] for i, _ in chunk}
        gold = {i: p["answer"] for i, p in chunk}
        question = {i: p["question"] for i, p in chunk}
        live = [i for i, _ in chunk]
        rows = []
        for depth in range(args.depth):
            if not live:
                break
            asks = [(question[i], spines[i]) for i in live]
            cands = sampled_many(asks, args.k)
            answers = answer_many(asks)
            starts, owners = [], []
            for i, cs, a in zip(live, cands, answers):
                a = clean_step(a)
                rows.append({"index": i, "question": question[i], "answer": gold[i], "steps": list(spines[i]),
                             "candidate": a, "depth": depth, "is_answer_line": True, "rollouts": [a],
                             "fraction": 1.0 if same_number(answer_of(a), gold[i]) else 0.0})
                steps = [c for c in dict.fromkeys(clean_step(c) for c in cs)
                         if c and c not in spines[i] and answer_of(c) is None]
                for c in steps:
                    for _ in range(args.m):
                        starts.append((question[i], spines[i] + [c]))
                        owners.append((i, c))
            added = rollout_many(starts, sampled_many, answer_many, depth=args.rollout_depth)
            by_cand: dict[tuple, list] = {}
            for (i, c), tail in zip(owners, added):
                by_cand.setdefault((i, c), []).append(tail[-1] if tail else "")
            still = []
            for i in live:
                mine = [c for (j, c) in by_cand if j == i]
                for c in mine:
                    ends = by_cand[(i, c)]
                    right = sum(same_number(answer_of(e), gold[i]) for e in ends)
                    rows.append({"index": i, "question": question[i], "answer": gold[i],
                                 "steps": list(spines[i]), "candidate": c, "depth": depth,
                                 "is_answer_line": False, "rollouts": ends, "fraction": right / len(ends)})
                if mine and depth + 1 < args.depth:
                    spines[i].append(rng.choice(mine))
                    still.append(i)
            live = still
        for r in rows:
            r["label"] = 1 if r["fraction"] >= 0.5 else -1
        with open(ROWS, "a", encoding="utf-8") as f:
            for r in rows:
                f.write(json.dumps(r, ensure_ascii=False) + "\n")
        n_done = at + len(chunk)
        right = sum(r["label"] > 0 for r in rows)
        print(f"{n_done}/{len(todo)} problems, {len(rows)} candidates this chunk, {right} labelled right, "
              f"{trunk.tokens_out} tokens out, {time.perf_counter() - started:.0f}s", flush=True)
    (ROWS.parent / "rollouts.json").write_text(json.dumps({
        "set": "GSM8K train, data/gsm8k/train.jsonl", "n_problems": len(ps), "k": args.k, "m": args.m,
        "depth": args.depth, "rollout_depth": args.rollout_depth, "temperature": args.temperature,
        "max_step_tokens": args.max_step, "served_model": trunk.served_model(),
        "label": "fraction of m rollouts from the candidate reaching the gold number; +1 at 0.5 or more",
        "tokens_out": trunk.tokens_out, "seconds": round(time.perf_counter() - started, 1),
        "sha": sha(), "command": " ".join(sys.argv),
    }, indent=1), encoding="utf-8")


def features(args) -> None:
    from openplexus.trunk import LocalTrunk, states_resumable

    rows = [json.loads(line) for line in ROWS.read_text(encoding="utf-8").splitlines()]
    layers = [int(x) for x in args.layers.split(",")]
    indices = sorted({r["index"] for r in rows})
    rng = random.Random(args.seed)
    rng.shuffle(indices)
    n_held = n_test = max(1, len(indices) // 6)
    where = {i: "test" for i in indices[:n_test]}
    where.update({i: "held" for i in indices[n_test:n_test + n_held]})
    splits: dict[str, list[dict]] = {"fit": [], "held": [], "test": []}
    for r in rows:
        splits[where.get(r["index"], "fit")].append(r)
    for split, exs in splits.items():  # as many wrong as right, as the PRM800K features were drawn
        good = [x for x in exs if x["label"] > 0]
        bad = [x for x in exs if x["label"] < 0]
        rng.shuffle(good)
        rng.shuffle(bad)
        half = min(len(good), len(bad), args.n_max // 2)
        picked = good[:half] + bad[:half]
        rng.shuffle(picked)
        splits[split] = picked
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    trunk = LocalTrunk(args.model, batch=args.batch)
    started = time.perf_counter()
    for split, exs in splits.items():
        prompts = [judge_prompt(x["question"], x["steps"], x["candidate"], last=args.max_steps) for x in exs]
        hidden = states_resumable(trunk, prompts, layers, out / f"{split}.part", chunk=args.batch * 32)
        np.savez(out / f"{split}.npz", cand_label=np.array([x["label"] for x in exs], dtype=np.float32),
                 rating=np.array([1 if x["fraction"] > 0.5 else -1 if x["fraction"] < 0.5 else 0 for x in exs],
                                 dtype=np.int8),
                 fraction=np.array([x["fraction"] for x in exs], dtype=np.float32),
                 **{f"l{layer}": hidden[layer] for layer in layers})
        (out / f"{split}.json").write_text(json.dumps({"examples": exs}, ensure_ascii=False), encoding="utf-8")
        print(f"{split}: {len(exs)} candidates, {sum(x['label'] > 0 for x in exs)} right", flush=True)
    (out / "features.json").write_text(json.dumps({
        "model": trunk.served_model(), "layers": layers, "n_rows": len(rows), "n_problems": len(indices),
        "seed": args.seed, "prompt": "openplexus.steps.judge_prompt", "max_steps_shown": args.max_steps,
        "labels": "outcome: fraction of rollouts right, +1 at 0.5 or more, -1 below; drawn as many wrong as right",
        "rollouts": json.loads((ROWS.parent / "rollouts.json").read_text(encoding="utf-8")),
        "sizes": {k: len(v) for k, v in splits.items()}, "seconds": round(time.perf_counter() - started, 1),
        "sha": sha(), "command": " ".join(sys.argv),
    }, indent=1), encoding="utf-8")
    print("wrote", out / "features.json")


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="phase", required=True)
    r = sub.add_parser("rollouts")
    r.add_argument("--n", type=int, default=1500, help="training problems walked")
    r.add_argument("--k", type=int, default=3, help="sampled next steps a depth")
    r.add_argument("--m", type=int, default=3, help="rollouts a candidate")
    r.add_argument("--depth", type=int, default=6, help="steps of the walk")
    r.add_argument("--rollout-depth", type=int, default=10)
    r.add_argument("--temperature", type=float, default=0.7)
    r.add_argument("--max-step", type=int, default=48)
    r.add_argument("--chunk", type=int, default=16, help="problems walked in lockstep")
    r.add_argument("--parallel", type=int, default=8, help="requests in flight at the server")
    r.add_argument("--seed", type=int, default=0)
    r.set_defaults(run=rollouts)
    f = sub.add_parser("features")
    f.add_argument("--out", default="state/features/rollouts")
    f.add_argument("--layers", default="12,16,20")
    f.add_argument("--max-steps", type=int, default=6)
    f.add_argument("--n-max", type=int, default=8000, help="candidates a split at most")
    f.add_argument("--model", default="Qwen/Qwen3.5-2B")
    f.add_argument("--batch", type=int, default=6)
    f.add_argument("--seed", type=int, default=0)
    f.set_defaults(run=features)
    args = p.parse_args()
    args.run(args)


if __name__ == "__main__":
    main()
