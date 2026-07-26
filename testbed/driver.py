"""The driver end of the testbed: waits for nodes, runs a sequence, reports.

Prints a single JSON line so the orchestrator can read a result out of container
logs without parsing prose.

**It reports agreement with the single-process model, not just an accuracy.**
That is the only measurement that distinguishes a network which is slow from one
which is wrong, and under an impaired link both are live possibilities. A run
that merely completed tells you nothing.
"""

from __future__ import annotations

import json
import os
import sys
import time

import numpy as np

sys.path.insert(0, "/app")

from openplexus.distributed import Network  # noqa: E402
from openplexus.models.local_memory import (  # noqa: E402
    LocalAssociativeMemory, LocalMemoryConfig)

PORT = int(os.environ.get("OPENPLEXUS_DRIVER_PORT", "9999"))
NODES = int(os.environ.get("OPENPLEXUS_NODES", "2"))
D_MODEL = int(os.environ.get("OPENPLEXUS_D_MODEL", "16"))
VOCAB = int(os.environ.get("OPENPLEXUS_VOCAB_SIZE", "41"))
SEED = int(os.environ.get("OPENPLEXUS_SEED", "5"))
STEPS = int(os.environ.get("OPENPLEXUS_STEPS", "60"))
WINDOW = int(os.environ.get("OPENPLEXUS_WINDOW", "1"))


def main() -> int:
    config = LocalMemoryConfig(vocab_size=VOCAB, d_model=D_MODEL, lr=0.05,
                               key_scale=0.5, decay=0.9, derived_keys=True,
                               seed=SEED)
    reference = LocalAssociativeMemory(config)
    reference.wo[:] = reference.wv          # see openplexus/node_main.py
    tokens = np.random.default_rng(3).integers(0, VOCAB, STEPS)
    expected = reference.run(tokens)

    net = Network(config, NODES, reference.wv, reference.wo,
                  host="0.0.0.0", port=PORT, spawn=False)
    waiting = time.monotonic()
    with net:
        joined = time.monotonic() - waiting
        started = time.monotonic()
        predictions = net.run(tokens, window=WINDOW)
        elapsed = time.monotonic() - started

    agree = bool(np.array_equal(predictions, expected))
    print(json.dumps({
        "nodes": NODES,
        "d_model": D_MODEL,
        "window": WINDOW,
        "steps": int(STEPS),
        "join_seconds": round(joined, 3),
        "run_seconds": round(elapsed, 3),
        "seconds_per_step": round(elapsed / max(1, STEPS), 5),
        # The load-bearing field. An impaired link can make a network slow or
        # wrong, and only this separates them.
        "agrees_with_one_process": agree,
        "mismatches": int((predictions != expected).sum()),
    }), flush=True)
    return 0 if agree else 1


if __name__ == "__main__":
    raise SystemExit(main())
