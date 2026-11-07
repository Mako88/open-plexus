"""Store-first consolidation (John, 2026-11-05/06: the store is the knowledge; facts in the weights
compete with facts in the store): a sleep that writes rows, not weights. From the system's own
experience, the asking's search histories, every pair of passages it read one after the other on
a question becomes a chain row: the trunk writes, from the two passages, one line saying how they
connect ("Kiss and Tell (1945 film) -> Shirley Temple: she played Corliss Archer in it"). Code keeps
the line only if it names both passages' subjects; the passages themselves are kept. Asked again,
the store holds the chain beside them (`hotpot_reading.py --chains`), so a two-hop question can be
one search.

    .venv/Scripts/python scripts/chains.py --from hotpot-20261105T013918Z --out state/chains/r0-first200.jsonl
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.hotpot import normalise, paragraph_text, questions  # noqa: E402
from openplexus.lineage import Lineage  # noqa: E402
from openplexus.trunk import ServedTrunk  # noqa: E402

CHAIN_SYSTEM = (
    "You are given two passages. Write one line saying how the subject of the first is connected to the "
    "subject of the second, in the form 'A -> B: how they are connected', where A and B are the two "
    "subjects as the passages name them. Use only what the passages say. Reply with the one line only."
)


def subject(title: str) -> str:
    """A passage's subject as code checks for it: the title without its parenthesis."""
    return re.sub(r"\s*\(.*?\)\s*$", "", title).strip()


def kept(line: str, a: str, b: str) -> bool:
    """The line names both subjects; nothing else is required of it."""
    n = normalise(line)
    return bool(line.strip()) and normalise(subject(a)) in n and normalise(subject(b)) in n


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--from", dest="source", required=True, help="an asking reading whose search histories are the experience")
    p.add_argument("--out", required=True)
    p.add_argument("--n", type=int, default=200)
    p.add_argument("--skip", type=int, default=0)
    args = p.parse_args()
    rows = json.loads((ROOT / "readings" / f"{args.source}.json").read_text(encoding="utf-8"))["arms"]["ask"]["rows"]
    by_id = {r["id"]: r for r in rows}
    qs = [q for q in questions(args.n, skip=args.skip) if q["id"] in by_id]
    pairs = []
    for q in qs:
        text = {p_["title"]: paragraph_text(p_) for p_ in q["paragraphs"]}
        shown = [t for t in by_id[q["id"]]["shown"] if t in text]
        for a, b in zip(shown, shown[1:]):
            if a != b:
                pairs.append((q["id"], a, b, text[a], text[b]))
    trunk = ServedTrunk(max_new=80, parallel=8)
    names = {g.name for g in Lineage().ggufs()}
    trunk.lora = [{"id": x["id"], "scale": 1.0 if Path(x["path"]).name in names else 0.0} for x in trunk.adapters()]
    replies = trunk.ask_many([[{"role": "system", "content": CHAIN_SYSTEM},
                               {"role": "user", "content": f"First passage:\n{ta}\n\nSecond passage:\n{tb}"}] for _, _, _, ta, tb in pairs])
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    n_kept = 0
    with open(out, "w", encoding="utf-8") as f:
        for (qid, a, b, _, _), reply in zip(pairs, replies):
            line = reply.strip().splitlines()[0].strip() if reply.strip() else ""
            ok = kept(line, a, b)
            n_kept += ok
            f.write(json.dumps({"id": qid, "a": a, "b": b, "text": line, "kept": ok, "source": args.source}, ensure_ascii=False) + "\n")
    print(f"{len(pairs)} pairs read in sequence on {len(qs)} questions, {n_kept} chain rows kept, written to {out}", flush=True)


if __name__ == "__main__":
    main()
