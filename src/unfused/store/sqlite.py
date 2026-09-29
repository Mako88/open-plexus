"""`SqliteStore`: fragments in one SQLite file, found by words and by meaning.

Two rankers, because they fail differently. FTS5's BM25 finds a fragment that
shares the query's words and misses one that says the same thing differently.
The embedding finds paraphrase and blurs names it has never seen, which is
every name in a generated house. Reciprocal rank fusion combines them from
their orders alone, since BM25 scores and cosines are on scales nobody has
calibrated against each other. Importance and recency then weight the fused
score. Every weight is a constructor argument and every reading records them.

Brute-force cosine in NumPy is milliseconds at a million rows, and the whole
store is one file a phone can carry. A vector index is a later swap.
"""

from __future__ import annotations

import hashlib
import json
import math
import sqlite3
from collections.abc import Iterable
from pathlib import Path

import numpy as np

from . import Fragment, Hit
from .embed import Embedder, HashEmbedder

SCHEMA = """
CREATE TABLE IF NOT EXISTS fragments (
    id               TEXT PRIMARY KEY,
    kind             TEXT NOT NULL,
    text             TEXT NOT NULL,
    created_at       REAL NOT NULL,
    provenance       TEXT NOT NULL,
    importance       REAL NOT NULL DEFAULT 0.5,
    embedding        BLOB,
    recall_count     INTEGER NOT NULL DEFAULT 0,
    last_recalled_at REAL,
    archived_at      REAL,
    cites            TEXT NOT NULL DEFAULT '[]'
);
CREATE VIRTUAL TABLE IF NOT EXISTS fragments_fts USING fts5(id UNINDEXED, text);
"""


def fragment_id(kind: str, text: str, provenance: str) -> str:
    digest = hashlib.sha256(f"{kind}\x00{text}\x00{provenance}".encode())
    return digest.hexdigest()[:32]


