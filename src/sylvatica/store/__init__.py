"""`store` -- the slow memory. PROTOCOL ONLY; `SqliteStore` is Phase 2.

WHAT IS HERE AND WHY IT IS HERE NOW. The design doc's shape says every part has
a protocol at its top and no part reaches into another's internals. Writing the
protocol in Phase 0 costs nothing and settles the question a Phase 2 session
would otherwise answer under time pressure: what does the rest of the system get
to assume about a store?

WHAT IS DELIBERATELY ABSENT. `SqliteStore`, FTS5, the embeddings, the hybrid
rank. `tests/outstanding/test_the_order.py` is red until they land. Nothing here
should grow an implementation; a store that appears before its exam has one is a
store nobody can say anything about.

THE ONE RULE THAT IS NOT NEGOTIABLE: nothing is ever deleted. `archived_at` is
the strongest thing that happens to a row. Forgetting in this branch is what
falls out of the core's STATE and out of the store's hot tier -- it is never an
erase. That is Persistence's rule, inherited on purpose, and it is what makes a
correction auditable: a correction is a NEW fragment that cites the old one.
"""

from __future__ import annotations

from collections.abc import Iterable
from dataclasses import dataclass, field
from typing import Literal, Protocol, runtime_checkable

import numpy as np

Kind = Literal["episode", "summary", "fact", "identity"]


@dataclass(frozen=True)
class Fragment:
    """One immutable thing the system remembers.

    `id` is CONTENT-ADDRESSED, which is what makes gossip between nodes
    idempotent in Phase 5: two nodes that saw the same turn write the same id,
    so a fragment arriving twice is a no-op rather than a duplicate.

    Immutable once written. The mutable columns -- `last_recalled_at`,
    `recall_count`, `consolidated_at`, `archived_at` -- are the store's
    bookkeeping about a fragment and not part of its identity, which is why they
    do not enter the id. `recall_count` is a G-counter so it merges across nodes
    without coordination.
    """

    id: str
    kind: Kind
    text: str
    created_at: float
    provenance: str  # who said it, which turn, which node
    node_id: str
    importance: float = 0.5
    confidence: float = 1.0
    embedding: np.ndarray | None = None
    last_recalled_at: float | None = None
    recall_count: int = 0
    consolidated_at: float | None = None
    archived_at: float | None = None
    cites: tuple[str, ...] = field(default=())  # ids this fragment corrects or summarises


@dataclass(frozen=True)
class Hit:
    """A fragment a search returned, and why it ranked where it did.

    `lexical` and `vector` are kept SEPARATELY from `score` on purpose. The
    hybrid rank weights are an arm, not a decision, and an arm cannot be tuned
    off a number that has already had the weights folded into it.
    """

    fragment: Fragment
    score: float
    lexical_rank: int | None = None
    vector_rank: int | None = None


@dataclass(frozen=True)
class ReplaySpec:
    """The replay mix, which is a DIAL and is read on an exam.

    Three shares that sum to one: recent episodes, a rehearsal draw from older
    fragments weighted by importance and LOW recall count, and a slice of
    general text so the adapter stays anchored to language it did not learn in
    this house. The rehearsal weighting prefers low `recall_count` because a
    fragment that keeps being retrieved is already being served by the store;
    replay is for what the store is not reaching.
    """

    n: int = 256
    recent: float = 0.4
    rehearsal: float = 0.4
    general: float = 0.2
    seed: int | None = None


@runtime_checkable
class Store(Protocol):
    """The slow memory, as everything above it is allowed to see it.

    `search` takes a DEADLINE and not a timeout-or-error. Phase 5's constraint
    C3 is that a cluster vanishing mid-thought is normal, so a search that
    cannot reach a shard returns fewer memories rather than raising. A store
    whose failure mode is an exception cannot be sharded later without every
    caller changing.
    """

    def write(self, fragment: Fragment) -> None: ...

    def search(self, query_text: str, k: int, deadline: float) -> list[Hit]: ...

    def mark_recalled(self, ids: Iterable[str]) -> None: ...

    def sample_for_replay(self, spec: ReplaySpec) -> list[Fragment]: ...

    def tiers(self) -> dict[str, int]: ...


__all__ = ["Fragment", "Hit", "Kind", "ReplaySpec", "Store"]
