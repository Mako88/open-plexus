"""Phase 0's cost row: tokens a second at each size, and how big a state is.

WHAT THIS IS FOR. The design doc's Phase 0 exit asks for "a first cost row:
tokens a second at 0.4B and at 1.5B". Every later reading divides a score by
compute, and the cost meter's flops estimate is `2 * params * tokens` -- an
estimate, not a measurement. This is the row that says what those estimated
flops actually buy on THIS card, so a Phase 1 number can be read as a rate and
not just as a count.

IT ALSO SETTLES AN ASSUMPTION THE DOC MAKES. `core` says the state is "on the
order of tens of megabytes for 1.5B". That is a guess in a design document. It
is measured here, because the state's size is what a `Thread` writes every turn
and what Phase 5 would gossip.

  uv run python scripts/phase0_cost.py --sizes 0.4 1.5

One GPU, shared: this loads one core at a time and frees it before the next.
"""

from __future__ import annotations

import argparse
import json
import platform
import subprocess
import time
from datetime import UTC, datetime
from pathlib import Path

# A prompt long enough that the prefill is measured rather than the overhead of
# starting to measure. Fixed text, so the row is comparable across sizes.
PREFILL = (
    "User: Here is some background. The Halloway house sits at the end of Ferrin Lane. "
    "Marta Halloway keeps bees, eleven hives of them, and her brother Osric repairs "
    "clocks in the front room. The roof was replaced in the spring after a storm took "
    "half the tiles off. Summarise that in one sentence.\n\nAssistant:"
)


def gpu_name() -> str:
    """The card's name for the reading, or "unknown". NEVER an exception.

    This is a label on a row, not a measurement. A box without `nvidia-smi` on
    its path, or one where it hangs, must not cost somebody a completed reading
    that already took an hour of GPU time to produce -- so the catch is broad on
    purpose and the `check` is off on purpose.
    """
    try:
        out = subprocess.run(
            ["nvidia-smi", "--query-gpu=name", "--format=csv,noheader"],
            capture_output=True,
            text=True,
            timeout=20,
            check=False,
        )
        return out.stdout.strip().splitlines()[0]
    except Exception:  # noqa: BLE001 -- a label is never worth losing a reading over
        return "unknown"


def measure(size: float, budget: int, repeats: int) -> dict:
    import torch

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore

    checkpoint = ensure(size)
    load_started = time.perf_counter()
    core = RwkvCore(checkpoint)
    load_seconds = time.perf_counter() - load_started

    tokens = core.encode(PREFILL)

    # ONE UNTIMED PASS FIRST. The first forward pays for TorchScript compiling
    # the modules and for cuBLAS choosing its kernels, and on a cold card that
    # is seconds. Reading it as the model's speed would make the 0.4B look
    # slower than the 1.5B, which is the sort of number that gets believed.
    core.feed(tokens[:32], None)

    prefill, decode, state_bytes = [], [], 0
    for _ in range(repeats):
        state, feed_cost = core.feed(tokens, None)
        prefill.append(len(tokens) / feed_cost.seconds)
        state_bytes = core.state_bytes(state)

        out, _, gen_cost = core.generate(tokens, None, budget)
        # The generate cost covers prefill AND decode; the decode rate is what
        # a turn actually waits on, so the prefill time is subtracted out.
        decode_seconds = gen_cost.seconds - (len(tokens) / prefill[-1])
        if out and decode_seconds > 0:
            decode.append(len(out) / decode_seconds)

    row = {
        "size_b": size,
        "checkpoint": checkpoint.name,
        "params": core.params,
        "n_layer": core.n_layer,
        "n_embd": core.n_embd,
        "load_seconds": round(load_seconds, 2),
        "prefill_tokens_per_second": round(sum(prefill) / len(prefill), 1),
        "decode_tokens_per_second": round(sum(decode) / len(decode), 1) if decode else None,
        "state_bytes": state_bytes,
        "state_mb": round(state_bytes / 1e6, 2),
        "weights_mb": round(checkpoint.stat().st_size / 1e6, 1),
        "cuda_max_allocated_mb": round(torch.cuda.max_memory_allocated() / 1e6, 1),
    }

    del core
    torch.cuda.empty_cache()
    torch.cuda.reset_peak_memory_stats()
    return row


def main() -> int:
    import torch

    parser = argparse.ArgumentParser()
    parser.add_argument("--sizes", type=float, nargs="+", default=[0.4, 1.5])
    parser.add_argument("--budget", type=int, default=64)
    parser.add_argument("--repeats", type=int, default=3)
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    rows = []
    for size in args.sizes:
        print(f"-- {size}B --", flush=True)
        row = measure(size, args.budget, args.repeats)
        print(json.dumps(row, indent=2), flush=True)
        rows.append(row)

    reading = {
        "phase": 0,
        "kind": "cost",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "tokens a second and state size per checkpoint, inference only",
        "budget": args.budget,
        "repeats": args.repeats,
        "machine": {
            "gpu": gpu_name(),
            "torch": torch.__version__,
            "arch_list": torch.cuda.get_arch_list(),
            "capability": list(torch.cuda.get_device_capability(0)),
            "platform": platform.platform(),
            "rwkv_cuda_kernel": False,
        },
        "rows": rows,
    }
    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase0-cost-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")
    print(f"\nwrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
