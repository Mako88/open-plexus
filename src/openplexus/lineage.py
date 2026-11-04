"""The system as one lineage (John, 2026-11-04): a single adapter that every training round
starts from and writes back to, and a ledger of every round, so what is learnt is kept across
runs and the readings become a trend of one system over time rather than snapshots of branches.
`state/system/adapter/` is the adapter as it stands, `state/system/adapter.gguf` the same for the
server, `state/system/ledger.json` the rounds in order: what each trained on, the commit, the
readings before and after, the price a point. A round is committed only after its regression
readings are taken, and the adapter before it is kept as `adapter.prev` so a round that loses
more than it gains is rolled back. Readings never write here; only training does.
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent.parent
SYSTEM = ROOT / "state" / "system"
LLAMA_CPP = Path(os.environ.get("LLAMA_CPP", r"D:\tools\llama.cpp-src"))
BASE = Path(os.environ.get("TRUNK_SNAPSHOT",
                           r"C:\Users\John\.cache\huggingface\hub\models--Qwen--Qwen3.5-2B\snapshots\15852e8c16360a2fea060d615a32b45270f8a8fc"))


def sha() -> str:
    return subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip()


def now() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


class Lineage:
    def __init__(self, path: Path = SYSTEM):
        self.path = Path(path)
        self.adapter = self.path / "adapter"
        self.gguf = self.path / "adapter.gguf"
        self.ledger_path = self.path / "ledger.json"

    def exists(self) -> bool:
        return self.adapter.exists() and self.ledger_path.exists()

    def ledger(self) -> list[dict]:
        return json.loads(self.ledger_path.read_text(encoding="utf-8")) if self.ledger_path.exists() else []

    def _write(self, entries: list[dict]) -> None:
        self.path.mkdir(parents=True, exist_ok=True)
        self.ledger_path.write_text(json.dumps(entries, indent=1, ensure_ascii=False), encoding="utf-8")

    def convert(self) -> Path:
        """The adapter as a GGUF LoRA for the server, written beside it."""
        env = {**os.environ, "PYTHONPATH": str(LLAMA_CPP / "gguf-py")}
        subprocess.run([sys.executable, str(LLAMA_CPP / "convert_lora_to_gguf.py"), "--base", str(BASE) + "/",
                        "--outfile", str(self.gguf), "--outtype", "f16", str(self.adapter)], check=True, env=env,
                       capture_output=True)
        return self.gguf

    def init(self, root: Path, what: str, note: str = "", gguf: Path | None = None, convert: bool = True) -> dict:
        """Start the lineage from an adapter already trained; `gguf` is its converted form where one exists."""
        if self.exists():
            raise RuntimeError(f"{self.path} already holds a lineage of {len(self.ledger())} rounds")
        self.path.mkdir(parents=True, exist_ok=True)
        shutil.copytree(root, self.adapter)
        if gguf is not None:
            shutil.copy(gguf, self.gguf)
        elif convert:
            self.convert()
        entry = {"round": 0, "what": what, "from": str(root), "note": note, "sha": sha(), "at": now()}
        self._write([entry])
        return entry

    def commit(self, adapter: Path, what: str, trained_on: str, questions: int, readings: dict,
               note: str = "", convert: bool = True) -> dict:
        """Make a trained adapter the system's, keeping the one before as adapter.prev. `readings`
        names the readings taken of it (and of the system before it) by purpose, e.g.
        {"before": "hotpot-...", "after": "hotpot-...", "asking": "hotpot-...", "maths": "grpo-..."},
        with the scores where known; the price a point is written when before and after scores are given."""
        entries = self.ledger()
        prev = self.path / "adapter.prev"
        if prev.exists():
            shutil.rmtree(prev)
        if self.adapter.exists():
            shutil.move(str(self.adapter), str(prev))
        shutil.copytree(adapter, self.adapter)
        if convert:
            self.convert()
        entry = {"round": len(entries), "what": what, "trained_on": trained_on, "questions": questions,
                 "from": str(adapter), "readings": readings, "note": note, "sha": sha(), "at": now()}
        before, after = readings.get("score_before"), readings.get("score_after")
        if before is not None and after is not None:
            gain = round(100 * (after - before), 1)
            entry["points_gained"] = gain
            entry["questions_per_point"] = round(questions / gain, 1) if gain > 0 else None
        entries.append(entry)
        self._write(entries)
        return entry

    def rollback(self, why: str, convert: bool = True) -> dict:
        """Put the adapter before the last round back, recording why; the round stays in the ledger as rolled back."""
        entries = self.ledger()
        prev = self.path / "adapter.prev"
        if not prev.exists() or not entries:
            raise RuntimeError("nothing to roll back to")
        shutil.rmtree(self.adapter)
        shutil.move(str(prev), str(self.adapter))
        if convert:
            self.convert()
        entries[-1]["rolled_back"] = {"why": why, "at": now()}
        self._write(entries)
        return entries[-1]

    def current(self) -> Path | None:
        return self.adapter if self.exists() else None

    def current_gguf(self) -> Path | None:
        return self.gguf if self.exists() and self.gguf.exists() else None
