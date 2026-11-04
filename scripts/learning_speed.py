"""Rung five's number: how many training questions a round needed for each point its reading
gained (John, 2026-11-04). A system that is learning to learn needs fewer questions a point
each round; a round that gained nothing has no price. Each row names the round, the reading of
the adapter it started from, the reading of the adapter it produced, and the questions it
trained on; the scores are the readings' exact match on the same questions.

    .venv/Scripts/python scripts/learning_speed.py

Writes readings/learning-speed-*.json and prints the table. Add a row for every round.
"""

from __future__ import annotations

import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

# round, what it trained, the reading before (its init adapter or the plain trunk), the reading after, questions trained on
ROUNDS = [
    ("asking round one", "the asking on the ten-paragraph store", "hotpot-20261101T033048Z", "hotpot-20261101T033732Z", 120),
    ("asking round two", "the asking on the ten-paragraph store", "hotpot-20261101T033732Z", "hotpot-20261102T034219Z", 240),
    ("asking round three", "the asking on the ten-paragraph store", "hotpot-20261102T034219Z", "hotpot-20261102T034225Z", 480),
    ("full-store round one", "the asking on every Wikipedia abstract by BM25", "hotpot-20261102T060233Z", "hotpot-20261102T194748Z", 480),
    ("answering round one", "the answering from the gold paragraphs", "hotpot-20261102T213113Z", "hotpot-20261103T061906Z", 480),
]


def score(reading: str) -> tuple[float, int, str]:
    r = json.loads((ROOT / "readings" / f"{reading}.json").read_text(encoding="utf-8"))
    arm = next(iter(r["arms"].values()))
    return arm["exact"], arm["n"], next(iter(r["arms"]))


def main() -> None:
    rows = []
    for name, what, before, after, questions in ROUNDS:
        b, nb, arm_b = score(before)
        a, na, arm_a = score(after)
        gain = round(100 * (a - b), 1)
        rows.append({"round": name, "trained": what, "before": before, "after": after, "arm": arm_a,
                     "score_before": b, "score_after": a, "n": min(na, nb), "questions": questions, "points_gained": gain,
                     "questions_per_point": round(questions / gain, 1) if gain > 0 else None})
    for r in rows:
        price = f"{r['questions_per_point']} questions a point" if r["questions_per_point"] else "no gain"
        print(f"{r['round']:22s} {r['score_before']:.3f} -> {r['score_after']:.3f} on {r['n']} ({r['points_gained']:+.1f} points) "
              f"with {r['questions']} questions: {price}", flush=True)
    reading = {"kind": "learning-speed", "measure": "training questions a round needed for each point of exact match its reading gained",
               "rounds": rows, "command": "scripts/learning_speed.py " + " ".join(sys.argv[1:]),
               "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
               "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")}
    out = ROOT / "readings" / f"learning-speed-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(f"wrote {out}", flush=True)


if __name__ == "__main__":
    main()
