"""The chunked recurrence: is it the same forward, and how much faster? THE DIAL.

WHAT THIS DECIDES. `_recurrence_chunked` replaces the token-at-a-time loop with
C tokens at a time, and C is a free dial with a hazard at one end: the algebra
divides by W, a cumulative product of per-step decays, so a longer chunk lets
that division amplify further, with a ceiling of 0.5453^-(C/2) once the halves
are anchored at the chunk's midpoint.

THE MEASUREMENT WAS RUN EXPECTING THE CEILING TO BE PESSIMISTIC AND IT IS NOT.
It is achieved to three significant figures on both checkpoints, because it takes
only ONE channel pinned at the never-forget limit to reach it, and every RWKV-7
model has one: `max sigmoid(w0)` is 0.99966 at 0.4B and 0.99922 at 1.5B. So the
dial is checkpoint-independent and there is no real-world discount to collect.
The measurement stays because that conclusion is worth being able to point at,
and because it is the thing that would change if a future checkpoint clamped its
decays differently.

WHAT WOULD REFUTE THE CHUNKED PATH AT A GIVEN C: disagreeing with the loop by
more than the loop's own disagreement with the `rwkv` package's reference. The
loop is a hand transcription and is itself only accurate to about 1e-5 relative;
a chunked path inside that is not distinguishable from the thing it replaces,
and one outside it is a second source of error nobody asked for.

AND ONE THE SPEED HAS TO CLEAR: a chunked step that is not several times faster
is not worth a triangular solve in the training path, because Phase 3's problem
is an order of magnitude and not a factor.

  uv run python scripts/spike_chunked_wkv7.py --size 0.4
  uv run python scripts/spike_chunked_wkv7.py --size 1.5 --no-train
"""

from __future__ import annotations

import argparse
import json
import time
from datetime import UTC, datetime
from pathlib import Path

import torch

REFUTES = (
    "the chunked path disagreeing with the token-at-a-time loop by more than "
    "the loop's own disagreement with the rwkv package's reference; or a "
    "training step that is not several times faster, since Phase 3 needs an "
    "order of magnitude and not a factor"
)


