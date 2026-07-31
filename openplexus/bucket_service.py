"""One node's share of the grounding store, as something a socket can wrap.

`federated.Federation` splits the count table across owners and counts every
crossing, but it holds all of them in one object — so it demonstrates that the
rows are *separable* and not that they are *separated*. A process cannot hold
"all the nodes"; it holds one.

This is that one. It owns a slice of the buckets and a slice of the surfaces,
and everything else reaches it by **message**. Nothing here opens a socket:
`handle` takes a request and returns a reply, so the whole protocol is testable
without a network and `node_main` supplies the transport — the same split
`peer.py` and `node_main.py` already use, for the same reason.

## The messages, and why there are only five

    OBSERVE bucket surface reading   an observer saw something; route to a bucket
    FLUSH   bucket                   the bucket's window closed; emit its pairs
    NOTE    surface                  count a marginal at the surface's owner
    LINK    surface other            count one DIRECTION of a pair
    SEEN    surface                  what is this surface's marginal

`NOTE` and `LINK` are what a bucket owner sends out; `SEEN` is what a reader
needs and is **the only one that is a question**. That asymmetry is the design:
writes are fire-and-forget to a named owner, and the single read is the one
`g33-03` measured at one message per candidate partner.

**There is deliberately no message for "give me your whole row".** A reader ranks
at `owner(x)`, where the row already is, so the row never travels. A protocol
with a `ROW` verb would be easier and would move the ranking to the asker, which
is the gather amended C1 forbids wearing a smaller payload.

## What this does NOT duplicate, and what was searched

Searched by capability — node, serve, service, protocol, handler, shard —
across `openplexus/`, `tools/`, `tests/`, `testbed/` and `experiments/`.

- **`openplexus/federated.py` owns the ROUTING and the accounting**, and this
  reuses `Ring` through it rather than deciding ownership again. A second
  ownership rule would drift from the first and a link would be filed where
  nobody looks.
- **`openplexus/grounding.py` (`CoOccurrence`) IS the store**, one instance here,
  holding only the surfaces this node owns.
- **`openplexus/buckets.py` owns the JOIN's arithmetic** — which bucket a reading
  falls in, which bucket counts a pair, when a bucket closes. This does not
  re-derive any of it; it holds the open buckets for the ids it owns and calls
  the same helpers.
- **`openplexus/peer.py`** serves the superposed store over sockets and is the
  precedent for this split, not a thing to extend: it answers `read(concept,
  key)` against a `d x d` matrix, where this answers counting questions against
  a sparse table.
- **`openplexus/distributed.py`** owns the slice protocol and its framing;
  `node_main` reuses that framing rather than either file inventing another.

## What is still NOT here

**No socket, no process, no container.** `node_main` mode `bucket` is where those
go. What this buys on its own is that the protocol is *complete* — a node can be
driven entirely by the five messages above, with no method call reaching across
an ownership boundary — and `tests/test_bucket_service.py` asserts exactly that.
"""

from __future__ import annotations

from openplexus.buckets import BucketConfig
from openplexus.grounding import CoOccurrence, Statistic
from openplexus.ownership import Ring


