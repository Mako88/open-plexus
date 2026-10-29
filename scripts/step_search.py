"""Step 3 on maths: the search over solution steps with the step evaluator, read on GSM8K.

The trunk in PyTorch proposes k next steps for each live partial solution, by sampling; the
step evaluator (`step_evaluator.py`) scores each from the trunk's state over the judge prompt;
the search keeps the best `beam` paths by mean step score until the kept paths end in answer
lines (`steps.search_many`). Passes a question: tokens generated, plus one prefill a proposal
and one a judgement. Arms: `search:<k>:<beam>`; `search:1:1` is the control, the trunk's own
step-by-step with the evaluator choosing nothing, and `random:<k>` draws among k by chance in
the evaluator's place. Rows are appended to state/ as each chunk of questions finishes, and a
re-run with the same arm skips the questions already read. The deployed system has every input:
the question.

    .venv/Scripts/python scripts/step_search.py --arms search:1:1,search:4:1,search:4:2 --n 500

Writes readings/step-search-*.json. Needs the card to itself.
"""

from __future__ import annotations

import argparse
import json
import random
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.heads import LinearHead  # noqa: E402
from openplexus.maths import problems, same_number  # noqa: E402
from openplexus.steps import STEP_SYSTEM, judge_prompt, search_many, step_messages  # noqa: E402


def arm(spec: str, ps: list[dict], trunk, evaluator: LinearHead, args) -> dict:
    parts = spec.split(":")
    kind = parts[0]
    k = int(parts[1])
    beam = int(parts[2]) if len(parts) > 2 else 1
    rng = random.Random(args.seed)
    part = ROOT / "state" / f"step-search-{spec.replace(':', '-')}.part.jsonl"
    done = {}
    if part.exists():
        for line in part.read_text(encoding="utf-8").splitlines():
            r = json.loads(line)
            done[r["question"]] = r
    counted = {"tokens_out": 0, "prefills": 0}

    def propose_many(asks, kk):
        counted["prefills"] += len(asks)
        before = trunk.tokens_out
        out = trunk.propose([step_messages(p, s) for p, s in asks], kk, args.temperature if kk > 1 else 0.0,
                            args.max_step)
        counted["tokens_out"] += trunk.tokens_out - before
        return out

    def judge_many(asks):
        counted["prefills"] += len(asks)
        if kind == "random":
            return [rng.random() for _ in asks]
        x = trunk.hidden([judge_prompt(p, s, c, last=args.max_steps) for p, s, c in asks], [evaluator.layer])[evaluator.layer]
        return evaluator.query(x)[:, 0].tolist()

    started = time.perf_counter()
    todo = [p for p in ps if p["question"] not in done]
    for at in range(0, len(todo), args.parallel):
        chunk = todo[at:at + args.parallel]
        counted = {"tokens_out": 0, "prefills": 0}
        best = search_many([p["question"] for p in chunk], propose_many, judge_many, k=k, beam=beam,
                           depth=args.depth)
        passes = (counted["tokens_out"] + counted["prefills"]) / len(chunk)
        with open(part, "a", encoding="utf-8") as f:
            for p, b in zip(chunk, best):
                row = {"question": p["question"], "answer": p["answer"], "said": b.answer(),
                       "correct": same_number(b.answer(), p["answer"]), "steps": b.steps,
                       "scores": [round(s, 3) for s in b.scores], "finished": b.answer() is not None,
                       "passes": round(passes, 1)}
                done[p["question"]] = row
                f.write(json.dumps(row, ensure_ascii=False) + "\n")
        print(f"{spec} {min(at + args.parallel, len(todo))}/{len(todo)} "
              f"running {sum(done[p['question']]['correct'] for p in ps[:len(done)]) / len(done):.3f}", flush=True)
    rows = [done[p["question"]] for p in ps]
    n = len(rows)
    return {
        "kind": kind, "k": k, "beam": beam, "depth": args.depth, "temperature": args.temperature if k > 1 else 0.0,
        "max_step_tokens": args.max_step, "max_steps_shown": args.max_steps, "system_prompt": STEP_SYSTEM, "evaluator": str(args.evaluator),
        "evaluator_layer": evaluator.layer, "served_model": trunk.served_model(),
        "inputs_the_deployed_system_has": "all: the question",
        "score": round(sum(r["correct"] for r in rows) / n, 3), "n": n,
        "unfinished": sum(not r["finished"] for r in rows),
        "steps_per_question": round(sum(len(r["steps"]) for r in rows) / n, 1),
        "passes_per_question": round(sum(r["passes"] for r in rows) / n, 1),
        "seconds": round(time.perf_counter() - started, 1), "rows": rows,
    }


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--arms", default="search:1:1,search:4:1")
    p.add_argument("--n", type=int, default=500)
    p.add_argument("--evaluator", default="state/steps/evaluator.npz")
    p.add_argument("--model", default="Qwen/Qwen3.5-2B")
    p.add_argument("--parallel", type=int, default=8, help="questions searched in lockstep")
    p.add_argument("--depth", type=int, default=12)
    p.add_argument("--max-steps", type=int, default=6, help="steps so far the judge prompt shows, as the evaluator was trained")
    p.add_argument("--max-step", type=int, default=48, help="tokens a proposed step may take")
    p.add_argument("--temperature", type=float, default=0.7)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    args = p.parse_args()

    from openplexus.trunk import LocalTrunk

    ps = problems(args.n)
    evaluator = LinearHead.load(args.evaluator)
    trunk = LocalTrunk(args.model, batch=args.parallel)
    reading = {"kind": "step-search", "step": 3, "set": "GSM8K test, data/gsm8k/test.jsonl", "n": len(ps),
               "scorer": "the answer line's number, within 1e-6 of the gold", "arms": {}}
    for spec in args.arms.split(","):
        a = arm(spec, ps, trunk, evaluator, args)
        reading["arms"][spec] = a
        print(f"== {spec}: {a['score']} in {a['seconds']}s, {a['passes_per_question']} passes a question, "
              f"{a['unfinished']} unfinished", flush=True)
    reading.update({
        "command": "scripts/step_search.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                              cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"step-search-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
