"""Drive a distributed run whose nodes are separate containers, and check it is exact.

## What this measures that nothing has measured

Every distributed number in this project came from `Network(spawn=True)` — child
processes on loopback. Real sockets, real packets, and still one kernel, one clock, no
container boundary, no emulated latency. `spawn=False` exists to remove those and its
docstring says so: *"the only way G2, G3 and G4 stop being modelled."*

So this reports three things, in the order they matter:

    EXACTNESS   the distributed prediction must equal the in-process one. If the
                split is not exact, no timing number means anything
    LATENCY     per-step wall time, which is what `docs/SCALE.md` says is the
                binding constraint -- ten sequential hops at ~50 ms is ~500 ms
                against `d_max`'s 640 ms, about 20% headroom
    CHURN       with `deadline`, a step settles on what arrived. A node that stops
                answering must cost a candidate, not the run

## The window, and why 1 is the wrong default to celebrate

`Network.run(window=1)` is lock-step: *"every node must answer before anyone moves,
which is precisely the global synchronisation C1 forbids."* So a lock-step number is a
control, not a result, and both are reported.
"""

from __future__ import annotations

import argparse
import os
import statistics
import sys
import time
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT / "tools"))

from cluster_node import build, fingerprint, model_for  # noqa: E402

from openplexus.distributed import Network, slices_for  # noqa: E402
from openplexus.models.local_memory import LocalAssociativeMemory  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--nodes", type=int,
                        default=int(os.environ.get("NODES", "4")))
    parser.add_argument("--port", type=int,
                        default=int(os.environ.get("DRIVER_PORT", "9099")))
    parser.add_argument("--host", default=os.environ.get("BIND_HOST", "0.0.0.0"))
    parser.add_argument("--seed", type=int, default=int(os.environ.get("SEED", "0")))
    parser.add_argument("--d-model", type=int,
                        default=int(os.environ.get("D_MODEL", "64")))
    parser.add_argument("--vocab", type=int,
                        default=int(os.environ.get("VOCAB_SIZE", "64")))
    parser.add_argument("--steps", type=int, default=64)
    parser.add_argument("--windows", type=int, nargs="+", default=[1, 4])
    parser.add_argument("--deadline", type=float, default=None,
                        help="settle a step on what arrived, in seconds")
    args = parser.parse_args()

    config = build(args.seed, args.d_model, args.vocab)
    model = model_for(config)
    print(f"driver fingerprint {fingerprint(model.wv, model.wo)}   "
          f"(every node must print the same one)", flush=True)
    print(f"slices: {[(s.lo, s.hi) for s in slices_for(args.d_model, args.nodes)]}",
          flush=True)

    rng = np.random.default_rng(args.seed)
    tokens = rng.integers(0, args.vocab, args.steps)

    # THE BAR. One process, one array, no packets -- what the split must reproduce.
    wanted = model_for(config).run(tokens)

    # THE VACUITY GUARD. An untrained model predicts one token forever, and then
    # `array_equal` passes no matter what any node sends -- which is what every
    # earlier run of this tool did while a node served a different model entirely.
    distinct = len(set(wanted.tolist()))
    if distinct < 3:
        raise SystemExit(
            f"the bar predicts only {distinct} distinct token(s) over "
            f"{len(wanted)} steps, so comparing against it would pass whatever "
            f"the nodes do. Refusing to report an exactness number that cannot "
            f"fail. See `cluster_node.model_for`.")
    print(f"bar predicts {distinct} distinct tokens over {len(wanted)} steps "
          f"-- the exactness check can fail", flush=True)

    print(f"\nwaiting for {args.nodes} nodes on {args.host}:{args.port}", flush=True)
    with Network(config, args.nodes, model.wv, model.wo, host=args.host,
                 port=args.port, spawn=False) as net:
        print("all nodes connected\n", flush=True)
        print(f"{'window':>7s} {'exact':>7s} {'total s':>9s} "
              f"{'per step ms':>12s} {'p50 ms':>8s} {'short':>6s}")
        for window in args.windows:
            marks = []
            start = time.perf_counter()
            got = net.run(tokens, window=window, deadline=args.deadline)
            total = time.perf_counter() - start
            exact = bool(np.array_equal(got, wanted))
            per_step = total / len(tokens) * 1000
            short = sum(net.steps_settled_short.values()) if net.steps_settled_short else 0
            label = f"{window}" + (" (lock-step)" if window == 1 else "")
            print(f"{label:>7s} {str(exact):>7s} {total:9.3f} {per_step:12.2f} "
                  f"{per_step:8.2f} {short:6d}", flush=True)
            marks.append(per_step)
    print("\nEXACTNESS is the gate: a timing number from an inexact split measures "
          "nothing.\nwindow 1 is the lock-step CONTROL -- C1 forbids it as a "
          "deployment.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
