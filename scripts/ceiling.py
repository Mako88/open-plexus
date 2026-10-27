"""Step 0: the smallest trunk that composes in context.

Gives a model the whole bAbI story and the question, thinking off, greedy, and scores exact
match on the answer word. Two arms: `ceiling` (the story is given) and `floor` (it is not),
and the floor is the wiring check: if it reads like the ceiling, the story never reached the
model.

    .venv/Scripts/python scripts/ceiling.py --model Qwen/Qwen3.5-0.8B --tasks 1,2,3 --n 200

Writes readings/ceiling-<model>-<timestamp>.json.
"""

from __future__ import annotations

import argparse
import json
import random
import re
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

SYSTEM = (
    "You are given a short story and a question about it. Answer using only the story. "
    "Reply with the answer word only, nothing else."
)

ROOT = Path(__file__).resolve().parent.parent


def rows(tasks: list[int], n: int, seed: int, split: str = "test") -> list[dict]:
    by_task: dict[int, list[dict]] = {t: [] for t in tasks}
    with open(ROOT / "data" / "babi" / f"babi_{split}.jsonl", encoding="utf-8") as f:
        for line in f:
            r = json.loads(line)
            if r["task"] in by_task:
                by_task[r["task"]].append(r)
    rng = random.Random(seed)
    out = []
    for t in tasks:
        rs = by_task[t]
        rng.shuffle(rs)
        out.extend(rs[:n])
    return out


def normalise(s: str) -> list[str]:
    return re.findall(r"[a-z0-9]+", s.lower())


def correct(gold: str, reply: str) -> bool:
    words = normalise(reply)
    golds = [g.strip().lower() for g in gold.split(",")]
    return bool(words) and all(any(g == w for w in words) for g in golds)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--model", default="Qwen/Qwen3.5-0.8B")
    p.add_argument("--tasks", default="1,2,3")
    p.add_argument("--n", type=int, default=200, help="questions per task")
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--arms", default="ceiling,floor")
    p.add_argument("--batch", type=int, default=16)
    p.add_argument("--dtype", default="float32", choices=["float32", "float16"])
    p.add_argument("--max-new", type=int, default=8)
    p.add_argument("--shots", type=int, default=0,
                   help="worked examples per task from the train split, as earlier turns")
    p.add_argument("--note", default="")
    args = p.parse_args()

    import torch
    from transformers import AutoModelForCausalLM, AutoTokenizer

    tasks = [int(t) for t in args.tasks.split(",")]
    data = rows(tasks, args.n, args.seed)
    dtype = getattr(torch, args.dtype)
    tok = AutoTokenizer.from_pretrained(args.model)
    tok.padding_side = "left"
    if tok.pad_token is None:
        tok.pad_token = tok.eos_token
    model = AutoModelForCausalLM.from_pretrained(args.model, dtype=dtype).to("cuda").eval()

    shots = rows(tasks, args.shots, args.seed + 1, split="train") if args.shots else []

    def turn(r: dict, with_story: bool) -> str:
        story = r["passage"].strip() if with_story else "(no story given)"
        return f"Story:\n{story}\n\nQuestion: {r['question']}"

    def prompt(r: dict, with_story: bool) -> str:
        messages = [{"role": "system", "content": SYSTEM}]
        for s in shots:
            messages.append({"role": "user", "content": turn(s, True)})
            messages.append({"role": "assistant", "content": s["answer"]})
        messages.append({"role": "user", "content": turn(r, with_story)})
        return tok.apply_chat_template(
            messages, tokenize=False, add_generation_prompt=True, enable_thinking=False,
        )

    reading: dict = {
        "kind": "ceiling",
        "model": args.model,
        "dtype": args.dtype,
        "thinking": False,
        "system": SYSTEM,
        "tasks": tasks,
        "n_per_task": args.n,
        "seed": args.seed,
        "max_new_tokens": args.max_new,
        "shots_per_task": args.shots,
        "greedy": True,
        "command": " ".join(sys.argv),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True,
                              text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
        "arms": {},
    }
    for arm in args.arms.split(","):
        with_story = arm == "ceiling"
        started = time.perf_counter()
        out_rows = []
        tokens_out = 0
        for i in range(0, len(data), args.batch):
            chunk = data[i:i + args.batch]
            batch = tok([prompt(r, with_story) for r in chunk], return_tensors="pt",
                        padding=True).to("cuda")
            with torch.no_grad():
                gen = model.generate(**batch, max_new_tokens=args.max_new, do_sample=False,
                                     temperature=None, top_p=None, top_k=None,
                                     pad_token_id=tok.pad_token_id)
            new = gen[:, batch["input_ids"].shape[1]:]
            tokens_out += int((new != tok.pad_token_id).sum())
            for r, ids in zip(chunk, new):
                reply = tok.decode(ids, skip_special_tokens=True).strip()
                out_rows.append({"task": r["task"], "question": r["question"],
                                 "answer": r["answer"], "said": reply,
                                 "correct": correct(r["answer"], reply)})
            done = i + len(chunk)
            print(f"{arm} {done}/{len(data)} "
                  f"acc={sum(x['correct'] for x in out_rows) / len(out_rows):.3f}", flush=True)
        seconds = time.perf_counter() - started
        by_task = {t: round(sum(x["correct"] for x in out_rows if x["task"] == t)
                            / max(1, sum(1 for x in out_rows if x["task"] == t)), 3)
                   for t in tasks}
        reading["arms"][arm] = {
            "score": round(sum(x["correct"] for x in out_rows) / len(out_rows), 3),
            "by_task": by_task,
            "seconds": round(seconds, 1),
            "tokens_out": tokens_out,
            "questions_per_second": round(len(out_rows) / seconds, 2),
            "vram_peak_gb": round(torch.cuda.max_memory_allocated() / 1e9, 2),
            "rows": out_rows,
        }
        print(f"== {arm}: {reading['arms'][arm]['score']} by task {by_task} "
              f"in {seconds:.0f}s", flush=True)

    name = args.model.split("/")[-1]
    path = ROOT / "readings" / f"ceiling-{name}-{args.shots}shot-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print("wrote", path)


if __name__ == "__main__":
    main()
