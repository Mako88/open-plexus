"""Run arms through bAbI stories and write one reading per arm under `readings/`.

    uv run python scripts/babi.py --tasks 1,2,3 --stories 20 --arms blind,full,planned \
        --served Qwen3.5-0.8B-Q8_0 --port 8094 --note "..."

Every story is a world of its own and each arm starts empty on it. The blind rule
answers a task's commonest answer. A reading counts only with --stories unset or equal
across the arms it is compared with.
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

from unfused.arms import FullContext  # noqa: E402
from unfused.exam.babi import TASKS, fingerprint, modal_answers, stories  # noqa: E402
from unfused.exam.run import run, summarise  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


class TaskBlind:
    name = "blind"

    def __init__(self, table: dict[str, str]) -> None:
        self.table = table

    def hear(self, turn: int, text: str) -> None:
        pass

    def answer(self, question) -> str:
        return self.table.get(question.kind, "")

    def dials(self) -> dict:
        return {}

    def close(self) -> None:
        pass


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--tasks", default="1,2,3")
    p.add_argument("--stories", type=int, default=None)
    p.add_argument("--arms", default="blind,full")
    p.add_argument("--served", default="Qwen3.5-0.8B-Q8_0")
    p.add_argument("--port", type=int, default=8094)
    p.add_argument("--planner-port", type=int, default=None)
    p.add_argument("--planner-served", default=None)
    p.add_argument("--planner-judges", action="store_true")
    p.add_argument("--context", type=int, default=0)
    p.add_argument("--moves", action="store_true")
    p.add_argument("--binds", action="store_true")
    p.add_argument("--cleans", action="store_true")
    p.add_argument("--teach", type=int, default=0,
                   help="training stories a task told with their answers before the test")
    p.add_argument("--note", default="")
    args = p.parse_args()

    tasks = [int(t) for t in args.tasks.split(",")]
    worlds = {t: stories(t, args.stories) for t in tasks}
    table = {k: v for t in tasks for k, v in modal_answers(worlds[t]).items()}
    arms = args.arms.split(",")
    faculty = embedder = ear = planner = None
    if any(a != "blind" for a in arms):
        from unfused.faculty import ServedFaculty
        faculty = ServedFaculty(model_id=f"{args.served} (llama.cpp)",
                                url=f"http://127.0.0.1:{args.port}/v1/chat/completions")
    if any(a in ("system", "planned", "asked", "taught") for a in arms):
        from unfused.ears import Ear
        from unfused.store import MiniLmEmbedder
        embedder = MiniLmEmbedder()
        ear = Ear(url=faculty.url, name=faculty.name)
        if args.planner_port:
            planner = Ear(url=f"http://127.0.0.1:{args.planner_port}/v1/chat/completions",
                          name=f"{args.planner_served} (llama.cpp, planner)")

    def system(work, name, plans, learnt, holds=()):
        from unfused.system import SystemArm
        return SystemArm(work, ear, embedder, plans=name == "planned", known_plans=plans,
                         planner=planner, judge=planner if args.planner_judges else None,
                         asked=name == "asked", context=args.context, moves=args.moves,
                         taught=name == "taught", known_learnt=learnt, binds=args.binds,
                         cleans=args.cleans, known_holds=holds)

    def export(work, table_sql):
        import sqlite3
        db = sqlite3.connect(str(work / "system.db"))
        out = db.execute(table_sql).fetchall()
        db.close()
        return out

    for name in arms:
        rows, dials, seconds, plans, learnt, holds = [], None, 0.0, {}, [], []
        if name == "taught" and args.teach:
            # the teaching: training stories heard a line at a time, each question told
            # with its answer; what was learnt, with how often it held, carries on
            for t in tasks:
                for world in stories(t, args.teach, split="train"):
                    work = Path(tempfile.mkdtemp(prefix="babi-teach-"))
                    arm = system(work, name, plans, learnt, holds)
                    asked = world.questions_after()
                    for turn, text in enumerate(world.turns):
                        arm.hear(turn, text)
                        for q in asked.get(turn, []):
                            arm.teach(q.text, q.answer)
                    arm.close()
                    learnt = export(work, "SELECT shape, plan, hits, misses FROM learnt")
                    holds = export(work, "SELECT relation, yes, no FROM holds")
                    shutil.rmtree(work, ignore_errors=True)
        for t in tasks:
            for world in worlds[t]:
                work = Path(tempfile.mkdtemp(prefix=f"babi-{name}-"))
                if name == "blind":
                    def open_arm():
                        return TaskBlind(table)
                elif name == "full":
                    def open_arm(work=work):
                        return FullContext(work, faculty)
                else:
                    def open_arm(work=work):
                        return system(work, name, plans, learnt, holds)
                result = run(world, open_arm, reopen_every=10**9)
                if name == "planned":
                    # plans carry from story to story, as they carry from house to house
                    import sqlite3
                    db = sqlite3.connect(str(work / "system.db"))
                    plans.update({k: json.loads(v) for k, v in
                                  db.execute("SELECT shape, plan FROM plans")})
                    db.close()
                shutil.rmtree(work, ignore_errors=True)
                for r in result["rows"]:
                    r["task"] = t
                rows += result["rows"]
                dials, seconds = result["dials"], seconds + result["seconds"]
        summary = summarise(rows)
        by_task = {str(t): round(sum(r["correct"] for r in rows if r["task"] == t)
                                 / max(1, sum(r["task"] == t for r in rows)), 3) for t in tasks}
        taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
        reading = {
            "kind": "babi", "arm": name, "taken_at": taken, "note": args.note,
            "faculty": faculty.name if faculty and name != "blind" else None,
            "planner": args.planner_served if args.planner_port else None,
            "planner_judges": args.planner_judges, "context": args.context,
            "moves": args.moves, "teach": args.teach, "binds": args.binds, "cleans": args.cleans, "learnt": len(learnt),
            "world": {"tasks": {str(t): TASKS[t] for t in tasks}, "stories": args.stories,
                      "fingerprint": fingerprint([w for t in tasks for w in worlds[t]]),
                      "questions": len(rows)},
            "summary": {"score": summary["score"], "by_task": by_task},
            "dials": dials, "seconds": round(seconds, 1),
            "cost": faculty.cost.row() if faculty and name != "blind" else None,
            "rows": rows,
        }
        tag = args.served.split("-Q")[0]
        out = ROOT / "readings" / f"babi-{name}-{tag}-{taken}.json"
        out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        print(f"{name:8s} score {summary['score']}  {by_task}  {round(seconds)}s -> {out.name}",
              flush=True)


if __name__ == "__main__":
    main()
