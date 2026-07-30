"""One node of a distributed run, as its own OS process or container.

## Why this exists

`distributed.Network(spawn=False)` was built for exactly this and has never been used
outside a test. Its docstring says why it matters:

> *"Turning it off is what lets a node be a container on the other end of an emulated
> link, which is the only way G2, G3 and G4 stop being modelled."*

Everything measured so far spawns nodes as child processes on loopback. That is a real
socket and a real packet, and it is still one machine, one kernel, one clock, no
container boundary and no emulated latency. **This is the entry point that removes those.**

## What a node has to reconstruct rather than receive

The driver hands a spawned node `values[:, lo:hi]` and `readout[:, lo:hi]`. A container
cannot be handed anything, so it **rebuilds the model from the config and slices it
itself** — which works only because both sides are deterministic in `seed`, and is the
same property `derived_keys` rests on.

**If the two sides disagree about the config they will disagree silently**, producing votes
about a different model rather than an error. So the driver prints a fingerprint and this
prints one too, and they must match.

## Configuration

All by environment, because a container has no argv worth speaking of:

    DRIVER_HOST, DRIVER_PORT   where to connect
    SLICE_LO, SLICE_HI         this node's contiguous slice of the width
    SEED, D_MODEL, VOCAB_SIZE  the config, which must match the driver's exactly
    COMBINE                    `sum` or `vote`
    CONNECT_TIMEOUT            how long to keep retrying the driver
"""

from __future__ import annotations

import hashlib
import os
import socket
import sys
import time
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from openplexus.distributed import Slice, serve  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)


def build(seed: int, d_model: int, vocab: int) -> LocalMemoryConfig:
    """The config both sides must agree on, in one place so they cannot drift."""
    # `context_keys` is DELIBERATELY OFF, and finding out why is the point.
    #
    # A pair key is derived from `(previous, current)`. The driver broadcasts ONE
    # token id -- four bytes, per note 012 -- and `distributed.py` says that is
    # sufficient because a node redraws key row `t` from `(seed, t)`. **With pair
    # keys it is not sufficient: the node cannot know `previous`.** So the node
    # builds a different key from the driver's, and the split stops being exact.
    #
    # Measured: with `context_keys=True` the exactness check reports False; with it
    # off, True. See note 086.
    return LocalMemoryConfig(vocab_size=vocab, d_model=d_model, seed=seed,
                             derived_keys=True, lr=0.05, key_scale=0.5, decay=0.9)


def model_for(config: LocalMemoryConfig) -> LocalAssociativeMemory:
    """The model BOTH sides build, including the one line that stops it being vacuous.

    **`wo` is learned by the delta rule and starts at zeros**, so an untrained model
    scores every token 0 and predicts token 0 forever. Every node is then
    interchangeable, a departure changes nothing, and an exactness check compares
    all-zeros against all-zeros and passes whatever the nodes do.

    `tests/test_connection_order.py` hit this first and says so in as many words:
    *"which is exactly what the vacuity guard below caught on the first attempt."*
    This harness hit it second — every container run reported `exact=True` while one
    node was serving a completely different model. Seeding `wo` from `wv` is the fix,
    and `driver` carries the guard.
    """
    model = LocalAssociativeMemory(config)
    model.wo[:] = model.wv
    return model


def fingerprint(values: np.ndarray, readout: np.ndarray) -> str:
    """A short digest of the matrices, so a config mismatch is loud not silent.

    Two sides disagreeing about `seed` would each be confident and produce votes
    about different models. Nothing downstream could tell.
    """
    digest = hashlib.sha256()
    for array in (values, readout):
        digest.update(np.ascontiguousarray(array, dtype=np.float64).tobytes())
    return digest.hexdigest()[:12]


def main() -> int:
    host = os.environ.get("DRIVER_HOST", "127.0.0.1")
    port = int(os.environ["DRIVER_PORT"])
    lo, hi = int(os.environ["SLICE_LO"]), int(os.environ["SLICE_HI"])
    seed = int(os.environ.get("SEED", "0"))
    d_model = int(os.environ.get("D_MODEL", "64"))
    vocab = int(os.environ.get("VOCAB_SIZE", "64"))
    combine = os.environ.get("COMBINE", "sum")
    timeout = float(os.environ.get("CONNECT_TIMEOUT", "60"))

    config = build(seed, d_model, vocab)
    model = model_for(config)
    print(f"node [{lo},{hi}) fingerprint {fingerprint(model.wv, model.wo)} "
          f"-> {host}:{port}", flush=True)

    # Retry `serve` ITSELF rather than probing first. A probe connection is
    # indistinguishable from a node connecting: the driver accepts it, waits for
    # the slice announcement, gets a close instead, and reports "peer closed
    # after 0 of 4 bytes -- a partial message is not a departure, it is a bug".
    # It was right; the bug was the probe.
    deadline = time.monotonic() + timeout
    while True:
        try:
            serve(config, Slice(lo, hi), host, port,
                  model.wv[:, lo:hi].copy(), model.wo[:, lo:hi].copy(), combine)
            break
        except OSError as failure:
            # In a compose file the driver and nodes start together, so a refusal
            # here means "not listening yet" and startup order must not be a
            # correctness requirement.
            if time.monotonic() > deadline:
                print(f"node [{lo},{hi}) gave up on {host}:{port}: {failure}",
                      flush=True)
                return 1
            time.sleep(0.25)
    print(f"node [{lo},{hi}) driver hung up, exiting", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
