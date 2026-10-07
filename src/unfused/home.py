"""Where the caches that git does not hold live: `state/` and `data/`.

A worktree is a copy of the code and none of these, and a cache rebuilt there costs hours
of parsing. So they are found, not linked: `UNFUSED_HOME` where it is set, else the
checkout of the `unfused` branch, else the checkout this code is in."""

import os
import subprocess
from functools import cache
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


@cache
def home() -> Path:
    given = os.environ.get("UNFUSED_HOME")
    if given:
        return Path(given).resolve()
    try:
        out = subprocess.run(["git", "worktree", "list", "--porcelain"], cwd=ROOT,
                             capture_output=True, text=True, timeout=10).stdout
    except (OSError, subprocess.SubprocessError):
        return ROOT
    where = None
    for line in out.splitlines():
        if line.startswith("worktree "):
            where = line[len("worktree "):]
        elif line == "branch refs/heads/unfused" and where:
            return Path(where).resolve()
    return ROOT
