"""The system's day and sleep as its own habit: one cycle, run when the card has room, ended by the
verdict. Nothing in it names this machine: downtime comes from `openplexus.downtime`, the server
and the trunk from the environment the other scripts already read.

A cycle (store-first, 2026-11-07: the sleep writes rows, never weights, since every sleep into the
weights cost the asking and facts there compete with facts in the store):

1. The day: the system asks its way through questions it has not heard (validation questions
   1000 + 200 a round on, never the 200 reading questions) over the full store with its own rows
   beside it. That pass is also the trend line: if earlier rounds' rows carry over to new questions,
   it rises round on round.
2. The sleep: from the day's search histories the trunk writes a chain row for each pair of
   passages read one after the other (`chains.py --wiki`), each row carrying both passages.
3. The reading: the day's questions asked again with the new rows (the gain line, remembering what
   it worked out) and the fixed first 200 asked with them (the regression line, the rows not getting
   in the way), each paired against the system before the sleep.
4. `openplexus.verdict` keeps the rows as a round of the lineage or cuts them back out.

    .venv/Scripts/python scripts/live.py --dry-run
    .venv/Scripts/python scripts/live.py

The sleep into the weights (rehearsal, `consolidate.py`, a checkpointed training run supervised for
contention) is gone from here; revive from 6e528546.
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

from openplexus.downtime import idle  # noqa: E402
from openplexus.lineage import Lineage  # noqa: E402
from openplexus.verdict import verdict  # noqa: E402

PY = sys.executable
SERVER = os.environ.get("LLAMA_SERVER", r"D:\tools\llama.cpp-src\build\bin\Release\llama-server.exe")
GGUF = os.environ.get("TRUNK_GGUF", r"C:\Users\John\.cache\huggingface\hub\models--bartowski--Qwen_Qwen3.5-2B-GGUF\snapshots"
                      r"\7d26695454df6de5fbcce2e58681e62dae06ce43\Qwen_Qwen3.5-2B-Q8_0.gguf")
URL = os.environ.get("LOCAL_SERVER", "http://127.0.0.1:8093")
HEARD_FROM = 1000  # the day's questions start past every set the readings and earlier sleeps used


def log(msg: str) -> None:
    print(f"{time.strftime('%H:%M:%S')} {msg}", flush=True)


def wait_idle(need_gib: float) -> None:
    while not idle(need_gib):
        time.sleep(60)


def up() -> bool:
    try:
        with urllib.request.urlopen(URL + "/health", timeout=3) as r:
            return b"ok" in r.read()
    except Exception:  # noqa: BLE001
        return False


def serve(loras: list[Path], logfile: Path) -> subprocess.Popen | None:
    """The trunk served with the system's adapters; None when a server is already up at URL, which is then used as it is."""
    if up():
        return None
    args = [SERVER, "-m", GGUF, "--port", URL.rsplit(":", 1)[1], "--jinja", "--reasoning-budget", "0", "-ngl", "99",
            "-np", "8", "-c", "32768", "--lora-init-without-apply"]
    for p in loras:
        args += ["--lora", str(p)]
    proc = subprocess.Popen(args, stdout=open(logfile, "ab"), stderr=subprocess.STDOUT)
    for _ in range(120):
        if up():
            return proc
        time.sleep(1)
    proc.kill()
    raise RuntimeError("the server did not come up")


def run(cmd: list[str], logfile: Path) -> None:
    log(" ".join(cmd)[:200])
    with open(logfile, "ab") as f:
        subprocess.run(cmd, stdout=f, stderr=subprocess.STDOUT, check=True, cwd=ROOT)


def reading(skip: int, experience: str, against: str, note: str, logfile: Path, n: int = 200) -> dict:
    """The full-store asking on `n` questions from `skip`, the system's adapter on, with `experience`'s rows beside the store."""
    run([PY, "scripts/hotpot_reading.py", "--arms", "wiki:ask", "--n", str(n), "--skip", str(skip), "--min-n", str(n),
         "--adapter", "system", "--experience", experience, "--against", against, "--note", note], logfile)
    newest = max((ROOT / "readings").glob("hotpot-*.json"), key=lambda p: p.stat().st_mtime)
    r = json.loads(newest.read_text(encoding="utf-8"))
    a = r["arms"]["wiki:ask"]
    return {"reading": newest.stem, "exact": a["exact"], "lenient": a["lenient"], "n": a["n"]}


