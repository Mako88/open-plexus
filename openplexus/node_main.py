"""A node that starts, works out what it can afford, and joins a network.

Everything else in this package is called by an experiment. This is the first
thing meant to be *launched* — as a container, on a machine nobody has inspected,
with only environment variables to go on.

    python -m openplexus.node_main

## What it decides for itself, and what it is told

**Told:** where the driver is, how many nodes the network has, and which one this
is. Slice assignment is static for now, by John's explicit choice — a node that
negotiates its own slice is a coordination protocol, and there is no measurement
yet that needs one.

**Decided:** how much of this machine to use, via `deployment.plan`, which reads
cgroup limits so a container sees its own allowance rather than the host's. The
plan is reported at startup along with what it rests on, because a deployed node
that cannot say why it chose its size is a node nobody can debug.

## Why it rebuilds the model rather than being handed one

`serve` takes the value and readout columns for its slice. In-process those are
sliced from the driver's arrays. Across containers there is nothing to slice
from, so the node rebuilds the model from the shared config and takes its own
columns — which is **bit-identical**, because the arrays are drawn from a seeded
stream and the same seed produces the same table.

## The decoder switch, which is not a detail

`wo` is learned by the delta rule and starts at **zeros**. An untrained model
therefore scores every token zero and predicts token 0 forever, and in that state
every node is interchangeable: a departure changes nothing and a network of eight
is indistinguishable from a network of one. `OPENPLEXUS_DECODER=1` sets `wo = wv`
so predictions track the memory.

This was not a guess. `tests/test_connection_order.py` was written with `wo` left
at zeros, and its vacuity guard caught it — two different nodes departing gave
identical answers. A testbed that measured latency against that model would have
reported beautiful, meaningless numbers.

## The two modes, and why they live in one file

`OPENPLEXUS_MODE=slice` — the default and everything measured to date — joins a
driver and serves a **dimension** slice. `OPENPLEXUS_MODE=peer` serves the
**concepts** it owns over `peer.py`, with no driver anywhere.

**What this does not duplicate.** `tools/cluster_driver.py` is the driver side
and stays there; `openplexus/peer.py` owns the wire and the ring lookup;
`openplexus/distributed.py` owns the slice protocol. Peer mode adds no transport,
no routing and no store — it constructs the ones that exist and serves them.

They are one entrypoint rather than two because of `CLAUDE.md` rule 19's
calibration, which is about this exact file: `tools/cluster_node.py` was written
to run a node as a container when this module already did it better, and was
deleted. A second peer-shaped entrypoint would be the same mistake with a
different name — the cgroup sizing, the decoder switch and the config agreement
are wanted identically by both, and those are the parts that were rediscovered
from scratch last time.

**Peer mode is NOT the default**, so no existing measurement moves.

## What peer mode is FOR, labelled so it does not quietly become load-bearing

It exists to measure a walk's latency over a real impaired link. `note 101`'s
finding — a hop is two dependent round trips, so depth 10 costs 1,000 ms against
`d_max` 640 ms — is **counted analytically and priced at an assumed 50 ms RTT on
loopback.** Nothing has run a walk across peers with real delay.

The durable part is the entrypoint: a peer you can launch in a container. The
fact population is **scaffolding** and is named as such at its definition, per
rule 17 — a deployed peer receives facts, it does not generate them.
"""

from __future__ import annotations

import os
import sys
import time

from openplexus.deployment import plan
from openplexus.distributed import serve, slices_for
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)
from openplexus.ownership import Ring
from openplexus.partitioned import ConceptStore
from openplexus.peer import ConceptPeer

MODE_VAR = "OPENPLEXUS_MODE"
PEER_PORT_VAR = "OPENPLEXUS_PEER_PORT"
RING_SEED_VAR = "OPENPLEXUS_RING_SEED"
REPLICAS_VAR = "OPENPLEXUS_REPLICAS"
DRIVER_HOST_VAR = "OPENPLEXUS_DRIVER_HOST"
DRIVER_PORT_VAR = "OPENPLEXUS_DRIVER_PORT"
NODE_INDEX_VAR = "OPENPLEXUS_NODE_INDEX"
NODES_VAR = "OPENPLEXUS_NODES"
D_MODEL_VAR = "OPENPLEXUS_D_MODEL"
VOCAB_VAR = "OPENPLEXUS_VOCAB_SIZE"
SEED_VAR = "OPENPLEXUS_SEED"
DECODER_VAR = "OPENPLEXUS_DECODER"
CONTEXT_KEYS_VAR = "OPENPLEXUS_CONTEXT_KEYS"


