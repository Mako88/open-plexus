"""The co-occurrence table actually split across owners, with every message counted.

`grounding.CoOccurrence` holds one table for every surface in the world, and
`buckets.Join` writes into it. That makes the *bucket* half of
[`time-bucket-join.md`](../docs/options/time-bucket-join.md) real — a bucket's
owner is decided by `Ring` and nobody is asked — while leaving the other half
**asserted**: *"the link is written to `owner(surface)`, where it accumulates
over that percept's lifetime"* was true of the design and not of the code.

**One object holding every row cannot demonstrate that the rows are separable**,
however true it is that they are. This module is that demonstration:

- every surface's row lives in exactly one node's table, chosen by the same
  `Ring`, and `holdings` is checkable from outside — a node holding a row it does
  not own is a test failure rather than an argument;
- every write and every read that crosses a node boundary is **counted**, so the
  locality claim has a number instead of a paragraph.

## What this makes visible, which was the point of building it

`g33-01` established that `conditional` is the deployable statistic and `ppmi` is
not, because PPMI divides by a total no node can know. Here that stops being a
remark and becomes a shape: `rank` needs `count(y)` for **every candidate
partner**, each from `owner(y)`, and `remote_reads` counts them. Whether that
number is affordable is a measurement nobody has taken.

**`ppmi` is deliberately unavailable.** Offering it would mean either a global
gather or a per-node total that silently means something different on each node,
and both are worse than not offering it. `local_conditional` is available and
`g33-01` measured it failing, so the cost of the remote read is a choice with a
known price rather than an assumption.

## What this does NOT duplicate, and what was searched

Searched by capability — partition, shard, owner, route, federated, per-node —
across `openplexus/`, `tools/`, `tests/`, `testbed/` and `experiments/`.

- **`openplexus/partitioned.py` (`ConceptStore`) is the same IDEA on a different
  object and is not reused.** It splits a **superposed `d x d` matrix** by
  concept, so its per-node state is a dense array and its read is `M @ key`. This
  splits a **sparse count table**, whose read is a ranking over a row. A key
  vector cannot be inverted to an id, which is exactly the coupling that record
  names — here the id is all there is. Sharing a class would mean one type
  holding either a matrix or a dict.
- **`openplexus/ownership.py` (`Ring`) IS used, not reimplemented**, and it is the
  same ring `buckets.Join` uses, so a bucket owner and a surface owner are chosen
  by one rule rather than two that could drift.
- **`openplexus/grounding.py` (`CoOccurrence`) IS used, not reimplemented** — one
  instance per node. `observed_with` exists because a sharded owner may write
  only its own direction of a pair.
- **`openplexus/peer.py`** is point-to-point reads over real sockets against the
  superposed store. This counts messages rather than sending them; it is the
  arithmetic that has to be right before a socket is worth wrapping around it.
- **`openplexus/distributed.py`** is the driver-based dimension split, which is
  the arrangement `DECISIONS.md` §9 records as the C1 violation this avoids.

## What is still NOT here

**No sockets and no processes.** Every node's table is an object in one address
space; what is demonstrated is that nothing *reads across* them except through a
counted call. That is strictly weaker than separate processes and strictly
stronger than one shared table, and `testbed/run.py` is where the last step goes.
"""

from __future__ import annotations

from openplexus.grounding import CoOccurrence, Statistic
from openplexus.ownership import Ring


class _AtOwner:
    """What one node can see: its own rows, plus what it asks peers for.

    A statistic is written against `CoOccurrence` and reads three things —
    `together`, `seen` and `occasions`. Handing it the local table directly is
    wrong in a way that is silent: `seen(other)` is **another node's marginal**
    and comes back 0, so every chance-corrected score collapses to zero and the
    walk returns every surface alone. That happened, and it looked like the
    statistic failing rather than the data being absent.

    So this is the local table with the boundary made explicit:

        together    local, always -- the row belongs to this node
        seen        local if owned, otherwise a COUNTED remote read
        occasions   REFUSED, because no node knows it

    The refusal is the point. `ppmi` divides by `occasions`, so asking for it
    here raises instead of quietly using one node's share as if it were the
    world's — which would produce a plausible number that means nothing.
    """

    def __init__(self, federation: "Federation", home: int) -> None:
        self._federation = federation
        self._home = home
        self._table = federation._tables[home]     # noqa: SLF001 - same module

    @property
    def occasions(self) -> int:
        raise NotImplementedError(
            "a node cannot know how many occasions the whole system has seen. "
            "That is the collective amended C1 forbids, and it is why `ppmi` "
            "is a reference statistic rather than a deployable one -- see "
            "CoOccurrence.moment and g33-01. Use `conditional`.")

    def seen(self, surface: int) -> int:
        return self._federation.seen(surface, asker=self._home)

    def together(self, one: int, other: int) -> int:
        return self._table.together(one, other)

    def partners(self, surface: int) -> list[int]:
        return self._table.partners(surface)


