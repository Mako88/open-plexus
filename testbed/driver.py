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


def latency_summary(latencies: list[float]) -> dict:
    """Percentiles of the vote round trip, in milliseconds.

    A mean cannot size a timeout. SWIM sets its indirect-probe timeout from the
    mean OR the 99th percentile of the round-trip distribution and requires the
    protocol period to be at least three times the estimate -- so the tail is
    the load-bearing part, and reporting only a mean would hide exactly the
    samples a bound has to cover.

    `t_prime_floor_ms` is SWIM's `T' >= 3 x RTT` evaluated at p99, which is the
    first thing in this project to put a waiting parameter in a measured unit.
    """
    if not latencies:
        return {"votes_timed": 0}
    ordered = sorted(latencies)

    def at(fraction: float) -> float:
        index = min(len(ordered) - 1, int(fraction * len(ordered)))
        return round(ordered[index] * 1000, 3)

    return {
        "votes_timed": len(ordered),
        "rtt_ms_mean": round(sum(ordered) / len(ordered) * 1000, 3),
        "rtt_ms_p50": at(0.50),
        "rtt_ms_p99": at(0.99),
        "rtt_ms_max": round(ordered[-1] * 1000, 3),
        "t_prime_floor_ms": round(at(0.99) * 3, 3),
    }
WINDOW = int(os.environ.get("OPENPLEXUS_WINDOW", "1"))
# Departure. `absent` names nodes BY INDEX, which is exactly what the slice
# handshake exists to make meaningful: before it, the driver indexed connections
# by arrival order, and under a delayed link arrival order is whatever the
# network decides. A churn measurement taken then would have removed a node
# chosen at random and reported it as node 0.
ABSENT = os.environ.get("OPENPLEXUS_ABSENT", "")
LEAVE_AT = int(os.environ.get("OPENPLEXUS_LEAVE_AT", "0"))


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
        absent = {int(i) for i in ABSENT.split(",") if i.strip()}
        predictions = net.run(tokens, window=WINDOW,
                              absent=absent or None,
                              leave_at=LEAVE_AT or None)
        elapsed = time.monotonic() - started

    # With nodes departing, agreement with the whole network is NOT expected --
    # the network really has lost part of its memory. What is reported is where
    # the answers diverge, and the honest check is that they are identical
    # BEFORE the departure step and only differ after it. A difference before
    # `leave_at` means something other than the departure caused it.
    agree = bool(np.array_equal(predictions, expected))
    early = (0 if not LEAVE_AT
             else int((predictions[:LEAVE_AT] != expected[:LEAVE_AT]).sum()))
    print(json.dumps({
        "nodes": NODES,
        "d_model": D_MODEL,
        "window": WINDOW,
        "steps": int(STEPS),
        "join_seconds": round(joined, 3),
        "run_seconds": round(elapsed, 3),
        "seconds_per_step": round(elapsed / max(1, STEPS), 5),
        # THE ROUND-TRIP DISTRIBUTION, which is what a timeout has to be built
        # from. SWIM sets its indirect-probe timeout from an estimate of this
        # -- the mean or the 99th percentile -- and requires the protocol
        # period to be at least three times it. Everything in this project that
        # waits is currently tuned in STEPS, which have no fixed duration.
        #
        # Reported as percentiles rather than a mean alone: a mean cannot size
        # a timeout, because the whole question is how far the tail runs.
        **latency_summary(net.vote_latencies),
        # The load-bearing field. An impaired link can make a network slow or
        # wrong, and only this separates them.
        "agrees_with_one_process": agree,
        "mismatches": int((predictions != expected).sum()),
        "absent": sorted({int(i) for i in ABSENT.split(",") if i.strip()}),
        "leave_at": LEAVE_AT,
        # Divergence before the departure step. Must be zero whatever else
        # happens: a departure cannot change an answer that was already given.
        "mismatches_before_departure": early,
    }), flush=True)
    # A run WITH a departure is not required to agree -- it is required not to
    # diverge early.
    return 0 if (agree if not LEAVE_AT else early == 0) else 1


if __name__ == "__main__":
    raise SystemExit(main())
