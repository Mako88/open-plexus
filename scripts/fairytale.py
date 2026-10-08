"""Run arms through FairytaleQA's test tales and write one reading per arm under `readings/`.

    uv run python scripts/fairytale.py --arms window,graphed --note "..."
    uv run python scripts/fairytale.py --arms primed --primed 1000 --note "..."
    uv run python scripts/fairytale.py --arms reader --served Qwen3.5-2B-Q8_0 --port 8094 \
        --note "..."

One system hears every tale in turn, a break before each, and is never restarted between
them. Each question is asked once the sections it names are told, and nobody reacts: the
test split is untaught, as a benchmark's is. The arms:

- `window`: the told sentence that shares the most of the question's words, each weighted
  by how rare it is in the tale (MCTest's sliding window, Richardson et al. 2013). What a
  memory that matches words and understands nothing scores, under a scorer that rewards
  saying a whole sentence of the text.
- `graphed`: the system, starting empty.
- `primed`: the system after the first `--primed` stories of the TinyStories stream, heard
  and taught as `scripts/stories.py` does, so what it learnt there is read on another
  world (DECIDED, bAbI's milestone is transfer).
- `reader`: a served language model handed the tale so far and the question: what a frozen
  model reading the whole text scores (DECIDED, the models are a milestone check).
"""

from __future__ import annotations

import argparse
import json
import math
import os
import re
import shutil
import sys
import tempfile
import time
from collections import Counter, defaultdict
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.reading import utc_stamp  # noqa: E402
from unfused.exam.fairytales import fingerprint, rouge_l, tales  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
BREAK = "***"
READER = ("You answer questions about a story for a reading comprehension test. Answer in "
          "a short phrase or one short sentence, as a teacher's answer key would.")


def _words(text: str) -> list[str]:
    return re.findall(r"[a-z']+", text.lower())


class Window:
    name = "window"

    def __init__(self) -> None:
        self.told: list[str] = []

    def tell(self, text):
        self.told = [] if text == BREAK else self.told + [text]

    def ask(self, question):
        if not self.told:
            return "I don't know."
        counts = Counter(w for s in self.told for w in _words(s))
        asked = set(_words(question))
        return max(self.told, key=lambda s: sum(math.log(1 + 1 / counts[w])
                                                for w in set(_words(s)) & asked))

    def dials(self):
        return {}

    def close(self):
        pass


class Graphed:
    name = "graphed"

    def __init__(self, work: Path) -> None:
        from unfused.graph import GraphArm

        self.arm = GraphArm(work)

    def tell(self, text):
        self.arm.turn(self.arm_turn(), text)

    def arm_turn(self):
        self.t = getattr(self, "t", -1) + 1
        return self.t

    def ask(self, question):
        said = self.arm.turn(self.arm_turn(), question) or ""
        # untaught: the next telling is never read as a reaction to this answer
        self.arm.pending = None
        return said

    def dials(self):
        return self.arm.dials()

    def close(self):
        self.arm.close()


class Primed(Graphed):
    name = "primed"

    def __init__(self, work: Path, stories: int) -> None:
        sys.path.insert(0, str(ROOT / "scripts"))
        import stories as stream_runner
        from unfused.exam.stories import stream

        runner = stream_runner.Graphed(work)
        rows, sizes = [], {}
        self.t = stream_runner._stream(runner, stream(stories, 0), 100, rows, sizes) - 1
        self.arm = runner.arm
        self.primed = {"stories": stories, "questions": len(rows),
                       "right": sum(r["correct"] for r in rows)}

    def dials(self):
        return {**self.arm.dials(), "primed": self.primed}


class Reader:
    name = "reader"

    def __init__(self, faculty) -> None:
        self.faculty = faculty
        self.told: list[str] = []

    def tell(self, text):
        self.told = [] if text == BREAK else self.told + [text]

    def ask(self, question):
        story = " ".join(self.told)
        return self.faculty.chat(READER, f"Story:\n{story}\n\nQuestion: {question}", budget=48)

    def dials(self):
        return {}

    def close(self):
        pass


