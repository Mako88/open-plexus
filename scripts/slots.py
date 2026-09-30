"""Where the ear puts each gold filler of a fact, per kind of fact.

A taught step matches only rows filling the slots the taught row filled, with each
value in the slot it was taught in. This reads how often the ear's choice of slot
for the same role varies across tellings, which is what that strictness costs.

    uv run python scripts/slots.py --seeds 1000,1001,1002 --port 8094
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import tempfile
from collections import Counter, defaultdict
from datetime import UTC, datetime
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.ears import Ear, gold  # noqa: E402
from unfused.exam.world import generate_house  # noqa: E402
from unfused.store import MiniLmEmbedder  # noqa: E402
from unfused.system import SLOTS, SystemArm, norm  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SHORT = {"subject": "s", "object": "o", "place": "p", "quantity": "q"}


def layout(fillers: list[str], rows: list[dict]) -> str:
    """The slot each gold filler sits in, in the row of the sentence holding most of
    them: '-' where no slot holds it, and a slot shared by two fillers counts once."""
    best, found = "", -1
    for r in rows:
        where = []
        for f in fillers:
            slot = next((k for k in SLOTS if r[k] and f in r[k]), None)
            where.append(SHORT[slot] if slot else "-")
        n = len({w for w in where if w != "-"})
        if n > found:
            best, found = "".join(where), n
    return best or "-" * len(fillers)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--seeds", default="1000,1001,1002")
    p.add_argument("--port", type=int, default=8094)
    p.add_argument("--served", default="Qwen3.5-0.8B-Q8_0")
    args = p.parse_args()

    ear = Ear(url=f"http://127.0.0.1:{args.port}/v1/chat/completions",
              name=f"{args.served} (llama.cpp)")
    embedder = MiniLmEmbedder()
    by_kind: dict[str, Counter] = defaultdict(Counter)
    by_relation: dict[str, dict[str, Counter]] = defaultdict(lambda: defaultdict(Counter))
    for seed in (int(s) for s in args.seeds.split(",")):
        house = generate_house(seed=seed)
        work = Path(tempfile.mkdtemp(prefix="unfused-slots-"))
        arm = SystemArm(work, ear, embedder, cleans=True)
        for turn, text in enumerate(house.turns):
            arm.hear(turn, text)
        rows = [dict(zip(("relation", *SLOTS, "heard"), r)) for r in arm.db.execute(
            "SELECT relation, subject, object, place, quantity, heard FROM assertions")]
        arm.close()
        shutil.rmtree(work, ignore_errors=True)
        for fact in house.facts:
            heard = [r for r in rows if r["heard"] == fact.told]
            fillers = [norm(f) or "" for f in gold(fact)]
            shape = layout(fillers, heard)
            by_kind[fact.kind][shape] += 1
            relation = next((r["relation"] for r in heard), "(none)")
            by_relation[fact.kind][relation][shape] += 1
        print(f"seed {seed} heard", flush=True)

    out = {"kinds": {k: dict(c.most_common()) for k, c in by_kind.items()},
           "relations": {k: {r: dict(c.most_common()) for r, c in v.items()}
                         for k, v in by_relation.items()},
           "roles": {"trade": "who, what", "number": "thing, room, n",
                     "place": "who, thing, room", "colour": "thing, colour",
                     "relation": "a, b, cousin"},
           "seeds": args.seeds, "ear": ear.name}
    for kind, c in by_kind.items():
        total = sum(c.values())
        modal = c.most_common(1)[0][1]
        print(f"{kind:9} {total:3} modal {modal / total:.2f}  {dict(c.most_common())}")
    taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = ROOT / "readings" / f"slots-{args.served}-{taken}.json"
    path.write_text(json.dumps(out, indent=1), encoding="utf-8")
    print(path)


if __name__ == "__main__":
    main()
