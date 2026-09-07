"""`SqliteStore`: the slow memory. Every turn written, nothing ever deleted.

THE RULE THAT SHAPES THE WHOLE FILE: NOTHING IS EVER DELETED. `archived_at` is
the strongest thing that happens to a row, and a correction is a NEW fragment
that cites the old one. That is Persistence's rule, inherited on purpose, and it
is what makes the store the lossless record against which the state's forgetting
can be measured. Forgetting in this branch is what falls out of the STATE and out
of the HOT TIER; it is never an erase.

There is no `delete` method. Adding one is not a refactor, it is a change of
design, and `tests/guards/test_store.py` fails if one appears.

WHY SQLITE AND NOT A VECTOR DATABASE. A million rows of brute-force cosine in
NumPy is milliseconds, FTS5 is already in the standard library's SQLite, and the
whole thing is one file that a Phase 5 node can carry on a phone. A vector
extension is a later swap behind the same protocol, and buying one now would be
buying an operational dependency to solve a problem nobody has measured.

HYBRID RANKING IS RECIPROCAL RANK FUSION, then re-weighted by importance and
recency. RRF because the two rankers produce incomparable scores -- FTS5 gives
BM25, cosine gives a similarity -- and normalising them against each other means
choosing a scale nobody has calibrated. RRF only needs the ORDER from each,
which is the part both agree on the meaning of. The weights are an arm, not a
decision, so they are constructor arguments and every reading records them.
"""

from __future__ import annotations

import hashlib
import json
import math
import sqlite3
import time
from collections.abc import Iterable
from pathlib import Path

import numpy as np

from . import Fragment, Hit, Kind, ReplaySpec
from .embed import Embedder, HashEmbedder

SCHEMA = """
CREATE TABLE IF NOT EXISTS fragments (
    id            TEXT PRIMARY KEY,
    kind          TEXT NOT NULL,
    text          TEXT NOT NULL,
    created_at    REAL NOT NULL,
    provenance    TEXT NOT NULL,
    node_id       TEXT NOT NULL,
    importance    REAL NOT NULL DEFAULT 0.5,
    confidence    REAL NOT NULL DEFAULT 1.0,
    embedding     BLOB,
    last_recalled_at REAL,
    recall_count  INTEGER NOT NULL DEFAULT 0,
    consolidated_at  REAL,
    archived_at   REAL,
    cites         TEXT NOT NULL DEFAULT '[]'
);
CREATE INDEX IF NOT EXISTS fragments_kind ON fragments(kind);
CREATE INDEX IF NOT EXISTS fragments_created ON fragments(created_at);
CREATE INDEX IF NOT EXISTS fragments_consolidated ON fragments(consolidated_at);

-- FTS5 as its own table rather than an external-content one. External content
-- keeps the index in sync through triggers on DELETE and UPDATE, and this store
-- does neither -- so the triggers would be dead code guarding a case that
-- cannot arise, and the simple version is one insert per write.
CREATE VIRTUAL TABLE IF NOT EXISTS fragments_fts USING fts5(id UNINDEXED, text);
"""


def fragment_id(kind: str, text: str, provenance: str) -> str:
    """Content-addressed, and NOT over the node that wrote it.

    THIS IS WHAT MAKES GOSSIP IDEMPOTENT IN PHASE 5. Two nodes that saw the same
    turn must write the same id, or a fragment arriving from a peer is a
    duplicate rather than a no-op. So `node_id` is deliberately excluded: it
    records who happened to store a thing, not what the thing is. `provenance`
    identifies the turn -- thread and index -- which two nodes CAN agree on.
    """
    digest = hashlib.sha256(f"{kind}\x00{text}\x00{provenance}".encode())
    return digest.hexdigest()[:32]


def episode(
    text: str,
    provenance: str,
    node_id: str = "local",
    importance: float = 0.5,
    **kw,
) -> Fragment:
    """One turn, in or out. The lossless record, written before anything else."""
    return Fragment(
        id=fragment_id("episode", text, provenance),
        kind="episode",
        text=text,
        created_at=kw.pop("created_at", time.time()),
        provenance=provenance,
        node_id=node_id,
        importance=importance,
        **kw,
    )


