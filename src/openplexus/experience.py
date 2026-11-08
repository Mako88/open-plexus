"""The store with the system's own experience in it: the full Wikipedia store and, beside it, the
rows its sleeps wrote (store-first consolidation: chain rows, each bringing the passages it came
from). An experience row is numbered after the store's own rows and searched in the same pass:
its words against the query, and the reranker reading it beside Wikipedia's candidates, so a
chain the system already worked out comes first when it answers the query, and never displaces a
passage it does not.
"""

from __future__ import annotations

import re


def words(text: str) -> set[str]:
    return {w for w in re.findall(r"[a-z0-9]+", text.lower()) if len(w) > 2}


class Experience:
    def __init__(self, base, rows: list[dict]):
        """`base` is the store (WikiTexts); `rows` are {"text", "title"} the sleeps wrote."""
        self.base, self.rows = base, rows
        self.n = base.n + len(rows)
        self._words = [words(r["text"]) for r in rows]

    def text(self, i: int) -> str:
        return self.base.text(i) if i < self.base.n else self.rows[i - self.base.n]["text"]

    def title(self, i: int) -> str:
        return self.base.title(i) if i < self.base.n else self.rows[i - self.base.n]["title"]

    def candidates(self, query: str, m: int, exclude: set[int]) -> list[int]:
        """The experience rows sharing the most words with the query, best first, none with no word shared."""
        q = words(query)
        scored = [(len(q & w), j) for j, w in enumerate(self._words) if self.base.n + j not in exclude and q & w]
        return [self.base.n + j for _, j in sorted(scored, key=lambda x: -x[0])[:m]]

    def over(self, nearest, m: int = 5):
        """A first stage over store and experience together: the store's own candidates, then the
        experience rows that share most words with the query, for a reranker to order."""
        def search(queries, k, exclude):
            return [nearest([q], k, exclude)[0] + self.candidates(q, m, exclude) for q in queries]
        return search

    def beside(self, nearest, extra: int = 5):
        """Over the reranked search: the store's best rows as asked, and beside them the best experience
        row when it ranks above the last of them, never in place of one. In one slot (round 8, 2026-11-08)
        a wrong chain displaced the passage the asker needed: Wikipedia's gold shown fell from 1.3 a
        question to 0.69 and the day asked again lost 21 for 13."""
        def search(queries, k, exclude):
            out = []
            for q in queries:
                ranked = nearest([q], k + extra, exclude)[0]
                own = [r for r in ranked if r < self.base.n][:k]
                cut = ranked.index(own[-1]) if own else len(ranked)
                out.append(own + [r for r in ranked[:cut] if r >= self.base.n][:1])
            return out
        return search