class Federation:
    """Per-owner count tables, with every crossing counted.

    Attributes:
        nodes: How many machines hold rows.
        writes: Row updates applied, wherever they landed.
        remote_writes: Of those, how many went to a node other than the one that
            produced the update.
        remote_reads: `count(y)` lookups served by a node other than the asker.
            **This is the price of the only statistic that works** — see
            `rank` — and it is the number the design owes an answer for.
        hops: Node-to-node steps taken by `walk`.
    """

    def __init__(self, nodes: int = 8, seed: int = 0) -> None:
        if nodes < 1:
            raise ValueError("a federation needs at least one node")
        self.nodes = nodes
        self._ring = Ring(nodes=nodes, seed=seed)
        self._tables = [CoOccurrence() for _ in range(nodes)]
        self.writes = 0
        self.remote_writes = 0
        self.remote_reads = 0
        self.hops = 0
        self.unreachable = 0
        self._absent: set[int] = set()

    def owner(self, surface: int) -> int:
        """Which node holds this surface's row. No directory, no message."""
        return self._ring.owner(surface)

    def lose(self, node: int) -> None:
        """That node vanishes, and **its rows vanish with it.**

        `partitioned.ConceptStore.lose` keeps a concept reachable because it is
        held on `replicas` nodes and a read falls through to a survivor. **The
        grounding store has no replicas at all**, so this is the harsher case:
        every surface that node owned is gone outright, and no survivor holds a
        copy to fall through to.

        Two costs follow and only the first is obvious:

        - **the rows it held are lost.** Everything ever learned about those
          surfaces, permanently, because nothing is replicated and nothing is
          repaired;
        - **every SURVIVING surface loses those as candidates.** Ranking needs
          `count(y)` from `owner(y)`, so a partner on a departed node cannot be
          scored by anyone. A concept with one surface here is damaged even
          though its other surfaces are untouched.

        The second is why a departure costs more than its share of the ring, and
        measuring how much more is what `g35-02` is for.
        """
        self._absent.add(node)

    def present(self, surface: int) -> bool:
        """Whether this surface's owner is still here."""
        return self.owner(surface) not in self._absent

    def note(self, surface: int, *, sender: int | None = None) -> None:
        """Record that a surface was present, at its own owner."""
        target = self.owner(surface)
        self._tables[target].note(surface)
        self.writes += 1
        if sender is not None and sender != target:
            self.remote_writes += 1

    def link(self, one: int, other: int, *, sender: int | None = None) -> None:
        """Record that two surfaces met — **two messages, one per owner.**

        Each owner writes only its own direction. That is the whole reason
        `CoOccurrence.observed_with` exists: a node writing both halves would be
        reaching into a row it does not hold.
        """
        for surface, partner in ((one, other), (other, one)):
            target = self.owner(surface)
            self._tables[target].observed_with(surface, partner)
            self.writes += 1
            if sender is not None and sender != target:
                self.remote_writes += 1

    def seen(self, surface: int, *, asker: int | None = None) -> int:
        """How many occasions a surface was present on, from its owner.

        Counts a remote read when the asker is elsewhere, which is what makes
        the cost of `conditional` visible instead of implied.

        Raises:
            KeyError: if the owner has departed. **Not a zero** — a marginal of
                zero is an ordinary count that drives every chance-corrected
                score to zero, so a departed peer would read as a surface that
                simply never appeared. `rank` catches this and drops the
                candidate, which is the local and graceful thing to do; nothing
                else may treat it as data.
        """
        target = self.owner(surface)
        if target in self._absent:
            self.unreachable += 1
            raise KeyError(
                f"node {target} owns surface {surface} and has departed. "
                f"Returning 0 would make it look like a surface nobody ever "
                f"saw, which is a count rather than an absence")
        if asker is not None and asker != target:
            self.remote_reads += 1
        return self._tables[target].seen(surface)

    def partners_of(self, surface: int) -> list[int]:
        """Every surface ever seen beside this one, from its owner's row.

        A purely local read — the row belongs to that node — and it is the
        quantity the read cost scales with, so it is worth being able to ask for
        without reaching into the tables.
        """
        return self._tables[self.owner(surface)].partners(surface)

    def rank(self, surface: int, statistic: Statistic, k: int | None,
             look: int = 16) -> list[int]:
        """The `k` strongest partners of a surface, computed AT ITS OWNER.

        The owner has `count(surface, y)` for every `y` it has ever seen beside
        it, and `count(surface)`. It does **not** have `count(y)`, so a statistic
        that needs it pays one remote read per candidate — counted in
        `remote_reads`, and the reason this method exists rather than a call to
        `grounding.neighbours`.

        Zero scores are dropped rather than padding the list, exactly as
        `grounding.neighbours` does, so a statistic refusing a partner is not
        overruled by a quota.

        `k=None` derives the bound per surface from its own ranking, which is
        `g33-04`'s winner and the arrangement actually in use. **It was missing
        here until `g35-02` tried to use it**, so every federated number before
        that was taken at a fixed bound — which is stated where those numbers
        are, and is why `g33-03`'s read cost says `k 2` in its config block.
        """
        if k is not None and k < 1:
            raise ValueError("k must be at least 1")
        if look < 1:
            raise ValueError("look must be at least 1")
        home = self.owner(surface)
        view = _AtOwner(self, home)
        scored: list[tuple[float, int]] = []
        for other in view.partners(surface):
            # The remote read is charged by the VIEW, and only when the statistic
            # actually asks for `seen`. `local_conditional` asks only about the
            # surface itself, which its owner already holds, so it pays nothing
            # -- which is the whole reason it is worth measuring and the reason
            # this cost must not be charged unconditionally.
            try:
                score = statistic(view, surface, other)
            except KeyError:
                # The candidate's owner has left. DROPPING it is local and
                # graceful: this node cannot score it and cannot know what it
                # would have scored, so pretending otherwise is the only way to
                # get a wrong answer rather than a smaller one.
                continue
            if score > 0.0:
                scored.append((score, other))
        scored.sort(key=lambda pair: (-pair[0], pair[1]))
        if k is None:
            from openplexus.grounding import cliff
            window = scored[:look]
            keep = cliff([score for score, _ in window])
            return [other for _, other in window[:keep]]
        return [other for _, other in scored[:k]]

    def walk(self, start: int, statistic: Statistic, k: int | None,
             look: int = 16) -> frozenset[int]:
        """The equivalence class reached from one surface, by actual hops.

        `grounding.equivalence_classes` computes every class at once over a
        single table, which no node could do. This is the same rule executed the
        way a reader would: ask a surface's owner for its strongest partners, ask
        each of those owners in turn, and keep an edge only where the ranking is
        mutual.

        Returns:
            The set reached, including `start`. A surface with no returned link
            comes back alone.
        """
        # ONE RANKING PER SURFACE PER WALK, and it is not an optimisation.
        #
        # Checking mutuality means ranking the other end too, and a naive walk
        # re-ranks the same surface once per edge that touches it -- measured at
        # about EIGHT times the necessary work, so the cost of the design read
        # eight times worse than it is. A node ranking the same surface twice
        # inside one query has asked its peers the same question twice, which is
        # a real message on a real link.
        #
        # This is per WALK and deliberately not longer-lived: a cache that
        # outlives the query would be answering from a snapshot, and C4 says the
        # counts never stop changing.
        ranked: dict[int, list[int]] = {}

        def rank_once(surface: int) -> list[int]:
            if surface not in ranked:
                ranked[surface] = self.rank(surface, statistic, k, look)
            return ranked[surface]

        reached = {start}
        frontier = [start]
        while frontier:
            here = frontier.pop()
            for other in rank_once(here):
                if here not in rank_once(other):
                    continue                       # not mutual, so not an edge
                self.hops += 1
                if other not in reached:
                    reached.add(other)
                    frontier.append(other)
        return frozenset(reached)

    def holdings(self) -> list[set[int]]:
        """Which surfaces each node holds a row for. **The locality proof.**

        A node holding a surface it does not own is the failure this whole module
        exists to make impossible to miss, and `test_federated` asserts over this
        rather than over any claim in a docstring.

        Uses `CoOccurrence.rows` and not `surfaces`, because a pair can reach an
        owner before any marginal does — so a row can exist with nothing observed
        in it, and that is precisely the row a narrower check would miss.
        """
        return [set(table.rows()) for table in self._tables]

    def busiest_share(self) -> float:
        """Largest share of rows any one node holds."""
        held = [len(rows) for rows in self.holdings()]
        total = sum(held)
        return max(held) / total if total else 0.0
