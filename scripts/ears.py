"""Phase 3: read every telling in a house with one ear and score it against the facts.

    uv run python scripts/ears.py --seed 0 --url http://127.0.0.1:8094/v1/chat/completions \
        --name "Qwen3.5-2B-Q8_0 (llama.cpp, schema, cpu)"

recall     facts whose telling came back as one assertion holding every gold filler
precision  of the assertions naming a fact's subject, the share that were whole
filler     assertions read out of small talk, per filler sentence
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path

from unfused.ears import Ear, _fillers, gold, score_reading
from unfused.exam.world import _FILLER, generate_house

ROOT = Path(__file__).resolve().parents[1]


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--seed", type=int, default=0)
    p.add_argument("--url", default="http://127.0.0.1:8094/v1/chat/completions")
    p.add_argument("--name", required=True)
    p.add_argument("--limit", type=int, default=None)
    p.add_argument("--note", default="")
    args = p.parse_args()

    house = generate_house(args.seed)
    ear = Ear(url=args.url, name=args.name)
    facts = house.facts[: args.limit] if args.limit else house.facts
    rows, whole, named, named_whole = [], 0, 0, 0
    for fact in facts:
        assertions = ear.read(fact.told)
        scored = score_reading(fact, assertions)
        subject = fact.subject.lower()
        mine = [a for a in assertions if subject in _fillers(a)]
        wanted = [w.lower() for w in gold(fact)]
        named += len(mine)
        named_whole += sum(all(w in _fillers(a) for w in wanted) for a in mine)
        whole += scored["whole"]
        rows.append({"kind": fact.kind, "told": fact.told, "gold": gold(fact),
                     "assertions": assertions, **scored})
    filler = {s: ear.read(s) for s in _FILLER}
    reading = {
        "kind": "ears",
        "ear": args.name,
        "taken_at": datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ"),
        "note": args.note,
        "house": {"seed": args.seed, "fingerprint": house.fingerprint()},
        "limit": args.limit,
        "scorer": "each gold filler in a slot of its own",
        "recall": round(whole / len(facts), 3),
        "precision": round(named_whole / named, 3) if named else 0.0,
        "filler_assertions_per_sentence": round(
            sum(len(v) for v in filler.values()) / len(filler), 2),
        "by_kind": {k: round(sum(r["whole"] for r in rows if r["kind"] == k)
                             / max(1, sum(r["kind"] == k for r in rows)), 3)
                    for k in sorted({r["kind"] for r in rows})},
        "calls": ear.calls,
        "tokens": ear.tokens,
        "seconds": round(ear.seconds, 1),
        "unparsed": len(ear.failures),
        "rows": rows,
        "filler": filler,
    }
    tag = args.name.split(" ")[0]
    out = ROOT / "readings" / f"ears-{tag}-s{args.seed}-{reading['taken_at']}.json"
    out.write_text(json.dumps(reading, indent=1), encoding="utf-8")
    print(f"{args.name}: recall {reading['recall']} precision {reading['precision']} "
          f"filler {reading['filler_assertions_per_sentence']} by kind {reading['by_kind']} "
          f"{reading['seconds']}s -> {out.name}")


if __name__ == "__main__":
    main()
