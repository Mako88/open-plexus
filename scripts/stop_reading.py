"""Step 3, the stop over the thinking stream, read first on recorded thoughts.

The trunk's thinking states a conclusion, then circles and re-checks until the budget cuts it.
Before any head is trained, this counterfactual asks what cutting the thought at its k-th
stated conclusion would have scored, on the thoughts a thinking reading recorded: a regex for
"the answer is N" and its kin stands in for the stop. No forward pass is spent; the answer
taken is the conclusion's own number. If stopping early keeps the score, the stop head buys
cost; if the first conclusion is worse than the last, the head has to judge, not just stop.

    .venv/Scripts/python scripts/stop_reading.py --reading readings/ceiling-*think*.json --arm ceiling --kind count
    .venv/Scripts/python scripts/stop_reading.py --reading readings/maths-*.json --arm cot:2048 --kind maths

Writes readings/stop-*.json.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.maths import same_number  # noqa: E402

WORDS = {"none": "0", "zero": "0", "one": "1", "two": "2", "three": "3", "four": "4", "five": "5",
         "six": "6", "seven": "7", "eight": "8", "nine": "9", "ten": "10"}
LEAD = r"(?:the answer is|answer:|final answer:?(?: is)?|count ?=|total(?: is|:| =)|so,? |therefore,? )"
PATTERNS = {
    "count": re.compile(LEAD + r"\s*\**\s*(none|zero|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\b", re.I),
    "maths": re.compile(r"(?:the answer is|final answer:?(?: is)?|answer:|the total is|so the total is|"
                        r"so (?:he|she|they|it|there) \w+ (?:a total of )?)\s*\**\s*\$?\s*(-?\d[\d,]*(?:\.\d+)?)", re.I),
}


def number(text: str) -> str:
    x = text.strip().lower().replace(",", "")
    x = WORDS.get(x, x)
    return x[:-2] if x.endswith(".0") else x


def right(said: str | None, gold: str) -> bool:
    return said is not None and same_number(number(said), number(gold))


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--reading", required=True, help="a reading whose rows carry 'thought'")
    p.add_argument("--arm", required=True)
    p.add_argument("--kind", choices=sorted(PATTERNS), required=True)
    p.add_argument("--conclusions", type=int, default=3, help="read the stop at the 1st..k-th conclusion")
    p.add_argument("--note", default="")
    args = p.parse_args()

    source = json.load(open(args.reading, encoding="utf-8"))
    arm = source["arms"][args.arm]
    rows = [r for r in arm["rows"] if r.get("thought")]
    pat = PATTERNS[args.kind]
    own = sum(right(r.get("said"), r["answer"]) for r in rows) / len(rows)
    stops = {}
    out_rows = []
    for k in range(1, args.conclusions + 1):
        score = found = 0
        fracs = []
        for r in rows:
            ms = list(pat.finditer(r["thought"]))
            if ms:
                m = ms[min(k, len(ms)) - 1]
                said, at = m.group(1), m.end() / len(r["thought"])
                found += 1
                fracs.append(at)
            else:
                said, at = r.get("said"), 1.0
            ok = right(said, r["answer"])
            score += ok
            if k == 1:
                out_rows.append({"answer": r["answer"], "trunk_said": r.get("said"), "stopped_said": said,
                                 "stopped_at": round(at, 3), "correct": ok, "conclusions": len(ms)})
        fracs.sort()
        stops[f"conclusion_{k}"] = {
            "score": round(score / len(rows), 3), "found_in": found,
            "thought_fraction_median": round(fracs[len(fracs) // 2], 3) if fracs else None,
            "thought_fraction_mean": round(sum(fracs) / len(fracs), 3) if fracs else None,
        }
    reading = {
        "kind": "stop", "step": 3, "source_reading": str(Path(args.reading).as_posix()), "source_arm": args.arm,
        "model": source.get("model") or arm.get("served_model"), "pattern": pat.pattern,
        "scorer": "the conclusion's number against the gold, number words as digits",
        "n": len(rows), "trunks_own_end": round(own, 3), "stops": stops, "rows": out_rows,
        "command": "scripts/stop_reading.py " + " ".join(sys.argv[1:]),
        "sha": subprocess.run(["git", "rev-parse", "--short", "HEAD"], capture_output=True, text=True,
                              cwd=ROOT).stdout.strip(),
        "taken_at": datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"), "note": args.note,
    }
    out = ROOT / "readings" / f"stop-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(f"trunk's own end {own:.3f} on {len(rows)}; " +
          "; ".join(f"{k}: {v['score']} at {v['thought_fraction_median']} of the thought" for k, v in stops.items()))
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