class SqliteStore:
    def __init__(
        self,
        path: Path | str,
        embedder: Embedder | None = None,
        rrf_k: int = 60,
        importance_weight: float = 0.25,
        recency_halflife: float = 100.0,
        recency_weight: float = 0.15,
    ) -> None:
        self.path = Path(path)
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.embedder = embedder or HashEmbedder()
        self.rrf_k = rrf_k
        self.importance_weight = importance_weight
        self.recency_halflife = recency_halflife
        self.recency_weight = recency_weight
        self.db = sqlite3.connect(str(self.path), check_same_thread=False)
        self.db.row_factory = sqlite3.Row
        self.db.executescript(SCHEMA)
        self.db.commit()
        self.now = 0.0
        self._ids: list[str] = []
        self._vectors = np.zeros((0, self.embedder.dims), dtype=np.float32)
        self._load_vectors()

    def dials(self) -> dict[str, object]:
        return {
            "rrf_k": self.rrf_k,
            "importance_weight": self.importance_weight,
            "recency_halflife": self.recency_halflife,
            "recency_weight": self.recency_weight,
            "embedder": getattr(self.embedder, "model_id", type(self.embedder).__name__),
        }

    def write(
        self,
        text: str,
        provenance: str,
        kind: str = "episode",
        importance: float = 0.5,
        cites: Iterable[str] = (),
    ) -> Fragment:
        """Write a fragment at the current turn. Writing the same one twice is a no-op."""
        fid = fragment_id(kind, text, provenance)
        embedding = self.embedder.encode([text])[0].astype(np.float32)
        cur = self.db.execute(
            "INSERT INTO fragments (id, kind, text, created_at, provenance, importance,"
            " embedding, cites) VALUES (?,?,?,?,?,?,?,?) ON CONFLICT(id) DO NOTHING",
            (fid, kind, text, self.now, provenance, importance, embedding.tobytes(),
             json.dumps(list(cites))),
        )
        if cur.rowcount:
            self.db.execute("INSERT INTO fragments_fts (id, text) VALUES (?,?)", (fid, text))
            self._ids.append(fid)
            self._vectors = np.vstack([self._vectors, embedding[None, :]])
        self.db.commit()
        fragment = self.get(fid)
        assert fragment is not None
        return fragment

    def get(self, fid: str) -> Fragment | None:
        row = self.db.execute("SELECT * FROM fragments WHERE id = ?", (fid,)).fetchone()
        return _to_fragment(row) if row else None

    def count(self) -> int:
        return int(self.db.execute("SELECT COUNT(*) FROM fragments").fetchone()[0])

    def search(self, query: str, k: int = 5, kinds: Iterable[str] | None = None) -> list[Hit]:
        if not query.strip():
            return []
        kinds = tuple(kinds) if kinds else None
        lexical = self._lexical(query, k * 4, kinds)
        vector = self._vector(query, k * 4, kinds)
        return self._fuse(lexical, vector, k)

    def _lexical(self, query: str, n: int, kinds: tuple[str, ...] | None) -> list[str]:
        words = [w for w in "".join(c if c.isalnum() else " " for c in query).split()
                 if len(w) > 1]
        if not words:
            return []
        sql = ("SELECT f.id FROM fragments_fts JOIN fragments f ON f.id = fragments_fts.id"
               " WHERE fragments_fts MATCH ? AND f.archived_at IS NULL")
        args: list = [" OR ".join(f'"{w}"' for w in words)]
        if kinds:
            sql += f" AND f.kind IN ({','.join('?' * len(kinds))})"
            args += list(kinds)
        sql += " ORDER BY bm25(fragments_fts) LIMIT ?"
        args.append(n)
        return [r["id"] for r in self.db.execute(sql, args).fetchall()]

    def _vector(self, query: str, n: int, kinds: tuple[str, ...] | None) -> list[str]:
        if not self._ids:
            return []
        q = self.embedder.encode([query])[0]
        order = np.argsort(-(self._vectors @ q))
        out: list[str] = []
        for i in order:
            fragment = self.get(self._ids[int(i)])
            if fragment is None or fragment.archived_at is not None:
                continue
            if kinds and fragment.kind not in kinds:
                continue
            out.append(fragment.id)
            if len(out) >= n:
                break
        return out

    def _fuse(self, lexical: list[str], vector: list[str], k: int) -> list[Hit]:
        scores: dict[str, float] = {}
        for rank, fid in enumerate(lexical):
            scores[fid] = scores.get(fid, 0.0) + 1.0 / (self.rrf_k + rank)
        for rank, fid in enumerate(vector):
            scores[fid] = scores.get(fid, 0.0) + 1.0 / (self.rrf_k + rank)
        lex_rank = {fid: r for r, fid in enumerate(lexical)}
        vec_rank = {fid: r for r, fid in enumerate(vector)}
        hits: list[Hit] = []
        for fid, base in scores.items():
            fragment = self.get(fid)
            if fragment is None:
                continue
            age = max(0.0, self.now - fragment.created_at)
            recency = math.exp(-age * math.log(2) / self.recency_halflife)
            score = (base * (1.0 + self.importance_weight * fragment.importance)
                     * (1.0 + self.recency_weight * recency))
            hits.append(Hit(fragment, score, lex_rank.get(fid), vec_rank.get(fid)))
        hits.sort(key=lambda h: -h.score)
        return hits[:k]

    def mark_recalled(self, ids: Iterable[str]) -> None:
        """`recall_count` only rises, so two nodes' counts merge by taking the maximum."""
        self.db.executemany(
            "UPDATE fragments SET recall_count = recall_count + 1, last_recalled_at = ?"
            " WHERE id = ?",
            [(self.now, i) for i in ids],
        )
        self.db.commit()

    def archive(self, ids: Iterable[str]) -> None:
        """Excluded from search and still there to be read. There is no delete."""
        self.db.executemany(
            "UPDATE fragments SET archived_at = ? WHERE id = ?", [(self.now, i) for i in ids]
        )
        self.db.commit()

    def _load_vectors(self) -> None:
        rows = self.db.execute("SELECT id, embedding FROM fragments ORDER BY rowid").fetchall()
        self._ids = [r["id"] for r in rows]
        if rows:
            self._vectors = np.vstack(
                [np.frombuffer(r["embedding"], dtype=np.float32) for r in rows]
            )
            self.now = float(
                self.db.execute("SELECT MAX(created_at) FROM fragments").fetchone()[0]
            )

    def close(self) -> None:
        self.db.close()


def _to_fragment(row: sqlite3.Row) -> Fragment:
    return Fragment(
        id=row["id"],
        kind=row["kind"],
        text=row["text"],
        created_at=row["created_at"],
        provenance=row["provenance"],
        importance=row["importance"],
        embedding=np.frombuffer(row["embedding"], dtype=np.float32),
        recall_count=row["recall_count"],
        last_recalled_at=row["last_recalled_at"],
        archived_at=row["archived_at"],
        cites=tuple(json.loads(row["cites"])),
    )
