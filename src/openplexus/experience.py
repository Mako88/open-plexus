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
