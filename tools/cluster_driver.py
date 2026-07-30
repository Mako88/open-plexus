"""Drive a distributed run whose nodes are containers, and check the split is exact.

## What this adds, and what it deliberately does NOT re-implement

`openplexus/node_main.py` is already the launchable node: it reads `OPENPLEXUS_*` from the
environment, sizes itself with `deployment.plan` against cgroup limits, rebuilds the model
from the shared seed and joins. **This does not duplicate any of that** — an earlier version
of this tool reimplemented it as `tools/cluster_node.py`, worse, and had to be deleted.

So the only thing here is the driver side: the bar, the guard, and the timing.

## The guard, which is the part that earned its place

`Network(spawn=False)` has been exercised only by tests. Run from a container the first
time, this tool reported `exact=True` for every configuration — **including one where a node
served a completely different model.** `wo` is learned by the delta rule and starts at zeros,
so an untrained model predicts token 0 forever and `array_equal` compared all-zeros against
all-zeros.

`tests/test_connection_order.py` had already hit that and named it. `node_main` already
exposes the fix as `OPENPLEXUS_DECODER=1`. **So this refuses to report an exactness number
when the bar is constant**, because a check that cannot fail is worse than no check.

## What the numbers mean

    EXACTNESS   the gate. A timing number from an inexact split measures nothing
    LATENCY     `docs/SCALE.md` says hop latency binds -- ten sequential hops at
                ~50 ms is ~500 ms against `d_max`'s 640 ms
    WINDOW      `run(window=1)` is lock-step, *"precisely the global
                synchronisation C1 forbids"*, so it is a CONTROL and not a result
"""

from __future__ import annotations

import argparse
import os
import sys
import time
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from openplexus import node_main  # noqa: E402
from openplexus.distributed import Network, slices_for  # noqa: E402
from openplexus.models.local_memory import LocalAssociativeMemory  # noqa: E402


def bar_for(config) -> LocalAssociativeMemory:
    """The single-process model the split must reproduce.

    Built exactly as `node_main` builds a node's, including the decoder seeding, or
    the two sides would disagree about the model and every vote would be about the
    wrong arrays.
    """
    model = LocalAssociativeMemory(config)
    if os.environ.get(node_main.DECODER_VAR) == "1":
        model.wo[:] = model.wv
    return model


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--steps", type=int, default=64)
    parser.add_argument("--windows", type=int, nargs="+", default=[1, 4])
    parser.add_argument("--deadline", type=float, default=None)
    parser.add_argument("--bind", default=os.environ.get("BIND_HOST", "0.0.0.0"))
    args = parser.parse_args()

    # ONE config function, shared with every node. Two would drift silently.
    config = node_main.config_from_env()
    nodes = int(os.environ.get(node_main.NODES_VAR, "1"))
    port = int(os.environ.get(node_main.DRIVER_PORT_VAR, "0"))
    if not port:
        raise SystemExit(f"{node_main.DRIVER_PORT_VAR} is not set")

    model = bar_for(config)
    tokens = np.random.default_rng(config.seed).integers(
        0, config.vocab_size, args.steps)
    wanted = bar_for(config).run(tokens)

    distinct = len(set(wanted.tolist()))
    if distinct < 3:
        raise SystemExit(
            f"the bar predicts only {distinct} distinct token(s) over "
            f"{len(wanted)} steps, so comparing against it would pass whatever "
            f"the nodes send. Set {node_main.DECODER_VAR}=1 — an untrained `wo` is "
            f"zeros and predicts one token forever. Refusing to report an "
            f"exactness number that cannot fail.")

    print(f"driver: d={config.d_model} vocab={config.vocab_size} "
          f"seed={config.seed} nodes={nodes}", flush=True)
    print(f"slices: {[(s.lo, s.hi) for s in slices_for(config.d_model, nodes)]}",
          flush=True)
    print(f"bar predicts {distinct} distinct tokens over {len(wanted)} steps "
          f"— the exactness check can fail", flush=True)
    print(f"waiting for {nodes} nodes on {args.bind}:{port}\n", flush=True)

    with Network(config, nodes, model.wv, model.wo, host=args.bind, port=port,
                 spawn=False) as net:
        print(f"{'window':>13s} {'exact':>7s} {'total s':>9s} "
              f"{'per step ms':>12s} {'short':>6s}", flush=True)
        for window in args.windows:
            start = time.perf_counter()
            got = net.run(tokens, window=window, deadline=args.deadline)
            total = time.perf_counter() - start
            label = f"{window} (lock-step)" if window == 1 else str(window)
            print(f"{label:>13s} {str(bool(np.array_equal(got, wanted))):>7s} "
                  f"{total:9.3f} {total / len(tokens) * 1000:12.2f} "
                  f"{sum(net.steps_settled_short.values()):6d}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
