"""Re-judge a reading's rows with today's scorer and rewrite its summary.

    uv run python scripts/rescore.py readings/babi-full-*.json

The rows keep what each arm said, so a scorer corrected after a run is applied to it
without running it again. The reading records which scorer it was judged by.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

from unfused.exam.run import judge
from unfused.exam.world import Question

SCORER = "numbers matched whole and in either form"


def main() -> None:
    for name in sys.argv[1:]:
        path = Path(name)
        d = json.loads(path.read_text(encoding="utf-8"))
        for r in d["rows"]:
            q = Question(r["question"], r["answer"], r.get("kind", ""), r["form"], 0, 0)
            r.update(judge(q, r["said"]))
        tasks = sorted({r["task"] for r in d["rows"] if "task" in r})
        if tasks:
            d["summary"]["by_task"] = {str(t): round(
                sum(r["correct"] for r in d["rows"] if r["task"] == t)
                / max(1, sum(r["task"] == t for r in d["rows"])), 3) for t in tasks}
        pos = [r for r in d["rows"] if r["answer"] is not None]
        d["summary"]["score"] = round(sum(r["correct"] for r in pos) / len(pos), 3)
        d["scorer"] = SCORER
        path.write_text(json.dumps(d, indent=1), encoding="utf-8")
        print(path.name, d["summary"]["score"], d["summary"].get("by_task", ""))


if __name__ == "__main__":
    main()
