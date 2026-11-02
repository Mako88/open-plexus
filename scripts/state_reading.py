"""Step 5's reading: does a conversation continue across a restart from saved state? For each of
`--n` conversations, a story (the paragraphs of `--turns` + 1 HotpotQA questions) is followed by
`--turns` question-and-answer turns on one server slot; the slot is saved; then the final
question is asked three ways and the three replies compared. The ceiling is the unbroken
conversation (no restart). The arm is the restart with the slot restored from its file. The floor
is the restart with nothing restored, where the server reads the whole prefix again. The reading
records each one's reply, the prompt tokens the server processed for it, its time, the saved file's
size and the restore's time, and whether the restored reply and its first token's log-probability
are the unbroken one's. The server is this script's own: it starts it with `--slot-save-path` and
restarts it between arms.

    .venv/Scripts/python scripts/state_reading.py --n 8 --turns 4
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
sys.path.insert(0, str(ROOT / "scripts"))

from loop import SERVER  # noqa: E402  the server's exe and model, as the loop launches them
from openplexus.hotpot import paragraph_text, questions  # noqa: E402
from openplexus.state import SlotState, same_state, turn  # noqa: E402

SYSTEM = ("You are given passages and then questions about them. Answer each question from the "
          "passages in a few words, nothing else.")
SLOTS = ROOT / "state" / "slots"


class Server:
    """llama-server as this reading's own process: one slot, a save path, restarted on request."""

    def __init__(self, ctx: int, log: Path):
        self.ctx, self.log, self.proc = ctx, log, None

    def start(self) -> float:
        SLOTS.mkdir(parents=True, exist_ok=True)
        args = [a for a in SERVER if a not in ("-np", "8", "-c", "32768")]
        args += ["-np", "1", "-c", str(self.ctx), "--slot-save-path", str(SLOTS) + "\\"]
        started = time.perf_counter()
        with open(self.log, "ab") as f:
            self.proc = subprocess.Popen(args, stdout=f, stderr=subprocess.STDOUT)
        while True:
            try:
                with urllib.request.urlopen("http://127.0.0.1:8093/health", timeout=3) as r:
                    if b"ok" in r.read():
                        return round(time.perf_counter() - started, 1)
            except Exception:  # noqa: BLE001
                if self.proc.poll() is not None:
                    raise RuntimeError(f"the server exited, see {self.log}")
                time.sleep(1)

    def stop(self) -> None:
        if self.proc and self.proc.poll() is None:
            self.proc.terminate()
            self.proc.wait(timeout=60)
        self.proc = None

    def restart(self) -> float:
        self.stop()
        return self.start()