def _timed(fn, reps: int = 2) -> float:
    fn()
    torch.cuda.synchronize()
    t0 = time.perf_counter()
    for _ in range(reps):
        fn()
    torch.cuda.synchronize()
    return (time.perf_counter() - t0) / reps


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=0.4)
    parser.add_argument("--tokens", type=int, default=512)
    parser.add_argument("--chunks", type=int, nargs="+", default=[8, 16, 32, 64, 128])
    parser.add_argument("--no-train", action="store_true")
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core import wkv7 as wkv7_mod
    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore
    from sylvatica.core.wkv7 import DifferentiableRwkv7

    core = RwkvCore(ensure(args.size))
    text = "User: " + ("the kettle lives on the third shelf. " * 400)
    tokens = list(core.encode(text))[: args.tokens]
    T = len(tokens)
    print(f"{core.name}: {T} tokens\n")

    m = DifferentiableRwkv7(core)

    # THE AMPLIFICATION THAT ACTUALLY OCCURS -- and it must be measured on what
    # the code MATERIALISES, not on what a cumulative decay could reach. Since
    # the halves are anchored at the chunk's midpoint, the factor multiplying c
    # and k is `mid / W`, and the factor multiplying b and r is its reciprocal.
    # An earlier version of this spy reported the unanchored `1 / W` and so
    # described an implementation that no longer existed: it read 1e30 at C=128
    # for code whose worst real factor is bounded by half a chunk.
    #
    # `mid` UNDERFLOWING IS THE ACTUAL FAILURE and is tracked separately. Once
    # the cumulative decay at the chunk's midpoint reaches fp32's floor, the
    # anchor divides by zero and every ratio is NaN, which no bound on the
    # ratios themselves would predict.
    seen: dict[int, float] = {c: 1.0 for c in args.chunks}
    floor: dict[int, float] = {c: 1.0 for c in args.chunks}
    real = wkv7_mod._recurrence_chunked

    def spy(r, w, k, v, kk, a, s, t_, h_, n_, dtype, chunk):
        ww = w.view(t_, h_, n_).float()
        for c in args.chunks:
            for start in range(0, t_, c):
                cum = torch.cumprod(ww[start : start + c], dim=0)
                mid = cum[min(c // 2, cum.shape[0] - 1)]
                seen[c] = max(seen[c], float((mid / cum).max()))
                floor[c] = min(floor[c], float(mid.min()))
        return real(r, w, k, v, kk, a, s, t_, h_, n_, dtype, chunk)

    reference, ref_state = core._model.forward(list(tokens), None)
    scale = float(reference.float().abs().max())

    with torch.no_grad():
        m.chunk = 0
        loop_s = _timed(lambda: m.forward(list(tokens), None))
        loop, loop_state = m.forward(list(tokens), None)

        loop_vs_ref = (
            float((loop[-1].float() - reference.float()).abs().max()) / scale
        )
        loop_state_vs_ref = max(
            float((a_.float() - b_.float()).abs().max())
            for a_, b_ in zip(loop_state, ref_state)
        )
        print(
            f"loop vs reference: {loop_vs_ref:.3e} relative, "
            f"state {loop_state_vs_ref:.3e}"
        )
        print(f"loop forward: {loop_s:.3f}s\n")

        rows = []
        for i, c in enumerate(args.chunks):
            m.chunk = c
            # The decay spy only needs to run once; it is pure measurement and
            # costs a cumprod per chunk per layer.
            wkv7_mod._recurrence_chunked = spy if i == 0 else real
            try:
                out, st = m.forward(list(tokens), None)
            finally:
                wkv7_mod._recurrence_chunked = real
            secs = _timed(lambda: m.forward(list(tokens), None))
            rows.append({
                "chunk": c,
                "forward_s": round(secs, 4),
                "speedup_vs_loop": round(loop_s / secs, 2),
                "vs_loop_relative": float(
                    (out[-1].float() - loop[-1].float()).abs().max()
                ) / scale,
                "vs_reference_relative": float(
                    (out[-1].float() - reference.float()).abs().max()
                ) / scale,
                "state_vs_reference": max(
                    float((a_.float() - b_.float()).abs().max())
                    for a_, b_ in zip(st, ref_state)
                ),
                "argmax_agrees": int(out[-1].argmax()) == int(reference.argmax()),
                "worst_amplification": seen[c],
                "smallest_anchor": floor[c],
            })

    for r in rows:
        # THE BAR: inside the loop's own error against the reference, with a
        # floor so that a loop which happens to agree to 1e-7 on one prompt does
        # not set an unmeetable bar for the path replacing it.
        r["inside_the_loops_own_error"] = r["vs_reference_relative"] <= max(
            loop_vs_ref * 2, 1e-5
        )
        print(
            f"chunk {r['chunk']:4d}: {r['forward_s']:6.3f}s "
            f"({r['speedup_vs_loop']:5.2f}x)  vs loop {r['vs_loop_relative']:.2e}  "
            f"vs ref {r['vs_reference_relative']:.2e}  "
            f"mid/W up to {r['worst_amplification']:.2e}  "
            f"anchor down to {r['smallest_anchor']:.2e}  "
            f"{'OK' if r['inside_the_loops_own_error'] else 'OUTSIDE'}"
        )

    train_rows = []
    if not args.no_train:
        print()
        knob = torch.ones(1, device="cuda", requires_grad=True)
        name = "blocks.0.att.key.weight"
        m.adapter = lambda n, b: b * knob if n == name else b

        def step() -> bool:
            logits, _ = m.forward(list(tokens), None, checkpoint=True)
            loss = torch.nn.functional.cross_entropy(
                logits[:-1].float(), torch.tensor(tokens[1:], device=logits.device)
            )
            loss.backward()
            ok = knob.grad is not None and bool(torch.isfinite(knob.grad).all())
            knob.grad = None
            return ok

        for c in [0] + args.chunks:
            m.chunk = c
            finite = step()
            secs = _timed(step, reps=1)
            train_rows.append({
                "chunk": c,
                "step_s": round(secs, 3),
                "tokens_per_s": round(T / secs, 1),
                "gradient_finite": finite,
            })
            print(
                f"training chunk {c:4d}: {secs:7.3f}s = {T / secs:7.1f} tok/s  "
                f"grad finite {finite}"
            )
        m.adapter = None

    row = {
        "phase": 3,
        "kind": "chunked-wkv7",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "whether the chunked recurrence is the same forward, and how much faster",
        "would_refute": REFUTES,
        "core": core.name,
        "size_b": args.size,
        "tokens": T,
        "loop_forward_s": round(loop_s, 4),
        "loop_vs_reference_relative": loop_vs_ref,
        "loop_state_vs_reference": loop_state_vs_ref,
        "chunks": rows,
        "training": train_rows,
    }
    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase3-chunked-wkv7-{args.size}b-{stamp}.json"
    path.write_text(json.dumps(row, indent=2) + "\n", encoding="utf-8")
    print(f"\nwrote {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
