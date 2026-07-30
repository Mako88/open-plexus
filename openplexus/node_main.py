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
"""

from __future__ import annotations

import os
import sys

from openplexus.deployment import plan
from openplexus.distributed import serve, slices_for
from openplexus.models.local_memory import (
    LocalAssociativeMemory, LocalMemoryConfig)

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


def main(argv: list[str] | None = None) -> int:
    del argv
    config = config_from_env()
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
