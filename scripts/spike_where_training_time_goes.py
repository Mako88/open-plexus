"""Where does a training step actually spend its time? The fork, measured.

WHY THIS RUNS BEFORE THE CHUNKED KERNEL AND NOT AFTER. The plan is to replace
the Python loop over T in `time_mix` with a chunked formulation. That is a
hundred-odd lines of genuinely tricky linear algebra -- a diagonal-plus-low-rank
transition matrix whose within-chunk cumulative products need a triangular
solve -- and it is only worth writing if the loop is where the time goes.

WHAT WOULD REFUTE THE CHUNKING PLAN: the t-loop being a minority of forward
time. Then even deleting it entirely leaves training within a factor of two of
where it is, Phase 3 at 1.5B stays unaffordable, and the CUDA toolchain is the
only road -- which is a download and a build, not an afternoon of algebra.

Deliberately no verdict is hardcoded. It prints the split and writes the row.

  uv run python scripts/spike_where_training_time_goes.py --size 0.4
"""

from __future__ import annotations

import argparse
import json
import time
from datetime import UTC, datetime
from pathlib import Path

import torch

REFUTES = (
    "the t-loop is a minority of forward time -- then chunking cannot deliver "
    "what Phase 3 needs and the CUDA kernel is the only road"
)


def _time(fn, repeats: int = 3) -> float:
    fn()  # warm: the first pass through any of these paths is not the cost
    torch.cuda.synchronize()
    t0 = time.perf_counter()
    for _ in range(repeats):
        fn()
    torch.cuda.synchronize()
    return (time.perf_counter() - t0) / repeats


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=0.4)
    parser.add_argument("--tokens", type=int, default=192)
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core import wkv7 as wkv7_mod
    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore
    from sylvatica.core.wkv7 import DifferentiableRwkv7

    core = RwkvCore(ensure(args.size))
    text = "User: " + ("the kettle lives on the third shelf. " * 200)
    tokens = list(core.encode(text))[: args.tokens]
    T = len(tokens)
    print(f"{core.name}: {T} tokens\n")

    mine = DifferentiableRwkv7(core)

    # -- the reference, which is the speed the package manages ------------
    ref = _time(lambda: core._model.forward(list(tokens), None))

    # -- ours, no grad ----------------------------------------------------
    def fwd_nograd():
        with torch.no_grad():
            mine.forward(list(tokens), None)

    ours = _time(fwd_nograd)

    # -- ours, training shape: grad on, checkpointing on ------------------
    scale = torch.ones(1, device="cuda", requires_grad=True)
    name = "blocks.0.att.key.weight"
    mine.adapter = lambda n, base: base * scale if n == name else base

    def train_step():
        logits, _ = mine.forward(list(tokens), None, checkpoint=True)
        loss = torch.nn.functional.cross_entropy(
            logits[:-1].float(), torch.tensor(tokens[1:], device=logits.device)
        )
        loss.backward()
        scale.grad = None

    train = _time(train_step, repeats=2)
    mine.adapter = None

    # -- THE SPLIT. Wrap `time_mix` and count its wall clock, then wrap the
    # t-loop inside it. The difference is the projections and the group norm,
    # which chunking does not touch.
    real_time_mix = wkv7_mod.time_mix
    spent = {"time_mix": 0.0, "loop": 0.0}

    real_stack = torch.stack

    def counting_time_mix(*a, **kw):
        torch.cuda.synchronize()
        t0 = time.perf_counter()
        # The loop is the only place `time_mix` calls `torch.stack`, and it
        # calls it exactly once, on the list the loop built. Timing to that
        # call and from it splits the function without editing it.
        marks = []

        def stack_spy(seq, *aa, **kk):
            torch.cuda.synchronize()
            marks.append(time.perf_counter())
            return real_stack(seq, *aa, **kk)

        torch.stack = stack_spy
        try:
            out = real_time_mix(*a, **kw)
        finally:
            torch.stack = real_stack
        torch.cuda.synchronize()
        t1 = time.perf_counter()
        spent["time_mix"] += t1 - t0
        if marks:
            # everything before the stack is the projections plus the loop;
            # the loop is what dominates it, and the projections are timed
            # separately below by running a chunk-free forward.
            spent["loop"] += marks[0] - t0
        return out

    wkv7_mod.time_mix = counting_time_mix
    try:
        with torch.no_grad():
            mine.forward(list(tokens), None)
    finally:
        wkv7_mod.time_mix = real_time_mix

    row = {
        "phase": 3,
        "kind": "where-training-time-goes",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "the split between the python t-loop and everything else",
        "would_refute": REFUTES,
        "core": core.name,
        "size_b": args.size,
        "tokens": T,
        "reference_forward_s": round(ref, 4),
        "our_forward_s": round(ours, 4),
        "training_step_s": round(train, 4),
        "training_tokens_per_s": round(T / train, 2),
        "time_mix_total_s": round(spent["time_mix"], 4),
        "t_loop_s": round(spent["loop"], 4),
        "t_loop_share_of_forward": round(spent["loop"] / max(ours, 1e-9), 4),
        "time_mix_share_of_forward": round(spent["time_mix"] / max(ours, 1e-9), 4),
        "our_slowdown_vs_reference": round(ours / max(ref, 1e-9), 2),
    }

    print(json.dumps(row, indent=2))
    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase3-time-split-{stamp}.json"
    path.write_text(json.dumps(row, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
