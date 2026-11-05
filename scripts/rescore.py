"""The headline lines rescored with the lenient match (`openplexus.hotpot.lenient`), the frontier's
included, so every comparison uses one scorer (John, 2026-11-05). Reads the rows each reading
already holds; nothing is re-run.

    .venv/Scripts/python scripts/rescore.py
"""

from __future__ import annotations

import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.hotpot import exact, f1, lenient  # noqa: E402

LINES = [
    ("the frontier, with the ten paragraphs", "frontier-hotpot-20261031T210036Z", None),
    ("the frontier, from memory", "frontier-hotpot-memory-20261101T003417Z", None),
    ("floor, the trunk given nothing", "hotpot-20261031T232647Z", "floor"),
    ("gold, the two supporting paragraphs", "hotpot-20261102T213113Z", "gold"),
    ("gold with thinking", "hotpot-20261102T215031Z", "gold"),
    ("the trained asker on the ten paragraphs", "hotpot-20261102T034219Z", "ask"),
    ("top-2 over Wikipedia by BM25", "hotpot-20261102T055343Z", "wiki:top:2"),
    ("the trained asker over Wikipedia by BM25", "hotpot-20261102T060233Z", "wiki:ask"),
]


def rows_of(reading: dict, arm: str | None) -> list[dict]:
    return reading["rows"] if arm is None else reading["arms"][arm]["rows"]


def main() -> None:
    out = []
    for name, f, arm in LINES:
        r = json.loads((ROOT / "readings" / f"{f}.json").read_text(encoding="utf-8"))
        rows = rows_of(r, arm)
        said = lambda x: x.get("said") if "said" in x else x.get("prediction")  # noqa: E731
        n = len(rows)
        line = {"line": name, "reading": f, "arm": arm, "n": n,
                "exact": round(sum(exact(said(x), x["answer"]) for x in rows) / n, 3),
                "lenient": round(sum(lenient(said(x), x["answer"]) for x in rows) / n, 3),
                "f1": round(sum(f1(said(x), x["answer"]) for x in rows) / n, 3)}
        out.append(line)
        print(f"{name:42s} n {n:3d}  exact {line['exact']:.3f}  lenient {line['lenient']:.3f}  F1 {line['f1']:.3f}", flush=True)
    reading = {"kind": "rescore", "scorer": "openplexus.hotpot.lenient beside exact match and token F1", "lines": out,
               "command": "scripts/rescore.py", "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
               "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip()}
    path = ROOT / "readings" / f"rescore-{reading['taken_at']}.json"
    path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(f"wrote {path}")


if __name__ == "__main__":
    main()
