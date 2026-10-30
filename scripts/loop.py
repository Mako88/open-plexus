"""The loop, one round at a time: the pieces of step 3's forks run in order behind flags.

A round is: rollouts (the served trunk labels its own steps by outcome), features (the trunk in
process caches the judge-prompt states of those steps), fit (the evaluator refitted on them, read
on its own held-out rollouts and on the PRM800K test steps), adapter (the trunk learns the
right steps, unless --no-adapter), and the search reading on GSM8K test against the trunk's own
steps, with the new judge, the adapter if trained and the fetch arm if asked. Each stage is one
of the existing scripts, run as a subprocess; a stage whose output exists is skipped, so a round
ended for the card resumes at its stage; the server is started for the rollouts and stopped for
the stages that need the card. The round's reading is whether its search beats the round
before's at matched passes on problems never trained on.

    .venv/Scripts/python scripts/loop.py --round 1 --dry-run
    .venv/Scripts/python scripts/loop.py --round 1 --n-rollouts 700 --stages rollouts,features,fit,adapter,search

Writes state/rounds/round<R>.json with every command and where its output went.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PY = ROOT / ".venv" / "Scripts" / "python.exe"
SERVER = [r"D:\tools\llama.cpp-src\build\bin\Release\llama-server.exe", "-m",
          r"C:\Users\John\.cache\huggingface\hub\models--bartowski--Qwen_Qwen3.5-2B-GGUF\snapshots"
          r"\7d26695454df6de5fbcce2e58681e62dae06ce43\Qwen_Qwen3.5-2B-Q8_0.gguf",
          "--port", "8093", "--jinja", "-ngl", "99", "-np", "8", "-c", "32768", "--reasoning-budget", "0"]
STAGES = ("rollouts", "features", "fit", "adapter", "search")


def plan(args) -> list[dict]:
    """The round's stages as {"name", "command", "done" (a path whose existence means skip),
    "server" (whether the served trunk must be up)}."""
    r = args.round
    rows = ROOT / "state" / "rollouts" / "train.jsonl"
    feats = ROOT / "state" / "features" / f"rollouts-r{r}"
    steps = ROOT / "state" / f"steps-r{r}"
    adapter = ROOT / "state" / "adapters" / f"round{r}"
    arms = ["trunk", "search:4:1"] + (["search:4:1:fetch"] if args.fetch else [])
    search = [str(PY), "scripts/step_search.py", "--arms", ",".join(arms), "--n", str(args.n_search),
              "--evaluator", str(steps / "evaluator.npz"), "--reference", str(args.reference), "--min-n", "100",
              "--note", f"round {r}"]
    if not args.no_adapter:
        search += ["--adapter", str(adapter / "adapter")]
    if args.fetch:
        search += ["--fetch", str(rows)]
    stages = [
        {"name": "rollouts", "server": True, "done": None,
         "command": [str(PY), "scripts/step_rollouts.py", "rollouts", "--n", str(args.n_rollouts)]},
        {"name": "features", "server": False, "done": feats / "features.json",
         "command": [str(PY), "scripts/step_rollouts.py", "features", "--out", str(feats)]},
        {"name": "fit", "server": False, "done": steps / "evaluator.npz",
         "command": [str(PY), "scripts/step_evaluator.py", "fit", "--features", str(feats), "--out", str(steps),
                     "--also-test", "state/features/steps6", "--note", f"round {r}, labels from the system's own rollouts"]},
        {"name": "adapter", "server": False, "done": adapter / "adapter.json",
         "command": [str(PY), "scripts/trunk_adapter.py", "--rows", str(rows), "--out", str(adapter)]},
        {"name": "search", "server": False, "done": None, "command": search},
    ]
    if args.no_adapter:
        stages = [s for s in stages if s["name"] != "adapter"]
    return [s for s in stages if s["name"] in args.stages.split(",")]


def server_up() -> bool:
    import urllib.request

    try:
        with urllib.request.urlopen("http://127.0.0.1:8093/health", timeout=3) as r:
            return b"ok" in r.read()
    except Exception:  # noqa: BLE001
        return False


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--round", type=int, required=True)
    p.add_argument("--stages", default=",".join(STAGES))
    p.add_argument("--n-rollouts", type=int, default=700)
    p.add_argument("--n-search", type=int, default=500)
    p.add_argument("--reference", type=float, default=0.364, help="the control's score the search arms stop against")
    p.add_argument("--no-adapter", action="store_true", help="the round without the trunk learning: the control for fork b")
    p.add_argument("--fetch", action="store_true", help="add the fetch arm, fork d")
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()

    stages = plan(args)
    out = ROOT / "state" / "rounds" / f"round{args.round}.json"
    out.parent.mkdir(parents=True, exist_ok=True)
    record = {"round": args.round, "stages": [], "command": " ".join(sys.argv)}
    server = None
    for s in stages:
        line = " ".join(s["command"])
        if s["done"] is not None and Path(s["done"]).exists():
            print(f"== {s['name']}: done already ({s['done']})", flush=True)
            record["stages"].append({**s, "done": str(s["done"]), "skipped": True})
            continue
        print(f"== {s['name']}: {line}", flush=True)
        if args.dry_run:
            record["stages"].append({**s, "done": str(s["done"]), "dry_run": True})
            continue
        if s["server"] and not server_up():
            server = subprocess.Popen(SERVER, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            while not server_up():
                time.sleep(3)
        if not s["server"] and server is not None:
            server.terminate()
            server.wait()
            server = None
        started = time.perf_counter()
        code = subprocess.run(s["command"], cwd=ROOT).returncode
        record["stages"].append({**s, "done": str(s["done"]), "seconds": round(time.perf_counter() - started), "code": code})
        out.write_text(json.dumps(record, indent=1), encoding="utf-8")
        if code != 0:
            print(f"== {s['name']} failed with {code}; the round resumes here", flush=True)
            break
    if server is not None:
        server.terminate()
    out.write_text(json.dumps(record, indent=1), encoding="utf-8")
    print("wrote", out)


if __name__ == "__main__":
    main()