class BucketService:
    """The buckets and surface rows belonging to ONE node.

    Attributes:
        index: This node's `CoOccurrence`. Holds **only** surfaces it owns.
        sent: Messages this node has produced for other nodes, oldest first.
            Drained by `take`, so a caller with a transport can deliver them and
            a caller without one can assert on them.
    """

    def __init__(self, node: int, config: BucketConfig) -> None:
        if not 0 <= node < config.nodes:
            raise ValueError(
                f"node {node} is outside a network of {config.nodes}")
        self.node = node
        self.config = config
        self.index = CoOccurrence()
        self.sent: list[tuple[int, tuple]] = []
        self._ring = Ring(nodes=config.nodes, seed=config.seed)
        #: bucket id -> {surface: reading}. Only for buckets this node owns.
        self._open: dict[int, dict[int, int]] = {}

    def owner(self, key: int) -> int:
        """Who owns a bucket or a surface. One ring, so the two cannot drift."""
        return self._ring.owner(key)

    def owns(self, key: int) -> bool:
        return self.owner(key) == self.node

    def take(self) -> list[tuple[int, tuple]]:
        """Drain the outbox: `(destination node, message)` pairs."""
        out, self.sent = self.sent, []
        return out

    def handle(self, message: tuple):
        """Apply one message. Returns a reply for `SEEN`, otherwise `None`.

        Refuses any message about a key this node does not own. **That refusal is
        the point of the class**: a service that quietly served another node's
        surface would produce correct numbers from an arrangement that is not the
        one being claimed, and nothing downstream could tell.
        """
        verb, *rest = message
        if verb == "OBSERVE":
            bucket, surface, reading = rest
            self._require(bucket, verb)
            held = self._open.setdefault(bucket, {})
            if surface in held and held[surface] // self.config.width == bucket:
                return None
            held[surface] = reading
            return None
        if verb == "FLUSH":
            (bucket,) = rest
            self._require(bucket, verb)
            self._flush(bucket)
            return None
        if verb == "NOTE":
            (surface,) = rest
            self._require(surface, verb)
            self.index.note(surface)
            return None
        if verb == "LINK":
            surface, other = rest
            self._require(surface, verb)
            self.index.observed_with(surface, other)
            return None
        if verb == "SEEN":
            (surface,) = rest
            self._require(surface, verb)
            return self.index.seen(surface)
        raise ValueError(f"no such message: {verb!r}")

    def _require(self, key: int, verb: str) -> None:
        if not self.owns(key):
            raise ValueError(
                f"node {self.node} was sent {verb} for {key}, which node "
                f"{self.owner(key)} owns. Serving it anyway would give the "
                f"right answer from the wrong arrangement")

    def _flush(self, bucket: int) -> None:
        """Emit a closed bucket's marginals and pairs, then discard it.

        The arithmetic is `buckets.Join`'s and is not re-derived: a marginal is
        counted at the bucket its own reading centres on, a pair at the bucket
        holding the two readings' midpoint, so exactly one bucket acts and it
        decides alone.
        """
        present = self._open.pop(bucket, {})
        width = self.config.width
        items = sorted(present.items())

        noted = False
        for surface, reading in items:
            if reading // width == bucket:
                self._emit(surface, ("NOTE", surface))
                noted = True
        if noted:
            self._emit(self.node, ("MOMENT",))
        for i, (one, one_read) in enumerate(items):
            for other, other_read in items[i + 1:]:
                if ((one_read + other_read) // 2) // width != bucket:
                    continue
                self._emit(one, ("LINK", one, other))
                self._emit(other, ("LINK", other, one))

    def _emit(self, key: int, message: tuple) -> None:
        if message[0] == "MOMENT":
            # `occasions` is the one GLOBAL quantity and no node can hold it --
            # see `CoOccurrence.moment`. It is dropped here rather than sent
            # anywhere, which is why `ppmi` is unavailable across a real network
            # and `conditional` is the statistic this arrangement can serve.
            return
        self.sent.append((self.owner(key), message))

    def rank(self, surface: int, statistic: Statistic, k: int | None,
             seen: dict[int, int], look: int = 16) -> list[int]:
        """Rank a surface's partners HERE, using marginals fetched by the caller.

        Args:
            seen: `{other: count}` for every candidate, gathered by the caller
                with `SEEN` messages. Passing them in rather than fetching them
                is what keeps this class free of transport — and it makes the
                cost visible, because the caller has to have asked.

        Raises:
            KeyError: if a candidate's marginal is missing. **Deliberately not a
                default of zero**: a missing marginal silently makes every
                chance-corrected score collapse, which is exactly how the first
                federated walk returned every surface alone.
        """
        self._require(surface, "RANK")
        table = _Borrowed(self.index, seen)
        scored = [(statistic(table, surface, other), other)
                  for other in self.index.partners(surface)]
        scored = [(score, other) for score, other in scored if score > 0.0]
        scored.sort(key=lambda pair: (-pair[0], pair[1]))
        if k is not None:
            return [other for _, other in scored[:k]]
        from openplexus.grounding import cliff
        window = scored[:look]
        keep = cliff([score for score, _ in window])
        return [other for _, other in window[:keep]]

    def candidates(self, surface: int) -> list[int]:
        """Whose marginals a caller must fetch before `rank` will work."""
        self._require(surface, "CANDIDATES")
        return self.index.partners(surface)


class _Borrowed:
    """This node's row, plus marginals someone else supplied.

    The same boundary `federated._AtOwner` draws, for a caller that fetched the
    marginals over a wire instead of from a sibling object. `occasions` raises
    here too, because a node cannot know it however the marginals arrived.
    """

    def __init__(self, table: CoOccurrence, seen: dict[int, int]) -> None:
        self._table = table
        self._seen = seen

    @property
    def occasions(self) -> int:
        raise NotImplementedError(
            "a node cannot know how many occasions the whole system has seen, "
            "and fetching marginals does not change that. `ppmi` needs it; "
            "`conditional` does not. See CoOccurrence.moment and g33-01")

    def seen(self, surface: int) -> int:
        if surface in self._seen:
            return self._seen[surface]
        if self._table.seen(surface):
            return self._table.seen(surface)
        raise KeyError(
            f"no marginal for {surface}. Defaulting it to zero would make every "
            f"chance-corrected score collapse and the walk return each surface "
            f"alone, which is a failure that looks like a null result")

    def together(self, one: int, other: int) -> int:
        return self._table.together(one, other)

    def partners(self, surface: int) -> list[int]:
        return self._table.partners(surface)
