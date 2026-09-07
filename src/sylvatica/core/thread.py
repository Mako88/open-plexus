"""`Thread`: a named state file, its transcript, and what a restart resumes.

COMPLAINT 2 IS WHAT THIS IS FOR. An LLM call today is event-driven: it re-sends
the whole context every turn because it has nowhere to put what it already
heard. A thread is that somewhere. The state is written after every turn, the
process may be killed at any moment, and the next start picks the state up and
carries on without the transcript being in the prompt.

THE TRANSCRIPT IS NOT THE MEMORY AND IS NOT FED BACK. It is on disk so a
reading can be reconstructed and a human can see what was said. Feeding it back
in on restart is precisely the workaround this branch exists to avoid, and a
session that quietly adds that has refuted nothing and measured nothing. Phase 2
gives the transcript a second life as `episode` fragments in the store, where it
is retrieved SELECTIVELY -- which is a different thing from being re-sent.

The default thread is `main`, and that is the one the REPL resumes.
"""

from __future__ import annotations

import json
import os
import time
from collections.abc import Iterator
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Any

STATE_ROOT = Path(os.environ.get("SYLVATICA_STATE", "state"))
DEFAULT_THREAD = "main"


@dataclass(frozen=True)
class Turn:
    """One thing said, by one side, at one point in a thread."""

    index: int
    role: str  # "user" | "core" | "system"
    text: str
    tokens: int
    at: float = field(default_factory=time.time)
    seconds: float = 0.0
    flops: float = 0.0

    def line(self) -> str:
        return json.dumps(asdict(self), ensure_ascii=False)


class Thread:
    """A conversation that survives the process.

    Two files under `STATE_ROOT/<name>/`: `state.pt` is the core's fixed-size
    memory, `transcript.jsonl` is the append-only record. The transcript is
    appended and flushed per turn rather than buffered, because the normal way
    this process ends is being killed.
    """

    def __init__(self, name: str = DEFAULT_THREAD, root: Path | None = None) -> None:
        self.name = name
        self.root = Path(root or STATE_ROOT) / name
        self.root.mkdir(parents=True, exist_ok=True)

    @property
    def state_path(self) -> Path:
        return self.root / "state.pt"

    @property
    def transcript_path(self) -> Path:
        return self.root / "transcript.jsonl"

    @property
    def exists(self) -> bool:
        """Whether there is a state to resume. A transcript alone is not one."""
        return self.state_path.exists()

    # -- the transcript ----------------------------------------------------

    def turns(self) -> Iterator[Turn]:
        if not self.transcript_path.exists():
            return
        with self.transcript_path.open("r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line:
                    yield Turn(**json.loads(line))

    @property
    def next_index(self) -> int:
        last = -1
        for turn in self.turns():
            last = turn.index
        return last + 1

    def append(self, turn: Turn) -> Turn:
        with self.transcript_path.open("a", encoding="utf-8") as f:
            f.write(turn.line() + "\n")
            f.flush()
            os.fsync(f.fileno())
        return turn

    # -- the state ---------------------------------------------------------

    def save(self, core: Any, state: Any | None) -> None:
        core.save_state(state, self.state_path)

    def load(self, core: Any) -> Any | None:
        """The saved state, or `None` -- which is a core that has been told nothing.

        A state saved by a DIFFERENT checkpoint raises rather than loading
        something shaped wrong. See `RwkvCore.load_state`.
        """
        if not self.state_path.exists():
            return None
        return core.load_state(self.state_path)
