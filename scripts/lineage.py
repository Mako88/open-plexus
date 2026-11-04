"""The lineage's commands (`openplexus.lineage`): start it, commit a round after its readings, roll
the last round back, and show the ledger.

    .venv/Scripts/python scripts/lineage.py init --adapter state/ask/round2/adapter --gguf state/gguf/ask-round2-lora.gguf --what "the asking, round two"
    .venv/Scripts/python scripts/lineage.py commit --adapter state/ask/x/adapter --what "..." --trained-on "..." --questions 480 \
        --before hotpot-...:0.61 --after hotpot-...:0.65 --reading asking=hotpot-...:0.54 --reading maths=grpo-...:0.73
    .venv/Scripts/python scripts/lineage.py rollback --why "..."
    .venv/Scripts/python scripts/lineage.py show
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.lineage import Lineage  # noqa: E402


def reading_arg(text: str) -> tuple[str, float | None]:
    """'hotpot-20261104T160600Z:0.56' -> (id, score); the score may be left off."""
    name, _, score = text.partition(":")
    return name, (float(score) if score else None)


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="command", required=True)
    i = sub.add_parser("init")
    i.add_argument("--adapter", required=True)
    i.add_argument("--gguf", default=None, help="its GGUF where one exists; converted otherwise")
    i.add_argument("--what", required=True)
    i.add_argument("--note", default="")
    c = sub.add_parser("commit")
    c.add_argument("--adapter", required=True)
    c.add_argument("--what", required=True)
    c.add_argument("--trained-on", required=True)
    c.add_argument("--questions", type=int, required=True)
    c.add_argument("--before", default=None, help="the reading of the system before, id:score")
    c.add_argument("--after", default=None, help="the reading of the system after, id:score")
    c.add_argument("--reading", action="append", default=[], help="other readings, purpose=id:score (the regression set)")
    c.add_argument("--note", default="")
    r = sub.add_parser("rollback")
    r.add_argument("--why", required=True)
    sub.add_parser("show")
    args = p.parse_args()
    lin = Lineage()
    if args.command == "init":
        print(json.dumps(lin.init(Path(args.adapter), args.what, args.note, Path(args.gguf) if args.gguf else None), indent=1))
    elif args.command == "commit":
        readings = {}
        for key, text in (("before", args.before), ("after", args.after)):
            if text:
                name, score = reading_arg(text)
                readings[key] = name
                if score is not None:
                    readings[f"score_{key}"] = score
        for item in args.reading:
            purpose, _, text = item.partition("=")
            name, score = reading_arg(text)
            readings[purpose] = {"reading": name, "score": score}
        print(json.dumps(lin.commit(Path(args.adapter), args.what, args.trained_on, args.questions, readings, args.note), indent=1))
    elif args.command == "rollback":
        print(json.dumps(lin.rollback(args.why), indent=1))
    else:
        for e in lin.ledger():
            price = e.get("questions_per_point")
            print(f"round {e['round']}: {e['what']}" + (f" | {e['questions']} questions, {e.get('points_gained')} points, "
                  f"{price if price else 'no gain'}" + (" questions a point" if price else "") if "questions" in e else "")
                  + (" | ROLLED BACK: " + e["rolled_back"]["why"] if e.get("rolled_back") else ""))


if __name__ == "__main__":
    main()
