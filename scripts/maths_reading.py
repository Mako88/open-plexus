"""Step 3, arithmetic as a tool, on GSM8K: the trunk writes a Python program and code runs it.

Three arms through llama-server. `direct` asks for the number alone, thinking off; `cot:<budget>`
lets the trunk think, the server cutting the thinking at the budget (launch it with
`--reasoning-budget <budget>`), then answer in up to `--max-answer` tokens: after a cut thought
the trunk writes its working again before the number, and 64 tokens cut 301 of 500 answers
mid-sentence in the first reading; `python` asks for a program whose last line prints
the answer, thinking off, and runs it in a fresh interpreter with a timeout. Passes a question
are the tokens generated plus one prefill.

    .venv/Scripts/python scripts/maths_reading.py --arms direct,python --n 500
    .venv/Scripts/python scripts/maths_reading.py --arms cot:2048 --n 500   # server --reasoning-budget 2048

Writes readings/maths-*.json.
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

from openplexus.maths import (DIRECT_SYSTEM, PYTHON_SYSTEM, THINK_SYSTEM, messages, number_in,  # noqa: E402
                              problems, program_in, run_program, same_number)


def arm(kind: str, budget: int | None, ps: list[dict], args) -> dict:
    from openplexus.trunk import ServedTrunk

    think = kind == "cot"
    max_new = {"direct": 16, "cot": (budget or 0) + args.max_answer, "python": args.max_program}[kind]
    trunk = ServedTrunk(url=args.url, max_new=max_new, parallel=args.parallel, think=think)
    started = time.perf_counter()
    replies: list[str] = []
    chunk = args.parallel * 4
    for at in range(0, len(ps), chunk):
        replies.extend(trunk.ask_many([messages(kind, p["question"]) for p in ps[at:at + chunk]]))
        print(f"{kind}{':' + str(budget) if budget else ''} {len(replies)}/{len(ps)}", flush=True)
    rows = []
    ran = failed = 0
    for p, reply in zip(ps, replies):
        if kind == "python":
            code = program_in(reply)
            said = run_program(code, timeout=args.timeout)
            ran += 1
            failed += said is None
            extra = {"program": code}
        else:
            said = number_in(reply)
            extra = {"reply": reply}
        rows.append({"question": p["question"], "answer": p["answer"], "said": said,
                     "correct": same_number(said, p["answer"]), **extra})
    n = len(rows)
    return {
        "kind": kind, "think": think, "budget": budget, "max_new": max_new,
        "max_answer": args.max_answer if think else None,
        "system_prompt": {"direct": DIRECT_SYSTEM, "cot": THINK_SYSTEM, "python": PYTHON_SYSTEM}[kind],
        "served_model": trunk.served_model(),
        "inputs_the_deployed_system_has": "all: the question",
        "score": round(sum(r["correct"] for r in rows) / n, 3), "n": n,
        "no_answer": sum(r["said"] is None for r in rows),
        "programs_failed": failed if kind == "python" else None,
        "passes_per_question": round((trunk.tokens_out + n) / n, 1),
        "tokens_in_per_question": round(trunk.tokens_in / n, 1),
        "seconds": round(time.perf_counter() - started, 1), "rows": rows,
    }


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--arms", default="direct,python")
    p.add_argument("--n", type=int, default=500, help="problems read, from the start of the test set")
    p.add_argument("--url", default=None)
    p.add_argument("--parallel", type=int, default=8)
    p.add_argument("--max-program", type=int, default=384, help="tokens for the python arm")
    p.add_argument("--max-answer", type=int, default=512, help="tokens for the answer after the thinking, cot arms")
    p.add_argument("--timeout", type=float, default=5.0, help="seconds a program may run")
    p.add_argument("--note", default="")
    args = p.parse_args()

    ps = problems(args.n)
    reading = {"kind": "maths", "step": 3, "set": "GSM8K test, data/gsm8k/test.jsonl", "n": len(ps),
               "scorer": "the final number, within 1e-6 of the gold", "arms": {}}
    for spec in args.arms.split(","):
        parts = spec.split(":")
        budget = int(parts[1]) if len(parts) > 1 else None
        a = arm(parts[0], budget, ps, args)
        reading["arms"][spec] = a
        print(f"== {spec}: {a['score']} in {a['seconds']}s, {a['passes_per_question']} passes a question, "
              f"{a['no_answer']} without an answer", flush=True)
    reading.update({
        "command": "scripts/maths_reading.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                              cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"maths-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