def tag(experience: str) -> str:
    """The part tag hotpot_reading.py gives the full-store asking of the system with these rows."""
    suffix = "" if experience == "system" else "-exp-" + Path(experience).stem
    return f"wikiask-lexical-system-r{Lineage().round()}{suffix}-rerank50"


def pair(after: str, before: str, ids: set[str]) -> tuple[int, int]:
    """(made right, made wrong) by the lenient score on the shared questions among `ids`."""
    def rows(t):
        p = ROOT / "state" / f"hotpot-{t}.part.jsonl"
        return {r["id"]: r["lenient"] for r in map(json.loads, p.read_text(encoding="utf-8").splitlines()) if r["id"] in ids}
    a, b = rows(after), rows(before)
    shared = [i for i in a if i in b]
    return sum(a[i] and not b[i] for i in shared), sum(b[i] and not a[i] for i in shared)


def ids_of(reading_name: str) -> set[str]:
    return {r["id"] for r in json.loads((ROOT / "readings" / f"{reading_name}.json").read_text(encoding="utf-8"))["arms"]["wiki:ask"]["rows"]}


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--heard", type=int, default=None, help="the day's first question (default: 1000 + 200 per experience round so far)")
    p.add_argument("--n", type=int, default=200, help="questions a day and a reading (8 for a smoke)")
    p.add_argument("--need-gib", type=float, default=None,
                   help="free card memory the day needs: the store's keys and their scoring (5.5), and the server (3 more) unless one is up")
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()
    lin = Lineage()
    if not lin.exists():
        raise SystemExit("no lineage: scripts/lineage.py init first")
    rnd = len(lin.ledger())
    days = sum(e.get("part") == "experience" for e in lin.ledger())
    heard = args.heard if args.heard is not None else HEARD_FROM + 200 * days
    work = ROOT / "state" / "live" / (f"round{rnd}" if args.n == 200 else f"smoke-round{rnd}-n{args.n}")
    candidates = work / "rows.jsonl"
    print(f"round {rnd}: day on validation questions {heard}-{heard + 200} with {len(lin.experience())} rows of its own, "
          f"sleep into {candidates}, read the day again and the first 200, decide", flush=True)
    if args.dry_run:
        return
    work.mkdir(parents=True, exist_ok=True)
    logfile = work / "live.log"
    wait_idle(args.need_gib if args.need_gib is not None else 5.5 if up() else 8.5)
    srv = serve(lin.ggufs(), logfile)
    try:
        day = reading(heard, "system", "floor", f"round {rnd}: the day, questions it has not heard, with its own rows", logfile, n=args.n)
        if not candidates.exists():
            run([PY, "scripts/chains.py", "--wiki", "--from", day["reading"], "--out", str(candidates)], logfile)
        written = [json.loads(l) for l in candidates.read_text(encoding="utf-8").splitlines() if l.strip()]
        log(f"the sleep wrote {len(written)} rows")
        merged = work / f"experience-{work.name}.jsonl"  # its stem tags the readings, so a smoke never shares them with a round
        merged.write_text("".join(json.dumps(r, ensure_ascii=False) + "\n" for r in lin.experience() + written), encoding="utf-8")
        regression_before = reading(0, "system", "floor", f"round {rnd}: the first 200 before the sleep", logfile, n=args.n)
        again = reading(heard, str(merged), tag("system"), f"round {rnd}: the day's questions again, after the sleep", logfile, n=args.n)
        regression = reading(0, str(merged), tag("system"), f"round {rnd}: the first 200 after the sleep", logfile, n=args.n)
    finally:
        if srv is not None:
            srv.kill()
    lines = {"gain": pair(tag(str(merged)), tag("system"), ids_of(day["reading"])),
             "regression": pair(tag(str(merged)), tag("system"), ids_of(regression_before["reading"]))}
    v = verdict({"the first 200": lines["regression"]}, {"the day again": lines["gain"]})
    log(f"verdict: {v} on {lines}")
    readings = {"day": day, "again": again, "regression_before": regression_before, "regression_after": regression,
                "score_before": day["lenient"], "score_after": again["lenient"], "paired": lines, "verdict": v}
    if args.n != 200:
        log("a smoke: nothing committed")
        return
    lin.commit_rows(written, f"the live loop's sleep, round {rnd}: chain rows from the day's search histories",
                    f"validation questions {heard}-{heard + 200}", readings, note=v["why"])
    if not v["keep"]:
        lin.rollback_rows(v["why"])
    log("cycle done")


if __name__ == "__main__":
    main()
