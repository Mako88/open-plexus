"""The system's sleep as its own habit: one cycle, run when the card is idle, paused when its own
step rate collapses, resumed from its checkpoint, ended by the verdict. Nothing in it names this
machine: downtime and contention come from `openplexus.downtime`, the server and the trunk from
the environment the other scripts already read.

A cycle: the system writes its rehearsal (its own right answers and asking trajectories) on the
server, sleeps on the card from its adapter (`consolidate.py --rehearse`, checkpointed), is served
and read against the system before it on the regression lines (gold, the asking) and the gain
lines (the two-hop floor), and `openplexus.verdict` keeps the round in the lineage or leaves it out.

    .venv/Scripts/python scripts/live.py --rows-from 200 --rows 200 --dry-run
    .venv/Scripts/python scripts/live.py --rows-from 200 --rows 200
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.downtime import Rate, idle  # noqa: E402
from openplexus.lineage import Lineage  # noqa: E402
from openplexus.verdict import verdict  # noqa: E402

PY = sys.executable
SERVER = os.environ.get("LLAMA_SERVER", r"D:\tools\llama.cpp-src\build\bin\Release\llama-server.exe")
GGUF = os.environ.get("TRUNK_GGUF", r"C:\Users\John\.cache\huggingface\hub\models--bartowski--Qwen_Qwen3.5-2B-GGUF\snapshots"
                      r"\7d26695454df6de5fbcce2e58681e62dae06ce43\Qwen_Qwen3.5-2B-Q8_0.gguf")
URL = os.environ.get("LOCAL_SERVER", "http://127.0.0.1:8093")


def log(msg: str) -> None:
    print(f"{time.strftime('%H:%M:%S')} {msg}", flush=True)


def wait_idle(need_gib: float) -> None:
    while not idle(need_gib):
        time.sleep(60)


def serve(loras: list[Path], logfile: Path) -> subprocess.Popen:
    args = [SERVER, "-m", GGUF, "--port", URL.rsplit(":", 1)[1], "--jinja", "--reasoning-budget", "0", "-ngl", "99",
            "-np", "8", "-c", "32768", "--lora-init-without-apply"]
    for p in loras:
        args += ["--lora", str(p)]
    proc = subprocess.Popen(args, stdout=open(logfile, "ab"), stderr=subprocess.STDOUT)
    for _ in range(120):
        try:
            with urllib.request.urlopen(URL + "/health", timeout=3) as r:
                if b"ok" in r.read():
                    return proc
        except Exception:  # noqa: BLE001
            time.sleep(1)
    proc.kill()
    raise RuntimeError("the server did not come up")


def run(cmd: list[str], logfile: Path) -> None:
    log(" ".join(cmd)[:160])
    with open(logfile, "ab") as f:
        subprocess.run(cmd, stdout=f, stderr=subprocess.STDOUT, check=True)


def train_supervised(cmd: list[str], heartbeat: Path, logfile: Path, need_gib: float) -> None:
    """Run a checkpointed training command; when its own rate collapses, stop it, wait for the card,
    and run it again, which resumes from the checkpoint."""
    while True:
        wait_idle(need_gib)
        log("training: " + " ".join(cmd)[:120])
        rate = Rate()
        with open(logfile, "ab") as f:
            proc = subprocess.Popen(cmd, stdout=f, stderr=subprocess.STDOUT)
        while proc.poll() is None:
            time.sleep(30)
            if heartbeat.exists():
                try:
                    hb = json.loads(heartbeat.read_text(encoding="utf-8"))
                    rate.beat(hb["t"], hb["steps"])
                except (json.JSONDecodeError, KeyError):
                    pass
            if rate.contended(time.time()):
                log("the card is contended: pausing until it is free, then resuming from the checkpoint")
                proc.kill()
                proc.wait()
                break
        else:
            if proc.returncode == 0:
                return
            raise RuntimeError(f"training failed with {proc.returncode}, see {logfile}")


def reading(arm: str, adapter: Path, against: str, note: str, logfile: Path, skip: int = 0) -> dict:
    """One reading of 200 questions from `skip`; the regression lines on the fixed first 200, the
    floor, the gain line, on the questions whose rows the sleep took in."""
    run([PY, "scripts/hotpot_reading.py", "--arms", arm, "--n", "200", "--skip", str(skip), "--min-n", "200",
         "--adapter", str(adapter), "--against", against, "--note", note], logfile)
    newest = max((ROOT / "readings").glob("hotpot-*.json"), key=lambda p: p.stat().st_mtime)
    r = json.loads(newest.read_text(encoding="utf-8"))
    a = r["arms"][arm]
    pr = a["paired"].get(against.split(",")[0], {})
    return {"reading": newest.stem, "score": a["exact"], "made_right": pr.get("made_right", 0), "made_wrong": pr.get("made_wrong", 0)}


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--rows-from", type=int, required=True, help="the store's rows heard since the last sleep: these validation questions' supporting paragraphs")
    p.add_argument("--rows", type=int, default=200)
    p.add_argument("--epochs", type=int, default=4)
    p.add_argument("--need-gib", type=float, default=9.0, help="free memory the card must have to sleep")
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()
    lin = Lineage()
    if not lin.exists():
        raise SystemExit("no lineage: scripts/lineage.py init first")
    rnd = len(lin.ledger())
    work = ROOT / "state" / "live" / f"round{rnd}"
    work.mkdir(parents=True, exist_ok=True)
    logfile = work / "live.log"
    system_tag = f"system-r{rnd - 1}"
    plan = [f"round {rnd}: rehearse, sleep on rows {args.rows_from}-{args.rows_from + args.rows}, read, decide"]
    if args.dry_run:
        print("\n".join(plan))
        return
    rehearse = [work / "answer.jsonl", work / "ask.jsonl"]
    wait_idle(4.0)
    srv = serve([lin.gguf], logfile)
    try:
        if not rehearse[0].exists():
            run([PY, "scripts/rehearsal.py", "--n", "400", "--skip", str(2000 + 800 * rnd), "--adapter", "system", "--out", str(rehearse[0])], logfile)
        if not rehearse[1].exists():
            run([PY, "scripts/rehearsal.py", "--kind", "ask", "--n", "800", "--skip", str(2400 + 800 * rnd), "--adapter", "system", "--out", str(rehearse[1])], logfile)
        before = {
            "gold": reading("gold", lin.gguf, "gold", "the system before the sleep, gold", logfile),
            "asking": reading("ask", lin.gguf, "ask", "the system before the sleep, the asking", logfile),
            "floor": reading("floor", lin.gguf, "floor", "the system before the sleep, the floor on the rows it will sleep on", logfile, skip=args.rows_from),
        }
    finally:
        srv.kill()
    sleep_dir = work / "sleep"
    cmd = [PY, "scripts/consolidate.py", "--n", str(args.rows), "--skip", str(args.rows_from), "--epochs", str(args.epochs),
           "--augment", "4", "--out", str(sleep_dir), "--note", f"the live loop's sleep, round {rnd}"]
    for r in rehearse:
        cmd += ["--rehearse", str(r)]
    # the augmentations are written on the server before the card is taken
    srv = serve([lin.gguf], logfile)
    try:
        run(cmd + ["--augment-only"], logfile)
    finally:
        srv.kill()
    train_supervised(cmd, sleep_dir / "heartbeat.json", logfile, args.need_gib)
    gguf = work / f"sleep-r{rnd}.gguf"  # its stem tags the readings, so one a round
    env = {**os.environ, "PYTHONPATH": str(Path(os.environ.get("LLAMA_CPP", r"D:\tools\llama.cpp-src")) / "gguf-py")}
    subprocess.run([PY, str(Path(os.environ.get("LLAMA_CPP", r"D:\tools\llama.cpp-src")) / "convert_lora_to_gguf.py"),
                    "--base", os.environ.get("TRUNK_SNAPSHOT", r"C:\Users\John\.cache\huggingface\hub\models--Qwen--Qwen3.5-2B\snapshots\15852e8c16360a2fea060d615a32b45270f8a8fc") + "/",
                    "--outfile", str(gguf), "--outtype", "f16", str(sleep_dir / "adapter")], check=True, env=env, capture_output=True)
    wait_idle(4.0)
    srv = serve([lin.gguf, gguf], logfile)
    try:
        after = {k: reading(arm, gguf, f"{arm}-{system_tag}", f"round {rnd} after the sleep, {k}", logfile,
                            skip=args.rows_from if k == "floor" else 0)
                 for k, arm in (("gold", "gold"), ("asking", "ask"), ("floor", "floor"))}
    finally:
        srv.kill()
    def pair(k):
        a = {json.loads(l)["id"]: json.loads(l)["correct"] for l in open(ROOT / "state" / f"hotpot-{'ask' if k == 'asking' else k}-{gguf.stem}.part.jsonl", encoding="utf-8")}
        b = {json.loads(l)["id"]: json.loads(l)["correct"] for l in open(ROOT / "state" / f"hotpot-{'ask' if k == 'asking' else k}-{system_tag}.part.jsonl", encoding="utf-8")}
        shared = [i for i in a if i in b]
        return sum(a[i] and not b[i] for i in shared), sum(b[i] and not a[i] for i in shared)
    v = verdict({"gold": pair("gold"), "asking": pair("asking")}, {"floor": pair("floor")})
    log(f"verdict: {v}")
    readings = {"before": before["floor"]["reading"], "after": after["floor"]["reading"],
                "score_before": before["floor"]["score"], "score_after": after["floor"]["score"],
                **{f"{k}_after": after[k] for k in after}, "verdict": v}
    lin.commit(sleep_dir / "adapter", f"the live loop's sleep, round {rnd}", f"validation questions {args.rows_from}-{args.rows_from + args.rows}'s supporting paragraphs",
               0, readings, note=v["why"])
    if not v["keep"]:
        lin.rollback(v["why"])
    log("cycle done")


if __name__ == "__main__":
    main()
