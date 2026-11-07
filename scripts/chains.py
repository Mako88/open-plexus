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
    "You are given two passages. Write one line: the subject of the first passage, then ' -> ', then the "
    "subject of the second, then ': ', then in your own words the fact that connects them, taken from the "
    "passages. For example: 'Ed Wood (film) -> Tim Burton: Tim Burton directed the film.' Reply with the one line only."
)


def subject(title: str) -> str:
    """A passage's subject as code checks for it: the title without its parenthesis."""
    return re.sub(r"\s*\(.*?\)\s*$", "", title).strip()


def kept(line: str, a: str, b: str) -> bool:
    """The line names both subjects; nothing else is required of it."""
    n = normalise(line)
    said = line.split(":", 1)[1].strip() if ":" in line else ""
    if not said or "how they are connected" in n or "tim burton directed" in n:
        return False  # the template or the example echoed back, not a connection
    return normalise(subject(a)) in n and normalise(subject(b)) in n


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--from", dest="source", required=True, help="an asking reading whose search histories are the experience")
    p.add_argument("--out", required=True)
    p.add_argument("--n", type=int, default=200)
    p.add_argument("--skip", type=int, default=0)
    p.add_argument("--wiki", action="store_true",
                   help="the experience is a full-store asking (wiki:ask, its shown_rows): every row written is self-contained, "
                        "its line and both Wikipedia passages, for the lineage's store part (Lineage.commit_rows)")
    p.add_argument("--pairs", help="with --wiki: the pairs to write lines for, from wide_pairs.py, in place of the histories")
    args = p.parse_args()
    reading = json.loads((ROOT / "readings" / f"{args.source}.json").read_text(encoding="utf-8"))
    pairs = []
    if args.wiki:
        from openplexus.wiki import WikiTexts

        wiki = WikiTexts(ROOT / "data" / "wiki-abstracts" / "minilm")
        had = {(r["a"], r["b"]) for r in Lineage().experience()}
        if args.pairs:  # the wide sleep's own search (wide_pairs.py), not the day's histories
            found = [json.loads(l) for l in Path(args.pairs).read_text(encoding="utf-8").splitlines() if l.strip()]
            seq = [(x["from"], x["a_row"], x["b_row"]) for x in found]
        else:
            seq = []
            for r in reading["arms"]["wiki:ask"]["rows"]:
                shown = [i for i in r.get("shown_rows", []) if i < wiki.n]  # the system's own rows are not chained again
                seq += [(r["id"], i, j) for i, j in zip(shown, shown[1:])]
        for qid, i, j in seq:
            a, b = wiki.title(i), wiki.title(j)
            if i != j and (a, b) not in had and (b, a) not in had:
                had.add((a, b))
                pairs.append((qid, a, b, wiki.text(i), wiki.text(j)))
        qs = reading["arms"]["wiki:ask"]["rows"]
    else:
        rows = reading["arms"]["ask"]["rows"]
        by_id = {r["id"]: r for r in rows}
        qs = [q for q in questions(args.n, skip=args.skip) if q["id"] in by_id]
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
        for (qid, a, b, ta, tb), reply in zip(pairs, replies):
            line = reply.strip().splitlines()[0].strip() if reply.strip() else ""
            ok = kept(line, a, b)
            n_kept += ok
            if args.wiki:
                if ok:  # only kept rows, each carrying what it came from
                    f.write(json.dumps({"title": f"chain: {a} -> {b}", "text": "\n\n".join([line, ta, tb]), "a": a, "b": b,
                                        "line": line, "from": qid, "source": args.source}, ensure_ascii=False) + "\n")
                continue
            f.write(json.dumps({"id": qid, "a": a, "b": b, "text": line, "kept": ok, "source": args.source}, ensure_ascii=False) + "\n")
    print(f"{len(pairs)} pairs read in sequence on {len(qs)} questions, {n_kept} chain rows kept, written to {out}", flush=True)


if __name__ == "__main__":
    main()
