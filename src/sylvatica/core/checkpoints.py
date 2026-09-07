"""Where a checkpoint comes from, and which one.

The design doc names the `BlinkDL/rwkv7-g1` line and says to VERIFY WHAT IS
PUBLISHED before choosing, since releases move. `published()` below is that
verification: it asks the Hub what files exist rather than trusting the table in
this file. The table is a cache of one session's answer and is allowed to go
stale; the function is not.

SIZES. 0.4B for iteration speed, 1.5B for readings that count. The doc's rule is
that nothing above 3B is downloaded without a line in the commit saying why, so
`BIG` stops at 2.9B and asking for more is a deliberate act.
"""

from __future__ import annotations

import os
import re
from dataclasses import dataclass
from pathlib import Path

REPO = "BlinkDL/rwkv7-g1"

# Where checkpoints land. Gigabytes of somebody else's weights: gitignored, and
# overridable so a second box or a fleet node can point at one shared copy.
CHECKPOINT_DIR = Path(os.environ.get("SYLVATICA_CHECKPOINTS", "checkpoints"))


@dataclass(frozen=True)
class Checkpoint:
    """One published `.pth`, and the two numbers a cost row needs about it."""

    size: str  # "0.4b"
    filename: str
    params: int  # nominal, from the name; the real count comes off the loaded core

    @property
    def path(self) -> Path:
        return CHECKPOINT_DIR / self.filename


def parse_size(filename: str) -> float | None:
    """Billions of parameters from a `rwkv7-g1-1.5b-...` style filename."""
    m = re.search(r"-(\d+(?:\.\d+)?)b-", filename.lower())
    return float(m.group(1)) if m else None


def parse_date(filename: str) -> int:
    """The `20260831` in a checkpoint name, or 0 if it has none.

    TWO RELEASES OF THE SAME SIZE IS THE NORMAL CASE, not an edge one -- the
    line ships a new letter every few weeks and leaves the old files up. Without
    a tie-break, `pick` returns whichever sorts first alphabetically, which is
    the OLDER one, and a reading would silently be taken on a superseded
    checkpoint while the commit message named the size.
    """
    m = re.search(r"-(\d{8})-", filename)
    return int(m.group(1)) if m else 0


def published(repo: str = REPO) -> list[str]:
    """Every `.pth` the Hub currently lists for `repo`, smallest first.

    THIS IS THE CHECK THE DOC ASKS FOR. A hardcoded filename that 404s is a
    twenty-minute detour into somebody's release notes; this turns it into one
    call. Requires network.
    """
    from huggingface_hub import HfApi

    files = [f for f in HfApi().list_repo_files(repo) if f.endswith(".pth")]
    return sorted(files, key=lambda f: (parse_size(f) or 1e9, parse_date(f), f))


def pick(size_b: float, repo: str = REPO) -> str:
    """The NEWEST published filename closest to `size_b` billion parameters."""
    files = published(repo)
    if not files:
        raise RuntimeError(f"{repo} lists no .pth files")
    sized = [(f, parse_size(f)) for f in files if parse_size(f) is not None]
    if not sized:
        raise RuntimeError(f"{repo} lists .pth files but none name a size: {files}")
    return min(sized, key=lambda fs: (abs(fs[1] - size_b), -parse_date(fs[0])))[0]


def fetch(filename: str, repo: str = REPO) -> Path:
    """Download `filename` into `CHECKPOINT_DIR` if it is not already there.

    Refuses anything the doc's 3B rule covers. The refusal is here rather than
    in a reviewer's head because the failure it prevents -- a card with 11 GB
    swapping on a model that does not fit -- looks like slowness rather than
    like a mistake.
    """
    size = parse_size(filename)
    if size is not None and size > 3.0:
        raise ValueError(
            f"{filename} is {size}B. The doc caps unattended downloads at 3B: "
            "fetch it by hand and say why in the commit."
        )
    from huggingface_hub import hf_hub_download

    CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)
    local = hf_hub_download(repo_id=repo, filename=filename, local_dir=str(CHECKPOINT_DIR))
    return Path(local)


def ensure(size_b: float, repo: str = REPO) -> Path:
    """The local path to a checkpoint near `size_b`, fetching it if needed.

    Checks the local directory FIRST, so a box that already has the weights
    never touches the network -- which is what makes it safe to call this from
    the REPL's startup path.
    """
    if CHECKPOINT_DIR.exists():
        local = sorted(CHECKPOINT_DIR.glob("*.pth"))
        near = [(p, parse_size(p.name)) for p in local]
        near = [(p, s) for p, s in near if s is not None and abs(s - size_b) < 0.05]
        if near:
            return near[0][0]
    return fetch(pick(size_b, repo), repo)
