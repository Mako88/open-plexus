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
import sys
import tempfile
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.reading import utc_stamp  # noqa: E402
from unfused.arms import FullContext  # noqa: E402
from unfused.exam.babi import TASKS, fingerprint, modal_answers, stories  # noqa: E402
from unfused.exam.run import converse, run, summarise  # noqa: E402

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
    p.add_argument("--teach", type=int, default=0,
                   help="training stories a task held as a conversation before the test")
    p.add_argument("--note", default="")
    args = p.parse_args()

    tasks = [int(t) for t in args.tasks.split(",")]
    worlds = {t: stories(t, args.stories) for t in tasks}
    # from the training stories: built from the test split, it read the key's marginals
    table = {k: v for t in tasks
             for k, v in modal_answers(stories(t, args.teach or 20, split="train")).items()}
    arms = args.arms.split(",")
    faculty = None
    if any(a not in ("blind", "graphed") for a in arms):
        from unfused.faculty import ServedFaculty
        faculty = ServedFaculty(model_id=f"{args.served} (llama.cpp)",
                                url=f"http://127.0.0.1:{args.port}/v1/chat/completions")
    for name in arms:
        rows, dials, seconds, known = [], None, 0.0, {}
        if name == "graphed" and args.teach:
            # taught on the training stories, with no faculty
            from unfused.graph import GraphArm

            for t in tasks:
                for world in stories(t, args.teach, split="train"):
                    work = Path(tempfile.mkdtemp(prefix="babi-graph-teach-"))
                    arm = GraphArm(work, known=known)
                    # taught in conversation, as on the house
                    converse(world, arm)
                    known = arm.export()
                    arm.close()
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
                elif name == "graphed":
                    from unfused.graph import GraphArm

                    def open_arm(work=work):
                        return GraphArm(work, known=known)
                else:
                    raise SystemExit(f"unknown arm {name}")
                result = run(world, open_arm, reopen_every=10**9)
                shutil.rmtree(work, ignore_errors=True)
                for r in result["rows"]:
                    r["task"] = t
                rows += result["rows"]
                dials, seconds = result["dials"], seconds + result["seconds"]
        summary = summarise(rows)
        by_task = {str(t): round(sum(r["correct"] for r in rows if r["task"] == t)
                                 / max(1, sum(r["task"] == t for r in rows)), 3) for t in tasks}
        taken = utc_stamp()
        reading = {
            "kind": "babi", "arm": name, "taken_at": taken, "note": args.note,
            # the command that took it, so a comparison copies it rather than rebuilds it
            "command": " ".join(sys.argv),
            "faculty": faculty.name if faculty and name != "blind" else None,
            "teach": args.teach, "learnt": len(known.get("learnt", [])),
            "world": {"tasks": {str(t): TASKS[t] for t in tasks}, "stories": args.stories,
                      "fingerprint": fingerprint([w for t in tasks for w in worlds[t]]),
                      "questions": len(rows)},
            "summary": {"score": summary["score"], "by_task": by_task},
            "dials": dials, "seconds": round(seconds, 1),
            "cost": faculty.cost.row() if faculty and name != "blind" else None,
            "rows": rows,
        }
        tag = "parse" if name == "graphed" else args.served.split("-Q")[0]
        out = ROOT / "readings" / f"babi-{name}-{tag}-{taken}.json"
        out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        print(f"{name:8s} score {summary['score']}  {by_task}  {round(seconds)}s -> {out.name}",
              flush=True)


if __name__ == "__main__":
    main()
