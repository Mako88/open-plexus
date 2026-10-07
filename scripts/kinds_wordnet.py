"""Kinds scored against WordNet's coarse noun categories on a stream graph, WordNet the
yardstick only and never read by the system: pairwise precision and recall over the
nouns heard most. Seconds, where a stream reading is minutes, so a change to `kinds` is
read here first and on the stream after.

    uv run python scripts/kinds_wordnet.py state/graphs/stream --top 300
"""

import argparse
import os
import time
from collections import Counter
from itertools import combinations
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")

from nltk.corpus import wordnet as wn  # noqa: E402

from unfused.graph import GraphArm  # noqa: E402
from unfused.kinds import kinds  # noqa: E402


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("graph", type=Path, help="a directory holding a stream run's graph.db")
    ap.add_argument("--top", type=int, default=300)
    args = ap.parse_args()
    a = GraphArm(args.graph)
    rows = a.arguments()
    heard = Counter(r[3] for r in rows)
    truths: dict = {}

    def truth(w: str) -> str | None:
        if w not in truths:
            mark = a.mark(w)
            got = wn.synsets(w.split()[-1], pos=wn.NOUN) if mark == "NOUN" else []
            truths[w] = ("noun.person" if mark == "PROPN"
                         else got[0].lexname() if got else None)
        return truths[w]

    nouns = [w for w, _ in heard.most_common() if w and truth(w)][:args.top]
    t = time.time()
    kind = kinds(rows)
    tp = fp = fn = 0
    for x, y in combinations(nouns, 2):
        same_k, same_t = kind.get(x) == kind.get(y), truth(x) == truth(y)
        tp += same_k and same_t
        fp += same_k and not same_t
        fn += same_t and not same_k
    p = tp / (tp + fp) if tp + fp else 0.0
    r = tp / (tp + fn) if tp + fn else 0.0
    sizes = Counter(kind[w] for w in nouns)
    print(f"{len(rows)} rows, {len(heard)} labels, {len(nouns)} nouns scored")
    print(f"P {p:.3f} R {r:.3f} F1 {2 * p * r / (p + r) if p + r else 0.0:.3f}  "
          f"kinds among them {len(sizes)}, alone "
          f"{sum(1 for w in nouns if sizes[kind[w]] == 1)}  ({time.time() - t:.0f}s)")


if __name__ == "__main__":
    main()
