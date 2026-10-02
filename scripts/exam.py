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
from unfused.exam.run import run  # noqa: E402
from unfused.exam.second import generate_second_house  # noqa: E402
from unfused.exam.world import generate_house  # noqa: E402

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
    p.add_argument("--planner-port", type=int, default=None,
                   help="a second llama-server that writes question plans; the ear reads")
    p.add_argument("--planner-served", default=None, help="which model that server runs")
    p.add_argument("--planner-judges", action="store_true",
                   help="the planner also says whether two wordings mean the same")
    p.add_argument("--shares", type=float, default=0.8,
                   help="property overlap at which one wording's verdict answers for another")
    p.add_argument("--plans", default=None,
                   help="a JSON file of plans by shape: `planned` starts with them and adds "
                        "what it learns")
    p.add_argument("--moves", action="store_true")
    p.add_argument("--cleans", action="store_true")
    p.add_argument("--world", default="first", choices=["first", "second"],
                   help="which house: the second shares no relation with the first, and is "
                        "taught from practice houses of its own")
    p.add_argument("--teach", type=int, default=0,
                   help="practice houses, other seeds than any tested, told with their "
                        "answers before the test: `taught` learns plans from them")
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
    if any(a.startswith("recall") or a in ("linked", "system", "planned", "searched", "asked", "taught") for a in arms):
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
        elif name in ("system", "planned", "searched", "asked", "taught"):
            from unfused.ears import Ear
            from unfused.system import SystemArm

            ear = Ear(url=faculty.url, name=faculty.name)
            planner = (Ear(url=f"http://127.0.0.1:{args.planner_port}/v1/chat/completions",
                           name=f"{args.planner_served} (llama.cpp, planner)")
                       if args.planner_port else None)

            known = (json.loads(Path(args.plans).read_text(encoding="utf-8"))
                     if name in ("planned", "searched") and args.plans and Path(args.plans).exists() else {})

            def system(work, learnt, holds=(), echoes=(), frames=(), meant=(), plans=name in ("planned", "searched"), known=known,
                       searched=name == "searched", asked=name == "asked"):
                return SystemArm(work, ear, embedder, plans=plans, known_plans=known,
                                 planner=planner,
                                 judge=planner if args.planner_judges else None,
                                 searched=searched, asked=asked,
                                 shares=args.shares, moves=args.moves,
                                 taught=name == "taught", known_learnt=learnt,
                                 cleans=args.cleans, known_holds=holds,
                                 known_echoes=echoes, known_frames=frames,
                                 known_meant=meant)

            learnt, holds, echoes, frames, meant = [], [], [], [], []
            if name == "taught":
                # the teaching: practice houses heard a turn at a time, each question with
                # an answer told with it; a negative has no answer to chain to and is not
                # told. What was learnt, with how often it held, carries to the test house
                for s in range(PRACTICE, PRACTICE + args.teach):
                    practice = generate(seed=s, n_facts=args.facts, n_turns=args.turns)
                    taught = Path(tempfile.mkdtemp(prefix="unfused-teach-"))
                    arm = system(taught, learnt, holds, echoes, frames, meant)
                    asked_after = practice.questions_after()
                    for turn, text in enumerate(practice.turns):
                        arm.hear(turn, text)
                        for q in asked_after.get(turn, []):
                            if q.answer is not None:
                                arm.teach(q.text, q.answer)
                    arm.close()
                    import sqlite3

                    db = sqlite3.connect(str(taught / "system.db"))
                    learnt = db.execute("SELECT shape, plan, hits, misses FROM learnt").fetchall()
                    holds = db.execute("SELECT relation, yes, no FROM holds").fetchall()
                    echoes = db.execute("SELECT shape FROM echoes").fetchall()
                    frames = db.execute("SELECT phrase FROM frames").fetchall()
                    meant = db.execute("SELECT phrase, name, n FROM meant").fetchall()
                    db.close()
                    shutil.rmtree(taught, ignore_errors=True)

            def open_arm(learnt=learnt, holds=holds, echoes=echoes, frames=frames,
                         meant=meant):
                return system(work, learnt, holds, echoes, frames, meant)
        elif name == "graphed":
            from unfused.graph import GraphArm

            # taught as the taught arm is, on the same practice houses, with no faculty
            learnt, positions = [], []
            for s in range(PRACTICE, PRACTICE + args.teach):
                practice = generate(seed=s, n_facts=args.facts, n_turns=args.turns)
                taught = Path(tempfile.mkdtemp(prefix="unfused-graph-teach-"))
                arm = GraphArm(taught, known_learnt=learnt, known_positions=positions)
                asked_after = practice.questions_after()
                for turn, text in enumerate(practice.turns):
                    arm.hear(turn, text)
                    for q in asked_after.get(turn, []):
                        if q.answer is not None:
                            arm.teach(q.text, q.answer)
                learnt, positions = arm.export()
                arm.close()
                shutil.rmtree(taught, ignore_errors=True)

            def open_arm(learnt=learnt, positions=positions):
                return GraphArm(work, known_learnt=learnt, known_positions=positions)
        elif name == "linked":
            from unfused.linked import LinkedRecall

            def open_arm():
                return LinkedRecall(work, faculty, embedder, k=args.k)
        else:
            raise SystemExit(f"unknown arm {name}")

        if faculty is not None:
            faculty.cost.__init__()
        result = run(house, open_arm, limit=args.limit)
        if name in ("planned", "searched"):
            import sqlite3

            db = sqlite3.connect(str(work / "system.db"))
            learnt = {k: json.loads(v) for k, v in db.execute("SELECT shape, plan FROM plans")}
            db.close()
            result["dials"]["plans_known_at_start"] = len(known)
            result["dials"]["plans_from"] = args.plans
            if args.plans:
                Path(args.plans).write_text(json.dumps(learnt, indent=1), encoding="utf-8")
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
            "teach": args.teach if name == "taught" else 0,
            "cost": faculty.cost.row() if faculty else None,
        }
        tag = (args.served.split("-Q")[0] if args.faculty == "served" else args.faculty) if faculty else "parse"
        world = "" if args.world == "first" else f"{args.world}-"
        out = ROOT / "readings" / f"exam-{world}{name}-{tag}-s{args.seed}-{taken}.json"
        out.parent.mkdir(exist_ok=True)
        out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        s = result["summary"]
        forms = {k: v["score"] for k, v in s["by_form"].items()}
        print(f"{name:10s} score {s['score']}  invented {s['invented']}  stale {s['stale']}"
              f"  {forms}  {result['seconds']}s  -> {out.name}", flush=True)


if __name__ == "__main__":
    main()