class SqliteStore:
    """The `Store` protocol over one SQLite file.

    `k`, the hybrid weights and the hot-tier floor are DIALS. They are arguments
    here rather than constants so an exam can sweep them, and every reading
    records what they were.
    """

    def __init__(
        self,
        path: Path | str = "state/store.db",
        embedder: Embedder | None = None,
        node_id: str = "local",
        rrf_k: int = 60,
        importance_weight: float = 0.25,
        recency_halflife_s: float = 7 * 24 * 3600.0,
        recency_weight: float = 0.15,
    ) -> None:
        self.path = Path(path)
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.node_id = node_id
        self.embedder = embedder or HashEmbedder()
        self.rrf_k = rrf_k
        self.importance_weight = importance_weight
        self.recency_halflife_s = recency_halflife_s
        self.recency_weight = recency_weight

        self.db = sqlite3.connect(str(self.path), check_same_thread=False)
        self.db.row_factory = sqlite3.Row
        self.db.executescript(SCHEMA)
        self.db.commit()

        # Held in memory for brute-force cosine. A million 384-dim fp32 vectors
        # is 1.5 GB, which is the point at which this stops being fine and a
        # vector extension becomes the swap the protocol was written for.
        self._ids: list[str] = []
        self._vectors: np.ndarray | None = None
        self._load_vectors()

    # -- writing -----------------------------------------------------------

    def write(self, fragment: Fragment) -> Fragment:
        """Write a fragment. Writing the same one twice is a no-op, not an error.

        IDEMPOTENT BECAUSE THE ID IS THE CONTENT. Phase 5's gossip will offer the
        same fragment from several peers, and a store that raised or duplicated
        on the second copy would need coordination to avoid -- which is the thing
        constraint C1 says it cannot have.
        """
        embedding = fragment.embedding
        if embedding is None:
            embedding = self.embedder.encode([fragment.text])[0]

        cur = self.db.execute(
            """
            INSERT INTO fragments
                (id, kind, text, created_at, provenance, node_id, importance,
                 confidence, embedding, last_recalled_at, recall_count,
                 consolidated_at, archived_at, cites)
            VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)
            ON CONFLICT(id) DO NOTHING
            """,
            (
                fragment.id,
                fragment.kind,
                fragment.text,
                fragment.created_at,
                fragment.provenance,
                fragment.node_id or self.node_id,
                fragment.importance,
                fragment.confidence,
                np.asarray(embedding, dtype=np.float32).tobytes(),
                fragment.last_recalled_at,
                fragment.recall_count,
                fragment.consolidated_at,
                fragment.archived_at,
                json.dumps(list(fragment.cites)),
            ),
        )
        if cur.rowcount:
            self.db.execute(
                "INSERT INTO fragments_fts (id, text) VALUES (?,?)",
                (fragment.id, fragment.text),
            )
            self._append_vector(fragment.id, np.asarray(embedding, dtype=np.float32))
        self.db.commit()
        return fragment

    def write_turn(
        self, text: str, provenance: str, importance: float = 0.5
    ) -> Fragment:
        """The Phase 2 rule: EVERY TURN IS WRITTEN, in and out, before anything else."""
        return self.write(episode(text, provenance, self.node_id, importance))

    # -- reading -----------------------------------------------------------

    def get(self, fragment_id_: str) -> Fragment | None:
        row = self.db.execute(
            "SELECT * FROM fragments WHERE id = ?", (fragment_id_,)
        ).fetchone()
        return _to_fragment(row) if row else None

    def search(
        self,
        query_text: str,
        k: int = 5,
        deadline: float | None = None,
        include_archived: bool = False,
    ) -> list[Hit]:
        """Hybrid search. Returns what it found by `deadline`, never raises on time.

        `deadline` is an absolute `time.monotonic()` value, not a duration.
        Phase 5's constraint C3 is that a cluster vanishing mid-thought is
        normal, so a search that runs out of time returns FEWER MEMORIES rather
        than an error -- a store whose failure mode is an exception cannot be
        sharded later without every caller changing. Here, with everything local,
        it can only really fire on a very large table; it exists so the contract
        is the same one `FanoutStore` will have to keep.
        """
        if not query_text.strip():
            return []

        lexical = self._lexical(query_text, k * 4, include_archived)
        if deadline is not None and time.monotonic() > deadline:
            return self._fuse(lexical, [], k)
        vector = self._vector(query_text, k * 4, include_archived)
        return self._fuse(lexical, vector, k)

    def _lexical(self, query_text: str, n: int, include_archived: bool) -> list[str]:
        # FTS5 MATCH takes a query syntax, and a user's sentence is not one --
        # an apostrophe or a bare `AND` raises. The words are extracted and
        # OR-ed, which is what "find rows sharing words with this" means.
        words = [w for w in "".join(
            c if c.isalnum() else " " for c in query_text
        ).split() if len(w) > 1]
        if not words:
            return []
        match = " OR ".join(words)
        sql = (
            "SELECT f.id FROM fragments_fts JOIN fragments f ON f.id = fragments_fts.id "
            "WHERE fragments_fts MATCH ? "
        )
        if not include_archived:
            sql += "AND f.archived_at IS NULL "
        sql += "ORDER BY bm25(fragments_fts) LIMIT ?"
        try:
            rows = self.db.execute(sql, (match, n)).fetchall()
        except sqlite3.OperationalError:
            return []
        return [r["id"] for r in rows]

    def _vector(self, query_text: str, n: int, include_archived: bool) -> list[str]:
        if self._vectors is None or not len(self._ids):
            return []
        q = self.embedder.encode([query_text])[0]
        sims = self._vectors @ q
        order = np.argsort(-sims)[: n * 2]
        out = []
        for i in order:
            fid = self._ids[int(i)]
            if not include_archived and self._archived(fid):
                continue
            out.append(fid)
            if len(out) >= n:
                break
        return out

    def _fuse(self, lexical: list[str], vector: list[str], k: int) -> list[Hit]:
        """Reciprocal rank fusion, then importance and recency.

        RRF USES ONLY THE ORDER FROM EACH RANKER, which is the whole reason it is
        here: BM25 and cosine produce numbers on incomparable scales, and
        normalising one against the other means choosing a scale nobody has
        calibrated. `1 / (k + rank)` needs no such choice.
        """
        scores: dict[str, float] = {}
        lex_rank: dict[str, int] = {}
        vec_rank: dict[str, int] = {}

        for rank, fid in enumerate(lexical):
            lex_rank[fid] = rank
            scores[fid] = scores.get(fid, 0.0) + 1.0 / (self.rrf_k + rank)
        for rank, fid in enumerate(vector):
            vec_rank[fid] = rank
            scores[fid] = scores.get(fid, 0.0) + 1.0 / (self.rrf_k + rank)

        if not scores:
            return []

        now = time.time()
        hits: list[Hit] = []
        for fid, base in scores.items():
            fragment = self.get(fid)
            if fragment is None:
                continue
            age = max(0.0, now - fragment.created_at)
            recency = math.exp(-age * math.log(2) / self.recency_halflife_s)
            score = (
                base
                * (1.0 + self.importance_weight * fragment.importance)
                * (1.0 + self.recency_weight * recency)
            )
            hits.append(
                Hit(
                    fragment=fragment,
                    score=score,
                    lexical_rank=lex_rank.get(fid),
                    vector_rank=vec_rank.get(fid),
                )
            )
        hits.sort(key=lambda h: -h.score)
        return hits[:k]

    # -- bookkeeping -------------------------------------------------------

    def mark_recalled(self, ids: Iterable[str]) -> None:
        """`recall_count` is a G-COUNTER: it only ever increases.

        That is what lets two nodes count the same recall and merge by taking
        the maximum per replica, without coordinating. A counter that could go
        down would need consensus, which constraint C1 forbids.
        """
        now = time.time()
        self.db.executemany(
            "UPDATE fragments SET recall_count = recall_count + 1, "
            "last_recalled_at = ? WHERE id = ?",
            [(now, i) for i in ids],
        )
        self.db.commit()

    def mark_consolidated(self, ids: Iterable[str], at: float | None = None) -> None:
        """What moves a fragment out of the hot tier: the weights now answer it."""
        at = at or time.time()
        self.db.executemany(
            "UPDATE fragments SET consolidated_at = ? WHERE id = ?",
            [(at, i) for i in ids],
        )
        self.db.commit()

    def archive(self, ids: Iterable[str], at: float | None = None) -> None:
        """THE STRONGEST THING THAT HAPPENS TO A ROW. Not a delete.

        An archived fragment is excluded from the default search and is still
        there to be read, cited and replayed. There is no `delete` and adding one
        is a change of design rather than a refactor.
        """
        at = at or time.time()
        self.db.executemany(
            "UPDATE fragments SET archived_at = ? WHERE id = ?", [(at, i) for i in ids]
        )
        self.db.commit()

    def tiers(self) -> dict[str, int]:
        """HOT is what the weights do not answer yet. COLD is what they do.

        Phase 4 gives this a floor and makes retrieval prefer hot; for now it is
        the count, which is what a reading needs to watch the store's shape move
        over a day of idle cycles.
        """
        row = self.db.execute(
            """
            SELECT
              COUNT(*) AS total,
              SUM(CASE WHEN consolidated_at IS NULL AND archived_at IS NULL
                       THEN 1 ELSE 0 END) AS hot,
              SUM(CASE WHEN consolidated_at IS NOT NULL AND archived_at IS NULL
                       THEN 1 ELSE 0 END) AS cold,
              SUM(CASE WHEN archived_at IS NOT NULL THEN 1 ELSE 0 END) AS archived
            FROM fragments
            """
        ).fetchone()
        return {
            "total": row["total"] or 0,
            "hot": row["hot"] or 0,
            "cold": row["cold"] or 0,
            "archived": row["archived"] or 0,
        }

    def sample_for_replay(self, spec: ReplaySpec) -> list[Fragment]:
        """The replay mix, per the doc: recent, rehearsal, and general.

        REHEARSAL PREFERS LOW `recall_count` ON PURPOSE. A fragment that keeps
        being retrieved is already being served by the store; replay is for what
        the store is NOT reaching, which is the part consolidation has to carry.

        The general slice is not this store's to give -- it is a fixed local
        corpus of text the house did not produce, and it is Phase 3's to load.
        `spec.general` is honoured by returning fewer rows, so the caller can
        see the gap rather than silently getting house text in its place.
        """
        rng = np.random.default_rng(spec.seed)
        n_recent = int(spec.n * spec.recent)
        n_rehearsal = int(spec.n * spec.rehearsal)

        recent = [
            _to_fragment(r)
            for r in self.db.execute(
                "SELECT * FROM fragments WHERE archived_at IS NULL "
                "ORDER BY created_at DESC LIMIT ?",
                (n_recent,),
            )
        ]

        pool = [
            _to_fragment(r)
            for r in self.db.execute(
                "SELECT * FROM fragments WHERE archived_at IS NULL "
                "AND id NOT IN (SELECT id FROM fragments ORDER BY created_at DESC LIMIT ?) "
                "ORDER BY created_at DESC LIMIT ?",
                (n_recent, n_rehearsal * 8 if n_rehearsal else 0),
            )
        ]
        rehearsal: list[Fragment] = []
        if pool and n_rehearsal:
            weights = np.array(
                [f.importance / (1.0 + f.recall_count) for f in pool], dtype=np.float64
            )
            if weights.sum() <= 0:
                weights = np.ones(len(pool))
            weights = weights / weights.sum()
            take = min(n_rehearsal, len(pool))
            picked = rng.choice(len(pool), size=take, replace=False, p=weights)
            rehearsal = [pool[int(i)] for i in picked]

        return recent + rehearsal

    # -- vectors -----------------------------------------------------------

    def _load_vectors(self) -> None:
        rows = self.db.execute(
            "SELECT id, embedding FROM fragments WHERE embedding IS NOT NULL "
            "ORDER BY rowid"
        ).fetchall()
        self._ids = [r["id"] for r in rows]
        if rows:
            self._vectors = np.stack(
                [np.frombuffer(r["embedding"], dtype=np.float32) for r in rows]
            )
        else:
            self._vectors = None

    def _append_vector(self, fid: str, vector: np.ndarray) -> None:
        self._ids.append(fid)
        row = vector.reshape(1, -1)
        self._vectors = row if self._vectors is None else np.vstack([self._vectors, row])

    def _archived(self, fid: str) -> bool:
        row = self.db.execute(
            "SELECT archived_at FROM fragments WHERE id = ?", (fid,)
        ).fetchone()
        return bool(row and row["archived_at"] is not None)

    def close(self) -> None:
        self.db.close()


def _to_fragment(row: sqlite3.Row) -> Fragment:
    return Fragment(
        id=row["id"],
        kind=row["kind"],
        text=row["text"],
        created_at=row["created_at"],
        provenance=row["provenance"],
        node_id=row["node_id"],
        importance=row["importance"],
        confidence=row["confidence"],
        embedding=(
            np.frombuffer(row["embedding"], dtype=np.float32)
            if row["embedding"] is not None
            else None
        ),
        last_recalled_at=row["last_recalled_at"],
        recall_count=row["recall_count"],
        consolidated_at=row["consolidated_at"],
        archived_at=row["archived_at"],
        cites=tuple(json.loads(row["cites"])),
    )


__all__ = ["Kind", "SqliteStore", "episode", "fragment_id"]
