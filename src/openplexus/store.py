"""The store: a table on disk of what the system has read or been told.

A row is a sentence as heard, with the time it was heard (its position in the table), the
episode it was heard in (the conversation; here, one bAbI story), and one vector per embedder.
Learning is appending a row. Nothing is ever retrained or deleted.

On disk a store is a directory: `rows.jsonl` holds the rows in time order, and each embedder
has a `vectors-<name>.f32` of raw float32 rows appended in the same order, with its dimension
in `meta.json`. Everything is appended as it is heard, so a process that dies mid-way loses
nothing already written, and opening the directory again gives back the same store. Search
is brute force in numpy over L2-normalised vectors, so a score is a cosine; an approximate
index comes only when this is the bottleneck.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np


@dataclass(frozen=True)
class Row:
    id: int  # also its time: rows are numbered in the order they were heard
    episode: str
    text: str


@dataclass(frozen=True)
class Hit:
    row: Row
    score: float


class Store:
    def __init__(self, path: str | Path):
        self.path = Path(path)
        self.path.mkdir(parents=True, exist_ok=True)
        self._rows_file = self.path / "rows.jsonl"
        self._meta_file = self.path / "meta.json"
        self.rows: list[Row] = []
        self.episodes: np.ndarray = np.zeros(0, dtype=object)
        self.dims: dict[str, int] = {}
        self._vectors: dict[str, np.ndarray] = {}
        self._load()

    def _load(self) -> None:
        if self._rows_file.exists():
            with open(self._rows_file, encoding="utf-8") as f:
                self.rows = [Row(**json.loads(line)) for line in f if line.strip()]
        self.episodes = np.array([r.episode for r in self.rows], dtype=object)
        if self._meta_file.exists():
            self.dims = json.loads(self._meta_file.read_text(encoding="utf-8"))["dims"]
        for name, dim in self.dims.items():
            raw = np.fromfile(self._vector_file(name), dtype=np.float32)
            self._vectors[name] = raw.reshape(-1, dim)
            if len(self._vectors[name]) != len(self.rows):
                raise ValueError(f"{name}: {len(self._vectors[name])} vectors for "
                                 f"{len(self.rows)} rows")

    def _vector_file(self, name: str) -> Path:
        return self.path / f"vectors-{name}.f32"

    def __len__(self) -> int:
        return len(self.rows)

    @property
    def embedders(self) -> list[str]:
        return list(self.dims)

    def bytes_on_disk(self) -> int:
        files = [self._rows_file, self._meta_file] + [self._vector_file(n) for n in self.dims]
        return sum(p.stat().st_size for p in files if p.exists())

    def append(self, texts: list[str], episode: str, vectors: dict[str, np.ndarray]) -> list[int]:
        """Hear `texts` in one episode. `vectors` maps an embedder's name to a (len(texts), dim)
        array; every embedder the store already knows must be present, so the table stays
        rectangular. Returns the new rows' ids."""
        if set(vectors) != set(self.dims) and self.dims:
            raise ValueError(f"store has {sorted(self.dims)}, got {sorted(vectors)}")
        for name, v in vectors.items():
            v = np.asarray(v, dtype=np.float32)
            if v.shape != (len(texts), v.shape[1]):
                raise ValueError(f"{name}: {v.shape} for {len(texts)} texts")
            if name not in self.dims:
                self.dims[name] = int(v.shape[1])
                self._vectors[name] = np.zeros((0, v.shape[1]), dtype=np.float32)
            norms = np.linalg.norm(v, axis=1, keepdims=True)
            v = v / np.maximum(norms, 1e-12)
            with open(self._vector_file(name), "ab") as f:
                f.write(v.tobytes())
            self._vectors[name] = np.concatenate([self._vectors[name], v])
        first = len(self.rows)
        new = [Row(id=first + i, episode=episode, text=t) for i, t in enumerate(texts)]
        with open(self._rows_file, "a", encoding="utf-8") as f:
            for r in new:
                f.write(json.dumps(r.__dict__) + "\n")
        self.rows.extend(new)
        self.episodes = np.array([r.episode for r in self.rows], dtype=object)
        self._meta_file.write_text(json.dumps({"dims": self.dims}), encoding="utf-8")
        return [r.id for r in new]

    def search(self, name: str, query: np.ndarray, k: int, episode: str | None = None,
               before: int | None = None) -> list[Hit]:
        """Top-k rows by cosine to `query` under embedder `name`. `episode` restricts to one
        conversation; `before` restricts to rows heard before that time (id), which is how a
        question is asked as of the moment it was asked. Ties are broken by time, earliest
        first, so the result is a function of the store alone."""
        vectors = self._vectors[name]
        n = len(self.rows) if before is None else min(before, len(self.rows))
        mask = np.ones(n, dtype=bool)
        if episode is not None:
            mask &= self.episodes[:n] == episode
        ids = np.nonzero(mask)[0]
        if len(ids) == 0:
            return []
        q = np.asarray(query, dtype=np.float32).reshape(-1)
        q = q / max(float(np.linalg.norm(q)), 1e-12)
        scores = vectors[ids] @ q
        order = np.lexsort((ids, -scores))[:k]
        return [Hit(self.rows[int(ids[i])], float(scores[i])) for i in order]

    def in_episode(self, episode: str, before: int | None = None) -> list[Row]:
        n = len(self.rows) if before is None else min(before, len(self.rows))
        return [r for r in self.rows[:n] if r.episode == episode]
