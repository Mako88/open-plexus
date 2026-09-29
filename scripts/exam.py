"""Run arms through a house and write one reading per arm under `readings/`.

    uv run python scripts/exam.py --arms blind,recall,recall2,full --seed 0
    uv run python scripts/exam.py --faculty served ...   # llama-server on :8093

Arms: `blind`, `full` (full context), `recall` (store, one hop), `recall2`
(store, two hops). The faculty loads once and serves every arm in the run.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import tempfile
from datetime import UTC, datetime
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.arms import SYSTEM, Blind, FullContext, Recall  # noqa: E402
from unfused.exam.run import run  # noqa: E402
from unfused.exam.world import generate_house  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--arms", default="blind,recall")
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--facts", type=int, default=50)
    p.add_argument("--turns", type=int, default=300)
    p.add_argument("--k", type=int, default=5)
    p.add_argument("--limit", type=int, default=None)
    p.add_argument("--faculty", default="qwen3-1.7b", choices=["qwen3-1.7b", "served"])
    p.add_argument("--note", default="", help="what this run is for and what would refute it")
    args = p.parse_args()

    house = generate_house(seed=args.seed, n_facts=args.facts, n_turns=args.turns)
    arms = args.arms.split(",")
    faculty = embedder = None
    if any(a != "blind" for a in arms):
        from unfused.faculty import Faculty, ServedFaculty
        faculty = ServedFaculty() if args.faculty == "served" else Faculty()
    if any(a.startswith("recall") for a in arms):
        from unfused.store import MiniLmEmbedder
        embedder = MiniLmEmbedder()

    for name in arms:
        work = Path(tempfile.mkdtemp(prefix=f"unfused-{name}-"))
        if name == "blind":
            def open_arm():
                return Blind(house)
        elif name == "full":
            def open_arm():
                return FullContext(work, faculty)
        elif name.startswith("recall"):
            hops = int(name[len("recall"):] or 1)

            def open_arm(hops=hops):
                return Recall(work, faculty, embedder, k=args.k, hops=hops)
        else:
            raise SystemExit(f"unknown arm {name}")

        if faculty is not None:
            faculty.cost.__init__()
        result = run(house, open_arm, limit=args.limit)
        shutil.rmtree(work, ignore_errors=True)

        taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
        reading = {
            "kind": "exam",
            "arm": name,
            "taken_at": taken,
            "note": args.note,
            "faculty": faculty.name if faculty else None,
            "system": SYSTEM,
            "house": {"seed": args.seed, "facts": args.facts, "turns": args.turns,
                      "questions": len(house.questions),
                      "answer_entropy": house.answer_entropy()},
            "limit": args.limit,
            "cost": faculty.cost.row() if faculty else None,
            **result,
        }
        out = ROOT / "readings" / f"exam-{name}-{args.faculty}-s{args.seed}-{taken}.json"
        out.parent.mkdir(exist_ok=True)
        out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        s = result["summary"]
        forms = {k: v["score"] for k, v in s["by_form"].items()}
        print(f"{name:10s} score {s['score']}  invented {s['invented']}  stale {s['stale']}"
              f"  {forms}  {result['seconds']}s  -> {out.name}", flush=True)


if __name__ == "__main__":
    main()