def story_of(block: list[dict]) -> str:
    return "\n\n".join(paragraph_text(p) for q in block for p in q["paragraphs"])


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--n", type=int, default=8, help="conversations")
    p.add_argument("--turns", type=int, default=4, help="question turns before the save")
    p.add_argument("--skip", type=int, default=0)
    p.add_argument("--ctx", type=int, default=16384)
    p.add_argument("--max-new", type=int, default=24)
    p.add_argument("--note", default="")
    args = p.parse_args()

    per = args.turns + 1
    qs = questions(args.n * per, skip=args.skip)
    server = Server(args.ctx, ROOT / "state" / "state-reading-server.log")
    print(f"server up in {server.start()}s", flush=True)
    rows = []
    try:
        for c in range(args.n):
            block = qs[c * per:(c + 1) * per]
            state = SlotState(f"conversation-{c}.bin")
            try:
                state.erase()
            except Exception:  # noqa: BLE001
                pass
            messages = [{"role": "system", "content": SYSTEM}, {"role": "user", "content": story_of(block)}]
            history = []
            for q in block[:-1]:
                messages.append({"role": "user", "content": q["question"]})
                t = turn(messages, max_new=args.max_new)
                messages.append({"role": "assistant", "content": t["content"]})
                history.append({"question": q["question"], "answer": q["answer"], "said": t["content"], "prompt_n": t["prompt_n"]})
            t0 = time.perf_counter()
            saved = state.save()
            save_s = round(time.perf_counter() - t0, 2)
            size = (SLOTS / state.name).stat().st_size
            final = messages + [{"role": "user", "content": block[-1]["question"]}]
            unbroken = turn(final, max_new=args.max_new)

            restart_s = server.restart()
            t0 = time.perf_counter()
            restored_n = state.restore()
            restore_s = round(time.perf_counter() - t0, 2)
            restored = turn(final, max_new=args.max_new)

            server.restart()
            cold = turn(final, max_new=args.max_new)

            row = {"conversation": c, "ids": [q["id"] for q in block], "turns": history,
                   "final": {"question": block[-1]["question"], "answer": block[-1]["answer"]},
                   "saved_tokens": saved, "restored_tokens": restored_n, "file_bytes": size,
                   "save_s": save_s, "restore_s": restore_s, "server_restart_s": restart_s,
                   "unbroken": unbroken, "restored": restored, "cold": cold,
                   "restored_vs_unbroken": same_state(unbroken, restored), "cold_vs_unbroken": same_state(unbroken, cold)}
            rows.append(row)
            print(f"{c}: saved {saved} tokens, {size / 1e6:.0f} MB, restore {restore_s}s; prompt tokens unbroken {unbroken['prompt_n']} "
                  f"restored {restored['prompt_n']} cold {cold['prompt_n']}; restored same reply {row['restored_vs_unbroken']['same_reply']} "
                  f"gap {row['restored_vs_unbroken']['logprob_gap']}; cold same {row['cold_vs_unbroken']['same_reply']}", flush=True)
    finally:
        server.stop()

    def mean(key, arm=None):
        vals = [(r[arm][key] if arm else r[key]) for r in rows]
        return round(sum(vals) / len(vals), 2)

    summary = {
        "n": len(rows),
        "restored_same_reply": round(sum(r["restored_vs_unbroken"]["same_reply"] for r in rows) / len(rows), 3),
        "cold_same_reply": round(sum(r["cold_vs_unbroken"]["same_reply"] for r in rows) / len(rows), 3),
        "restored_logprob_gap_mean": round(sum(r["restored_vs_unbroken"]["logprob_gap"] or 0 for r in rows) / len(rows), 4),
        "cold_logprob_gap_mean": round(sum(r["cold_vs_unbroken"]["logprob_gap"] or 0 for r in rows) / len(rows), 4),
        "saved_tokens_mean": mean("saved_tokens"), "file_mb_mean": round(mean("file_bytes") / 1e6, 1),
        "bytes_per_token": round(sum(r["file_bytes"] for r in rows) / max(1, sum(r["saved_tokens"] for r in rows))),
        "save_s_mean": mean("save_s"), "restore_s_mean": mean("restore_s"), "server_restart_s_mean": mean("server_restart_s"),
        "prompt_n_mean": {arm: mean("prompt_n", arm) for arm in ("unbroken", "restored", "cold")},
        "prompt_ms_mean": {arm: mean("prompt_ms", arm) for arm in ("unbroken", "restored", "cold")},
        "seconds_mean": {arm: mean("seconds", arm) for arm in ("unbroken", "restored", "cold")},
    }
    print("==", json.dumps(summary), flush=True)
    reading = {
        "kind": "state-reading", "set": "HotpotQA distractor validation paragraphs as the story, its questions as the turns",
        "system_prompt": SYSTEM, "thinking": False, "ctx": args.ctx, "turns": args.turns, "summary": summary, "rows": rows,
        "command": "scripts/state_reading.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True, cwd=ROOT).stdout.strip(),
        "model": SERVER[2], "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    }
    out = ROOT / "readings" / f"state-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {out}", flush=True)


if __name__ == "__main__":
    main()
