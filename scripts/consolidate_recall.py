"""Step 7's single-fact reading: is a consolidated fact answerable at all, before asking whether two
can be composed. HotpotQA's floor is two-hop, and a trunk that holds both facts in its weights may
still not compose them (the compositionality gap, Press et al. 2022), so the floor cannot tell a
consolidation that failed from a composition that failed. Here the questions are single facts of
the replayed rows: the question-and-answer lines the trunk itself wrote and that were replayed
(`trained`, recall of what was trained), and fresh ones the base trunk writes now from the same
rows with another seed (`fresh`, the fact asked another way). Each is asked with no passage, the
adapter on and off, scored by exact match and F1 against the line's own answer.

    .venv/Scripts/python scripts/consolidate_recall.py --out state/consolidate/first200-aug --adapter state/gguf/consolidate-first200-aug-lora.gguf
"""

from __future__ import annotations

import argparse
import json
import random
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from consolidate import ASK, rows_of  # noqa: E402
from openplexus.hotpot import READ_SYSTEM, answer_of, exact, f1, questions  # noqa: E402
from openplexus.trunk import ServedTrunk  # noqa: E402


def split_qa(text: str) -> tuple[str, str] | None:
    lines = [x.strip() for x in text.splitlines() if x.strip()]
    if len(lines) < 2 or not lines[0].startswith("Question:") or not lines[1].startswith("Answer:"):
        return None
    return lines[0][len("Question:"):].strip(), lines[1][len("Answer:"):].strip().rstrip(".")


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--out", default="state/consolidate/first200-aug")
    p.add_argument("--adapter", required=True, help="the consolidation adapter's GGUF, loaded by the server")
    p.add_argument("--n", type=int, default=200, help="questions of each kind")
    p.add_argument("--seed", type=int, default=9)
    p.add_argument("--note", default="")
    args = p.parse_args()
    rng = random.Random(args.seed)
    rows = rows_of(questions(200), "gold")

    trained = []
    for line in (Path(args.out) / "augmented.jsonl").read_text(encoding="utf-8").splitlines():
        r = json.loads(line)
        qa = split_qa(r["text"]) if r["kind"] == "ask" and r["text"] else None
        if qa:
            trained.append({"row": r["row"], "question": qa[0], "answer": qa[1]})
    rng.shuffle(trained)
    trained = trained[:args.n]

    base = ServedTrunk(max_new=320, parallel=8, temperature=0.8, seed=args.seed)
    picks = rng.sample(range(len(rows)), min(args.n, len(rows)))
    fresh = []
    for i, reply in zip(picks, base.ask_many([[{"role": "system", "content": ASK}, {"role": "user", "content": rows[i]}] for i in picks])):
        qa = split_qa(reply)
        if qa:
            fresh.append({"row": i, "question": qa[0], "answer": qa[1]})
    print(f"{len(trained)} trained questions, {len(fresh)} fresh", flush=True)

    loaded = base.adapters()
    name = Path(args.adapter).name
    arms = {"off": [{"id": a["id"], "scale": 0.0} for a in loaded],
            "on": [{"id": a["id"], "scale": 1.0 if Path(a["path"]).name == name else 0.0} for a in loaded]}
    if not any(x["scale"] for x in arms["on"]):
        raise SystemExit(f"the server has not loaded {name}: {loaded}")
    reading = {"kind": "consolidate-recall", "rows_replayed": len(rows), "adapter": args.adapter, "system_prompt": READ_SYSTEM,
               "sets": {}, "rows": {}}
    for kind, items in (("trained", trained), ("fresh", fresh)):
        reading["rows"][kind] = []
        said = {}
        for arm, lora in arms.items():
            trunk = ServedTrunk(max_new=160, parallel=8, lora=lora)
            replies = trunk.ask_many([[{"role": "system", "content": READ_SYSTEM}, {"role": "user", "content": x["question"]}] for x in items])
            said[arm] = [answer_of(r) for r in replies]
        for i, x in enumerate(items):
            reading["rows"][kind].append({**x, "said_off": said["off"][i], "said_on": said["on"][i],
                                          "exact_off": exact(said["off"][i], x["answer"]), "exact_on": exact(said["on"][i], x["answer"]),
                                          "f1_off": round(f1(said["off"][i], x["answer"]), 3), "f1_on": round(f1(said["on"][i], x["answer"]), 3)})
        rs = reading["rows"][kind]
        n = len(rs)
        made_right = sum(r["exact_on"] and not r["exact_off"] for r in rs)
        made_wrong = sum(r["exact_off"] and not r["exact_on"] for r in rs)
        reading["sets"][kind] = {
            "n": n, "exact_off": round(sum(r["exact_off"] for r in rs) / n, 3), "exact_on": round(sum(r["exact_on"] for r in rs) / n, 3),
            "f1_off": round(sum(r["f1_off"] for r in rs) / n, 3), "f1_on": round(sum(r["f1_on"] for r in rs) / n, 3),
            "made_right": made_right, "made_wrong": made_wrong,
            "standard_errors": round((made_right - made_wrong) / max(1, (made_right + made_wrong)) ** 0.5, 1),
        }
        print(f"== {kind}: exact off {reading['sets'][kind]['exact_off']} on {reading['sets'][kind]['exact_on']}, "
              f"F1 off {reading['sets'][kind]['f1_off']} on {reading['sets'][kind]['f1_on']}, made right {made_right} wrong {made_wrong}", flush=True)
    reading.update({
        "served_model": base.served_model(), "command": "scripts/consolidate_recall.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    })
    out = ROOT / "readings" / f"consolidate-recall-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {out}", flush=True)


if __name__ == "__main__":
    main()
