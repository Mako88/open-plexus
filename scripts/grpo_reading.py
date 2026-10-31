"""The GRPO policy read on GSM8K test: the trunk's whole solution, greedy, with and without the
adapter the loop trained (`grpo.py`), the same system prompt as training. Passes a question: the
tokens of the solution plus one prefill. Arms are adapter paths, or `base` for the plain trunk.
An arm is compared with `--against` (other arms' tags, `base` by default, the first the one that
stops it) on the same questions, by the count of problems made right and made wrong, and stops
once that paired gap is `--clear` standard errors from zero after `--min-n` questions; the
unpaired `--reference` is the fallback where the other arm has no rows, and it over-reads a
prefix the control also found easy. Rows
are appended to state/ and a re-run skips the questions read.

    .venv/Scripts/python scripts/grpo_reading.py --arms base --n 500
    .venv/Scripts/python scripts/grpo_reading.py --arms state/grpo/round1/adapter --n 500 --reference <base>

Writes readings/grpo-*.json. Needs the card.
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

from grpo import SYSTEM, answer_in  # noqa: E402
from openplexus.maths import problems, same_number  # noqa: E402


def part_rows(tag: str) -> dict:
    part = ROOT / "state" / f"grpo-reading-{tag}.part.jsonl"
    done = {}
    if part.exists():
        for line in part.read_text(encoding="utf-8").splitlines():
            r = json.loads(line)
            done[r["question"]] = r
    return done


def paired(rows: dict, other: dict, ps: list[dict]) -> dict | None:
    """The arm against the other on the questions both answered: made right, made wrong, and
    the gap in standard errors (McNemar's), or None where they share no question."""
    both = [p["question"] for p in ps if p["question"] in rows and p["question"] in other]
    if not both:
        return None
    fixed = sum(rows[q]["correct"] and not other[q]["correct"] for q in both)
    broke = sum(other[q]["correct"] and not rows[q]["correct"] for q in both)
    se = max((fixed + broke) ** 0.5 / len(both), 1e-9)
    return {"n": len(both), "score": round(sum(rows[q]["correct"] for q in both) / len(both), 3),
            "other_score": round(sum(other[q]["correct"] for q in both) / len(both), 3),
            "made_right": fixed, "made_wrong": broke, "gap": round((fixed - broke) / len(both), 3),
            "standard_errors": round((fixed - broke) / len(both) / se, 1)}


def arm(spec: str, ps: list[dict], args) -> dict:
    from openplexus.trunk import LocalTrunk

    adapter = None if spec == "base" else spec
    tag = "base" if adapter is None else Path(adapter).parent.name
    part = ROOT / "state" / f"grpo-reading-{tag}.part.jsonl"
    done = part_rows(tag)
    others = {o: part_rows(o) for o in args.against.split(",") if o and o != tag}
    others = {o: rows for o, rows in others.items() if rows}
    other = next(iter(others.values()), {})
    trunk = LocalTrunk(args.model, batch=args.batch, max_new=args.max_completion, adapter=adapter)
    started = time.perf_counter()
    todo = [p for p in ps if p["question"] not in done]
    for at in range(0, len(todo), args.batch):
        chunk = todo[at:at + args.batch]
        before_out, before_in = trunk.tokens_out, trunk.tokens_in
        replies = trunk.ask_many([[{"role": "system", "content": SYSTEM}, {"role": "user", "content": p["question"]}]
                                  for p in chunk])
        tokens_out = trunk.tokens_out - before_out
        with open(part, "a", encoding="utf-8") as f:
            for p, reply in zip(chunk, replies):
                said = answer_in(reply)
                row = {"question": p["question"], "answer": p["answer"], "said": said,
                       "correct": same_number(said, p["answer"]), "reply": reply,
                       "passes": round(tokens_out / len(chunk) + 1, 1)}
                done[p["question"]] = row
                f.write(json.dumps(row, ensure_ascii=False) + "\n")
        score = sum(done[p["question"]]["correct"] for p in ps[:len(done)]) / len(done)
        print(f"{tag} {len(done)}/{len(ps)} running {score:.3f}", flush=True)
        if len(done) < args.min_n:
            continue
        pr = paired(done, other, ps) if other else None
        if pr is not None and pr["n"] >= args.min_n:
            if abs(pr["standard_errors"]) >= args.clear:
                print(f"{tag} stopped at {len(done)}: {pr['made_right']} made right and {pr['made_wrong']} made "
                      f"wrong against {next(iter(others))}, {pr['standard_errors']} standard errors", flush=True)
                break
        elif args.reference is not None:
            se = max((score * (1 - score) / len(done)) ** 0.5, 1e-9)
            if abs(score - args.reference) > args.clear * se:
                print(f"{tag} stopped at {len(done)}: {score:.3f} is {abs(score - args.reference) / se:.1f} "
                      f"standard errors from {args.reference}", flush=True)
                break
    trunk.close()
    rows = [done[p["question"]] for p in ps if p["question"] in done]
    n = len(rows)
    return {
        "adapter": adapter, "served_model": trunk.served_model(), "system_prompt": SYSTEM, "greedy": True,
        "max_completion": args.max_completion, "inputs_the_deployed_system_has": "all: the question",
        "score": round(sum(r["correct"] for r in rows) / n, 3), "n": n, "stopped_early": n < len(ps),
        "reference": args.reference, "paired": {o: paired(done, rows, ps) for o, rows in others.items()},
        "unanswered": sum(r["said"] is None for r in rows),
        "passes_per_question": round(sum(r["passes"] for r in rows) / n, 1),
        "seconds": round(time.perf_counter() - started, 1), "rows": rows,
    }


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--arms", default="base")
    p.add_argument("--n", type=int, default=500)
    p.add_argument("--model", default="Qwen/Qwen3.5-2B")
    p.add_argument("--batch", type=int, default=8)
    p.add_argument("--max-completion", type=int, default=400)
    p.add_argument("--against", default="base", help="arms compared with on the same questions, the first stopping")
    p.add_argument("--reference", type=float, default=None, help="unpaired fallback")
    p.add_argument("--min-n", type=int, default=100)
    p.add_argument("--clear", type=float, default=2.5)
    p.add_argument("--note", default="")
    args = p.parse_args()
    ps = problems(args.n)
    reading = {"kind": "grpo-reading", "set": "GSM8K test, data/gsm8k/test.jsonl", "n": len(ps),
               "scorer": "the answer line's number, else the last number, within 1e-6 of the gold", "arms": {}}
    for spec in args.arms.split(","):
        a = arm(spec, ps, args)
        reading["arms"][spec] = a
        print(f"== {spec}: {a['score']} on {a['n']} at {a['passes_per_question']} passes a question, "
              f"{a['unanswered']} unanswered, {a['seconds']}s", flush=True)
    reading.update({
        "command": "scripts/grpo_reading.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                              cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"grpo-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
