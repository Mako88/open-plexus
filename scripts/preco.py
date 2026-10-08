"""Read the system's own binding of pronouns against PreCo's gold chains, and write one
reading per arm under `readings/`.

    uv run python scripts/preco.py --arms graphed,primed --note "..."

One system hears every document in turn, a break before each, and is never restarted
between them; nothing is asked and nobody reacts. At each third-person pronoun with an
earlier mention in its gold chain, what the system bound it to is right where that
individual is one an earlier naming of the chain became: a mention the system heard is
the gold one whose span holds it, the narrowest, and an earlier pronoun counts for
nothing, so a chain of pronouns bound alike to the wrong thing is wrong at every one.
Read beside it, from the same run:

- `recent`: the individual of the last thing named before the pronoun, the system's own
  mentions in the order said. What binding by recency alone scores.
- `reachable`: the share of pronouns whose chain has an earlier mention the system made
  an individual of at all, so the ceiling binding can reach while the mentions are read
  as they are.

The arms:

- `graphed`: the system, starting empty.
- `primed`: the system after the first `--primed` stories of the TinyStories stream, heard
  and taught as `scripts/stories.py` does, so what it learnt of agreement there is read.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
import tempfile
import time
from collections import defaultdict
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from unfused.exam.preco import PRONOUNS, documents, fingerprint  # noqa: E402
from unfused.reading import utc_stamp  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
BREAK = "***"
GROUPS = {"he": {"he", "him", "his", "himself"}, "she": {"she", "her", "hers", "herself"},
          "it": {"it", "its", "itself"}, "they": {"they", "them", "their", "theirs",
                                                  "themselves"}}


def _open(name: str, work: Path, primed: int):
    from unfused.graph import GraphArm

    if name == "graphed":
        return GraphArm(work), 0
    if name == "primed":
        sys.path.insert(0, str(ROOT / "scripts"))
        import stories as stream_runner
        from unfused.exam.stories import stream

        runner = stream_runner.Graphed(work)
        turn = stream_runner._stream(runner, stream(primed, 0), 100, [], {})
        runner.arm.pending = None
        return runner.arm, turn
    raise SystemExit(f"unknown arm {name}")


def _gold(doc, sentence: int, at: int):
    """The narrowest gold mention of a sentence whose span holds a place."""
    held = [m for m in doc.mentions if m.sentence == sentence and m.start <= at < m.end]
    return min(held, key=lambda m: m.end - m.start, default=None)


def run(arm, turn: int, docs) -> dict:
    rows = []
    started, cpu = time.perf_counter(), time.process_time()
    for doc in docs:
        arm.hear(turn, BREAK)
        turn += 1
        # each gold mention, by its place, with the individual the system made of it
        became: dict[tuple, str] = {}
        # the system's own mentions in the order said, for recency
        heard: list[tuple[int, int, str]] = []
        bound: dict[tuple, str | None] = {}
        for s, text in enumerate(doc.told):
            arm.hear(turn, text)
            turn += 1
            for (kind, at), node in sorted(arm.hearer.bound.items(), key=lambda kv: kv[0][1]):
                if not node.startswith("i:"):
                    continue
                g = _gold(doc, s, at)
                if kind == "pronoun":
                    bound[(s, at)] = node
                    continue
                heard.append((s, at, node))
                if g is not None:
                    became.setdefault((g.sentence, g.start, g.end), node)
        for i, m in enumerate(doc.mentions):
            if m.word not in PRONOUNS:
                continue
            earlier = [e for e in doc.mentions[:i] if e.chain == m.chain
                       and (e.sentence, e.start) < (m.sentence, m.start)]
            if not earlier:
                continue
            nodes = {became[k] for e in earlier
                     if (k := (e.sentence, e.start, e.end)) in became}
            node = bound.get((m.sentence, m.start))
            before = [n for s, at, n in heard if (s, at) < (m.sentence, m.start)]
            rows.append({"doc": doc.name, "sentence": m.sentence, "at": m.start,
                         "pronoun": m.word, "said": doc.told[m.sentence],
                         "same_sentence": any(e.sentence == m.sentence for e in earlier),
                         "bound": node is not None, "right": node in nodes,
                         "recent": bool(before) and before[-1] in nodes,
                         "reachable": bool(nodes)})
        print(f"  {doc.name}: {len(doc.told)} sentences, {len(rows)} pronouns so far",
              flush=True)
    return {"rows": rows, "seconds": round(time.perf_counter() - started, 1),
            "cpu_seconds": round(time.process_time() - cpu, 1)}


def summary(rows) -> dict:
    def mean(rs):
        n = len(rs)
        return {k: round(sum(r[k] for r in rs) / n, 3) if n else None
                for k in ("right", "recent", "bound", "reachable")} | {"n": n}

    by_group = defaultdict(list)
    for r in rows:
        by_group[next(g for g, ws in GROUPS.items() if r["pronoun"] in ws)].append(r)
    return {**mean(rows),
            "same_sentence": mean([r for r in rows if r["same_sentence"]]),
            "earlier_sentence": mean([r for r in rows if not r["same_sentence"]]),
            "by_pronoun": {k: mean(v) for k, v in sorted(by_group.items())}}


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--arms", default="graphed")
    p.add_argument("--split", default="dev")
    p.add_argument("--docs", type=int, default=None, help="the first N documents")
    p.add_argument("--primed", type=int, default=1000)
    p.add_argument("--note", default="")
    args = p.parse_args()

    docs = documents(args.split, args.docs)
    print(f"{len(docs)} documents", flush=True)
    for name in args.arms.split(","):
        work = Path(tempfile.mkdtemp(prefix=f"unfused-preco-{name}-"))
        arm, turn = _open(name, work, args.primed)
        out = run(arm, turn, docs)
        dials = arm.dials()
        arm.close()
        shutil.rmtree(work, ignore_errors=True)
        taken = utc_stamp()
        reading = {"kind": "preco", "arm": name, "taken_at": taken, "note": args.note,
                   "command": " ".join(sys.argv),
                   "world": {"source": f"PreCo-{args.split}", "documents": len(docs),
                             "pronouns": len(out["rows"]), "fingerprint": fingerprint(docs)},
                   "summary": summary(out["rows"]), "dials": dials,
                   "seconds": out["seconds"], "cpu_seconds": out["cpu_seconds"],
                   "rows": out["rows"]}
        tag = f"n{args.primed}" if name == "primed" else "none"
        path = ROOT / "readings" / f"preco-{name}-{tag}-{taken}.json"
        path.write_text(json.dumps(reading, indent=1), encoding="utf-8")
        s = reading["summary"]
        groups = "  ".join(f"{k}:{v['right']}/{v['recent']} (n={v['n']})"
                           for k, v in s["by_pronoun"].items())
        print(f"{name:8} right {s['right']}  recent {s['recent']}  bound {s['bound']}  "
              f"reachable {s['reachable']}  same-sentence {s['same_sentence']['right']}  "
              f"earlier {s['earlier_sentence']['right']}  {out['seconds']}s -> {path.name}"
              f"\n  {groups}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
