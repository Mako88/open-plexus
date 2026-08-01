"""One way to write a result, and it records what produced it.

Every sweep here already emits JSON rows rather than prose, which is the half
this project got right. The half it did not: **a rows file does not say what
made it.** Not which commit, not which arguments, not when. Six sessions later a
number in `out/` is unattributable, and the way that failure ends is somebody
re-running the experiment to find out — which is the same waste as rebuilding a
module nobody knew existed.

So a result is `{"run": {...}, "rows": [...]}`. The run block carries the commit,
whether the tree was dirty when it ran, the full command line, and the wall
clock. **A dirty tree is recorded rather than refused**: a sweep run against
uncommitted work is often exactly what is wanted while iterating, and the thing
that matters is that the file says so.

## What this deliberately does not do

**It does not touch stdout.** A run's printed table is for a human reading a
terminal and the JSON is for anything else; making one derive from the other
would put formatting decisions inside the record. The rule this project already
has is that prose carries no numbers a machine needs, and this is the other side
of it.

**It does not define a row schema.** Sweeps measure different things and a schema
would be a place for a number to go missing when it does not fit. Rows are
whatever the sweep says they are.

## What this does NOT duplicate, and what was searched

Searched by capability — emit, json, results, record, provenance, run metadata —
across `experiments/`, `tools/` and `tests/`.

- **`tools/check_constants.py`** asks whether a pinned number says where it came
  from. This asks the same of a produced one.
- **`tools/check_experiments.py`** proves a sweep starts, and now also that it
  can be asked for structured output at all.
"""

from __future__ import annotations

import json
import pathlib
import subprocess
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]


def commit() -> tuple[str, bool]:
    """The current commit and whether the tree was dirty. `("unknown", True)` off git."""
    try:
        at = subprocess.run(["git", "rev-parse", "HEAD"], cwd=ROOT,
                            capture_output=True, text=True, timeout=10)
        dirty = subprocess.run(["git", "status", "--porcelain"], cwd=ROOT,
                               capture_output=True, text=True, timeout=10)
        if at.returncode:
            return "unknown", True
        return at.stdout.strip(), bool(dirty.stdout.strip())
    except (OSError, subprocess.SubprocessError):
        return "unknown", True


def emit(path, rows, started: float | None = None, **parameters) -> None:
    """Write `rows` to `path` with a run block saying what produced them.

    Args:
        path: Where to write. Parents are created; `None` writes nothing, so a
            caller can pass its `--json` argument straight through.
        rows: Whatever the sweep measured, as a list of dicts.
        started: `time.time()` from the run's start, to record its duration.
        parameters: The sweep's own settings — every axis and every pinned
            value it used. **The point of the file**: a row without the
            arguments that produced it cannot be compared with another.
    """
    if path is None:
        return
    path = pathlib.Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    at, dirty = commit()
    record = {
        "run": {
            "commit": at,
            "dirty": dirty,
            "command": " ".join(sys.argv),
            "when": time.strftime("%Y-%m-%dT%H:%M:%S"),
            "seconds": round(time.time() - started, 1) if started else None,
            "parameters": parameters,
        },
        "rows": list(rows),
    }
    path.write_text(json.dumps(record, indent=1), encoding="utf-8")
    print(f"{len(record['rows'])} rows -> {path}"
          + ("  (TREE WAS DIRTY)" if dirty else ""))
