"""Run arms through a house and write one reading per arm under `readings/`.

    uv run python scripts/exam.py --arms blind,recall,recall2,full --seed 0
    uv run python scripts/exam.py --faculty served ...   # llama-server on :8093

Arms: `blind`, `full` (full context), `recall` (store, one hop), `recall2`
(store, two hops), `linked` (symbols and spreading), and `system` (the faculty
only reads and asks under a schema; the system matches and chains). The faculty loads once and serves every arm in the run.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
import tempfile
from datetime import UTC, datetime
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.arms import SYSTEM, Blind, FullContext, Recall  # noqa: E402
from unfused.exam.run import converse, run  # noqa: E402
from unfused.exam.second import generate_second_house  # noqa: E402
from unfused.exam.world import UNTAUGHT, generate_house  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
# practice houses are seeded from here, far from any seed a reading tests
PRACTICE = 1000


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--arms", default="blind,recall")
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--facts", type=int, default=50)
    p.add_argument("--turns", type=int, default=300)
    p.add_argument("--k", type=int, default=5)
    p.add_argument("--limit", type=int, default=None)
    p.add_argument("--faculty", default="qwen3-1.7b", choices=["qwen3-1.7b", "served"])
    p.add_argument("--served", default="Qwen3.5-9B-Q6_K_L",
                   help="which model the llama-server is running, for the reading")
    p.add_argument("--port", type=int, default=8093)
    p.add_argument("--note", default="", help="what this run is for and what would refute it")
    p.add_argument("--world", default="first", choices=["first", "second"],
                   help="which house: the second shares no relation with the first, and is "
                        "taught from practice houses of its own")
    p.add_argument("--teach", type=int, default=0,
                   help="practice houses, other seeds than any tested, held as a "
                        "conversation before the test: `graphed` learns plans from them")
    p.add_argument("--carry", default="learnt,positions,aliases",
                   help="which of the tables `graphed` learnt in practice reach the test "
                        "house: without aliases, every synonym is one no lesson used")
    args = p.parse_args()

    generate = generate_second_house if args.world == "second" else generate_house
    house = generate(seed=args.seed, n_facts=args.facts, n_turns=args.turns)
    arms = args.arms.split(",")
    faculty = embedder = None
    if any(a not in ("blind", "graphed") for a in arms):
        from unfused.faculty import Faculty, ServedFaculty
        faculty = (ServedFaculty(model_id=f"{args.served} (llama.cpp)",
                                 url=f"http://127.0.0.1:{args.port}/v1/chat/completions")
                   if args.faculty == "served" else Faculty())
    if any(a.startswith("recall") or a == "linked" for a in arms):
        from unfused.store import MiniLmEmbedder
        embedder = MiniLmEmbedder()

    for name in arms:
        work = Path(tempfile.mkdtemp(prefix=f"unfused-{name}-"))
        if name == "blind":
            n = max(args.teach, 5)
            practice = [generate(seed=s, n_facts=args.facts, n_turns=args.turns)
                        for s in range(PRACTICE, PRACTICE + n)]

            def open_arm(practice=practice, n=n):
                return Blind(practice, f"practice {PRACTICE}-{PRACTICE + n - 1}")
        elif name == "full":
            def open_arm():
                return FullContext(work, faculty)
        elif name.startswith("recall"):
            hops = int(name[len("recall"):] or 1)

            def open_arm(hops=hops):
                return Recall(work, faculty, embedder, k=args.k, hops=hops)
        elif name == "graphed":
            from unfused.graph import GraphArm

            # taught on practice houses, with no faculty
            known = {}
            for s in range(PRACTICE, PRACTICE + args.teach):
                practice = generate(seed=s, n_facts=args.facts, n_turns=args.turns)
                taught = Path(tempfile.mkdtemp(prefix="unfused-graph-teach-"))
                arm = GraphArm(taught, known=known)
                # taught in conversation: nothing labels a turn, and a lesson is the
                # teacher's reaction to what the arm answered
                converse(practice, arm, UNTAUGHT)
                known = arm.export()
                arm.close()
                shutil.rmtree(taught, ignore_errors=True)

            carried = args.carry.split(",")
            known = {table: rows for table, rows in known.items() if table in carried}

            def open_arm(known=known):
                return GraphArm(work, known=known)
        elif name == "linked":
            from unfused.linked import LinkedRecall

            def open_arm():
                return LinkedRecall(work, faculty, embedder, k=args.k)
        else:
            raise SystemExit(f"unknown arm {name}")

        if faculty is not None:
            faculty.cost.__init__()
        result = run(house, open_arm, limit=args.limit)
        shutil.rmtree(work, ignore_errors=True)

        taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
        reading = {
            **result,
            "kind": "exam",
            # the command that took it, so a comparison copies it rather than rebuilds it
            "command": " ".join(sys.argv),
            "arm": name,
            "arm_class": result["arm"],
            "taken_at": taken,
            "note": args.note,
            "faculty": faculty.name if faculty else None,
            "system": SYSTEM,
            "house": {"world": args.world, "seed": args.seed, "facts": args.facts, "turns": args.turns,
                      "fingerprint": house.fingerprint(),
                      "questions": len(house.questions),
                      "answer_entropy": house.answer_entropy()},
            "limit": args.limit,
            "teach": args.teach if name == "graphed" else 0,
            "carry": args.carry if name == "graphed" else None,
            "cost": faculty.cost.row() if faculty else None,
        }
        tag = (args.served.split("-Q")[0] if args.faculty == "served" else args.faculty) if faculty else "parse"
        world = "" if args.world == "first" else f"{args.world}-"
        out = ROOT / "readings" / f"exam-{world}{name}-{tag}-s{args.seed}-{taken}.json"
        out.parent.mkdir(exist_ok=True)
        out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        s = result["summary"]
        forms = {k: v["score"] for k, v in s["by_form"].items()}
        print(f"{name:10s} score {s['score']}  invented {s['invented']}  stale {s['stale']}  misled {s.get('misled')}"
              f"  {forms}  {result['seconds']}s  -> {out.name}", flush=True)


if __name__ == "__main__":
    main()
