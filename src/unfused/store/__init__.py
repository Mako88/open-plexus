"""`store`: the lossless record of everything heard, searchable two ways.

A fragment is written once and never deleted; archiving is the strongest thing
that happens to a row, and a correction is a new fragment that cites the old
one. That is Persistence's rule.

Time here is a logical clock, the turn index, rather than wall-clock seconds. An
exam runs three hundred turns in minutes, and a recency weight computed from
seconds would treat all of them as equally recent. A turn index is also
something two nodes can agree on.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Literal

import numpy as np

Kind = Literal["episode", "fact", "summary"]


@dataclass(frozen=True)
class Fragment:
    """One immutable thing remembered.

    `id` is content-addressed over kind, text and provenance, so two nodes that
    saw the same turn write the same id and a fragment arriving twice is a
    no-op. The mutable columns (`recall_count`, `last_recalled_at`,
    `archived_at`) are bookkeeping about the fragment and stay out of the id.
    """

    id: str
    kind: Kind
    text: str
    created_at: float  # turn index
    provenance: str
    importance: float = 0.5
    embedding: np.ndarray | None = None
    recall_count: int = 0
    last_recalled_at: float | None = None
    archived_at: float | None = None
    cites: tuple[str, ...] = field(default=())


@dataclass(frozen=True)
class Hit:
    """A fragment a search returned. The two ranks are kept apart from the score
    so the fusion weights can be read off a reading rather than guessed."""

    fragment: Fragment
    score: float
    lexical_rank: int | None = None
    vector_rank: int | None = None


from .embed import Embedder, HashEmbedder, MiniLmEmbedder  # noqa: E402
from .sqlite import SqliteStore, fragment_id  # noqa: E402

__all__ = [
    "Embedder",
    "Fragment",
    "HashEmbedder",
    "Hit",
    "Kind",
    "MiniLmEmbedder",
    "SqliteStore",
    "fragment_id",
]
