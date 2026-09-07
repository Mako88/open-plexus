"""Phase 0's exit: a thread continued across a restart, taken as a reading.

WHAT IS BEING DEMONSTRATED, and what is not. Two SEPARATE PROCESSES. The first
is told some things and is then gone -- its memory freed, its Python
interpreter exited. The second loads the state file the first wrote and is asked
about what it was told, with NOTHING ABOUT THOSE FACTS IN ITS PROMPT. That is
complaint 4's mechanism working; it is not yet complaint 4's bar, which is
Phase 4's exit and needs the facts to survive into WEIGHTS rather than into a
state file.

WHY TWO PROCESSES AND NOT TWO `Thread` OBJECTS. A test that saves and reloads in
one process proves that `torch.save` round-trips, which nobody doubted. The
interesting claim is that a machine can be killed and resume, so the kill is
real: `subprocess`, a fresh interpreter, a cold model load.

THE SCORING IS CONTAINS-MATCH ON A SHORT CANONICAL ANSWER, which is the exam's
own rule, kept here so Phase 1 does not have to invent a second one. It is a
weak check and it is meant to be -- this reading says the MECHANISM works, and
whether the state holds anything usable at a delay is Phase 1's first reading
and could still refute the core choice.

  uv run python scripts/phase0_restart.py --size 1.5
"""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from datetime import UTC, datetime
from pathlib import Path

# Invented on purpose. A house made of real facts would measure what the
# checkpoint already knew, which is the one thing this is not asking about.
TOLD = [
    "My name is Osric and I keep eleven beehives behind the shed on Ferrin Lane.",
    "The roof was replaced last spring after a storm took half the tiles off.",
    "My sister Marta repairs clocks in the front room and hates the smell of beeswax.",
]

ASKED = [
    ("What is my name, and how many beehives do I keep?", ["osric", "eleven"]),
    ("What happened to the roof?", ["storm"]),
    ("What does Marta do?", ["clock"]),
]


def run(args: list[str]) -> str:
    proc = subprocess.run(
        [sys.executable, "-m", "sylvatica.loop.repl", *args],
        capture_output=True,
        text=True,
        check=True,
    )
    return proc.stdout


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=1.5)
    parser.add_argument("--thread", default="phase0-restart")
    parser.add_argument("--budget", type=int, default=80)
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core.thread import STATE_ROOT, Thread

    # A THREAD THAT ALREADY HAS A STATE WOULD MAKE THIS READING A LIE, so the
    # directory is cleared rather than `--fresh`ed: `--fresh` drops the state and
    # keeps the transcript, and a transcript left over from a previous run would
    # sit in the record as though this run had produced it.
    root = Path(STATE_ROOT) / args.thread
    if root.exists():
        shutil.rmtree(root)

    common = ["--thread", args.thread, "--size", str(args.size), "--budget", str(args.budget)]

    run([*common, "--fresh", *[a for t in TOLD for a in ("--say", t)]])

    thread = Thread(args.thread)
    state_bytes = thread.state_path.stat().st_size
    turns_after_first = sum(1 for _ in thread.turns())

    # THE FIRST PROCESS IS GONE. Everything below comes off the disk.
    answers_path = root / "answers.jsonl"
    run(
        [
            *common,
            "--answers",
            str(answers_path),
            *[a for q, _ in ASKED for a in ("--say", q)],
        ]
    )
    said = [json.loads(ln) for ln in answers_path.read_text(encoding="utf-8").splitlines() if ln]
    answers = [row["answer"] for row in said]

    results = []
    for (question, needles), answer in zip(ASKED, answers + [""] * len(ASKED)):
        lowered = answer.lower()
        results.append(
            {
                "question": question,
                "answer": answer,
                "wanted": needles,
                "correct": all(n in lowered for n in needles),
            }
        )

    reading = {
        "phase": 0,
        "kind": "restart",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": (
            "a thread continued across a process restart: told in one process, "
            "asked in another, with nothing about the facts in the second prompt"
        ),
        "size_b": args.size,
        "budget": args.budget,
        "thread": args.thread,
        "told": TOLD,
        "state_bytes_on_disk": state_bytes,
        "turns_after_first_process": turns_after_first,
        "results": results,
        "correct": sum(1 for r in results if r["correct"]),
        "asked": len(results),
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase0-restart-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    for r in results:
        print(f"{'OK ' if r['correct'] else 'NO '} {r['question']}\n     {r['answer']}")
    print(f"\n{reading['correct']}/{reading['asked']} across a restart; wrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
