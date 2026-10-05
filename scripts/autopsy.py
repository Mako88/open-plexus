"""Why each stream question went as it did, read off the graph at the moment it was asked.

    uv run python scripts/autopsy.py --stories 300 --note "..."

For every question, before it is answered: whether a plan was ever learnt for its exact
shape; whether any path joins one of its names to the right answer in the graph, and how
short; and where the right answer ranked in focus, on each of focus's factors alone and
on all three. The reading says whether the plans had anything to find, and which factor
sank the right answer when focus missed it.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
import tempfile
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")
sys.path.insert(0, str(Path(__file__).resolve().parent))

import stories as runner  # noqa: E402
from unfused.exam.stories import fingerprint, stream  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
# how far a path from the question's names to the answer is looked for
REACH = 4


def rank(scores: dict, gold: set, key) -> int | None:
    """The best place any right name takes when names are ordered by `key`, 1 being first."""
    order = sorted(scores, key=lambda n: (key(scores[n]), n), reverse=True)
    places = [i + 1 for i, n in enumerate(order) if n in gold]
    return min(places) if places else None


class Traced(runner.Graphed):
    def __init__(self, work: Path) -> None:
        super().__init__(work)
        self.traces: list[dict] = []

    def ask(self, turn, story):
        a = self.arm
        shape, fillers = a.shape(story.question)
        gold = {n[2:] for w in story.answers for n in a.holding(w)}
        # the shortest way from any name the question gives to any right name
        hops = None
        for f in fillers:
            for g in gold:
                for limit in range(1, REACH + 1):
                    if hops is not None and limit >= hops:
                        break
                    if a.paths(f"n:{f}", f"n:{g}", limit=limit, most=1):
                        hops = limit
                        break
        scores = a.focus(story.question) or {}
        self.traces.append({
            "shape_learnt": a.db.execute("SELECT 1 FROM learnt WHERE shape = ? LIMIT 1",
                                         (shape,)).fetchone() is not None,
            "fillers": fillers,
            "gold_heard": bool(gold),
            "hops": hops,
            "focus_size": len(scores),
            "rank": rank(scores, gold, lambda s: s[0] * s[1] * s[2]),
            "rank_recency": rank(scores, gold, lambda s: s[0]),
            "rank_fit": rank(scores, gold, lambda s: s[1]),
            "rank_mark": rank(scores, gold, lambda s: s[2]),
        })
        return super().ask(turn, story)


def summarise(rows: list[dict]) -> dict:
    """Each form apart, as the stream reads it."""
    return {form: _summarise([r for r in rows if r["form"] == form])
            for form in sorted({r["form"] for r in rows})}


def _summarise(rows: list[dict]) -> dict:
    out = {}
    for name, keep in (("all", lambda r: True), ("early", lambda r: r["story"] < 100),
                       ("late", lambda r: r["story"] >= 100)):
        rs = [r for r in rows if keep(r)]
        wrong = [r for r in rs if not r["correct"]]
        in_focus = [r for r in wrong if r["rank"] is not None]

        def share(xs, f):
            return round(sum(1 for x in xs if f(x)) / len(xs), 3) if xs else None

        out[name] = {
            "n": len(rs), "right": share(rs, lambda r: r["correct"]),
            "shape_learnt": share(rs, lambda r: r["shape_learnt"]),
            "answer_heard": share(rs, lambda r: r["gold_heard"]),
            "path_from_question": share(rs, lambda r: r["hops"] is not None),
            "hops": dict(sorted(Counter(r["hops"] for r in rs).items(), key=str)),
            "wrong": len(wrong),
            "wrong_answer_in_focus": share(wrong, lambda r: r["rank"] is not None),
            "wrong_answer_rank": dict(sorted(Counter(
                min(r["rank"], 6) for r in in_focus).items())),
            # of the misses with the answer in focus, which factor alone ranked it first
            "wrong_first_on": {
                f: share(in_focus, lambda r, f=f: r[f"rank_{f}"] == 1)
                for f in ("recency", "fit", "mark")},
            "by": dict(Counter(r["notes"][-1] for r in rs if r["notes"])),
        }
    return out


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--stories", type=int, default=300)
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--note", default="")
    args = p.parse_args()
    stories = stream(args.stories, args.seed)
    work = Path(tempfile.mkdtemp(prefix="unfused-autopsy-"))
    arm = Traced(work)
    out = runner.run(arm, stories)
    shutil.rmtree(work, ignore_errors=True)
    rows = [{**r, **t} for r, t in zip(out["rows"], arm.traces)]
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    reading = {"kind": "autopsy", "taken_at": stamp, "note": args.note,
               "stream": {"source": "TinyStories-valid", "seed": args.seed,
                          "stories": args.stories, "fingerprint": fingerprint(stories)},
               "score": out["score"], "summary": summarise(rows), "rows": rows}
    path = ROOT / "readings" / f"autopsy-s{args.seed}-n{args.stories}-{stamp}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(json.dumps(reading["summary"], indent=1))
    print(f"score {out['score']} -> {path.name}")
    return 1 if "crashed" in out else 0


if __name__ == "__main__":
    sys.exit(main())
