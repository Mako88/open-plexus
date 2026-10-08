"""Run arms through ProPara's paragraphs, asking where each participant is after every
sentence, and write one reading per arm under `readings/`.

    uv run python scripts/propara.py --arms window,graphed,primed --note "..."

One system hears every paragraph in turn, a break before each, and is never restarted
between them; nobody reacts. An answer is right where it says the last word of the
annotators' place ('underground' for 'one mile underground'), or of one of its
alternatives, lower case and with a plural's 's' let go. The arms are `scripts/fairytale.py`'s:
`window` (the told sentence sharing the most rare words with the question: what the scorer
gives a memory that matches words), `graphed` (the system, empty) and `primed` (after the
first `--primed` stories of the TinyStories stream).
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
import tempfile
import time
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))

from fairytale import BREAK, Graphed, Primed, Window  # noqa: E402
from unfused.exam.propara import fingerprint, paragraphs  # noqa: E402
from unfused.reading import utc_stamp  # noqa: E402


def _word(w: str) -> str:
    return w[:-1] if w.endswith("s") and not w.endswith("ss") else w


def right(places, said: str) -> bool:
    if not said or "don't know" in said.lower():
        return False
    words = {_word(w) for w in re.findall(r"[a-z]+", said.lower())}
    return any(_word(re.findall(r"[a-z]+", p.lower())[-1]) in words
               for p in places if re.findall(r"[a-z]+", p.lower()))


def run(arm, ps) -> dict:
    rows = []
    started, cpu = time.perf_counter(), time.process_time()
    for p in ps:
        arm.tell(BREAK)
        for k, sentence in enumerate(p.told, 1):
            arm.tell(sentence)
            for q in (q for q in p.questions if q.at == k):
                said = arm.ask(q.question)
                rows.append({"paragraph": p.name, "at": k, "question": q.question,
                             "places": list(q.places), "said": said,
                             "right": right(q.places, said),
                             "refused": "don't know" in (said or "").lower(),
                             "moved": q.moved, "told": q.told})
        print(f"  {p.name}: {len(p.told)} sentences, {len(rows)} asked so far", flush=True)
    return {"rows": rows, "seconds": round(time.perf_counter() - started, 1),
            "cpu_seconds": round(time.process_time() - cpu, 1)}


def summary(rows) -> dict:
    def mean(rs):
        n = len(rs)
        return {"right": round(sum(r["right"] for r in rs) / n, 3) if n else None,
                "refused": round(sum(r["refused"] for r in rs) / n, 3) if n else None,
                "n": n}

    return {**mean(rows), "moved": mean([r for r in rows if r["moved"]]),
            "stayed": mean([r for r in rows if not r["moved"]]),
            "told": mean([r for r in rows if r["told"]]),
            "untold": mean([r for r in rows if not r["told"]])}


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--arms", default="window,graphed")
    p.add_argument("--split", default="test")
    p.add_argument("--paragraphs", type=int, default=None)
    p.add_argument("--primed", type=int, default=1000)
    p.add_argument("--note", default="")
    args = p.parse_args()

    ps = paragraphs(args.split, args.paragraphs)
    print(f"{len(ps)} paragraphs, {sum(len(x.questions) for x in ps)} questions", flush=True)
    for name in args.arms.split(","):
        work = Path(tempfile.mkdtemp(prefix=f"unfused-propara-{name}-"))
        arm = {"window": lambda: Window(), "graphed": lambda: Graphed(work),
               "primed": lambda: Primed(work, args.primed)}[name]()
        out = run(arm, ps)
        dials = arm.dials()
        arm.close()
        shutil.rmtree(work, ignore_errors=True)
        taken = utc_stamp()
        reading = {"kind": "propara", "arm": name, "taken_at": taken, "note": args.note,
                   "command": " ".join(sys.argv),
                   "world": {"source": f"ProPara-{args.split}", "paragraphs": len(ps),
                             "questions": len(out["rows"]), "fingerprint": fingerprint(ps)},
                   "summary": summary(out["rows"]), "dials": dials,
                   "seconds": out["seconds"], "cpu_seconds": out["cpu_seconds"],
                   "rows": out["rows"]}
        tag = f"n{args.primed}" if name == "primed" else "none"
        path = ROOT / "readings" / f"propara-{name}-{tag}-{taken}.json"
        path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        s = reading["summary"]
        print(f"{name:8} right {s['right']}  moved {s['moved']['right']}  stayed "
              f"{s['stayed']['right']}  told {s['told']['right']}  untold "
              f"{s['untold']['right']}  refused {s['refused']}  {out['seconds']}s "
              f"-> {path.name}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
