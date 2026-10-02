"""The second house's tellings read by the ear and by the parse, on one generic gold.

    uv run python scripts/parsed_second.py

Held out from the making of `scripts/parsed.py`'s mapping, so it says whether that
mapping carries to relations it was not written against.
"""

import json
import sys
import time
from datetime import UTC, datetime
from itertools import permutations
from pathlib import Path

sys.path.insert(0, "scripts")
import spacy  # noqa: E402
from parsed import assertions  # noqa: E402

from unfused.ears import Ear  # noqa: E402
from unfused.exam.second import generate_second_house  # noqa: E402


def gold(fact):
    return [fact.subject, fact.answer, *[v for v in fact.fields.values() if v]]


def whole(fact, read):
    wanted = [w.lower() for w in gold(fact)]
    for a in read:
        slots = [str(v).lower() for v in a.values() if v]
        if len(slots) >= len(wanted) and any(
                all(w in s for w, s in zip(wanted, chosen))
                for chosen in permutations(slots, len(wanted))):
            return True
    return False


nlp = spacy.load("en_core_web_trf")
ear = Ear(url="http://127.0.0.1:8094/v1/chat/completions")
out = []
for seed in (1, 2, 3):
    house = generate_second_house(seed)
    tally = {"ear": [], "parse": []}
    times = {"ear": 0.0, "parse": 0.0}
    for f in house.facts:
        t = time.perf_counter(); r = ear.read(f.told); times["ear"] += time.perf_counter() - t
        tally["ear"].append({"kind": f.kind, "told": f.told, "whole": whole(f, r), "read": r})
        t = time.perf_counter(); r = assertions(nlp(f.told)); times["parse"] += time.perf_counter() - t
        tally["parse"].append({"kind": f.kind, "told": f.told, "whole": whole(f, r), "read": r})
    summary = {}
    for arm, rows in tally.items():
        kinds = sorted({r["kind"] for r in rows})
        summary[arm] = {"recall": round(sum(r["whole"] for r in rows) / len(rows), 3),
                        "by_kind": {k: round(sum(r["whole"] for r in rows if r["kind"] == k)
                                             / sum(r["kind"] == k for r in rows), 2) for k in kinds}}
    print(seed, summary)
    out.append({"seed": seed, "summary": summary, "rows": tally})

taken = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
Path(f"readings/parsed-second-{taken}.json").write_text(json.dumps({
    "kind": "parsed", "world": "second", "taken_at": taken,
    "note": "the second house's tellings, held out from the mapping's making, read by the 0.8B ear "
            "(no relations list, cached) and by en_core_web_trf with scripts/parsed.py's mapping; "
            "gold is the subject, the answer and any thing, each in a slot of its own",
    "seeds": out}, indent=1), encoding="utf-8")
print("-> parsed-second-" + taken + ".json")
