"""What the system rehearses while it sleeps: its own answers that were right. Step 7's sleep that
replayed only new facts cost the trunk its reading (gold 0.61 to 0.52, 2026-11-03), the forgetting
the sleep itself causes; the cure is interleaving, the new rows replayed beside a sample of what the
system already does. Here the served system answers training questions from their gold paragraphs
in the reading's own form, and the answers the key marks right are kept as transcripts to replay
(`consolidate.py --rehearse`). Self-distillation: nothing is handed to the system that it did not
produce; the key only picks.

    .venv/Scripts/python scripts/rehearsal.py --n 400 --skip 2000 --adapter system --out state/rehearsal/system-r0.jsonl
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.hotpot import READ_SYSTEM, answer_of, exact, questions, read_text  # noqa: E402
from openplexus.lineage import Lineage  # noqa: E402
from openplexus.trunk import ServedTrunk  # noqa: E402


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--n", type=int, default=400, help="training questions asked")
    p.add_argument("--skip", type=int, default=2000, help="past the questions the rounds trained on")
    p.add_argument("--adapter", default="system", help="the GGUF the server has loaded to apply: 'system' is the lineage's")
    p.add_argument("--out", required=True)
    p.add_argument("--max-new", type=int, default=160)
    args = p.parse_args()
    gguf = Path(Lineage().current_gguf()) if args.adapter == "system" else Path(args.adapter)
    trunk = ServedTrunk(max_new=args.max_new, parallel=8)
    loaded = trunk.adapters()
    trunk.lora = [{"id": a["id"], "scale": 1.0 if Path(a["path"]).name == gguf.name else 0.0} for a in loaded]
    if loaded and not any(x["scale"] for x in trunk.lora):
        raise SystemExit(f"the server has not loaded {gguf.name}: {loaded}")
    qs = questions(args.n, split="train", skip=args.skip)
    prompts = [[{"role": "system", "content": READ_SYSTEM},
                {"role": "user", "content": read_text(q, [x for x in q["paragraphs"] if x["supporting"]])}] for q in qs]
    replies = trunk.ask_many(prompts)
    kept = 0
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        for q, prompt, reply in zip(qs, prompts, replies):
            if exact(answer_of(reply), q["answer"]):
                kept += 1
                f.write(json.dumps({"id": q["id"], "messages": prompt + [{"role": "assistant", "content": reply}]}, ensure_ascii=False) + "\n")
    print(f"{kept} of {len(qs)} answers right and kept as rehearsal in {out} (served {trunk.served_model()[-80:]})", flush=True)


if __name__ == "__main__":
    main()