def run(arm, ts) -> dict:
    rows = []
    started, cpu = time.perf_counter(), time.process_time()
    for t in ts:
        arm.tell(BREAK)
        due = defaultdict(list)
        for q in t.questions:
            due[q.at].append(q)
        for p, sentence in enumerate(t.told, 1):
            arm.tell(sentence)
            for q in due.get(p, []):
                t0 = time.perf_counter()
                said = arm.ask(q.question)
                rows.append({"tale": t.name, "at": q.at, "question": q.question,
                             "answers": list(q.answers), "said": said,
                             "rouge_l": round(rouge_l(q.answers, said), 4),
                             "refused": "don't know" in (said or "").lower(),
                             "attribute": q.attribute, "explicit": q.explicit,
                             "local": q.local, "seconds": round(time.perf_counter() - t0, 3)})
        print(f"  {t.name}: {len(t.told)} sentences, {len(rows)} asked so far", flush=True)
    return {"rows": rows, "seconds": round(time.perf_counter() - started, 1),
            "cpu_seconds": round(time.process_time() - cpu, 1)}


def summary(rows) -> dict:
    def mean(rs):
        return {"rouge_l": round(sum(r["rouge_l"] for r in rs) / len(rs), 4) if rs else None,
                "refused": round(sum(r["refused"] for r in rs) / len(rs), 3) if rs else None,
                "n": len(rs)}

    by_attribute = defaultdict(list)
    for r in rows:
        by_attribute[r["attribute"]].append(r)
    return {**mean(rows),
            "explicit": mean([r for r in rows if r["explicit"]]),
            "implicit": mean([r for r in rows if not r["explicit"]]),
            "by_attribute": {k: mean(v) for k, v in sorted(by_attribute.items())}}


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--arms", default="window,graphed")
    p.add_argument("--split", default="test")
    p.add_argument("--tales", type=int, default=None, help="the first N tales, for a smoke run")
    p.add_argument("--primed", type=int, default=1000)
    p.add_argument("--served", default="Qwen3.5-2B-Q8_0")
    p.add_argument("--port", type=int, default=8094)
    p.add_argument("--note", default="")
    args = p.parse_args()

    ts = tales(args.split, args.tales)
    print(f"{len(ts)} tales, {sum(len(t.questions) for t in ts)} questions", flush=True)
    for name in args.arms.split(","):
        work = Path(tempfile.mkdtemp(prefix=f"unfused-fairytale-{name}-"))
        faculty = None
        if name == "window":
            arm = Window()
        elif name == "graphed":
            arm = Graphed(work)
        elif name == "primed":
            arm = Primed(work, args.primed)
        elif name == "reader":
            from unfused.faculty import ServedFaculty

            faculty = ServedFaculty(model_id=f"{args.served} (llama.cpp)",
                                    url=f"http://127.0.0.1:{args.port}/v1/chat/completions")
            arm = Reader(faculty)
        else:
            raise SystemExit(f"unknown arm {name}")
        out = run(arm, ts)
        dials = arm.dials()
        arm.close()
        shutil.rmtree(work, ignore_errors=True)
        taken = utc_stamp()
        reading = {"kind": "fairytaleqa", "arm": name, "taken_at": taken, "note": args.note,
                   "command": " ".join(sys.argv),
                   "faculty": faculty.name if faculty else None,
                   "world": {"source": f"FairytaleQA-{args.split}", "tales": len(ts),
                             "questions": len(out["rows"]), "fingerprint": fingerprint(ts)},
                   "summary": summary(out["rows"]), "dials": dials,
                   "seconds": out["seconds"], "cpu_seconds": out["cpu_seconds"],
                   "rows": out["rows"]}
        tag = args.served.split("-Q")[0] if name == "reader" else (
            f"n{args.primed}" if name == "primed" else "none")
        path = ROOT / "readings" / f"fairytale-{name}-{tag}-{taken}.json"
        path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        s = reading["summary"]
        attrs = "  ".join(f"{k}:{v['rouge_l']}" for k, v in s["by_attribute"].items())
        print(f"{name:8} rouge-L {s['rouge_l']}  explicit {s['explicit']['rouge_l']}  "
              f"implicit {s['implicit']['rouge_l']}  refused {s['refused']}  "
              f"{out['seconds']}s -> {path.name}\n  {attrs}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
