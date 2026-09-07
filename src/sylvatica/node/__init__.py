"""`node` -- the fleet. PROTOCOL ONLY, and the doc says it stays that way until Phase 5.

COMPLAINT 7 IS SERVED BY WHERE THE STORE LIVES, NOT BY SPLITTING A FORWARD PASS.
That is a DECIDED item and this file exists to hold the line on it. A dense
network needs all-to-all traffic at every layer, so internet latency multiplies
by depth; Petals and hivemind measured seconds a token. Retrieval, replay and
consolidation tolerate latency. So those are what go on the weak machines, and
per-token compute does not distribute. A session that finds itself designing a
sharded forward pass has reopened a decision, which is John's conversation.

CONSTRAINTS C1 TO C4, CARRIED UNCHANGED FROM THE EARLIER BRANCHES:

  C1  no node reads another's data directly;
  C2  messages are late, jittered, and out of order;
  C3  a cluster vanishing mid-thought is normal;
  C4  no episode boundary -- nothing may depend on train-then-test.

C3 is why `Store.search` takes a deadline instead of raising, and C4 is why
consolidation is a scheduled cycle rather than a phase. Both of those shapes are
already in the code above this file, which is the point of writing the protocol
this early: the constraints reach back into Phase 2's design instead of arriving
after it is built.

TWENTY PHONES ARE BOUGHT WHEN, AND ONLY WHEN, the one-box fleet has shown that a
store shard on a weak node is worth having. The footprint row per node is what
decides that, and it is a reading.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol

from ..store import Fragment, Hit


@dataclass(frozen=True)
class Footprint:
    """What one node costs to run. The row that decides whether phones get bought."""

    node_id: str
    fragments: int
    bytes_on_disk: int
    rss_bytes: int
    search_p50_ms: float
    search_p99_ms: float


class Node(Protocol):
    """A store shard, a consolidation worker, and optionally a core.

    Nodes exchange fragments by GOSSIP rather than by a coordinator. A fragment
    id is content-addressed, so the same fragment arriving from two peers is one
    write; `recall_count` is a G-counter, so two nodes counting the same recall
    merge by taking the max per replica rather than by agreeing.
    """

    @property
    def node_id(self) -> str: ...

    def offer(self, fragments: list[Fragment]) -> None: ...

    def search_shard(self, query_text: str, k: int, deadline: float) -> list[Hit]: ...

    def footprint(self) -> Footprint: ...


class FanoutStore(Protocol):
    """A `Store` implemented over many nodes.

    A MISSING NODE IS MISSING MEMORIES RATHER THAN AN ERROR. `search` fans out
    with a deadline and returns what arrived. The Phase 5 refutation is Tier B
    with three of eight shards down falling to Tier A -- at that point the store
    did not survive its own constraints, and the fleet is the wrong shape.
    """

    def search(self, query_text: str, k: int, deadline: float) -> list[Hit]: ...

    def reachable(self) -> list[str]: ...


__all__ = ["FanoutStore", "Footprint", "Node"]