def config_from_env() -> LocalMemoryConfig:
    """The model config, which every node in a network must agree on exactly.

    Any disagreement here is silent: the nodes still run, still vote, and still
    produce a number. Only the driver's slice check would notice, and only if the
    width disagreed.

    `OPENPLEXUS_CONTEXT_KEYS=1` selects PAIR keys, which every relational result in
    this project uses. Off by default because every distributed measurement to date
    was taken with single-token keys, and note 086 is the entry about a default that
    moved quietly. `Node.key` reconstructs the pair by remembering the previous
    token; before that existed, this configuration was silently INEXACT.
    """
    return LocalMemoryConfig(
        vocab_size=int(os.environ.get(VOCAB_VAR, "41")),
        d_model=int(os.environ.get(D_MODEL_VAR, "16")),
        lr=0.05, key_scale=0.5, decay=0.9,
        derived_keys=True,
        context_keys=os.environ.get(CONTEXT_KEYS_VAR) == "1",
        seed=int(os.environ.get(SEED_VAR, "5")))


def scaffold_facts(count: int, vocab: int) -> tuple:
    """Deterministic `(entity, relation, object)` triples.

    **THIS IS SCAFFOLDING AND IS LABELLED AS SUCH**, per `CLAUDE.md` rule 17: a
    component that exists only because a measurement needs it gets named in the
    commit that adds it, before anything is measured on top of it.

    A deployed peer is GIVEN facts; it does not invent them. This exists so that
    every container in a latency measurement can populate an identical store
    without a coordinator — the same property `config_from_env` relies on, that a
    seeded stream produces the same table everywhere.

    Entities and relations overlap deliberately: several entities share a
    relation and several relations share an entity, so a routing bug that keyed
    on the wrong half of the pair would still land correctly and go unnoticed.
    That shape is taken from `tests/test_peer_reads.py`, which says why.
    """
    return tuple((1 + i % (vocab // 3), (vocab // 2) + (i % 8),
                  1 + ((i + 3) % (vocab // 3))) for i in range(count))


def derive(width: int, vocab: int, seed: int) -> tuple:
    """The value table and the keys, from seeds alone. Returns `(values, keys)`.

    **Both a peer and whatever asks it must produce these identically**, and they
    do it without exchanging anything — the same property `config_from_env`
    relies on. A disagreement is silent: the reader asks a real peer, the peer
    answers from a store keyed differently, and the answer is a vector that
    decodes to something. `peer.fingerprint` is the guard, and it can only guard
    what both sides derive the same way, which is why this is one function and
    not two matching blocks.
    """
    import numpy as np

    from openplexus.keys import PairKeys

    rng = np.random.default_rng(seed)
    values = rng.normal(0.0, 1.0, (vocab, width))
    values /= np.linalg.norm(values, axis=1, keepdims=True)
    keys = PairKeys(seed=1, spread=1.0 / np.sqrt(width), width=width,
                    start=vocab, route="first-concept", markers=frozenset({0}))
    return values, keys


def populate(index: int, nodes: int, width: int, vocab: int, seed: int,
             ring_seed: int, replicas: int, facts) -> tuple:
    """Build this node's share of the store. Returns `(store, keys, held)`.

    Split out of `serve_peer` so it can be asserted on: `serve_peer` blocks
    forever, so a test that could only reach it through the serving loop would
    have to rebuild this logic and then be checking its own copy. Rule 9 — one
    implementation per behaviour — and the copy is what would drift.
    """
    values, keys = derive(width, vocab, seed)
    store = ConceptStore(nodes=1, width=width, seed=0, replicas=1)
    ring = Ring(nodes, seed=ring_seed)

    held = 0
    for entity, relation, obj in facts:
        concept = keys.owner(entity, relation)
        # EVERY holder, not only the owner -- `ConceptStore.write` fans out so a
        # departure needs no data movement, and writing to the owner alone leaves
        # the replica fallback nothing to find.
        if index in ring.holders(concept, replicas):
            store.write(concept, keys.pair(entity, relation), values[obj])
            held += 1
    return store, keys, held


def serve_peer(config: LocalMemoryConfig) -> int:
    """Serve the concepts this node owns, with no driver anywhere.

    Every peer derives the ring, the keys, the value table and the facts from
    seeds rather than being told them, so two containers agree without
    exchanging anything. A disagreement here is silent — the peers still serve,
    and a caller asking the wrong owner gets **zeros**, which decode to whatever
    the readout prefers. That is an answer, not an error, which is why
    `peer.fingerprint` exists and why it is checked on connect.
    """
    import numpy as np

    from openplexus.keys import PairKeys

    port = int(os.environ.get(PEER_PORT_VAR, "0"))
    if not port:
        print(f"{PEER_PORT_VAR} is not set; a peer must be reachable",
              file=sys.stderr)
        return 2

    nodes = int(os.environ.get(NODES_VAR, "1"))
    index = int(os.environ.get(NODE_INDEX_VAR, "0"))
    ring_seed = int(os.environ.get(RING_SEED_VAR, "0"))
    replicas = int(os.environ.get(REPLICAS_VAR, "2"))
    width, vocab = config.d_model, config.vocab_size

    facts = scaffold_facts(int(os.environ.get("OPENPLEXUS_FACTS", "24")), vocab)
    store, keys, held = populate(index, nodes, width, vocab, config.seed,
                                 ring_seed, replicas, facts)

    peer = ConceptPeer(store, keys, host=os.environ.get("BIND_HOST", "0.0.0.0"),
                       port=port, peers=nodes, seed=ring_seed).start()
    print(f"peer {index}/{nodes} on port {peer.port}, "
          f"holding {held} of {len(facts)} facts at {replicas} replicas",
          flush=True)
    print(f"  fingerprint {peer.fingerprint.hex()}", flush=True)
    print(f"  a caller whose fingerprint differs is asking a DIFFERENT network",
          flush=True)
    try:
        while True:
            time.sleep(3600)
    except KeyboardInterrupt:
        pass
    finally:
        peer.close()
    return 0


def main(argv: list[str] | None = None) -> int:
    del argv
    config = config_from_env()
    if os.environ.get(MODE_VAR) == "peer":
        return serve_peer(config)
    nodes = int(os.environ.get(NODES_VAR, "1"))
    index = int(os.environ.get(NODE_INDEX_VAR, "0"))
    host = os.environ.get(DRIVER_HOST_VAR, "127.0.0.1")
    port = int(os.environ.get(DRIVER_PORT_VAR, "0"))
    if not port:
        print(f"{DRIVER_PORT_VAR} is not set; there is nothing to join",
              file=sys.stderr)
        return 2

    # Reported rather than obeyed. This node's width is fixed by the network's
    # slicing, so the plan is advisory here -- what it is for is saying whether
    # this machine could have hosted more, which is the question a deployment
    # asks and an experiment does not.
    afforded = plan(config.d_model, config.vocab_size)
    own = slices_for(config.d_model, nodes)[index]

    model = LocalAssociativeMemory(config)
    if os.environ.get(DECODER_VAR) == "1":
        model.wo[:] = model.wv

    print(f"node {index}/{nodes} slice [{own.lo}, {own.hi}) "
          f"joining {host}:{port}", flush=True)
    print(f"  this machine could afford {afforded.capacity} dimensions as "
          f"{afforded.nodes} node(s) of width {afforded.node_width}", flush=True)
    print(f"  basis: {afforded.basis}", flush=True)
    if own.width > afforded.capacity:
        # Not fatal: the plan spends a quarter of memory, and a node asked to do
        # more will simply use more. Saying so is the point.
        print(f"  WARNING: this slice is wider than the plan affords "
              f"({own.width} > {afforded.capacity})", flush=True)

    serve(config, own, host, port,
          model.wv[:, own.lo:own.hi].copy(),
          model.wo[:, own.lo:own.hi].copy())
    print(f"node {index} finished", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
