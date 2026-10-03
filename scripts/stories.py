"""Run arms through the TinyStories stream and write one reading per arm under `readings/`.

    uv run python scripts/stories.py --stories 300 --arms frequent,blind,graphed --note "..."

One system hears every story in turn and is never restarted between them; it is closed
and reopened every 100 stories, so what it knows must survive on disk. Two baselines:
`blind` says the commonest answer of the questions before it and never reads a story;
`frequent` says the commonest noun the story has told so far.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
import tempfile
import time
from collections import Counter, defaultdict
from datetime import UTC, datetime
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.exam.stories import bucket, fingerprint, right, stream  # noqa: E402
from unfused.exam.world import RIGHT, WRONG  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


class Blind:
    name = "blind"

    def __init__(self) -> None:
        self.answers: Counter = Counter()

    def tell(self, turn, text):
        pass

    def ask(self, turn, story):
        return self.answers.most_common(1)[0][0] if self.answers else "I don't know."

    def react(self, turn, story, said):
        self.answers[story.answer] += 1

    def dials(self):
        return {}

    def close(self):
        pass


class Frequent(Blind):
    name = "frequent"

    def ask(self, turn, story):
        counts = Counter(story.earlier)
        if not counts:
            return "I don't know."
        top = max(counts.values())
        # ties go to the latest named
        return next(w for w in reversed(story.earlier) if counts[w] == top)

    def react(self, turn, story, said):
        pass


class Graphed:
    name = "graphed"

    def __init__(self, work: Path) -> None:
        from unfused.graph import GraphArm

        self.open = lambda: GraphArm(work)
        self.arm = self.open()

    def reopen(self):
        self.arm.close()
        self.arm = self.open()

    def tell(self, turn, text):
        self.arm.turn(turn, text)

    def ask(self, turn, story):
        return self.arm.turn(turn, story.question) or ""

    def react(self, turn, story, said):
        ok = right(story.answers, said)
        self.arm.turn(turn, RIGHT if ok else WRONG.format(answer=story.answer))

    def dials(self):
        return self.arm.dials()

    def close(self):
        self.arm.close()


def run(arm, stories, reopen_every: int = 100) -> dict:
    rows, turn, sizes = [], 0, {}
    started = time.perf_counter()
    crashed = None
    try:
        turn = _stream(arm, stories, reopen_every, rows, sizes)
    except Exception:
        # the reading so far is kept and says where it stopped; the run still fails
        import traceback

        crashed = traceback.format_exc()
    out = _summary(arm, rows, sizes, turn, started)
    if crashed:
        out["crashed"] = crashed
    return out


def _stream(arm, stories, reopen_every, rows, sizes) -> int:
    turn = 0
    for s in stories:
        if s.index and s.index % reopen_every == 0 and hasattr(arm, "reopen"):
            arm.reopen()
        for text in s.told:
            arm.tell(turn, text)
            turn += 1
        if s.question is not None:
            t0 = time.perf_counter()
            said = arm.ask(turn, s)
            seconds = time.perf_counter() - t0
            arm.react(turn, s, said)
            rows.append({"story": s.index, "bucket": bucket(s.index), "question": s.question,
                         "answer": s.answer, "said": said, "correct": right(s.answers, said),
                         "refused": "don't know" in (said or "").lower(),
                         "seconds": round(seconds, 3),
                         "gave_up": "(gave up)" in getattr(getattr(arm, "arm", None),
                                                          "last_notes", [])})
        for text in s.after:
            arm.tell(turn, text)
            turn += 1
        if (b := bucket(s.index)) != bucket(s.index + 1):
            sizes[b] = arm.dials()
    return turn


def _summary(arm, rows, sizes, turn, started) -> dict:
    dials = arm.dials()
    arm.close()
    by = defaultdict(list)
    for r in rows:
        by[r["bucket"]].append(r)
    curve = {b: {"score": round(sum(r["correct"] for r in rs) / len(rs), 3), "n": len(rs),
                 "refused": round(sum(r["refused"] for r in rs) / len(rs), 3),
                 "seconds_per_question": round(sum(r["seconds"] for r in rs) / len(rs), 3),
                 "size": sizes.get(b)}
             for b, rs in by.items()}
    return {"arm": arm.name, "dials": dials, "turns": turn,
            "seconds": round(time.perf_counter() - started, 1),
            "score": round(sum(r["correct"] for r in rows) / len(rows), 3) if rows else None,
            "curve": curve, "rows": rows}


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--stories", type=int, default=300)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--arms", default="frequent,blind,graphed")
    p.add_argument("--note", default="")
    args = p.parse_args()

    stories = stream(args.stories, args.seed)
    asked = sum(s.question is not None for s in stories)
    print(f"{len(stories)} stories, {asked} questions", flush=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    for name in args.arms.split(","):
        work = Path(tempfile.mkdtemp(prefix=f"unfused-stories-{name}-"))
        arm = {"blind": Blind, "frequent": Frequent}[name]() if name != "graphed" \
            else Graphed(work)
        out = run(arm, stories)
        shutil.rmtree(work, ignore_errors=True)
        reading = {"kind": "stories", "taken_at": stamp, "note": args.note,
                   "stream": {"source": "TinyStories-valid", "seed": args.seed,
                              "stories": args.stories, "questions": asked,
                              "fingerprint": fingerprint(stories)},
                   **out}
        path = ROOT / "readings" / f"stories-{name}-s{args.seed}-n{args.stories}-{stamp}.json"
        path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        curve = "  ".join(f"{b}:{c['score']}" for b, c in out["curve"].items())
        print(f"{name:9} score {out['score']}  {curve}  {out['seconds']}s -> {path.name}",
              flush=True)
        if "crashed" in out:
            print(out["crashed"], flush=True)
            return 1


if __name__ == "__main__":
    sys.exit(main())
